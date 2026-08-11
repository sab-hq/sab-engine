using System.Management.Automation;
using System.Management.Automation.Runspaces;
using SabEngine.Core;

namespace SabEngine.Execution;

/// <summary>
/// Wraps an open WinRM runspace. See WinRmExecutionConnector for the
/// full picture — this is what actually resolves a moduleId to a real
/// script file (from a local OSML checkout) and executes it against the
/// remote target.
/// </summary>
public sealed class WinRmExecutionSession(Runspace runspace, string modulesRootPath) : IExecutionSession
{
    public async Task<ExecutionResult> ExecuteAsync(
        Guid workflowRunId,
        string moduleId,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var scriptPath = Path.Combine(modulesRootPath, moduleId, $"{moduleId}.ps1");

        if (!File.Exists(scriptPath))
        {
            return new ExecutionResult
            {
                WorkflowRunId = workflowRunId,
                ModuleId = moduleId,
                Succeeded = false,
                Output = $"No script found for module '{moduleId}' at '{scriptPath}'.",
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
            };
        }

        var scriptText = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddScript(scriptText);
        foreach (var (name, value) in parameters)
        {
            ps.AddParameter(name, value);
        }

        try
        {
            var results = await Task.Run(() => ps.Invoke(), cancellationToken);
            var output = string.Join(Environment.NewLine, results.Select(r => r?.ToString() ?? string.Empty));

            // Same non-terminating-vs-terminating-error lesson from
            // PD-7's PowerShellExecutor: a genuine `throw` in the module
            // script is caught below by the try/catch; ps.HadErrors
            // covers non-terminating errors (Write-Error) here.
            string fullOutput = output;
            if (ps.HadErrors)
            {
                var errors = string.Join(Environment.NewLine, ps.Streams.Error.Select(e => e.ToString()));
                fullOutput = string.IsNullOrEmpty(output) ? errors : $"{output}{Environment.NewLine}{errors}";
            }

            return new ExecutionResult
            {
                WorkflowRunId = workflowRunId,
                ModuleId = moduleId,
                Succeeded = !ps.HadErrors,
                Output = fullOutput,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
            };
        }
        catch (Exception ex)
        {
            return new ExecutionResult
            {
                WorkflowRunId = workflowRunId,
                ModuleId = moduleId,
                Succeeded = false,
                Output = ex.Message,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
            };
        }
    }

    public ValueTask DisposeAsync()
    {
        runspace.Close();
        runspace.Dispose();
        return ValueTask.CompletedTask;
    }
}
