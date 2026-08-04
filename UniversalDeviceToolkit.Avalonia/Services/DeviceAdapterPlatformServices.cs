using System.Globalization;
using UniversalDeviceToolkit.Abstractions.Hardware;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Presents a machine adapter snapshot through the Avalonia dashboard contract.
/// </summary>
public sealed class DeviceAdapterPlatformServices(IDeviceAdapter adapter) : IPlatformServices
{
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
            FormatCapabilityStatus(capability))));
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
            "Actions" => "hardware-identity",
            "Macro" => "keyboard-backlight",
            "WindowsOptimization" => "read-only-telemetry",
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

    public Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync() =>
        Task.FromResult(new AutomationWorkspaceState(false, Array.Empty<AutomationPipelineItem>()));

    public Task<IReadOnlyList<AutomationTriggerOption>> GetAutomationTriggerOptionsAsync() =>
        Task.FromResult<IReadOnlyList<AutomationTriggerOption>>(Array.Empty<AutomationTriggerOption>());

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
            FormatCapabilityStatus(capability))));
        return groups;
    }

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
