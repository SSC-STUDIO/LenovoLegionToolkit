#if WINDOWS

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.System;
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
    private readonly SensorsGroupController? _sensorsGroupController;

    private WindowsPlatformServices(IDeviceAdapter adapter)
    {
        _inner = new DeviceAdapterPlatformServices(adapter);
        _featureHost = WindowsFeatureHostServices.TryCreate();
        _sensorController = IoCContainer.TryResolve<ISensorsController>();
        _sensorsGroupController = IoCContainer.TryResolve<SensorsGroupController>();
    }

    public static IPlatformServices Create() => new WindowsPlatformServices(new WindowsDeviceAdapter());

    public Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync() => _inner.GetFeatureGroupsAsync();

    public async Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync()
    {
        var readings = await _inner.GetSensorReadingsAsync().ConfigureAwait(false);
        return await AppendHardwareSensorReadingsAsync(readings).ConfigureAwait(false);
    }

    public async Task<SensorDetailsSnapshot> GetSensorDetailsAsync()
    {
        var details = SensorDetailsSnapshot.Empty;

        try
        {
            if (_sensorController is not null
                && await _sensorController.IsSupportedAsync().ConfigureAwait(false))
            {
                await _sensorController.PrepareAsync().ConfigureAwait(false);
                var data = await _sensorController.GetDataAsync(detailed: true).ConfigureAwait(false);
                details = new SensorDetailsSnapshot
                {
                    IsAvailable = true,
                    CpuPowerWatts = NonNegative(data.CPU.Wattage),
                    CpuVoltageVolts = Positive(data.CPU.Voltage),
                    CpuTemperatureMinimumCelsius = Positive(data.CPU.MinTemperature),
                    CpuTemperatureMaximumCelsius = Positive(data.CPU.MaxTemperatureRecord),
                    CpuVoltageMinimumVolts = Positive(data.CPU.MinVoltage),
                    CpuVoltageMaximumVolts = Positive(data.CPU.MaxVoltage),
                    GpuMemoryClockMHz = NonNegative(data.GPU.MemoryClock),
                    GpuPowerWatts = NonNegative(data.GPU.Wattage),
                    GpuVoltageVolts = Positive(data.GPU.Voltage),
                    GpuTemperatureMinimumCelsius = Positive(data.GPU.MinTemperature),
                    GpuTemperatureMaximumCelsius = Positive(data.GPU.MaxTemperatureRecord),
                    GpuVoltageMinimumVolts = Positive(data.GPU.MinVoltage),
                    GpuVoltageMaximumVolts = Positive(data.GPU.MaxVoltage),
                };

                if (_sensorsGroupController is { } group)
                {
                    if (!group.IsLibreHardwareMonitorInitialized())
                        await group.IsSupportedAsync().ConfigureAwait(false);

                    if (group.IsLibreHardwareMonitorInitialized())
                    {
                        await group.UpdateAsync().ConfigureAwait(false);
                        var gpuVramUsedTask = group.GetGpuVramUsedAsync();
                        var gpuVramTotalTask = group.GetGpuVramTotalAsync();
                        var gpuVramUsageTask = group.GetGpuVramUtilizationAsync();
                        var gpuVramTemperatureTask = group.GetGpuVramTemperatureAsync();
                        var gpuHotSpotTemperatureTask = group.GetGpuHotSpotTemperatureAsync();
                        var gpuPcieRxTask = group.GetGpuPcieRxThroughputAsync();
                        var gpuPcieTxTask = group.GetGpuPcieTxThroughputAsync();
                        var gpuIntegratedTask = group.IsCurrentGpuIntegratedAsync();
                        var cpuPowerTask = group.GetCpuPowerAsync();
                        var cpuComponentPowersTask = group.GetCpuComponentPowersAsync();
                        var cpuPCoreClockTask = group.GetCpuPCoreClockAsync();
                        var cpuECoreClockTask = group.GetCpuECoreClockAsync();
                        var memoryUsageTask = group.GetMemoryUsageAsync();
                        var memoryUsedTask = group.GetMemoryUsedAsync();
                        var memoryTotalTask = group.GetMemoryTotalAsync();
                        var memoryTemperatureTask = group.GetHighestMemoryTemperatureAsync();
                        var ssdTemperaturesTask = group.GetSsdTemperaturesAsync();

                        await Task.WhenAll(
                            gpuVramUsedTask,
                            gpuVramTotalTask,
                            gpuVramUsageTask,
                            gpuVramTemperatureTask,
                            gpuHotSpotTemperatureTask,
                            gpuPcieRxTask,
                            gpuPcieTxTask,
                            gpuIntegratedTask,
                            cpuPowerTask,
                            cpuComponentPowersTask,
                            cpuPCoreClockTask,
                            cpuECoreClockTask,
                            memoryUsageTask,
                            memoryUsedTask,
                            memoryTotalTask,
                            memoryTemperatureTask,
                            ssdTemperaturesTask).ConfigureAwait(false);

                        var cpuComponents = await cpuComponentPowersTask.ConfigureAwait(false);
                        var ssdTemperatures = await ssdTemperaturesTask.ConfigureAwait(false);
                        details = details with
                        {
                            IsIntegratedGpu = await gpuIntegratedTask.ConfigureAwait(false),
                            CpuPowerWatts = PreferPositive(await cpuPowerTask.ConfigureAwait(false), details.CpuPowerWatts),
                            CpuCoresPowerWatts = Positive(cpuComponents.cores),
                            CpuMemoryPowerWatts = Positive(cpuComponents.memory),
                            CpuPlatformPowerWatts = Positive(cpuComponents.platform),
                            CpuPCoreClockMHz = Positive(await cpuPCoreClockTask.ConfigureAwait(false)),
                            CpuECoreClockMHz = Positive(await cpuECoreClockTask.ConfigureAwait(false)),
                            CpuMemoryUsagePercent = NonNegative(await memoryUsageTask.ConfigureAwait(false)),
                            CpuMemoryUsedGb = Positive(await memoryUsedTask.ConfigureAwait(false)),
                            CpuMemoryTotalGb = Positive(await memoryTotalTask.ConfigureAwait(false)),
                            CpuMemoryTemperatureCelsius = Positive(await memoryTemperatureTask.ConfigureAwait(false)),
                            CpuSsdTemperature1Celsius = Positive(ssdTemperatures.Item1),
                            CpuSsdTemperature2Celsius = Positive(ssdTemperatures.Item2),
                            GpuVramUsedGb = Positive(await gpuVramUsedTask.ConfigureAwait(false)),
                            GpuVramTotalGb = Positive(await gpuVramTotalTask.ConfigureAwait(false)),
                            GpuVramUsagePercent = NonNegative(await gpuVramUsageTask.ConfigureAwait(false)),
                            GpuVramTemperatureCelsius = Positive(await gpuVramTemperatureTask.ConfigureAwait(false)),
                            GpuHotSpotTemperatureCelsius = Positive(await gpuHotSpotTemperatureTask.ConfigureAwait(false)),
                            GpuPcieRxBytesPerSecond = NonNegative(await gpuPcieRxTask.ConfigureAwait(false)),
                            GpuPcieTxBytesPerSecond = NonNegative(await gpuPcieTxTask.ConfigureAwait(false)),
                        };
                    }
                }
            }

            var batteryState = await ReadDashboardBatteryStateAsync().ConfigureAwait(false);
            if (batteryState.IsAvailable)
            {
                details = details with
                {
                    IsAvailable = details.IsAvailable || batteryState.IsAvailable,
                    BatteryIsCharging = batteryState.IsCharging,
                    BatteryIsLowBattery = batteryState.IsLowBattery,
                    BatteryPowerAdapterStatus = batteryState.PowerAdapterStatus,
                    BatteryPercentage = batteryState.Percentage,
                    BatteryLifeRemainingSeconds = batteryState.LifeRemainingSeconds,
                    BatteryFullLifeRemainingSeconds = batteryState.FullLifeRemainingSeconds,
                    BatteryHealthPercent = batteryState.HealthPercent,
                    BatteryRateWatts = batteryState.DischargeRateWatts,
                    BatteryMinRateWatts = batteryState.MinDischargeRateWatts,
                    BatteryMaxRateWatts = batteryState.MaxDischargeRateWatts,
                    BatteryDesignCapacityWh = batteryState.DesignCapacityWh,
                    BatteryChargeCapacityWh = batteryState.ChargeCapacityWh,
                    BatteryFullCapacityWh = batteryState.FullCapacityWh,
                    BatteryCycleCount = batteryState.CycleCount,
                    BatteryTemperatureCelsius = batteryState.TemperatureCelsius,
                    BatteryManufactureDate = batteryState.ManufactureDate,
                    BatteryFirstUseDate = batteryState.FirstUseDate,
                    BatteryOnBatterySince = batteryState.OnBatterySince,
                    BatteryModelName = batteryState.ModelName,
                };
            }

            return details;
        }
        catch
        {
            return SensorDetailsSnapshot.Empty;
        }
    }

    public async Task<DashboardSnapshot> GetDashboardSnapshotAsync()
    {
        var snapshot = await _inner.GetDashboardSnapshotAsync().ConfigureAwait(false);
        var readings = await AppendHardwareSensorReadingsAsync(snapshot.SensorReadings).ConfigureAwait(false);
        var battery = await ReadDashboardBatteryStateAsync().ConfigureAwait(false);
        return snapshot with { SensorReadings = readings, Battery = battery };
    }

    public Task<DashboardBatteryState> GetDashboardBatteryStateAsync() =>
        ReadDashboardBatteryStateAsync();

    public Task<DashboardLayoutState> GetDashboardLayoutAsync() =>
        _featureHost is null
            ? _inner.GetDashboardLayoutAsync()
            : _featureHost.GetDashboardLayoutAsync();

    public Task<bool> SaveDashboardLayoutAsync(DashboardLayoutState layout) =>
        _featureHost is null
            ? _inner.SaveDashboardLayoutAsync(layout)
            : _featureHost.SaveDashboardLayoutAsync(layout);

    public Task<IReadOnlyList<DashboardItemState>> GetDashboardItemStatesAsync(
        IReadOnlyList<string> itemIdentifiers) =>
        _featureHost is null
            ? _inner.GetDashboardItemStatesAsync(itemIdentifiers)
            : _featureHost.GetDashboardItemStatesAsync(itemIdentifiers);

    public Task<bool> SetDashboardItemStateAsync(string itemIdentifier, string state) =>
        _featureHost is null
            ? _inner.SetDashboardItemStateAsync(itemIdentifier, state)
            : _featureHost.SetDashboardItemStateAsync(itemIdentifier, state);

    public Task<BalanceModeSettingsState> GetBalanceModeSettingsAsync() =>
        _featureHost is null
            ? _inner.GetBalanceModeSettingsAsync()
            : _featureHost.GetBalanceModeSettingsAsync();

    public Task<bool> SaveBalanceModeSettingsAsync(bool aiModeEnabled) =>
        _featureHost is null
            ? _inner.SaveBalanceModeSettingsAsync(aiModeEnabled)
            : _featureHost.SaveBalanceModeSettingsAsync(aiModeEnabled);

    public Task<GodModeSettingsState> GetGodModeSettingsAsync() =>
        _featureHost is null
            ? _inner.GetGodModeSettingsAsync()
            : _featureHost.GetGodModeSettingsAsync();

    public Task<IReadOnlyList<ushort>?> GetDefaultGodModeFanCurveAsync() =>
        _featureHost is null
            ? _inner.GetDefaultGodModeFanCurveAsync()
            : _featureHost.GetDefaultGodModeFanCurveAsync();

    public Task<bool> SetGodModePresetAsync(Guid presetId) =>
        _featureHost is null
            ? _inner.SetGodModePresetAsync(presetId)
            : _featureHost.SetGodModePresetAsync(presetId);

    public Task<bool> AddGodModePresetAsync(string name) =>
        _featureHost is null
            ? _inner.AddGodModePresetAsync(name)
            : _featureHost.AddGodModePresetAsync(name);

    public Task<bool> RenameGodModePresetAsync(Guid presetId, string name) =>
        _featureHost is null
            ? _inner.RenameGodModePresetAsync(presetId, name)
            : _featureHost.RenameGodModePresetAsync(presetId, name);

    public Task<bool> DeleteGodModePresetAsync(Guid presetId) =>
        _featureHost is null
            ? _inner.DeleteGodModePresetAsync(presetId)
            : _featureHost.DeleteGodModePresetAsync(presetId);

    public Task<bool> SaveGodModeSettingsAsync(GodModeSettingsUpdate update) =>
        _featureHost is null
            ? _inner.SaveGodModeSettingsAsync(update)
            : _featureHost.SaveGodModeSettingsAsync(update);

    public Task<DiscreteGpuState> GetDiscreteGpuStateAsync() =>
        _featureHost is null
            ? _inner.GetDiscreteGpuStateAsync()
            : _featureHost.GetDiscreteGpuStateAsync();

    public Task<bool> KillDiscreteGpuProcessesAsync() =>
        _featureHost is null
            ? _inner.KillDiscreteGpuProcessesAsync()
            : _featureHost.KillDiscreteGpuProcessesAsync();

    public Task<bool> RestartDiscreteGpuAsync() =>
        _featureHost is null
            ? _inner.RestartDiscreteGpuAsync()
            : _featureHost.RestartDiscreteGpuAsync();

    public Task<bool> TurnOffMonitorsAsync() =>
        _featureHost is null
            ? _inner.TurnOffMonitorsAsync()
            : _featureHost.TurnOffMonitorsAsync();

    public Task<GpuOverclockState> GetGpuOverclockStateAsync() =>
        _featureHost is null
            ? _inner.GetGpuOverclockStateAsync()
            : _featureHost.GetGpuOverclockStateAsync();

    public Task<bool> SetGpuOverclockAsync(bool enabled, int coreDeltaMhz, int memoryDeltaMhz) =>
        _featureHost is null
            ? _inner.SetGpuOverclockAsync(enabled, coreDeltaMhz, memoryDeltaMhz)
            : _featureHost.SetGpuOverclockAsync(enabled, coreDeltaMhz, memoryDeltaMhz);

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

    public Task<PluginPageState> GetPluginSettingsPageStateAsync(string pluginId) =>
        _featureHost is null
            ? _inner.GetPluginSettingsPageStateAsync(pluginId)
            : _featureHost.GetPluginSettingsPageStateAsync(pluginId);

    public Task<bool> SetFeatureActionAsync(string routeKey, string actionKey, bool isSelected) =>
        _featureHost is null
            ? _inner.SetFeatureActionAsync(routeKey, actionKey, isSelected)
            : _featureHost.SetActionAsync(routeKey, actionKey, isSelected);

    public Task<bool> ImportPluginAsync(string zipFilePath) =>
        _featureHost is null
            ? _inner.ImportPluginAsync(zipFilePath)
            : _featureHost.ImportPluginAsync(zipFilePath);

    public Task<PluginCatalogState> GetPluginCatalogAsync(bool forceRefresh = false) =>
        _featureHost is null
            ? _inner.GetPluginCatalogAsync(forceRefresh)
            : _featureHost.GetPluginCatalogAsync(forceRefresh);

    public Task<bool> UpdatePluginAsync(string pluginId) =>
        _featureHost is null
            ? _inner.UpdatePluginAsync(pluginId)
            : _featureHost.UpdatePluginAsync(pluginId);

    public Task<bool> InstallPluginAsync(string pluginId) =>
        _featureHost is null
            ? _inner.InstallPluginAsync(pluginId)
            : _featureHost.InstallPluginAsync(pluginId);

    public Task<MacroWorkspaceState> GetMacroWorkspaceAsync() =>
        _featureHost is null
            ? _inner.GetMacroWorkspaceAsync()
            : _featureHost.GetMacroWorkspaceAsync();

    public Task<bool> SetMacroEnabledAsync(bool enabled) =>
        _featureHost is null
            ? _inner.SetMacroEnabledAsync(enabled)
            : _featureHost.SetMacroEnabledAsync(enabled);

    public Task<bool> StartMacroRecordingAsync(ulong key, MacroRecordingMode mode) =>
        _featureHost is null
            ? _inner.StartMacroRecordingAsync(key, mode)
            : _featureHost.StartMacroRecordingAsync(key, mode);

    public Task<bool> SetMacroSequenceOptionsAsync(
        ulong key,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey) =>
        _featureHost is null
            ? _inner.SetMacroSequenceOptionsAsync(key, repeatCount, ignoreDelays, interruptOnOtherKey)
            : _featureHost.SetMacroSequenceOptionsAsync(key, repeatCount, ignoreDelays, interruptOnOtherKey);

    public Task<bool> ClearMacroSequenceAsync(ulong key) =>
        _featureHost is null
            ? _inner.ClearMacroSequenceAsync(key)
            : _featureHost.ClearMacroSequenceAsync(key);

    public Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync() =>
        _featureHost is null
            ? _inner.GetAutomationWorkspaceAsync()
            : _featureHost.GetAutomationWorkspaceAsync();

    public Task<IReadOnlyList<AutomationTriggerOption>> GetAutomationTriggerOptionsAsync() =>
        _featureHost is null
            ? _inner.GetAutomationTriggerOptionsAsync()
            : _featureHost.GetAutomationTriggerOptionsAsync();

    internal Task StartAutomationForHostAsync() =>
        _featureHost?.StartAutomationForHostAsync() ?? Task.CompletedTask;

    public Task<IReadOnlyList<AutomationStepOption>> GetAutomationStepOptionsAsync() =>
        _featureHost is null
            ? _inner.GetAutomationStepOptionsAsync()
            : _featureHost.GetAutomationStepOptionsAsync();

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

    public Task<bool> ResetKeyboardSpectrumProfileAsync() =>
        _featureHost is null
            ? _inner.ResetKeyboardSpectrumProfileAsync()
            : _featureHost.ResetKeyboardSpectrumProfileAsync();

    public Task<bool> ExportKeyboardSpectrumProfileAsync(string filePath) =>
        _featureHost is null
            ? _inner.ExportKeyboardSpectrumProfileAsync(filePath)
            : _featureHost.ExportKeyboardSpectrumProfileAsync(filePath);

    public Task<bool> ImportKeyboardSpectrumProfileAsync(string filePath) =>
        _featureHost is null
            ? _inner.ImportKeyboardSpectrumProfileAsync(filePath)
            : _featureHost.ImportKeyboardSpectrumProfileAsync(filePath);

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

    public Task<string> RestoreNetworkAccelerationAsync() =>
        _featureHost is null
            ? _inner.RestoreNetworkAccelerationAsync()
            : _featureHost.RestoreNetworkAccelerationAsync();

    public Task<NetworkAccelerationRuntimeState> GetNetworkAccelerationRuntimeAsync() =>
        _featureHost is null
            ? _inner.GetNetworkAccelerationRuntimeAsync()
            : _featureHost.GetNetworkAccelerationRuntimeAsync();

    public Task<NetworkNatDiagnosticState> RunNetworkNatDiagnosticAsync(string stunHost) =>
        _featureHost is null
            ? _inner.RunNetworkNatDiagnosticAsync(stunHost)
            : _featureHost.RunNetworkNatDiagnosticAsync(stunHost);

    public Task<NetworkDnsDiagnosticState> RunNetworkDnsDiagnosticAsync(
        string domain,
        string? dnsServer,
        bool useDoh,
        string? dohUrl) =>
        _featureHost is null
            ? _inner.RunNetworkDnsDiagnosticAsync(domain, dnsServer, useDoh, dohUrl)
            : _featureHost.RunNetworkDnsDiagnosticAsync(domain, dnsServer, useDoh, dohUrl);

    public Task<NetworkIpv6DiagnosticState> RunNetworkIpv6DiagnosticAsync() =>
        _featureHost is null
            ? _inner.RunNetworkIpv6DiagnosticAsync()
            : _featureHost.RunNetworkIpv6DiagnosticAsync();

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

    private static async Task<DashboardBatteryState> ReadDashboardBatteryStateAsync()
    {
        try
        {
            var battery = Battery.GetBatteryInformation();
            var adapterStatus = await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false);
            var onBatterySince = Battery.GetOnBatterySince();

            return new DashboardBatteryState
            {
                IsAvailable = true,
                IsCharging = battery.IsCharging,
                IsLowBattery = battery.IsLowBattery,
                PowerAdapterStatus = adapterStatus.ToString(),
                Percentage = NonNegative(battery.BatteryPercentage),
                LifeRemainingSeconds = NonNegative(battery.BatteryLifeRemaining),
                FullLifeRemainingSeconds = NonNegative(battery.FullBatteryLifeRemaining),
                DischargeRateWatts = battery.DischargeRate / 1000d,
                MinDischargeRateWatts = battery.MinDischargeRate == int.MaxValue
                    ? null
                    : battery.MinDischargeRate / 1000d,
                MaxDischargeRateWatts = battery.MaxDischargeRate > 0
                    ? battery.MaxDischargeRate / 1000d
                    : null,
                DesignCapacityWh = Positive(battery.DesignCapacity / 1000d),
                ChargeCapacityWh = Positive(battery.EstimateChargeRemaining / 1000d),
                FullCapacityWh = Positive(battery.FullChargeCapacity / 1000d),
                HealthPercent = Positive(battery.BatteryHealth),
                CycleCount = NonNegative(battery.CycleCount),
                TemperatureCelsius = battery.BatteryTemperatureC,
                ManufactureDate = ToDateTimeOffset(battery.ManufactureDate),
                FirstUseDate = ToDateTimeOffset(battery.FirstUseDate),
                OnBatterySince = ToDateTimeOffset(onBatterySince),
                ModelName = battery.ModelName,
            };
        }
        catch
        {
            // Battery APIs are optional on desktops and can fail without invalidating other telemetry.
            return DashboardBatteryState.Empty;
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

    private static double? Positive(double value) =>
        double.IsFinite(value) && value > 0 ? value : null;

    private static double? NonNegative(double value) =>
        double.IsFinite(value) && value >= 0 ? value : null;

    private static double? PreferPositive(double value, double? fallback) =>
        Positive(value) ?? fallback;

    private static DateTimeOffset? ToDateTimeOffset(DateTime? value) =>
        value is { } date ? new DateTimeOffset(date) : null;
}

#endif
