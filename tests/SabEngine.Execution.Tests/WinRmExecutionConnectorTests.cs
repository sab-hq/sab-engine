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
}
