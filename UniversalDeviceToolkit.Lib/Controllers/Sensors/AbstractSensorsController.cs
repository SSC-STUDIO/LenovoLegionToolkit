using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;
using NvAPIWrapper.Native;
using NvAPIWrapper.Native.GPU;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public abstract class AbstractSensorsController(GPUController gpuController) : ISensorsController
{
    protected readonly record struct LibreHardwareMonitorReadings(
        int CpuUtilization,
        int CpuTemperature,
        int CpuCoreClock,
        int CpuWattage,
        double CpuVoltage,
        int GpuUtilization,
        int GpuTemperature,
        int GpuCoreClock,
        int GpuMemoryClock,
        int GpuWattage,
        double GpuVoltage);

    protected readonly struct GPUInfo(
        int utilization,
        int coreClock,
        int maxCoreClock,
        int memoryClock,
        int maxMemoryClock,
        int temperature,
        int maxTemperature,
        int wattage,
        double voltage)
    {
        public static readonly GPUInfo Empty = new(-1, -1, -1, -1, -1, -1, -1, -1, 0);

        public int Utilization { get; } = utilization;
        public int CoreClock { get; } = coreClock;
        public int MaxCoreClock { get; } = maxCoreClock;
        public int MemoryClock { get; } = memoryClock;
        public int MaxMemoryClock { get; } = maxMemoryClock;
        public int Temperature { get; } = temperature;
        public int MaxTemperature { get; } = maxTemperature;
        public int Wattage { get; } = wattage;
        public double Voltage { get; } = voltage;
    }

    private readonly SafePerformanceCounter _percentProcessorPerformanceCounter = new("Processor Information", "% Processor Performance", "_Total");
    private readonly SafePerformanceCounter _percentProcessorUtilityCounter = new("Processor Information", "% Processor Utility", "_Total");
    private readonly SafePerformanceCounter? _cpuPowerCounter = TryCreatePowerCounter();

    private int? _cpuBaseClockCache;
    private int? _cpuMaxCoreClockCache;
    private int? _cpuMaxFanSpeedCache;
    private int? _gpuMaxFanSpeedCache;

    // Sensor data cache, cache time is 100ms
    private readonly object _cacheLock = new();
    private SensorsData? _cachedSensorsData;
    private DateTime _lastCacheUpdateTime = DateTime.MinValue;
    private const int CACHE_EXPIRATION_MS = 100;
    private const int SENSOR_READ_TIMEOUT_SECONDS = 2;

    private bool _disposed;

    protected async Task<bool> CanReadSensorSnapshotAsync()
    {
        try
        {
            await GetSensorSnapshotAsync(detailed: false).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Sensor snapshot probe failed. [type={GetType().Name}]", ex);

            return false;
        }
    }

    public abstract Task<bool> IsSupportedAsync();

    protected virtual bool ShouldGateGpuInfoByLenovoController => true;

    public Task PrepareAsync()
    {
        _percentProcessorPerformanceCounter.Reset();
        _percentProcessorUtilityCounter.Reset();
        
        try { NVAPI.Initialize(); } catch
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to initialize NVAPI");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _percentProcessorPerformanceCounter.Dispose();
        _percentProcessorUtilityCounter.Dispose();
        _cpuPowerCounter?.Dispose();

        try { NVAPI.Unload(); } catch
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to unload NVAPI");
        }

        GC.SuppressFinalize(this);
    }

    private double _cpuMinVoltage = double.MaxValue;
    private double _cpuMaxVoltage = double.MinValue;
    private int _cpuMinTemp = int.MaxValue;
    private int _cpuMaxTemp = int.MinValue;
    
    private double _gpuMinVoltage = double.MaxValue;
    private double _gpuMaxVoltage = double.MinValue;
    private int _gpuMinTemp = int.MaxValue;
    private int _gpuMaxTemp = int.MinValue;

    public async Task<SensorsData> GetDataAsync(bool detailed = false)
    {
        // Check if cache is valid, return cached data if it is
        var now = DateTime.UtcNow;
        lock (_cacheLock)
        {
            if (!detailed && _cachedSensorsData.HasValue && (now - _lastCacheUpdateTime).TotalMilliseconds < CACHE_EXPIRATION_MS)
            {
                return _cachedSensorsData.Value;
            }
        }

        try
        {
            // Apply a 2-second timeout to prevent slow sensors from blocking the UI.
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(SENSOR_READ_TIMEOUT_SECONDS));
            var snapshotTask = GetSensorSnapshotAsync(detailed);
            var timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, timeoutCts.Token);

            if (await Task.WhenAny(snapshotTask, timeoutTask).ConfigureAwait(false) == timeoutTask)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Sensor read timed out after {SENSOR_READ_TIMEOUT_SECONDS}s, falling back to cache. [type={GetType().Name}]");

                lock (_cacheLock)
                {
                    if (_cachedSensorsData.HasValue)
                        return _cachedSensorsData.Value;
                }

                return new SensorsData(new SensorData(), new SensorData());
            }

            var (cpu, gpu) = await snapshotTask.ConfigureAwait(false);

            var result = new SensorsData(cpu, gpu);

            // Update cache only for the fast summary path.
            if (!detailed)
            {
                lock (_cacheLock)
                {
                    _cachedSensorsData = result;
                    _lastCacheUpdateTime = now;
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Current data: {result} [type={GetType().Name}]");

            return result;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Sensor read failed. [type={GetType().Name}]", ex);

            lock (_cacheLock)
            {
                if (_cachedSensorsData.HasValue)
                    return _cachedSensorsData.Value;
            }

            return new SensorsData(new SensorData(), new SensorData());
        }
    }

    private async Task<(SensorData cpu, SensorData gpu)> GetSensorSnapshotAsync(bool detailed)
    {
        const int GENERIC_MAX_UTILIZATION = 100;
        const int GENERIC_MAX_TEMPERATURE = 100;
        Task<LibreHardwareMonitorReadings?>? libreHardwareMonitorReadingsTask = null;
        Task<LibreHardwareMonitorReadings?> GetLibreHardwareMonitorReadingsOnceAsync() =>
            libreHardwareMonitorReadingsTask ??= GetLibreHardwareMonitorReadingsAsync();

        var cpuUtilization = SafeRead(() => GetCpuUtilization(GENERIC_MAX_UTILIZATION), -1, "CPU utilization");
        var cpuMaxCoreClock = await SafeReadAsync(async () => _cpuMaxCoreClockCache ??= await GetCpuMaxCoreClockAsync().ConfigureAwait(false), -1, "CPU max core clock").ConfigureAwait(false);
        var cpuCoreClock = SafeRead(GetCpuCoreClock, -1, "CPU core clock");
        var cpuCurrentTemperature = NormalizeTemperatureReading(await SafeReadAsync(GetCpuCurrentTemperatureAsync, -1, "CPU temperature").ConfigureAwait(false));
        var cpuCurrentFanSpeed = await SafeReadAsync(GetCpuCurrentFanSpeedAsync, -1, "CPU fan speed").ConfigureAwait(false);
        var cpuMaxFanSpeed = await SafeReadAsync(async () => _cpuMaxFanSpeedCache ??= await GetCpuMaxFanSpeedAsync().ConfigureAwait(false), -1, "CPU max fan speed").ConfigureAwait(false);

        double cpuVoltage = 0;
        int cpuWattage = -1;

        if (cpuUtilization < 0 || cpuCoreClock < 0 || cpuCurrentTemperature < 0)
        {
            var libreHardwareMonitorReadings = await GetLibreHardwareMonitorReadingsOnceAsync().ConfigureAwait(false);
            if (libreHardwareMonitorReadings is { } readings)
            {
                if (cpuUtilization < 0 && readings.CpuUtilization >= 0)
                    cpuUtilization = readings.CpuUtilization;
                if (cpuCoreClock < 0 && readings.CpuCoreClock >= 0)
                    cpuCoreClock = readings.CpuCoreClock;
                if (cpuCurrentTemperature < 0 && readings.CpuTemperature > 0)
                    cpuCurrentTemperature = readings.CpuTemperature;
            }
        }

        if (cpuCurrentTemperature < 0)
        {
            var fallback = await SensorReadingHelper.GetCpuTemperatureFromAcpiAsync().ConfigureAwait(false);
            if (fallback > 0 && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"CPU temperature from ACPI thermal zone fallback: {fallback}C");

            cpuCurrentTemperature = fallback > 0 ? fallback : -1;
        }

        if (detailed)
        {
            cpuVoltage = await SafeReadAsync(WMI.Win32.Processor.GetVoltageAsync, 0d, "CPU voltage").ConfigureAwait(false);
            cpuWattage = await SafeReadAsync(GetCpuWattageAsync, -1, "CPU wattage").ConfigureAwait(false);

            if (cpuVoltage <= 0 || cpuWattage < 0)
            {
                var libreHardwareMonitorReadings = await GetLibreHardwareMonitorReadingsOnceAsync().ConfigureAwait(false);
                if (libreHardwareMonitorReadings is { } readings)
                {
                    if (cpuVoltage <= 0 && readings.CpuVoltage > 0)
                        cpuVoltage = readings.CpuVoltage;
                    if (cpuWattage < 0 && readings.CpuWattage > 0)
                        cpuWattage = readings.CpuWattage;
                }
            }
        }

        var gpuInfo = await SafeReadAsync(GetGPUInfoAsync, GPUInfo.Empty, "GPU info").ConfigureAwait(false);
        var gpuUtilization = gpuInfo.Utilization;
        var gpuCoreClock = gpuInfo.CoreClock;
        var gpuMaxCoreClock = gpuInfo.MaxCoreClock;
        var gpuMemoryClock = gpuInfo.MemoryClock;
        var gpuMaxMemoryClock = gpuInfo.MaxMemoryClock;
        var gpuCurrentTemperature = gpuInfo.Temperature > 0
            ? gpuInfo.Temperature
            : NormalizeTemperatureReading(await SafeReadAsync(GetGpuCurrentTemperatureAsync, -1, "GPU temperature").ConfigureAwait(false));
        var gpuMaxTemperature = gpuInfo.MaxTemperature >= 0 ? gpuInfo.MaxTemperature : GENERIC_MAX_TEMPERATURE;
        var gpuWattage = gpuInfo.Wattage;
        var gpuVoltage = gpuInfo.Voltage;
        var gpuCurrentFanSpeed = await SafeReadAsync(GetGpuCurrentFanSpeedAsync, -1, "GPU fan speed").ConfigureAwait(false);
        var gpuMaxFanSpeed = await SafeReadAsync(async () => _gpuMaxFanSpeedCache ??= await GetGpuMaxFanSpeedAsync().ConfigureAwait(false), -1, "GPU max fan speed").ConfigureAwait(false);

        if (gpuUtilization < 0 || gpuCoreClock < 0 || gpuCurrentTemperature < 0 || (detailed && (gpuVoltage <= 0 || gpuWattage < 0)))
        {
            var libreHardwareMonitorReadings = await GetLibreHardwareMonitorReadingsOnceAsync().ConfigureAwait(false);
            if (libreHardwareMonitorReadings is { } readings)
            {
                if (gpuUtilization < 0 && readings.GpuUtilization >= 0)
                    gpuUtilization = readings.GpuUtilization;
                if (gpuCoreClock < 0 && readings.GpuCoreClock >= 0)
                    gpuCoreClock = readings.GpuCoreClock;
                if (gpuMemoryClock < 0 && readings.GpuMemoryClock >= 0)
                    gpuMemoryClock = readings.GpuMemoryClock;
                if (gpuCurrentTemperature < 0 && readings.GpuTemperature > 0)
                    gpuCurrentTemperature = readings.GpuTemperature;
                if (detailed && gpuWattage < 0 && readings.GpuWattage > 0)
                    gpuWattage = readings.GpuWattage;
                if (detailed && gpuVoltage <= 0 && readings.GpuVoltage > 0)
                    gpuVoltage = readings.GpuVoltage;
            }
        }

        if (gpuMaxCoreClock < 0 && gpuCoreClock >= 0)
            gpuMaxCoreClock = gpuCoreClock;
        if (gpuMaxMemoryClock < 0 && gpuMemoryClock >= 0)
            gpuMaxMemoryClock = gpuMemoryClock;

        // Update Min/Max records
        if (cpuVoltage > 0)
        {
            if (cpuVoltage < _cpuMinVoltage) _cpuMinVoltage = cpuVoltage;
            if (cpuVoltage > _cpuMaxVoltage) _cpuMaxVoltage = cpuVoltage;
        }
        if (cpuCurrentTemperature > 0)
        {
            if (cpuCurrentTemperature < _cpuMinTemp) _cpuMinTemp = cpuCurrentTemperature;
            if (cpuCurrentTemperature > _cpuMaxTemp) _cpuMaxTemp = cpuCurrentTemperature;
        }
        
        if (gpuVoltage > 0)
        {
            if (gpuVoltage < _gpuMinVoltage) _gpuMinVoltage = gpuVoltage;
            if (gpuVoltage > _gpuMaxVoltage) _gpuMaxVoltage = gpuVoltage;
        }
        if (gpuCurrentTemperature > 0)
        {
            if (gpuCurrentTemperature < _gpuMinTemp) _gpuMinTemp = gpuCurrentTemperature;
            if (gpuCurrentTemperature > _gpuMaxTemp) _gpuMaxTemp = gpuCurrentTemperature;
        }

        var cpu = new SensorData(cpuUtilization,
            GENERIC_MAX_UTILIZATION,
            cpuCoreClock,
            cpuMaxCoreClock,
            -1,
            -1,
            cpuCurrentTemperature,
            GENERIC_MAX_TEMPERATURE,
            cpuWattage,
            cpuVoltage,
            cpuCurrentFanSpeed,
            cpuMaxFanSpeed).WithMinMax(_cpuMinVoltage, _cpuMaxVoltage, _cpuMinTemp, _cpuMaxTemp);
            
        var gpu = new SensorData(gpuUtilization,
            GENERIC_MAX_UTILIZATION,
            gpuCoreClock,
            gpuMaxCoreClock,
            gpuMemoryClock,
            gpuMaxMemoryClock,
            gpuCurrentTemperature,
            gpuMaxTemperature,
            gpuWattage,
            gpuVoltage,
            gpuCurrentFanSpeed,
            gpuMaxFanSpeed).WithMinMax(_gpuMinVoltage, _gpuMaxVoltage, _gpuMinTemp, _gpuMaxTemp);

        return (cpu, gpu);
    }

    public async Task<(int cpuFanSpeed, int gpuFanSpeed)> GetFanSpeedsAsync()
    {
        var now = DateTime.UtcNow;
        lock (_cacheLock)
        {
            if (_cachedSensorsData.HasValue && (now - _lastCacheUpdateTime).TotalMilliseconds < CACHE_EXPIRATION_MS)
            {
                return (_cachedSensorsData.Value.CPU.FanSpeed, _cachedSensorsData.Value.GPU.FanSpeed);
            }
        }

        var data = await GetDataAsync().ConfigureAwait(false);
        return (data.CPU.FanSpeed, data.GPU.FanSpeed);
    }

    protected abstract Task<int> GetCpuCurrentTemperatureAsync();

    protected abstract Task<int> GetGpuCurrentTemperatureAsync();

    protected abstract Task<int> GetCpuCurrentFanSpeedAsync();

    protected abstract Task<int> GetGpuCurrentFanSpeedAsync();

    protected virtual Task<int> GetPchCurrentTemperatureAsync() => Task.FromResult(-1);

    protected virtual Task<int> GetPchCurrentFanSpeedAsync() => Task.FromResult(-1);

    protected abstract Task<int> GetCpuMaxFanSpeedAsync();

    protected abstract Task<int> GetGpuMaxFanSpeedAsync();

    protected virtual Task<int> GetPchMaxFanSpeedAsync() => Task.FromResult(-1);

    protected virtual async Task<LibreHardwareMonitorReadings?> GetLibreHardwareMonitorReadingsAsync()
    {
        try
        {
            if (IoCContainer.TryResolve<SensorsGroupController>() is not { } sensorsGroupController)
                return null;

            if (!sensorsGroupController.IsLibreHardwareMonitorInitialized())
                _ = await sensorsGroupController.IsSupportedAsync().ConfigureAwait(false);

            if (!sensorsGroupController.IsLibreHardwareMonitorInitialized())
                return null;

            await sensorsGroupController.UpdateAsync().ConfigureAwait(false);

            return new LibreHardwareMonitorReadings(
                NormalizeLibreHardwareMonitorMetric(await sensorsGroupController.GetCpuUsageAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorMetric(await sensorsGroupController.GetCpuTemperatureAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorMetric(await sensorsGroupController.GetCpuCoreClockAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorPositiveMetric(await sensorsGroupController.GetCpuPowerAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorVoltage(await sensorsGroupController.GetCpuVoltageAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorMetric(await sensorsGroupController.GetGpuUsageAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorMetric(await sensorsGroupController.GetGpuTemperatureAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorMetric(await sensorsGroupController.GetGpuCoreClockAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorMetric(await sensorsGroupController.GetGpuMemoryClockAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorPositiveMetric(await sensorsGroupController.GetGpuPowerAsync().ConfigureAwait(false)),
                NormalizeLibreHardwareMonitorVoltage(await sensorsGroupController.GetGpuVoltageAsync().ConfigureAwait(false)));
        }
        catch
        {
            return null;
        }
    }

    private static int NormalizeLibreHardwareMonitorMetric(float value) =>
        value >= 0 ? (int)Math.Round(value) : -1;

    private static int NormalizeLibreHardwareMonitorPositiveMetric(float value) =>
        value > 0 ? (int)Math.Round(value) : -1;

    private static double NormalizeLibreHardwareMonitorVoltage(float value) =>
        value > 0 ? Math.Round(value, 3) : 0;

    private static int NormalizeTemperatureReading(int value) =>
        value > 0 ? value : -1;

    protected virtual int GetCpuUtilization(int maxUtilization)
    {
        var result = (int)_percentProcessorUtilityCounter.NextValue();
        if (result < 0)
            return -1;
        return Math.Min(result, maxUtilization);
    }

    protected virtual int GetCpuCoreClock()
    {
        var baseClock = _cpuBaseClockCache ??= GetCpuBaseClock();
        var clock = (int)(baseClock * (_percentProcessorPerformanceCounter.NextValue() / 100f));
        if (clock < 1)
            return -1;
        return clock;
    }

    private static unsafe int GetCpuBaseClock()
    {
        var ptr = IntPtr.Zero;
        try
        {
            PInvoke.GetSystemInfo(out var systemInfo);

            var numberOfProcessors = Math.Min(32, (int)systemInfo.dwNumberOfProcessors);
            var infoSize = Marshal.SizeOf<PROCESSOR_POWER_INFORMATION>();
            var infosSize = numberOfProcessors * infoSize;

            ptr = Marshal.AllocHGlobal(infosSize);

            var result = PInvoke.CallNtPowerInformation(POWER_INFORMATION_LEVEL.ProcessorInformation,
                null,
                0,
                ptr.ToPointer(),
                (uint)infosSize);
            if (result != 0)
                return 0;

            var infos = new PROCESSOR_POWER_INFORMATION[numberOfProcessors];

            for (var i = 0; i < infos.Length; i++)
                infos[i] = Marshal.PtrToStructure<PROCESSOR_POWER_INFORMATION>(IntPtr.Add(ptr, i * infoSize));

            return (int)infos.Select(p => p.MaxMhz).Max();
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>
    /// Asynchronously gets the CPU maximum core clock frequency.
    /// </summary>
    /// <returns>CPU maximum core clock frequency in MHz.</returns>
    /// <remarks>
    /// This method can be overridden in unit tests to avoid actual WMI calls.
    /// </remarks>
    protected virtual Task<int> GetCpuMaxCoreClockAsync() => WMI.LenovoGameZoneData.GetCPUFrequencyAsync();

    private static SafePerformanceCounter? TryCreatePowerCounter()
    {
        try
        {
            // Try to create a performance counter for CPU power consumption
            // Windows 10/11 may provide "Processor Information" category with "Power" counter
            // Note: This may not be available on all systems
            return new SafePerformanceCounter("Processor Information", "Power", "_Total");
        }
        catch
        {
            // Counter not available, return null
            return null;
        }
    }

    protected virtual async Task<int> GetCpuWattageAsync()
    {
        await Task.Yield();

        // Try method 1: Performance counter (if available)
        var performanceCounterWattage = GetCpuWattageFromPerformanceCounter();
        if (performanceCounterWattage > 0)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"CPU power from performance counter: {performanceCounterWattage}W");
            return performanceCounterWattage;
        }

        // Try method 2: WMI query for power meter (if available)
        try
        {
            var wattage = await GetCpuWattageFromWmiAsync().ConfigureAwait(false);
            if (wattage > 0)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"CPU power from WMI: {wattage}W");
                return wattage;
            }

            if (wattage == 0 && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("CPU power from WMI was 0W; continuing to LibreHardwareMonitor fallback.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get CPU power from WMI: {ex.Message}");
        }

        // Try method 3: reuse LibreHardwareMonitor package power if that path is already available
        var libreHardwareMonitorWattage = await GetCpuWattageFromLibreHardwareMonitorAsync().ConfigureAwait(false);
        if (libreHardwareMonitorWattage > 0)
            return libreHardwareMonitorWattage;

        // Method not available, return -1
        return -1;
    }

    protected virtual int GetCpuWattageFromPerformanceCounter()
    {
        if (_cpuPowerCounter == null)
            return -1;

        try
        {
            var powerValue = _cpuPowerCounter.NextValue();
            var wattage = SensorReadingHelper.NormalizePowerReadingToWatts(powerValue);
            if (wattage > 0 && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"CPU power performance counter raw value: {powerValue}");

            return wattage;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get CPU power from performance counter: {ex.Message}");
            return -1;
        }
    }

    protected virtual Task<int> GetCpuWattageFromWmiAsync() =>
        SensorReadingHelper.GetCpuWattageFromWmiAsync();

    protected virtual async Task<int> GetCpuWattageFromLibreHardwareMonitorAsync()
    {
        try
        {
            if (IoCContainer.TryResolve<SensorsGroupController>() is { } sensorsGroupController)
            {
                if (!sensorsGroupController.IsLibreHardwareMonitorInitialized())
                    _ = await sensorsGroupController.IsSupportedAsync().ConfigureAwait(false);

                if (sensorsGroupController.IsLibreHardwareMonitorInitialized())
                {
                    await sensorsGroupController.UpdateAsync().ConfigureAwait(false);

                    var cpuPower = await sensorsGroupController.GetCpuPowerAsync().ConfigureAwait(false);
                    if (cpuPower > 0)
                    {
                        var wattage = (int)Math.Round(cpuPower);
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"CPU power from LibreHardwareMonitor: {wattage}W (raw: {cpuPower})");
                        return wattage;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get CPU power from LibreHardwareMonitor: {ex.Message}");
        }

        return -1;
    }

    private static async Task<(int wattage, double voltage)> GetGpuInfoFromNvidiaSmiAsync()
    {
        try
        {
            var executablePath = FindNvidiaSmiPath();
            if (executablePath is null)
                return (-1, 0);

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "-q",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return (-1, 0);

            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            int wattage = -1;
            double voltage = 0;

            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            bool inPowerReadings = false;
            bool inVoltageReadings = false;

            foreach (var line in lines)
            {
                if (line.Contains("GPU Power Readings"))
                {
                    inPowerReadings = true;
                    inVoltageReadings = false;
                    continue;
                }
                if (line.Contains("Voltage"))
                {
                    inVoltageReadings = true;
                    inPowerReadings = false;
                    continue;
                }

                var trimmed = line.Trim();
                if (inPowerReadings && trimmed.StartsWith("Power Draw"))
                {
                    var parts = trimmed.Split(':');
                    if (parts.Length > 1)
                    {
                        var val = parts[1].Trim().Split(' ')[0];
                        if (double.TryParse(val, global::System.Globalization.CultureInfo.InvariantCulture, out var w))
                            wattage = (int)w;
                    }
                    inPowerReadings = false; 
                }
                else if (inPowerReadings && (trimmed.StartsWith("Instantaneous Power Draw") || trimmed.StartsWith("Average Power Draw")))
                {
                    var parts = trimmed.Split(':');
                    if (parts.Length > 1)
                    {
                        var val = parts[1].Trim().Split(' ')[0];
                        if (double.TryParse(val, global::System.Globalization.CultureInfo.InvariantCulture, out var w))
                            wattage = (int)w;
                    }
                }
                else if (inVoltageReadings && trimmed.StartsWith("Graphics"))
                {
                    var parts = trimmed.Split(':');
                    if (parts.Length > 1)
                    {
                        var val = parts[1].Trim().Split(' ')[0];
                        if (double.TryParse(val, global::System.Globalization.CultureInfo.InvariantCulture, out var v))
                            voltage = v / 1000.0;
                    }
                    inVoltageReadings = false;
                }
            }

            return (wattage, voltage);
        }
        catch
        {
            return (-1, 0);
        }
    }

    protected internal static string? FindNvidiaSmiPath()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new[]
        {
            Path.Combine(programFiles, "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe"),
            Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? programFiles, "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    protected virtual async Task<GPUInfo> GetGPUInfoAsync()
    {
        if (ShouldGateGpuInfoByLenovoController)
        {
            if (await gpuController.IsSupportedAsync().ConfigureAwait(false))
                await gpuController.StartAsync().ConfigureAwait(false);

            if (await gpuController.GetLastKnownStateAsync().ConfigureAwait(false) is GPUState.PoweredOff or GPUState.Unknown)
                return GPUInfo.Empty;
        }

        try
        {
            var gpu = NVAPI.GetGPU();
            if (gpu is null)
                return GPUInfo.Empty;

            var utilization = Math.Min(100, Math.Max(gpu.UsageInformation.GPU.Percentage, gpu.UsageInformation.VideoEngine.Percentage));

            var currentCoreClock = (int)gpu.CurrentClockFrequencies.GraphicsClock.Frequency / 1000;
            var currentMemoryClock = (int)gpu.CurrentClockFrequencies.MemoryClock.Frequency / 1000;

            var maxCoreClock = (int)gpu.BoostClockFrequencies.GraphicsClock.Frequency / 1000;
            var maxMemoryClock = (int)gpu.BoostClockFrequencies.MemoryClock.Frequency / 1000;

            // Get current performance state
            var currentPerformanceState = PerformanceStateId.P0_3DPerformance;
            try
            {
                var stateIdString = gpu.PerformanceStatesInfo.CurrentPerformanceState.StateId.ToString();
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"GPU performance state: {stateIdString}");
                    
                // Try to parse the current performance state
                if (Enum.TryParse<PerformanceStateId>(stateIdString, out var parsedState))
                {
                    currentPerformanceState = parsedState;
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to get current performance state: {ex.Message}");
            }

            var states = GPUApi.GetPerformanceStates20(gpu.Handle);
            
            // Try to get overclock offsets for current performance state, fall back to P0 if not available
            int maxCoreClockOffset = 0;
            int maxMemoryClockOffset = 0;
            try
            {
                maxCoreClockOffset = states.Clocks[currentPerformanceState][0].FrequencyDeltaInkHz.DeltaValue / 1000;
                maxMemoryClockOffset = states.Clocks[currentPerformanceState][1].FrequencyDeltaInkHz.DeltaValue / 1000;
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Using overclock offsets from {currentPerformanceState}: core={maxCoreClockOffset}MHz, memory={maxMemoryClockOffset}MHz");
            }
            catch
            {
                // Fall back to P0_3DPerformance if current state doesn't have offsets
                try
                {
                    maxCoreClockOffset = states.Clocks[PerformanceStateId.P0_3DPerformance][0].FrequencyDeltaInkHz.DeltaValue / 1000;
                    maxMemoryClockOffset = states.Clocks[PerformanceStateId.P0_3DPerformance][1].FrequencyDeltaInkHz.DeltaValue / 1000;
                    
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Falling back to P0_3DPerformance offsets: core={maxCoreClockOffset}MHz, memory={maxMemoryClockOffset}MHz");
                }
                catch
                {
                    // No overclock offsets available
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"No overclock offsets available");
                }
            }

            var temperatureSensor = gpu.ThermalInformation.ThermalSensors.FirstOrDefault();
            var currentTemperature = temperatureSensor?.CurrentTemperature ?? -1;
            var maxTemperature = temperatureSensor?.DefaultMaximumTemperature ?? -1;

            // Get GPU Power and Voltage
            int currentWattage = -1;
            double currentVoltage = 0;
            
            // Fallback: Try method 1: NvAPIWrapper reflection
            if (currentWattage < 0)
            {
                try
                {
                    var powerInfoProp = gpu.GetType().GetProperty("PowerInformation");
                    if (powerInfoProp != null)
                    {
                        var powerInfo = powerInfoProp.GetValue(gpu);
                        if (powerInfo != null)
                        {
                            var powerEntriesProp = powerInfo.GetType().GetProperty("PowerEntries");
                            if (powerEntriesProp != null)
                            {
                                var powerEntries = powerEntriesProp.GetValue(powerInfo) as IEnumerable;
                                if (powerEntries != null)
                                {
                                    var firstEntry = powerEntries.Cast<object>().FirstOrDefault();
                                    if (firstEntry != null)
                                    {
                                        var powerProp = firstEntry.GetType().GetProperty("Power");
                                        if (powerProp != null)
                                        {
                                            var powerValue = powerProp.GetValue(firstEntry);
                                            if (powerValue != null)
                                            {
                                                currentWattage = (int)(Convert.ToDouble(powerValue) / 1000.0);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to get NVML data: {ex.Message}", ex);
                }
            }
            
            // Try to get voltage via VoltageSensor property
            try
            {
                var voltageSensorProp = gpu.GetType().GetProperty("VoltageSensor");
                
                if (voltageSensorProp != null)
                {
                    var voltageSensor = voltageSensorProp.GetValue(gpu);
                    if (voltageSensor != null)
                    {
                        var isAvailableProp = voltageSensor.GetType().GetProperty("IsAvailable");
                        var currentVoltageProp = voltageSensor.GetType().GetProperty("CurrentVoltage");
                        
                        if (isAvailableProp != null && currentVoltageProp != null)
                        {
                            var isAvailable = isAvailableProp.GetValue(voltageSensor);
                            if (isAvailable is bool available && available)
                            {
                                var voltageValue = currentVoltageProp.GetValue(voltageSensor);
                                if (voltageValue != null)
                                {
                                    // Voltage is typically in millivolts, convert to volts
                                    if (voltageValue is uint voltageUint)
                                    {
                                        currentVoltage = voltageUint / 1000.0;
                                    }
                                    else if (voltageValue is int voltageInt)
                                    {
                                        currentVoltage = voltageInt / 1000.0;
                                    }
                                    else if (voltageValue is float voltageFloat)
                                    {
                                        currentVoltage = voltageFloat;
                                    }
                                    else if (voltageValue is double voltageDouble)
                                    {
                                        currentVoltage = voltageDouble;
                                    }
                                    else
                                    {
                                        currentVoltage = Convert.ToDouble(voltageValue);
                                    }
                                    
                                    if (Log.Instance.IsTraceEnabled)
                                        Log.Instance.Trace($"GPU voltage: {currentVoltage}V (raw: {voltageValue})");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to get GPU voltage: {ex.Message}");
            }

            // Try to get Wattage via PrivatePowerTopologiesStatusV1 (Reflection)
            try
            {
                // Note: The structure PrivatePowerTopologiesStatusV1 has 'PowerUsageInPCM'.
                // If NvAPIWrapper exposes PowerTopologyInformation (usually via 'PowerTopology' property on PhysicalGPU)
                
                // Let's try 'PowerTopology' property
                var powerTopologyProp = gpu.GetType().GetProperty("PowerTopology");
                if (powerTopologyProp != null)
                {
                    var powerTopology = powerTopologyProp.GetValue(gpu);
                    // Check for 'Status' property
                    var statusProp = powerTopology?.GetType().GetProperty("Status");
                    if (statusProp != null)
                    {
                        var status = statusProp.GetValue(powerTopology);
                        // PrivatePowerTopologiesStatusV1 exposes 'PowerPolicyStatusEntries' (typo in lib?) or 'Entries'
                        // The decompiled code showed: public PowerTopologiesStatusEntry[] PowerPolicyStatusEntries { get => ... }
                        // It seems the property name in wrapper might be 'PowerPolicyStatusEntries' even for Topology status.
                        
                        var entriesProp = status?.GetType().GetProperty("PowerPolicyStatusEntries");
                        if (entriesProp != null)
                        {
                            var entries = entriesProp.GetValue(status) as Array;
                            if (entries != null)
                            {
                                foreach (var entry in entries)
                                {
                                    if (entry == null) continue;
                                    
                                    // entry is PowerTopologiesStatusEntry
                                    var domainProp = entry.GetType().GetProperty("Domain");
                                    var usageProp = entry.GetType().GetProperty("PowerUsageInPCM");
                                    
                                    if (domainProp != null && usageProp != null)
                                    {
                                        var domainValue = domainProp.GetValue(entry);
                                        if (domainValue != null)
                                        {
                                            var domain = domainValue.ToString();
                                            // Domain is likely an enum PowerTopologyDomain. GPU or Board.
                                            if (domain == "GPU" || domain == "Board") 
                                            {
                                                // PowerUsageInPCM is in milliwatts usually for this struct?
                                                // Or is it 1/1000 percent? 
                                                // "PCM" = Per Cent Mille = 1/1000 %.
                                                // If it is PCM, we need the TDP to calculate Watts.
                                                
                                                // However, some sources say for Topology status it might be absolute power in mW.
                                                // Let's assume mW for now because we don't have TDP readily available in this context easily.
                                                // Actually, 'PowerUsageInPCM' name suggests percentage.
                                                // But let's look at the value. If it is e.g. 50000, it is 50%.
                                                // If it is e.g. 30000, it is 30W? No.
                                                
                                                // Let's try to find if there is a 'PowerUsage' property directly in Watts on the entry?
                                                // The struct only showed PowerUsageInPCM.
                                                
                                                // If we can't get Watts, we skip.
                                                // But user insists on getting power.
                                                
                                                // Let's try another property: 'CurrentPower' on PhysicalGPU?
                                                // No such property in standard wrapper.
                                                
                                                // Let's try to interpret PCM as mW? 
                                                // In some NvAPI contexts, it is mW.
                                                // Let's store it as mW if > 1000? 
                                                // If it is %, 100% = 100000.
                                                // If it is mW, 100W = 100000.
                                                // It is ambiguous.
                                                
                                                // Let's assume it is mW.
                                                var val = Convert.ToUInt32(usageProp.GetValue(entry));
                                                if (Log.Instance.IsTraceEnabled && val > 0)
                                                    Log.Instance.Trace($"Ignoring ambiguous GPU PowerUsageInPCM reading: {val}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to get GPU info: {ex.Message}", ex);
            }

            // Final fallback: nvidia-smi
            if (currentWattage < 0 || currentVoltage == 0)
            {
                var (smiWattage, smiVoltage) = await GetGpuInfoFromNvidiaSmiAsync().ConfigureAwait(false);
                if (currentWattage < 0 && smiWattage >= 0)
                    currentWattage = smiWattage;
                if (currentVoltage == 0 && smiVoltage > 0)
                    currentVoltage = smiVoltage;
            }

            // Debug logging
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"GPU frequencies - Utilization: {utilization}%");
                Log.Instance.Trace($"  Current: core={currentCoreClock}MHz, memory={currentMemoryClock}MHz");
                Log.Instance.Trace($"  Boost: core={maxCoreClock}MHz, memory={maxMemoryClock}MHz");
                Log.Instance.Trace($"  Offsets: core={maxCoreClockOffset}MHz, memory={maxMemoryClockOffset}MHz");
                Log.Instance.Trace($"  Final max: core={maxCoreClock + maxCoreClockOffset}MHz, memory={maxMemoryClock + maxMemoryClockOffset}MHz");
            }

            return new(utilization,
                currentCoreClock,
                maxCoreClock + maxCoreClockOffset,
                currentMemoryClock,
                maxMemoryClock + maxMemoryClockOffset,
                currentTemperature,
                maxTemperature,
                currentWattage,
                currentVoltage);
        }
        catch
        {
            return GPUInfo.Empty;
        }
    }

    private static T SafeRead<T>(Func<T> operation, T fallback, string metricName)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read {metricName}.", ex);

            return fallback;
        }
    }

    private static async Task<T> SafeReadAsync<T>(Func<Task<T>> operation, T fallback, string metricName)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read {metricName}.", ex);

            return fallback;
        }
    }
}
