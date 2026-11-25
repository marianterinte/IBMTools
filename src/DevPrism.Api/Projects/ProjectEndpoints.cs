// Spec: Documentation/dev-prism-ai-specs/11_API_AND_EVENTS.md (project boundaries).
namespace DevPrism.Api.Projects;

using System.Text.RegularExpressions;
using DevPrism.Api.Local;
using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Exposes only workspaces that belong to the local workspace.</summary>
internal static partial class ProjectEndpoints
{
    /// <summary>Registers the local project routes.</summary>
    public static void MapProjects(this RouteGroupBuilder api)
    {
        var projects = api.MapGroup("/projects");
        projects.MapGet("", List).Produces<ProjectList>().ProducesProblem(503);
        projects.MapGet("/{projectId:guid}", Get).WithName("GetProject")
            .Produces<ProjectSummary>().Produces(404).ProducesProblem(503);
        projects.MapPost("", Create)
            .Produces<ProjectSummary>(201).ProducesValidationProblem().ProducesProblem(403)
            .ProducesProblem(409).ProducesProblem(503);
    }

    /// <summary>Returns a bounded list of the local operator's projects.</summary>
    private static async Task<IResult> List(
        DevPrismDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        if (!DatabaseConfigured(configuration)) return DatabaseUnavailable();
        var owner = LocalWorkspace.OperatorId;
        var items = await db.Projects.AsNoTracking().Where(row => row.OwnerId == owner)
            .OrderBy(row => row.Slug).Take(100)
            .Select(row => new ProjectSummary(row.Id, row.Slug, row.Name, row.Version)).ToArrayAsync(ct);
        return Results.Ok(new ProjectList(items));
    }

    /// <summary>Returns a project without disclosing a project owned by another identity.</summary>
    private static async Task<IResult> Get(
        Guid projectId, DevPrismDbContext db, IConfiguration configuration, CancellationToken ct)
    {
        if (!DatabaseConfigured(configuration)) return DatabaseUnavailable();
        var owner = LocalWorkspace.OperatorId;
        var project = await db.Projects.AsNoTracking().Where(row => row.Id == projectId && row.OwnerId == owner)
            .Select(row => new ProjectSummary(row.Id, row.Slug, row.Name, row.Version)).SingleOrDefaultAsync(ct);
        return project is null ? Results.NotFound() : Results.Ok(project);
    }

    /// <summary>Creates a server-scoped project with validated identity and a unique operator slug.</summary>
    private static async Task<IResult> Create(
        ProjectRequest request, DevPrismDbContext db, IConfiguration configuration,
        TimeProvider clock, CancellationToken ct)
    {
        if (!DatabaseConfigured(configuration)) return DatabaseUnavailable();
        var name = request.Name?.Trim();
        if (name is not { Length: > 0 and <= 160 })
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Enter a name with 1–160 characters."] });
        var slug = request.Slug?.Trim().ToLowerInvariant();
        if (slug is not { Length: > 0 and <= 80 } || !SlugPattern().IsMatch(slug))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["slug"] = ["Use up to 80 letters, numbers and single hyphens."] });

        var row = new ProjectRecord
        {
            Id = Guid.NewGuid(), OwnerId = LocalWorkspace.OperatorId, Name = name, Slug = slug, CreatedAt = clock.GetUtcNow(),
        };
        db.Projects.Add(row);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_projects_owner_id_slug",
        })
        {
            return Results.Problem(statusCode: 409, title: "Project slug already exists");
        }

        return Results.CreatedAtRoute("GetProject", new { projectId = row.Id },
            new ProjectSummary(row.Id, row.Slug, row.Name, row.Version));
    }

    /// <summary>Prevents an unconfigured workspace from looking like an empty database.</summary>
    private static bool DatabaseConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DevPrism"));

    /// <summary>Reports missing local setup without printing connection configuration.</summary>
    private static IResult DatabaseUnavailable() => Results.Problem(statusCode: 503, title: "Local database is not configured");

    /// <summary>Accepts ASCII slug segments separated by a single hyphen.</summary>
    [GeneratedRegex(@"\A[a-z0-9]+(?:-[a-z0-9]+)*\z", RegexOptions.NonBacktracking)]
    private static partial Regex SlugPattern();

    /// <summary>Client-supplied fields for creating a project.</summary>
    internal sealed record ProjectRequest(string? Name, string? Slug);

    /// <summary>Project metadata returned without its owner or any source credentials.</summary>
    internal sealed record ProjectSummary(Guid Id, string Slug, string Name, long Version);

    /// <summary>Bounded workspace list for a single operator.</summary>
    internal sealed record ProjectList(ProjectSummary[] Items);
}
