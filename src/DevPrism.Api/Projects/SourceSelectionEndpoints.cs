namespace DevPrism.Api.Projects;

using DevPrism.Api.Local;
using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Stores project-scoped source selections without granting provider access or making remote calls.</summary>
internal static class SourceSelectionEndpoints
{
    private const int MaxResults = 100;

    internal static void MapSourceSelections(this RouteGroupBuilder api)
    {
        var routes = api.MapGroup("/projects/{projectId:guid}/source-selections");
        routes.MapGet("", List).Produces<SelectionList>().Produces(404).ProducesProblem(503);
        routes.MapPost("", Save).Produces<SelectionSummary>().Produces<SelectionSummary>(201)
            .ProducesValidationProblem().Produces(404).ProducesProblem(409).ProducesProblem(503);
        routes.MapDelete("/{selectionId:guid}", Remove).Produces(204).Produces(404)
            .ProducesProblem(409).ProducesProblem(503);
    }

    private static async Task<IResult> List(Guid projectId, DevPrismDbContext db, IConfiguration config, CancellationToken ct)
    {
        if (!Configured(config)) return Unavailable();
        try
        {
            if (!await OwnsProject(db, projectId, ct)) return Results.NotFound();
            var rows = await SavedSelections(db, projectId).AsNoTracking().OrderBy(row => row.Provider)
                .ThenBy(row => row.ScopeKey).Take(MaxResults + 1).ToArrayAsync(ct);
            var items = new List<SelectionSummary>(Math.Min(rows.Length, MaxResults));
            foreach (var row in rows.Take(MaxResults))
            {
                if (SourceSelection.TryRead(row.Provider, row.ScopeKey, out var selection))
                    items.Add(Summary(row, selection));
            }
            return Results.Ok(new SelectionList(items, rows.Length > MaxResults));
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException) { return Unavailable(); }
    }

    private static async Task<IResult> Save(Guid projectId, SelectionRequest input, DevPrismDbContext db,
        IConfiguration config, CancellationToken ct)
    {
        if (!Configured(config)) return Unavailable();
        try
        {
            if (!await OwnsProject(db, projectId, ct)) return Results.NotFound();
            if (!SourceSelection.TryParse(input.Provider, input.SourceUrl, out var selection))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["sourceUrl"] = ["Enter an HTTPS Azure DevOps project or Boards work item URL, or a Figma design file or frame URL, for the selected provider."],
                });

            var existing = SavedSelections(db, projectId).AsNoTracking().Where(row =>
                row.Provider == selection.Provider && row.ScopeKey == selection.ScopeKey);
            if (await existing.SingleOrDefaultAsync(ct) is { } saved) return Results.Ok(Summary(saved, selection));

            var row = new ConnectionRecord
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Provider = selection.Provider,
                ScopeKey = selection.ScopeKey, Enabled = false,
            };
            db.Connections.Add(row);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return await existing.SingleOrDefaultAsync(ct) is { } duplicate
                    ? Results.Ok(Summary(duplicate, selection))
                    : Results.Problem(statusCode: 409, title: "A provider connection already uses this source");
            }
            return Results.Json(Summary(row, selection), statusCode: StatusCodes.Status201Created);
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException or DbUpdateException { InnerException: NpgsqlException })
        {
            return Unavailable();
        }
    }

    private static async Task<IResult> Remove(Guid projectId, Guid selectionId, DevPrismDbContext db,
        IConfiguration config, CancellationToken ct)
    {
        if (!Configured(config)) return Unavailable();
        try
        {
            if (!await OwnsProject(db, projectId, ct)) return Results.NotFound();
            var removed = await SavedSelections(db, projectId).Where(row => row.Id == selectionId).ExecuteDeleteAsync(ct);
            return removed == 0 ? Results.NotFound() : Results.NoContent();
        }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Problem(statusCode: 409, title: "This selection is referenced by sync history and cannot be removed");
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException) { return Unavailable(); }
    }

    private static IQueryable<ConnectionRecord> SavedSelections(DevPrismDbContext db, Guid projectId) =>
        db.Connections.Where(row => row.ProjectId == projectId && !row.Enabled && row.SecretReference == null
            && (row.Provider == SourceSelection.Boards || row.Provider == SourceSelection.Figma)
            && row.ScopeKey.StartsWith(SourceSelection.Prefix));

    private static Task<bool> OwnsProject(DevPrismDbContext db, Guid projectId, CancellationToken ct) =>
        db.Projects.AnyAsync(row => row.Id == projectId && row.OwnerId == LocalWorkspace.OperatorId, ct);

    private static bool Configured(IConfiguration config) => !string.IsNullOrWhiteSpace(config.GetConnectionString("DevPrism"));
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "Source selections are unavailable");
    private static SelectionSummary Summary(ConnectionRecord row, SourceSelection selection) =>
        new(row.Id, selection.Provider, selection.SourceUrl);

    internal sealed record SelectionRequest(string? Provider, string? SourceUrl);
    internal sealed record SelectionSummary(Guid Id, string Provider, string SourceUrl);
    internal sealed record SelectionList(IReadOnlyList<SelectionSummary> Items, bool Truncated);
}
