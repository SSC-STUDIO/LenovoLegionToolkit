using System;
using System.IO;

namespace LenovoLegionToolkit.Lib.Utils;

/// <summary>
/// Validates installer launch paths before elevation/execution.
/// </summary>
public static class InstallerLaunchPathValidator
{
    public static bool TryValidateForExecution(
        string? installerPath,
        string? downloadDirectory,
        string? expectedFileName,
        out string normalizedInstallerPath,
        out string failureReason)
    {
        normalizedInstallerPath = string.Empty;
        failureReason = string.Empty;

        if (string.IsNullOrWhiteSpace(installerPath))
        {
            failureReason = "Installer path is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(downloadDirectory))
        {
            failureReason = "Download directory is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(expectedFileName))
        {
            failureReason = "Expected installer file name is empty.";
            return false;
        }

        string normalizedDownloadDirectory;
        try
        {
            normalizedInstallerPath = Path.GetFullPath(installerPath);
            normalizedDownloadDirectory = Path.GetFullPath(downloadDirectory);
        }
        catch (Exception ex)
        {
            normalizedInstallerPath = string.Empty;
            failureReason = $"Invalid installer path: {ex.Message}";
            return false;
        }

        if (!File.Exists(normalizedInstallerPath))
        {
            normalizedInstallerPath = string.Empty;
            failureReason = "Installer file does not exist.";
            return false;
        }

        var normalizedDownloadPrefix = EnsureTrailingSeparator(normalizedDownloadDirectory);
        if (!normalizedInstallerPath.StartsWith(normalizedDownloadPrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalizedInstallerPath = string.Empty;
            failureReason = "Installer path is outside the configured download directory.";
            return false;
        }

        var actualFileName = Path.GetFileName(normalizedInstallerPath);
        if (!string.Equals(actualFileName, expectedFileName, StringComparison.OrdinalIgnoreCase))
        {
            normalizedInstallerPath = string.Empty;
            failureReason = $"Unexpected installer file name: {actualFileName}.";
            return false;
        }

        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(normalizedInstallerPath);
        }
        catch (Exception ex)
        {
            normalizedInstallerPath = string.Empty;
            failureReason = $"Failed to inspect installer file attributes: {ex.Message}";
            return false;
        }

        if ((attributes & FileAttributes.Directory) != 0)
        {
            normalizedInstallerPath = string.Empty;
            failureReason = "Installer path points to a directory.";
            return false;
        }

        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            normalizedInstallerPath = string.Empty;
            failureReason = "Installer path points to a reparse point.";
            return false;
        }

        return true;
    }

    private static string EnsureTrailingSeparator(string directoryPath)
    {
        if (directoryPath.EndsWith(Path.DirectorySeparatorChar) || directoryPath.EndsWith(Path.AltDirectorySeparatorChar))
            return directoryPath;

        return directoryPath + Path.DirectorySeparatorChar;
    }
}
