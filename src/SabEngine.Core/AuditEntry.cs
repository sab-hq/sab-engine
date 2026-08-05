namespace SabEngine.Core;

/// <summary>
/// An immutable audit log line for a single state transition. Written
/// once, never updated. See docs/SAB_Design_Document_v0.1.2.md, Section 7
/// ("Tamper-evidence, made concrete") — each entry's hash chains to the
/// previous entry's hash (append-only, hash-linked) so silent post-hoc
/// edits are detectable. <see cref="PreviousEntryHash"/> and
/// <see cref="Hash"/> are computed by the writer, not by this type.
/// </summary>
public sealed class AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid WorkflowRunId { get; init; }

    public required WorkflowState FromState { get; init; }

    public required WorkflowState ToState { get; init; }

    /// <summary>The specific human or system actor responsible for this transition.</summary>
    public required string Actor { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public string? PreviousEntryHash { get; init; }

    public required string Hash { get; init; }
}
