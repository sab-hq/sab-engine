using SabEngine.Execution;
using Xunit;

namespace SabEngine.Execution.Tests;

/// <summary>
/// Verifies PowerShellExecutor (pre-development-checklist.md, PD-7)
/// against a real, local PowerShell engine — not a mock. There's no
/// meaningful way to fake "does this actually run PowerShell correctly"
/// the way we faked the LLM in SabAgent.Tests; the whole point of this
/// component is real interop, so these tests genuinely exercise it.
/// </summary>
public class PowerShellExecutorTests
{
    [Fact]
    public async Task A_simple_script_returns_its_output()
    {
        var sut = new PowerShellExecutor();

        var result = await sut.RunScriptAsync("Write-Output 'hello from powershell'");

        Assert.True(result.Succeeded);
        Assert.Contains("hello from powershell", result.Output);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Parameters_are_passed_into_a_script_with_a_matching_param_block()
    {
        var sut = new PowerShellExecutor();

        var result = await sut.RunScriptAsync(
            "param($Name) Write-Output \"hello, $Name\"",
            new Dictionary<string, object?> { ["Name"] = "sab-engine" });

        Assert.True(result.Succeeded);
        Assert.Contains("hello, sab-engine", result.Output);
    }

    [Fact]
    public async Task A_script_that_writes_to_the_error_stream_is_reported_as_failed()
    {
        var sut = new PowerShellExecutor();

        var result = await sut.RunScriptAsync("Write-Error 'something went wrong'");

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("something went wrong", result.Errors[0]);
    }

    [Fact]
    public async Task A_script_that_throws_is_reported_as_failed_with_the_exception_message()
    {
        var sut = new PowerShellExecutor();

        var result = await sut.RunScriptAsync("throw 'deliberate test failure'");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("deliberate test failure"));
    }

    [Fact]
    public async Task Multiple_output_objects_all_come_back_in_order()
    {
        var sut = new PowerShellExecutor();

        var result = await sut.RunScriptAsync("1..3 | ForEach-Object { \"item-$_\" }");

        Assert.True(result.Succeeded);
        Assert.Equal(["item-1", "item-2", "item-3"], result.Output);
    }
}
