using System.Text.Json.Serialization;

namespace PluginTooling.Core;

public enum PluginArchetype
{
    SettingsOnly,
    FeatureSettings,
    RuntimeOptimization,
}

public enum PluginValidationProfile
{
    Contributor,
    OfficialCandidate,
    OfficialRelease,
}

public enum PluginWorkbenchThemeMode
{
    System,
    Light,
    Dark,
}

public enum PluginWorkbenchView
{
    Feature,
    Settings,
    Optimization,
}

public sealed record PluginManifest(
    string Id,
    string Name,
    string Version,
    string MinLltVersion,
    string Author,
    bool IsSystemPlugin,
    string Repository,
    string Issues);

public sealed record OfficialStoreEntry(
    string Description,
    string Icon,
    string IconBackground,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> SupportedLanguages,
    string? RepositoryUrl);

public sealed record PluginContext(
    string RepositoryRoot,
    string FolderName,
    string DirectoryPath,
    string ManifestPath,
    PluginManifest Manifest,
    string? ProjectPath,
    string? TestProjectPath,
    string? ChangelogPath,
    string? StoreEntryPath,
    OfficialStoreEntry? StoreEntry)
{
    public string OutputDirectory => Path.Combine(RepositoryRoot, "Build", "plugins", $"LenovoLegionToolkit.Plugins.{FolderName}");
    public string ExpectedAssemblyName => $"LenovoLegionToolkit.Plugins.{FolderName}";
    public string ExpectedAssemblyPath => Path.Combine(OutputDirectory, $"{ExpectedAssemblyName}.dll");
}

public sealed record RepositoryContext(
    string RootPath,
    string SolutionPath,
    string PluginsRoot,
    string HostDependenciesRoot,
    IReadOnlyDictionary<string, PluginContext> Plugins,
    StoreDocument? StoreDocument);

public sealed class StoreDocument
{
    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("plugins")]
    public List<StorePluginEntry> Plugins { get; set; } = [];
}

public sealed class StorePluginEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("minLLTVersion")]
    public string MinLltVersion { get; set; } = string.Empty;

    [JsonPropertyName("isSystemPlugin")]
    public bool IsSystemPlugin { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("changelog")]
    public string Changelog { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonPropertyName("repositoryUrl")]
    public string RepositoryUrl { get; set; } = string.Empty;

    [JsonPropertyName("supportedLanguages")]
    public List<string> SupportedLanguages { get; set; } = [];

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("iconBackground")]
    public string IconBackground { get; set; } = string.Empty;

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}

public sealed class ValidationRequest
{
    public string RepositoryRoot { get; init; } = string.Empty;
    public string Configuration { get; init; } = "Release";
    public bool SkipBuild { get; init; }
    public bool SkipTests { get; init; }
    public PluginValidationProfile Profile { get; init; } = PluginValidationProfile.Contributor;
    public IReadOnlyList<string> PluginIds { get; init; } = Array.Empty<string>();
}

public sealed class ValidationReport
{
    [JsonPropertyName("totals")]
    public ValidationTotals Totals { get; set; } = new();

    [JsonPropertyName("plugins")]
    public List<PluginReportItem> Plugins { get; set; } = [];

    [JsonPropertyName("steps")]
    public List<StepReportItem> Steps { get; set; } = [];
}

public sealed class ValidationTotals
{
    [JsonPropertyName("pluginCount")]
    public int PluginCount { get; set; }

    [JsonPropertyName("failures")]
    public int Failures { get; set; }

    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }
}

public sealed class PluginReportItem
{
    [JsonPropertyName("pluginId")]
    public string PluginId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("failures")]
    public int Failures { get; set; }

    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }
}

public sealed class StepReportItem
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("pluginId")]
    public string PluginId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public sealed class DoctorCheck
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "INFO";

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

public sealed class DoctorResult
{
    [JsonPropertyName("repositoryRoot")]
    public string RepositoryRoot { get; init; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public string GeneratedAt { get; init; } = string.Empty;

    [JsonPropertyName("checks")]
    public List<DoctorCheck> Checks { get; } = [];

    [JsonPropertyName("failureCount")]
    public int FailureCount => Checks.Count(check => string.Equals(check.Status, "FAIL", StringComparison.OrdinalIgnoreCase));
}

public sealed class PluginInspectionReport
{
    [JsonPropertyName("repositoryRoot")]
    public string RepositoryRoot { get; init; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public string GeneratedAt { get; init; } = string.Empty;

    [JsonPropertyName("pluginCount")]
    public int PluginCount => Plugins.Count;

    [JsonPropertyName("plugins")]
    public List<PluginInspectionItem> Plugins { get; init; } = [];
}

public sealed class PluginInspectionItem
{
    [JsonPropertyName("pluginId")]
    public string PluginId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("minLLTVersion")]
    public string MinLltVersion { get; init; } = string.Empty;

    [JsonPropertyName("folderName")]
    public string FolderName { get; init; } = string.Empty;

    [JsonPropertyName("directoryPath")]
    public string DirectoryPath { get; init; } = string.Empty;

    [JsonPropertyName("manifestPath")]
    public string ManifestPath { get; init; } = string.Empty;

    [JsonPropertyName("projectPath")]
    public string? ProjectPath { get; init; }

    [JsonPropertyName("testProjectPath")]
    public string? TestProjectPath { get; init; }

    [JsonPropertyName("changelogPath")]
    public string? ChangelogPath { get; init; }

    [JsonPropertyName("storeEntryPath")]
    public string? StoreEntryPath { get; init; }

    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; init; } = string.Empty;

    [JsonPropertyName("expectedAssemblyPath")]
    public string ExpectedAssemblyPath { get; init; } = string.Empty;

    [JsonPropertyName("hasBuildOutput")]
    public bool HasBuildOutput { get; init; }

    [JsonPropertyName("hasPluginAssembly")]
    public bool HasPluginAssembly { get; init; }

    [JsonPropertyName("hasOutputManifest")]
    public bool HasOutputManifest { get; init; }

    [JsonPropertyName("hasTestProject")]
    public bool HasTestProject { get; init; }

    [JsonPropertyName("hasChangelog")]
    public bool HasChangelog { get; init; }

    [JsonPropertyName("hasUnreleasedChangelog")]
    public bool HasUnreleasedChangelog { get; init; }

    [JsonPropertyName("hasStoreEntry")]
    public bool HasStoreEntry { get; init; }

    [JsonPropertyName("storeJsonEntry")]
    public StoreInspectionItem? StoreJsonEntry { get; init; }
}

public sealed class StoreInspectionItem
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("minLLTVersion")]
    public string MinLltVersion { get; init; } = string.Empty;

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; init; } = string.Empty;

    [JsonPropertyName("changelog")]
    public string Changelog { get; init; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; init; }

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; init; } = string.Empty;

    [JsonPropertyName("matchesManifestVersion")]
    public bool MatchesManifestVersion { get; init; }
}

public sealed class ScaffoldRequest
{
    public string RepositoryRoot { get; init; } = string.Empty;
    public PluginArchetype Template { get; init; }
    public string FolderName { get; init; } = string.Empty;
    public string PluginId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Author { get; init; } = Environment.UserName;
    public string Description { get; init; } = string.Empty;
    public string MinimumHostVersion { get; init; } = "3.6.15";
    public string? NamespaceSegment { get; init; }
    public string? ClassPrefix { get; init; }
    public bool Official { get; init; }
}

public sealed record ScaffoldResult(string PluginDirectory, string TestDirectory, string ProjectPath, string TestProjectPath, string? StoreEntryPath);

public sealed class PackRequest
{
    public string RepositoryRoot { get; init; } = string.Empty;
    public string PluginId { get; init; } = string.Empty;
    public string Configuration { get; init; } = "Release";
    public string? OutputDirectory { get; init; }
    public bool BuildFirst { get; init; }
}

public sealed record PackResult(string ZipPath, string AssetName, long FileSize);

public sealed class PromoteRequest
{
    public string RepositoryRoot { get; init; } = string.Empty;
    public string PluginId { get; init; } = string.Empty;
    public bool Overwrite { get; init; }
}

public sealed record PromoteResult(string StoreEntryPath, bool Created);

public sealed class StoreGenerationRequest
{
    public string RepositoryRoot { get; init; } = string.Empty;
    public string? OutputPath { get; init; }
    public string ReleaseRepositoryUrl { get; init; } = "https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases";
    public string? AssetRoot { get; init; }
    public IReadOnlyList<string> PluginIds { get; init; } = Array.Empty<string>();
    public DateTimeOffset? ReleaseDate { get; init; }
}

public sealed record StoreCheckResult(string StorePath, bool Matches, string Message);

public sealed class ArchetypeDefinition
{
    public string Name { get; init; } = string.Empty;
    public bool HasFeaturePage { get; init; }
    public bool HasSettingsPage { get; init; }
    public bool HasRuntime { get; init; }
    public bool HasOptimizationCategory { get; init; }
}
