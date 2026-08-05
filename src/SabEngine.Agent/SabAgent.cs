namespace SabEngine.Agent;

/// <summary>
/// The "what and why" layer (design doc, Section 3/4.3). Reads a
/// workflow, target state, and history from the Engine State Store, and
/// produces a proposed <see cref="SabEngine.Core.Plan"/> — never
/// executes anything directly.
///
/// Implementation is pre-development-checklist.md PD-6 (Semantic Kernel
/// integration). Deliberately left as a stub here — this commit is
/// project scaffolding (PD-2), not the agent itself.
/// </summary>
public sealed class SabAgent
{
    // TODO(PD-6): integrate Microsoft Semantic Kernel; produce a Plan via
    //   structured function-calling (a typed object, not free text) per
    //   Section 4.3, "How the agent's reasoning is structured"
}
