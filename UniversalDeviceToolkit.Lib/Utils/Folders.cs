using System;
using System.IO;

namespace LenovoLegionToolkit.Lib.Utils;

public static class Folders
{
    public const string AppDataOverrideEnvironmentVariable = "UDT_APPDATA_OVERRIDE";

    public static string Program => AppDomain.CurrentDomain.SetupInformation.ApplicationBase ?? string.Empty;
    public static string LegacyAppData => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppIdentity.LegacyCompactName);

    public static string AppData
    {
        get
        {
            var overridePath = Environment.GetEnvironmentVariable(AppDataOverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                var fullOverridePath = Path.GetFullPath(overridePath);
                Directory.CreateDirectory(fullOverridePath);
                return fullOverridePath;
            }

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
        catch
        {
            Directory.CreateDirectory(destinationDirectory);
        }
    }
}
