// Spec: Documentation/dev-prism-ai-specs/05_DATA_MODEL.md (connection identity).
namespace DevPrism.Infrastructure.Persistence;

/// <summary>A project-bound provider scope; never contains a provider credential or raw endpoint.</summary>
public sealed class ConnectionRecord
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public required string Provider { get; init; }
    public required string ScopeKey { get; init; }
    public string? SecretReference { get; set; }
    public bool Enabled { get; set; } = true;
    public long Version { get; set; } = 1;
}
