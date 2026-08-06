using Microsoft.EntityFrameworkCore;
using SabEngine.Core;
using Xunit;

namespace SabEngine.Orchestration.Tests;

/// <summary>
/// Verifies the claim/lease pattern from docs/design/SAB_Design_Document_v0.1.2.md,
/// Section 4.1. Runs against a real, disposable Postgres database (see
/// PostgresTestDatabase.cs) rather than EF Core's InMemory provider —
/// InMemory can't translate the atomic ExecuteUpdateAsync this service
/// depends on, so testing against it would validate the wrong thing.
/// Requires Docker running locally (`docker compose up -d`).
/// </summary>
public class WorkflowRunClaimServiceTests : IAsyncLifetime
{
    private readonly PostgresTestDatabase _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync().AsTask();

    private static WorkflowRun NewRun(WorkflowState state, DateTimeOffset createdAt) => new()
    {
        WorkflowId = "patch-windows-server",
        Target = "srv-01",
        State = state,
        CreatedAt = createdAt,
    };

    [Fact]
    public async Task Claims_the_oldest_eligible_run_first()
    {
        var db = _fixture.Context;
        var older = NewRun(WorkflowState.Requested, DateTimeOffset.UtcNow.AddMinutes(-10));
        var newer = NewRun(WorkflowState.Requested, DateTimeOffset.UtcNow.AddMinutes(-1));
        db.WorkflowRuns.AddRange(older, newer);
        await db.SaveChangesAsync();

        var sut = new WorkflowRunClaimService(db);
        var claimed = await sut.TryClaimNextAsync("worker-1", TimeSpan.FromMinutes(5));

        Assert.NotNull(claimed);
        Assert.Equal(older.Id, claimed!.Id);
        Assert.Equal("worker-1", claimed.ClaimedByWorkerId);
        Assert.NotNull(claimed.ClaimedAt);
    }

    [Theory]
    [InlineData(WorkflowState.Requested)]
    [InlineData(WorkflowState.Declined)]
    [InlineData(WorkflowState.Approved)]
    [InlineData(WorkflowState.Failed)]
    public async Task Claims_runs_in_any_claimable_state(WorkflowState state)
    {
        var db = _fixture.Context;
        var run = NewRun(state, DateTimeOffset.UtcNow);
        db.WorkflowRuns.Add(run);
        await db.SaveChangesAsync();

        var sut = new WorkflowRunClaimService(db);
        var claimed = await sut.TryClaimNextAsync("worker-1", TimeSpan.FromMinutes(5));

        Assert.NotNull(claimed);
        Assert.Equal(run.Id, claimed!.Id);
    }

    [Theory]
    [InlineData(WorkflowState.PlanDrafted)]
    [InlineData(WorkflowState.PendingApproval)]
    [InlineData(WorkflowState.Executing)]
    [InlineData(WorkflowState.Completed)]
    [InlineData(WorkflowState.RolledBack)]
    public async Task Never_claims_runs_outside_the_claimable_states(WorkflowState state)
    {
        var db = _fixture.Context;
        db.WorkflowRuns.Add(NewRun(state, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var sut = new WorkflowRunClaimService(db);
        var claimed = await sut.TryClaimNextAsync("worker-1", TimeSpan.FromMinutes(5));

        // These states either belong to a human (PendingApproval), a
        // worker already mid-flight (PlanDrafted, Executing), or are
        // terminal (Completed, RolledBack) — none should ever be handed
        // out by the claim service.
        Assert.Null(claimed);
    }

    [Fact]
    public async Task Returns_null_when_nothing_is_claimable()
    {
        var db = _fixture.Context;
        var sut = new WorkflowRunClaimService(db);

        var claimed = await sut.TryClaimNextAsync("worker-1", TimeSpan.FromMinutes(5));

        Assert.Null(claimed);
    }

    [Fact]
    public async Task A_run_already_claimed_with_a_fresh_lease_is_not_claimed_again()
    {
        var db = _fixture.Context;
        db.WorkflowRuns.Add(NewRun(WorkflowState.Requested, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var sut = new WorkflowRunClaimService(db);
        var first = await sut.TryClaimNextAsync("worker-1", TimeSpan.FromMinutes(5));
        var second = await sut.TryClaimNextAsync("worker-2", TimeSpan.FromMinutes(5));

        Assert.NotNull(first);
        Assert.Null(second); // worker-2 must not be able to steal worker-1's fresh claim
    }

    [Fact]
    public async Task A_run_whose_lease_has_expired_can_be_reclaimed_by_a_different_worker()
    {
        var db = _fixture.Context;
        var run = NewRun(WorkflowState.Requested, DateTimeOffset.UtcNow.AddMinutes(-10));
        run.ClaimedByWorkerId = "worker-1";
        run.ClaimedAt = DateTimeOffset.UtcNow.AddMinutes(-10); // stale — worker-1 presumably crashed
        db.WorkflowRuns.Add(run);
        await db.SaveChangesAsync();

        var sut = new WorkflowRunClaimService(db);

        // A short lease means the 10-minute-old claim above has already expired.
        var reclaimed = await sut.TryClaimNextAsync("worker-2", TimeSpan.FromMinutes(1));

        Assert.NotNull(reclaimed);
        Assert.Equal("worker-2", reclaimed!.ClaimedByWorkerId);
    }

    [Fact]
    public async Task ReleaseClaimAsync_clears_the_claim_fields()
    {
        var db = _fixture.Context;
        db.WorkflowRuns.Add(NewRun(WorkflowState.Requested, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var sut = new WorkflowRunClaimService(db);
        var claimed = await sut.TryClaimNextAsync("worker-1", TimeSpan.FromMinutes(5));
        await sut.ReleaseClaimAsync(claimed!.Id);

        var reloaded = await db.WorkflowRuns.AsNoTracking().SingleAsync(w => w.Id == claimed.Id);
        Assert.Null(reloaded.ClaimedByWorkerId);
        Assert.Null(reloaded.ClaimedAt);
    }
}
