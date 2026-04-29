namespace PluginTooling.Core;

public sealed class PluginInspectionService
{
    private readonly PluginRepository _repository = new();

    public PluginInspectionReport Inspect(string repositoryRoot, IReadOnlyList<string> pluginIds)
    {
        var repository = _repository.Load(repositoryRoot);
        var selectedPluginIds = _repository.ResolveTargetPluginIds(repository, pluginIds);

        var report = new PluginInspectionReport
        {
            RepositoryRoot = repository.RootPath,
            GeneratedAt = DateTimeOffset.UtcNow.ToString("O"),
        };

        foreach (var pluginId in selectedPluginIds)
        {
            var plugin = repository.Plugins[pluginId];
            var storeEntry = repository.StoreDocument?.Plugins.FirstOrDefault(entry =>
                string.Equals(entry.Id, plugin.Manifest.Id, StringComparison.OrdinalIgnoreCase));

            report.Plugins.Add(new PluginInspectionItem
            {
                PluginId = plugin.Manifest.Id,
                Name = plugin.Manifest.Name,
                Version = plugin.Manifest.Version,
                MinLltVersion = plugin.Manifest.MinLltVersion,
                FolderName = plugin.FolderName,
                DirectoryPath = plugin.DirectoryPath,
                ManifestPath = plugin.ManifestPath,
                ProjectPath = plugin.ProjectPath,
                TestProjectPath = plugin.TestProjectPath,
                ChangelogPath = plugin.ChangelogPath,
                StoreEntryPath = plugin.StoreEntryPath,
                OutputDirectory = plugin.OutputDirectory,
                ExpectedAssemblyPath = plugin.ExpectedAssemblyPath,
                HasBuildOutput = Directory.Exists(plugin.OutputDirectory),
                HasPluginAssembly = File.Exists(plugin.ExpectedAssemblyPath),
                HasOutputManifest = File.Exists(Path.Combine(plugin.OutputDirectory, "plugin.json")),
                HasTestProject = !string.IsNullOrWhiteSpace(plugin.TestProjectPath) && File.Exists(plugin.TestProjectPath),
                HasChangelog = !string.IsNullOrWhiteSpace(plugin.ChangelogPath) && File.Exists(plugin.ChangelogPath),
                HasUnreleasedChangelog = HasUnreleasedChangelog(plugin.ChangelogPath),
                HasStoreEntry = plugin.StoreEntry is not null && !string.IsNullOrWhiteSpace(plugin.StoreEntryPath) && File.Exists(plugin.StoreEntryPath),
                StoreJsonEntry = storeEntry is null
                    ? null
                    : new StoreInspectionItem
                    {
                        Version = storeEntry.Version,
                        MinLltVersion = storeEntry.MinLltVersion,
                        DownloadUrl = storeEntry.DownloadUrl,
                        Changelog = storeEntry.Changelog,
                        FileSize = storeEntry.FileSize,
                        ReleaseDate = storeEntry.ReleaseDate,
                        MatchesManifestVersion = string.Equals(storeEntry.Version, plugin.Manifest.Version, StringComparison.OrdinalIgnoreCase),
                    },
            });
        }

        return report;
    }

    private static bool HasUnreleasedChangelog(string? changelogPath)
    {
        if (string.IsNullOrWhiteSpace(changelogPath) || !File.Exists(changelogPath))
            return false;

        var text = File.ReadAllText(changelogPath);
        return text.Contains("## [Unreleased]", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("## Unreleased", StringComparison.OrdinalIgnoreCase);
    }
}
