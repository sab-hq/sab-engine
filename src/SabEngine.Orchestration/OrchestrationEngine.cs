namespace SabEngine.Orchestration;

/// <summary>
/// The "how and when" layer (design doc, Section 3/4.1). Takes an
/// approved <see cref="SabEngine.Core.Plan"/> and reliably carries it
/// out — sequencing modules, tracking state, triggering rollback
/// automatically on failure.
///
/// Implementation is pre-development-checklist.md PD-4 (state machine)
/// and PD-5 (concurrency model). Deliberately left as a stub here — this
/// commit is project scaffolding (PD-2), not the engine itself.
/// </summary>
public sealed class OrchestrationEngine
{
    // TODO(PD-4): implement the state machine transitions
    //   Requested -> PlanDrafted -> PendingApproval -> Approved/Declined
    //   -> Executing -> Completed/Failed -> RolledBack
    // TODO(PD-5): implement the claim/lease concurrency pattern for
    //   stateless workers running against shared PostgreSQL state
}
