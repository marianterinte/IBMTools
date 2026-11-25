// Spec: Documentation/dev-prism-ai-specs/05_DATA_MODEL.md (feature identity).
namespace DevPrism.Infrastructure.Persistence;

/// <summary>A stable, locally authored feature in an operator-owned project.</summary>
public sealed class FeatureRecord
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public long Version { get; set; } = 1;
}
