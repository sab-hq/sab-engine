namespace SabEngine.Core;

/// <summary>
/// The AI agent's proposed sequence of module invocations for a given
/// <see cref="WorkflowRun"/>, plus its plain-language reasoning.
/// See docs/SAB_Design_Document_v0.1.2.md, Section 4.1 and 4.3.
///
/// This is a structured object, not free text (Section 4.3, "How the
/// agent's reasoning is structured") — that's what lets the orchestration
/// engine validate it deterministically before a human ever sees it: does
/// every proposed module have a tested rollback and `production-approved`
/// status? (Section 4.1/4.2's hard rule.)
/// </summary>
public sealed class Plan
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid WorkflowRunId { get; init; }

    /// <summary>
    /// The proposed module invocations, in order. Each entry references a
    /// module by its unique ID (design doc, AR-5) plus the specific
    /// parameters proposed for this run.
    /// </summary>
    public required IReadOnlyList<ProposedModuleStep> Steps { get; init; }

    /// <summary>Plain-language explanation of why this plan makes sense right now.</summary>
    public required string Reasoning { get; init; }

    /// <summary>
    /// True if this run was flagged as unusual compared to past runs
    /// (design doc, Section 4.3, "What 'unusual' means") — surfaced to
    /// the human approver as an extra signal, not a block.
    /// </summary>
    public bool IsFlaggedUnusual { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>One step within a <see cref="Plan"/> — a single proposed module invocation.</summary>
public sealed class ProposedModuleStep
{
    /// <summary>The module's unique ID, e.g. "apply-patch-windows".</summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// The exact module version this run pins to (design doc, Section
    /// 4.2, "Versioning/compatibility") — a later module update must not
    /// silently change behavior for a run already in progress.
    /// </summary>
    public required string ModuleVersion { get; init; }

    public required IReadOnlyDictionary<string, object?> Parameters { get; init; }
}
