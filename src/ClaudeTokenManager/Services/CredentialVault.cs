using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ClaudeTokenManager.Services;

// Generic credentials in Windows Credential Manager, scoped to the current user.
public static class CredentialVault
{
    private const string Prefix = "ClaudeTokenManager:";
    private const int CRED_TYPE_GENERIC = 1;
    private const int CRED_PERSIST_LOCAL_MACHINE = 2;
    private const int ERROR_NOT_FOUND = 1168;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref CREDENTIAL credential, int flags);

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int flags, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, int type, int flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    public static void Save(string profileId, string profileName, string token)
    {
        var bytes = Encoding.Unicode.GetBytes(token);
        var blob = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);
            var cred = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = Prefix + profileId,
                Comment = profileName,
                CredentialBlobSize = bytes.Length,
                CredentialBlob = blob,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = profileName,
            };
            if (!CredWrite(ref cred, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            // Don't leave copies of the token in freed memory.
            Marshal.Copy(new byte[bytes.Length], 0, blob, bytes.Length);
            Marshal.FreeHGlobal(blob);
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public static string? Read(string profileId)
    {
        if (!CredRead(Prefix + profileId, CRED_TYPE_GENERIC, 0, out var ptr))
        {
            var err = Marshal.GetLastWin32Error();
            if (err == ERROR_NOT_FOUND) return null;
            throw new Win32Exception(err);
        }
        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
            if (cred.CredentialBlobSize == 0) return "";
            return Marshal.PtrToStringUni(cred.CredentialBlob, cred.CredentialBlobSize / 2);
        }
        finally
        {
            CredFree(ptr);
        }
    }

    public static void Delete(string profileId)
    {
        if (!CredDelete(Prefix + profileId, CRED_TYPE_GENERIC, 0))
        {
            var err = Marshal.GetLastWin32Error();
            if (err != ERROR_NOT_FOUND) throw new Win32Exception(err);
        }
    }
}
