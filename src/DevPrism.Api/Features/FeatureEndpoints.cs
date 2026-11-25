// Spec: Documentation/dev-prism-ai-specs/11_API_AND_EVENTS.md (feature reads and creation).
namespace DevPrism.Api.Features;

using System.Security.Claims;
using System.Text.RegularExpressions;
using DevPrism.Api.Auth;
using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Maps the local Feature Explorer independently of provider availability.</summary>
internal static partial class FeatureEndpoints
{
    /// <summary>Registers the owned-project routes and applies antiforgery to local creation.</summary>
    public static void MapFeatures(this RouteGroupBuilder api)
    {
        var features = api.MapGroup("/projects/{projectId:guid}/features").RequireAuthorization();
        features.MapGet("", List).Produces<FeatureList>().Produces(401).Produces(404).ProducesProblem(503);
        features.MapGet("/{featureId:guid}", Get).WithName("GetFeature")
            .Produces<FeatureSummary>().Produces(401).Produces(404).ProducesProblem(503);
        features.MapPost("", Create).AddEndpointFilter(LocalAuthEndpoints.ValidateLocalMutation)
            .Produces<FeatureSummary>(201).ProducesValidationProblem().Produces(401).Produces(404)
            .ProducesProblem(409).ProducesProblem(503);
    }

    /// <summary>Returns the first page of an owned project's features with stable ordering.</summary>
    private static async Task<IResult> List(Guid projectId, string? search, ClaimsPrincipal actor,
        DevPrismDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        if (!DatabaseConfigured(configuration)) return Unavailable();
        var term = search?.Trim();
        if (term is { Length: > 120 }) return Invalid("search", "Enter at most 120 characters.");
        try
        {
            if (!await OwnsProject(db, actor, projectId, ct)) return Results.NotFound();
            var scope = db.Features.AsNoTracking().Where(row => row.ProjectId == projectId);
            if (!string.IsNullOrEmpty(term))
            {
                var lower = term.ToLowerInvariant();
                scope = scope.Where(row => row.Key.Contains(lower) || row.Name.ToLower().Contains(lower));
            }
            var rows = await scope.OrderBy(row => row.Key).Take(101).ToArrayAsync(ct);
            return Results.Ok(new FeatureList(rows.Take(100).Select(ToSummary).ToArray(), rows.Length > 100));
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException) { return Unavailable(); }
    }

    /// <summary>Looks up a feature without disclosing a foreign project or feature.</summary>
    private static async Task<IResult> Get(Guid projectId, Guid featureId, ClaimsPrincipal actor,
        DevPrismDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        if (!DatabaseConfigured(configuration)) return Unavailable();
        try
        {
            if (!await OwnsProject(db, actor, projectId, ct)) return Results.NotFound();
            var feature = await db.Features.AsNoTracking()
                .SingleOrDefaultAsync(row => row.ProjectId == projectId && row.Id == featureId, ct);
            return feature is null ? Results.NotFound() : Results.Ok(ToSummary(feature));
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException) { return Unavailable(); }
    }

    /// <summary>Creates a project-scoped local feature; this never talks to source providers.</summary>
    private static async Task<IResult> Create(Guid projectId, FeatureRequest request, ClaimsPrincipal actor,
        DevPrismDbContext db, IConfiguration configuration, TimeProvider time, CancellationToken ct)
    {
        if (!DatabaseConfigured(configuration)) return Unavailable();
        try
        {
            if (!await OwnsProject(db, actor, projectId, ct)) return Results.NotFound();
            var name = request.Name?.Trim();
            var key = request.Key?.Trim().ToLowerInvariant();
            var description = request.Description?.Trim() ?? string.Empty;
            if (name is not { Length: > 0 and <= 160 }) return Invalid("name", "Enter a feature name with 1–160 characters.");
            if (key is not { Length: > 0 and <= 80 } || !KeyPattern().IsMatch(key))
                return Invalid("key", "Use up to 80 letters, numbers and single hyphens.");
            if (description.Length > 2000) return Invalid("description", "Enter at most 2000 characters.");

            var feature = new FeatureRecord
            {
                Id = Guid.NewGuid(), ProjectId = projectId, Name = name, Key = key,
                Description = description, CreatedAt = time.GetUtcNow(),
            };
            db.Features.Add(feature);
            await db.SaveChangesAsync(ct);
            return Results.CreatedAtRoute("GetFeature", new { projectId, featureId = feature.Id }, ToSummary(feature));
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_features_project_key",
        }) { return Results.Problem(statusCode: 409, title: "Feature key already exists"); }
        catch (Exception error) when (error is NpgsqlException or TimeoutException
            || error is DbUpdateException { InnerException: NpgsqlException }) { return Unavailable(); }
    }

    /// <summary>Enforces the paired identity and the project boundary before a feature operation.</summary>
    private static Task<bool> OwnsProject(DevPrismDbContext db, ClaimsPrincipal actor, Guid projectId, CancellationToken ct)
    {
        var owner = Guid.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return db.Projects.AnyAsync(row => row.Id == projectId && row.OwnerId == owner, ct);
    }

    /// <summary>Accepts ASCII lowercase slug segments with single hyphen separators.</summary>
    [GeneratedRegex(@"\A[a-z0-9]+(?:-[a-z0-9]+)*\z", RegexOptions.NonBacktracking)]
    private static partial Regex KeyPattern();

    /// <summary>Converts persistence data into the UI's feature metadata.</summary>
    private static FeatureSummary ToSummary(FeatureRecord row) => new(row.Id, row.Key, row.Name, row.Description, row.Version);

    /// <summary>Reports a field-specific constraint without exposing implementation details.</summary>
    private static IResult Invalid(string field, string message) => Results.ValidationProblem(
        new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>Checks local setup without retrieving or emitting a secret.</summary>
    private static bool DatabaseConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DevPrism"));

    /// <summary>Distinguishes database unavailability from empty results.</summary>
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "Local features are unavailable");

    /// <summary>Client-supplied editable metadata, never project ownership.</summary>
    internal sealed record FeatureRequest(string? Key, string? Name, string? Description);

    /// <summary>Local feature metadata and current optimistic revision.</summary>
    internal sealed record FeatureSummary(Guid Id, string Key, string Name, string Description, long Version);

    /// <summary>Bounded local search result with an explicit truncation indicator.</summary>
    internal sealed record FeatureList(FeatureSummary[] Items, bool Truncated);
}
