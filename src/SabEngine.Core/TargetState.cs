namespace SabEngine.Core;

/// <summary>
/// Current known facts about one managed system, updated as workflow
/// runs complete. See docs/SAB_Design_Document_v0.1.2.md, Section 4.5
/// ("Data model, first draft") — this is what lets the AI agent avoid
/// guessing about a target's current state before proposing a plan.
/// </summary>
public sealed class TargetState
{
    /// <summary>The target identifier this state describes (matches WorkflowRun.Target).</summary>
    public required string Target { get; init; }

    /// <summary>Free-form facts as of the last run — patch level, last run timestamp, etc.</summary>
    public required IReadOnlyDictionary<string, object?> Facts { get; init; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
