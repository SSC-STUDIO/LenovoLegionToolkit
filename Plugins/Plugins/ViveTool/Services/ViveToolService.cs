using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.ViveTool.Services;

/// <summary>
/// ViVeTool service implementation that aggregates specialized services.
/// This class delegates operations to focused services following Single Responsibility Principle.
/// </summary>
public class ViveToolService : IViveToolService
{
    private readonly ViveToolPathService _pathService;
    private readonly ViveToolFeatureService _featureService;
    private readonly ViveToolDownloadService _downloadService;

    /// <summary>
    /// Initializes a new instance of ViveToolService with required sub-services.
    /// </summary>
    public ViveToolService()
    {
        _pathService = new ViveToolPathService();
        var processService = new ViveToolProcessService();
        _featureService = new ViveToolFeatureService(_pathService, processService);
        _downloadService = new ViveToolDownloadService(_pathService);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace("ViveTool: ViveToolService initialized with specialized services");
    }

    /// <summary>
    /// Check if vivetool.exe is available
    /// </summary>
    public async Task<bool> IsViveToolAvailableAsync()
    {
        var path = await GetViveToolPathAsync().ConfigureAwait(false);
        var available = !string.IsNullOrEmpty(path);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: ViVeTool availability check: {available}");

        return available;
    }

    /// <summary>
    /// Get the path to vivetool.exe
    /// </summary>
    public async Task<string?> GetViveToolPathAsync()
    {
        var path = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: ViVeTool path resolved: {path ?? "(not found)"}");

        return path;
    }

    /// <summary>
    /// Enable a feature flag by ID
    /// </summary>
    public async Task<bool> EnableFeatureAsync(int featureId)
    {
        var success = await _featureService.EnableFeatureAsync(featureId).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Enable feature {featureId}: {success}");

        return success;
    }

    /// <summary>
    /// Disable a feature flag by ID
    /// </summary>
    public async Task<bool> DisableFeatureAsync(int featureId)
    {
        var success = await _featureService.DisableFeatureAsync(featureId).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Disable feature {featureId}: {success}");

        return success;
    }

    /// <summary>
    /// Get the status of a feature flag
    /// </summary>
    public async Task<FeatureFlagStatus?> GetFeatureStatusAsync(int featureId)
    {
        var status = await _featureService.GetFeatureStatusAsync(featureId).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Feature {featureId} status: {status}");

        return status;
    }

    /// <summary>
    /// List all feature flags
    /// </summary>
    public async Task<List<FeatureFlagInfo>> ListFeaturesAsync()
    {
        var features = await _featureService.ListFeaturesAsync().ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Listed {features.Count} features");

        return features;
    }

    /// <summary>
    /// Search for feature flags by keyword
    /// </summary>
    public async Task<List<FeatureFlagInfo>> SearchFeaturesAsync(string keyword)
    {
        var features = await _featureService.SearchFeaturesAsync(keyword).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Found {features.Count} features matching '{keyword}'");

        return features;
    }

    /// <summary>
    /// Import feature flags from a file
    /// </summary>
    public async Task<List<FeatureFlagInfo>> ImportFeaturesFromFileAsync(string filePath)
    {
        var features = await _downloadService.ImportFeaturesFromFileAsync(filePath).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Imported {features.Count} features from file: {filePath}");

        return features;
    }

    /// <summary>
    /// Import feature flags from a URL
    /// </summary>
    public async Task<List<FeatureFlagInfo>> ImportFeaturesFromUrlAsync(string url)
    {
        var features = await _downloadService.ImportFeaturesFromUrlAsync(url).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Imported {features.Count} features from URL: {url}");

        return features;
    }

    /// <summary>
    /// Set the path to vivetool.exe manually
    /// </summary>
    public async Task<bool> SetViveToolPathAsync(string filePath)
    {
        var success = await _pathService.SetViveToolPathAsync(filePath).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Set ViVeTool path: {filePath}, success: {success}");

        return success;
    }

    /// <summary>
    /// Download and install ViVeTool with progress reporting
    /// </summary>
    public async Task<bool> DownloadViveToolAsync(IProgress<long>? progress = null)
    {
        var success = await _downloadService.DownloadViveToolAsync(progress).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Download ViVeTool: {success}");

        return success;
    }

    /// <summary>
    /// Clear the feature cache to force reload on next request
    /// </summary>
    public void ClearFeatureCache()
    {
        _featureService.ClearFeatureCache();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace("ViveTool: Feature cache cleared");
    }

    /// <summary>
    /// Get the ViVeTool version
    /// </summary>
    public async Task<string?> GetViveToolVersionAsync()
    {
        var version = await _featureService.GetViveToolVersionAsync().ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: ViVeTool version: {version ?? "(unknown)"}");

        return version;
    }
}