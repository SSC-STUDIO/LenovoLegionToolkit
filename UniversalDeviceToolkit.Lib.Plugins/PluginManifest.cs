using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin manifest model for online plugin store
/// </summary>
public class PluginManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("usageGuide")]
    public string? UsageGuide { get; set; }

    [JsonPropertyName("localizations")]
    public Dictionary<string, PluginManifestLocalization>? Localizations { get; set; }

    [JsonPropertyName("store")]
    public PluginManifestStore? Store { get; set; }

    [JsonPropertyName("contributes")]
    public PluginManifestContributions? Contributes { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("iconBackground")]
    public string? IconBackground { get; set; }

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("minimumHostVersion")]
    public string MinimumHostVersion { get; set; } = "1.0.0";

    // Backward compatibility: some store manifests still use minLLTVersion.
    [JsonPropertyName("minLLTVersion")]
    public string? LegacyMinimumHostVersion
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                MinimumHostVersion = value;
            }
        }
    }

    [JsonPropertyName("dependencies")]
    public string[]? Dependencies { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("fileHash")]
    public string FileHash { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonPropertyName("changelog")]
    public string? Changelog { get; set; }

    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }

    [JsonPropertyName("isSystemPlugin")]
    public bool IsSystemPlugin { get; set; }

    [JsonPropertyName("localizedNames")]
    public Dictionary<string, string>? LocalizedNames { get; set; }

    [JsonPropertyName("localizedDescriptions")]
    public Dictionary<string, string>? LocalizedDescriptions { get; set; }

    [JsonPropertyName("localizedTags")]
    public Dictionary<string, string[]>? LocalizedTags { get; set; }

    /// <summary>
    /// Store lifecycle: <c>Active</c> (default), <c>Offline</c>, <c>Removed</c>.
    /// Offline/Removed entries are hidden from the in-app marketplace.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// When true, the package is migration-only and must not be offered for install.
    /// </summary>
    [JsonPropertyName("internalMigrationOnly")]
    public bool InternalMigrationOnly { get; set; }

    /// <summary>True when this catalog entry should appear in the store UI.</summary>
    [JsonIgnore]
    public bool IsListedInStore
    {
        get
        {
            if (InternalMigrationOnly)
                return false;

            if (string.IsNullOrWhiteSpace(Status))
                return true;

            return Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                   || Status.Equals("Online", StringComparison.OrdinalIgnoreCase);
        }
    }
}

public class PluginManifestStore
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("usageGuide")]
    public string? UsageGuide { get; set; }

    [JsonPropertyName("localizations")]
    public Dictionary<string, PluginManifestLocalization>? Localizations { get; set; }

    [JsonPropertyName("localizedNames")]
    public Dictionary<string, string>? LocalizedNames { get; set; }

    [JsonPropertyName("localizedDescriptions")]
    public Dictionary<string, string>? LocalizedDescriptions { get; set; }

    [JsonPropertyName("localizedTags")]
    public Dictionary<string, string[]>? LocalizedTags { get; set; }

    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }
}

public class PluginManifestLocalization
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("usageGuide")]
    public string? UsageGuide { get; set; }
}

public class PluginManifestContributions
{
    [JsonPropertyName("featurePage")]
    public PluginManifestPageContribution? FeaturePage { get; set; }

    [JsonPropertyName("settingsPage")]
    public PluginManifestPageContribution? SettingsPage { get; set; }

    [JsonPropertyName("runtime")]
    public PluginManifestRuntimeContribution? Runtime { get; set; }

    [JsonPropertyName("optimizationActions")]
    public List<PluginManifestOptimizationContribution>? OptimizationActions { get; set; }
}

public class PluginManifestPageContribution
{
    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

public class PluginManifestRuntimeContribution
{
    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;
}

public class PluginManifestOptimizationContribution
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("recommended")]
    public bool? Recommended { get; set; }
}

/// <summary>
/// Plugin store response containing list of plugins
/// </summary>
public class PluginStoreResponse
{
    [JsonPropertyName("plugins")]
    public List<PluginManifest> Plugins { get; set; } = new();

    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("storeVersion")]
    public string StoreVersion { get; set; } = "1.0.0";

    // Backward compatibility: some generated store manifests still emit "version" at the root.
    [JsonPropertyName("version")]
    public string? LegacyStoreVersion
    {
        get => null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                StoreVersion = value;
            }
        }
    }
}

/// <summary>
/// Plugin download progress info
/// </summary>
public class PluginDownloadProgress
{
    public string PluginId { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double ProgressPercentage { get; set; }
    public bool IsCompleted { get; set; }
    public string? ErrorMessage { get; set; }
    public string? LocalFilePath { get; set; }
}

/// <summary>
/// GitHub API file response model
/// </summary>
public class GitHubFileResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("git_url")]
    public string GitUrl { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = string.Empty;
}
