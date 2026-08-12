using SabEngine.Execution;
using Xunit;

namespace SabEngine.Execution.Tests;

/// <summary>
/// Verifies CredentialHandleResolver (PD-25) — the tier-handle-with-fallback
/// convention that makes least-privilege credential selection possible.
/// </summary>
public sealed class CredentialHandleResolverTests
{
    [Fact]
    public async Task Resolves_to_the_tier_specific_handle_when_one_is_registered()
    {
        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("srv-01:elevated", new StoredCredential("AdminUser", "secret").ToJson());

        var sut = new CredentialHandleResolver(secretStore);
        var handle = await sut.ResolveAsync("srv-01", CredentialTier.Elevated);

        Assert.Equal("srv-01:elevated", handle);
    }

    [Fact]
    public async Task Falls_back_to_the_bare_target_handle_when_no_tier_specific_credential_exists()
    {
        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("srv-01", new StoredCredential("StandardUser", "secret").ToJson());

        var sut = new CredentialHandleResolver(secretStore);
        var handle = await sut.ResolveAsync("srv-01", CredentialTier.Elevated);

        // No "srv-01:elevated" registered — falls back to the single
        // standing credential rather than failing outright.
        Assert.Equal("srv-01", handle);
    }

    [Fact]
    public async Task Different_tiers_for_the_same_target_resolve_independently()
    {
        var secretStore = new FakeSecretStore();
        await secretStore.SetSecretAsync("srv-01:standard", new StoredCredential("ReadOnlyUser", "secret").ToJson());
        // Deliberately no "srv-01:elevated" and no bare "srv-01" registered.

        var sut = new CredentialHandleResolver(secretStore);

        var standardHandle = await sut.ResolveAsync("srv-01", CredentialTier.Standard);
        var elevatedHandle = await sut.ResolveAsync("srv-01", CredentialTier.Elevated);

        Assert.Equal("srv-01:standard", standardHandle);
        Assert.Equal("srv-01", elevatedHandle); // falls back, since no tier-specific or bare handle changes this
    }

    [Fact]
    public async Task Resolving_for_a_target_with_no_credentials_at_all_still_falls_back_cleanly()
    {
        var secretStore = new FakeSecretStore();
        var sut = new CredentialHandleResolver(secretStore);

        var handle = await sut.ResolveAsync("brand-new-server", CredentialTier.Standard);

        // Resolving never throws by itself — ConnectAsync (PD-23) is what
        // actually fails loudly if the resolved handle has nothing behind it.
        Assert.Equal("brand-new-server", handle);
    }
}
