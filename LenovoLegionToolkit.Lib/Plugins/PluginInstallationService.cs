using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin installation service - Supports pre-compiled DLL plugins only.
/// Provides safe extraction, validation, and organization of plugin packages.
/// </summary>
public class PluginInstallationService
{
    private readonly IPluginManager _pluginManager;

    public PluginInstallationService(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// Installs a pre-compiled DLL plugin from a ZIP file.
    /// </summary>
    /// <param name="zipFilePath">Path to the plugin ZIP file.</param>
    /// <param name="pluginsDir">Target directory for plugins.</param>
    /// <returns>True if installation was successful.</returns>
    public async Task<bool> ExtractAndInstallPluginAsync(string zipFilePath, string pluginsDir)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "LLTPluginImport", Guid.NewGuid().ToString());

        try
        {
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(zipFilePath, tempDir);

            // Analyze and fix the plugin structure
            var pluginId = await AnalyzeAndFixPluginStructureAsync(tempDir).ConfigureAwait(false);
            if (string.IsNullOrEmpty(pluginId))
            {
                throw new InvalidOperationException("No valid plugin DLL found in ZIP file. Plugins must be pre-compiled and include a main DLL (either LenovoLegionToolkit.Plugins.*.dll or an ID-based name like custom-mouse.dll).");
            }

            // Install ZIP-imported plugins to a 'local' subdirectory
            var targetDir = Path.Combine(pluginsDir, "local", pluginId);
            if (Directory.Exists(targetDir))
            {
                try
                {
                    var backupDir = $"{targetDir}_backup_{DateTime.Now:yyyyMMddHHmmss}";
                    Directory.Move(targetDir, backupDir);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Renamed existing plugin directory {targetDir} to {backupDir} to resolve conflict during import.");
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to rename existing plugin directory {targetDir}, falling back to deletion: {ex.Message}");
                    Directory.Delete(targetDir, true);
                }
            }

            // Move organized temp directory to target location
            Directory.CreateDirectory(Path.GetDirectoryName(targetDir)!);
            Directory.Move(tempDir, targetDir);

            // Validate the installed plugin
            if (!await ValidatePluginAsync(targetDir).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"Plugin {pluginId} validation failed after installation.");
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Successfully installed plugin {pluginId} to {targetDir}");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to install plugin from {zipFilePath}: {ex.Message}", ex);
            throw;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to cleanup temporary directory {tempDir}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Validates the plugin directory for required files and valid assembly.
    /// </summary>
    private async Task<bool> ValidatePluginAsync(string pluginDir)
    {
        try
        {
            await Task.Yield();

            var manifestPluginId = TryReadPluginIdFromManifest(pluginDir);
            var pluginDll = FindPluginMainDll(pluginDir, manifestPluginId, SearchOption.TopDirectoryOnly);

            if (string.IsNullOrEmpty(pluginDll))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Validation failed: No plugin DLL found.");
                return false;
            }

            // Optional: Check for manifest file if required by the system
            var manifestFile = Path.Combine(pluginDir, "plugin.json");
            if (!File.Exists(manifestFile))
            {
                // Some plugins might not have a manifest yet, but we should log it
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Warning: Plugin manifest (plugin.json) not found.");
            }

            // Verify it's a valid .NET assembly
            try
            {
                global::System.Reflection.AssemblyName.GetAssemblyName(pluginDll);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Validation failed: {pluginDll} is not a valid assembly. {ex.Message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin validation error: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Analyzes the extracted directory and reorganizes it into a standard plugin structure.
    /// </summary>
    /// <returns>The plugin ID if successful.</returns>
    public async Task<string?> AnalyzeAndFixPluginStructureAsync(string extractDir)
    {
        await Task.Yield();

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Analyzing plugin structure in {extractDir}");

            // Case 1: Root directory contains the DLL
            var rootManifestPluginId = TryReadPluginIdFromManifest(extractDir);
            var rootDll = FindPluginMainDll(extractDir, rootManifestPluginId, SearchOption.TopDirectoryOnly);
            if (rootDll != null)
            {
                return GetPluginIdFromDll(rootDll, rootManifestPluginId);
            }

            // Case 2: DLL is inside a subfolder
            var subDirs = Directory.GetDirectories(extractDir);
            foreach (var subDir in subDirs)
            {
                var subDirManifestPluginId = TryReadPluginIdFromManifest(subDir);
                var expectedPluginId = subDirManifestPluginId ?? Path.GetFileName(subDir);
                var subDirDll = FindPluginMainDll(subDir, expectedPluginId, SearchOption.TopDirectoryOnly);
                if (subDirDll != null)
                {
                    var pluginId = GetPluginIdFromDll(subDirDll, subDirManifestPluginId);
                    
                    // Reorganize: Move contents of subDir to extractDir
                    await MoveDirectoryContentsAsync(subDir, extractDir).ConfigureAwait(false);
                    Directory.Delete(subDir, true);
                    
                    return pluginId;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error analyzing plugin structure: {ex.Message}", ex);
            return null;
        }
    }

    private static string? FindPluginMainDll(string searchRoot, string? preferredPluginId, SearchOption searchOption)
    {
        var pluginDlls = Directory.GetFiles(searchRoot, "*.dll", searchOption)
            .Where(path => !IsIgnoredDll(path))
            .ToList();

        if (!pluginDlls.Any())
            return null;

        if (!string.IsNullOrWhiteSpace(preferredPluginId))
        {
            var exactMatch = pluginDlls.FirstOrDefault(path =>
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                return fileNameWithoutExtension.Equals(preferredPluginId, StringComparison.OrdinalIgnoreCase) ||
                       fileNameWithoutExtension.Equals($"LenovoLegionToolkit.Plugins.{preferredPluginId}", StringComparison.OrdinalIgnoreCase);
            });

            if (exactMatch != null)
                return exactMatch;

            var normalizedPreferred = NormalizePluginToken(preferredPluginId);
            var normalizedMatches = pluginDlls
                .Where(path => NormalizePluginToken(Path.GetFileNameWithoutExtension(path))
                    .Equals(normalizedPreferred, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (normalizedMatches.Count == 1)
                return normalizedMatches[0];

            if (normalizedMatches.Count > 1)
            {
                return normalizedMatches.FirstOrDefault(path =>
                           Path.GetFileName(path).StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
                       ?? normalizedMatches[0];
            }
        }

        var prefixedDlls = pluginDlls
            .Where(path => Path.GetFileName(path).StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (prefixedDlls.Count == 1)
            return prefixedDlls[0];

        if (prefixedDlls.Count > 1)
            return prefixedDlls[0];

        return pluginDlls[0];
    }

    private static bool IsIgnoredDll(string dllPath)
    {
        var fileName = Path.GetFileName(dllPath);
        return fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("LenovoLegionToolkit.Plugins.SDK.dll", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePluginToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToLowerInvariant();
    }

    private static string? TryReadPluginIdFromManifest(string pluginDir)
    {
        try
        {
            var manifestPath = Path.Combine(pluginDir, "plugin.json");
            if (!File.Exists(manifestPath))
                return null;

            using var stream = File.OpenRead(manifestPath);
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("id", out var idElement))
                return null;

            var pluginId = idElement.GetString();
            return string.IsNullOrWhiteSpace(pluginId) ? null : pluginId.Trim();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read plugin manifest in {pluginDir}: {ex.Message}", ex);
            return null;
        }
    }

    private string GetPluginIdFromDll(string dllPath, string? manifestPluginId = null)
    {
        if (!string.IsNullOrWhiteSpace(manifestPluginId))
            return manifestPluginId.Trim();

        var dllName = Path.GetFileNameWithoutExtension(dllPath);

        if (dllName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
            return dllName.Replace("LenovoLegionToolkit.Plugins.", "", StringComparison.OrdinalIgnoreCase);

        return dllName;
    }

    /// <summary>
    /// Moves all files and subdirectories from source to target.
    /// </summary>
    public async Task MoveDirectoryContentsAsync(string sourceDir, string targetDir)
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                if (File.Exists(destFile))
                    File.Delete(destFile);
                File.Move(file, destFile);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destDir = Path.Combine(targetDir, Path.GetFileName(dir));
                if (Directory.Exists(destDir))
                    Directory.Delete(destDir, true);
                Directory.Move(dir, destDir);
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Deep copies a directory.
    /// </summary>
    public void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }
}
