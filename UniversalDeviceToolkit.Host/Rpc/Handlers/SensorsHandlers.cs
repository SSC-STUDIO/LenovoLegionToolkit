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
    private sealed class SensorSubscriber
    {
        public static object Named(string id) => id;
    }

    private static readonly object SubscribeLock = new();
    private static readonly HashSet<string> VendorSubscriberIds = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, double> VendorIntervals = new(StringComparer.Ordinal);
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
            if (snapshot is not null)
                return snapshot;
            // LibreHardwareMonitor initialized but exposed no CPU/GPU sensors
            // (e.g. running without administrator rights) — fall back to the
            // vendor snapshot instead of rendering empty panels.
        }

        return await BuildVendorSnapshotAsync().ConfigureAwait(false);
    }

    private static async Task<object?> BuildLhmSnapshotAsync(SensorsGroupController group)
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
        var (cpuName, gpuName) = await MergeHardwareNamesAsync(
            NullIf(cpuNameTask.Result, "UNKNOWN"),
            NullIf(gpuNameTask.Result, "UNKNOWN")).ConfigureAwait(false);

        // LibreHardwareMonitor may initialize without exposing any CPU/GPU
        // sensors (e.g. without administrator rights). Treat that as "no data"
        // so the caller falls back to the vendor snapshot.
        var hasCpuData = HasValue(cpuTempTask.Result) || HasValue(cpuUsageTask.Result) ||
                         HasValue(cpuClockTask.Result) || HasValue(cpuFanTask.Result);
        var hasGpuData = HasValue(gpuTempTask.Result) || HasValue(gpuUsageTask.Result) ||
                         HasValue(gpuClockTask.Result) || HasValue(gpuFanTask.Result);
        if (!hasCpuData && !hasGpuData)
            return null;

        return new
        {
            ts = DateTime.UtcNow,
            source = "LibreHardwareMonitor",
            initialized = true,
            isHybrid = group.IsHybrid,
            info = new
            {
                cpuName,
                gpuName,
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
                // GetGpuVramUsed/TotalAsync return GiB; bridge fields are MiB.
                vramUsedMb = GigabytesToMegabytes(gpuVramUsedTask.Result),
                vramTotalMb = GigabytesToMegabytes(gpuVramTotalTask.Result),
                pcieRxThroughput = NullIf(gpuPcieRxTask.Result),
                pcieTxThroughput = NullIf(gpuPcieTxTask.Result),
                fanSpeed = NullIf(gpuFanTask.Result),
            },
            memory = new
            {
                usage = NullIf(memUsageTask.Result),
                // LHM SensorType.Data is GiB; bridge fields are MiB.
                usedMb = GigabytesToMegabytes(memUsedTask.Result),
                totalMb = GigabytesToMegabytes(memTotalTask.Result),
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
        var group = GetSensorsGroup();
        var (cpuName, gpuName) = await MergeHardwareNamesAsync(null, null).ConfigureAwait(false);
        var gpuIsIntegrated = false;
        try
        {
            if (group.IsLibreHardwareMonitorInitialized())
                gpuIsIntegrated = await group.IsCurrentGpuIntegratedAsync().ConfigureAwait(false);
        }
        catch
        {
            // Best-effort; vendor snapshot still returns sensor readings.
        }

        return new
        {
            ts = DateTime.UtcNow,
            source = "vendor",
            initialized = true,
            isHybrid = false,
            info = new { cpuName, gpuName, gpuIsIntegrated },
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
                avgTemperature = info.AvgTemperatureC,
                chargeRate = info.DischargeRate,
                minDischargeRate = info.MinDischargeRate == int.MaxValue ? null : (int?)info.MinDischargeRate,
                maxDischargeRate = (int?)info.MaxDischargeRate,
                designCapacity = info.DesignCapacity > 0 ? (int?)info.DesignCapacity : null,
                fullChargeCapacity = info.FullChargeCapacity > 0 ? (int?)info.FullChargeCapacity : null,
                cycleCount = info.CycleCount >= 0 ? (int?)info.CycleCount : null,
                manufactureDate = info.ManufactureDate?.ToString("yyyy-MM-dd"),
                firstUseDate = info.FirstUseDate?.ToString("yyyy-MM-dd"),
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
            string? cpuName = null;
            string? gpuName = null;
            var gpuIsIntegrated = false;
            if (initialized)
            {
                cpuName = NullIf(await group.GetCpuNameAsync().ConfigureAwait(false), "UNKNOWN");
                gpuName = NullIf(await group.GetGpuNameAsync().ConfigureAwait(false), "UNKNOWN");
                gpuIsIntegrated = await group.IsCurrentGpuIntegratedAsync().ConfigureAwait(false);
            }

            (cpuName, gpuName) = await MergeHardwareNamesAsync(cpuName, gpuName).ConfigureAwait(false);

            return BridgeResult.Ok(new
            {
                initialized,
                isHybrid = group.IsHybrid,
                cpuName,
                gpuName,
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

    private static string ReadSubscriberId(BridgeRequest request)
    {
        if (request.Parameters.ValueKind == JsonValueKind.Object &&
            request.Parameters.TryGetProperty("subscriberId", out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            var id = prop.GetString();
            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        return "ui";
    }

    private static async Task EnsureLibreHardwareMonitorAsync()
    {
        var applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
        if (!applicationSettings.Store.EnableHardwareSensors)
            return;

        var group = GetSensorsGroup();
        if (group.IsLibreHardwareMonitorInitialized())
            return;

        try
        {
            _ = await group.IsSupportedAsync().ConfigureAwait(false);
        }
        catch
        {
            // Vendor fallback in HandleSubscribeAsync.
        }
    }

    private static void StopVendorTimerIfIdle()
    {
        lock (SubscribeLock)
        {
            if (VendorSubscriberIds.Count > 0)
                return;
            _vendorPollTimer?.Dispose();
            _vendorPollTimer = null;
        }
    }

    private static void StartOrUpdateVendorTimer(BridgeRpcServer rpc)
    {
        lock (SubscribeLock)
        {
            if (VendorSubscriberIds.Count == 0)
            {
                _vendorPollTimer?.Dispose();
                _vendorPollTimer = null;
                return;
            }

            _vendorPollIntervalSec = VendorIntervals.Values.Min();
            _sensorsRpc = rpc;
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
                TimeSpan.FromSeconds(_vendorPollIntervalSec),
                TimeSpan.FromSeconds(_vendorPollIntervalSec));
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
            var subscriberId = ReadSubscriberId(request);
            var subscriber = SensorSubscriber.Named(subscriberId);

            await EnsureLibreHardwareMonitorAsync().ConfigureAwait(false);

            var group = GetSensorsGroup();

            // LibreHardwareMonitor path: the group's producer loop raises
            // SensorsUpdated only after LHM initialization succeeds.
            // LHM may be marked initialized while exposing no readable sensors
            // (e.g. NVAPI/performance counters unavailable) — in that case the
            // vendor fallback below is stable, whereas the LHM loop would
            // publish nothing but null frames.
            if (group.IsLibreHardwareMonitorInitialized() && await LhmHasSensorDataAsync(group).ConfigureAwait(false))
            {
                lock (SubscribeLock)
                {
                    VendorSubscriberIds.Remove(subscriberId);
                    VendorIntervals.Remove(subscriberId);
                }
                StopVendorTimerIfIdle();

                _subscribedGroup = group;
                _sensorsRpc = rpc;
                group.SensorsUpdated -= OnSensorsUpdated;
                group.SensorsUpdated += OnSensorsUpdated;
                group.Start(subscriber, TimeSpan.FromSeconds(intervalSec));

                await Task.CompletedTask;
                return BridgeResult.Ok(new { subscribed = true, effectiveIntervalSec = intervalSec });
            }

            lock (SubscribeLock)
            {
                VendorSubscriberIds.Add(subscriberId);
                VendorIntervals[subscriberId] = intervalSec;
            }
            _subscribedGroup = null;
            StartOrUpdateVendorTimer(rpc);

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
            var subscriberId = ReadSubscriberId(request);
            var subscriber = SensorSubscriber.Named(subscriberId);
            var group = GetSensorsGroup();
            group.Stop(subscriber);
            lock (SubscribeLock)
            {
                VendorSubscriberIds.Remove(subscriberId);
                VendorIntervals.Remove(subscriberId);
                if (VendorSubscriberIds.Count == 0 && !group.IsLibreHardwareMonitorInitialized())
                {
                    group.SensorsUpdated -= OnSensorsUpdated;
                    _subscribedGroup = null;
                    _sensorsRpc = null;
                }
            }
            StopVendorTimerIfIdle();
            if (VendorSubscriberIds.Count > 0 && _sensorsRpc is not null)
                StartOrUpdateVendorTimer(_sensorsRpc);
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
            // LibreHardwareMonitor may briefly report no CPU/GPU data after a
            // re-subscribe (data recovering). Publish nothing in that case so
            // the renderer keeps the last good frame instead of receiving null.
            if (snapshot is null)
                return;
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

    private static bool HasValue(float value) => value >= 0 && !float.IsNaN(value) && !float.IsInfinity(value);

    /// <summary>
    /// Cheap probe for readable LHM sensors (CPU/GPU temperature or usage).
    /// Returns false when LHM is initialized but has no readable data, so the
    /// caller can fall back to the stable vendor snapshot path.
    /// </summary>
    private static async Task<bool> LhmHasSensorDataAsync(SensorsGroupController group)
    {
        try
        {
            var cpuTemp = await group.GetCpuTemperatureAsync().ConfigureAwait(false);
            var cpuUsage = await group.GetCpuUsageAsync().ConfigureAwait(false);
            var gpuTemp = await group.GetGpuTemperatureAsync().ConfigureAwait(false);
            var gpuUsage = await group.GetGpuUsageAsync().ConfigureAwait(false);
            return HasValue(cpuTemp) || HasValue(cpuUsage) || HasValue(gpuTemp) || HasValue(gpuUsage);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(string? CpuName, string? GpuName)> MergeHardwareNamesAsync(
        string? cpuName,
        string? gpuName)
    {
        if (!string.IsNullOrWhiteSpace(cpuName) && !string.IsNullOrWhiteSpace(gpuName))
            return (cpuName, gpuName);

        try
        {
            var inventory = await HardwareInventoryProvider.ReadAsync().ConfigureAwait(false);
            cpuName ??= NullIfBlank(inventory.PrimaryProcessorName);
            gpuName ??= NullIfBlank(inventory.PrimaryVideoControllerName);
        }
        catch
        {
            // WMI inventory is optional; keep any names already resolved.
        }

        return (cpuName, gpuName);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// Converts LibreHardwareMonitor Data / GetGpuVram* gigabyte readings into
    /// the MiB values expected by Electron <c>*Mb</c> snapshot fields.
    /// </summary>
    internal static float? GigabytesToMegabytes(float gigabytes)
        => HasValue(gigabytes) ? gigabytes * 1024f : null;

    private static float? NullIf(float value) => HasValue(value) ? value : null;
    private static double? NullIf(double value) => value <= 0 ? null : value;
    private static int? NullIf(int value) => value < 0 ? null : value;
    private static string? NullIf(string value, string sentinel) => value == sentinel ? null : value;
}
