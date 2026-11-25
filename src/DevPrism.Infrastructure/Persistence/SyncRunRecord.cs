// Spec: Documentation/dev-prism-ai-specs/04_ARCHITECTURE.md (durable import state).
namespace DevPrism.Infrastructure.Persistence;

/// <summary>Durable states recorded independently of provider coverage or capability.</summary>
public enum SyncRunState { Queued, Running, Succeeded, Partial, Failed, Interrupted, Cancelled }

/// <summary>Distinguishes a selected read probe from a normal import.</summary>
public enum SyncOperation { Probe, Import }

/// <summary>Project-bound run history. It carries no provider credentials or raw response text.</summary>
public sealed class SyncRunRecord
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ConnectionId { get; init; }
    public long ConnectionVersion { get; init; }
    public SyncOperation Operation { get; init; }
    public SyncRunState State { get; set; } = SyncRunState.Queued;
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? ReasonCode { get; set; }
}
