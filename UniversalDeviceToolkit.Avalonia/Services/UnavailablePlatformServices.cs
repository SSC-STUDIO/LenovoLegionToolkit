using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Shared.Settings;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Empty production fallback for runtimes without a registered adapter.
/// </summary>
public sealed class UnavailablePlatformServices : IPlatformServices
{
    private readonly AvaloniaDashboardPreferences _dashboardPreferences = new();
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

    public Task<DashboardLayoutState> GetDashboardLayoutAsync()
    {
        var store = _dashboardPreferences.Store;
        return Task.FromResult(new DashboardLayoutState(
            store.ShowSensors,
            store.SensorsRefreshIntervalSeconds,
            store.Groups.Select(group => new DashboardGroupState(
                group.Type,
                group.CustomName,
                group.Items.ToArray())).ToArray()));
    }

    public Task<bool> SaveDashboardLayoutAsync(DashboardLayoutState layout)
    {
        if (layout is null)
            return Task.FromResult(false);

        var store = _dashboardPreferences.Store;
        store.ShowSensors = layout.ShowSensors;
        store.SensorsRefreshIntervalSeconds = Math.Clamp(layout.SensorsRefreshIntervalSeconds, 1, 60);
        store.Groups = layout.Groups
            .Where(group => group is not null && !string.IsNullOrWhiteSpace(group.Type))
            .Select(group => new AvaloniaDashboardGroupPreference
            {
                Type = group.Type,
                CustomName = group.CustomName,
                Items = (group.Items ?? []).ToList(),
            })
            .ToList();
        _dashboardPreferences.SynchronizeStore();
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<DashboardItemState>> GetDashboardItemStatesAsync(
        IReadOnlyList<string> itemIdentifiers) =>
        Task.FromResult<IReadOnlyList<DashboardItemState>>(
            itemIdentifiers
                .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
                .Where(identifier => !DashboardItemStateRouting.IsDedicatedControl(identifier))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(identifier => new DashboardItemState(
                    identifier,
                    false,
                    null,
                    Array.Empty<string>(),
                    "No platform adapter is registered for dashboard controls."))
                .ToArray());

    public Task<bool> SetDashboardItemStateAsync(string itemIdentifier, string state) =>
        Task.FromResult(false);

    public Task<DiscreteGpuState> GetDiscreteGpuStateAsync() =>
        Task.FromResult(new DiscreteGpuState(
            false,
            AvaloniaLocalization.GetString("Dashboard_Status_Unavailable", "Unavailable"),
            string.Empty,
            0,
            false,
            false,
            AvaloniaLocalization.GetString(
                "Dashboard_AdapterControlsUnavailable",
                "No platform adapter is registered for GPU controls.")));

    public Task<bool> KillDiscreteGpuProcessesAsync() => Task.FromResult(false);

    public Task<bool> RestartDiscreteGpuAsync() => Task.FromResult(false);

    public Task<bool> TurnOffMonitorsAsync() => Task.FromResult(false);

    public Task<GpuOverclockState> GetGpuOverclockStateAsync() =>
        Task.FromResult(new GpuOverclockState(
            false,
            false,
            0,
            0,
            0,
            0,
            AvaloniaLocalization.GetString(
                "Dashboard_AdapterControlsUnavailable",
                "No platform adapter is registered for GPU controls.")));

    public Task<bool> SetGpuOverclockAsync(bool enabled, int coreDeltaMhz, int memoryDeltaMhz) =>
        Task.FromResult(false);

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

    public Task<bool> ImportPluginAsync(string zipFilePath) => Task.FromResult(false);

    public Task<PluginCatalogState> GetPluginCatalogAsync(bool forceRefresh = false) =>
        Task.FromResult(new PluginCatalogState(
            false,
            AvaloniaLocalization.GetString("PluginExtensionsPage_StoreUnavailableMessage", "Plugin store is unavailable on this host."),
            Array.Empty<PluginCatalogItem>()));

    public Task<bool> UpdatePluginAsync(string pluginId) => Task.FromResult(false);

    public Task<bool> InstallPluginAsync(string pluginId) => Task.FromResult(false);

    public Task<IReadOnlyList<CustomCleanupRuleItem>> GetCustomCleanupRulesAsync() =>
        Task.FromResult<IReadOnlyList<CustomCleanupRuleItem>>([]);

    public Task<bool> SaveCustomCleanupRulesAsync(IReadOnlyList<CustomCleanupRuleItem> rules) =>
        Task.FromResult(false);

    public Task<MacroWorkspaceState> GetMacroWorkspaceAsync() =>
        Task.FromResult(new MacroWorkspaceState(false, false, Array.Empty<MacroSlotState>()));

    public Task<bool> SetMacroEnabledAsync(bool enabled) => Task.FromResult(false);

    public Task<bool> SetMacroSequenceOptionsAsync(
        ulong key,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey) => Task.FromResult(false);

    public Task<bool> ClearMacroSequenceAsync(ulong key) => Task.FromResult(false);

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

    public Task<PluginPageState> GetPluginSettingsPageStateAsync(string pluginId) =>
        Task.FromResult(new PluginPageState(
            pluginId,
            pluginId,
            AvaloniaLocalization.GetString("PluginPage_AdapterDescription", "Plugin settings require the host plugin service."),
            null,
            false,
            false,
            false,
            AvaloniaLocalization.GetString("PluginPage_AdapterUnavailable", "No platform adapter can host plugin settings.")));

    public Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync() =>
        Task.FromResult(new AutomationWorkspaceState(false, Array.Empty<AutomationPipelineItem>()));

    public Task<IReadOnlyList<AutomationTriggerOption>> GetAutomationTriggerOptionsAsync() =>
        Task.FromResult<IReadOnlyList<AutomationTriggerOption>>(Array.Empty<AutomationTriggerOption>());

    public Task<IReadOnlyList<AutomationStepOption>> GetAutomationStepOptionsAsync() =>
        Task.FromResult<IReadOnlyList<AutomationStepOption>>(Array.Empty<AutomationStepOption>());

    public Task<bool> SetAutomationEnabledAsync(bool enabled) => Task.FromResult(false);

    public Task<bool> SaveAutomationWorkspaceAsync(IReadOnlyList<AutomationPipelineDraft> pipelines) =>
        Task.FromResult(false);

    public Task<KeyboardLightingState?> GetKeyboardLightingStateAsync() =>
        Task.FromResult<KeyboardLightingState?>(null);

    public Task<bool> SetKeyboardLightingAsync(KeyboardLightingUpdate update) =>
        Task.FromResult(false);

    public Task<NetworkAccelerationState> GetNetworkAccelerationStateAsync() =>
        Task.FromResult(new NetworkAccelerationState(
            false,
            false,
            false,
            false,
            "Off",
            AvaloniaLocalization.GetString("NetworkAcceleration_AdapterUnavailable", "Network acceleration is unavailable in this host."),
            0,
            Array.Empty<NetworkAccelerationGroupState>()));

    public Task<bool> SetNetworkAccelerationEnabledAsync(bool enabled) => Task.FromResult(false);
    public Task<bool> SetNetworkAccelerationModeAsync(string mode) => Task.FromResult(false);
    public Task<bool> SetNetworkAccelerationGroupEnabledAsync(string groupId, bool enabled) => Task.FromResult(false);
    public Task<bool> ToggleNetworkAccelerationAsync() => Task.FromResult(false);
    public Task<string> RunNetworkDiagnosticsAsync() =>
        Task.FromResult(AvaloniaLocalization.GetString("NetworkAcceleration_AdapterUnavailable", "Network acceleration is unavailable in this host."));

    public Task<DriverDownloadState> GetDriverDownloadStateAsync() =>
        Task.FromResult(new DriverDownloadState(
            false,
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<DriverPackageItem>(),
            AvaloniaLocalization.GetString("DriverDownload_AdapterUnavailable", "Driver downloads require the Windows host.")));

    public Task<DriverDownloadState> SearchDriverPackagesAsync(string source, string machineType, string os, bool onlyUpdates) =>
        GetDriverDownloadStateAsync();

    public Task<bool> DownloadDriverPackageAsync(string packageId, string destinationFolder) => Task.FromResult(false);
}
