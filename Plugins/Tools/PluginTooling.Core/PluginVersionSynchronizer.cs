using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PluginTooling.Core;

public enum VersionBumpPart
{
    Patch,
    Minor,
    Major,
}

public sealed class VersionSyncReport
{
    public string PluginId { get; init; } = string.Empty;

    public string ManifestVersion { get; init; } = string.Empty;

    public string? ProjectVersion { get; init; }

    public string? PluginAttributeVersion { get; init; }

    public string? StoreVersion { get; init; }

    public bool IsAligned { get; init; }

    public bool Changed { get; init; }

    public IReadOnlyList<string> DriftMessages { get; init; } = [];

    public IReadOnlyList<string> Actions { get; init; } = [];
}

public sealed class PluginVersionSynchronizer
{
    private static readonly Regex PluginAttributeVersionRegex = new(
        @"version:\s*""[^""]*""",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly PluginRepository _repository = new();
    private readonly PluginManifestMigrator _migrator = new();

    public static string BumpSemVer(string version, VersionBumpPart part)
    {
        if (!Version.TryParse(version, out var parsed))
        {
            throw new FormatException($"Version '{version}' is not a valid SemVer string.");
        }

        var major = parsed.Major;
        var minor = parsed.Minor;
        var patch = parsed.Build >= 0 ? parsed.Build : 0;

        return part switch
        {
            VersionBumpPart.Major => $"{major + 1}.0.0",
            VersionBumpPart.Minor => $"{major}.{minor + 1}.0",
            VersionBumpPart.Patch => $"{major}.{minor}.{patch + 1}",
            _ => throw new ArgumentOutOfRangeException(nameof(part), part, null),
        };
    }

    public IReadOnlyList<VersionSyncReport> SyncRepository(
        string repositoryRoot,
        IReadOnlyList<string> pluginIds,
        bool checkOnly = false,
        Action<string>? log = null)
    {
        var repository = _repository.Load(repositoryRoot);
        var reports = new List<VersionSyncReport>();

        foreach (var pluginId in _repository.ResolveTargetPluginIds(repository, pluginIds))
        {
            var plugin = repository.Plugins[pluginId];
            reports.Add(Sync(plugin, writeChanges: !checkOnly, log));
        }

        return reports;
    }

    public VersionSyncReport Bump(
        PluginContext plugin,
        VersionBumpPart? part = null,
        string? explicitVersion = null,
        bool writeChanges = true,
        Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (explicitVersion is not null && part is not null)
        {
            throw new InvalidOperationException("Use either --version or --part, not both.");
        }

        var currentVersion = plugin.UnifiedManifest.Version;
        var nextVersion = explicitVersion
            ?? (part is null
                ? throw new InvalidOperationException("Bump requires --part or --version.")
                : BumpSemVer(currentVersion, part.Value));

        if (!writeChanges)
        {
            return BuildDriftReport(plugin, nextVersion);
        }

        plugin.UnifiedManifest.Version = nextVersion;
        plugin.UnifiedManifest.Package.AssetName = $"{plugin.UnifiedManifest.Id}-v{nextVersion}.zip";

        var manifestPath = Path.Combine(plugin.DirectoryPath, "plugin.manifest.json");
        PluginRepository.WriteJsonFile(manifestPath, plugin.UnifiedManifest);
        log?.Invoke($"Bumped {plugin.Manifest.Id} to {nextVersion} in plugin.manifest.json");

        return Sync(ReloadPlugin(plugin), writeChanges: true, log);
    }

    public VersionSyncReport Sync(PluginContext plugin, bool writeChanges = true, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        var manifestVersion = plugin.UnifiedManifest.Version;
        var projectVersion = ReadCsprojVersion(plugin.ProjectPath);
        var attributeVersion = ReadPluginAttributeVersion(plugin.DirectoryPath);
        var storeEntry = _repository.Load(plugin.RepositoryRoot).StoreDocument?.Plugins
            .FirstOrDefault(entry => string.Equals(entry.Id, plugin.Manifest.Id, StringComparison.OrdinalIgnoreCase));

        var drift = CollectDrift(plugin, manifestVersion, projectVersion, attributeVersion, storeEntry?.Version);
        if (!writeChanges)
        {
            return new VersionSyncReport
            {
                PluginId = plugin.Manifest.Id,
                ManifestVersion = manifestVersion,
                ProjectVersion = projectVersion,
                PluginAttributeVersion = attributeVersion,
                StoreVersion = storeEntry?.Version,
                IsAligned = drift.Count == 0,
                Changed = false,
                DriftMessages = drift,
            };
        }

        if (drift.Count == 0)
        {
            return new VersionSyncReport
            {
                PluginId = plugin.Manifest.Id,
                ManifestVersion = manifestVersion,
                ProjectVersion = projectVersion,
                PluginAttributeVersion = attributeVersion,
                StoreVersion = storeEntry?.Version,
                IsAligned = true,
                Changed = false,
                DriftMessages = drift,
            };
        }

        var actions = new List<string>();

        plugin.UnifiedManifest.Package.AssetName = $"{plugin.UnifiedManifest.Id}-v{manifestVersion}.zip";
        PluginRepository.WriteJsonFile(
            Path.Combine(plugin.DirectoryPath, "plugin.manifest.json"),
            plugin.UnifiedManifest);
        actions.Add("package.assetName");

        if (!string.Equals(projectVersion, manifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            WriteCsprojVersion(plugin.ProjectPath, manifestVersion);
            actions.Add("csproj");
            log?.Invoke($"Synced {plugin.Manifest.Id} csproj version -> {manifestVersion}");
        }

        if (!string.Equals(attributeVersion, manifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            WritePluginAttributeVersion(plugin.DirectoryPath, manifestVersion);
            actions.Add("Plugin attribute");
            log?.Invoke($"Synced {plugin.Manifest.Id} [Plugin] attribute -> {manifestVersion}");
        }

        _migrator.Migrate(plugin.RepositoryRoot, [plugin.Manifest.Id], log);

        var refreshed = ReloadPlugin(plugin);
        var remainingDrift = CollectDrift(
            refreshed,
            manifestVersion,
            manifestVersion,
            manifestVersion,
            storeEntry?.Version);

        return new VersionSyncReport
        {
            PluginId = plugin.Manifest.Id,
            ManifestVersion = manifestVersion,
            ProjectVersion = manifestVersion,
            PluginAttributeVersion = manifestVersion,
            StoreVersion = storeEntry?.Version,
            IsAligned = remainingDrift.All(message => message.StartsWith("store.json", StringComparison.Ordinal)),
            Changed = true,
            DriftMessages = drift,
            Actions = actions,
        };
    }

    private PluginContext ReloadPlugin(PluginContext plugin)
    {
        var repository = _repository.Load(plugin.RepositoryRoot);
        return repository.Plugins[plugin.Manifest.Id];
    }

    private static VersionSyncReport BuildDriftReport(PluginContext plugin, string targetVersion)
    {
        var projectVersion = ReadCsprojVersion(plugin.ProjectPath);
        var attributeVersion = ReadPluginAttributeVersion(plugin.DirectoryPath);
        var drift = CollectDrift(plugin, targetVersion, projectVersion, attributeVersion, storeVersion: null);
        drift.Insert(0, $"manifest would change: {plugin.UnifiedManifest.Version} -> {targetVersion}");

        return new VersionSyncReport
        {
            PluginId = plugin.Manifest.Id,
            ManifestVersion = targetVersion,
            ProjectVersion = projectVersion,
            PluginAttributeVersion = attributeVersion,
            IsAligned = false,
            Changed = false,
            DriftMessages = drift,
        };
    }

    private static List<string> CollectDrift(
        PluginContext plugin,
        string manifestVersion,
        string? projectVersion,
        string? attributeVersion,
        string? storeVersion)
    {
        var drift = new List<string>();
        var expectedAsset = $"{plugin.Manifest.Id}-v{manifestVersion}.zip";

        if (!string.Equals(plugin.UnifiedManifest.Package.AssetName, expectedAsset, StringComparison.OrdinalIgnoreCase))
        {
            drift.Add($"package.assetName is '{plugin.UnifiedManifest.Package.AssetName}', expected '{expectedAsset}'.");
        }

        if (!string.Equals(projectVersion, manifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            drift.Add($"csproj Version is '{projectVersion ?? "(missing)"}', expected '{manifestVersion}'.");
        }

        if (!string.Equals(attributeVersion, manifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            drift.Add($"[Plugin] attribute version is '{attributeVersion ?? "(missing)"}', expected '{manifestVersion}'.");
        }

        if (!string.Equals(plugin.Manifest.Version, manifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            drift.Add($"plugin.json Version is '{plugin.Manifest.Version}', expected '{manifestVersion}'. Run sync-version.");
        }

        if (storeVersion is not null &&
            !string.Equals(storeVersion, manifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            drift.Add($"store.json version is '{storeVersion}', expected '{manifestVersion}'. Run generate-store after packaging.");
        }

        return drift;
    }

    public static string? ReadCsprojVersion(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
        {
            return null;
        }

        var document = XDocument.Load(projectPath, LoadOptions.None);
        return ReadProperty(document, "Version");
    }

    public static void WriteCsprojVersion(string? projectPath, string version)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Plugin project file not found: {projectPath}");
        }

        var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
        var propertyGroups = document.Root?
            .Elements()
            .Where(element => string.Equals(element.Name.LocalName, "PropertyGroup", StringComparison.Ordinal))
            .ToList() ?? [];

        var targetGroup = propertyGroups.FirstOrDefault(group => group.Elements().Any(element =>
            string.Equals(element.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase)))
            ?? propertyGroups.FirstOrDefault()
            ?? throw new InvalidOperationException($"No PropertyGroup found in '{projectPath}'.");

        SetOrAddProperty(targetGroup, "Version", version);
        SetOrAddProperty(targetGroup, "FileVersion", version);
        SetOrAddProperty(targetGroup, "AssemblyVersion", version);

        File.WriteAllText(projectPath, PluginRepository.NormalizeLineEndings(document.ToString(SaveOptions.DisableFormatting)));
    }

    public static string? ReadPluginAttributeVersion(string pluginDirectory)
    {
        var pluginFile = FindPluginEntryFile(pluginDirectory);
        if (pluginFile is null)
        {
            return null;
        }

        var match = PluginAttributeVersionRegex.Match(File.ReadAllText(pluginFile));
        if (!match.Success)
        {
            return null;
        }

        var valueMatch = Regex.Match(match.Value, @"""([^""]*)""", RegexOptions.CultureInvariant);
        return valueMatch.Success ? valueMatch.Groups[1].Value : null;
    }

    public static void WritePluginAttributeVersion(string pluginDirectory, string version)
    {
        var pluginFile = FindPluginEntryFile(pluginDirectory)
            ?? throw new FileNotFoundException($"Plugin entry file with [Plugin(...)] not found under '{pluginDirectory}'.");

        var text = File.ReadAllText(pluginFile);
        if (!PluginAttributeVersionRegex.IsMatch(text))
        {
            throw new InvalidOperationException($"[Plugin(... version: ...)] not found in '{pluginFile}'.");
        }

        var updated = PluginAttributeVersionRegex.Replace(text, $"version: \"{version}\"", 1);
        File.WriteAllText(pluginFile, PluginRepository.NormalizeLineEndings(updated));
    }

    private static string? FindPluginEntryFile(string pluginDirectory)
    {
        var candidates = Directory
            .EnumerateFiles(pluginDirectory, "*Plugin.cs", SearchOption.TopDirectoryOnly)
            .Where(path => File.ReadAllText(path).Contains("[Plugin(", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return candidates.Count switch
        {
            0 => null,
            1 => candidates[0],
            _ => candidates.FirstOrDefault(path => Path.GetFileName(path).EndsWith("Plugin.cs", StringComparison.OrdinalIgnoreCase)),
        };
    }

    private static string ReadProperty(XDocument document, string propertyName)
    {
        return document.Root?
            .Elements()
            .Where(element => string.Equals(element.Name.LocalName, "PropertyGroup", StringComparison.Ordinal))
            .Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
            ?.Value.Trim() ?? string.Empty;
    }

    private static void SetOrAddProperty(XElement propertyGroup, string propertyName, string value)
    {
        var existing = propertyGroup
            .Elements()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            propertyGroup.Add(new XElement(propertyName, value));
            return;
        }

        existing.Value = value;
    }
}