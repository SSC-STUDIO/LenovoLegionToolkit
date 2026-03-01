using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LenovoLegionToolkit.Lib.Plugins;

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
