using System;
using System.IO;
using System.Text.Json;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
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
