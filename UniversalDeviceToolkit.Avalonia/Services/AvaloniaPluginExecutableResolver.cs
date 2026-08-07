using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Host-neutral port of the WPF PluginExecutableResolver. Resolves the launch
/// executable inside a plugin install directory (preferring the conventional
/// assembly names) and verifies the Authenticode signature through the Windows
/// WinVerifyTrust API. DEBUG builds bypass verification exactly like the WPF
/// launch path does; non-Windows hosts have no native trust store and allow.
/// </summary>
public static class AvaloniaPluginExecutableResolver
{
    public static string? ResolveExecutablePath(string? pluginInstallDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginInstallDirectory)
            || !Directory.Exists(pluginInstallDirectory))
            return null;

        try
        {
            var trimmed = pluginInstallDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            var folderName = Path.GetFileName(trimmed);
            return PickExecutable(
                Directory.GetFiles(pluginInstallDirectory, "*.exe", SearchOption.TopDirectoryOnly),
                folderName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Picks the preferred executable from the given file paths. Conventionally
    /// named executables (folder name, UniversalDeviceToolkit.Plugins.* and
    /// LenovoLegionToolkit.Plugins.* variants, with and without dashes) win over
    /// an arbitrary first match, mirroring the WPF candidate ordering.
    /// </summary>
    public static string? PickExecutable(IEnumerable<string> executableFiles, string? pluginFolderName)
    {
        if (executableFiles is null)
            return null;

        var files = executableFiles
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (files.Length == 0)
            return null;

        var folderName = pluginFolderName?.Trim().TrimEnd('\\', '/');
        if (!string.IsNullOrWhiteSpace(folderName))
        {
            foreach (var preferredName in GetPreferredExecutableNames(folderName))
            {
                var match = files.FirstOrDefault(path => string.Equals(
                    Path.GetFileNameWithoutExtension(path),
                    preferredName,
                    StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    return match;
            }
        }

        return files[0];
    }

    public static bool IsSignatureValid(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

#if DEBUG
        // Matches the WPF launch path: unsigned executables are allowed in DEBUG.
        return true;
#else
        return VerifyAuthenticode(path);
#endif
    }

    private static IEnumerable<string> GetPreferredExecutableNames(string pluginFolderName)
    {
        yield return pluginFolderName;
        yield return $"UniversalDeviceToolkit.Plugins.{pluginFolderName}";
        yield return $"LenovoLegionToolkit.Plugins.{pluginFolderName}";
        yield return $"UniversalDeviceToolkit.Plugins.{pluginFolderName.Replace("-", string.Empty)}";
        yield return $"LenovoLegionToolkit.Plugins.{pluginFolderName.Replace("-", string.Empty)}";
    }

#if WINDOWS
    private static bool VerifyAuthenticode(string path)
    {
        try
        {
            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                pcwszFilePath = path,
            };
            var data = new WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                dwUIChoice = WTD_UI_NONE,
                fdwRevocationChecks = WTD_REVOKE_NONE,
                dwUnionChoice = WTD_CHOICE_FILE,
                dwStateAction = WTD_STATEACTION_VERIFY,
            };

            var fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
            var dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_DATA>());
            try
            {
                var action = WinTrustActionGenericVerifyV2;
                Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);
                data.pFile = fileInfoPtr;
                Marshal.StructureToPtr(data, dataPtr, false);
                var status = WinVerifyTrust(IntPtr.Zero, ref action, dataPtr);

                data.dwStateAction = WTD_STATEACTION_CLOSE;
                Marshal.StructureToPtr(data, dataPtr, false);
                WinVerifyTrust(IntPtr.Zero, ref action, dataPtr);

                return status == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
                Marshal.FreeHGlobal(fileInfoPtr);
            }
        }
        catch
        {
            return false;
        }
    }

    private const uint WTD_UI_NONE = 2;
    private const uint WTD_REVOKE_NONE = 0;
    private const uint WTD_CHOICE_FILE = 1;
    private const uint WTD_STATEACTION_VERIFY = 1;
    private const uint WTD_STATEACTION_CLOSE = 2;

    private static readonly Guid WinTrustActionGenericVerifyV2 =
        new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    [DllImport("wintrust.dll", SetLastError = true)]
    private static extern uint WinVerifyTrust(IntPtr hwnd, ref Guid actionId, IntPtr winTrustData);

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
#else
    private static bool VerifyAuthenticode(string path) => true;
#endif
}
