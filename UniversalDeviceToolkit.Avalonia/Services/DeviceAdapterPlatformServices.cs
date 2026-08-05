using System.Globalization;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Avalonia;
using UniversalDeviceToolkit.Shared.Settings;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Presents a machine adapter snapshot through the Avalonia dashboard contract.
/// </summary>
public sealed class DeviceAdapterPlatformServices(IDeviceAdapter adapter) : IPlatformServices
{
    private readonly AvaloniaDashboardPreferences _dashboardPreferences = new();
    private readonly SemaphoreSlim _snapshotLock = new(1, 1);
    private DeviceSnapshot? _snapshot;

    public async Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync()
    {
        var snapshot = await ReadSnapshotAsync(forceRefresh: false).ConfigureAwait(false);
        var groups = new List<FeatureGroupItem>
        {
            new(
                DashboardLocalization.Get("Dashboard_Feature_Device", "Device"),
                FormatIdentity(snapshot.Identity),
                snapshot.Support.DisplayName),
            new(
                DashboardLocalization.Get("Dashboard_Feature_DevicePack", "Device support"),
                snapshot.Support.DevicePackId,
                snapshot.Support.SupportLevel),
        };

        groups.AddRange(snapshot.Capabilities.Select(capability => new FeatureGroupItem(
            Humanize(capability.Id),
            capability.Reason,
            FormatCapabilityStatus(capability),
            GetFeatureRoute(capability.Id))));
        return groups;
    }

    public async Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync()
    {
        var snapshot = await ReadSnapshotAsync(forceRefresh: false).ConfigureAwait(false);
        if (snapshot.SensorReadings.Count == 0)
            return Array.Empty<SensorReadingItem>();

        return snapshot.SensorReadings
            .Select(reading => new SensorReadingItem(
                reading.Name,
                $"{reading.Value.ToString("0.##", CultureInfo.InvariantCulture)} {reading.Unit}".Trim(),
                reading.Category,
                reading.Value,
                reading.Unit))
            .ToArray();
    }

    public async Task<bool> IsSupportedLegionMachineAsync()
    {
        var snapshot = await ReadSnapshotAsync(forceRefresh: false).ConfigureAwait(false);
        return snapshot.Support.IsHardwareControlAvailable &&
               snapshot.Support.DevicePackId.Contains("legion", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<FeaturePageState> GetFeaturePageStateAsync(string routeKey)
    {
        var snapshot = await ReadSnapshotAsync(forceRefresh: true).ConfigureAwait(false);
        var capabilityId = routeKey switch
        {
            "Keyboard" => "keyboard-backlight",
            // Actions and Macro are host automation surfaces, not generic device
            // capabilities. The portable adapter must not make them appear
            // available just because it can read hardware identity or telemetry.
            "Actions" => "actions",
            "Macro" => "macro-controller",
            "WindowsOptimization" => "windows-optimization",
            "PluginExtensions" => "plugin-extensions",
            _ => string.Empty,
        };
        var capability = snapshot.Capabilities.FirstOrDefault(item =>
            item.Id.Equals(capabilityId, StringComparison.OrdinalIgnoreCase));
        var isAvailable = capability?.IsAvailable == true &&
                          (routeKey is not "Actions" || capability.CanWrite);
        var statusMessage = capability?.Reason
            ?? DashboardLocalization.Get("FeaturePage_AdapterUnavailable", "The platform adapter did not report this feature.");

        var actions = routeKey switch
        {
            "Keyboard" => BuildActions(
                "keyboard-backlight",
                "Keyboard backlight",
                "Read the detected keyboard backlight capability and supported hardware state.",
                capability,
                isToggle: false),
            "Actions" => snapshot.Capabilities
                .Select(item => new FeatureActionItem(
                    item.Id,
                    Humanize(item.Id),
                    item.Reason,
                    FormatCapabilityStatus(item),
                    item.CanWrite,
                    false,
                    false))
                .ToArray(),
            "Macro" => BuildActions(
                "macro-controller",
                "Macro controller",
                "Macro input requires the host macro controller and input permissions.",
                null,
                isToggle: true),
            "WindowsOptimization" => BuildActions(
                "optimization-service",
                "Windows optimization service",
                "Optimization actions are exposed only through the Windows optimization service.",
                capability,
                isToggle: false),
            "PluginExtensions" => BuildActions(
                "plugin-extensions",
                "Plugin extension manager",
                "Plugin discovery and installation require the plugin host service.",
                capability,
                isToggle: false),
            _ => [],
        };

        return new FeaturePageState(
            routeKey,
            Humanize(routeKey),
            DashboardLocalization.Get("FeaturePage_PlatformDescription", "This feature is provided by the host platform adapter."),
            isAvailable
                ? DashboardLocalization.Get("FeaturePage_Available", "Available")
                : DashboardLocalization.Get("FeaturePage_Unsupported", "Unavailable on this device"),
            statusMessage,
            isAvailable,
            actions);
    }

    public Task<bool> SetFeatureActionAsync(string routeKey, string actionKey, bool isSelected) =>
        Task.FromResult(false);

    public Task<bool> ImportPluginAsync(string zipFilePath) => Task.FromResult(false);

    public Task<PluginCatalogState> GetPluginCatalogAsync(bool forceRefresh = false) =>
        Task.FromResult(new PluginCatalogState(
            false,
            DashboardLocalization.Get("PluginExtensionsPage_StoreUnavailableMessage", "Plugin store requires the host plugin service."),
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
            DashboardLocalization.Get("PluginPage_AdapterDescription", "Plugin pages require the host plugin service."),
            null,
            false,
            false,
            false,
            DashboardLocalization.Get("PluginPage_AdapterUnavailable", "The platform adapter cannot host plugin pages.")));

    public Task<PluginPageState> GetPluginSettingsPageStateAsync(string pluginId) =>
        Task.FromResult(new PluginPageState(
            pluginId,
            pluginId,
            DashboardLocalization.Get("PluginPage_AdapterDescription", "Plugin settings require the host plugin service."),
            null,
            false,
            false,
            false,
            DashboardLocalization.Get("PluginPage_AdapterUnavailable", "The platform adapter cannot host plugin settings.")));

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
            DashboardLocalization.Get("NetworkAcceleration_AdapterUnavailable", "Network acceleration requires the Windows host."),
            0,
            Array.Empty<NetworkAccelerationGroupState>()));

    public Task<bool> SetNetworkAccelerationEnabledAsync(bool enabled) => Task.FromResult(false);
    public Task<bool> SetNetworkAccelerationModeAsync(string mode) => Task.FromResult(false);
    public Task<bool> SetNetworkAccelerationGroupEnabledAsync(string groupId, bool enabled) => Task.FromResult(false);
    public Task<bool> ToggleNetworkAccelerationAsync() => Task.FromResult(false);
    public Task<string> RunNetworkDiagnosticsAsync() =>
        Task.FromResult(DashboardLocalization.Get("NetworkAcceleration_AdapterUnavailable", "Network acceleration requires the Windows host."));

    public Task<DriverDownloadState> GetDriverDownloadStateAsync() =>
        Task.FromResult(new DriverDownloadState(
            false,
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<DriverPackageItem>(),
            DashboardLocalization.Get("DriverDownload_AdapterUnavailable", "Driver downloads require the Windows host.")));

    public Task<DriverDownloadState> SearchDriverPackagesAsync(string source, string machineType, string os, bool onlyUpdates) =>
        GetDriverDownloadStateAsync();

    public Task<bool> DownloadDriverPackageAsync(string packageId, string destinationFolder) => Task.FromResult(false);

    private static IReadOnlyList<FeatureActionItem> BuildActions(
        string key,
        string title,
        string description,
        DeviceCapability? capability,
        bool isToggle) =>
        [new FeatureActionItem(
            key,
            title,
            description,
            capability is null
                ? DashboardLocalization.Get("Dashboard_Status_NotSupported", "Not supported")
                : FormatCapabilityStatus(capability),
            capability?.CanWrite == true,
            false,
            isToggle)];

    public async Task<DashboardSnapshot> GetDashboardSnapshotAsync()
    {
        var snapshot = await ReadSnapshotAsync(forceRefresh: true).ConfigureAwait(false);
        var featureGroups = BuildFeatureGroups(snapshot);
        var sensors = BuildSensorReadings(snapshot);
        return new DashboardSnapshot(
            FormatIdentity(snapshot.Identity),
            snapshot.Support.DisplayName,
            string.IsNullOrWhiteSpace(snapshot.PowerStatus)
                ? DashboardLocalization.Get("Dashboard_Status_NoPowerTelemetry", "No power telemetry available")
                : snapshot.PowerStatus!,
            featureGroups,
            sensors,
            DateTimeOffset.UtcNow);
    }

    public Task<DashboardLayoutState> GetDashboardLayoutAsync()
    {
        var store = _dashboardPreferences.Store;
        return Task.FromResult(ToLayoutState(store));
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
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(identifier => new DashboardItemState(
                    identifier,
                    false,
                    null,
                    Array.Empty<string>(),
                    "This device adapter does not expose dashboard controls."))
                .ToArray());

    public Task<bool> SetDashboardItemStateAsync(string itemIdentifier, string state) =>
        Task.FromResult(false);

    public Task<DiscreteGpuState> GetDiscreteGpuStateAsync() =>
        Task.FromResult(new DiscreteGpuState(
            false,
            DashboardLocalization.Get("Dashboard_Status_Unavailable", "Unavailable"),
            string.Empty,
            0,
            false,
            false,
            DashboardLocalization.Get(
                "Dashboard_AdapterControlsUnavailable",
                "The platform adapter does not expose GPU controls.")));

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
            DashboardLocalization.Get(
                "Dashboard_AdapterControlsUnavailable",
                "The platform adapter does not expose GPU controls.")));

    public Task<bool> SetGpuOverclockAsync(bool enabled, int coreDeltaMhz, int memoryDeltaMhz) =>
        Task.FromResult(false);

    private async Task<DeviceSnapshot> ReadSnapshotAsync(bool forceRefresh)
    {
        if (!forceRefresh && _snapshot is not null)
            return _snapshot;

        await _snapshotLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _snapshot = await adapter.ReadSnapshotAsync().ConfigureAwait(false);
            return _snapshot;
        }
        catch (Exception ex)
        {
            _snapshot = CreateUnavailableSnapshot(ex.Message);
            return _snapshot;
        }
        finally
        {
            _snapshotLock.Release();
        }
    }

    private DeviceSnapshot CreateUnavailableSnapshot(string reason) => new(
        DeviceIdentity.Unknown(adapter.PlatformId, "adapter"),
        new DeviceSupportInfo(
            "Safe basic mode",
            DeviceSupportMatcher.GenericBasicPackId,
            "Generic PC Basic",
            ["diagnostics"],
            ["lenovo-hardware-controls", "power-modes", "fan-curve", "keyboard-backlight"],
            reason),
        [DeviceCapability.Unavailable("hardware-identity", reason, adapter.PlatformId)],
        [],
        null,
        adapter.PlatformId);

    private static string FormatIdentity(DeviceIdentity identity)
    {
        var value = string.Join(" ", new[] { identity.Vendor, identity.ProductName, identity.Model }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(value)
            ? DashboardLocalization.Get("Dashboard_Status_Unknown", "Unknown")
            : value;
    }

    private static string FormatCapabilityStatus(DeviceCapability capability) =>
        !capability.IsAvailable
            ? DashboardLocalization.Get("Dashboard_Status_NotSupported", "Not supported")
            : capability.CanWrite
                ? DashboardLocalization.Get("Dashboard_Status_Available", "Available")
                : DashboardLocalization.Get("Dashboard_Status_ReadOnly", "Read-only");

    private static string Humanize(string value) =>
        string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));

    private IReadOnlyList<FeatureGroupItem> BuildFeatureGroups(DeviceSnapshot snapshot)
    {
        var groups = new List<FeatureGroupItem>
        {
            new(
                DashboardLocalization.Get("Dashboard_Feature_Device", "Device"),
                FormatIdentity(snapshot.Identity),
                snapshot.Support.DisplayName),
            new(
                DashboardLocalization.Get("Dashboard_Feature_DevicePack", "Device support"),
                snapshot.Support.DevicePackId,
                snapshot.Support.SupportLevel),
        };

        groups.AddRange(snapshot.Capabilities.Select(capability => new FeatureGroupItem(
            Humanize(capability.Id),
            capability.Reason,
            FormatCapabilityStatus(capability),
            GetFeatureRoute(capability.Id))));
        return groups;
    }

    private static string? GetFeatureRoute(string capabilityId) => capabilityId.ToLowerInvariant() switch
    {
        "keyboard-backlight" => MainNavigation.Keyboard,
        "plugin-extensions" => MainNavigation.PluginExtensions,
        _ => null,
    };

    private static DashboardLayoutState ToLayoutState(AvaloniaDashboardPreferenceStore store) =>
        new(
            store.ShowSensors,
            store.SensorsRefreshIntervalSeconds,
            store.Groups.Select(group => new DashboardGroupState(
                group.Type,
                group.CustomName,
                group.Items.ToArray())).ToArray());

    private IReadOnlyList<SensorReadingItem> BuildSensorReadings(DeviceSnapshot snapshot)
    {
        if (snapshot.SensorReadings.Count == 0)
            return Array.Empty<SensorReadingItem>();

        return snapshot.SensorReadings
            .Select(reading => new SensorReadingItem(
                reading.Name,
                $"{reading.Value.ToString("0.##", CultureInfo.InvariantCulture)} {reading.Unit}".Trim(),
                reading.Category,
                reading.Value,
                reading.Unit))
            .ToArray();
    }
}
