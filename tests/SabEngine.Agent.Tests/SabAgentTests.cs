using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SabEngine.Core;
using Xunit;

namespace SabEngine.Agent.Tests;

/// <summary>
/// Verifies SabAgent's own logic — request building, response parsing,
/// and the Section 4.1/4.2 hard-rule validation — using a fake
/// IChatCompletionService (see FakeChatCompletionService.cs) rather than
/// a real model. This deliberately does NOT test whether a real LLM
/// produces a good plan; that's a different, later concern (needs an
/// actual API key, which doesn't belong in this repo). What's tested
/// here is: does SabAgent correctly parse a well-formed response, and
/// does it correctly refuse an unsafe one?
/// </summary>
public class SabAgentTests
{
    private static readonly ModuleCandidate PreFlightCheck = new(
        "pre-flight-check", "Pre-flight check", "1.0.0", ModuleValidationStatus.ProductionApproved, HasTestedRollback: true);

    private static readonly ModuleCandidate ApplyPatches = new(
        "apply-patches", "Apply patches", "1.0.0", ModuleValidationStatus.ProductionApproved, HasTestedRollback: true);

    private static readonly ModuleCandidate LabOnlyModule = new(
        "experimental-module", "Experimental module", "0.1.0", ModuleValidationStatus.LabValidated, HasTestedRollback: true);

    private static readonly ModuleCandidate NoRollbackModule = new(
        "risky-module", "Risky module", "1.0.0", ModuleValidationStatus.ProductionApproved, HasTestedRollback: false);

    private static SabAgent CreateAgent(string cannedResponse)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(new FakeChatCompletionService(cannedResponse));
        var kernel = builder.Build();
        return new SabAgent(kernel);
    }

    [Fact]
    public async Task A_well_formed_response_produces_a_matching_plan()
    {
        var response = """
            {
              "steps": [
                { "moduleId": "pre-flight-check", "moduleVersion": "1.0.0", "parameters": {} },
                { "moduleId": "apply-patches", "moduleVersion": "1.0.0", "parameters": { "patchIds": ["KB123"] } }
              ],
              "reasoning": "Server looks healthy, safe to patch.",
              "isFlaggedUnusual": false
            }
            """;

        var agent = CreateAgent(response);
        var plan = await agent.ProposePlanAsync(
            Guid.NewGuid(), "patch-windows-server", "srv-01", [PreFlightCheck, ApplyPatches]);

        Assert.Equal(2, plan.Steps.Count);
        Assert.Equal("pre-flight-check", plan.Steps[0].ModuleId);
        Assert.Equal("apply-patches", plan.Steps[1].ModuleId);
        Assert.Equal("Server looks healthy, safe to patch.", plan.Reasoning);
        Assert.False(plan.IsFlaggedUnusual);
    }

    [Fact]
    public async Task A_plan_referencing_an_unknown_module_is_rejected()
    {
        var response = """
            {
              "steps": [
                { "moduleId": "this-module-does-not-exist", "moduleVersion": "1.0.0", "parameters": {} }
              ],
              "reasoning": "test",
              "isFlaggedUnusual": false
            }
            """;

        var agent = CreateAgent(response);

        await Assert.ThrowsAsync<PlanValidationException>(() =>
            agent.ProposePlanAsync(Guid.NewGuid(), "patch-windows-server", "srv-01", [PreFlightCheck]));
    }

    [Fact]
    public async Task A_plan_referencing_a_lab_validated_module_is_rejected()
    {
        var response = """
            {
              "steps": [
                { "moduleId": "experimental-module", "moduleVersion": "0.1.0", "parameters": {} }
              ],
              "reasoning": "test",
              "isFlaggedUnusual": false
            }
            """;

        var agent = CreateAgent(response);

        // Section 4.1/4.2's hard rule: a lab-validated module is not
        // eligible for a real plan, no matter what the model proposes.
        await Assert.ThrowsAsync<PlanValidationException>(() =>
            agent.ProposePlanAsync(Guid.NewGuid(), "patch-windows-server", "srv-01", [LabOnlyModule]));
    }

    [Fact]
    public async Task A_plan_referencing_a_module_without_tested_rollback_is_rejected()
    {
        var response = """
            {
              "steps": [
                { "moduleId": "risky-module", "moduleVersion": "1.0.0", "parameters": {} }
              ],
              "reasoning": "test",
              "isFlaggedUnusual": false
            }
            """;

        var agent = CreateAgent(response);

        await Assert.ThrowsAsync<PlanValidationException>(() =>
            agent.ProposePlanAsync(Guid.NewGuid(), "patch-windows-server", "srv-01", [NoRollbackModule]));
    }

    [Fact]
    public async Task A_plan_with_zero_steps_is_rejected()
    {
        var response = """
            {
              "steps": [],
              "reasoning": "Nothing needs to happen.",
              "isFlaggedUnusual": false
            }
            """;

        var agent = CreateAgent(response);

        await Assert.ThrowsAsync<PlanValidationException>(() =>
            agent.ProposePlanAsync(Guid.NewGuid(), "patch-windows-server", "srv-01", [PreFlightCheck]));
    }

    [Fact]
    public async Task A_malformed_response_is_rejected_with_a_clear_error()
    {
        var agent = CreateAgent("this is not JSON at all");

        await Assert.ThrowsAsync<PlanValidationException>(() =>
            agent.ProposePlanAsync(Guid.NewGuid(), "patch-windows-server", "srv-01", [PreFlightCheck]));
    }

    [Fact]
    public async Task The_plan_is_linked_to_the_correct_WorkflowRunId()
    {
        var response = """
            {
              "steps": [{ "moduleId": "pre-flight-check", "moduleVersion": "1.0.0", "parameters": {} }],
              "reasoning": "test",
              "isFlaggedUnusual": false
            }
            """;

        var runId = Guid.NewGuid();
        var agent = CreateAgent(response);
        var plan = await agent.ProposePlanAsync(runId, "patch-windows-server", "srv-01", [PreFlightCheck]);

        Assert.Equal(runId, plan.WorkflowRunId);
    }
}
