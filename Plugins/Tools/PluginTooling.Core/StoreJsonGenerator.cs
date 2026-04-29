namespace PluginTooling.Core;

public sealed class StoreJsonGenerator
{
    private readonly PluginRepository _repository = new();

    public StoreDocument Generate(StoreGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var selectedPluginIds = request.PluginIds.Count == 0
            ? repository.Plugins.Values.Where(plugin => plugin.StoreEntry is not null).Select(plugin => plugin.Manifest.Id).ToArray()
            : _repository.ResolveTargetPluginIds(repository, request.PluginIds);

        var assetRoot = request.AssetRoot is null
            ? Path.Combine(repository.RootPath, "Build", "release-assets")
            : Path.GetFullPath(request.AssetRoot);

        var releaseDate = request.ReleaseDate ?? DateTimeOffset.UtcNow;

        var store = new StoreDocument
        {
            LastUpdated = releaseDate.ToString("O"),
            Plugins = [],
        };

        foreach (var pluginId in selectedPluginIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            var plugin = repository.Plugins[pluginId];
            if (plugin.StoreEntry is null)
                continue;

            var tagName = $"{plugin.Manifest.Id}-v{plugin.Manifest.Version}";
            var assetName = $"{tagName}.zip";
            var assetPath = Path.Combine(assetRoot, assetName);

            store.Plugins.Add(new StorePluginEntry
            {
                Id = plugin.Manifest.Id,
                Name = plugin.Manifest.Name,
                Description = plugin.StoreEntry.Description,
                Author = plugin.Manifest.Author,
                Version = plugin.Manifest.Version,
                MinLltVersion = plugin.Manifest.MinLltVersion,
                IsSystemPlugin = plugin.Manifest.IsSystemPlugin,
                DownloadUrl = $"{request.ReleaseRepositoryUrl}/download/{tagName}/{assetName}",
                Changelog = $"{request.ReleaseRepositoryUrl}/tag/{tagName}",
                FileSize = File.Exists(assetPath) ? new FileInfo(assetPath).Length : 0,
                ReleaseDate = releaseDate.ToString("O"),
                RepositoryUrl = plugin.StoreEntry.RepositoryUrl ?? plugin.Manifest.Repository,
                SupportedLanguages = plugin.StoreEntry.SupportedLanguages.ToList(),
                Icon = plugin.StoreEntry.Icon,
                IconBackground = plugin.StoreEntry.IconBackground,
                Dependencies = plugin.StoreEntry.Dependencies.ToList(),
                Tags = plugin.StoreEntry.Tags.ToList(),
            });
        }

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
}
