using System;
using System.IO;
using System.Linq;

namespace UniversalDeviceToolkit.Shared.Utils;

/// <summary>
/// Cross-platform path security utilities.
/// Extracted from Lib.Utils.PathSecurity — Windows-specific driver/registry validators omitted.
/// </summary>
public static class PathSecurity
{
    // Windows reserved device names that could be used for attacks
    private static readonly string[] ReservedDeviceNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];

    // Dangerous path patterns that indicate path traversal attempts
    private static readonly string[] DangerousPathPatterns =
    [
        "..", "~", "%", "$", "@", "|", ">", "<", "*", "?", "\"", "'", "\0"
    ];

    // Invalid file name characters (cross-platform superset)
    private static readonly char[] InvalidFileNameChars =
    [
        '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0'
    ];

    /// <summary>
    /// Validates that a file name does not contain path traversal or other dangerous patterns.
    /// </summary>
    public static bool IsValidFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (fileName.Contains('/') || fileName.Contains('\\'))
            return false;

        foreach (var pattern in DangerousPathPatterns)
        {
            if (fileName.Contains(pattern))
                return false;
        }

        if (fileName.IndexOfAny(InvalidFileNameChars) >= 0)
            return false;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(nameWithoutExt))
            return false;

        if (ReservedDeviceNames.Any(r => nameWithoutExt.Equals(r, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (fileName.EndsWith(".", StringComparison.Ordinal) || fileName.EndsWith(" ", StringComparison.Ordinal))
            return false;

        return true;
    }

    /// <summary>
    /// Validates a full path to ensure it doesn't escape the allowed base directory.
    /// </summary>
    public static bool IsPathWithinAllowedDirectory(string? path, string? basePath, bool allowNonExistent = true)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(basePath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var fullBasePath = Path.GetFullPath(basePath);

            if (!fullBasePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) &&
                !fullBasePath.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fullBasePath += Path.DirectorySeparatorChar;
            }

            if (!fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
                return false;

            if (fullPath.Contains(".." + Path.DirectorySeparatorChar) ||
                fullPath.Contains(".." + Path.AltDirectorySeparatorChar))
                return false;

            if (!allowNonExistent)
            {
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                    return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return false;
        }
    }
}
