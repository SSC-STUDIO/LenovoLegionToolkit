namespace PluginTooling.Core;

public sealed class StoreJsonGenerator
{
    private readonly PluginRepository _repository = new();

    public StoreDocument Generate(StoreGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var selectedPluginIds = request.PluginIds.Count == 0
            ? repository.Plugins.Values.Where(HasStoreMetadata).Select(plugin => plugin.Manifest.Id).ToArray()
            : _repository.ResolveTargetPluginIds(repository, request.PluginIds);

        var assetRoot = request.AssetRoot is null
            ? Path.Combine(repository.RootPath, "Build", "release-assets")
            : Path.GetFullPath(request.AssetRoot);

        var releaseDate = request.ReleaseDate ?? DateTimeOffset.UtcNow;
        var existingEntries = repository.StoreDocument?.Plugins.ToDictionary(
            entry => entry.Id,
            entry => entry,
            StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, StorePluginEntry>(StringComparer.OrdinalIgnoreCase);

        var store = new StoreDocument
        {
            LastUpdated = releaseDate.ToString("O"),
            Plugins = [],
        };

        if (request.MergeExisting && repository.StoreDocument is not null)
        {
            store.LastUpdated = repository.StoreDocument.LastUpdated;
            store.Plugins.AddRange(repository.StoreDocument.Plugins.Select(Clone));
        }

        foreach (var pluginId in selectedPluginIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            var plugin = repository.Plugins[pluginId];
            if (!HasStoreMetadata(plugin))
                continue;

            var storeMetadata = plugin.UnifiedManifest.Store;
            var tagName = $"{plugin.Manifest.Id}-v{plugin.Manifest.Version}";
            var assetName = $"{tagName}.zip";
            var assetPath = Path.Combine(assetRoot, assetName);
            var assetExists = File.Exists(assetPath);
            if (request.RequireAssets && !assetExists)
                throw new FileNotFoundException($"Release asset is required for store generation but was not found: {assetPath}", assetPath);

            var generatedEntry = new StorePluginEntry
            {
                Id = plugin.Manifest.Id,
                Name = plugin.Manifest.Name,
                Description = storeMetadata.Description,
                Author = plugin.Manifest.Author,
                Version = plugin.Manifest.Version,
                MinLltVersion = plugin.Manifest.MinLltVersion,
                IsSystemPlugin = plugin.Manifest.IsSystemPlugin,
                DownloadUrl = $"{request.ReleaseRepositoryUrl}/download/{tagName}/{assetName}",
                Changelog = $"{request.ReleaseRepositoryUrl}/tag/{tagName}",
                FileSize = assetExists ? new FileInfo(assetPath).Length : 0,
                ReleaseDate = releaseDate.ToString("O"),
                RepositoryUrl = storeMetadata.RepositoryUrl ?? plugin.Manifest.Repository,
                SupportedLanguages = storeMetadata.SupportedLanguages.ToList(),
                Icon = storeMetadata.Icon,
                IconBackground = storeMetadata.IconBackground,
                Dependencies = storeMetadata.Dependencies.ToList(),
                Tags = storeMetadata.Tags.ToList(),
            };

            var hasExistingEntry = existingEntries.TryGetValue(plugin.Manifest.Id, out var existingEntry);
            if (request.MergeExisting &&
                hasExistingEntry &&
                existingEntry is not null &&
                string.Equals(existingEntry.Version, plugin.Manifest.Version, StringComparison.OrdinalIgnoreCase) &&
                existingEntry.FileSize > 0 &&
                existingEntry.FileSize == generatedEntry.FileSize)
            {
                generatedEntry.ReleaseDate = existingEntry.ReleaseDate;
            }

            ReplaceOrAdd(store.Plugins, generatedEntry);

            if (!request.MergeExisting || !hasExistingEntry || existingEntry is null || !EntriesEqual(existingEntry, generatedEntry))
                store.LastUpdated = releaseDate.ToString("O");
        }

        store.Plugins = store.Plugins
            .OrderBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return store;
    }

    public string Write(StoreGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var outputPath = request.OutputPath is null
            ? Path.Combine(repository.RootPath, "store.json")
            : Path.GetFullPath(request.OutputPath);

        var store = Generate(request);
        PluginRepository.WriteJsonFile(outputPath, store);
        return outputPath;
    }

    public StoreCheckResult Check(StoreGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var storePath = request.OutputPath is null
            ? Path.Combine(repository.RootPath, "store.json")
            : Path.GetFullPath(request.OutputPath);

        if (!File.Exists(storePath))
            return new StoreCheckResult(storePath, false, $"Store file not found: {storePath}");

        var generated = Generate(request);
        var expected = PluginRepository.ToJson(generated);
        var current = File.ReadAllText(storePath);

        var matches = NormalizeForComparison(current) == NormalizeForComparison(expected);
        return matches
            ? new StoreCheckResult(storePath, true, "store.json matches generator output.")
            : new StoreCheckResult(storePath, false, "store.json differs from generator output. Re-run generate-store with the same arguments to update it.");
    }

    private static string NormalizeForComparison(string value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] == '\uFEFF')
            value = value[1..];

        return PluginRepository.NormalizeLineEndings(value).TrimEnd();
    }

    private static bool HasStoreMetadata(PluginContext plugin)
    {
        var store = plugin.UnifiedManifest.Store;
        return !string.IsNullOrWhiteSpace(store.Description) &&
               !string.IsNullOrWhiteSpace(store.Icon) &&
               !string.IsNullOrWhiteSpace(store.IconBackground) &&
               store.Tags.Count > 0 &&
               store.SupportedLanguages.Count > 0;
    }

    private static void ReplaceOrAdd(List<StorePluginEntry> entries, StorePluginEntry entry)
    {
        var index = entries.FindIndex(existing => string.Equals(existing.Id, entry.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            entries[index] = entry;
        else
            entries.Add(entry);
    }

    private static StorePluginEntry Clone(StorePluginEntry entry)
    {
        return new StorePluginEntry
        {
            Id = entry.Id,
            Name = entry.Name,
            Description = entry.Description,
            Author = entry.Author,
            Version = entry.Version,
            MinLltVersion = entry.MinLltVersion,
            IsSystemPlugin = entry.IsSystemPlugin,
            DownloadUrl = entry.DownloadUrl,
            Changelog = entry.Changelog,
            FileSize = entry.FileSize,
            ReleaseDate = entry.ReleaseDate,
            RepositoryUrl = entry.RepositoryUrl,
            SupportedLanguages = entry.SupportedLanguages.ToList(),
            Icon = entry.Icon,
            IconBackground = entry.IconBackground,
            Dependencies = entry.Dependencies.ToList(),
            Tags = entry.Tags.ToList(),
        };
    }

    private static bool EntriesEqual(StorePluginEntry left, StorePluginEntry right)
    {
        return string.Equals(left.Id, right.Id, StringComparison.Ordinal) &&
               string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
               string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
               string.Equals(left.Author, right.Author, StringComparison.Ordinal) &&
               string.Equals(left.Version, right.Version, StringComparison.Ordinal) &&
               string.Equals(left.MinLltVersion, right.MinLltVersion, StringComparison.Ordinal) &&
               left.IsSystemPlugin == right.IsSystemPlugin &&
               string.Equals(left.DownloadUrl, right.DownloadUrl, StringComparison.Ordinal) &&
               string.Equals(left.Changelog, right.Changelog, StringComparison.Ordinal) &&
               left.FileSize == right.FileSize &&
               string.Equals(left.ReleaseDate, right.ReleaseDate, StringComparison.Ordinal) &&
               string.Equals(left.RepositoryUrl, right.RepositoryUrl, StringComparison.Ordinal) &&
               left.SupportedLanguages.SequenceEqual(right.SupportedLanguages, StringComparer.Ordinal) &&
               string.Equals(left.Icon, right.Icon, StringComparison.Ordinal) &&
               string.Equals(left.IconBackground, right.IconBackground, StringComparison.Ordinal) &&
               left.Dependencies.SequenceEqual(right.Dependencies, StringComparer.Ordinal) &&
               left.Tags.SequenceEqual(right.Tags, StringComparer.Ordinal);
    }
}
