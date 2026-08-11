namespace SabEngine.Core;

/// <summary>
/// The connector contract every execution environment must implement,
/// per docs/SAB_Design_Document_v0.1.2.md, Section 4.4 ("Connector
/// interface, first draft"). The first real implementation (WinRM, for
/// on-prem Windows) lives in SabEngine.Execution —
/// see pre-development-checklist.md, PD-17 through PD-20.
/// </summary>
public interface IExecutionConnector
{
    /// <summary>
    /// <paramref name="credentialHandle"/> is deliberately never the raw
    /// credential itself — it's a reference this connector resolves
    /// against the secrets store at connection time (design doc, Section
    /// 7), so neither the module nor the AI agent layer ever holds a
    /// real secret.
    /// </summary>
    Task<IExecutionSession> ConnectAsync(string target, string credentialHandle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Probes reachability before committing a worker to a potentially
    /// hanging connection attempt, rather than discovering a dead target
    /// mid-execution.
    /// </summary>
    Task<bool> HealthCheckAsync(string target, CancellationToken cancellationToken = default);
}

public interface IExecutionSession : IAsyncDisposable
{
    /// <summary>
    /// <paramref name="workflowRunId"/> was missing from this signature
    /// until PD-23 — a real gap found while actually building the first
    /// implementation of this interface: <see cref="ExecutionResult"/>
    /// requires a <c>WorkflowRunId</c>, but there was previously no way
    /// for a caller to supply one. Fixed here rather than worked around,
    /// since every future connector implementation needs this too, not
    /// just the WinRM one.
    /// </summary>
    Task<ExecutionResult> ExecuteAsync(Guid workflowRunId, string moduleId, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
}
