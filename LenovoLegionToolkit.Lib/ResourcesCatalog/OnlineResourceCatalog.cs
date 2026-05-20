using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LenovoLegionToolkit.Lib.ResourcesCatalog;

public sealed record OnlineResourceCatalog
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; init; } = string.Empty;

    [JsonPropertyName("languages")]
    public IReadOnlyCollection<OnlineLanguageResource> Languages { get; init; } = [];

    [JsonPropertyName("devicePacks")]
    public IReadOnlyCollection<OnlineDevicePackResource> DevicePacks { get; init; } = [];
}

public sealed record OnlineLanguageResource
{
    [JsonPropertyName("culture")]
    public string Culture { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }
}

public sealed record OnlineDevicePackResource
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("vendor")]
    public string Vendor { get; init; } = string.Empty;

    [JsonPropertyName("families")]
    public IReadOnlyCollection<string> Families { get; init; } = [];

    [JsonPropertyName("modelPrefixes")]
    public IReadOnlyCollection<string> ModelPrefixes { get; init; } = [];

    [JsonPropertyName("machineTypes")]
    public IReadOnlyCollection<string> MachineTypes { get; init; } = [];

    [JsonPropertyName("modelKeywords")]
    public IReadOnlyCollection<string> ModelKeywords { get; init; } = [];

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }
}
