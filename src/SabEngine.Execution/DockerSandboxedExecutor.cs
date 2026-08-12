using System.Diagnostics;

namespace SabEngine.Execution;

public interface IDockerSandboxedExecutor
{
    Task<PowerShellExecutionResult> RunScriptAsync(
        string scriptPath,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Docker-based module sandboxing, per SE-2 (confirmed) and
/// pre-development-checklist.md, PD-27. Runs a PowerShell script inside
/// a disposable container rather than directly on the host — the
/// mechanism a future module-validation step (e.g. promoting
/// lab-validated → production-approved, PD-29) would use to safely
/// dry-run a module's actual script before it's trusted enough to send
/// anywhere real, matching this item's own framing: sandboxing that
/// wraps execution "before real testing begins."
///
/// TWO REAL ISOLATION GUARANTEES, both enforced, not just assumed:
/// (1) --network none — the container gets no network access at all;
/// (2) the script's own directory is mounted read-only — the script
/// cannot modify anything outside the ephemeral container itself.
///
/// A REAL, HONEST LIMITATION — not glossed over: this defaults to the
/// standard Linux-based mcr.microsoft.com/powershell image, which is
/// what Docker Desktop runs without any reconfiguration. SAB's actual
/// four modules (pre-flight-check, stage-patches, apply-patches,
/// validate) are all Windows-specific — WUA COM APIs, Get-Service,
/// wusa.exe — none of which exist inside a Linux container. Sandboxing
/// those specific modules for real would need a Windows-based container
/// image, which requires switching Docker Desktop to Windows-container
/// mode — a genuinely disruptive choice, not something to force as a
/// silent default. This class sandboxes *any* PowerShell script
/// correctly; it does not yet sandbox SAB's own real modules end to end.
/// </summary>
public sealed class DockerSandboxedExecutor(string sandboxImage = "mcr.microsoft.com/powershell:latest") : IDockerSandboxedExecutor
{
    public async Task<PowerShellExecutionResult> RunScriptAsync(
        string scriptPath,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(scriptPath))
        {
            return new PowerShellExecutionResult(false, [], [$"No script found at '{scriptPath}'."]);
        }

        var scriptDir = Path.GetDirectoryName(Path.GetFullPath(scriptPath))!;
        var scriptFileName = Path.GetFileName(scriptPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--rm");
        startInfo.ArgumentList.Add("--network");
        startInfo.ArgumentList.Add("none");
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add($"{scriptDir}:/sab-module:ro");
        startInfo.ArgumentList.Add(sandboxImage);
        startInfo.ArgumentList.Add("pwsh");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add($"/sab-module/{scriptFileName}");

        if (parameters is not null)
        {
            foreach (var (name, value) in parameters)
            {
                startInfo.ArgumentList.Add($"-{name}");
                startInfo.ArgumentList.Add(value?.ToString() ?? string.Empty);
            }
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            var output = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var errors = stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new PowerShellExecutionResult(
                Succeeded: process.ExitCode == 0,
                Output: output,
                Errors: errors);
        }
        catch (Exception ex)
        {
            return new PowerShellExecutionResult(false, [], [ex.Message]);
        }
    }
}
