using System.Text.Json;

namespace SabEngine.Execution;

/// <summary>
/// The shape a WinRM credential is stored as inside <c>ISecretStore</c>
/// (PD-9). <c>ISecretStore</c> only ever stores/retrieves a single
/// string per handle — it has no concept of "username + password" pairs
/// — so this is the convention: the secret string itself is this record,
/// JSON-serialized. A real design decision, not an interface change,
/// since the <c>ISecretStore</c> contract (Core) is already
/// verified/working code from PD-9 and shouldn't need to change just to
/// support one connector's specific credential shape.
/// </summary>
public sealed record StoredCredential(string Username, string Password)
{
    public string ToJson() => JsonSerializer.Serialize(this);

    public static StoredCredential FromJson(string json) =>
        JsonSerializer.Deserialize<StoredCredential>(json)
        ?? throw new InvalidOperationException("Stored credential JSON deserialized to null.");
}
