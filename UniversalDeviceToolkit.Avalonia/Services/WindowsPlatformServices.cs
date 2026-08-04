#if WINDOWS

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Platform.Windows;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Compatibility facade for callers that used the old Avalonia Windows service.
/// The dashboard now consumes the shared read-only device snapshot.
/// </summary>
public sealed class WindowsPlatformServices : IPlatformServices
{
    private readonly DeviceAdapterPlatformServices _inner;
    private readonly WindowsFeatureHostServices? _featureHost;
    private readonly ISensorsController? _sensorController;

    private WindowsPlatformServices(IDeviceAdapter adapter)
    {
        _inner = new DeviceAdapterPlatformServices(adapter);
        _featureHost = WindowsFeatureHostServices.TryCreate();
        _sensorController = IoCContainer.TryResolve<ISensorsController>();
    }

    public static IPlatformServices Create() => new WindowsPlatformServices(new WindowsDeviceAdapter());

    public Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync() => _inner.GetFeatureGroupsAsync();

    public async Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync()
    {
        var readings = await _inner.GetSensorReadingsAsync().ConfigureAwait(false);
        return await AppendHardwareSensorReadingsAsync(readings).ConfigureAwait(false);
    }

    public async Task<DashboardSnapshot> GetDashboardSnapshotAsync()
    {
        var snapshot = await _inner.GetDashboardSnapshotAsync().ConfigureAwait(false);
        var readings = await AppendHardwareSensorReadingsAsync(snapshot.SensorReadings).ConfigureAwait(false);
        return snapshot with { SensorReadings = readings };
    }

    public Task<bool> IsSupportedLegionMachineAsync() => _inner.IsSupportedLegionMachineAsync();

    public Task<FeaturePageState> GetFeaturePageStateAsync(string routeKey) =>
        _featureHost is null
            ? _inner.GetFeaturePageStateAsync(routeKey)
            : _featureHost.GetStateAsync(routeKey);

    public Task<IReadOnlyList<CustomCleanupRuleItem>> GetCustomCleanupRulesAsync() =>
        _featureHost is null
            ? _inner.GetCustomCleanupRulesAsync()
            : _featureHost.GetCustomCleanupRulesAsync();

    public Task<bool> SaveCustomCleanupRulesAsync(IReadOnlyList<CustomCleanupRuleItem> rules) =>
        _featureHost is null
            ? _inner.SaveCustomCleanupRulesAsync(rules)
            : _featureHost.SaveCustomCleanupRulesAsync(rules);

    public Task<PluginPageState> GetPluginPageStateAsync(string pluginId) =>
        _featureHost is null
            ? _inner.GetPluginPageStateAsync(pluginId)
            : _featureHost.GetPluginPageStateAsync(pluginId);

    public Task<bool> SetFeatureActionAsync(string routeKey, string actionKey, bool isSelected) =>
        _featureHost is null
            ? _inner.SetFeatureActionAsync(routeKey, actionKey, isSelected)
            : _featureHost.SetActionAsync(routeKey, actionKey, isSelected);

    public Task<MacroWorkspaceState> GetMacroWorkspaceAsync() =>
        _featureHost is null
            ? _inner.GetMacroWorkspaceAsync()
            : _featureHost.GetMacroWorkspaceAsync();

    public Task<bool> SetMacroEnabledAsync(bool enabled) =>
        _featureHost is null
            ? _inner.SetMacroEnabledAsync(enabled)
            : _featureHost.SetMacroEnabledAsync(enabled);

    public Task<bool> SetMacroSequenceOptionsAsync(
        ulong key,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey) =>
        _featureHost is null
            ? _inner.SetMacroSequenceOptionsAsync(key, repeatCount, ignoreDelays, interruptOnOtherKey)
            : _featureHost.SetMacroSequenceOptionsAsync(key, repeatCount, ignoreDelays, interruptOnOtherKey);

    public Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync() =>
        _featureHost is null
            ? _inner.GetAutomationWorkspaceAsync()
            : _featureHost.GetAutomationWorkspaceAsync();

    public Task<IReadOnlyList<AutomationTriggerOption>> GetAutomationTriggerOptionsAsync() =>
        _featureHost is null
            ? _inner.GetAutomationTriggerOptionsAsync()
            : _featureHost.GetAutomationTriggerOptionsAsync();

    public Task<bool> SetAutomationEnabledAsync(bool enabled) =>
        _featureHost is null
            ? _inner.SetAutomationEnabledAsync(enabled)
            : _featureHost.SetAutomationEnabledAsync(enabled);

    public Task<bool> SaveAutomationWorkspaceAsync(IReadOnlyList<AutomationPipelineDraft> pipelines) =>
        _featureHost is null
            ? _inner.SaveAutomationWorkspaceAsync(pipelines)
            : _featureHost.SaveAutomationWorkspaceAsync(pipelines);

    public Task<KeyboardLightingState?> GetKeyboardLightingStateAsync() =>
        _featureHost is null
            ? _inner.GetKeyboardLightingStateAsync()
            : _featureHost.GetKeyboardLightingStateAsync();

    public Task<bool> SetKeyboardLightingAsync(KeyboardLightingUpdate update) =>
        _featureHost is null
            ? _inner.SetKeyboardLightingAsync(update)
            : _featureHost.SetKeyboardLightingAsync(update);

    public Task<NetworkAccelerationState> GetNetworkAccelerationStateAsync() =>
        _featureHost is null
            ? _inner.GetNetworkAccelerationStateAsync()
            : _featureHost.GetNetworkAccelerationStateAsync();

    public Task<bool> SetNetworkAccelerationEnabledAsync(bool enabled) =>
        _featureHost is null
            ? _inner.SetNetworkAccelerationEnabledAsync(enabled)
            : _featureHost.SetNetworkAccelerationEnabledAsync(enabled);

    public Task<bool> SetNetworkAccelerationModeAsync(string mode) =>
        _featureHost is null
            ? _inner.SetNetworkAccelerationModeAsync(mode)
            : _featureHost.SetNetworkAccelerationModeAsync(mode);

    public Task<bool> SetNetworkAccelerationGroupEnabledAsync(string groupId, bool enabled) =>
        _featureHost is null
            ? _inner.SetNetworkAccelerationGroupEnabledAsync(groupId, enabled)
            : _featureHost.SetNetworkAccelerationGroupEnabledAsync(groupId, enabled);

    public Task<bool> ToggleNetworkAccelerationAsync() =>
        _featureHost is null
            ? _inner.ToggleNetworkAccelerationAsync()
            : _featureHost.ToggleNetworkAccelerationAsync();

    public Task<string> RunNetworkDiagnosticsAsync() =>
        _featureHost is null
            ? _inner.RunNetworkDiagnosticsAsync()
            : _featureHost.RunNetworkDiagnosticsAsync();

    public Task<DriverDownloadState> GetDriverDownloadStateAsync() =>
        _featureHost is null
            ? _inner.GetDriverDownloadStateAsync()
            : _featureHost.GetDriverDownloadStateAsync();

    public Task<DriverDownloadState> SearchDriverPackagesAsync(string source, string machineType, string os, bool onlyUpdates) =>
        _featureHost is null
            ? _inner.SearchDriverPackagesAsync(source, machineType, os, onlyUpdates)
            : _featureHost.SearchDriverPackagesAsync(source, machineType, os, onlyUpdates);

    public Task<bool> DownloadDriverPackageAsync(string packageId, string destinationFolder) =>
        _featureHost is null
            ? _inner.DownloadDriverPackageAsync(packageId, destinationFolder)
            : _featureHost.DownloadDriverPackageAsync(packageId, destinationFolder);

    private async Task<IReadOnlyList<SensorReadingItem>> AppendHardwareSensorReadingsAsync(
        IReadOnlyList<SensorReadingItem> existing)
    {
        if (_sensorController is null)
            return existing;

        try
        {
            if (!await _sensorController.IsSupportedAsync().ConfigureAwait(false))
                return existing;

            await _sensorController.PrepareAsync().ConfigureAwait(false);
            var data = await _sensorController.GetDataAsync(detailed: true).ConfigureAwait(false);
            var (cpuFanSpeed, gpuFanSpeed) = await _sensorController.GetFanSpeedsAsync().ConfigureAwait(false);
            var readings = existing.ToList();

            AppendSensorData(readings, "CPU", data.CPU, cpuFanSpeed);
            AppendSensorData(readings, "GPU", data.GPU, gpuFanSpeed);
            return readings;
        }
        catch
        {
            // Generic adapter readings remain available when a vendor sensor backend
            // is unavailable or times out during a refresh.
            return existing;
        }
    }

    private static void AppendSensorData(
        ICollection<SensorReadingItem> readings,
        string category,
        SensorData data,
        int fanSpeed)
    {
        AddIfMissing(readings, $"{category} Utilization", category, data.Utilization, "%", 0, 100);
        AddIfMissing(readings, $"{category} Core Clock", category, data.CoreClock, "MHz");
        AddIfMissing(readings, $"{category} Temperature", category, data.Temperature, "\u00B0C");
        AddIfMissing(readings, $"{category} Fan Speed", category, fanSpeed, "RPM");
        AddIfMissing(readings, $"{category} Power", category, data.Wattage, "W");
        AddIfMissing(readings, $"{category} Voltage", category, data.Voltage, "V");
    }

    private static void AddIfMissing(
        ICollection<SensorReadingItem> readings,
        string name,
        string category,
        double value,
        string unit,
        double minimum = 0,
        double maximum = double.PositiveInfinity)
    {
        if (value < minimum || value > maximum || !double.IsFinite(value)
            || readings.Any(reading => reading.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return;

        var displayValue = $"{value:0.##} {unit}".Trim();
        readings.Add(new SensorReadingItem(name, displayValue, category, value, unit));
    }
}

#endif
