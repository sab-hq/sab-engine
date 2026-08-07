using SabEngine.Execution;
using Xunit;

namespace SabEngine.Execution.Tests;

/// <summary>
/// Verifies WindowsCredentialManagerSecretStore (pre-development-checklist.md,
/// PD-9) against the real Windows Credential Manager on this machine —
/// not a fake. Like PowerShellExecutorTests, there's no meaningful way
/// to fake whether P/Invoke marshaling into a real Win32 API actually
/// works; the whole point is real interop. Each test uses a unique,
/// Guid-based handle and cleans up after itself so nothing gets left
/// behind in the real Credential Manager on Brock's machine.
/// </summary>
public class WindowsCredentialManagerSecretStoreTests : IDisposable
{
    private readonly WindowsCredentialManagerSecretStore _sut = new();
    private readonly string _handle = $"test-{Guid.NewGuid():N}";

    public void Dispose() => _sut.DeleteSecretAsync(_handle).GetAwaiter().GetResult();

    [Fact]
    public async Task A_secret_that_was_never_set_returns_null()
    {
        var result = await _sut.GetSecretAsync(_handle);

        Assert.Null(result);
    }

    [Fact]
    public async Task Setting_then_getting_a_secret_round_trips_the_exact_value()
    {
        await _sut.SetSecretAsync(_handle, "correct-horse-battery-staple");

        var result = await _sut.GetSecretAsync(_handle);

        Assert.Equal("correct-horse-battery-staple", result);
    }

    [Fact]
    public async Task Setting_a_secret_twice_overwrites_the_previous_value()
    {
        await _sut.SetSecretAsync(_handle, "first-value");
        await _sut.SetSecretAsync(_handle, "second-value");

        var result = await _sut.GetSecretAsync(_handle);

        Assert.Equal("second-value", result);
    }

    [Fact]
    public async Task Deleting_a_secret_makes_it_unretrievable()
    {
        await _sut.SetSecretAsync(_handle, "temporary-value");
        await _sut.DeleteSecretAsync(_handle);

        var result = await _sut.GetSecretAsync(_handle);

        Assert.Null(result);
    }

    [Fact]
    public async Task Deleting_a_secret_that_was_never_set_does_not_throw()
    {
        // A missing credential is a normal, expected outcome — not an error.
        var exception = await Record.ExceptionAsync(() => _sut.DeleteSecretAsync(_handle));

        Assert.Null(exception);
    }

    [Fact]
    public async Task A_secret_containing_unicode_characters_round_trips_correctly()
    {
        const string value = "pässwörd-with-ünïcode-日本語-🔒";
        await _sut.SetSecretAsync(_handle, value);

        var result = await _sut.GetSecretAsync(_handle);

        Assert.Equal(value, result);
    }
}
