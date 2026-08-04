using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Empty production fallback for runtimes without a registered adapter.
/// </summary>
public sealed class UnavailablePlatformServices : IPlatformServices
{
    public Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync() =>
        Task.FromResult<IReadOnlyList<FeatureGroupItem>>(
        [
            new(
                AvaloniaLocalization.GetString("Dashboard_Feature_SystemTelemetry", "System Telemetry"),
                AvaloniaLocalization.GetString(
                    "Dashboard_Description_NoAdapter",
                    "No platform adapter is registered for telemetry."),
                AvaloniaLocalization.GetString(
                    "Dashboard_Status_NoTelemetry",
                    "No telemetry available")),
        ]);

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync() =>
        Task.FromResult<IReadOnlyList<SensorReadingItem>>([]);

    public async Task<DashboardSnapshot> GetDashboardSnapshotAsync() =>
        new(
            AvaloniaLocalization.GetString("Dashboard_Status_Unknown", "Unknown device"),
            AvaloniaLocalization.GetString("Dashboard_Status_NoTelemetry", "No telemetry available"),
            AvaloniaLocalization.GetString("Dashboard_Status_NoPowerTelemetry", "No power telemetry available"),
            await GetFeatureGroupsAsync(),
            await GetSensorReadingsAsync(),
            DateTimeOffset.UtcNow);

    public Task<bool> IsSupportedLegionMachineAsync() => Task.FromResult(false);

    public Task<FeaturePageState> GetFeaturePageStateAsync(string routeKey) =>
        Task.FromResult(new FeaturePageState(
            routeKey,
            routeKey,
            AvaloniaLocalization.GetString("FeaturePage_PlatformDescription", "This feature is provided by the host platform adapter."),
            AvaloniaLocalization.GetString("FeaturePage_Unsupported", "Unavailable on this device"),
            AvaloniaLocalization.GetString("FeaturePage_AdapterUnavailable", "No platform adapter is registered for this host."),
            false,
            [new FeatureActionItem(
                "adapter",
                AvaloniaLocalization.GetString("FeaturePage_AdapterAction", "Platform adapter"),
                AvaloniaLocalization.GetString("FeaturePage_AdapterActionDescription", "Install or enable a compatible platform adapter to use this feature."),
                AvaloniaLocalization.GetString("Dashboard_Status_NotSupported", "Not supported"),
                false,
                false,
                false)]));

    public Task<bool> SetFeatureActionAsync(string routeKey, string actionKey, bool isSelected) =>
        Task.FromResult(false);

    public Task<PluginPageState> GetPluginPageStateAsync(string pluginId) =>
        Task.FromResult(new PluginPageState(
            pluginId,
            pluginId,
            AvaloniaLocalization.GetString("PluginPage_AdapterDescription", "Plugin pages require the host plugin service."),
            null,
            false,
            false,
            false,
            AvaloniaLocalization.GetString("PluginPage_AdapterUnavailable", "No platform adapter can host plugin pages.")));

    public Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync() =>
        Task.FromResult(new AutomationWorkspaceState(false, Array.Empty<AutomationPipelineItem>()));

    public Task<bool> SetAutomationEnabledAsync(bool enabled) => Task.FromResult(false);

    public Task<bool> SaveAutomationWorkspaceAsync(IReadOnlyList<AutomationPipelineDraft> pipelines) =>
        Task.FromResult(false);

    public Task<KeyboardLightingState?> GetKeyboardLightingStateAsync() =>
        Task.FromResult<KeyboardLightingState?>(null);

    public Task<bool> SetKeyboardLightingAsync(KeyboardLightingUpdate update) =>
        Task.FromResult(false);
}
