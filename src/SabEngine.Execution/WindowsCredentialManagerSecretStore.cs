using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using SabEngine.Core;

namespace SabEngine.Execution;

/// <summary>
/// The Phase 1 default <see cref="ISecretStore"/> — Windows Credential
/// Manager, per docs/design/SAB_Design_Document_v0.1.2.md, Section 7 and
/// SE-1 (confirmed). There's no managed .NET wrapper for Windows
/// Credential Manager, so this talks to it directly via P/Invoke into
/// advapi32.dll (CredWrite/CredRead/CredDelete/CredFree) — a
/// well-documented, stable Win32 API, but genuinely the highest-risk
/// code in this project so far: struct marshaling bugs don't show up as
/// compile errors, only as runtime failures or (worse) silent
/// corruption. See pre-development-checklist.md, PD-9.
///
/// Every handle is namespaced under a "SabEngine:" prefix in Credential
/// Manager's target-name space, so SAB's own secrets are clearly
/// distinguishable from unrelated saved credentials on the same
/// machine.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsCredentialManagerSecretStore : ISecretStore
{
    private const uint CredTypeGeneric = 1;
    private const uint CredPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public Task<string?> GetSecretAsync(string handle, CancellationToken cancellationToken = default)
    {
        if (!NativeMethods.CredRead(BuildTargetName(handle), CredTypeGeneric, 0, out var credentialPtr))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == ErrorNotFound)
            {
                return Task.FromResult<string?>(null);
            }

            throw new InvalidOperationException($"Failed to read the secret for handle '{handle}' (Win32 error {error}).");
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeMethods.CREDENTIAL>(credentialPtr);
            var bytes = new byte[credential.CredentialBlobSize];
            if (bytes.Length > 0)
            {
                Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            }

            return Task.FromResult<string?>(Encoding.Unicode.GetString(bytes));
        }
        finally
        {
            NativeMethods.CredFree(credentialPtr);
        }
    }

    public Task SetSecretAsync(string handle, string secretValue, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.Unicode.GetBytes(secretValue);
        var blobPtr = Marshal.AllocHGlobal(bytes.Length);

        try
        {
            Marshal.Copy(bytes, 0, blobPtr, bytes.Length);

            var credential = new NativeMethods.CREDENTIAL
            {
                Type = CredTypeGeneric,
                TargetName = BuildTargetName(handle),
                Comment = "SAB Engine secret — see pre-development-checklist.md, PD-9",
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = blobPtr,
                Persist = CredPersistLocalMachine,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                TargetAlias = null,
                UserName = "sab-engine",
            };

            if (!NativeMethods.CredWrite(ref credential, 0))
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to write the secret for handle '{handle}' (Win32 error {error}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
        }

        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string handle, CancellationToken cancellationToken = default)
    {
        // A missing credential is a normal, expected outcome here, not
        // an error — CredDelete returning false for "not found" is
        // treated the same as success.
        if (!NativeMethods.CredDelete(BuildTargetName(handle), CredTypeGeneric, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != ErrorNotFound)
            {
                throw new InvalidOperationException($"Failed to delete the secret for handle '{handle}' (Win32 error {error}).");
            }
        }

        return Task.CompletedTask;
    }

    private static string BuildTargetName(string handle) => $"SabEngine:{handle}";

    /// <summary>Raw Win32 P/Invoke declarations, isolated in one place.</summary>
    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string? Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string? TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredWrite(ref CREDENTIAL userCredential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        public static extern void CredFree(IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredDelete(string target, uint type, uint flags);
    }
}
