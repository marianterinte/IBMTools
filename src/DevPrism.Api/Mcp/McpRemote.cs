namespace DevPrism.Api.Mcp;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DevPrism.Api.Projects;
using Microsoft.AspNetCore.WebUtilities;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Token = McpSessionState.AccessToken;

/// <summary>Only probes the two approved remote endpoints and their typed read operations.</summary>
internal static class McpRemote
{
    internal static async Task<ProbeResult> Check(McpProfile profile, Token? token, SourceSelection? source, CancellationToken ct)
    {
        var expectedTool = profile.Provider == SourceSelection.Boards ? "wit_work_item" : "get_design_context";
        var endpoint = new Uri(profile.Endpoint!);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        using var handler = new McpEndpointHandler(endpoint);
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        if (token is not null) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = endpoint, TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false, MaxReconnectionAttempts = 0,
            AdditionalHeaders = profile.Provider == SourceSelection.Boards
                ? new Dictionary<string, string> { ["X-MCP-Readonly"] = "true", ["X-MCP-Tools"] = expectedTool }
                : null,
        }, http);
        var connected = false;
        try
        {
            await using var client = await McpClient.CreateAsync(transport, cancellationToken: deadline.Token);
            var page = await client.ListToolsAsync(new ListToolsRequestParams(), deadline.Token);
            connected = true;
            var tool = page.Tools.FirstOrDefault(item => item.Name == expectedTool);
            if (source is null) return new(token is null ? "SignInRequired" : "Connected", tool is null ? "ReadToolUnavailable" : null);
            if (tool is null) return new("Connected", "ReadToolUnavailable");
            var arguments = BuildArguments(source, tool.InputSchema);
            if (arguments is null) return new("Connected", "UnsupportedReadSchema");
            var result = await client.CallToolAsync(expectedTool, arguments, cancellationToken: deadline.Token);
            if (result.IsError == true) return new("Connected", "ReadRejected");
            var readStatus = !Usable(source, result) ? "UnverifiedResponse"
                : source.Provider == SourceSelection.Figma ? "FrameResponseReceived" : "ReadSucceeded";
            return new("Connected", readStatus);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return handler.Failure switch
            {
                HttpStatusCode.Unauthorized => new("SignInRequired", null),
                HttpStatusCode.Forbidden => connected ? new("Connected", "ReadRejected") : new("AccessDenied", null),
                HttpStatusCode.TooManyRequests => new("RateLimited", null),
                _ => connected ? new("Connected", "ReadUnavailable") : new("Unavailable", null),
            };
        }
    }

    internal static string? SelectionIssue(McpProfile profile, SourceSelection source)
    {
        var uri = new Uri(source.SourceUrl);
        if (source.Provider == SourceSelection.Boards)
        {
            var parts = uri.AbsolutePath.Split('/');
            if (!string.Equals(parts[1], profile.Organization, StringComparison.Ordinal)) return "OrganizationMismatch";
            if (parts.Length != 6) return "WorkItemRequired";
        }
        else if (!QueryHelpers.ParseQuery(uri.Query).ContainsKey("node-id")) return "FrameRequired";
        return null;
    }

    private static Dictionary<string, object?>? BuildArguments(SourceSelection source, JsonElement schema)
    {
        if (!McpJson.Property(schema, "properties", out var properties) || properties.ValueKind != JsonValueKind.Object) return null;
        var uri = new Uri(source.SourceUrl);
        var parts = uri.AbsolutePath.Split('/');
        var args = new Dictionary<string, object?>();
        if (source.Provider == SourceSelection.Boards)
        {
            if (!McpJson.Property(properties, "action", out var action) || McpJson.Text(action, "type") != "string"
                || (McpJson.Property(action, "enum", out var values) && (values.ValueKind != JsonValueKind.Array
                    || !values.EnumerateArray().Any(value => value.ValueKind == JsonValueKind.String && value.GetString() == "get")))) return null;
            args["action"] = "get";
            var idKey = new[] { "id", "workItemId", "work_item_id" }.FirstOrDefault(key => McpJson.Property(properties, key, out _));
            if (idKey is null || !McpJson.Property(properties, idKey, out var idSchema)) return null;
            var idType = McpJson.Text(idSchema, "type");
            if (idType is not ("integer" or "number" or "string")) return null;
            args[idKey] = idType == "string" ? parts[5] : int.Parse(parts[5], CultureInfo.InvariantCulture);
            var projectKey = new[] { "project", "projectName", "project_name" }.FirstOrDefault(key => IsString(properties, key));
            if (projectKey is not null) args[projectKey] = Uri.UnescapeDataString(parts[2]);
        }
        else
        {
            if (!IsString(properties, "fileKey") || !IsString(properties, "nodeId")) return null;
            args["fileKey"] = parts[2];
            args["nodeId"] = QueryHelpers.ParseQuery(uri.Query)["node-id"].ToString().Replace('-', ':');
            foreach (var name in new[] { "clientLanguages", "clientFrameworks" })
                if (IsString(properties, name)) args[name] = "unknown";
        }
        if (McpJson.Property(schema, "required", out var required) && (required.ValueKind != JsonValueKind.Array
            || required.EnumerateArray().Any(key => key.ValueKind != JsonValueKind.String || !args.ContainsKey(key.GetString()!)))) return null;
        return args;
    }

    private static bool IsString(JsonElement properties, string key) => McpJson.Property(properties, key, out var info)
        && McpJson.Text(info, "type") == "string";

    private static bool Usable(SourceSelection source, CallToolResult result)
    {
        if (source.Provider == SourceSelection.Figma)
        {
            // Observed only as a response to a fileKey/nodeId-bound call. Not an import or proof of a native revision.
            return result.Content.OfType<TextContentBlock>().Take(8).Any(block => !string.IsNullOrWhiteSpace(block.Text));
        }
        var parts = new Uri(source.SourceUrl).AbsolutePath.Split('/');
        var project = Uri.UnescapeDataString(parts[2]);
        var id = int.Parse(parts[5], CultureInfo.InvariantCulture);
        if (result.StructuredContent is JsonElement structured && MatchItem(structured, project, id)) return true;
        foreach (var block in result.Content.OfType<TextContentBlock>().Take(4))
        {
            try
            {
                using var json = JsonDocument.Parse(block.Text, new JsonDocumentOptions { MaxDepth = 32 });
                if (MatchItem(json.RootElement, project, id)) return true;
            }
            catch (JsonException) { }
        }
        return false;
    }

    private static bool MatchItem(JsonElement item, string project, int id)
    {
        if (!McpJson.Property(item, "id", out _))
            foreach (var key in new[] { "workItem", "data", "value" })
                if (McpJson.Property(item, key, out var wrapped) && McpJson.Property(wrapped, "id", out _)) { item = wrapped; break; }
        return McpJson.Property(item, "id", out var identity) && identity.ValueKind == JsonValueKind.Number
            && identity.TryGetInt32(out var actualId) && actualId == id && McpJson.Property(item, "fields", out var fields)
            && string.Equals(McpJson.Text(fields, "System.TeamProject"), project, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(McpJson.Text(fields, "System.Title"))
            && new[] { "System.State", "System.Description", "Microsoft.VSTS.Common.AcceptanceCriteria" }
                .Any(field => !string.IsNullOrWhiteSpace(McpJson.Text(fields, field)));
    }

    internal sealed record ProbeResult(string Status, string? SourceStatus);
}

/// <summary>Restricts MCP requests to the endpoint selected by the caller's allowlist and bounds responses.</summary>
internal sealed class McpEndpointHandler(Uri endpoint) : DelegatingHandler(new HttpClientHandler
{
    AllowAutoRedirect = false, UseCookies = false, UseProxy = false,
})
{
    internal HttpStatusCode? Failure { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.RequestUri?.AbsoluteUri != endpoint.AbsoluteUri) throw new InvalidOperationException("Unexpected MCP destination");
        var response = await base.SendAsync(request, ct);
        try
        {
            if (!response.IsSuccessStatusCode && request.Method != HttpMethod.Delete) Failure = response.StatusCode;
            if (response.Content.Headers.ContentLength is > 1_048_576) throw new IOException("MCP response exceeds size limit");
            var original = response.Content;
            var content = new StreamContent(new McpBoundedStream(await original.ReadAsStreamAsync(ct), 1_048_576));
            foreach (var header in original.Headers) content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            response.Content = content;
            return response;
        }
        catch { response.Dispose(); throw; }
    }
}

/// <summary>Also bounds chunked and event-stream bodies; never buffers unbounded provider content.</summary>
internal sealed class McpBoundedStream(Stream inner, int limit) : Stream
{
    private int remaining = limit;
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer) => Count(inner.Read(buffer[..Math.Min(buffer.Length, remaining + 1)]));
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        Count(await inner.ReadAsync(buffer[..Math.Min(buffer.Length, remaining + 1)], cancellationToken));
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    private int Count(int bytes)
    {
        remaining -= bytes;
        if (remaining < 0) throw new IOException("Provider response exceeds size limit");
        return bytes;
    }
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
}
