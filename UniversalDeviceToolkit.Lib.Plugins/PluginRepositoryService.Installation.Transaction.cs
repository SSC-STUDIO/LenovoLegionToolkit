using System;
using System.IO;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    private void InstallExtractedPluginPayload(
        string extractPath,
        string pluginDir,
        PluginManifest manifest,
        bool trustAsOfficialOnlinePackage,
        out string? backupDir)
    {
        backupDir = null;

        if (Directory.Exists(pluginDir))
        {
            try
            {
                backupDir = $"{pluginDir}_backup_{DateTime.UtcNow:yyyyMMddHHmmss}";
                Directory.Move(pluginDir, backupDir);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Renamed existing plugin directory {pluginDir} to {backupDir} to resolve conflict.");
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to rename plugin directory {pluginDir}, falling back to deletion: {ex.Message}");

                try
                {
                    Directory.Delete(pluginDir, true);
                }
                catch (Exception deleteEx)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to delete plugin directory {pluginDir}: {deleteEx.Message}");

                    // Try to delete individual files instead.
                    try
                    {
                        foreach (var file in Directory.GetFiles(pluginDir, "*.*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception fileEx)
                            {
                                Log.Instance.TraceOnce(
                                    "plugin-repo-delete-file",
                                    $"Could not delete locked plugin file during reinstall: {file}",
                                    fileEx);
                            }
                        }
                    }
                    catch (Exception enumEx)
                    {
                        Log.Instance.TraceOnce(
                            "plugin-repo-delete-enum",
                            $"Could not enumerate plugin files for delete before reinstall: {pluginDir}",
                            enumEx);
                    }
                }
            }
        }

        Directory.CreateDirectory(pluginDir);

        foreach (var file in Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories))
        {
            if (ShouldSkipPluginPayloadFile(file))
                continue;

            // SECURITY: skip reparse points (symlinks/junctions) to prevent
            // a malicious archive from writing outside the plugin directory.
            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(file);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Skipping unreadable payload entry '{file}': {ex.Message}");
                continue;
            }

            if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Skipping reparse point payload entry '{file}'.");
                continue;
            }

            var relativePath = file.Substring(extractPath.Length).TrimStart('\\', '/');
            var destPath = Path.Combine(pluginDir, relativePath);

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            File.Copy(file, destPath, overwrite: true);
        }

        EnsureInstalledManifest(pluginDir, manifest);
        TryStageCanonicalPluginSharedAssembly(pluginDir);
        TryStageCanonicalPluginSdkAssembly(pluginDir);
        if (trustAsOfficialOnlinePackage)
        {
            TrustedPluginPackageStore.TrustPluginDirectory(manifest.Id, pluginDir);
        }
        else
        {
            // A local/dev fallback uses the same marketplace install path, so clear any
            // stale trust record from a previous online install before the plugin is loaded.
            TrustedPluginPackageStore.Remove(manifest.Id);
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Installed plugin {manifest.Id} to {pluginDir}");

        if (!string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir))
        {
            try
            {
                Directory.Delete(backupDir, true);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to clean up plugin backup directory {backupDir}: {ex.Message}", ex);
            }
        }
    }
}
