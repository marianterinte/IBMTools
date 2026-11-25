// Spec: Documentation/dev-prism-ai-specs/12_SECURITY_AND_OPERATIONS.md (local auth).
namespace DevPrism.Api.Auth;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>Binds the paired local operator to authenticated requests.</summary>
internal sealed class LocalOperatorHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    LocalOperatorSession sessions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Accepts only a valid session cookie from this process.</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!sessions.IsAuthenticated(Request.Cookies[LocalOperatorSession.CookieName]))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, LocalOperatorSession.OperatorId.ToString())], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
