using SabEngine.Core;
using Xunit;

namespace SabEngine.Execution.Tests;

/// <summary>
/// A minimal in-memory ISecretStore for testing — faster and more
/// portable than using the real WindowsCredentialManagerSecretStore
/// (PD-9) for these specific tests, which aren't about the secrets
/// backend itself.
/// </summary>
public sealed class FakeSecretStore : ISecretStore
{
    private readonly Dictionary<string, string> _secrets = new();

    public Task<string?> GetSecretAsync(string handle, CancellationToken cancellationToken = default) =>
        Task.FromResult(_secrets.TryGetValue(handle, out var value) ? value : null);

    public Task SetSecretAsync(string handle, string secretValue, CancellationToken cancellationToken = default)
    {
        _secrets[handle] = secretValue;
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string handle, CancellationToken cancellationToken = default)
    {
        _secrets.Remove(handle);
        return Task.CompletedTask;
    }
}
