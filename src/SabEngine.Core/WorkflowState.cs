namespace SabEngine.Core;

/// <summary>
/// The fixed set of states a <see cref="WorkflowRun"/> moves through.
/// See docs/SAB_Design_Document_v0.1.2.md, Section 4.1 ("State machine").
///
///   Requested -> PlanDrafted -> PendingApproval -> Approved | Declined
///     -> Executing -> Completed | Failed -> RolledBack (if Failed)
///
/// Persisted so a crashed worker can resume rather than lose track of
/// where it was (design doc, Section 4.1, AR-1).
/// </summary>
public enum WorkflowState
{
    Requested,
    PlanDrafted,
    PendingApproval,
    Approved,
    Declined,
    Executing,
    Completed,
    Failed,
    RolledBack,
}
