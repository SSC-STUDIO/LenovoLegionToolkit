using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Verifies that a PE file's Authenticode signature covers the file bytes
/// (WinVerifyTrust), not merely that a certificate blob can be extracted.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class AuthenticodeVerifier
{
    private const uint WTD_UI_NONE = 2;
    private const uint WTD_REVOKE_NONE = 0;
    private const uint WTD_CHOICE_FILE = 1;
    private const uint WTD_STATEACTION_VERIFY = 1;
    private const uint WTD_STATEACTION_CLOSE = 2;
    private const uint WTD_DISABLE_MD2_MD4 = 0x2000;
    private const uint WTD_REVOCATION_CHECK_NONE = 0x10;

    // WINTRUST_ACTION_GENERIC_VERIFY_V2
    private static readonly Guid GenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    /// <summary>
    /// Returns true when WinVerifyTrust accepts the file signature.
    /// Returns false when the signature is missing, invalid, or does not match content.
    /// </summary>
    public static bool TryVerifyFile(string filePath, out int trustStatus)
    {
        trustStatus = unchecked((int)0x800B0100); // TRUST_E_NOSIGNATURE default

        if (string.IsNullOrWhiteSpace(filePath) || !global::System.IO.File.Exists(filePath))
            return false;

        var fileInfo = new WINTRUST_FILE_INFO
        {
            cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
            pcwszFilePath = filePath,
            hFile = IntPtr.Zero,
            pgKnownSubject = IntPtr.Zero
        };

        var fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(fileInfo));
        try
        {
            Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

            var data = new WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = WTD_UI_NONE,
                fdwRevocationChecks = WTD_REVOKE_NONE,
                dwUnionChoice = WTD_CHOICE_FILE,
                pFile = fileInfoPtr,
                dwStateAction = WTD_STATEACTION_VERIFY,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = IntPtr.Zero,
                dwProvFlags = WTD_DISABLE_MD2_MD4 | WTD_REVOCATION_CHECK_NONE,
                dwUIContext = 0,
                pSignatureSettings = IntPtr.Zero
            };

            var action = GenericVerifyV2;
            trustStatus = WinVerifyTrust(IntPtr.Zero, ref action, ref data);

            // Release provider state
            data.dwStateAction = WTD_STATEACTION_CLOSE;
            _ = WinVerifyTrust(IntPtr.Zero, ref action, ref data);

            return trustStatus == 0;
        }
        finally
        {
            Marshal.DestroyStructure<WINTRUST_FILE_INFO>(fileInfoPtr);
            Marshal.FreeHGlobal(fileInfoPtr);
        }
    }

    [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid pgActionID, ref WINTRUST_DATA pWVTData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINTRUST_DATA
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
