using System;
using System.IO;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Plugins.Resources;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    /// <summary>
    /// Serializes installation transactions so concurrent downloads cannot replace or scan
    /// the same plugin directory at the same time.
    /// </summary>
    public async Task<bool> DownloadAndInstallPluginAsync(PluginManifest manifest)
    {
        if (!PathSecurity.IsValidPluginId(manifest.Id))
        {
            Log.Instance.Warning($"Rejected plugin installation with invalid plugin id: {manifest.Id}");
            DownloadFailed?.Invoke(
                this,
                string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
            return false;
        }

        await _installationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await DownloadAndInstallPluginCoreAsync(manifest).ConfigureAwait(false);
        }
        finally
        {
            _installationGate.Release();
        }
    }

    private async Task<bool> DownloadAndInstallPluginCoreAsync(PluginManifest manifest)
    {
        var tempFilePath = Path.Combine(_tempDownloadDirectory, $"{manifest.Id}.zip");
        var extractPath = Path.Combine(_tempDownloadDirectory, manifest.Id);

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

            var installed = await ExtractAndInstallPluginAsync(
                tempFilePath,
                extractPath,
                manifest,
                downloadResult.TrustAsOfficialOnlinePackage).ConfigureAwait(false);

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
        finally
        {
            CleanupInstallationArtifacts(tempFilePath, extractPath);
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

    private static void CleanupInstallationArtifacts(string tempFilePath, string extractPath)
    {
        DeletePartialDownload(tempFilePath);

        try
        {
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, recursive: true);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-repo-extract-cleanup",
                $"Failed to clean up plugin extraction directory: {extractPath}",
                ex);
        }
    }

}
