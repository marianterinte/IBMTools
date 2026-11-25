// Spec: Documentation/dev-prism-ai-specs/11_API_AND_EVENTS.md (scoped sync history).
namespace DevPrism.Api.Sync;

using DevPrism.Api.Local;
using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Reads local run status without probing or contacting providers.</summary>
internal static class SyncRunEndpoints
{
    /// <summary>Registers the read-only local sync history endpoints.</summary>
    public static void MapSyncRuns(this RouteGroupBuilder api)
    {
        var routes = api.MapGroup("/projects/{projectId:guid}/sync-runs");
        routes.MapGet("", List).Produces<SyncRunList>().Produces(404).ProducesProblem(503);
        routes.MapGet("/{runId:guid}", Get).Produces<SyncRunSummary>().Produces(404).ProducesProblem(503);
    }

    /// <summary>Shows up to 100 newest recorded attempts for an owned project.</summary>
    private static Task<IResult> List(Guid projectId, DevPrismDbContext db,
        IConfiguration configuration, CancellationToken ct) => Read(projectId, null, db, configuration, ct);

    /// <summary>Returns a single run only from the owned project named in the path.</summary>
    private static Task<IResult> Get(Guid projectId, Guid runId, DevPrismDbContext db,
        IConfiguration configuration, CancellationToken ct) => Read(projectId, runId, db, configuration, ct);

    /// <summary>Applies the project ownership check before querying persisted local status.</summary>
    private static async Task<IResult> Read(Guid projectId, Guid? runId,
        DevPrismDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DevPrism"))) return Unavailable();
        var ownerId = LocalWorkspace.OperatorId;
        try
        {
            if (!await db.Projects.AnyAsync(row => row.Id == projectId && row.OwnerId == ownerId, ct))
                return Results.NotFound();

            var runs = db.SyncRuns.AsNoTracking().Where(row => row.ProjectId == projectId);
            if (runId is Guid id)
            {
                var row = await runs.SingleOrDefaultAsync(run => run.Id == id, ct);
                return row is null ? Results.NotFound() : Results.Ok(ToSummary(row));
            }

            var latest = await runs.OrderByDescending(run => run.RequestedAt).ThenByDescending(run => run.Id)
                .Take(100).ToArrayAsync(ct);
            return Results.Ok(new SyncRunList(Array.ConvertAll(latest, ToSummary)));
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            return Unavailable();
        }
    }

    /// <summary>Returns only normalized status, identifiers and timestamps.</summary>
    private static SyncRunSummary ToSummary(SyncRunRecord row) => new(row.Id, row.ConnectionId,
        row.ConnectionVersion, row.Operation.ToString(), row.State.ToString(), row.RequestedAt,
        row.StartedAt, row.FinishedAt, row.ReasonCode);

    /// <summary>Does not mistake an unconfigured or offline database for an empty history.</summary>
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "Local sync history is unavailable");

    /// <summary>Contains observed local attempt fields, not an assertion of provider success.</summary>
    internal sealed record SyncRunSummary(Guid Id, Guid ConnectionId, long ConnectionVersion, string Operation,
        string State, DateTimeOffset RequestedAt, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, string? ReasonCode);

    /// <summary>Bounded local history response.</summary>
    internal sealed record SyncRunList(SyncRunSummary[] Items);
}
