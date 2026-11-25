// Spec: Documentation/dev-prism-ai-specs/07_PROJECT_BRAIN.md (local decision lifecycle).
namespace DevPrism.Api.Features;

using System.Security.Claims;
using DevPrism.Api.Auth;
using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Records explicit operator decisions without contacting any source provider.</summary>
internal static class DecisionEndpoints
{
    /// <summary>Registers scoped listing, draft creation and exact-version approval.</summary>
    public static void MapDecisions(this RouteGroupBuilder api)
    {
        var routes = api.MapGroup("/projects/{projectId:guid}/features/{featureId:guid}/decisions").RequireAuthorization();
        routes.MapGet("", List).Produces<DecisionList>().Produces(404).ProducesProblem(503);
        routes.MapPost("", Draft).AddEndpointFilter(LocalAuthEndpoints.ValidateLocalMutation)
            .Produces<DecisionSummary>(201).ProducesValidationProblem().Produces(404).ProducesProblem(409);
        routes.MapPost("/{versionId:guid}/approve", Approve).AddEndpointFilter(LocalAuthEndpoints.ValidateLocalMutation)
            .Produces<DecisionSummary>().Produces(404).ProducesProblem(409);
    }

    /// <summary>Returns bounded local history including each current draft and approved rule.</summary>
    private static async Task<IResult> List(Guid projectId, Guid featureId, ClaimsPrincipal actor,
        DevPrismDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        if (!Configured(configuration)) return Unavailable();
        try
        {
            if (!await OwnsFeature(db, actor, projectId, featureId, ct)) return Results.NotFound();
            var rows = await db.Decisions.AsNoTracking().Where(row => row.ProjectId == projectId && row.FeatureId == featureId)
                .OrderBy(row => row.State == DecisionState.Superseded).ThenBy(row => row.Key)
                .ThenByDescending(row => row.Revision).Take(101).ToArrayAsync(ct);
            return Results.Ok(new DecisionList(rows.Take(100).Select(ToSummary).ToArray(), rows.Length > 100));
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException) { return Unavailable(); }
    }

    /// <summary>Appends a new draft, never edits the content of an earlier revision.</summary>
    private static async Task<IResult> Draft(Guid projectId, Guid featureId, DraftRequest input, ClaimsPrincipal actor,
        DevPrismDbContext db, IConfiguration configuration, TimeProvider time, CancellationToken ct)
    {
        if (!Configured(configuration)) return Unavailable();
        try
        {
            if (!await OwnsFeature(db, actor, projectId, featureId, ct)) return Results.NotFound();
            var key = input.Key?.Trim().ToUpperInvariant();
            var statement = input.Statement?.Trim();
            var rationale = input.Rationale?.Trim();
            if (key is not { Length: > 0 and <= 80 } || key.Split('-').Any(part => part.Length == 0 || !part.All(char.IsAsciiLetterOrDigit)))
                return Invalid("key", "Use ASCII letters, numbers and single hyphens.");
            if (statement is not { Length: > 0 and <= 2000 }) return Invalid("statement", "Enter a statement with 1–2000 characters.");
            if (rationale is not { Length: > 0 and <= 2000 }) return Invalid("rationale", "Enter a rationale with 1–2000 characters.");

            var latest = await db.Decisions.AsNoTracking().Where(row => row.ProjectId == projectId && row.Key == key)
                .OrderByDescending(row => row.Revision).FirstOrDefaultAsync(ct);
            if (latest is not null && (latest.FeatureId != featureId || latest.State == DecisionState.Draft))
                return Conflict();
            var draft = new DecisionVersionRecord
            {
                Id = Guid.NewGuid(), ProjectId = projectId, FeatureId = featureId, Key = key,
                Revision = (latest?.Revision ?? 0) + 1, Statement = statement, Rationale = rationale,
                AuthoredBy = ActorId(actor), CreatedAt = time.GetUtcNow(), State = DecisionState.Draft,
            };
            db.Decisions.Add(draft);
            await db.SaveChangesAsync(ct);
            return Results.Json(ToSummary(draft), statusCode: 201);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return Conflict(); }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException || ex is DbUpdateException { InnerException: NpgsqlException }) { return Unavailable(); }
    }

    /// <summary>Atomically supersedes the expected baseline and activates the reviewed draft.</summary>
    private static async Task<IResult> Approve(Guid projectId, Guid featureId, Guid versionId, ApproveRequest input,
        ClaimsPrincipal actor, DevPrismDbContext db, IConfiguration configuration, TimeProvider time, CancellationToken ct)
    {
        if (!Configured(configuration)) return Unavailable();
        try
        {
            if (!await OwnsFeature(db, actor, projectId, featureId, ct)) return Results.NotFound();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var revisions = db.Decisions.Where(row => row.ProjectId == projectId && row.FeatureId == featureId);
            var draft = await revisions.SingleOrDefaultAsync(row => row.Id == versionId, ct);
            if (draft is null) return Results.NotFound();
            if (draft.State != DecisionState.Draft) return Conflict();
            var currentId = await revisions.Where(row => row.Key == draft.Key && row.State == DecisionState.Approved)
                .Select(row => (Guid?)row.Id).SingleOrDefaultAsync(ct);
            if (currentId != input.ExpectedBaselineId) return Conflict();
            if (currentId is Guid baselineId)
            {
                // Release the unique approved slot before occupying it, within the same transaction.
                var updated = await revisions.Where(row => row.Id == baselineId && row.State == DecisionState.Approved)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.State, DecisionState.Superseded), ct);
                if (updated != 1) return Conflict();
            }
            draft.State = DecisionState.Approved;
            draft.ApprovedBy = ActorId(actor);
            draft.ApprovedAt = time.GetUtcNow();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(ToSummary(draft));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return Conflict(); }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException || ex is DbUpdateException { InnerException: NpgsqlException }) { return Unavailable(); }
    }

    /// <summary>Checks both the paired operator and the relationship between the supplied IDs.</summary>
    private static Task<bool> OwnsFeature(DevPrismDbContext db, ClaimsPrincipal actor, Guid projectId, Guid featureId, CancellationToken ct)
    {
        var owner = ActorId(actor);
        return db.Features.AnyAsync(feature => feature.Id == featureId && feature.ProjectId == projectId
            && db.Projects.Any(project => project.Id == feature.ProjectId && project.OwnerId == owner), ct);
    }

    /// <summary>Obtains the operator identifier from the authenticated session.</summary>
    private static Guid ActorId(ClaimsPrincipal actor) => Guid.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!);
    /// <summary>Checks whether local storage is configured without exposing its settings.</summary>
    private static bool Configured(IConfiguration config) => !string.IsNullOrWhiteSpace(config.GetConnectionString("DevPrism"));
    /// <summary>Distinguishes conflict from successful approval.</summary>
    private static IResult Conflict() => Results.Problem(statusCode: 409, title: "Decision changed or key already has a draft");
    /// <summary>Reports local unavailability rather than presenting an empty rule history.</summary>
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "Local decisions are unavailable");
    /// <summary>Reports the field to be corrected.</summary>
    private static IResult Invalid(string field, string reason) => Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [reason] });
    /// <summary>Returns only local operator evidence; never invents provider provenance.</summary>
    private static DecisionSummary ToSummary(DecisionVersionRecord row) => new(row.Id, row.Key, row.Revision,
        row.Statement, row.Rationale, row.State.ToString(), "OperatorStatement", row.ApprovedAt);

    /// <summary>Inputs for appending a local draft.</summary>
    internal sealed record DraftRequest(string? Key, string? Statement, string? Rationale);
    /// <summary>The baseline reviewed alongside the identified draft.</summary>
    internal sealed record ApproveRequest(Guid? ExpectedBaselineId);
    /// <summary>Versioned decision and provenance returned to the operator.</summary>
    internal sealed record DecisionSummary(Guid Id, string Key, int Revision, string Statement, string Rationale,
        string State, string Evidence, DateTimeOffset? ApprovedAt);
    /// <summary>Bounded local decision history.</summary>
    internal sealed record DecisionList(DecisionSummary[] Items, bool Truncated);
}
