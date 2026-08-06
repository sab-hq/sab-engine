namespace SabEngine.Orchestration;

/// <summary>
/// The "how and when" layer (design doc, Section 3/4.1). Takes an
/// approved <see cref="SabEngine.Core.Plan"/> and reliably carries it
/// out — sequencing modules, tracking state, triggering rollback
/// automatically on failure.
///
/// The state machine itself now lives in <see cref="WorkflowRunStateMachine"/>
/// (pre-development-checklist.md, PD-4, done). What's still a stub here:
/// actually calling modules in sequence during the Executing state (via
/// SabEngine.Execution's IExecutionConnector) and triggering rollback
/// automatically on module failure — that wiring isn't itemized as its
/// own PD- entry yet; add one when picking it up.
/// </summary>
public sealed class OrchestrationEngine
{
    // TODO: wire WorkflowRunStateMachine + IExecutionConnector together
    //   so entering the Executing state actually calls modules in order,
    //   and a module failure triggers TransitionAsync(..., Failed, ...)
    //   followed by the module's own rollback procedure.
}
