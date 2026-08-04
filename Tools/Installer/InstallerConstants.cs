using System;
using System.IO;

namespace UniversalDeviceToolkit.Installer;

/// <summary>
/// Single source of truth for installer metadata. Values mirror the legacy
/// Inno script (MakeInstaller.iss) so existing installs are recognized and
/// upgraded cleanly.
/// </summary>
internal static class InstallerConstants
{
    public const string AppName = "Universal Device Toolkit";
    public const string AppNameCompact = "UniversalDeviceToolkit";
    public const string Publisher = "SSC-STUDIO";
    public const string AppUrl = "https://github.com/SSC-STUDIO/UniversalDeviceToolkit";
    public const string MainExeName = "Universal Device Toolkit.exe";
    public const string UninstallerExeName = "Uninstall.exe";

    /// <summary>Legacy Inno AppId — used to detect and supersede Inno-based installs.</summary>
    public const string LegacyInnoAppId = "{0C37B9AC-9C3D-4302-8ABB-125C7C7D83D5}";
    public const string LegacyInnoUninstallKeyName = LegacyInnoAppId + "_is1";

    /// <summary>Our own per-user uninstall registry key name.</summary>
    public const string UninstallKeyName = "UniversalDeviceToolkit";

    public const string AutorunTaskNameNew = "UniversalDeviceToolkit_Autorun_6efcc882-924c-4cbc-8fec-f45c25696f98";
    public const string AutorunTaskNameLegacy = "LenovoLegionToolkit_Autorun_6efcc882-924c-4cbc-8fec-f45c25696f98";

    public const string DotNetRuntimeName = "Microsoft.WindowsDesktop.App";
    public const int DotNetRuntimeMajor = 10;
    public static readonly Version DotNetRuntimeMinimum = new(10, 0, 7);
    public const string DotNetRuntimeInstallerUrl =
        "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.7/windowsdesktop-runtime-10.0.7-win-x64.exe";
    // SHA-512 published in the official .NET 10.0.7 Windows Desktop Runtime release metadata.
    public const string DotNetRuntimeInstallerSha512 =
        "6473d5b01e93d12720c3befa5ee52c61b189760db5c029182838a8c24daf8e03b33787de6ddac7c4c9f97c08c65c528bf9c366ec9c401ae5462824c4f5e70604";
    public const string DotNetRuntimeInstallerArgs = "/install /repair /passive /norestart";

    public static string DefaultInstallDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        AppNameCompact);

    public static string AppDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppNameCompact);

    public static string StartMenuShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", AppName + ".lnk");

    public static string DesktopShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), AppName + ".lnk");
}
