using System;
using System.IO;
using System.Security;
using UniversalDeviceToolkit.Shared.Logging;

namespace UniversalDeviceToolkit.Shared.Utils;

/// <summary>
/// Cross-platform application folder system.
/// Extracted from Lib.Utils.Folders with platform-conditional legacy migration.
/// On Linux/macOS the config home is <c>~/.config/udt/</c>;
/// on Windows it remains <c>%LOCALAPPDATA%\UniversalDeviceToolkit</c>.
/// </summary>
public static class Folders
{
    public static string AppDataOverrideEnvironmentVariable => string.Concat("UDT", "_APPDATA", "_OVERRIDE");
    private const string LegacyMigrationMarkerFileName = ".legacy-appdata-migrated";

    public static string Program => AppDomain.CurrentDomain.SetupInformation.ApplicationBase ?? string.Empty;

    /// <summary>
    /// Legacy AppData folder (LenovoLegionToolkit). Only meaningful on Windows where
    /// the original LLT installation stored its data. Returns empty string on other platforms.
    /// </summary>
    public static string LegacyAppData
    {
        get
        {
            if (!OperatingSystem.IsWindows())
                return string.Empty;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppIdentity.LegacyCompactName);
        }
    }

    public static string AppData
    {
        get
        {
#if UDT_TEST_HOOKS
            var overridePath = Environment.GetEnvironmentVariable(AppDataOverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                var fullOverridePath = Path.GetFullPath(overridePath);
                Directory.CreateDirectory(fullOverridePath);
                return fullOverridePath;
            }
#endif

            var folderPath = Path.Combine(ConfigHome, AppIdentity.CompactName);
            Directory.CreateDirectory(folderPath);

            // Legacy migration is Windows-only — LLT was a Windows-only application.
            if (OperatingSystem.IsWindows())
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var legacyFolderPath = Path.Combine(appData, AppIdentity.LegacyCompactName);

                var markerPath = Path.Combine(folderPath, LegacyMigrationMarkerFileName);
                if (Directory.Exists(legacyFolderPath) && !File.Exists(markerPath))
                {
                    TryCopyMissingDirectoryEntries(legacyFolderPath, folderPath);
                    TryWriteMigrationMarker(markerPath);
                }
            }

            return folderPath;
        }
    }

    /// <summary>
    /// Platform-appropriate configuration home directory.
    /// On Linux/macOS: <c>$XDG_CONFIG_HOME</c> or <c>~/.config</c>.
    /// On Windows: <c>%LOCALAPPDATA%</c>.
    /// </summary>
    private static string ConfigHome
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // XDG Base Directory Specification: $XDG_CONFIG_HOME, fallback to ~/.config
            var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrWhiteSpace(xdgConfigHome))
                return xdgConfigHome;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".config");
        }
    }

    public static string GetAppDataSubdirectory(string subdirectory)
    {
        var targetDirectory = Path.Combine(AppData, subdirectory);
        Directory.CreateDirectory(targetDirectory);

        // Legacy subdirectory migration is Windows-only.
        if (!OperatingSystem.IsWindows())
            return targetDirectory;

        var markerPath = Path.Combine(targetDirectory, LegacyMigrationMarkerFileName);
        if (!File.Exists(markerPath))
        {
            MigrateLegacyAppDataSubdirectory(subdirectory, targetDirectory);
            TryWriteMigrationMarker(markerPath);
        }

        return targetDirectory;
    }

    private static void TryWriteMigrationMarker(string markerPath)
    {
        try
        {
            File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            SharedLog.Warning($"Failed to write legacy AppData migration marker \"{markerPath}\": {ex.Message}", ex);
        }
    }

    public static string Temp
    {
        get
        {
            var appData = Path.GetTempPath();
            var folderPath = Path.Combine(appData, AppIdentity.CompactName);
            Directory.CreateDirectory(folderPath);
            return folderPath;
        }
    }

    private static void MigrateLegacyAppDataSubdirectory(string subdirectory, string targetDirectory)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var legacyRoots = new[]
        {
            Path.Combine(localAppData, AppIdentity.LegacyCompactName),
            Path.Combine(roamingAppData, AppIdentity.LegacyCompactName),
            Path.Combine(localAppData, AppIdentity.CompactName),
        };

        foreach (var legacyRoot in legacyRoots)
        {
            var legacySubdirectory = Path.Combine(legacyRoot, subdirectory);
            if (Directory.Exists(legacySubdirectory))
                TryCopyMissingDirectoryEntries(legacySubdirectory, targetDirectory);
        }
    }

    internal static void TryCopyMissingDirectoryEntries(string sourceDirectory, string destinationDirectory)
    {
        try
        {
            Directory.CreateDirectory(destinationDirectory);

            foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDirectory, directory);
                Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
            }

            foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDirectory, file);
                var destinationPath = Path.Combine(destinationDirectory, relativePath);
                if (File.Exists(destinationPath))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(file, destinationPath, false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or DirectoryNotFoundException)
        {
            SharedLog.Warning(
                $"Failed to copy missing directory entries from \"{sourceDirectory}\" to \"{destinationDirectory}\": {ex.Message}",
                ex);

            try
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            catch (Exception createEx) when (createEx is IOException or UnauthorizedAccessException or SecurityException or DirectoryNotFoundException)
            {
                SharedLog.Warning(
                    $"Failed to ensure destination directory \"{destinationDirectory}\": {createEx.Message}",
                    createEx);
            }
        }
    }
}
