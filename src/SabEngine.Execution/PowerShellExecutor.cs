using System.Management.Automation;

namespace SabEngine.Execution;

/// <summary>Runs a PowerShell script and returns a structured result.</summary>
public interface IPowerShellExecutor
{
    Task<PowerShellExecutionResult> RunScriptAsync(
        string script,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The PowerShell interop primitive from docs/design/SAB_Design_Document_v0.1.2.md,
/// Section 4.4 (pre-development-checklist.md, PD-7). Runs locally, in-process
/// via Microsoft.PowerShell.SDK — this is deliberately the first, simplest
/// version. The WinRM connector (PD-17–PD-20, not built yet) will reuse
/// this same interop, just pointed at a remote session/runspace instead
/// of the local one this class creates by default.
///
/// A script with a matching <c>param()</c> block picks up
/// <paramref name="parameters"/> by name — e.g. a script starting with
/// <c>param($PatchIds)</c> receives whatever's passed under the key
/// <c>"PatchIds"</c>.
/// </summary>
public sealed class PowerShellExecutor : IPowerShellExecutor
{
    public async Task<PowerShellExecutionResult> RunScriptAsync(
        string script,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var ps = System.Management.Automation.PowerShell.Create();
        ps.AddScript(script);

        if (parameters is not null)
        {
            foreach (var (name, value) in parameters)
            {
                ps.AddParameter(name, value);
            }
        }

        // NOTE: System.Management.Automation.PowerShell.Invoke() is
        // synchronous with no built-in cancellation; wrapping it in
        // Task.Run() lets the caller await it, but cancellationToken
        // here only prevents starting the task, not aborting a script
        // already mid-execution. Wiring cancellationToken to ps.Stop()
        // for real mid-run cancellation is a reasonable future
        // improvement, not itemized as its own PD- entry yet.
        //
        // PowerShell distinguishes non-terminating errors (e.g.
        // Write-Error — captured gracefully into ps.Streams.Error,
        // HadErrors set) from terminating errors (e.g. an unhandled
        // `throw` — which Invoke() propagates as a real .NET exception
        // instead). Both need to land in the same structured result; a
        // module's script throwing shouldn't crash this caller with an
        // unhandled exception.
        try
        {
            var results = await Task.Run(() => ps.Invoke(), cancellationToken).ConfigureAwait(false);

            var output = results.Select(r => r?.ToString() ?? string.Empty).ToList();
            var errors = ps.Streams.Error.Select(e => e.ToString()).ToList();

            return new PowerShellExecutionResult(
                Succeeded: !ps.HadErrors,
                Output: output,
                Errors: errors);
        }
        catch (Exception ex)
        {
            var errors = ps.Streams.Error.Select(e => e.ToString()).Append(ex.Message).ToList();
            return new PowerShellExecutionResult(Succeeded: false, Output: [], Errors: errors);
        }
    }
}
