// Spec: Documentation/dev-prism-ai-specs/12_SECURITY_AND_OPERATIONS.md (single-operator boundary).
namespace DevPrism.Api.Local;

using System.Net;

/// <summary>Defines the local operator and the only supported browser origin.</summary>
internal static class LocalWorkspace
{
    // Retain the original operator ID so existing rows remain visible after removing pairing.
    internal static readonly Guid OperatorId = new("9d95cc2f-af03-4494-9ac4-8ad034aa2b36");
    internal const int Port = 5081;
    internal const string Origin = "https://localhost:5081";
    internal const string DevOrigin = "http://127.0.0.1:5173";

    /// <summary>Rejects non-local requests and cross-origin access to workspace API or media.</summary>
    internal static async Task EnforceBoundary(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;
        var peer = context.Connection.RemoteIpAddress;
        if (peer?.IsIPv4MappedToIPv6 == true) peer = peer.MapToIPv4();
        if (peer is null || !IPAddress.IsLoopback(peer) || !request.IsHttps
            || !string.Equals(request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || request.Host.Port != Port)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (request.Path.StartsWithSegments("/api") || request.Path.StartsWithSegments("/media"))
        {
            context.Response.Headers.CacheControl = "no-store";
            var origin = request.Headers.Origin;
            var site = request.Headers["Sec-Fetch-Site"];
            var safeMethod = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);
            // Writes must come from the packaged origin. Reject browser requests from other origins
            // even for reads; native local clients may omit Origin on read-only requests.
            var matchesOrigin = origin.Count == 1 && string.Equals(origin[0], Origin, StringComparison.Ordinal);
            var foreignSite = site.Count > 0 && (site.Count != 1 || site[0] is not ("same-origin" or "none"));
            // The dev server proxies only readiness, not workspace data. It can preserve Fetch Metadata
            // from its local browser; any explicit Origin still has to match the packaged or dev origin.
            var devHealth = safeMethod && request.Path == "/api/v1/health" && (origin.Count == 0
                || (origin.Count == 1 && string.Equals(origin[0], DevOrigin, StringComparison.Ordinal)));
            if (!devHealth && (foreignSite || (!matchesOrigin && (!safeMethod || origin.Count != 0))))
            {
                await Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Local application origin required")
                    .ExecuteAsync(context);
                return;
            }
        }
        await next(context);
    }
}
