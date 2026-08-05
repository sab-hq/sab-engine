namespace SabEngine.Core;

/// <summary>
/// One row per workflow execution. The orchestration engine is a
/// stateless task runner (design doc, Section 4.1, AR-1) — every field
/// here has to be durable in the Engine State Store (ESS), not held only
/// in memory, so a crashed worker can resume from wherever this row says
/// it left off.
/// </summary>
public sealed class WorkflowRun
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The workflow's own unique ID (design doc, AR-5/AR-6, confirmed —
    /// every workflow has one). Not this run's ID — the ID of the
    /// *recipe* being followed, e.g. "patch-windows-server".
    /// </summary>
    public required string WorkflowId { get; init; }

    /// <summary>The target system this run is/was executing against.</summary>
    public required string Target { get; init; }

    public WorkflowState State { get; set; } = WorkflowState.Requested;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Set when a worker claims this run for execution — part of the
    /// claim/lease concurrency pattern (design doc, Section 4.1,
    /// "Concurrency model"; pre-development-checklist.md PD-5).
    /// </summary>
    public string? ClaimedByWorkerId { get; set; }

    public DateTimeOffset? ClaimedAt { get; set; }
}
