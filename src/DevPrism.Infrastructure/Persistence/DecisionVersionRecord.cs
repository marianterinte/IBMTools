// Spec: Documentation/dev-prism-ai-specs/07_PROJECT_BRAIN.md (approved local memory).
namespace DevPrism.Infrastructure.Persistence;

/// <summary>One immutable statement and rationale with a locally authorized lifecycle.</summary>
public sealed class DecisionVersionRecord
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid FeatureId { get; init; }
    public required string Key { get; init; }
    public int Revision { get; init; }
    public required string Statement { get; init; }
    public required string Rationale { get; init; }
    public Guid AuthoredBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DecisionState State { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}

/// <summary>Only explicit operator approval changes the current local decision.</summary>
public enum DecisionState { Draft, Approved, Superseded }
