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
    Task<bool> IsViveToolAvailableAsync();

    /// <summary>
    /// Get the path to vivetool.exe
    /// </summary>
    Task<string?> GetViveToolPathAsync();

    /// <summary>
    /// Enable a feature flag by ID
    /// </summary>
    Task<bool> EnableFeatureAsync(int featureId);

    /// <summary>
    /// Disable a feature flag by ID
    /// </summary>
    Task<bool> DisableFeatureAsync(int featureId);

    /// <summary>
    /// Get the status of a feature flag
    /// </summary>
    Task<FeatureFlagStatus?> GetFeatureStatusAsync(int featureId);

    /// <summary>
    /// List all feature flags
    /// </summary>
    Task<List<FeatureFlagInfo>> ListFeaturesAsync();

    /// <summary>
    /// Search for feature flags by keyword
    /// </summary>
    Task<List<FeatureFlagInfo>> SearchFeaturesAsync(string keyword);

    /// <summary>
    /// Import feature flags from a file
    /// </summary>
    Task<List<FeatureFlagInfo>> ImportFeaturesFromFileAsync(string filePath);

    /// <summary>
    /// Import feature flags from a URL
    /// </summary>
    Task<List<FeatureFlagInfo>> ImportFeaturesFromUrlAsync(string url);

    /// <summary>
    /// Set the path to vivetool.exe manually
    /// </summary>
    Task<bool> SetViveToolPathAsync(string filePath);

    /// <summary>
    /// Download and install ViVeTool with progress reporting
    /// </summary>
    Task<bool> DownloadViveToolAsync(System.IProgress<long>? progress = null);
    
    /// <summary>
    /// Clear the feature cache to force reload on next request
    /// </summary>
    void ClearFeatureCache();
    
    /// <summary>
    /// Get the ViVeTool version
    /// </summary>
    Task<string?> GetViveToolVersionAsync();
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
