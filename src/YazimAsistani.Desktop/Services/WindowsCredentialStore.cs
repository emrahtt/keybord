using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace YazimAsistani.Desktop.Services;

public sealed class WindowsCredentialStore
{
    public const string GeminiApiKeyTarget = "YazimAsistani/GeminiApiKey";

    private const uint GenericCredential = 1;
    private const uint PersistLocalMachine = 2;

    public bool Contains(string targetName)
    {
        if (CredRead(targetName, GenericCredential, 0, out var credentialPointer))
        {
            CredFree(credentialPointer);
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        return error switch
        {
            1168 => false,
            _ => throw new Win32Exception(error)
        };
    }

    public string? Read(string targetName)
    {
        if (!CredRead(targetName, GenericCredential, 0, out var credentialPointer))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == 1168)
            {
                return null;
            }

            throw new Win32Exception(error);
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(credentialPointer);
            return credential.CredentialBlob == 0 || credential.CredentialBlobSize == 0
                ? string.Empty
                : Marshal.PtrToStringUni(
                    credential.CredentialBlob,
                    checked((int)credential.CredentialBlobSize / sizeof(char)));
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    public void Write(string targetName, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("API anahtarı boş olamaz.", nameof(secret));
        }

        var bytes = Encoding.Unicode.GetBytes(secret);
        var blobPointer = Marshal.AllocCoTaskMem(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blobPointer, bytes.Length);
            var credential = new Credential
            {
                Type = GenericCredential,
                TargetName = targetName,
                CredentialBlobSize = checked((uint)bytes.Length),
                CredentialBlob = blobPointer,
                Persist = PersistLocalMachine,
                UserName = Environment.UserName
            };

            if (!CredWrite(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(blobPointer);
            Array.Clear(bytes);
        }
    }

    public void Delete(string targetName)
    {
        if (!CredDelete(targetName, GenericCredential, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 1168)
            {
                throw new Win32Exception(error);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public nint CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public nint Attributes;
        public string? TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref Credential credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint flags, out nint credentialPointer);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(nint buffer);
}

