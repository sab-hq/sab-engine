using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net.Sockets;
using System.Security;
using SabEngine.Core;

namespace SabEngine.Execution;

/// <summary>
/// The first real IExecutionConnector implementation — reaches a target
/// over WinRM (PowerShell remoting), per docs/design/SAB_Design_Document_v0.1.2.md,
/// Section 4.4. See docs/learn/execution-environment.md for the
/// plain-language version.
///
/// Resolves credentialHandle via ISecretStore (PD-9) at connection time
/// — the raw credential is never held by a module or the AI agent, only
/// by this connector, and only for as long as the connection is open.
///
/// Reads module scripts directly from a local checkout of the OSML
/// (sab-modules), since there's no real module catalog loader yet (a
/// gap flagged repeatedly since PD-6) — modulesRootPath points at
/// wherever that checkout's modules/ folder actually lives.
///
/// TESTABILITY NOTE: real WinRM connections need a real remote Windows
/// target — there's no way to meaningfully mock WSManConnectionInfo
/// itself. The actual remote-connection code path in ConnectAsync is
/// genuinely unverified without hitting a real target (the lab VM,
/// PD-11). What IS testable, and what this project's tests cover, is
/// everything downstream of "a runspace exists" — script resolution,
/// parameter passing, result construction, error handling — using a
/// real LOCAL runspace as a substitute, injected via openRunspace.
/// </summary>
public sealed class WinRmExecutionConnector(
    ISecretStore secretStore,
    string modulesRootPath,
    Func<string, PSCredential, Runspace>? openRunspace = null) : IExecutionConnector
{
    private readonly Func<string, PSCredential, Runspace> _openRunspace = openRunspace ?? DefaultOpenRunspace;

    public async Task<IExecutionSession> ConnectAsync(string target, string credentialHandle, CancellationToken cancellationToken = default)
    {
        var secretJson = await secretStore.GetSecretAsync(credentialHandle, cancellationToken)
            ?? throw new InvalidOperationException($"No credential found for handle '{credentialHandle}'.");

        var credential = StoredCredential.FromJson(secretJson);

        var secureString = new SecureString();
        foreach (var c in credential.Password)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();
        var psCredential = new PSCredential(credential.Username, secureString);

        var runspace = await Task.Run(() => _openRunspace(target, psCredential), cancellationToken);

        return new WinRmExecutionSession(runspace, modulesRootPath);
    }

    public async Task<bool> HealthCheckAsync(string target, CancellationToken cancellationToken = default)
    {
        // A lightweight TCP reachability check on WinRM's HTTP port
        // (5985) — cheaper and faster than opening a full runspace just
        // to find out whether the target is even reachable at all.
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(target, 5985, cancellationToken).AsTask();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            var completed = await Task.WhenAny(connectTask, timeoutTask);
            return completed == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static Runspace DefaultOpenRunspace(string target, PSCredential credential)
    {
        var connectionInfo = new WSManConnectionInfo(
            useSsl: false,
            target,
            5985,
            "/wsman",
            "http://schemas.microsoft.com/powershell/Microsoft.PowerShell",
            credential);

        var runspace = RunspaceFactory.CreateRunspace(connectionInfo);
        runspace.Open();
        return runspace;
    }
}
