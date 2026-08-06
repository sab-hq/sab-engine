using Microsoft.EntityFrameworkCore;
using SabEngine.Core;
using SabEngine.Data;
using Xunit;

namespace SabEngine.Orchestration.Tests;

/// <summary>
/// Verifies the state machine matches docs/SAB_Design_Document_v0.1.2.md,
/// Section 4.1 exactly — both the allowed transitions and the
/// hash-linked audit trail from Section 7. Uses EF Core's InMemory
/// provider, not real Postgres — this is testing the state machine's own
/// logic, not the database.
/// </summary>
public class WorkflowRunStateMachineTests
{
    private static SabEngineDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SabEngineDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SabEngineDbContext(options);
    }

    [Fact]
    public async Task RequestAsync_creates_a_run_in_the_Requested_state_with_a_root_audit_entry()
    {
        await using var db = CreateDbContext();
        var sut = new WorkflowRunStateMachine(db);

        var run = await sut.RequestAsync("patch-windows-server", "srv-01", actor: "scheduler");

        Assert.Equal(WorkflowState.Requested, run.State);

        var entry = await db.AuditEntries.SingleAsync(e => e.WorkflowRunId == run.Id);
        Assert.Null(entry.PreviousEntryHash);
        Assert.Equal(WorkflowState.Requested, entry.FromState);
        Assert.Equal(WorkflowState.Requested, entry.ToState);
    }

    [Fact]
    public async Task The_full_happy_path_transitions_in_order_and_chains_every_audit_hash()
    {
        await using var db = CreateDbContext();
        var sut = new WorkflowRunStateMachine(db);

        var run = await sut.RequestAsync("patch-windows-server", "srv-01", actor: "scheduler");

        await sut.TransitionAsync(run.Id, WorkflowState.PlanDrafted, actor: "ai-agent");
        await sut.TransitionAsync(run.Id, WorkflowState.PendingApproval, actor: "ai-agent");
        await sut.TransitionAsync(run.Id, WorkflowState.Approved, actor: "brock");
        await sut.TransitionAsync(run.Id, WorkflowState.Executing, actor: "orchestration-engine");
        await sut.TransitionAsync(run.Id, WorkflowState.Completed, actor: "orchestration-engine");

        var reloaded = await db.WorkflowRuns.SingleAsync(w => w.Id == run.Id);
        Assert.Equal(WorkflowState.Completed, reloaded.State);

        var entries = await db.AuditEntries
            .Where(e => e.WorkflowRunId == run.Id)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        Assert.Equal(6, entries.Count); // the root entry + 5 transitions

        // Every entry after the first must chain to the one before it —
        // this is what makes the trail tamper-evident (Section 7).
        for (var i = 1; i < entries.Count; i++)
        {
            Assert.Equal(entries[i - 1].Hash, entries[i].PreviousEntryHash);
        }
    }

    [Fact]
    public async Task An_illegal_transition_throws_and_leaves_state_unchanged()
    {
        await using var db = CreateDbContext();
        var sut = new WorkflowRunStateMachine(db);

        var run = await sut.RequestAsync("patch-windows-server", "srv-01", actor: "scheduler");

        // Requested -> Executing is not in the allowed map — the whole
        // point of Section 4.1's state machine is that this must be
        // impossible, not just discouraged.
        await Assert.ThrowsAsync<InvalidWorkflowStateTransitionException>(
            () => sut.TransitionAsync(run.Id, WorkflowState.Executing, actor: "someone-in-a-hurry"));

        var reloaded = await db.WorkflowRuns.SingleAsync(w => w.Id == run.Id);
        Assert.Equal(WorkflowState.Requested, reloaded.State);
    }

    [Fact]
    public async Task A_declined_plan_can_be_revised_by_returning_to_PlanDrafted()
    {
        await using var db = CreateDbContext();
        var sut = new WorkflowRunStateMachine(db);

        var run = await sut.RequestAsync("patch-windows-server", "srv-01", actor: "scheduler");
        await sut.TransitionAsync(run.Id, WorkflowState.PlanDrafted, actor: "ai-agent");
        await sut.TransitionAsync(run.Id, WorkflowState.PendingApproval, actor: "ai-agent");
        await sut.TransitionAsync(run.Id, WorkflowState.Declined, actor: "brock");

        // Section 4.3: "Decline it, and nothing runs — the agent can
        // revise and try again" — on the same run.
        await sut.TransitionAsync(run.Id, WorkflowState.PlanDrafted, actor: "ai-agent");

        var reloaded = await db.WorkflowRuns.SingleAsync(w => w.Id == run.Id);
        Assert.Equal(WorkflowState.PlanDrafted, reloaded.State);
    }

    [Fact]
    public async Task A_failed_run_can_be_rolled_back()
    {
        await using var db = CreateDbContext();
        var sut = new WorkflowRunStateMachine(db);

        var run = await sut.RequestAsync("patch-windows-server", "srv-01", actor: "scheduler");
        await sut.TransitionAsync(run.Id, WorkflowState.PlanDrafted, actor: "ai-agent");
        await sut.TransitionAsync(run.Id, WorkflowState.PendingApproval, actor: "ai-agent");
        await sut.TransitionAsync(run.Id, WorkflowState.Approved, actor: "brock");
        await sut.TransitionAsync(run.Id, WorkflowState.Executing, actor: "orchestration-engine");
        await sut.TransitionAsync(run.Id, WorkflowState.Failed, actor: "orchestration-engine");
        await sut.TransitionAsync(run.Id, WorkflowState.RolledBack, actor: "orchestration-engine");

        var reloaded = await db.WorkflowRuns.SingleAsync(w => w.Id == run.Id);
        Assert.Equal(WorkflowState.RolledBack, reloaded.State);
    }
}
