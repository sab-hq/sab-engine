using SabEngine.Core;

namespace SabEngine.Execution;

public interface ICredentialHandleResolver
{
    /// <summary>
    /// Resolves which stored credential handle to actually use for a
    /// given target and tier — the piece that makes least-privilege
    /// credential selection possible at all, rather than every caller
    /// hardcoding one handle for everything.
    /// </summary>
    Task<string> ResolveAsync(string target, CredentialTier tier, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implements Section 4.4's least-privilege principle via a simple,
/// real convention: a tier-specific handle is named
/// <c>"{target}:{tier}"</c> (e.g. <c>"srv-01:elevated"</c>). If nothing
/// is registered under that name, this falls back to a bare
/// <c>"{target}"</c> handle — the "one standing credential" pattern —
/// so an operator can adopt tiered credentials gradually, server by
/// server, rather than needing every target reconfigured before any of
/// this works.
///
/// NOTE ON SCOPE (pre-development-checklist.md, PD-25): this resolver
/// exists and is real, tested code — but nothing calls it per-module
/// yet, because the orchestration engine itself still doesn't wire
/// modules together (a gap flagged since PD-4). Wiring "each module in
/// a workflow opens its own appropriately-scoped connection" is real,
/// separate integration work once that orchestration wiring exists —
/// not itemized as its own PD- entry yet.
/// </summary>
public sealed class CredentialHandleResolver(ISecretStore secretStore) : ICredentialHandleResolver
{
    public async Task<string> ResolveAsync(string target, CredentialTier tier, CancellationToken cancellationToken = default)
    {
        var tierHandle = $"{target}:{tier.ToString().ToLowerInvariant()}";

        var tierSpecificCredentialExists = await secretStore.GetSecretAsync(tierHandle, cancellationToken) is not null;
        if (tierSpecificCredentialExists)
        {
            return tierHandle;
        }

        return target;
    }
}
