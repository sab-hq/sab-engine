using System.Management.Automation;
using System.Management.Automation.Runspaces;
using SabEngine.Core;

namespace SabEngine.Execution;

/// <summary>
/// Wraps an open WinRM runspace. See WinRmExecutionConnector for the
/// full picture — this is what actually resolves a moduleId to a real
/// script file (from a local OSML checkout) and executes it against the
/// remote target.
///
/// ISOLATION (PD-26): a hang against this session's target must never
/// affect any other target's execution. Two things make that real,
/// rather than aspirational: (1) the blocking PowerShell invocation runs
/// on a dedicated thread (TaskCreationOptions.LongRunning), not the
/// shared .NET thread pool other targets' executions might also depend
/// on — no cross-target thread-pool starvation; (2) a real, enforced
/// timeout calls PowerShell's Stop() to forcibly interrupt a hung
/// pipeline, rather than the previous behavior (noted here until now)
/// where an external cancellationToken only prevented *starting* the
/// work, never interrupted it mid-flight.
///
/// NOTE — a real, unverified assumption: this is the first use of
/// PowerShell's Stop() in this project. It's assumed to make a blocking
/// Invoke() on another thread either throw or return early with partial
/// results — the code below handles both possibilities, but which one
/// actually happens is genuinely unverified until tested against a real
/// long-running script. See pre-development-checklist.md, PD-26.
/// </summary>
public sealed class WinRmExecutionSession(Runspace runspace, string modulesRootPath, TimeSpan? executionTimeout = null) : IExecutionSession
{
    private readonly TimeSpan _executionTimeout = executionTimeout ?? TimeSpan.FromMinutes(10);

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

        using var timeoutCts = new CancellationTokenSource(_executionTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // A dedicated thread, not the shared pool — see ISOLATION note
        // above. Deliberately CancellationToken.None here: we don't want
        // the Task itself to transition to Canceled before Invoke() even
        // starts (a real risk if the pool is momentarily busy) — the
        // actual interruption mechanism is ps.Stop() below, not this.
        var invokeTask = Task.Factory.StartNew(
            () => ps.Invoke(),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        using var stopRegistration = combinedCts.Token.Register(() =>
        {
            try { ps.Stop(); } catch { /* best-effort interrupt; the pipeline may already be finishing */ }
        });

        try
        {
            var results = await invokeTask;

            if (timeoutCts.IsCancellationRequested)
            {
                return TimedOutResult(workflowRunId, moduleId, startedAt);
            }

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
            if (timeoutCts.IsCancellationRequested)
            {
                return TimedOutResult(workflowRunId, moduleId, startedAt);
            }

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

    private ExecutionResult TimedOutResult(Guid workflowRunId, string moduleId, DateTimeOffset startedAt) => new()
    {
        WorkflowRunId = workflowRunId,
        ModuleId = moduleId,
        Succeeded = false,
        Output = $"Execution timed out after {_executionTimeout} and was forcibly stopped — this target's hang did not block any other target's execution.",
        StartedAt = startedAt,
        CompletedAt = DateTimeOffset.UtcNow,
    };

    public ValueTask DisposeAsync()
    {
        runspace.Close();
        runspace.Dispose();
        return ValueTask.CompletedTask;
    }
}
