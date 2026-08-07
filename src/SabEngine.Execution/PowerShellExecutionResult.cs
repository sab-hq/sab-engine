namespace SabEngine.Execution;

/// <summary>
/// The structured result of running a PowerShell script or command via
/// <see cref="PowerShellExecutor"/>. See docs/design/SAB_Design_Document_v0.1.2.md,
/// Section 4.4 — this is the interop primitive the WinRM connector
/// (not yet built, PD-17–PD-20) will eventually use to actually carry
/// out a module's work against a real target.
/// </summary>
public sealed record PowerShellExecutionResult(
    bool Succeeded,
    IReadOnlyList<string> Output,
    IReadOnlyList<string> Errors);
