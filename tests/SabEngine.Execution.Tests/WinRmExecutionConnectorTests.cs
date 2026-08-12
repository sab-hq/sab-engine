using System.Diagnostics;
using System.Management.Automation.Runspaces;
using SabEngine.Execution;
using Xunit;

namespace SabEngine.Execution.Tests;

/// <summary>
/// Verifies WinRmExecutionConnector/WinRmExecutionSession (PD-23).
/// A real WinRM connection needs a real remote Windows target — there's
/// no way to meaningfully mock WSManConnectionInfo — so ConnectAsync's
/// actual remote-connection code path is genuinely unverified by these
/// tests. What IS covered: credential resolution/parsing (via a fake
/// ISecretStore), and everything downstream of "a runspace exists" —
/// script resolution, parameter passing, result construction, error
/// handling — using a real LOCAL runspace substituted via the
/// openRunspace seam.
/// </summary>
public sealed class WinRmExecutionConnectorTests : IDisposable
{
    private readonly string _modulesRoot = Path.Combine(Path.GetTempPath(), $"sab-test-modules-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_modulesRoot))
        {
            Directory.Delete(_modulesRoot, recursive: true);
        }
    }

    private void WriteTestModule(string moduleId, string scriptContent)
    {
        var dir = Path.Combine(_modulesRoot, moduleId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{moduleId}.ps1"), scriptContent);
    }

    private static Runspace OpenLocalRunspaceForTesting(string target, System.Management.Automation.PSCredential credential)
    {
        // Substitutes a real LOCAL runspace in place of a real remote
        // WinRM connection — target/credential are accepted (matching
        // the real signature) but intentionally unused here.
        var runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        return runspace;
    }

    [Fact]
    public async Task ConnectAsync_throws_when_the_credential_handle_has_no_stored_secret()
    {
        var secretStore = new FakeSecretStore();
        var sut = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ConnectAsync("some-target", "missing-handle"));
    }

    [Fact]
    public async Task ConnectAsync_succeeds_and_returns_a_usable_session_when_the_credential_is_valid()
    {
        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("srv-01-admin", new StoredCredential("Administrator", "correct-horse-battery-staple").ToJson());

        var sut = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting);
        await using var session = await sut.ConnectAsync("srv-01", "srv-01-admin");

        Assert.NotNull(session);
    }

    [Fact]
    public async Task ExecuteAsync_runs_a_real_module_script_and_reports_success()
    {
        WriteTestModule("test-module", "param($Name) Write-Output \"hello, $Name\"");

        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("h", new StoredCredential("user", "pass").ToJson());
        var connector = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting);
        await using var session = await connector.ConnectAsync("target", "h");

        var runId = Guid.NewGuid();
        var result = await session.ExecuteAsync(runId, "test-module", new Dictionary<string, object?> { ["Name"] = "sab-engine" });

        Assert.Equal(runId, result.WorkflowRunId);
        Assert.True(result.Succeeded);
        Assert.Contains("hello, sab-engine", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_reports_failure_when_the_module_script_writes_a_non_terminating_error()
    {
        WriteTestModule("failing-module", "Write-Error 'deliberate failure'");

        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("h", new StoredCredential("user", "pass").ToJson());
        var connector = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting);
        await using var session = await connector.ConnectAsync("target", "h");

        var result = await session.ExecuteAsync(Guid.NewGuid(), "failing-module", new Dictionary<string, object?>());

        Assert.False(result.Succeeded);
        Assert.Contains("deliberate failure", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_reports_failure_when_the_module_script_throws()
    {
        WriteTestModule("throwing-module", "throw 'deliberate terminating failure'");

        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("h", new StoredCredential("user", "pass").ToJson());
        var connector = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting);
        await using var session = await connector.ConnectAsync("target", "h");

        // Same throw-vs-Write-Error distinction PD-7's PowerShellExecutor
        // hit — this must not throw an unhandled exception out of
        // ExecuteAsync, it must return a structured failed result.
        var result = await session.ExecuteAsync(Guid.NewGuid(), "throwing-module", new Dictionary<string, object?>());

        Assert.False(result.Succeeded);
        Assert.Contains("deliberate terminating failure", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_reports_a_clean_failure_when_the_module_script_does_not_exist()
    {
        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("h", new StoredCredential("user", "pass").ToJson());
        var connector = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting);
        await using var session = await connector.ConnectAsync("target", "h");

        var result = await session.ExecuteAsync(Guid.NewGuid(), "nonexistent-module", new Dictionary<string, object?>());

        Assert.False(result.Succeeded);
        Assert.Contains("No script found", result.Output);
    }

    [Fact]
    public async Task HealthCheckAsync_returns_false_for_an_unreachable_target()
    {
        var secretStore = new FakeSecretStore();
        var sut = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting);

        // 240.0.0.0/4 is reserved (class E), guaranteed unroutable — a
        // reliable way to force a connection failure without depending
        // on any specific machine's actual network state.
        var reachable = await sut.HealthCheckAsync("240.0.0.1");

        Assert.False(reachable);
    }

    [Fact]
    public async Task A_hanging_script_times_out_and_is_forcibly_stopped_rather_than_blocking_forever()
    {
        // PD-26: the actual isolation guarantee — an enforced timeout
        // calls ps.Stop() to interrupt a hung pipeline. If this fails,
        // it fails by the test itself hanging, not by a normal assertion
        // failure — worth knowing if this test ever needs debugging.
        WriteTestModule("hanging-module", "Start-Sleep -Seconds 9999");

        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("h", new StoredCredential("user", "pass").ToJson());
        var connector = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting, executionTimeout: TimeSpan.FromSeconds(2));
        await using var session = await connector.ConnectAsync("target", "h");

        var stopwatch = Stopwatch.StartNew();
        var result = await session.ExecuteAsync(Guid.NewGuid(), "hanging-module", new Dictionary<string, object?>());
        stopwatch.Stop();

        Assert.False(result.Succeeded);
        Assert.Contains("timed out", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15), $"Expected the 2-second timeout to fire promptly; took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task A_hang_on_one_target_does_not_delay_execution_against_a_different_target()
    {
        // The actual PD-26 requirement, proven directly: run a hanging
        // execution and a fast one concurrently, against two separate
        // sessions (two separate targets), and confirm the fast one
        // finishes quickly regardless of the slow one's state. This is
        // deliberately a long timeout on the hanging one — the point is
        // that the FAST session is never blocked by it at all, not that
        // the slow one eventually times out.
        WriteTestModule("hanging-module", "Start-Sleep -Seconds 9999");
        WriteTestModule("fast-module", "Write-Output 'done fast'");

        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("h", new StoredCredential("user", "pass").ToJson());

        var slowConnector = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting, executionTimeout: TimeSpan.FromMinutes(10));
        var fastConnector = new WinRmExecutionConnector(secretStore, _modulesRoot, OpenLocalRunspaceForTesting, executionTimeout: TimeSpan.FromMinutes(10));

        await using var slowSession = await slowConnector.ConnectAsync("target-slow", "h");
        await using var fastSession = await fastConnector.ConnectAsync("target-fast", "h");

        // Deliberately not awaited to completion — we don't need the
        // hang to resolve to prove the fast target isn't blocked by it.
        var slowTask = slowSession.ExecuteAsync(Guid.NewGuid(), "hanging-module", new Dictionary<string, object?>());

        var stopwatch = Stopwatch.StartNew();
        var fastResult = await fastSession.ExecuteAsync(Guid.NewGuid(), "fast-module", new Dictionary<string, object?>());
        stopwatch.Stop();

        Assert.True(fastResult.Succeeded);
        Assert.Contains("done fast", fastResult.Output);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"The fast target took {stopwatch.Elapsed} — isolation may be broken; it should never wait on the slow target's hang.");

        _ = slowTask; // deliberately not awaited — see comment above
    }
}
