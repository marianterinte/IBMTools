namespace DevPrism.Api.Mcp;

using System.Security.Cryptography;
using System.Text.Json;

internal static class FigmaCredentialEndpoints
{
    internal static void MapFigmaCredentials(this RouteGroupBuilder api)
    {
        var endpoints = api.MapGroup("/mcp/figma-credentials");
        endpoints.MapGet("", (FigmaCredentials credentials) => Results.Ok(credentials.Describe()))
            .Produces<FigmaCredentials.Status>();
        endpoints.MapPut("", Save).Produces<FigmaCredentials.Status>().ProducesValidationProblem()
            .ProducesProblem(409).ProducesProblem(503);
        endpoints.MapDelete("", Remove).Produces<FigmaCredentials.Status>().ProducesProblem(409).ProducesProblem(503);
    }

    private static async Task<IResult> Save(HttpRequest request, FigmaCredentials credentials,
        McpSessionState sessions, CancellationToken ct)
    {
        if (!credentials.CanEdit) return NotEditable();
        Input? input;
        try { input = request.HasJsonContentType() ? await request.ReadFromJsonAsync<Input>(ct) : null; }
        catch (JsonException) { input = null; }
        var id = input?.ClientId?.Trim();
        var secret = input?.ClientSecret;
        if (!FigmaCredentials.Valid(id) || !FigmaCredentials.Valid(secret))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Enter a Client ID and a Client Secret without spaces or control characters."],
            });
        return Update(credentials, sessions, () => credentials.Save(new(id!, secret!)));
    }

    private static IResult Remove(FigmaCredentials credentials, McpSessionState sessions) =>
        Update(credentials, sessions, credentials.Remove);

    private static IResult Update(FigmaCredentials credentials, McpSessionState sessions, Action update)
    {
        if (!credentials.CanEdit) return NotEditable();
        if (!sessions.Operation.Wait(0)) return McpOperations.Busy();
        try
        {
            update();
            sessions.ForgetFigmaAuthorization();
            return Results.Ok(credentials.Describe());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException)
        {
            return Results.Problem(statusCode: 503, title: "Protected Figma credential storage is unavailable");
        }
        finally { sessions.Operation.Release(); }
    }

    private static IResult NotEditable() => Results.Problem(statusCode: 409,
        title: "Figma credentials cannot be managed by this app instance");

    private sealed class Input
    {
        public string? ClientId { get; init; }
        public string? ClientSecret { get; init; }
    }
}
