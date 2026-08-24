using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.SDK;
using UniversalDeviceToolkit.Plugins.Core;
using UniversalDeviceToolkit.Plugins.ViveTool.Resources;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;

namespace UniversalDeviceToolkit.Plugins.ViveTool;

[Plugin(
    id: "vive-tool",
    name: "ViVeTool",
    version: "2.0.0",
    description: "Manage Windows feature flags using ViVeTool",
    author: "SSC-STUDIO",
    MinimumHostVersion = "6.0.0",
    Icon = "Code24"
)]
public class ViveToolPlugin : UniversalDeviceToolkit.Plugins.SDK.PluginBase
{
    private readonly ViveToolService _service = new();

    static ViveToolPlugin()
    {
        PluginLog.Configure(
            isTraceEnabled: () => UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled,
            trace: (message, exception) => UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(message, exception));
    }

    public override string Id => "vive-tool";
    public override string Name => Resource.ViveTool_PageTitle;
    public override string Description => Resource.ViveTool_PageDescription;
    public override string Icon => "Code24";
    public override bool IsSystemPlugin => false;

    public override object? GetFeatureExtension()
    {
        return null;
    }

    public override object? GetSettingsPage()
    {
        return null;
    }

    public override void OnShutdown()
    {
        _service.Dispose();
        base.OnShutdown();
    }

    public async Task<object> GetBridgeStatusAsync()
    {
        return new
        {
            available = await _service.IsViveToolAvailableAsync().ConfigureAwait(false),
            path = await _service.GetViveToolPathAsync().ConfigureAwait(false),
            version = await _service.GetViveToolVersionAsync().ConfigureAwait(false),
        };
    }

    public async Task<IReadOnlyList<object>> ListFeaturesForBridgeAsync()
    {
        var features = await _service.ListFeaturesAsync().ConfigureAwait(false);
        return ProjectFeatures(features);
    }

    public async Task<IReadOnlyList<object>> SearchFeaturesForBridgeAsync(string keyword)
    {
        var features = await _service.SearchFeaturesAsync(keyword).ConfigureAwait(false);
        return ProjectFeatures(features);
    }

    public Task<bool> EnableFeatureAsync(int featureId) => _service.EnableFeatureAsync(featureId);

    public Task<bool> DisableFeatureAsync(int featureId) => _service.DisableFeatureAsync(featureId);

    public void RefreshFeatures() => _service.ClearFeatureCache();

    public Task<bool> SetViveToolPathAsync(string filePath) => _service.SetViveToolPathAsync(filePath);

    public Task<bool> DownloadViveToolAsync(IProgress<long>? progress = null) =>
        _service.DownloadViveToolAsync(progress);

    public Task<IReadOnlyList<object>> ImportFeaturesFromFileAsync(string filePath) =>
        ProjectFeaturesAsync(_service.ImportFeaturesFromFileAsync(filePath));

    public Task<IReadOnlyList<object>> ImportFeaturesFromUrlAsync(string url) =>
        ProjectFeaturesAsync(_service.ImportFeaturesFromUrlAsync(url));

    public async Task<bool> ExportFeaturesToFileAsync(string filePath)
    {
        var features = await _service.ListFeaturesAsync().ConfigureAwait(false);
        return await _service.ExportFeaturesToFileAsync(filePath, features).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<object>> ProjectFeaturesAsync(Task<List<FeatureFlagInfo>> task)
    {
        var features = await task.ConfigureAwait(false);
        return ProjectFeatures(features);
    }

    private static IReadOnlyList<object> ProjectFeatures(IReadOnlyCollection<FeatureFlagInfo> features)
    {
        return features.Select(static feature => (object)new
        {
            id = feature.Id,
            name = feature.Name,
            status = feature.Status.ToString(),
            description = feature.Description,
        }).ToList();
    }
}
