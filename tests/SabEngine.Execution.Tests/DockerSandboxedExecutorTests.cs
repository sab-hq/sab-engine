using SabEngine.Execution;
using Xunit;

namespace SabEngine.Execution.Tests;

/// <summary>
/// Verifies DockerSandboxedExecutor (PD-27) against a REAL Docker
/// daemon — no way to meaningfully mock container isolation the way
/// COM boundaries were mocked elsewhere. Requires Docker Desktop
/// running, same standing prerequisite as the Postgres-dependent
/// Orchestration tests. UNLIKE every other test in this project, the
/// first run here will also pull a real image
/// (mcr.microsoft.com/powershell:latest, several hundred MB) over the
/// network — expect the first run to be noticeably slower than
/// everything else in the suite; subsequent runs use the cached image.
///
/// These tests deliberately use plain, cross-platform PowerShell
/// scripts, not SAB's real modules — see DockerSandboxedExecutor's own
/// docs for why the real modules (Windows-only APIs) don't run inside
/// the default Linux container this class uses.
/// </summary>
public sealed class DockerSandboxedExecutorTests : IDisposable
{
    private readonly string _scriptDir = Path.Combine(Path.GetTempPath(), $"sab-test-sandbox-{Guid.NewGuid():N}");

    public DockerSandboxedExecutorTests()
    {
        Directory.CreateDirectory(_scriptDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_scriptDir))
        {
            Directory.Delete(_scriptDir, recursive: true);
        }
    }

    private string WriteScript(string fileName, string content)
    {
        var path = Path.Combine(_scriptDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task A_simple_script_runs_successfully_inside_the_sandbox()
    {
        var scriptPath = WriteScript("hello.ps1", "Write-Output 'hello from sandbox'");

        var sut = new DockerSandboxedExecutor();
        var result = await sut.RunScriptAsync(scriptPath);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Output, line => line.Contains("hello from sandbox"));
    }

    [Fact]
    public async Task The_sandbox_genuinely_has_no_network_access()
    {
        // Proves the isolation guarantee directly, rather than just
        // trusting the --network none flag does what it claims. The
        // script catches its own failure and reports it via output, so
        // this test doesn't depend on interpreting exit codes from a
        // failed network call inside the container.
        var scriptPath = WriteScript("network-check.ps1", """
            try {
                $null = Invoke-WebRequest -Uri 'http://example.com' -TimeoutSec 3 -UseBasicParsing
                Write-Output 'NETWORK_REACHABLE'
            } catch {
                Write-Output 'NETWORK_BLOCKED'
            }
            """);

        var sut = new DockerSandboxedExecutor();
        var result = await sut.RunScriptAsync(scriptPath);

        Assert.Contains(result.Output, line => line.Contains("NETWORK_BLOCKED"));
    }

    [Fact]
    public async Task The_mounted_script_directory_is_genuinely_read_only()
    {
        var scriptPath = WriteScript("write-check.ps1", """
            try {
                'test' | Out-File -FilePath '/sab-module/should-not-be-writable.txt' -ErrorAction Stop
                Write-Output 'WRITE_SUCCEEDED'
            } catch {
                Write-Output 'WRITE_BLOCKED'
            }
            """);

        var sut = new DockerSandboxedExecutor();
        var result = await sut.RunScriptAsync(scriptPath);

        Assert.Contains(result.Output, line => line.Contains("WRITE_BLOCKED"));
        Assert.False(File.Exists(Path.Combine(_scriptDir, "should-not-be-writable.txt")));
    }

    [Fact]
    public async Task A_script_that_exits_non_zero_is_reported_as_failed()
    {
        var scriptPath = WriteScript("failing.ps1", "Write-Error 'deliberate failure'; exit 1");

        var sut = new DockerSandboxedExecutor();
        var result = await sut.RunScriptAsync(scriptPath);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task A_nonexistent_script_fails_cleanly_without_even_invoking_docker()
    {
        var sut = new DockerSandboxedExecutor();
        var result = await sut.RunScriptAsync(Path.Combine(_scriptDir, "does-not-exist.ps1"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("No script found"));
    }
}
