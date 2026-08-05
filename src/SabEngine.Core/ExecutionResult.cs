namespace SabEngine.Core;

/// <summary>
/// The outcome of a single module's execution within a workflow run.
/// See docs/SAB_Design_Document_v0.1.2.md, Section 4.1.
/// </summary>
public sealed class ExecutionResult
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid WorkflowRunId { get; init; }

    public required string ModuleId { get; init; }

    public required bool Succeeded { get; init; }

    /// <summary>
    /// True if this module's rollback procedure was invoked. Rollback is
    /// automatic — the orchestration engine triggers it the moment a
    /// module fails, per Section 2's non-negotiable rollback rule, never
    /// requiring a human to notice the failure first.
    /// </summary>
    public bool RollbackFired { get; init; }

    /// <summary>Raw output/details from the module, for the audit trail.</summary>
    public string? Output { get; init; }

    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }
}
