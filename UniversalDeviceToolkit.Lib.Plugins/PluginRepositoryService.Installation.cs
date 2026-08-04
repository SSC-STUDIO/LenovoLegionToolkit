using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Plugins.Resources;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    /// <summary>
    /// Download and install a plugin from the repository.
    /// </summary>
    public async Task<bool> DownloadAndInstallPluginAsync(PluginManifest manifest)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting download and install for plugin: {manifest.Id}");

            var versionChecker = new VersionChecker();
            if (!versionChecker.IsCompatible(manifest.MinimumHostVersion))
            {
                var compatibilityMessage = string.Format(
                    Resource.Plugin_Error_Repository_HostIncompatible,
                    manifest.Id,
                    manifest.MinimumHostVersion);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(compatibilityMessage);

                DownloadFailed?.Invoke(this, compatibilityMessage);
                return false;
            }

            // Create temporary download path
            var tempFilePath = Path.Combine(_tempDownloadDirectory, $"{manifest.Id}.zip");

            // Download the plugin
            var downloadResult = await DownloadPluginAsync(manifest, tempFilePath).ConfigureAwait(false);
            if (!downloadResult.Success)
            {
                DownloadFailed?.Invoke(this, string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
                return false;
            }

            if (!await VerifyDownloadedPackageIntegrityAsync(
                    tempFilePath,
                    manifest,
                    downloadResult.TrustAsOfficialOnlinePackage).ConfigureAwait(false))
            {
                DownloadFailed?.Invoke(this, string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
                return false;
            }

            // Extract and install
            var extractPath = Path.Combine(_tempDownloadDirectory, manifest.Id);
            var installed = await ExtractAndInstallPluginAsync(
                tempFilePath,
                extractPath,
                manifest,
                downloadResult.TrustAsOfficialOnlinePackage).ConfigureAwait(false);

            // Clean up temp files
            try
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Cleanup of temp download directory failed: {ex.Message}", ex);
            }

            if (installed)
            {
                await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);

                if (!IsInstalledPluginUsable(manifest))
                {
                    var error = string.Format(Resource.Plugin_Error_Repository_NotLoadable, manifest.Id);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace(error);

                    await RemoveUnusableInstalledPayloadAsync(manifest.Id).ConfigureAwait(false);
                    DownloadFailed?.Invoke(this, error);
                    return false;
                }

                _pluginManager.InstallPlugin(manifest.Id);
                await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);

                DownloadCompleted?.Invoke(this, manifest.Id);
            }

            return installed;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error installing plugin {manifest.Id}: {ex.Message}", ex);
            DownloadFailed?.Invoke(this, ex.Message);
            return false;
        }
    }

    private bool IsInstalledPluginUsable(PluginManifest manifest)
    {
        if (_pluginManager.TryGetPlugin(manifest.Id, out var plugin) && plugin is not null and not PluginManifestAdapter)
            return true;

        return PluginUiCapabilityResolver.ResolveFromManifest(manifest).HasAny ||
               PluginUiCapabilityResolver.ResolveFromInstalledManifest(manifest.Id).HasAny;
    }

    private Task RemoveUnusableInstalledPayloadAsync(string pluginId)
    {
        TrustedPluginPackageStore.Remove(pluginId);
        return RestorePluginDirectoryAsync(Path.Combine(_pluginsDirectory, pluginId), backupDir: null, pluginId);
    }

    /// <summary>
    /// Extract plugin zip and install to plugins directory
    /// </summary>
    private async Task<bool> ExtractAndInstallPluginAsync(
        string zipPath,
        string extractPath,
        PluginManifest manifest,
        bool trustAsOfficialOnlinePackage)
    {
        string? backupDir = null;
        var pluginDir = Path.Combine(_pluginsDirectory, manifest.Id);

        try
        {
            // Clean up previous extraction
            if (Directory.Exists(extractPath))
            {
                try
                {
                    Directory.Delete(extractPath, true);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to clean up extraction path {extractPath}: {ex.Message}");
                }
            }
            Directory.CreateDirectory(extractPath);

            // Extract zip with path traversal protection
            var extractRoot = Path.GetFullPath(extractPath);
            if (!extractRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractRoot += Path.DirectorySeparatorChar;

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                var destinationPath = Path.GetFullPath(Path.Combine(extractRoot, entry.FullName));
                if (!destinationPath.StartsWith(extractRoot, StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException(string.Format(Resource.Plugin_Error_Repository_PathTraversal, entry.FullName));

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }

            var installationService = new PluginInstallationService(_pluginManager);
            var resolvedPluginId = await installationService.AnalyzeAndFixPluginStructureAsync(extractPath).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(resolvedPluginId))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Unable to normalize plugin package structure for {manifest.Id}");
                return false;
            }

            if (!resolvedPluginId.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Normalized plugin package id '{resolvedPluginId}' does not match requested manifest id '{manifest.Id}'. Aborting installation.");

                return false;
            }

            // Verify hash
            var dllPath = FindPluginMainDll(extractPath, resolvedPluginId);
            if (string.IsNullOrEmpty(dllPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin DLL not found for {manifest.Id}");
                return false;
            }

            var requireDllIntegrity = RequirePackageIntegrity(trustAsOfficialOnlinePackage);
            var hashString = await PluginPackageIntegrity.ComputeSha256HexAsync(dllPath).ConfigureAwait(false);
            if (!PluginPackageIntegrity.TryVerifyExpectedHash(
                    manifest.FileHash,
                    hashString,
                    requireDllIntegrity,
                    out var dllIntegrityFailure))
            {
                Log.Instance.Warning($"Plugin DLL integrity check failed for {manifest.Id}: {dllIntegrityFailure}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.FileHash) && !requireDllIntegrity)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {manifest.Id} has no fileHash in store manifest; skipping DLL integrity verification.");
            }

            // SECURITY: Validate plugin ID before using in path construction
            if (!PathSecurity.IsValidPluginId(manifest.Id))
            {
                Log.Instance.Warning($"SECURITY: Invalid plugin ID format: {manifest.Id}");
                return false;
            }

            // SECURITY: Verify the constructed path is within allowed directory
            if (!PathSecurity.IsPathWithinAllowedDirectory(pluginDir, _pluginsDirectory))
            {
                Log.Instance.Warning($"SECURITY: Plugin directory path traversal detected: {pluginDir}");
                return false;
            }

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

                        // Try to delete individual files instead
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

            // Copy all files from extraction
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

            return true;
        }
        catch (Exception ex)
        {
            await RestorePluginDirectoryAsync(pluginDir, backupDir, manifest.Id).ConfigureAwait(false);

            Log.Instance.Error($"Error extracting plugin {manifest.Id}: {ex.Message}", ex);
            return false;
        }
    }

    private static Task RestorePluginDirectoryAsync(string pluginDir, string? backupDir, string pluginId)
    {
        try
        {
            if (Directory.Exists(pluginDir))
                Directory.Delete(pluginDir, true);

            if (!string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir))
            {
                Directory.Move(backupDir, pluginDir);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rolled back plugin directory for {pluginId} from backup {backupDir}.");
            }
        }
        catch (Exception restoreEx)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to roll back plugin directory for {pluginId}: {restoreEx.Message}", restoreEx);
        }

        return Task.CompletedTask;
    }

    private static void EnsureInstalledManifest(string pluginDir, PluginManifest storeManifest)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pluginDir) || string.IsNullOrWhiteSpace(storeManifest.Id))
                return;

            Directory.CreateDirectory(pluginDir);

            var installedManifest = TryReadInstalledManifest(pluginDir, out _);
            var manifestToWrite = MergeInstalledManifest(installedManifest, storeManifest);
            var manifestPath = Path.Combine(pluginDir, "plugin.manifest.json");

            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifestToWrite, ManifestJsonOptions));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to persist plugin manifest metadata for {storeManifest.Id}: {ex.Message}", ex);
        }
    }

    private static PluginManifest? TryReadInstalledManifest(string pluginDir, out string? manifestPath)
    {
        manifestPath = null;

        foreach (var manifestFileName in InstalledManifestFileNames)
        {
            var candidate = Path.Combine(pluginDir, manifestFileName);
            if (!File.Exists(candidate))
                continue;

            try
            {
                manifestPath = candidate;
                return JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(candidate), ManifestJsonOptions);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to read installed plugin manifest {candidate}: {ex.Message}", ex);
            }
        }

        return null;
    }
}
