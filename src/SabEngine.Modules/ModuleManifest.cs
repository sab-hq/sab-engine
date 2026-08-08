using SabEngine.Core;

namespace SabEngine.Modules;

/// <summary>
/// A fully parsed, validated module manifest — matches the YAML schema
/// from docs/design/SAB_Design_Document_v0.1.2.md, Section 4.2 exactly.
/// Only ever produced by <see cref="ModuleManifestParser"/>, which
/// enforces every field Section 4.2 lists as required before this type
/// can exist at all — there's no way to construct a
/// <see cref="ModuleManifest"/> that's missing a rollback procedure or a
/// test suite, matching Section 2's non-negotiable rule.
/// </summary>
public sealed class ModuleManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required RiskLevel RiskLevel { get; init; }
    public required IReadOnlyList<string> Environments { get; init; }
    public required ModuleValidationStatus ValidationStatus { get; init; }
    public required IReadOnlyDictionary<string, ParameterSpec> Inputs { get; init; }
    public required IReadOnlyDictionary<string, ParameterSpec> Outputs { get; init; }
    public required RollbackSpec Rollback { get; init; }
    public required TestsSpec Tests { get; init; }
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
}

public sealed record ParameterSpec(string Type, bool Required);

public sealed record RollbackSpec(string Procedure, bool Tested);

public sealed record TestsSpec(string LabSuite);
