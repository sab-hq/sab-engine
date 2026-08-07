using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SabEngine.Core;

namespace SabEngine.Agent;

/// <summary>
/// The "what and why" layer (design doc, Section 3/4.3). Reads a
/// workflow, target, and the list of modules actually available to
/// propose from, and produces a proposed <see cref="Plan"/> — it never
/// executes anything directly, and it never gets to skip the human
/// approval gate (see docs/learn/recommend-and-approve-mode.md).
///
/// Note on scope (PD-6): this takes <paramref name="availableModules"/>
/// as an input rather than loading a real catalog itself — there's no
/// module catalog loader yet, since no real modules exist (PD-14–PD-17
/// aren't done). Building that loader (reading manifests from the OSML)
/// is separate, future work.
/// </summary>
public sealed class SabAgent(Kernel kernel)
{
    private readonly IChatCompletionService _chat = kernel.GetRequiredService<IChatCompletionService>();

    /// <summary>
    /// Drafts a plan, then validates it against Section 4.1/4.2's hard
    /// rule before returning it — every referenced module must be a real
    /// candidate from <paramref name="availableModules"/>, with
    /// production-approved status and a tested rollback. A plan that
    /// fails this check is never returned; it throws
    /// <see cref="PlanValidationException"/> instead, the same way the
    /// orchestration engine itself refuses an unsafe plan (Section 4.1).
    /// </summary>
    public async Task<Plan> ProposePlanAsync(
        Guid workflowRunId,
        string workflowId,
        string target,
        IReadOnlyList<ModuleCandidate> availableModules,
        CancellationToken cancellationToken = default)
    {
        var history = BuildChatHistory(workflowId, target, availableModules);
        var response = await _chat.GetChatMessageContentsAsync(history, kernel: kernel, cancellationToken: cancellationToken);
        var responseText = response.Count > 0 ? response[0].Content ?? string.Empty : string.Empty;

        var parsed = ParseResponse(responseText);
        var plan = new Plan
        {
            WorkflowRunId = workflowRunId,
            Steps = parsed.Steps.Select(s => new ProposedModuleStep
            {
                ModuleId = s.ModuleId,
                ModuleVersion = s.ModuleVersion,
                Parameters = s.Parameters ?? new Dictionary<string, object?>(),
            }).ToList(),
            Reasoning = parsed.Reasoning,
            IsFlaggedUnusual = parsed.IsFlaggedUnusual,
        };

        ValidateAgainstHardRule(plan, availableModules);

        return plan;
    }

    /// <summary>
    /// Section 4.1/4.2's hard rule, enforced here too (defense in
    /// depth) — every proposed module must exist in the candidate list,
    /// be ProductionApproved, and have a tested rollback. Thrown, not
    /// silently filtered, because a model proposing an unsafe module is
    /// a real problem worth surfacing loudly, not quietly papering over.
    /// </summary>
    private static void ValidateAgainstHardRule(Plan plan, IReadOnlyList<ModuleCandidate> availableModules)
    {
        if (plan.Steps.Count == 0)
        {
            throw new PlanValidationException("The model returned a plan with zero steps.");
        }

        foreach (var step in plan.Steps)
        {
            var candidate = availableModules.FirstOrDefault(m => m.Id == step.ModuleId);

            if (candidate is null)
            {
                throw new PlanValidationException(
                    $"The model proposed module '{step.ModuleId}', which isn't in the list of available modules for this run.");
            }

            if (candidate.ValidationStatus != ModuleValidationStatus.ProductionApproved)
            {
                throw new PlanValidationException(
                    $"The model proposed module '{step.ModuleId}', which is not production-approved (status: {candidate.ValidationStatus}).");
            }

            if (!candidate.HasTestedRollback)
            {
                throw new PlanValidationException(
                    $"The model proposed module '{step.ModuleId}', which does not have a tested rollback procedure.");
            }
        }
    }

    private static ChatHistory BuildChatHistory(string workflowId, string target, IReadOnlyList<ModuleCandidate> availableModules)
    {
        var history = new ChatHistory();

        history.AddSystemMessage(
            """
            You are the AI agent layer of SAB (System Administration Builder). Your only job
            is to propose a plan — an ordered sequence of module invocations — for a given
            workflow and target. You never execute anything yourself; a human always approves
            or declines your proposal before anything runs.

            Only ever propose modules from the list of available modules you're given. Never
            invent a module ID.

            Respond with ONLY a single JSON object, no other text, matching this exact shape:
            {
              "steps": [
                { "moduleId": "string", "moduleVersion": "string", "parameters": { } }
              ],
              "reasoning": "plain-language explanation of why this plan makes sense",
              "isFlaggedUnusual": false
            }
            """);

        var modulesDescription = new StringBuilder();
        foreach (var module in availableModules)
        {
            modulesDescription.AppendLine(
                $"- id: {module.Id}, version: {module.Version}, name: {module.Name}, validationStatus: {module.ValidationStatus}, hasTestedRollback: {module.HasTestedRollback}");
        }

        history.AddUserMessage(
            $"""
            Workflow: {workflowId}
            Target: {target}

            Available modules:
            {modulesDescription}

            Propose a plan.
            """);

        return history;
    }

    private static AgentPlanResponse ParseResponse(string responseText)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<AgentPlanResponse>(responseText, JsonOptions);
            return parsed ?? throw new PlanValidationException("The model's response deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new PlanValidationException($"The model's response wasn't valid JSON matching the expected plan shape: {ex.Message}");
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>The raw shape the model is asked to return — mapped into the real <see cref="Plan"/> type after parsing.</summary>
    private sealed class AgentPlanResponse
    {
        [JsonPropertyName("steps")]
        public List<AgentPlanStep> Steps { get; set; } = [];

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        [JsonPropertyName("isFlaggedUnusual")]
        public bool IsFlaggedUnusual { get; set; }
    }

    private sealed class AgentPlanStep
    {
        [JsonPropertyName("moduleId")]
        public string ModuleId { get; set; } = string.Empty;

        [JsonPropertyName("moduleVersion")]
        public string ModuleVersion { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object?>? Parameters { get; set; }
    }
}
