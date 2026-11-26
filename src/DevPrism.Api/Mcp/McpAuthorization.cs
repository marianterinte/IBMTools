namespace DevPrism.Api.Mcp;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DevPrism.Api.Local;
using DevPrism.Api.Projects;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Identity.Client;
using Token = McpSessionState.AccessToken;

/// <summary>Provider-specific delegated authorization. Client credentials are supplied to the backend only.</summary>
internal sealed class McpAuthorization(IConfiguration configuration, TimeProvider clock, FigmaCredentials figma)
{
    private const string FigmaAuthorize = "https://www.figma.com/oauth/mcp";
    private const string FigmaToken = "https://api.figma.com/v1/oauth/token";
    internal const string FigmaCallbackPath = "/oauth/mcp/figma";
    private const string Redirect = LocalWorkspace.Origin + FigmaCallbackPath;
    private const string BoardsScope = "https://mcp.dev.azure.com/wit.read";

    private string? BoardsTenant => configuration["Mcp:AzureDevOps:TenantId"];
    private string? BoardsClient => configuration["Mcp:AzureDevOps:ClientId"];

    internal bool CanAuthorize(string provider) => provider switch
    {
        SourceSelection.Figma => figma.Current is not null,
        SourceSelection.Boards => ValidGuid(BoardsTenant) && ValidGuid(BoardsClient),
        _ => false,
    };

    internal static McpSessionState.AuthAttempt NewFigmaAttempt(Guid? projectId, McpProfile profile) =>
        new(projectId, profile, RandomValue(), RandomValue());

    internal string FigmaAuthorizationUrl(McpSessionState.AuthAttempt attempt) => QueryHelpers.AddQueryString(FigmaAuthorize,
        new Dictionary<string, string?>
        {
            ["response_type"] = "code", ["client_id"] = figma.Current!.ClientId, ["redirect_uri"] = Redirect,
            ["scope"] = "mcp:connect", ["state"] = attempt.State, ["resource"] = McpProfile.FigmaEndpoint,
            ["code_challenge"] = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(attempt.Verifier))),
            ["code_challenge_method"] = "S256",
        });

    internal async Task<Token?> ExchangeFigma(string code, McpSessionState.AuthAttempt attempt, CancellationToken ct)
    {
        if (figma.Current is not { } credential || code is not { Length: > 0 and <= 4096 } || code.Any(char.IsControl)) return null;
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false })
        { Timeout = TimeSpan.FromSeconds(15) };
        using var request = new HttpRequestMessage(HttpMethod.Post, FigmaToken)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Redirect,
                ["code_verifier"] = attempt.Verifier, ["resource"] = McpProfile.FigmaEndpoint,
                ["client_id"] = credential.ClientId, ["client_secret"] = credential.Secret,
            }),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) return null;
        await using var body = await response.Content.ReadAsStreamAsync(ct);
        using var data = await JsonDocument.ParseAsync(new McpBoundedStream(body, 32 * 1024), cancellationToken: ct);
        var root = data.RootElement;
        if (!string.Equals(McpJson.Text(root, "token_type"), "Bearer", StringComparison.OrdinalIgnoreCase)
            || McpJson.Text(root, "access_token") is not { Length: > 0 and <= 16384 } token
            || !McpJson.Property(root, "expires_in", out var seconds) || seconds.ValueKind != JsonValueKind.Number
            || !seconds.TryGetInt32(out var lifetime) || lifetime <= 0
            || (McpJson.Property(root, "scope", out _) && McpJson.Text(root, "scope") != "mcp:connect")) return null;
        return new(token, clock.GetUtcNow().AddSeconds(Math.Min(lifetime, 43_200)));
    }

    internal async Task<Token?> SignInBoards(CancellationToken ct)
    {
        if (!CanAuthorize(SourceSelection.Boards)) return null;
        var tenant = Guid.Parse(BoardsTenant!).ToString();
        var client = PublicClientApplicationBuilder.Create(Guid.Parse(BoardsClient!).ToString())
            .WithAuthority(AzureCloudInstance.AzurePublic, tenant).WithRedirectUri("http://localhost").Build();
        var result = await client.AcquireTokenInteractive([BoardsScope]).WithUseEmbeddedWebView(false).ExecuteAsync(ct);
        var scopes = result.Scopes.Select(scope => scope[(scope.LastIndexOf('/') + 1)..]).ToArray();
        if (!string.Equals(result.TenantId, tenant, StringComparison.OrdinalIgnoreCase)
            || !scopes.Contains("wit.read", StringComparer.OrdinalIgnoreCase)
            || scopes.Any(scope => !new[] { "wit.read", "openid", "profile", "offline_access", "email" }.Contains(scope, StringComparer.OrdinalIgnoreCase))
            || result.ExpiresOn <= clock.GetUtcNow() || string.IsNullOrWhiteSpace(result.AccessToken)) return null;
        return new(result.AccessToken, result.ExpiresOn);
    }

    internal static bool Matches(string? actual, string expected) => actual?.Length == expected.Length
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected));

    private static bool ValidGuid(string? value) => Guid.TryParse(value, out var parsed) && parsed != Guid.Empty;
    private static string RandomValue() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}

internal static class McpJson
{
    internal static bool Property(JsonElement value, string name, out JsonElement property)
    {
        property = default;
        return value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out property);
    }

    internal static string? Text(JsonElement value, string name) => Property(value, name, out var property)
        && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
}
