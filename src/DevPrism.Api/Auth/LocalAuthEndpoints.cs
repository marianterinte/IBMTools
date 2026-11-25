// Spec: Documentation/dev-prism-ai-specs/11_API_AND_EVENTS.md (local auth HTTP boundary).
namespace DevPrism.Api.Auth;

using Microsoft.AspNetCore.Antiforgery;

/// <summary>Defines first-run pairing, session discovery and revocation.</summary>
internal static class LocalAuthEndpoints
{
    /// <summary>Registers the public session probe and antiforgery-protected auth operations.</summary>
    public static void MapLocalAuth(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth");
        auth.MapGet("/session", (HttpContext context, LocalOperatorSession sessions, IAntiforgery antiforgery) =>
        {
            var status = context.User.Identity?.IsAuthenticated == true ? "authenticated"
                : sessions.CanPair ? "pairingRequired" : "pairingUnavailable";
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { status, csrfToken = tokens.RequestToken });
        });

        auth.MapPost("/pair", (PairRequest request, HttpContext context, LocalOperatorSession sessions) =>
        {
            if (!sessions.TryPair(request.Code, out var token, out var expiresAt))
                return Results.Problem(statusCode: 401, title: "Pairing not authorized");

            var options = SessionCookieOptions();
            options.Expires = expiresAt;
            context.Response.Cookies.Append(LocalOperatorSession.CookieName, token, options);
            return Results.NoContent();
        }).AddEndpointFilter(ValidateLocalMutation).RequireRateLimiting("pairing");

        auth.MapPost("/sign-out", (HttpContext context, LocalOperatorSession sessions) =>
        {
            sessions.Revoke(context.Request.Cookies[LocalOperatorSession.CookieName]);
            context.Response.Cookies.Delete(LocalOperatorSession.CookieName, SessionCookieOptions());
            return Results.NoContent();
        }).AddEndpointFilter(ValidateLocalMutation).RequireAuthorization();
    }

    /// <summary>Checks the packaged browser origin and antiforgery token before local mutations.</summary>
    internal static async ValueTask<object?> ValidateLocalMutation(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var context = invocation.HttpContext;
        var origin = context.Request.Headers.Origin;
        if (origin.Count != 1 || !string.Equals(origin[0], "https://localhost:5081", StringComparison.OrdinalIgnoreCase))
            return Results.Problem(statusCode: 403, title: "Local browser origin required");

        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        if (!await antiforgery.IsRequestValidAsync(context))
            return Results.Problem(statusCode: 400, title: "Invalid antiforgery token");

        return await next(invocation);
    }

    /// <summary>Uses a host-bound cookie that is never delivered over HTTP or to client code.</summary>
    private static CookieOptions SessionCookieOptions() => new()
    {
        Path = "/", HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, IsEssential = true,
    };

    /// <summary>First-run proof typed into the local application, never sent to a provider.</summary>
    internal sealed record PairRequest(string? Code);
}
