using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin file system manager interface
/// </summary>
public interface IPluginFileSystemManager
{
    /// <summary>
    /// Get the plugins directory path
    /// </summary>
    string GetPluginsDirectory();

    /// <summary>
    /// Get all plugin DLL files in the plugins directory
    /// </summary>
    List<string> GetPluginDllFiles();

    /// <summary>
    /// Get name candidates for a plugin DLL file
    /// </summary>
    string[] GetMainPluginDllNameCandidates(string pluginId);

    /// <summary>
    /// Delete a plugin file with retry mechanism
    /// </summary>
    Task<bool> DeleteFileWithRetryAsync(string filePath, int maxRetries = 10, int delayMs = 200);

    /// <summary>
    /// Delete a plugin directory with retry mechanism
    /// </summary>
    Task<bool> DeleteDirectoryWithRetryAsync(string directoryPath, int maxRetries = 10, int delayMs = 200);

    /// <summary>
    /// Update the file cache with a plugin file path
    /// </summary>
    void UpdateFileCache(string filePath);

    /// <summary>
    /// Clear the plugin file timestamp cache before a forced rescan.
    /// </summary>
    void ClearFileCache();

    /// <summary>
    /// Get culture folders that should be skipped during plugin scanning
    /// </summary>
    HashSet<string> GetCultureFolders();
}

/// <summary>
/// Plugin file system manager implementation
/// Handles file system operations for plugins
/// </summary>
public class PluginFileSystemManager : IPluginFileSystemManager
{
    private readonly HashSet<string> _cultureFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu", "it", "ja", "ko",
        "lv", "nl-nl", "pl", "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz-latn-uz",
        "vi", "zh-hans", "zh-hant", "tools"
    };

    private readonly Dictionary<string, DateTime> _pluginFileCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get the plugins directory path
    /// </summary>
    public string GetPluginsDirectory()
    {
        var pluginsDirectory = PluginPaths.GetPluginsDirectory();
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Using plugins directory: {pluginsDirectory}");
        return pluginsDirectory;
    }

    /// <summary>
    /// Get all plugin DLL files in the plugins directory
    /// </summary>
    public List<string> GetPluginDllFiles()
    {
        var pluginsDirectory = GetPluginsDirectory();

        if (!Directory.Exists(pluginsDirectory))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugins directory does not exist: {pluginsDirectory}");
            return new List<string>();
        }

        if (!PathSecurity.IsValidDirectoryPath(pluginsDirectory))
        {
            Log.Instance.Warning($"SECURITY: Invalid plugins directory path: {pluginsDirectory}");
            return new List<string>();
        }

        var candidates = new List<(string FilePath, string? ParentDirectoryName)>();
        var subdirectories = Directory.GetDirectories(pluginsDirectory);

        foreach (var subdir in subdirectories)
        {
            var dirName = Path.GetFileName(subdir);
            if (_cultureFolders.Contains(dirName) || PluginAssemblyNaming.IsSdkDirectoryName(dirName))
                continue;

            // If this is the "local" directory, scan its subdirectories
            if (dirName.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                var localSubDirs = Directory.GetDirectories(subdir);
                foreach (var localSubDir in localSubDirs)
                {
                    var localDirName = Path.GetFileName(localSubDir);
                    candidates.AddRange(
                        Directory.GetFiles(localSubDir, "*.dll", SearchOption.TopDirectoryOnly)
                            .Select(filePath => (FilePath: filePath, ParentDirectoryName: (string?)localDirName)));
                }
                continue;
            }

            candidates.AddRange(
                Directory.GetFiles(subdir, "*.dll", SearchOption.TopDirectoryOnly)
                    .Select(filePath => (FilePath: filePath, ParentDirectoryName: (string?)dirName)));
        }

        candidates.AddRange(
            Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(filePath => (FilePath: filePath, ParentDirectoryName: (string?)null)));

        return candidates
            .Where(candidate => IsPluginDll(candidate.FilePath, candidate.ParentDirectoryName))
            .Select(candidate => candidate.FilePath)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Check if a DLL file is a plugin DLL
    /// </summary>
    private bool IsPluginDll(string filePath, string? parentDirectoryName = null)
    {
        var pluginsDirectory = GetPluginsDirectory();
        if (!PathSecurity.IsPathWithinAllowedDirectory(filePath, pluginsDirectory))
        {
            Log.Instance.Warning($"SECURITY: Plugin DLL path outside allowed directory: {filePath}");
            return false;
        }

        var fileName = Path.GetFileName(filePath);
        if (!PathSecurity.IsValidFileName(fileName))
        {
            Log.Instance.Warning($"SECURITY: Invalid plugin DLL file name: {fileName}");
            return false;
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var fullPath = Path.GetFullPath(filePath);
        var fileInfo = new FileInfo(filePath);

        if (_pluginFileCache.TryGetValue(fullPath, out var cachedTime) && fileInfo.LastWriteTimeUtc <= cachedTime)
            return false;

        if (PluginAssemblyNaming.IsSdkOrSharedDllFileName(fileName) ||
            fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (PluginAssemblyNaming.IsPluginPrefixedFileName(fileName))
            return true;

        if (string.IsNullOrWhiteSpace(parentDirectoryName))
            return false;

        var normalizedDllName = NormalizePluginToken(fileNameWithoutExtension);
        var normalizedParentName = NormalizePluginToken(parentDirectoryName);
        var normalizedParentShortName = NormalizePluginToken(
            PluginAssemblyNaming.StripPluginPrefixForNormalization(parentDirectoryName));

        if (string.IsNullOrWhiteSpace(normalizedDllName))
            return false;

        return normalizedDllName.Equals(normalizedParentName, StringComparison.OrdinalIgnoreCase) ||
               normalizedDllName.Equals(normalizedParentShortName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalize a plugin token for comparison
    /// </summary>
    private static string NormalizePluginToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    /// <summary>
    /// Get name candidates for a plugin DLL file
    /// </summary>
    public string[] GetMainPluginDllNameCandidates(string pluginId)
    {
        if (!PathSecurity.IsValidPluginId(pluginId))
            return Array.Empty<string>();

        var normalized = NormalizePluginToken(pluginId);
        var pascalCase = ToPascalCasePluginId(pluginId);

        var candidates = new List<string>
        {
            $"{pluginId}.dll",
            $"{normalized}.dll"
        };

        candidates.AddRange(PluginAssemblyNaming.EnumeratePrefixedPluginDllFileNames(pluginId));
        candidates.AddRange(PluginAssemblyNaming.EnumeratePrefixedPluginDllFileNames(normalized));
        candidates.AddRange(PluginAssemblyNaming.EnumeratePrefixedPluginDllFileNames(pascalCase));

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Where(PathSecurity.IsValidFileName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Convert a plugin ID to Pascal case
    /// </summary>
    private static string ToPascalCasePluginId(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return string.Empty;

        var segments = pluginId
            .Split(new[] { '-', '_', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..].ToLowerInvariant());

        return string.Concat(segments);
    }

    /// <summary>
    /// Delete a file with retry mechanism to handle locked files
    /// </summary>
    public async Task<bool> DeleteFileWithRetryAsync(string filePath, int maxRetries = 10, int delayMs = 200)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return true;
            }
            catch (IOException)
            {
                if (i == maxRetries - 1)
                    return false;
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                if (i == maxRetries - 1)
                    return false;
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
        }
        return false;
    }

    /// <summary>
    /// Delete a directory with retry mechanism to handle locked files
    /// </summary>
    public async Task<bool> DeleteDirectoryWithRetryAsync(string directoryPath, int maxRetries = 10, int delayMs = 200)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                    return true;
                }
                return true;
            }
            catch (IOException)
            {
                if (i == maxRetries - 1)
                    return false;
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                if (i == maxRetries - 1)
                    return false;
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
        }
        return false;
    }

    /// <summary>
    /// Update the file cache with a plugin file path
    /// </summary>
    public void UpdateFileCache(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        _pluginFileCache[fullPath] = new FileInfo(filePath).LastWriteTimeUtc;
    }

    /// <summary>
    /// Clear the file cache
    /// </summary>
    public void ClearFileCache()
    {
        _pluginFileCache.Clear();
    }

    /// <summary>
    /// Get culture folders that should be skipped during plugin scanning
    /// </summary>
    public HashSet<string> GetCultureFolders() => _cultureFolders;
}
