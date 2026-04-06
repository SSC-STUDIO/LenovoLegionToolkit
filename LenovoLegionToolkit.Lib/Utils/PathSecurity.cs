using System;
using System.IO;
using System.Linq;

namespace LenovoLegionToolkit.Lib.Utils;

/// <summary>
/// Security utilities for path validation and sanitization to prevent path traversal attacks.
/// </summary>
public static class PathSecurity
{
    // Windows reserved device names that could be used for attacks
    private static readonly string[] ReservedDeviceNames = new[]
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    // Dangerous path patterns that indicate path traversal attempts
    private static readonly string[] DangerousPathPatterns = new[]
    {
        "..",          // Parent directory traversal
        "~",           // Home directory shortcut
        "%",           // Environment variable expansion
        "$",           // Environment variable (Unix)
        "@",           // Special characters
        "|",           // Pipe character
        ">",           // Output redirection
        "<",           // Input redirection
        "*",           // Wildcard
        "?",           // Single char wildcard
        "\"",          // Quote
        "'",           // Single quote
        "\0",          // Null byte
    };

    // Invalid file name characters (Windows)
    private static readonly char[] InvalidFileNameChars = new[]
    {
        '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0'
    };

    /// <summary>
    /// Validates that a file name does not contain path traversal or other dangerous patterns.
    /// This checks only the file name, not the full path.
    /// </summary>
    public static bool IsValidFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Check for directory separators - file name should not contain paths
        if (fileName.Contains('/') || fileName.Contains('\\'))
            return false;

        // Check for dangerous patterns
        foreach (var pattern in DangerousPathPatterns)
        {
            if (fileName.Contains(pattern))
                return false;
        }

        // Check for invalid characters
        if (fileName.IndexOfAny(InvalidFileNameChars) >= 0)
            return false;

        // Check for reserved device names
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(nameWithoutExt))
            return false;

        if (ReservedDeviceNames.Any(r => nameWithoutExt.Equals(r, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Check for trailing dots or spaces (Windows compatibility issue)
        if (fileName.EndsWith(".") || fileName.EndsWith(" "))
            return false;

        return true;
    }

    /// <summary>
    /// Validates a full path to ensure it doesn't escape the allowed base directory.
    /// This is the primary defense against path traversal attacks.
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <param name="basePath">The base directory that the path must be within</param>
    /// <param name="allowNonExistent">If true, allows paths to files/directories that don't exist yet</param>
    public static bool IsPathWithinAllowedDirectory(string? path, string? basePath, bool allowNonExistent = true)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(basePath))
            return false;

        try
        {
            // Normalize both paths to full absolute paths
            var fullPath = Path.GetFullPath(path);
            var fullBasePath = Path.GetFullPath(basePath);

            // Ensure base path ends with separator for proper prefix checking
            if (!fullBasePath.EndsWith(Path.DirectorySeparatorChar.ToString()) && 
                !fullBasePath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                fullBasePath += Path.DirectorySeparatorChar;
            }

            // Check that the full path starts with the base path
            if (!fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
                return false;

            // Additional check: ensure no ".." remains in the normalized path
            if (fullPath.Contains(".." + Path.DirectorySeparatorChar) || 
                fullPath.Contains(".." + Path.AltDirectorySeparatorChar))
                return false;

            // If not allowing non-existent paths, verify the file/directory exists
            if (!allowNonExistent)
            {
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                    return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException || ex is NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Sanitizes a file name by removing or replacing dangerous characters.
    /// </summary>
    public static string SanitizeFileName(string? fileName, string replacement = "_")
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "unnamed";

        // Remove directory separators first
        var sanitized = fileName.Replace('/', replacement).Replace('\\', replacement);

        // Remove dangerous patterns
        foreach (var pattern in DangerousPathPatterns)
        {
            sanitized = sanitized.Replace(pattern, replacement);
        }

        // Remove invalid characters
        foreach (var c in InvalidFileNameChars)
        {
            sanitized = sanitized.Replace(c.ToString(), replacement);
        }

        // Check for reserved device names
        var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
        if (ReservedDeviceNames.Any(r => nameWithoutExt.Equals(r, StringComparison.OrdinalIgnoreCase)))
        {
            sanitized = "_" + sanitized;
        }

        // Trim trailing dots and spaces
        sanitized = sanitized.TrimEnd('.', ' ');

        // Ensure not empty
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "unnamed";

        return sanitized;
    }

    /// <summary>
    /// Creates a safe file path by combining a base directory with a file name,
    /// ensuring the result stays within the base directory.
    /// </summary>
    public static string? CreateSafeFilePath(string baseDirectory, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory) || string.IsNullOrWhiteSpace(fileName))
            return null;

        // Sanitize the file name first
        var sanitizedFileName = SanitizeFileName(fileName);

        // Combine paths
        var fullPath = Path.Combine(baseDirectory, sanitizedFileName);

        // Validate the result is within the base directory
        if (!IsPathWithinAllowedDirectory(fullPath, baseDirectory))
            return null;

        return fullPath;
    }

    /// <summary>
    /// Validates a plugin ID to ensure it doesn't contain path traversal patterns.
    /// </summary>
    public static bool IsValidPluginId(string? pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        // Plugin IDs should be alphanumeric with limited safe characters
        foreach (char c in pluginId)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.')
                return false;
        }

        // Check for dangerous patterns
        if (pluginId.Contains(".."))
            return false;

        // Must start with letter
        if (!char.IsLetter(pluginId[0]))
            return false;

        return true;
    }

    /// <summary>
    /// Validates a directory path for safety.
    /// </summary>
    public static bool IsValidDirectoryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Check for dangerous patterns
            foreach (var pattern in DangerousPathPatterns)
            {
                if (path.Contains(pattern))
                    return false;
            }

            // Try to get full path - this will throw for invalid paths
            var fullPath = Path.GetFullPath(path);

            // Check path length
            if (fullPath.Length > 260) // Windows MAX_PATH
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a registry path for safety.
    /// </summary>
    public static bool IsValidRegistryPath(string? registryPath)
    {
        if (string.IsNullOrWhiteSpace(registryPath))
            return false;

        // Only allow specific registry roots
        var allowedRoots = new[]
        {
            "HKEY_CURRENT_USER",
            "HKEY_LOCAL_MACHINE",
            "HKEY_CLASSES_ROOT",
            "HKEY_USERS",
            "HKCU",
            "HKLM",
            "HKCR",
            "HKU"
        };

        var upperPath = registryPath.ToUpperInvariant();
        
        bool startsWithAllowedRoot = false;
        foreach (var root in allowedRoots)
        {
            if (upperPath.StartsWith(root) || upperPath.StartsWith("\\" + root))
            {
                startsWithAllowedRoot = true;
                break;
            }
        }

        if (!startsWithAllowedRoot)
            return false;

        // Check for path traversal in registry path
        if (registryPath.Contains(".."))
            return false;

        // Check for null bytes
        if (registryPath.Contains('\0'))
            return false;

        return true;
    }

    /// <summary>
    /// Validates a driver path for safety.
    /// </summary>
    public static bool IsValidDriverPath(string? driverPath)
    {
        if (string.IsNullOrWhiteSpace(driverPath))
            return false;

        // Driver paths should be in system directories
        var allowedDriverRoots = new[]
        {
            @"C:\Windows\System32\drivers",
            @"C:\Windows\SysWOW64\drivers",
            @"C:\Windows\System32\DriverStore",
        };

        try
        {
            var fullPath = Path.GetFullPath(driverPath);

            // Must start with an allowed driver root
            bool inAllowedLocation = false;
            foreach (var root in allowedDriverRoots)
            {
                if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    inAllowedLocation = true;
                    break;
                }
            }

            if (!inAllowedLocation)
                return false;

            // Check for path traversal
            if (fullPath.Contains(".."))
                return false;

            // Must be a .sys file
            if (!fullPath.EndsWith(".sys", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}
