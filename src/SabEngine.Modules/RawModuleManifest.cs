namespace SabEngine.Modules;

/// <summary>
/// The internal, permissive shape used only during YAML deserialization
/// — every field nullable so YamlDotNet can populate whatever's actually
/// present, before <see cref="ModuleManifestParser"/> validates it
/// against Section 4.2's required-fields list and produces a real,
/// non-nullable <see cref="ModuleManifest"/>. Never exposed publicly.
/// </summary>
internal sealed class RawModuleManifest
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? RiskLevel { get; set; }
    public List<string>? Environments { get; set; }
    public string? ValidationStatus { get; set; }
    public Dictionary<string, RawParameterSpec>? Inputs { get; set; }
    public Dictionary<string, RawParameterSpec>? Outputs { get; set; }
    public RawRollbackSpec? Rollback { get; set; }
    public RawTestsSpec? Tests { get; set; }
}

internal sealed class RawParameterSpec
{
    public string? Type { get; set; }
    public bool Required { get; set; }
}

internal sealed class RawRollbackSpec
{
    public string? Procedure { get; set; }
    public bool Tested { get; set; }
}

internal sealed class RawTestsSpec
{
    public string? LabSuite { get; set; }
}
