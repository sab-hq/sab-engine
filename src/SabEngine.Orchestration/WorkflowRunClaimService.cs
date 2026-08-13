using Microsoft.EntityFrameworkCore;
using SabEngine.Core;
using SabEngine.Data;

namespace SabEngine.Orchestration;

/// <summary>
/// The claim/lease pattern from docs/design/SAB_Design_Document_v0.1.2.md,
/// Section 4.1 ("Concurrency model"): because the orchestration engine is
/// stateless (AR-1), scaling to multiple concurrent workers is a matter
/// of running more workers against the same PostgreSQL-backed state,
/// each claiming a run before acting on it, rather than adding
/// in-process threading complexity.
///
/// A worker calls <see cref="TryClaimNextAsync"/> in a loop; it either
/// gets back a run nobody else currently holds, or null if there's
/// nothing to do right now. If a worker crashes mid-processing, its
/// claim simply expires after <c>leaseDuration</c> and another worker
/// picks the run back up — nothing needs an explicit "worker died"
/// signal.
/// </summary>
public sealed class WorkflowRunClaimService(SabEngineDbContext db)
{
    /// <summary>
    /// States where a background worker — not a human, not an
    /// already-in-flight process — is the next thing that needs to act:
    /// draft/redraft a plan (Requested, Declined), execute an approved
    /// plan (Approved), or trigger a rollback (Failed). PlanDrafted,
    /// PendingApproval, Executing, Completed, and RolledBack are
    /// deliberately excluded — they're either mid-flight under a worker
    /// that already holds them, waiting on a human, or terminal.
    /// </summary>
    private static readonly WorkflowState[] ClaimableStates =
    [
        WorkflowState.Requested,
        WorkflowState.Declined,
        WorkflowState.Approved,
        WorkflowState.Failed,
    ];

    /// <summary>How many candidate runs to try before giving up and returning null (Section 4.1's concurrency model, kept simple for Phase 1).</summary>
    private const int MaxCandidatesToTry = 5;

    /// <summary>
    /// Attempts to claim the oldest eligible run. Returns null if nothing
    /// is currently claimable — that's a normal outcome, not an error;
    /// callers should treat it as "nothing to do right now" and poll
    /// again later.
    /// </summary>
    public async Task<WorkflowRun?> TryClaimNextAsync(string workerId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        var leaseExpiredBefore = DateTimeOffset.UtcNow - leaseDuration;

        var candidateIds = await db.WorkflowRuns
            .Where(w => ClaimableStates.Contains(w.State))
            .Where(w => w.ClaimedByWorkerId == null || w.ClaimedAt < leaseExpiredBefore)
            .OrderBy(w => w.CreatedAt)
            .Take(MaxCandidatesToTry)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        foreach (var candidateId in candidateIds)
        {
            // The atomic step: this UPDATE only succeeds if the row still
            // matches the same unclaimed-or-expired condition at write
            // time, not just at the SELECT above — that's what actually
            // prevents two workers from both winning the same run. If
            // another worker claimed it in between our SELECT and this
            // UPDATE, rowsAffected is 0 and we just move on to the next
            // candidate rather than treating it as an error.
            var rowsAffected = await db.WorkflowRuns
                .Where(w => w.Id == candidateId)
                .Where(w => w.ClaimedByWorkerId == null || w.ClaimedAt < leaseExpiredBefore)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(w => w.ClaimedByWorkerId, workerId)
                    .SetProperty(w => w.ClaimedAt, DateTimeOffset.UtcNow), cancellationToken);

            if (rowsAffected == 1)
            {
                return await db.WorkflowRuns.AsNoTracking().SingleAsync(w => w.Id == candidateId, cancellationToken);
            }
        }

        return null;
    }

    /// <summary>
    /// Releases a claim once a worker has handed a run off to a state it
    /// no longer owns (e.g. after Requested → PlanDrafted →
    /// PendingApproval, which is now waiting on a human, not this
    /// worker). Not strictly required for correctness — a run outside
    /// the claimable states is never selected as a candidate regardless
    /// — but keeps ClaimedByWorkerId from misleadingly showing a run as
    /// "still held" by a worker that's actually done with it.
    /// </summary>
    public async Task ReleaseClaimAsync(Guid workflowRunId, CancellationToken cancellationToken = default)
    {
        await db.WorkflowRuns
            .Where(w => w.Id == workflowRunId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.ClaimedByWorkerId, (string?)null)
                .SetProperty(w => w.ClaimedAt, (DateTimeOffset?)null), cancellationToken);
    }
}
