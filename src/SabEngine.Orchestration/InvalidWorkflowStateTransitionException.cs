namespace SabEngine.Orchestration;

/// <summary>
/// Thrown when code attempts a state transition that isn't in the
/// allowed map (see <see cref="WorkflowRunStateMachine.AllowedTransitions"/>).
/// This is the state-machine's own enforcement of Section 4.1's design —
/// an illegal transition is a bug to catch immediately, not something to
/// silently allow.
/// </summary>
public sealed class InvalidWorkflowStateTransitionException(SabEngine.Core.WorkflowState from, SabEngine.Core.WorkflowState to)
    : InvalidOperationException($"Cannot transition a WorkflowRun from {from} to {to} — not an allowed transition.")
{
    public SabEngine.Core.WorkflowState From { get; } = from;
    public SabEngine.Core.WorkflowState To { get; } = to;
}
