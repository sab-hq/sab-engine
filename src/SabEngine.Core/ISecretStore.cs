namespace SabEngine.Core;

/// <summary>
/// The pluggable secrets backend contract from docs/design/SAB_Design_Document_v0.1.2.md,
/// Section 7 ("Secrets Management") and SE-1 (confirmed): support
/// HashiCorp Vault as an option for organizations already running it,
/// with a native OS credential store as the simpler Phase 1 default —
/// avoid building custom secrets infrastructure.
///
/// This is what <see cref="IExecutionConnector"/>'s <c>credentialHandle</c>
/// parameter actually resolves against at connection time. Modules and
/// the AI agent layer never see this interface or a real secret directly
/// — only the execution environment does, at the moment it needs one.
/// </summary>
public interface ISecretStore
{
    /// <summary>Returns null if no secret exists for this handle — not an error, just "nothing there yet".</summary>
    Task<string?> GetSecretAsync(string handle, CancellationToken cancellationToken = default);

    Task SetSecretAsync(string handle, string secretValue, CancellationToken cancellationToken = default);

    /// <summary>Deleting a handle that doesn't exist is a no-op, not an error.</summary>
    Task DeleteSecretAsync(string handle, CancellationToken cancellationToken = default);
}
