using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
                throw new InvalidOperationException("No valid plugin DLL found in ZIP file. Plugins must be pre-compiled and follow the naming convention: LenovoLegionToolkit.Plugins.*.dll");
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

            // Check for the required DLL
            var pluginDll = Directory.GetFiles(pluginDir, "LenovoLegionToolkit.Plugins.*.dll", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

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
            var rootDlls = Directory.GetFiles(extractDir, "LenovoLegionToolkit.Plugins.*.dll", SearchOption.TopDirectoryOnly);
            if (rootDlls.Any())
            {
                return GetPluginIdFromDll(rootDlls.First());
            }

            // Case 2: DLL is inside a subfolder
            var subDirs = Directory.GetDirectories(extractDir);
            foreach (var subDir in subDirs)
            {
                var subDirDlls = Directory.GetFiles(subDir, "LenovoLegionToolkit.Plugins.*.dll", SearchOption.TopDirectoryOnly);
                if (subDirDlls.Any())
                {
                    var pluginId = GetPluginIdFromDll(subDirDlls.First());
                    
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

    private string GetPluginIdFromDll(string dllPath)
    {
        var dllName = Path.GetFileNameWithoutExtension(dllPath);
        return dllName.Replace("LenovoLegionToolkit.Plugins.", "", StringComparison.OrdinalIgnoreCase);
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
