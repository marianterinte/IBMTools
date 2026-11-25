// Spec: Documentation/dev-prism-ai-specs/05_DATA_MODEL.md (project scope).
namespace DevPrism.Infrastructure.Persistence;

/// <summary>A local workspace owned by the authenticated operator.</summary>
public sealed class ProjectRecord
{
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public required string Slug { get; init; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public long Version { get; set; } = 1;
}
