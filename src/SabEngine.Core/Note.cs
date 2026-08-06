namespace SabEngine.Core;

/// <summary>
/// A free-text human annotation, linked to a target or a specific run.
/// See docs/SAB_Design_Document_v0.1.2.md, Section 4.5 — kept as its own
/// table, separate from the structured tables, so a human's "watch out,
/// this one's flaky" comment doesn't have to force a schema change.
/// </summary>
public sealed class Note
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The target this note is about. Always set.</summary>
    public required string Target { get; init; }

    /// <summary>Optionally links this note to a specific run, if it's about one in particular.</summary>
    public Guid? WorkflowRunId { get; init; }

    public required string Text { get; init; }

    public required string AuthorUserId { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
