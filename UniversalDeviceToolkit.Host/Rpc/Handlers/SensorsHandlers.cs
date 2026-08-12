using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Sensor bridge: LibreHardwareMonitor snapshot + subscription, vendor fallback,
/// FPS monitoring and sensor-related settings.
/// </summary>
public static class SensorsHandlers
{
    private sealed class SensorSubscriber { public static readonly SensorSubscriber Instance = new(); }

    private static readonly object FpsLock = new();
    private static int _fpsSubscriberCount;
    private static SensorsGroupController? _subscribedGroup;
    private static BridgeRpcServer? _sensorsRpc;
    private static System.Threading.Timer? _vendorPollTimer;
    private static double _vendorPollIntervalSec = 1.0;
    private static FpsSensorController? _subscribedFpsController;
    private static BridgeRpcServer? _fpsRpc;

    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("sensors.getStatus", (request, _) => HandleGetStatusAsync(request));
        rpc.RegisterHandler("sensors.getSnapshot", (request, _) => HandleGetSnapshotAsync(request));
        rpc.RegisterHandler("sensors.getDetailed", (request, _) => HandleGetDetailedAsync(request));
        rpc.RegisterHandler("sensors.subscribe", (request, _) => HandleSubscribeAsync(request, rpc));
        rpc.RegisterHandler("sensors.unsubscribe", (request, _) => HandleUnsubscribeAsync(request));
        rpc.RegisterHandler("sensors.getSettings", (request, _) => HandleGetSettingsAsync(request));
        rpc.RegisterHandler("sensors.setSettings", (request, _) => HandleSetSettingsAsync(request, rpc));
        rpc.RegisterHandler("sensors.getFps", (request, _) => HandleGetFpsAsync(request));
        rpc.RegisterHandler("sensors.subscribeFps", (request, _) => HandleSubscribeFpsAsync(request, rpc));
        rpc.RegisterHandler("sensors.unsubscribeFps", (request, _) => HandleUnsubscribeFpsAsync(request));
    }

    private static SensorsGroupController GetSensorsGroup()
        => IoCContainer.Resolve<SensorsGroupController>();

    private static SensorsController GetVendorSensors()
        => IoCContainer.Resolve<SensorsController>();

    private static FpsSensorController GetFpsController()
        => IoCContainer.Resolve<FpsSensorController>();

    // ── snapshot assembly ───────────────────────────────────────────────────

    private static async Task<object> BuildSnapshotAsync()
    {
        var group = GetSensorsGroup();
        var applicationSettings = IoCContainer.Resolve<ApplicationSettings>();

        if (applicationSettings.Store.EnableHardwareSensors)
        {
            try
            {
                await group.IsSupportedAsync().ConfigureAwait(false);
            }
            catch
            {
                // Initialization failed; fall back to the vendor path below.
            }
        }

        if (group.IsLibreHardwareMonitorInitialized())
        {
            var snapshot = await BuildLhmSnapshotAsync(group).ConfigureAwait(false);
            return snapshot;
        }

        return await BuildVendorSnapshotAsync().ConfigureAwait(false);
    }

    private static async Task<object> BuildLhmSnapshotAsync(SensorsGroupController group)
    {
        var cpuTempTask = group.GetCpuTemperatureAsync();
        var cpuUsageTask = group.GetCpuUsageAsync();
        var cpuFanTask = group.GetCpuFanSpeedAsync();
        var cpuPowerTask = group.GetCpuPowerAsync();
        var cpuComponentPowersTask = group.GetCpuComponentPowersAsync();
        var cpuVoltageTask = group.GetCpuVoltageAsync();
        var cpuClockTask = group.GetCpuCoreClockAsync();
        var cpuPClockTask = group.GetCpuPCoreClockAsync();
        var cpuEClockTask = group.GetCpuECoreClockAsync();
        var gpuUsageTask = group.GetGpuUsageAsync();
        var gpuTempTask = group.GetGpuTemperatureAsync();
        var gpuClockTask = group.GetGpuCoreClockAsync();
        var gpuMemoryClockTask = group.GetGpuMemoryClockAsync();
        var gpuPowerTask = group.GetGpuPowerAsync();
        var gpuVoltageTask = group.GetGpuVoltageAsync();
        var gpuVramTempTask = group.GetGpuVramTemperatureAsync();
        var gpuHotSpotTask = group.GetGpuHotSpotTemperatureAsync();
        var gpuVramUtilTask = group.GetGpuVramUtilizationAsync();
        var gpuVramUsedTask = group.GetGpuVramUsedAsync();
        var gpuVramTotalTask = group.GetGpuVramTotalAsync();
        var gpuPcieRxTask = group.GetGpuPcieRxThroughputAsync();
        var gpuPcieTxTask = group.GetGpuPcieTxThroughputAsync();
        var gpuFanTask = group.GetGpuFanSpeedAsync();
        var memUsageTask = group.GetMemoryUsageAsync();
        var memUsedTask = group.GetMemoryUsedAsync();
        var memTotalTask = group.GetMemoryTotalAsync();
        var memMaxTempTask = group.GetHighestMemoryTemperatureAsync();
        var motherboardMaxTempTask = group.GetHighestMotherboardTemperatureAsync();
        var ssdTempsTask = group.GetSsdTemperaturesAsync();
        var cpuNameTask = group.GetCpuNameAsync();
        var gpuNameTask = group.GetGpuNameAsync();
        var gpuIsIntegratedTask = group.IsCurrentGpuIntegratedAsync();

        await Task.WhenAll(
            cpuTempTask, cpuUsageTask, cpuFanTask, cpuPowerTask, cpuComponentPowersTask,
            cpuVoltageTask, cpuClockTask, cpuPClockTask, cpuEClockTask,
            gpuUsageTask, gpuTempTask, gpuClockTask, gpuMemoryClockTask, gpuPowerTask,
            gpuVoltageTask, gpuVramTempTask, gpuHotSpotTask, gpuVramUtilTask,
            gpuVramUsedTask, gpuVramTotalTask, gpuPcieRxTask, gpuPcieTxTask, gpuFanTask,
            memUsageTask, memUsedTask, memTotalTask, memMaxTempTask, motherboardMaxTempTask,
            ssdTempsTask, cpuNameTask, gpuNameTask, gpuIsIntegratedTask).ConfigureAwait(false);

        var ssdTemps = ssdTempsTask.Result;
        var battery = await BuildBatteryAsync().ConfigureAwait(false);

        return new
        {
            ts = DateTime.UtcNow,
            source = "LibreHardwareMonitor",
            initialized = true,
            isHybrid = group.IsHybrid,
            info = new
            {
                cpuName = NullIf(cpuNameTask.Result, "UNKNOWN"),
                gpuName = NullIf(gpuNameTask.Result, "UNKNOWN"),
                gpuIsIntegrated = gpuIsIntegratedTask.Result,
            },
            cpu = new
            {
                temperature = NullIf(cpuTempTask.Result),
                usage = NullIf(cpuUsageTask.Result),
                fanSpeed = NullIf(cpuFanTask.Result),
                power = NullIf(cpuPowerTask.Result),
                powerCores = NullIf(cpuComponentPowersTask.Result.cores),
                powerMemory = NullIf(cpuComponentPowersTask.Result.memory),
                powerPlatform = NullIf(cpuComponentPowersTask.Result.platform),
                voltage = NullIf(cpuVoltageTask.Result),
                coreClockMax = NullIf(cpuClockTask.Result),
                coreClockAvg = (float?)null,
                pCoreClock = NullIf(cpuPClockTask.Result),
                eCoreClock = NullIf(cpuEClockTask.Result),
            },
            gpu = new
            {
                usage = NullIf(gpuUsageTask.Result),
                temperature = NullIf(gpuTempTask.Result),
                coreClock = NullIf(gpuClockTask.Result),
                memoryClock = NullIf(gpuMemoryClockTask.Result),
                power = NullIf(gpuPowerTask.Result),
                voltage = NullIf(gpuVoltageTask.Result),
                vramTemperature = NullIf(gpuVramTempTask.Result),
                hotSpotTemperature = NullIf(gpuHotSpotTask.Result),
                vramUtilization = NullIf(gpuVramUtilTask.Result),
                vramUsedMb = NullIf(gpuVramUsedTask.Result),
                vramTotalMb = gpuVramTotalTask.Result >= 0 ? (float?)(gpuVramTotalTask.Result * 1024f) : null,
                pcieRxThroughput = NullIf(gpuPcieRxTask.Result),
                pcieTxThroughput = NullIf(gpuPcieTxTask.Result),
                fanSpeed = NullIf(gpuFanTask.Result),
            },
            memory = new
            {
                usage = NullIf(memUsageTask.Result),
                usedMb = NullIf(memUsedTask.Result),
                totalMb = NullIf(memTotalTask.Result),
                highestTemperature = NullIf(memMaxTempTask.Result),
            },
            battery,
            motherboard = new
            {
                highestTemperature = NullIf(motherboardMaxTempTask.Result),
            },
            storage = new
            {
                temperatures = new float?[] { NullIf(ssdTemps.Item1), NullIf(ssdTemps.Item2) },
            },
        };
    }

    private static async Task<object> BuildVendorSnapshotAsync()
    {
        SensorsData data;
        try
        {
            data = await GetVendorSensors().GetDataAsync(detailed: true).ConfigureAwait(false);
        }
        catch
        {
            var batteryOnly = await BuildBatteryAsync().ConfigureAwait(false);
            return new { ts = DateTime.UtcNow, source = "vendor", initialized = false, battery = batteryOnly };
        }

        var battery = await BuildBatteryAsync().ConfigureAwait(false);
        return new
        {
            ts = DateTime.UtcNow,
            source = "vendor",
            initialized = true,
            isHybrid = false,
            info = new { cpuName = (string?)null, gpuName = (string?)null, gpuIsIntegrated = false },
            cpu = new
            {
                temperature = NullIf(data.CPU.Temperature),
                usage = NullIf(data.CPU.Utilization),
                fanSpeed = NullIf(data.CPU.FanSpeed),
                power = NullIf(data.CPU.Wattage),
                voltage = NullIf(data.CPU.Voltage),
                coreClockMax = NullIf(data.CPU.CoreClock),
                pCoreClock = (int?)null,
                eCoreClock = (int?)null,
            },
            gpu = new
            {
                temperature = NullIf(data.GPU.Temperature),
                usage = NullIf(data.GPU.Utilization),
                coreClock = NullIf(data.GPU.CoreClock),
                memoryClock = NullIf(data.GPU.MemoryClock),
                power = NullIf(data.GPU.Wattage),
                voltage = NullIf(data.GPU.Voltage),
                fanSpeed = NullIf(data.GPU.FanSpeed),
            },
            memory = new { usage = (int?)null, usedMb = (int?)null, totalMb = (int?)null, highestTemperature = (double?)null },
            battery,
            motherboard = new { highestTemperature = (double?)null },
            storage = new { temperatures = new int?[] { null, null } },
        };
    }

    /// <summary>
    /// Avalonia SensorsControl.RefreshBattery parity: Battery.GetBatteryInformation +
    /// Power.IsPowerAdapterConnectedAsync for low-wattage adapter warning.
    /// Health is 0..1 (BatteryHealth is already 0..100 percent).
    /// </summary>
    private static async Task<object?> BuildBatteryAsync()
    {
        try
        {
            var info = Battery.GetBatteryInformation();
            var adapter = await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false);
            return new
            {
                chargeLevel = info.BatteryPercentage >= 0 ? (int?)info.BatteryPercentage : null,
                health = info.DesignCapacity > 0 ? (double?)(info.BatteryHealth / 100.0) : null,
                temperature = info.BatteryTemperatureC,
                chargeRate = info.DischargeRate,
                designCapacity = info.DesignCapacity > 0 ? (int?)info.DesignCapacity : null,
                fullChargeCapacity = info.FullChargeCapacity > 0 ? (int?)info.FullChargeCapacity : null,
                isCharging = info.IsCharging,
                isLowBattery = info.IsLowBattery,
                isLowPowerAdapter = adapter == PowerAdapterStatus.ConnectedLowWattage,
                modelName = string.IsNullOrWhiteSpace(info.ModelName) ? null : info.ModelName,
            };
        }
        catch
        {
            return null;
        }
    }

    // ── handlers ────────────────────────────────────────────────────────────

    private static async Task<BridgeResult> HandleGetStatusAsync(BridgeRequest request)
    {
        try
        {
            var group = GetSensorsGroup();
            var initialized = group.IsLibreHardwareMonitorInitialized();
            var cpuName = "UNKNOWN";
            var gpuName = "UNKNOWN";
            var gpuIsIntegrated = false;
            if (initialized)
            {
                cpuName = await group.GetCpuNameAsync().ConfigureAwait(false);
                gpuName = await group.GetGpuNameAsync().ConfigureAwait(false);
                gpuIsIntegrated = await group.IsCurrentGpuIntegratedAsync().ConfigureAwait(false);
            }

            return BridgeResult.Ok(new
            {
                initialized,
                isHybrid = group.IsHybrid,
                cpuName = NullIf(cpuName, "UNKNOWN"),
                gpuName = NullIf(gpuName, "UNKNOWN"),
                gpuIsIntegrated,
                initialState = group.InitialState.ToString(),
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleGetSnapshotAsync(BridgeRequest request)
    {
        try
        {
            var snapshot = await BuildSnapshotAsync().ConfigureAwait(false);
            return BridgeResult.Ok(snapshot);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleGetDetailedAsync(BridgeRequest request)
    {
        try
        {
            var data = await GetVendorSensors().GetDataAsync(detailed: true).ConfigureAwait(false);
            return BridgeResult.Ok(new
            {
                source = "vendor",
                cpu = new
                {
                    utilization = NullIf(data.CPU.Utilization),
                    coreClock = NullIf(data.CPU.CoreClock),
                    temperature = NullIf(data.CPU.Temperature),
                    wattage = NullIf(data.CPU.Wattage),
                    voltage = NullIf(data.CPU.Voltage),
                    fanSpeed = NullIf(data.CPU.FanSpeed),
                    minTemperature = NullIf(data.CPU.MinTemperature),
                    maxTemperature = NullIf(data.CPU.MaxTemperatureRecord),
                },
                gpu = new
                {
                    utilization = NullIf(data.GPU.Utilization),
                    coreClock = NullIf(data.GPU.CoreClock),
                    memoryClock = NullIf(data.GPU.MemoryClock),
                    temperature = NullIf(data.GPU.Temperature),
                    wattage = NullIf(data.GPU.Wattage),
                    voltage = NullIf(data.GPU.Voltage),
                    fanSpeed = NullIf(data.GPU.FanSpeed),
                },
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSubscribeAsync(BridgeRequest request, BridgeRpcServer rpc)
    {
        try
        {
            var intervalSec = 1.0;
            if (request.Parameters.TryGetProperty("intervalSec", out var intervalProp) &&
                intervalProp.ValueKind == JsonValueKind.Number)
            {
                intervalSec = intervalProp.GetDouble();
            }

            intervalSec = Math.Clamp(intervalSec, 0.5, 30.0);

            var group = GetSensorsGroup();

            // LibreHardwareMonitor path: the group's producer loop raises
            // SensorsUpdated only after LHM initialization succeeds.
            if (group.IsLibreHardwareMonitorInitialized())
            {
                _subscribedGroup = group;
                _sensorsRpc = rpc;
                group.SensorsUpdated -= OnSensorsUpdated;
                group.SensorsUpdated += OnSensorsUpdated;
                group.Start(SensorSubscriber.Instance, TimeSpan.FromSeconds(intervalSec));

                await Task.CompletedTask;
                return BridgeResult.Ok(new { subscribed = true, effectiveIntervalSec = intervalSec });
            }

            // Vendor fallback path (EnableHardwareSensors off / LHM unavailable):
            // poll BuildSnapshotAsync on the interval and broadcast the same
            // sensors.updated event the renderer subscribes to.
            _subscribedGroup = null;
            _sensorsRpc = rpc;
            _vendorPollIntervalSec = intervalSec;
            _vendorPollTimer?.Dispose();
            _vendorPollTimer = new System.Threading.Timer(
                async _ =>
                {
                    var rpcRef = _sensorsRpc;
                    if (rpcRef is null) return;
                    try
                    {
                        var snapshot = await BuildSnapshotAsync().ConfigureAwait(false);
                        rpcRef.Publish("sensors.updated", snapshot);
                    }
                    catch (Exception ex)
                    {
                        if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"sensors.updated vendor publish failed: {ex.Message}", ex);
                    }
                },
                null,
                TimeSpan.FromSeconds(intervalSec),
                TimeSpan.FromSeconds(intervalSec));

            await Task.CompletedTask;
            return BridgeResult.Ok(new { subscribed = true, effectiveIntervalSec = intervalSec });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleUnsubscribeAsync(BridgeRequest request)
    {
        try
        {
            var group = GetSensorsGroup();
            group.Stop(SensorSubscriber.Instance);
            group.SensorsUpdated -= OnSensorsUpdated;
            _subscribedGroup = null;
            _vendorPollTimer?.Dispose();
            _vendorPollTimer = null;
            _sensorsRpc = null;
            await Task.CompletedTask;
            return BridgeResult.Ok(new { unsubscribed = true });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void OnSensorsUpdated(object? sender, EventArgs args)
    {
        if (_subscribedGroup is null || _sensorsRpc is null)
            return;

        try
        {
            var snapshot = BuildLhmSnapshotAsync(_subscribedGroup).GetAwaiter().GetResult();
            _sensorsRpc.Publish("sensors.updated", snapshot);
        }
        catch (Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"sensors.updated publish failed: {ex.Message}", ex);
        }
    }

    private static async Task<BridgeResult> HandleGetSettingsAsync(BridgeRequest request)
    {
        try
        {
            var applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
            var osdSettings = IoCContainer.Resolve<OsdSettings>();
            var hardwareSensorSettings = IoCContainer.Resolve<HardwareSensorSettings>();

            return BridgeResult.Ok(new
            {
                enableHardwareSensors = applicationSettings.Store.EnableHardwareSensors,
                osdRefreshIntervalSec = osdSettings.Store.OsdRefreshInterval,
                selectedGpuIsIgpu = hardwareSensorSettings.Store.SelectedGpuIsIgpu,
                showCpuAverageFrequency = hardwareSensorSettings.Store.ShowCpuAverageFrequency,
                displayMemoryInGigabytes = hardwareSensorSettings.Store.DisplayMemoryInGigabytes,
                visibleSections = hardwareSensorSettings.Store.VisibleSections,
                sectionOrder = hardwareSensorSettings.Store.SectionOrder,
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSetSettingsAsync(BridgeRequest request, BridgeRpcServer rpc)
    {
        try
        {
            var applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
            var hardwareSensorSettings = IoCContainer.Resolve<HardwareSensorSettings>();

            var changed = false;

            if (request.Parameters.TryGetProperty("enableHardwareSensors", out var enableProp) &&
                enableProp.ValueKind == JsonValueKind.True ||
                request.Parameters.TryGetProperty("enableHardwareSensors", out enableProp) &&
                enableProp.ValueKind == JsonValueKind.False)
            {
                var enabled = enableProp.GetBoolean();
                var feature = IoCContainer.Resolve<HardwareSensorsFeature>();
                await feature.SetStateAsync(enabled ? HardwareSensorsState.On : HardwareSensorsState.Off).ConfigureAwait(false);
                changed = true;
            }

            if (request.Parameters.TryGetProperty("selectedGpuIsIgpu", out var igpuProp) &&
                igpuProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                hardwareSensorSettings.Store.SelectedGpuIsIgpu = igpuProp.GetBoolean();
                GetSensorsGroup().SelectedGpuIsIgpu = igpuProp.GetBoolean();
                changed = true;
            }

            if (request.Parameters.TryGetProperty("showCpuAverageFrequency", out var avgFreqProp) &&
                avgFreqProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                hardwareSensorSettings.Store.ShowCpuAverageFrequency = avgFreqProp.GetBoolean();
                GetSensorsGroup().ShowAverageCpuFrequency = avgFreqProp.GetBoolean();
                changed = true;
            }

            if (request.Parameters.TryGetProperty("displayMemoryInGigabytes", out var memGigabytesProp) &&
                memGigabytesProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                hardwareSensorSettings.Store.DisplayMemoryInGigabytes = memGigabytesProp.GetBoolean();
                changed = true;
            }

            if (changed)
            {
                hardwareSensorSettings.SynchronizeStore();
                hardwareSensorSettings.NotifySectionsChanged();
            }

            await Task.CompletedTask;
            return BridgeResult.Ok(new { saved = true });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleGetFpsAsync(BridgeRequest request)
    {
        try
        {
            var data = GetFpsController().GetCurrentFpsData();
            return BridgeResult.Ok(MapFpsData(data));
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSubscribeFpsAsync(BridgeRequest request, BridgeRpcServer rpc)
    {
        try
        {
            var controller = GetFpsController();
            var blacklist = ParseFpsBlacklist(request.Parameters);

            lock (FpsLock)
            {
                if (_fpsSubscriberCount == 0)
                {
                    _subscribedFpsController = controller;
                    _fpsRpc = rpc;
                    TryApplyFpsBlacklist(controller, blacklist);
                    controller.FpsDataUpdated -= OnFpsDataUpdated;
                    controller.FpsDataUpdated += OnFpsDataUpdated;
                    _ = controller.StartMonitoringAsync();
                }

                _fpsSubscriberCount++;
            }

            await Task.CompletedTask;
            return BridgeResult.Ok(new { monitoring = true });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleUnsubscribeFpsAsync(BridgeRequest request)
    {
        try
        {
            var controller = GetFpsController();

            lock (FpsLock)
            {
                _fpsSubscriberCount = Math.Max(0, _fpsSubscriberCount - 1);
                if (_fpsSubscriberCount == 0)
                {
                    controller.FpsDataUpdated -= OnFpsDataUpdated;
                    controller.StopMonitoring();
                    _subscribedFpsController = null;
                }
            }

            await Task.CompletedTask;
            return BridgeResult.Ok(new { monitoring = false });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void OnFpsDataUpdated(object? sender, FpsSensorController.FpsData data)
    {
        if (_fpsRpc is null)
            return;

        try
        {
            _fpsRpc.Publish("sensors.fpsUpdated", MapFpsData(data));
        }
        catch (Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"sensors.fpsUpdated publish failed: {ex.Message}", ex);
        }
    }

    private static object MapFpsData(FpsSensorController.FpsData data)
    {
        return new
        {
            process = (string?)null,
            fps = ParseFps(data.Fps),
            lowFps = ParseFps(data.LowFps),
            frameTimeMs = ParseFps(data.FrameTime),
        };
    }

    private static double? ParseFps(string value)
        => double.TryParse(value, out var parsed) && parsed >= 0 ? parsed : null;

    // ── helpers ─────────────────────────────────────────────────────────────

    private static string[]? ParseFpsBlacklist(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("blacklist", out var blacklistProp) || blacklistProp.ValueKind != JsonValueKind.Array)
            return null;

        var entries = blacklistProp.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return entries.Length > 0 ? entries : null;
    }

    /// <summary>
    /// Applies the subscription-time blacklist to the FPS controller. Lib's
    /// FpsSensorController only exposes the blacklist read-only (Blacklist),
    /// so the backing field is written directly; if its shape ever changes the
    /// application is skipped and monitoring keeps the previous behavior.
    /// </summary>
    private static void TryApplyFpsBlacklist(FpsSensorController controller, string[]? blacklist)
    {
        if (blacklist is null)
            return;

        try
        {
            var field = typeof(FpsSensorController).GetField("_blacklist", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field is null || field.FieldType != typeof(List<string>))
                return;

            field.SetValue(controller, new List<string>(blacklist));
        }
        catch (Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to apply FPS blacklist: {ex.Message}", ex);
        }
    }

    private static float? NullIf(float value) => value < 0 ? null : value;
    private static double? NullIf(double value) => value <= 0 ? null : value;
    private static int? NullIf(int value) => value < 0 ? null : value;
    private static string? NullIf(string value, string sentinel) => value == sentinel ? null : value;
}
