namespace DevPrism.Api.Mcp;

using DevPrism.Api.Local;
using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Resolves global defaults and optional project overrides without modifying source selections.</summary>
internal static class McpProfiles
{
    internal static void MapMcp(this RouteGroupBuilder api)
    {
        Map(api.MapGroup("/mcp"));
        Map(api.MapGroup("/projects/{projectId:guid}/mcp"));
    }

    private static void Map(RouteGroupBuilder routes)
    {
        routes.MapGet("/profiles", List).Produces<ProfileSummary[]>().Produces(404).ProducesProblem(503);
        routes.MapPut("/profiles/{provider}", Save).Produces<ProfileSummary>()
            .ProducesValidationProblem().Produces(404).ProducesProblem(409).ProducesProblem(503);
        routes.MapPost("/profiles/{provider}/reset", Reset).Produces<ProfileSummary>()
            .Produces(404).ProducesProblem(409).ProducesProblem(503);
        McpOperations.Map(routes);
    }

    private static async Task<IResult> List(HttpContext context, DevPrismDbContext db, IConfiguration config,
        McpSessionState sessions, McpAuthorization auth, CancellationToken ct)
    {
        var projectId = ProjectScope(context);
        if (!Configured(config)) return Unavailable();
        try
        {
            if (!await HasProject(db, projectId, ct)) return Results.NotFound();
            var rows = await db.McpProfiles.AsNoTracking().Where(row => row.ProjectId == null || row.ProjectId == projectId).ToArrayAsync(ct);
            return Results.Ok(McpProfile.Providers.Select(provider => Summary(projectId, provider, rows, sessions, auth)).ToArray());
        }
        catch (Exception error) when (DbFailure(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Save(HttpContext context, string provider, ProfileChange input, DevPrismDbContext db,
        IConfiguration config, McpSessionState sessions, McpAuthorization auth, CancellationToken ct)
    {
        var projectId = ProjectScope(context);
        if (!McpProfile.Providers.Contains(provider)) return Results.NotFound();
        if (!Configured(config)) return Unavailable();
        if (!McpProfile.TryEndpoint(provider, input.Endpoint, out var endpoint))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["endpoint"] = ["Use the official Figma MCP endpoint or the Azure DevOps MCP endpoint for an organization, as appropriate."],
            });
        try
        {
            if (!await HasProject(db, projectId, ct)) return Results.NotFound();
            var row = await db.McpProfiles.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Provider == provider, ct);
            if (row?.Version != input.ExpectedVersion) return Conflict();
            if (row is not null && row.Endpoint == endpoint && row.Enabled == input.Enabled)
                return Results.Ok(ToSummary(McpProfile.Resolve(provider, row), row.Version, sessions, auth));
            if (row is null)
            {
                row = new McpProfileRecord { Id = Guid.NewGuid(), ProjectId = projectId, Provider = provider };
                db.McpProfiles.Add(row);
            }
            row.Endpoint = endpoint;
            row.Enabled = input.Enabled;
            row.Version = Guid.NewGuid();
            await db.SaveChangesAsync(ct);
            sessions.Forget(projectId, provider);
            return Results.Ok(ToSummary(McpProfile.Resolve(provider, row), row.Version, sessions, auth));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return Conflict(); }
        catch (Exception error) when (DbFailure(error)) { return Unavailable(); }
    }

    private static async Task<IResult> Reset(HttpContext context, string provider, ProfileVersion input, DevPrismDbContext db,
        IConfiguration config, McpSessionState sessions, McpAuthorization auth, CancellationToken ct)
    {
        var projectId = ProjectScope(context);
        if (!McpProfile.Providers.Contains(provider)) return Results.NotFound();
        if (!Configured(config)) return Unavailable();
        try
        {
            if (!await HasProject(db, projectId, ct)) return Results.NotFound();
            var row = await db.McpProfiles.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Provider == provider, ct);
            if (row?.Version != input.ExpectedVersion) return Conflict();
            if (row is not null)
            {
                db.Remove(row);
                await db.SaveChangesAsync(ct);
            }
            sessions.Forget(projectId, provider);
            var fallback = await Effective(db, projectId, provider, ct);
            return Results.Ok(ToSummary(fallback, null, sessions, auth));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(); }
        catch (Exception error) when (DbFailure(error)) { return Unavailable(); }
    }

    internal static async Task<McpProfile> Effective(DevPrismDbContext db, Guid? projectId, string provider, CancellationToken ct)
    {
        var rows = await db.McpProfiles.AsNoTracking().Where(row => row.Provider == provider
            && (row.ProjectId == null || row.ProjectId == projectId)).ToArrayAsync(ct);
        return Resolve(projectId, provider, rows);
    }

    private static McpProfile Resolve(Guid? projectId, string provider, IEnumerable<McpProfileRecord> rows) =>
        McpProfile.Resolve(provider, rows.FirstOrDefault(row => row.Provider == provider && row.ProjectId == projectId)
            ?? rows.FirstOrDefault(row => row.Provider == provider && row.ProjectId is null));

    private static ProfileSummary Summary(Guid? projectId, string provider, McpProfileRecord[] rows, McpSessionState sessions,
        McpAuthorization auth) => ToSummary(Resolve(projectId, provider, rows),
            rows.FirstOrDefault(row => row.Provider == provider && row.ProjectId == projectId)?.Version, sessions, auth);

    internal static ProfileSummary ToSummary(McpProfile profile, Guid? ownVersion, McpSessionState sessions, McpAuthorization auth)
    {
        var local = sessions.Status(profile);
        return new(profile.Provider, profile.Endpoint, "streamable-http", profile.Enabled, profile.Origin, ownVersion,
            profile.Version, auth.CanAuthorize(profile.Provider), local.State, local.CheckedAt);
    }

    // The global route must never take a project scope from the query string.
    internal static Guid? ProjectScope(HttpContext context) =>
        context.Request.RouteValues.TryGetValue("projectId", out var value) && Guid.TryParse(value?.ToString(), out var projectId)
            ? projectId : null;

    internal static Task<bool> HasProject(DevPrismDbContext db, Guid? projectId, CancellationToken ct) => projectId is null
        ? Task.FromResult(true)
        : db.Projects.AnyAsync(row => row.Id == projectId && row.OwnerId == LocalWorkspace.OperatorId, ct);
    internal static bool Configured(IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DevPrism"));
    internal static bool DbFailure(Exception error) => error is NpgsqlException or TimeoutException or DbUpdateException { InnerException: NpgsqlException };
    internal static IResult Unavailable() => Results.Problem(statusCode: 503, title: "MCP configuration is unavailable");
    internal static IResult Conflict() => Results.Problem(statusCode: 409, title: "MCP configuration has changed; refresh and try again");

    internal sealed record ProfileChange(string? Endpoint, bool Enabled, Guid? ExpectedVersion);
    internal sealed record ProfileVersion(Guid? ExpectedVersion);
    internal sealed record ProfileSummary(string Provider, string? Endpoint, string Transport, bool Enabled, string Scope,
        Guid? OwnVersion, Guid? EffectiveVersion, bool AuthorizationConfigured, string Status, DateTimeOffset? CheckedAt);
}
