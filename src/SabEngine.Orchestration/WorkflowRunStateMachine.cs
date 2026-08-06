using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SabEngine.Core;
using SabEngine.Data;

namespace SabEngine.Orchestration;

/// <summary>
/// Drives a WorkflowRun through the state machine from
/// docs/SAB_Design_Document_v0.1.2.md, Section 4.1:
///
///   Requested -> PlanDrafted -> PendingApproval -> Approved | Declined
///     -> Executing -> Completed | Failed -> RolledBack (if Failed)
///
/// (Declined -> PlanDrafted is also allowed — Section 4.3: "Decline it,
/// and nothing runs — the agent can revise and try again", on the same
/// run rather than starting a new one.)
///
/// Every transition writes an immutable, hash-linked AuditEntry (Section
/// 7, "Tamper-evidence, made concrete") — this is not optional bolt-on
/// logging, it's how every transition happens; there is no code path
/// that changes WorkflowRun.State without also writing the audit record
/// for it in the same operation.
/// </summary>
public sealed class WorkflowRunStateMachine(SabEngineDbContext db)
{
    /// <summary>
    /// The only transitions this state machine will ever allow. Enforced
    /// in code, not left to callers to get right — this is Section 4.1's
    /// state machine made real, matching the module/rollback hard rule's
    /// spirit: the rule lives here, not in documentation someone has to
    /// remember.
    /// </summary>
    private static readonly IReadOnlyDictionary<WorkflowState, WorkflowState[]> AllowedTransitions = new Dictionary<WorkflowState, WorkflowState[]>
    {
        [WorkflowState.Requested] = [WorkflowState.PlanDrafted],
        [WorkflowState.PlanDrafted] = [WorkflowState.PendingApproval],
        [WorkflowState.PendingApproval] = [WorkflowState.Approved, WorkflowState.Declined],
        [WorkflowState.Declined] = [WorkflowState.PlanDrafted],
        [WorkflowState.Approved] = [WorkflowState.Executing],
        [WorkflowState.Executing] = [WorkflowState.Completed, WorkflowState.Failed],
        [WorkflowState.Failed] = [WorkflowState.RolledBack],
        // Completed and RolledBack are terminal — no further transitions.
        [WorkflowState.Completed] = [],
        [WorkflowState.RolledBack] = [],
    };

    /// <summary>Creates a new WorkflowRun in the Requested state (design doc, Section 3, step 1).</summary>
    public async Task<WorkflowRun> RequestAsync(string workflowId, string target, string actor, CancellationToken cancellationToken = default)
    {
        var run = new WorkflowRun { WorkflowId = workflowId, Target = target };
        db.WorkflowRuns.Add(run);

        await WriteAuditEntryAsync(run.Id, from: run.State, to: run.State, actor, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return run;
    }

    /// <summary>Advances a run to a new state, enforcing the allowed-transitions map and writing the audit trail.</summary>
    public async Task TransitionAsync(Guid workflowRunId, WorkflowState to, string actor, CancellationToken cancellationToken = default)
    {
        var run = await db.WorkflowRuns.FindAsync([workflowRunId], cancellationToken)
            ?? throw new InvalidOperationException($"WorkflowRun {workflowRunId} not found.");

        var from = run.State;

        if (!AllowedTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
        {
            throw new InvalidWorkflowStateTransitionException(from, to);
        }

        run.State = to;
        run.UpdatedAt = DateTimeOffset.UtcNow;

        await WriteAuditEntryAsync(workflowRunId, from, to, actor, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Writes one immutable AuditEntry, chained to the previous entry's
    /// hash for this run (Section 7's append-only, hash-linked design).
    /// The very first entry for a run has PreviousEntryHash = null.
    /// </summary>
    private async Task WriteAuditEntryAsync(Guid workflowRunId, WorkflowState from, WorkflowState to, string actor, CancellationToken cancellationToken)
    {
        var previous = await db.AuditEntries
            .Where(e => e.WorkflowRunId == workflowRunId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var timestamp = DateTimeOffset.UtcNow;
        var hash = ComputeHash(workflowRunId, from, to, actor, timestamp, previous?.Hash);

        db.AuditEntries.Add(new AuditEntry
        {
            WorkflowRunId = workflowRunId,
            FromState = from,
            ToState = to,
            Actor = actor,
            Timestamp = timestamp,
            PreviousEntryHash = previous?.Hash,
            Hash = hash,
        });
    }

    private static string ComputeHash(Guid workflowRunId, WorkflowState from, WorkflowState to, string actor, DateTimeOffset timestamp, string? previousHash)
    {
        var payload = $"{previousHash}|{workflowRunId}|{from}|{to}|{actor}|{timestamp:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
