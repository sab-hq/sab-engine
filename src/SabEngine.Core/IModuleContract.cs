namespace SabEngine.Core;

/// <summary>
/// The metadata contract every module must satisfy, per
/// docs/SAB_Design_Document_v0.1.2.md, Section 4.2 ("The Module
/// Contract"). Actual module implementations (PowerShell/Bash scripts)
/// live in the OSML (`sab-hq/sab-modules`), not in this project — this
/// interface is what lets the orchestration engine work with any module
/// interchangeably, regardless of who wrote it or what it does.
/// </summary>
public interface IModuleContract
{
    /// <summary>
    /// Required, not optional (design doc, AR-5/AR-6, confirmed). No two
    /// modules, from any source, can share one.
    /// </summary>
    string Id { get; }

    string Name { get; }

    string Version { get; }

    /// <summary>
    /// "lab-validated" or "production-approved" — the field that lets the
    /// orchestration engine enforce Section 2's reliability principle in
    /// code. A module starts lab-validated and only becomes eligible for
    /// production WorkflowRuns once explicitly promoted.
    /// </summary>
    ModuleValidationStatus ValidationStatus { get; }

    /// <summary>
    /// Required, not optional, per Section 2. If a module can't be safely
    /// undone, it cannot be part of a workflow that touches production —
    /// enforced here, not left to a human to remember.
    /// </summary>
    bool HasTestedRollback { get; }
}

public enum ModuleValidationStatus
{
    LabValidated,
    ProductionApproved,
}
