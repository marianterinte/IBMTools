namespace DevPrism.Api.Mcp;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

/// <summary>Manual, workstation-only diagnostic. Not a remote profile or a source reader.</summary>
internal static class FigmaDesktopCheck
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:3845/mcp");
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(6);

    internal static async Task<IResult> Run(TimeProvider clock, CancellationToken ct)
    {
        if (!Gate.Wait(0)) return Results.Problem(statusCode: 409, title: "Figma Desktop check already running");
        try
        {
            var status = await Probe(ct);
            return Results.Ok(new Observation(status, clock.GetUtcNow()));
        }
        finally { Gate.Release(); }
    }

    private static async Task<string> Probe(CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(Timeout);
        using var handler = new McpEndpointHandler(Endpoint);
        using var http = new HttpClient(handler) { Timeout = Timeout };
        // Fixed loopback address, no OAuth, no credentials and no tool calls.
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = Endpoint, TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false, MaxReconnectionAttempts = 0,
        }, http);
        try
        {
            await using var client = await McpClient.CreateAsync(transport, cancellationToken: deadline.Token);
            var tools = await client.ListToolsAsync(new ListToolsRequestParams(), deadline.Token);
            return tools.Tools.Any(tool => tool.Name == "get_design_context") ? "Connected" : "ToolNotFound";
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return "Unavailable"; }
    }

    internal sealed record Observation(string Status, DateTimeOffset CheckedAt);
}
