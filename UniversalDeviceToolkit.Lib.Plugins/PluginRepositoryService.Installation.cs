using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Plugins.Resources;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
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

            InstallExtractedPluginPayload(
                extractPath,
                pluginDir,
                manifest,
                trustAsOfficialOnlinePackage,
                out backupDir);

            return true;
        }
        catch (Exception ex)
        {
            await RestorePluginDirectoryAsync(pluginDir, backupDir, manifest.Id).ConfigureAwait(false);

            Log.Instance.Error($"Error extracting plugin {manifest.Id}: {ex.Message}", ex);
            return false;
        }
    }

}
