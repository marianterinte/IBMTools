// Spec: Documentation/local-mvp-remote-read-runbook.md (disposable G0 read proof).
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Identity.Client;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

const string toolName = "wit_work_item";
const string resource = "https://mcp.dev.azure.com";

if (args is ["--help"])
{
    Console.WriteLine("Azure DevOps remote MCP read-only probe. Run without arguments to enter the authorized demo scope locally.");
    Console.WriteLine("Requires a dedicated Entra public client with delegated wit.read and http://localhost redirect. Never enter credentials here.");
    return 0;
}
if (args.Length != 0 || Console.IsInputRedirected) return Stop("INTERACTIVE_INPUT_REQUIRED", 2);

var organization = Read("Azure DevOps organization slug (not a URL)");
var project = Read("Exact Azure DevOps project name");
var item = Read("Synthetic work item numeric ID");
var tenant = Read("Microsoft Entra tenant ID (GUID, not a secret)");
var clientId = Read("Approved Entra application (client) ID (GUID, not a secret)");
if (!Regex.IsMatch(organization, @"\A[a-zA-Z0-9][a-zA-Z0-9-]{0,62}\z")
    || project.Length is < 1 or > 128 || project.Any(char.IsControl)
    || !int.TryParse(item, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var itemId)
    || itemId <= 0 || !Guid.TryParse(tenant, out var tenantId) || tenantId == Guid.Empty
    || !Guid.TryParse(clientId, out var applicationId) || applicationId == Guid.Empty)
    return Stop("INVALID_SCOPE_OR_CLIENT_IDS", 2);

if (!string.Equals(Read("Type READ to confirm this is an authorized synthetic work item"), "READ", StringComparison.Ordinal))
    return Stop("NOT_CONFIRMED", 2);

var endpoint = new Uri($"{resource}/{organization}", UriKind.Absolute);
using var interrupt = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; interrupt.Cancel(); };
var stage = "ENTRA_SIGN_IN";
try
{
    using var authTimeout = CancellationTokenSource.CreateLinkedTokenSource(interrupt.Token);
    authTimeout.CancelAfter(TimeSpan.FromMinutes(3));
    // MSAL handles the system-browser callback and keeps the token in process memory only.
    var authClient = PublicClientApplicationBuilder.Create(applicationId.ToString())
        .WithAuthority(AzureCloudInstance.AzurePublic, tenantId.ToString())
        .WithRedirectUri("http://localhost")
        .Build();
    Console.WriteLine("Complete sign-in and consent in the Microsoft system browser window.");
    var auth = await authClient.AcquireTokenInteractive([$"{resource}/.default"])
        .WithUseEmbeddedWebView(false)
        .ExecuteAsync(authTimeout.Token);
    // Check the granted scopes from MSAL rather than parsing or printing the token.
    var scopes = auth.Scopes.Select(scope => scope[(scope.LastIndexOf('/') + 1)..]).ToArray();
    if (!Guid.TryParse(auth.TenantId, out var actualTenant) || actualTenant != tenantId)
        return Stop("UNEXPECTED_AUTH_TENANT", 3);
    if (!scopes.Contains("wit.read", StringComparer.OrdinalIgnoreCase)
        || scopes.Any(scope => !new[] { "wit.read", "openid", "profile", "offline_access", "email" }
            .Contains(scope, StringComparer.OrdinalIgnoreCase)))
        return Stop("EXPECTED_ONLY_WIT_READ", 3);

    stage = "MCP_INITIALIZE";
    using var mcpTimeout = CancellationTokenSource.CreateLinkedTokenSource(interrupt.Token);
    mcpTimeout.CancelAfter(TimeSpan.FromSeconds(35));
    using var http = new HttpClient(new ProbeEndpointHandler(endpoint)) { Timeout = TimeSpan.FromSeconds(30) };
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    var transport = new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = endpoint,
        TransportMode = HttpTransportMode.StreamableHttp,
        AdditionalHeaders = new Dictionary<string, string> { ["X-MCP-Readonly"] = "true", ["X-MCP-Tools"] = toolName },
    }, http);
    await using var client = await McpClient.CreateAsync(transport, cancellationToken: mcpTimeout.Token);

    stage = "MCP_SCHEMA";
    var page = await client.ListToolsAsync(new ListToolsRequestParams(), mcpTimeout.Token);
    var tool = page.Tools.FirstOrDefault(candidate => string.Equals(candidate.Name, toolName, StringComparison.Ordinal));
    if (tool is null) return Stop("EXPECTED_READ_TOOL_NOT_ADVERTISED", 3);
    var schemaHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tool.InputSchema.GetRawText())));
    Console.WriteLine($"Read tool advertised: {toolName}; input schema SHA-256: {schemaHash}");
    var input = BuildReadArguments(tool.InputSchema, project, itemId);
    if (input is null) return Stop("UNRECOGNIZED_READ_SCHEMA", 3);

    stage = "MCP_BOARDS_READ";
    var result = await client.CallToolAsync(toolName, input, cancellationToken: mcpTimeout.Token);
    if (result.IsError == true) return Stop("TOOL_RETURNED_ERROR", 3);
    if (!HasUsableWorkItem(result, project, itemId, out var nativeRevision))
        return Stop("RESULT_DID_NOT_PROVE_ID_PROJECT_AND_CONTENT", 3);

    Console.WriteLine("Verified live: Azure DevOps Boards via remote MCP (Streamable HTTP).");
    Console.WriteLine($"Matched selected item and project; title and a meaningful field present; native revision present: {nativeRevision}.");
    return 0;
}
catch (MsalException error)
{
    // Error descriptions may contain account IDs, tenant names, or callback details.
    var code = Regex.IsMatch(error.ErrorCode ?? string.Empty, @"\A[a-zA-Z0-9_]{1,64}\z") ? error.ErrorCode : "unknown";
    return Stop($"{stage}_{code}", 3);
}
catch (OperationCanceledException) { return Stop($"{stage}_CANCELLED_OR_TIMED_OUT", 3); }
catch (Exception error)
{
    // Never log full exceptions, payloads, headers, passwords or tokens in probe evidence.
    return Stop($"{stage}_{error.GetType().Name}", 3);
}

/// <summary>Reads one non-secret configuration value from the operator's terminal.</summary>
static string Read(string label)
{
    Console.Write($"{label}: ");
    return Console.ReadLine()?.Trim() ?? string.Empty;
}

/// <summary>Prints a safe failure class and returns a nonzero exit status.</summary>
static int Stop(string reason, int exitCode)
{
    Console.Error.WriteLine($"Not verified: {reason}");
    return exitCode;
}

/// <summary>Maps only recognized inputs for the documented get operation, never arbitrary tool actions.</summary>
static Dictionary<string, object?>? BuildReadArguments(JsonElement schema, string project, int id)
{
    if (!Property(schema, "properties", out var props) || !Property(props, "action", out var action)) return null;
    if (Property(action, "enum", out var values) && (values.ValueKind != JsonValueKind.Array
        || !values.EnumerateArray().Any(value => value.ValueKind == JsonValueKind.String && value.GetString() == "get"))) return null;

    var idKey = new[] { "id", "workItemId", "work_item_id" }.FirstOrDefault(key => Property(props, key, out _));
    if (idKey is null || !Property(props, idKey, out var idSchema)) return null;
    var input = new Dictionary<string, object?> { ["action"] = "get", [idKey] = StringProperty(idSchema, "type") == "string"
        ? id.ToString(System.Globalization.CultureInfo.InvariantCulture) : id };
    var projectKey = new[] { "project", "projectName", "project_name" }.FirstOrDefault(key => Property(props, key, out _));
    if (projectKey is not null) input.Add(projectKey, project);
    if (Property(schema, "required", out var required) && (required.ValueKind != JsonValueKind.Array
        || required.EnumerateArray().Any(key => key.ValueKind != JsonValueKind.String || !input.ContainsKey(key.GetString()!)))) return null;
    return input;
}

/// <summary>Accepts bounded structured or JSON text results; never interprets arbitrary prose.</summary>
static bool HasUsableWorkItem(CallToolResult result, string project, int id, out bool hasRevision)
{
    hasRevision = false;
    if (result.StructuredContent is JsonElement structured && structured.GetRawText().Length <= 256_000
        && MatchItem(structured, project, id, out hasRevision)) return true;
    foreach (var block in result.Content.OfType<TextContentBlock>().Take(4))
    {
        if (block.Text.Length > 256_000) continue;
        try
        {
            using var document = JsonDocument.Parse(block.Text, new JsonDocumentOptions { MaxDepth = 32 });
            if (MatchItem(document.RootElement, project, id, out hasRevision)) return true;
        }
        catch (JsonException) { /* Non-JSON tool text cannot prove source identity. */ }
    }
    return false;
}

/// <summary>Matches the selected item and its actual Team Project before reporting usable content.</summary>
static bool MatchItem(JsonElement value, string project, int id, out bool hasRevision)
{
    hasRevision = false;
    if (!Property(value, "id", out _))
        foreach (var key in new[] { "workItem", "data", "value" })
            if (Property(value, key, out var wrapped) && Property(wrapped, "id", out _)) { value = wrapped; break; }
    if (!Property(value, "id", out var identity) || identity.ValueKind != JsonValueKind.Number
        || !identity.TryGetInt32(out var actualId) || actualId != id || !Property(value, "fields", out var fields)
        || !string.Equals(StringProperty(fields, "System.TeamProject"), project, StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(StringProperty(fields, "System.Title"))) return false;
    hasRevision = Property(value, "rev", out var revision) && revision.ValueKind == JsonValueKind.Number
        && revision.TryGetInt32(out var actualRevision) && actualRevision > 0;
    return new[] { "System.State", "System.Description", "Microsoft.VSTS.Common.AcceptanceCriteria" }
        .Any(key => !string.IsNullOrWhiteSpace(StringProperty(fields, key)));
}

/// <summary>Reads an object member without coercing unknown response types.</summary>
static bool Property(JsonElement value, string name, out JsonElement property)
{
    property = default;
    return value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out property);
}

/// <summary>Returns a string member only when present as JSON text.</summary>
static string? StringProperty(JsonElement value, string name) => Property(value, name, out var property)
    && property.ValueKind == JsonValueKind.String ? property.GetString() : null;

/// <summary>Prevents a supplied origin, redirect or unexpected SDK request from receiving the bearer credential.</summary>
internal sealed class ProbeEndpointHandler(Uri endpoint) : DelegatingHandler(new HttpClientHandler { AllowAutoRedirect = false })
{
    /// <summary>Allows only the exact official MCP endpoint selected by the operator.</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!endpoint.Equals(request.RequestUri)) throw new InvalidOperationException("Unexpected destination");
        return base.SendAsync(request, cancellationToken);
    }
}
