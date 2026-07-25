using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Win11Tweaker.App.Interop;

public static class SignatureVerifier
{
    static readonly Guid GenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    public static (bool Trusted, string? Signer) Check(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return (false, null);

        bool trusted;
        try { trusted = Verify(path); }
        catch (Exception) { return (false, null); }

        if (!trusted)
            return (false, null);

        try
        {
#pragma warning disable SYSLIB0057
            using var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
            var signer = cert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
            return (true, string.IsNullOrWhiteSpace(signer) ? null : signer);
        }
        catch (Exception)
        {
            return (true, null);
        }
    }

    static bool Verify(string path)
    {
        var fileInfo = new WintrustFileInfo
        {
            cbStruct = (uint)Marshal.SizeOf<WintrustFileInfo>(),
            pcwszFilePath = path
        };

        var pFile = Marshal.AllocHGlobal(Marshal.SizeOf<WintrustFileInfo>());
        try
        {
            Marshal.StructureToPtr(fileInfo, pFile, fDeleteOld: false);

            var data = new WintrustData
            {
                cbStruct = (uint)Marshal.SizeOf<WintrustData>(),
                dwUIChoice = 2,
                fdwRevocationChecks = 0,
                dwUnionChoice = 1,
                pFile = pFile,
                dwStateAction = 1,
                dwProvFlags = 0x00000010 | 0x00001000
            };

            var action = GenericVerifyV2;
            var result = WinVerifyTrust(InvalidHandle, ref action, ref data);

            data.dwStateAction = 2;
            WinVerifyTrust(InvalidHandle, ref action, ref data);

            return result == 0;
        }
        finally
        {
            Marshal.FreeHGlobal(pFile);
        }
    }

    static readonly IntPtr InvalidHandle = new(-1);

    [DllImport("wintrust.dll", ExactSpelling = true)]
    static extern int WinVerifyTrust(IntPtr hwnd, ref Guid actionId, ref WintrustData data);

    [StructLayout(LayoutKind.Sequential)]
    struct WintrustFileInfo
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)] public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WintrustData
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }
}
