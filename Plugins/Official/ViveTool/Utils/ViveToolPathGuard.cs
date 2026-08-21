using System;
using System.IO;
using System.IO.Compression;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Utils;

internal static class ViveToolPathGuard
{
    public static bool ContainsUnsafePathSegment(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        if (path.Contains('\0', StringComparison.Ordinal))
        {
            return true;
        }

        if (path.Contains("..", StringComparison.Ordinal))
        {
            return true;
        }

        if (path.StartsWith(@"\\.\", StringComparison.Ordinal) ||
            path.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            path.StartsWith("//./", StringComparison.Ordinal) ||
            path.StartsWith("//?/", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    public static bool TryNormalizeUserFilePath(string? path, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (ContainsUnsafePathSegment(path))
        {
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(path!);
            if (string.IsNullOrWhiteSpace(fullPath) || ContainsUnsafePathSegment(fullPath))
            {
                return false;
            }

            var fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains(':', StringComparison.Ordinal))
            {
                return false;
            }

            if (IsUnderWindowsDirectory(fullPath))
            {
                return false;
            }

            normalizedPath = fullPath;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or IOException)
        {
            return false;
        }
    }

    public static bool TryNormalizeExecutablePath(string? path, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (ContainsUnsafePathSegment(path))
        {
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(path!);
            if (string.IsNullOrWhiteSpace(fullPath) || ContainsUnsafePathSegment(fullPath))
            {
                return false;
            }

            var fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains(':', StringComparison.Ordinal))
            {
                return false;
            }

            normalizedPath = fullPath;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or IOException)
        {
            return false;
        }
    }

    public static bool TryGetSafeZipDestination(ZipArchiveEntry entry, string stagingDirectory, out string destinationPath)
    {
        destinationPath = string.Empty;
        if (string.IsNullOrWhiteSpace(stagingDirectory) || string.IsNullOrWhiteSpace(entry.Name))
        {
            return false;
        }

        if (!IsSafeZipEntryName(entry.Name) || !IsSafeZipFullName(entry.FullName))
        {
            return false;
        }

        try
        {
            var stagingFullPath = Path.GetFullPath(stagingDirectory);
            if (!stagingFullPath.EndsWith(Path.DirectorySeparatorChar))
            {
                stagingFullPath += Path.DirectorySeparatorChar;
            }

            var candidate = Path.GetFullPath(Path.Combine(stagingDirectory, entry.Name));
            if (!candidate.StartsWith(stagingFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            destinationPath = candidate;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or IOException)
        {
            return false;
        }
    }

    private static bool IsSafeZipEntryName(string name)
    {
        if (name.Contains("..", StringComparison.Ordinal) ||
            name.Contains('/', StringComparison.Ordinal) ||
            name.Contains('\\', StringComparison.Ordinal) ||
            name.Contains('\0', StringComparison.Ordinal) ||
            name.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
        return !string.IsNullOrWhiteSpace(nameWithoutExtension);
    }

    private static bool IsSafeZipFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return false;
        }

        if (fullName.Contains("..", StringComparison.Ordinal) ||
            fullName.Contains('\0', StringComparison.Ordinal) ||
            Path.IsPathRooted(fullName.Replace('/', Path.DirectorySeparatorChar)))
        {
            return false;
        }

        return true;
    }

    private static bool IsUnderWindowsDirectory(string fullPath)
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (string.IsNullOrWhiteSpace(windowsDirectory))
        {
            return false;
        }

        var windowsPrefix = Path.GetFullPath(windowsDirectory);
        if (!windowsPrefix.EndsWith(Path.DirectorySeparatorChar) &&
            !windowsPrefix.EndsWith(Path.AltDirectorySeparatorChar))
        {
            windowsPrefix += Path.DirectorySeparatorChar;
        }

        return fullPath.StartsWith(windowsPrefix, StringComparison.OrdinalIgnoreCase) ||
               fullPath.Equals(windowsPrefix.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }
}
