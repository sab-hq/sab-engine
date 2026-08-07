namespace SabEngine.Agent;

/// <summary>
/// Thrown when a plan the model returned violates the hard rule from
/// docs/design/SAB_Design_Document_v0.1.2.md, Section 4.1/4.2: every
/// proposed module must be a real, known module with tested rollback and
/// production-approved status. This is the AI agent layer's own
/// enforcement of that rule — the same kind of validation the
/// orchestration engine itself does before a human ever sees a plan
/// (Section 4.1's hard rule), just applied one step earlier, right where
/// the model's output first comes back.
///
/// This is deliberate defense in depth, not redundant: the agent should
/// never even *propose* something the engine would reject anyway — a
/// human approver seeing "the agent tried to propose an unapproved
/// module and was refused" is a worse experience than the agent simply
/// never proposing it.
/// </summary>
public sealed class PlanValidationException(string message) : InvalidOperationException(message);
