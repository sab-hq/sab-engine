using SabEngine.Core;
using SabEngine.Modules;
using Xunit;

namespace SabEngine.Modules.Tests;

/// <summary>
/// Verifies ModuleManifestParser against the exact "apply-patch-windows"
/// example from docs/design/SAB_Design_Document_v0.1.2.md, Section 4.2 —
/// if this parser can't read the design doc's own reference example
/// correctly, nothing else about it matters.
/// </summary>
public class ModuleManifestParserTests
{
    // The literal YAML from Section 4.2, unchanged.
    private const string ValidManifestYaml = """
        id: apply-patch-windows
        name: Apply Windows Update patch
        version: 1.2.0
        risk_level: medium
        environments: [windows-server-2019, windows-server-2022]
        validation_status: production-approved
        inputs:
          patch_ids: { type: array<string>, required: true }
          maintenance_window: { type: string, required: false }
        outputs:
          result: { type: "enum[success, failure]" }
          applied_patch_ids: { type: array<string> }
        rollback:
          procedure: rollback-patch-windows.ps1
          tested: true
        tests:
          lab_suite: apply-patch-windows.tests.ps1
        """;

    private static readonly ModuleManifestParser Sut = new();

    [Fact]
    public void The_design_docs_own_reference_example_parses_correctly()
    {
        var manifest = Sut.Parse(ValidManifestYaml);

        Assert.Equal("apply-patch-windows", manifest.Id);
        Assert.Equal("Apply Windows Update patch", manifest.Name);
        Assert.Equal("1.2.0", manifest.Version);
        Assert.Equal(RiskLevel.Medium, manifest.RiskLevel);
        Assert.Equal(["windows-server-2019", "windows-server-2022"], manifest.Environments);
        Assert.Equal(ModuleValidationStatus.ProductionApproved, manifest.ValidationStatus);

        Assert.True(manifest.Inputs["patch_ids"].Required);
        Assert.Equal("array<string>", manifest.Inputs["patch_ids"].Type);
        Assert.False(manifest.Inputs["maintenance_window"].Required);

        Assert.Equal("rollback-patch-windows.ps1", manifest.Rollback.Procedure);
        Assert.True(manifest.Rollback.Tested);
        Assert.Equal("apply-patch-windows.tests.ps1", manifest.Tests.LabSuite);
    }

    [Fact]
    public void Lab_validated_status_is_recognized_too()
    {
        var yaml = ValidManifestYaml.Replace("validation_status: production-approved", "validation_status: lab-validated");

        var manifest = Sut.Parse(yaml);

        Assert.Equal(ModuleValidationStatus.LabValidated, manifest.ValidationStatus);
    }

    [Theory]
    [InlineData("id: apply-patch-windows\n")]
    [InlineData("name: Apply Windows Update patch\n")]
    public void Malformed_or_incomplete_yaml_is_rejected(string incompleteYaml)
    {
        Assert.Throws<ModuleManifestParseException>(() => Sut.Parse(incompleteYaml));
    }

    [Fact]
    public void A_manifest_missing_the_rollback_procedure_is_rejected()
    {
        // Section 2's non-negotiable rule, enforced at parse time.
        var yaml = ValidManifestYaml.Replace(
            "rollback:\n  procedure: rollback-patch-windows.ps1\n  tested: true",
            "rollback:\n  tested: true");

        var ex = Assert.Throws<ModuleManifestParseException>(() => Sut.Parse(yaml));
        Assert.Contains("rollback.procedure", ex.Message);
    }

    [Fact]
    public void A_manifest_missing_the_tests_section_is_rejected()
    {
        var yaml = ValidManifestYaml.Replace(
            "tests:\n  lab_suite: apply-patch-windows.tests.ps1",
            "");

        Assert.Throws<ModuleManifestParseException>(() => Sut.Parse(yaml));
    }

    [Fact]
    public void An_unrecognized_validation_status_is_rejected_with_a_clear_message()
    {
        var yaml = ValidManifestYaml.Replace("validation_status: production-approved", "validation_status: yolo-approved");

        var ex = Assert.Throws<ModuleManifestParseException>(() => Sut.Parse(yaml));
        Assert.Contains("yolo-approved", ex.Message);
    }

    [Fact]
    public void An_unrecognized_risk_level_is_rejected()
    {
        var yaml = ValidManifestYaml.Replace("risk_level: medium", "risk_level: catastrophic");

        Assert.Throws<ModuleManifestParseException>(() => Sut.Parse(yaml));
    }

    [Fact]
    public void Genuinely_broken_yaml_syntax_is_rejected_not_thrown_as_a_raw_YamlException()
    {
        var brokenYaml = "id: [this is not: closed properly";

        // Callers should only ever need to catch ModuleManifestParseException,
        // never a raw YamlDotNet exception type.
        Assert.Throws<ModuleManifestParseException>(() => Sut.Parse(brokenYaml));
    }

    [Fact]
    public void ToModuleCandidate_projects_the_fields_the_AI_agent_layer_actually_needs()
    {
        var manifest = Sut.Parse(ValidManifestYaml);

        var candidate = manifest.ToModuleCandidate();

        Assert.Equal(manifest.Id, candidate.Id);
        Assert.Equal(manifest.Name, candidate.Name);
        Assert.Equal(manifest.Version, candidate.Version);
        Assert.Equal(manifest.ValidationStatus, candidate.ValidationStatus);
        Assert.Equal(manifest.Rollback.Tested, candidate.HasTestedRollback);
    }
}
