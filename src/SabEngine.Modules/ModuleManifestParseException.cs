namespace SabEngine.Modules;

/// <summary>
/// Thrown when a module manifest is malformed YAML, or well-formed YAML
/// that's missing a field docs/design/SAB_Design_Document_v0.1.2.md,
/// Section 4.2 requires (unique ID, rollback procedure, tests, etc.).
/// Deliberately fails loudly at parse time rather than producing a
/// partially-populated manifest that fails later in a more confusing
/// place — same philosophy as PlanValidationException in SabEngine.Agent.
/// </summary>
public sealed class ModuleManifestParseException(string message) : InvalidOperationException(message);
