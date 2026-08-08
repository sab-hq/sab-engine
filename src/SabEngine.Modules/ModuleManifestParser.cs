using SabEngine.Core;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SabEngine.Modules;

public interface IModuleManifestParser
{
    ModuleManifest Parse(string yaml);
}

/// <summary>
/// Parses and validates a module manifest against the schema from
/// docs/design/SAB_Design_Document_v0.1.2.md, Section 4.2. See
/// docs/learn/modules.md for the plain-language version of what a
/// module contract actually is.
///
/// <c>validation_status</c> uses hyphenated values in YAML
/// ("lab-validated", "production-approved") that don't map cleanly onto
/// a C# enum via a naming convention alone, so it's deserialized as a
/// plain string and converted explicitly here instead.
/// </summary>
public sealed class ModuleManifestParser : IModuleManifestParser
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public ModuleManifest Parse(string yaml)
    {
        RawModuleManifest raw;
        try
        {
            raw = _deserializer.Deserialize<RawModuleManifest>(yaml)
                ?? throw new ModuleManifestParseException("The manifest YAML deserialized to nothing (empty document?).");
        }
        catch (YamlException ex)
        {
            throw new ModuleManifestParseException($"The manifest isn't valid YAML: {ex.Message}");
        }

        // Section 4.2's "At minimum, each module needs" list, enforced
        // here — a manifest missing any of these is rejected outright at
        // parse time, not silently accepted and discovered broken later.
        RequireNonEmpty(raw.Id, "id");
        RequireNonEmpty(raw.Name, "name");
        RequireNonEmpty(raw.Version, "version");
        RequireNonEmpty(raw.RiskLevel, "risk_level");
        RequireNonEmpty(raw.ValidationStatus, "validation_status");

        if (raw.Environments is null || raw.Environments.Count == 0)
        {
            throw new ModuleManifestParseException("The manifest is missing 'environments', or it's empty.");
        }

        if (raw.Inputs is null)
        {
            throw new ModuleManifestParseException("The manifest is missing 'inputs' (use an empty mapping '{}' if the module genuinely takes none).");
        }

        if (raw.Outputs is null)
        {
            throw new ModuleManifestParseException("The manifest is missing 'outputs' (use an empty mapping '{}' if the module genuinely produces none).");
        }

        if (raw.Rollback is null || string.IsNullOrWhiteSpace(raw.Rollback.Procedure))
        {
            // Section 2's non-negotiable rule: a rollback procedure is
            // required, not optional. Rejected here, at parse time — not
            // discovered later when the orchestration engine or the AI
            // agent's own hard-rule check happens to catch it.
            throw new ModuleManifestParseException("The manifest is missing 'rollback.procedure' — every module must declare a rollback procedure (design doc, Section 2).");
        }

        if (raw.Tests is null || string.IsNullOrWhiteSpace(raw.Tests.LabSuite))
        {
            throw new ModuleManifestParseException("The manifest is missing 'tests.lab_suite'.");
        }

        return new ModuleManifest
        {
            Id = raw.Id!,
            Name = raw.Name!,
            Version = raw.Version!,
            RiskLevel = ParseRiskLevel(raw.RiskLevel!),
            Environments = raw.Environments,
            ValidationStatus = ParseValidationStatus(raw.ValidationStatus!),
            Inputs = ToParameterSpecs(raw.Inputs),
            Outputs = ToParameterSpecs(raw.Outputs),
            Rollback = new RollbackSpec(raw.Rollback.Procedure!, raw.Rollback.Tested),
            Tests = new TestsSpec(raw.Tests.LabSuite!),
        };
    }

    private static void RequireNonEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ModuleManifestParseException($"The manifest is missing a required field: '{fieldName}'.");
        }
    }

    private static IReadOnlyDictionary<string, ParameterSpec> ToParameterSpecs(Dictionary<string, RawParameterSpec> raw) =>
        raw.ToDictionary(kv => kv.Key, kv => new ParameterSpec(kv.Value.Type ?? string.Empty, kv.Value.Required));

    private static RiskLevel ParseRiskLevel(string value) => value.Trim().ToLowerInvariant() switch
    {
        "low" => RiskLevel.Low,
        "medium" => RiskLevel.Medium,
        "high" => RiskLevel.High,
        _ => throw new ModuleManifestParseException($"Unrecognized risk_level '{value}' — expected 'low', 'medium', or 'high'."),
    };

    private static ModuleValidationStatus ParseValidationStatus(string value) => value.Trim().ToLowerInvariant() switch
    {
        "lab-validated" => ModuleValidationStatus.LabValidated,
        "production-approved" => ModuleValidationStatus.ProductionApproved,
        _ => throw new ModuleManifestParseException($"Unrecognized validation_status '{value}' — expected 'lab-validated' or 'production-approved'."),
    };
}
