namespace PluginTooling.Core;

public sealed class PluginManifestMigrator
{
    private readonly PluginRepository _repository = new();

    public IReadOnlyList<string> Migrate(string repositoryRoot, IReadOnlyList<string> pluginIds, Action<string>? log = null)
    {
        var repository = _repository.Load(repositoryRoot);
        var written = new List<string>();

        foreach (var pluginId in _repository.ResolveTargetPluginIds(repository, pluginIds))
        {
            var plugin = repository.Plugins[pluginId];
            var unifiedPath = Path.Combine(plugin.DirectoryPath, "plugin.manifest.json");
            var unifiedManifest = plugin.UnifiedManifest;

            if ((unifiedManifest.Store.SupportedLanguages ?? []).Count == 0)
            {
                unifiedManifest.Store.SupportedLanguages ??= [];
                unifiedManifest.Store.SupportedLanguages.AddRange(_repository.InferSupportedLanguages(plugin));
            }

            if (string.IsNullOrWhiteSpace(unifiedManifest.Package.AssetName))
            {
                unifiedManifest.Package.AssetName = $"{unifiedManifest.Id}-v{unifiedManifest.Version}.zip";
            }

            EnsureRequiredFile(unifiedManifest, $"{plugin.ExpectedAssemblyName}.dll");
            EnsureRequiredFile(unifiedManifest, "UniversalDeviceToolkit.Plugins.SDK.dll");
            EnsureRequiredFile(unifiedManifest, "plugin.json");
            EnsureRequiredFile(unifiedManifest, "plugin.manifest.json");

            PluginRepository.WriteJsonFile(unifiedPath, unifiedManifest);
            PluginRepository.WriteJsonFile(Path.Combine(plugin.DirectoryPath, "plugin.json"), PluginRepository.ToLegacyManifest(unifiedManifest));
            if (HasStoreMetadata(unifiedManifest))
            {
                PluginRepository.WriteJsonFile(Path.Combine(plugin.DirectoryPath, "store-entry.json"), PluginRepository.ToStoreEntry(unifiedManifest));
            }

            written.Add(unifiedPath);
            log?.Invoke($"Migrated {pluginId}: {unifiedPath}");
        }

        return written;
    }

    private static void EnsureRequiredFile(UnifiedPluginManifest manifest, string fileName)
    {
        if (!(manifest.Package.RequiredFiles ?? []).Contains(fileName, StringComparer.OrdinalIgnoreCase))
        {
            manifest.Package.RequiredFiles ??= [];
            manifest.Package.RequiredFiles.Add(fileName);
        }
    }

    private static bool HasStoreMetadata(UnifiedPluginManifest manifest)
    {
        return !string.IsNullOrWhiteSpace(manifest.Store.Description) &&
               !string.IsNullOrWhiteSpace(manifest.Store.Icon) &&
               !string.IsNullOrWhiteSpace(manifest.Store.IconBackground) &&
               (manifest.Store.Tags ?? []).Count > 0 &&
               (manifest.Store.SupportedLanguages ?? []).Count > 0;
    }
}
