using System;
using System.IO;
using System.Security;

namespace LenovoLegionToolkit.Lib.Utils;

public static class Folders
{
    public static string AppDataOverrideEnvironmentVariable => string.Concat("UDT", "_APPDATA", "_OVERRIDE");

    public static string Program => AppDomain.CurrentDomain.SetupInformation.ApplicationBase ?? string.Empty;
    public static string LegacyAppData => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppIdentity.LegacyCompactName);

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

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folderPath = Path.Combine(appData, AppIdentity.CompactName);
            var legacyFolderPath = Path.Combine(appData, AppIdentity.LegacyCompactName);

            if (Directory.Exists(legacyFolderPath))
                TryCopyMissingDirectoryEntries(legacyFolderPath, folderPath);

            Directory.CreateDirectory(folderPath);
            return folderPath;
        }
    }

    public static string GetAppDataSubdirectory(string subdirectory)
    {
        var targetDirectory = Path.Combine(AppData, subdirectory);
        MigrateLegacyAppDataSubdirectory(subdirectory, targetDirectory);
        Directory.CreateDirectory(targetDirectory);
        return targetDirectory;
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
        var legacyRoots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppIdentity.LegacyCompactName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppIdentity.LegacyCompactName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppIdentity.CompactName),
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
            Log.Instance.Warning(
                $"Failed to copy missing directory entries from \"{sourceDirectory}\" to \"{destinationDirectory}\": {ex.Message}",
                ex);

            try
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            catch (Exception createEx) when (createEx is IOException or UnauthorizedAccessException or SecurityException or DirectoryNotFoundException)
            {
                Log.Instance.Warning(
                    $"Failed to ensure destination directory \"{destinationDirectory}\": {createEx.Message}",
                    createEx);
            }
        }
    }
}
