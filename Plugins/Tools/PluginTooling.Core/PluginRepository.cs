using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace PluginTooling.Core;

public sealed class PluginRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public RepositoryContext Load(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var rootPath = Path.GetFullPath(repositoryRoot);
        EnsureRepositoryRoot(rootPath);

        var solutionPath = Path.Combine(rootPath, "LenovoLegionToolkit-Plugins.sln");
        var pluginsRoot = Path.Combine(rootPath, "Plugins");
        var hostDependenciesRoot = Path.Combine(rootPath, "Dependencies", "Host");
        var storePath = Path.Combine(rootPath, "store.json");

        var plugins = DiscoverPlugins(rootPath, pluginsRoot)
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        var storeDocument = File.Exists(storePath)
            ? ReadJsonFile<StoreDocument>(storePath)
            : null;

        return new RepositoryContext(rootPath, solutionPath, pluginsRoot, hostDependenciesRoot, plugins, storeDocument);
    }

    public static string FindRepositoryRoot(string startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        for (var i = 0; i < 10 && current is not null; i++)
        {
            if (File.Exists(Path.Combine(current.FullName, "LenovoLegionToolkit-Plugins.sln")) &&
                Directory.Exists(Path.Combine(current.FullName, "Plugins")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate LenovoLegionToolkit-Plugins repository root from '{startPath}'.");
    }

    public IReadOnlyList<string> ResolveTargetPluginIds(RepositoryContext repository, IReadOnlyList<string> pluginIds)
    {
        if (pluginIds.Count == 0)
            return repository.Plugins.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();

        var resolved = new List<string>();
        foreach (var selection in pluginIds)
        {
            if (repository.Plugins.ContainsKey(selection))
            {
                resolved.Add(selection);
                continue;
            }

            var byFolder = repository.Plugins.Values.FirstOrDefault(plugin =>
                string.Equals(plugin.FolderName, selection, StringComparison.OrdinalIgnoreCase));

            if (byFolder is null)
                throw new InvalidOperationException($"Unknown plugin selection '{selection}'.");

            resolved.Add(byFolder.Manifest.Id);
        }

        return resolved.Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ArchetypeDefinition LoadArchetypeDefinition(string repositoryRoot, PluginArchetype archetype)
    {
        var templateKey = archetype switch
        {
            PluginArchetype.SettingsOnly => "settings-only",
            PluginArchetype.FeatureSettings => "feature-settings",
            PluginArchetype.RuntimeOptimization => "runtime-optimization",
            _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null),
        };

        var path = Path.Combine(repositoryRoot, "Templates", "PluginArchetypes", templateKey, "template.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Template definition not found: {path}");

        return ReadJsonFile<ArchetypeDefinition>(path);
    }

    public IReadOnlyList<string> InferSupportedLanguages(PluginContext plugin)
    {
        var resourcesDirectory = Path.Combine(plugin.DirectoryPath, "Resources");
        if (!Directory.Exists(resourcesDirectory))
            return ["en"];

        var languages = Directory.EnumerateFiles(resourcesDirectory, "Resource*.resx", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Select(static name =>
            {
                if (string.Equals(name, "Resource", StringComparison.OrdinalIgnoreCase))
                    return "en";

                const string prefix = "Resource.";
                return name is not null && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    ? name[prefix.Length..]
                    : null;
            })
            .Where(static language => !string.IsNullOrWhiteSpace(language))
            .Select(static language => language!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static language => language, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return languages.Length == 0 ? ["en"] : languages;
    }

    public void EnsureRepositoryRoot(string rootPath)
    {
        if (!File.Exists(Path.Combine(rootPath, "LenovoLegionToolkit-Plugins.sln")) ||
            !Directory.Exists(Path.Combine(rootPath, "Plugins")))
        {
            throw new DirectoryNotFoundException($"Path is not LenovoLegionToolkit-Plugins repository root: {rootPath}");
        }
    }

    public static T ReadJsonFile<T>(string path)
    {
        var json = File.ReadAllText(path);
        if (!string.IsNullOrEmpty(json) && json[0] == '\uFEFF')
            json = json[1..];

        return JsonSerializer.Deserialize<T>(json, JsonOptions)
               ?? throw new InvalidOperationException($"Failed to deserialize '{path}' as {typeof(T).Name}.");
    }

    public static void WriteJsonFile<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, ToJson(value));
    }

    public static string ToJson<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return json + Environment.NewLine;
    }

    public static string ReadProjectProperty(string projectPath, string propertyName)
    {
        var document = XDocument.Load(projectPath, LoadOptions.None);
        var value = document.Root?
            .Elements()
            .Where(element => string.Equals(element.Name.LocalName, "PropertyGroup", StringComparison.Ordinal))
            .Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        return value?.Trim() ?? string.Empty;
    }

    public static string NormalizeIdentifier(string value)
    {
        var parts = value
            .Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..])
            .ToArray();

        var normalized = string.Concat(parts);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException($"Unable to derive identifier from '{value}'.");

        return char.IsDigit(normalized[0]) ? $"Plugin{normalized}" : normalized;
    }

    public static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", Environment.NewLine, StringComparison.Ordinal);
    }

    private static IEnumerable<KeyValuePair<string, PluginContext>> DiscoverPlugins(string repositoryRoot, string pluginsRoot)
    {
        foreach (var pluginDirectory in Directory.EnumerateDirectories(pluginsRoot).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var folderName = Path.GetFileName(pluginDirectory);
            if (folderName is "Shared" or "TestCommon" || folderName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
                continue;

            var manifestPath = Path.Combine(pluginDirectory, "plugin.json");
            if (!File.Exists(manifestPath))
                continue;

            var manifest = ReadJsonFile<PluginManifest>(manifestPath);
            var projectPath = Directory.EnumerateFiles(pluginDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            var changelogPath = File.Exists(Path.Combine(pluginDirectory, "CHANGELOG.md"))
                ? Path.Combine(pluginDirectory, "CHANGELOG.md")
                : null;

            var testsDirectory = Path.Combine(pluginsRoot, $"{folderName}.Tests");
            var testProjectPath = Directory.Exists(testsDirectory)
                ? Directory.EnumerateFiles(testsDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault()
                : null;

            var storeEntryPath = Path.Combine(pluginDirectory, "store-entry.json");
            var storeEntry = File.Exists(storeEntryPath) ? ReadJsonFile<OfficialStoreEntry>(storeEntryPath) : null;

            yield return new KeyValuePair<string, PluginContext>(
                manifest.Id,
                new PluginContext(
                    repositoryRoot,
                    folderName,
                    pluginDirectory,
                    manifestPath,
                    manifest,
                    projectPath,
                    testProjectPath,
                    changelogPath,
                    File.Exists(storeEntryPath) ? storeEntryPath : null,
                    storeEntry));
        }
    }
}
