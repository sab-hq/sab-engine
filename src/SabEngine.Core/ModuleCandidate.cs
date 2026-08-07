namespace SabEngine.Core;

/// <summary>
/// A concrete, constructible implementation of <see cref="IModuleContract"/>
/// for use wherever code needs to pass around "here's what I know about
/// an available module" — the AI agent layer (Section 4.3) taking a
/// candidate list to propose from, tests, and eventually a real module
/// catalog loader reading manifests from the OSML (`sab-modules`).
/// </summary>
public sealed record ModuleCandidate(
    string Id,
    string Name,
    string Version,
    ModuleValidationStatus ValidationStatus,
    bool HasTestedRollback) : IModuleContract;
