using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;

namespace LenovoLegionToolkit.Plugins.ViveTool.Services;

/// <summary>
/// ViVeTool service interface for managing Windows feature flags
/// </summary>
public interface IViveToolService
{
    /// <summary>
    /// Check if vivetool.exe is available
    /// </summary>
    public Task<bool> IsViveToolAvailableAsync();

    /// <summary>
    /// Get the path to vivetool.exe
    /// </summary>
    public Task<string?> GetViveToolPathAsync();

    /// <summary>
    /// Enable a feature flag by ID
    /// </summary>
    public Task<bool> EnableFeatureAsync(int featureId);

    /// <summary>
    /// Disable a feature flag by ID
    /// </summary>
    public Task<bool> DisableFeatureAsync(int featureId);

    /// <summary>
    /// Get the status of a feature flag
    /// </summary>
    public Task<FeatureFlagStatus?> GetFeatureStatusAsync(int featureId);

    /// <summary>
    /// List all feature flags
    /// </summary>
    public Task<List<FeatureFlagInfo>> ListFeaturesAsync();

    /// <summary>
    /// Search for feature flags by keyword
    /// </summary>
    public Task<List<FeatureFlagInfo>> SearchFeaturesAsync(string keyword);

    /// <summary>
    /// Import feature flags from a file
    /// </summary>
    public Task<List<FeatureFlagInfo>> ImportFeaturesFromFileAsync(string filePath);

    /// <summary>
    /// Import feature flags from a URL
    /// </summary>
    public Task<List<FeatureFlagInfo>> ImportFeaturesFromUrlAsync(string url);

    /// <summary>
    /// Set the path to vivetool.exe manually
    /// </summary>
    public Task<bool> SetViveToolPathAsync(string filePath);

    /// <summary>
    /// Download and install ViVeTool with progress reporting
    /// </summary>
    public Task<bool> DownloadViveToolAsync(System.IProgress<long>? progress = null);

    /// <summary>
    /// Clear the feature cache to force reload on next request
    /// </summary>
    public void ClearFeatureCache();

    /// <summary>
    /// Get the ViVeTool version
    /// </summary>
    public Task<string?> GetViveToolVersionAsync();

    /// <summary>
    /// Export feature flags to a file
    /// </summary>
    public Task<bool> ExportFeaturesToFileAsync(string filePath, IReadOnlyCollection<FeatureFlagInfo> features);
}

/// <summary>
/// Feature flag information
/// </summary>
public class FeatureFlagInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public FeatureFlagStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Feature flag status
/// </summary>
public enum FeatureFlagStatus
{
    Unknown,
    Enabled,
    Disabled,
    Default
}
