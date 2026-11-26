namespace DevPrism.Api.Mcp;

using DevPrism.Api.Local;
using DevPrism.Api.Projects;
using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Explicit local authorization, MCP handshake, and ephemeral source checks.</summary>
internal static class McpOperations
{
    internal static void Map(RouteGroupBuilder routes)
    {
        routes.MapPost("/profiles/{provider}/connect", Connect).Produces<OperationResult>();
        routes.MapPost("/profiles/{provider}/refresh", Refresh).Produces<OperationResult>();
        routes.MapPost("/profiles/{provider}/disconnect", Disconnect).Produces<OperationResult>();
    }

    internal static void MapCallbacks(WebApplication app)
    {
        app.MapGet(McpAuthorization.FigmaCallbackPath, FigmaCallback).ExcludeFromDescription();
        app.MapPost("/api/v1/projects/{projectId:guid}/mcp/profiles/{provider}/sources/{selectionId:guid}/check", CheckSource)
            .Produces<OperationResult>();
    }

    private static Task<IResult> Connect(HttpContext context, string provider, OperationRequest request, DevPrismDbContext db,
        IConfiguration config, McpSessionState sessions, McpAuthorization auth, CancellationToken ct)
    {
        var projectId = McpProfiles.ProjectScope(context);
        return Execute(projectId, provider, request, db, config, sessions, async profile =>
        {
            if (!auth.CanAuthorize(provider)) return Results.Ok(new OperationResult("ClientSetupRequired"));
            if (sessions.Attempt() is not null) return Busy();
            if (provider == SourceSelection.Figma)
            {
                var attempt = McpAuthorization.NewFigmaAttempt(projectId, profile);
                sessions.PutAttempt(attempt);
                return Results.Ok(new OperationResult("AuthorizationPending", AuthorizationUrl: auth.FigmaAuthorizationUrl(attempt)));
            }
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromMinutes(3));
            McpSessionState.AccessToken? token;
            try { token = await auth.SignInBoards(timeout.Token); }
            catch (Exception) when (!ct.IsCancellationRequested) { token = null; }
            if (!await StillCurrent(db, projectId, profile, ct)) return McpProfiles.Conflict();
            if (token is null) return Results.Ok(new OperationResult("AuthorizationFailed"));
            return await Observe(db, projectId, sessions, profile, token, null, ct);
        }, ct);
    }

    private static Task<IResult> Refresh(HttpContext context, string provider, OperationRequest request, DevPrismDbContext db,
        IConfiguration config, McpSessionState sessions, CancellationToken ct)
    {
        var projectId = McpProfiles.ProjectScope(context);
        return Execute(projectId, provider, request, db, config, sessions, profile =>
            Observe(db, projectId, sessions, profile, sessions.Token(profile), null, ct), ct);
    }

    private static Task<IResult> Disconnect(HttpContext context, string provider, OperationRequest request, DevPrismDbContext db,
        IConfiguration config, McpSessionState sessions, CancellationToken ct) =>
        Execute(McpProfiles.ProjectScope(context), provider, request, db, config, sessions, profile =>
        {
            if (sessions.Attempt()?.Profile == profile) sessions.ForgetAttempt();
            sessions.Forget(profile.ProjectId, profile.Provider);
            return Task.FromResult(Results.Ok(new OperationResult("NotChecked")));
        }, ct, requireEnabled: false);

    private static Task<IResult> CheckSource(Guid projectId, string provider, Guid selectionId, OperationRequest request,
        DevPrismDbContext db, IConfiguration config, McpSessionState sessions, CancellationToken ct) =>
        Execute(projectId, provider, request, db, config, sessions, async profile =>
        {
            var row = await db.Connections.AsNoTracking().SingleOrDefaultAsync(item => item.Id == selectionId
                && item.ProjectId == projectId && item.Provider == provider && !item.Enabled && item.SecretReference == null, ct);
            if (row is null || !SourceSelection.TryRead(provider, row.ScopeKey, out var selection)) return Results.NotFound();
            if (McpRemote.SelectionIssue(profile, selection) is { } reason)
                return Results.Ok(new OperationResult(sessions.Status(profile).State, reason));
            var token = sessions.Token(profile);
            if (token is null) return Results.Ok(new OperationResult("SignInRequired"));
            return await Observe(db, projectId, sessions, profile, token, selection, ct);
        }, ct);

    private static async Task<IResult> Execute(Guid? projectId, string provider, OperationRequest request, DevPrismDbContext db,
        IConfiguration config, McpSessionState sessions, Func<McpProfile, Task<IResult>> action,
        CancellationToken ct, bool requireEnabled = true)
    {
        if (!McpProfile.Providers.Contains(provider)) return Results.NotFound();
        if (!McpProfiles.Configured(config)) return McpProfiles.Unavailable();
        if (!await sessions.Operation.WaitAsync(0, ct)) return Busy();
        try
        {
            if (!await McpProfiles.HasProject(db, projectId, ct)) return Results.NotFound();
            var profile = await McpProfiles.Effective(db, projectId, provider, ct);
            if (!Matches(profile, request)) return McpProfiles.Conflict();
            if (requireEnabled && (!profile.Enabled || profile.Endpoint is null))
                return Results.Ok(new OperationResult(profile.Enabled ? "EndpointRequired" : "Disabled"));
            return await action(profile);
        }
        catch (Exception error) when (McpProfiles.DbFailure(error)) { return McpProfiles.Unavailable(); }
        finally { sessions.Operation.Release(); }
    }

    private static async Task<IResult> FigmaCallback(HttpContext context, DevPrismDbContext db, IConfiguration config,
        McpSessionState sessions, McpAuthorization auth, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!await sessions.Operation.WaitAsync(0, ct)) return Results.Redirect("/settings/mcp?mcpAuth=busy");
        try
        {
            var query = context.Request.Query;
            var state = query["state"];
            var attempt = sessions.Attempt();
            if (attempt is null || state.Count != 1 || !McpAuthorization.Matches(state[0], attempt.State))
                return Results.Redirect("/settings/mcp?mcpAuth=expired");
            sessions.ForgetAttempt();
            var target = attempt.RouteProjectId is Guid projectId ? $"/projects/{projectId}/connections" : "/settings/mcp";
            var code = query["code"];
            if (!McpProfiles.Configured(config) || query.ContainsKey("error") || code.Count != 1 || code[0] is not string value)
                return Results.Redirect(target + "?mcpAuth=failed");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(45));
            try
            {
                if (!await StillCurrent(db, attempt.RouteProjectId, attempt.Profile, deadline.Token))
                    return Results.Redirect(target + "?mcpAuth=changed");
                var token = await auth.ExchangeFigma(value, attempt, deadline.Token);
                if (token is null) return Results.Redirect(target + "?mcpAuth=failed");
                var probe = await McpRemote.Check(attempt.Profile, token, null, deadline.Token);
                if (!await StillCurrent(db, attempt.RouteProjectId, attempt.Profile, deadline.Token))
                    return Results.Redirect(target + "?mcpAuth=changed");
                Remember(sessions, attempt.Profile, probe, token);
                return Results.Redirect(target + (probe.Status == "Connected" ? "?mcpAuth=connected" : "?mcpAuth=unverified"));
            }
            catch (Exception) when (!ct.IsCancellationRequested) { return Results.Redirect(target + "?mcpAuth=failed"); }
        }
        finally { sessions.Operation.Release(); }
    }

    private static async Task<IResult> Observe(DevPrismDbContext db, Guid? projectId, McpSessionState sessions, McpProfile profile,
        McpSessionState.AccessToken? token, SourceSelection? selection, CancellationToken ct)
    {
        var probe = await McpRemote.Check(profile, token, selection, ct);
        if (!await StillCurrent(db, projectId, profile, ct)) return McpProfiles.Conflict();
        Remember(sessions, profile, probe, token);
        return Results.Ok(new OperationResult(probe.Status, probe.SourceStatus));
    }

    private static void Remember(McpSessionState sessions, McpProfile profile, McpRemote.ProbeResult probe, McpSessionState.AccessToken? token) =>
        sessions.Set(profile, probe.Status, probe.Status is "SignInRequired" or "AccessDenied" ? null : token);

    private static async Task<bool> StillCurrent(DevPrismDbContext db, Guid? projectId, McpProfile profile, CancellationToken ct) =>
        await McpProfiles.HasProject(db, projectId, ct) && profile == await McpProfiles.Effective(db, projectId, profile.Provider, ct);

    private static bool Matches(McpProfile profile, OperationRequest request) => profile.Version == request.EffectiveVersion
        && profile.Origin == request.Scope;

    internal static IResult Busy() => Results.Problem(statusCode: 409, title: "Another MCP operation is in progress");

    internal sealed record OperationRequest(Guid? EffectiveVersion, string? Scope);
    internal sealed record OperationResult(string Status, string? SourceStatus = null, string? AuthorizationUrl = null);
}
