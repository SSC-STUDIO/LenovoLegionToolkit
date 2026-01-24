using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;
using NvAPIWrapper.GPU;
using NvAPIWrapper.Native;
using NvAPIWrapper.Native.GPU;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public abstract class AbstractSensorsController(GPUController gpuController) : ISensorsController
{
    private readonly struct GPUInfo(
        int utilization,
        int coreClock,
        int maxCoreClock,
        int memoryClock,
        int maxMemoryClock,
        int temperature,
        int maxTemperature)
    {
        public static readonly GPUInfo Empty = new(-1, -1, -1, -1, -1, -1, -1);

        public int Utilization { get; } = utilization;
        public int CoreClock { get; } = coreClock;
        public int MaxCoreClock { get; } = maxCoreClock;
        public int MemoryClock { get; } = memoryClock;
        public int MaxMemoryClock { get; } = maxMemoryClock;
        public int Temperature { get; } = temperature;
        public int MaxTemperature { get; } = maxTemperature;
    }

    private readonly SafePerformanceCounter _percentProcessorPerformanceCounter = new("Processor Information", "% Processor Performance", "_Total");
    private readonly SafePerformanceCounter _percentProcessorUtilityCounter = new("Processor Information", "% Processor Utility", "_Total");

    private int? _cpuBaseClockCache;
    private int? _cpuMaxCoreClockCache;
    private int? _cpuMaxFanSpeedCache;
    private int? _gpuMaxFanSpeedCache;

    // Sensor data cache, cache time is 100ms
    private readonly object _cacheLock = new();
    private SensorsData? _cachedSensorsData;
    private DateTime _lastCacheUpdateTime = DateTime.MinValue;
    private const int CACHE_EXPIRATION_MS = 100;

    public abstract Task<bool> IsSupportedAsync();

    public Task PrepareAsync()
    {
        _percentProcessorPerformanceCounter.Reset();
        _percentProcessorUtilityCounter.Reset();
        try
        {
            NVAPI.Initialize();
        }
        catch
        {
            // Ignore initialization errors here, individual calls might retry or fail gracefully
        }
        return Task.CompletedTask;
    }

    public async Task<SensorsData> GetDataAsync()
    {
        // Check if cache is valid, return cached data if it is
        var now = DateTime.UtcNow;
        lock (_cacheLock)
        {
            if (_cachedSensorsData.HasValue && (now - _lastCacheUpdateTime).TotalMilliseconds < CACHE_EXPIRATION_MS)
            {
                return _cachedSensorsData.Value;
            }
        }

        const int GENERIC_MAX_UTILIZATION = 100;
        const int GENERIC_MAX_TEMPERATURE = 100;

        var cpuUtilization = GetCpuUtilization(GENERIC_MAX_UTILIZATION);
        var cpuMaxCoreClock = _cpuMaxCoreClockCache ??= await GetCpuMaxCoreClockAsync().ConfigureAwait(false);
        var cpuCoreClock = GetCpuCoreClock();
        var cpuCurrentTemperature = await GetCpuCurrentTemperatureAsync().ConfigureAwait(false);
        var cpuCurrentFanSpeed = await GetCpuCurrentFanSpeedAsync().ConfigureAwait(false);
        var cpuMaxFanSpeed = _cpuMaxFanSpeedCache ??= await GetCpuMaxFanSpeedAsync().ConfigureAwait(false);

        var gpuInfo = await GetGPUInfoAsync().ConfigureAwait(false);
        var gpuCurrentTemperature = gpuInfo.Temperature >= 0 ? gpuInfo.Temperature : await GetGpuCurrentTemperatureAsync().ConfigureAwait(false);
        var gpuMaxTemperature = gpuInfo.MaxTemperature >= 0 ? gpuInfo.MaxTemperature : GENERIC_MAX_TEMPERATURE;
        var gpuCurrentFanSpeed = await GetGpuCurrentFanSpeedAsync().ConfigureAwait(false);
        var gpuMaxFanSpeed = _gpuMaxFanSpeedCache ??= await GetGpuMaxFanSpeedAsync().ConfigureAwait(false);

        var cpu = new SensorData(cpuUtilization,
            GENERIC_MAX_UTILIZATION,
            cpuCoreClock,
            cpuMaxCoreClock,
            -1,
            -1,
            cpuCurrentTemperature,
            GENERIC_MAX_TEMPERATURE,
            cpuCurrentFanSpeed,
            cpuMaxFanSpeed);
        var gpu = new SensorData(gpuInfo.Utilization,
            GENERIC_MAX_UTILIZATION,
            gpuInfo.CoreClock,
            gpuInfo.MaxCoreClock,
            gpuInfo.MemoryClock,
            gpuInfo.MaxMemoryClock,
            gpuCurrentTemperature,
            gpuMaxTemperature,
            gpuCurrentFanSpeed,
            gpuMaxFanSpeed);
        var result = new SensorsData(cpu, gpu);

        // Update cache
        lock (_cacheLock)
        {
            _cachedSensorsData = result;
            _lastCacheUpdateTime = now;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Current data: {result} [type={GetType().Name}]");

        return result;
    }

    public async Task<(int cpuFanSpeed, int gpuFanSpeed)> GetFanSpeedsAsync()
    {
        // Check if cache is valid, get fan speeds from cache if it is
        var now = DateTime.UtcNow;
        lock (_cacheLock)
        {
            if (_cachedSensorsData.HasValue && (now - _lastCacheUpdateTime).TotalMilliseconds < CACHE_EXPIRATION_MS)
            {
                return (_cachedSensorsData.Value.CPU.FanSpeed, _cachedSensorsData.Value.GPU.FanSpeed);
            }
        }

        // 缓存无效，重新获取数据
        var data = await GetDataAsync().ConfigureAwait(false);
        return (data.CPU.FanSpeed, data.GPU.FanSpeed);
    }

    protected abstract Task<int> GetCpuCurrentTemperatureAsync();

    protected abstract Task<int> GetGpuCurrentTemperatureAsync();

    protected abstract Task<int> GetCpuCurrentFanSpeedAsync();

    protected abstract Task<int> GetGpuCurrentFanSpeedAsync();

    protected abstract Task<int> GetCpuMaxFanSpeedAsync();

    protected abstract Task<int> GetGpuMaxFanSpeedAsync();

    private int GetCpuUtilization(int maxUtilization)
    {
        var result = (int)_percentProcessorUtilityCounter.NextValue();
        if (result < 0)
            return -1;
        return Math.Min(result, maxUtilization);
    }

    private int GetCpuCoreClock()
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

    private static Task<int> GetCpuMaxCoreClockAsync() => WMI.LenovoGameZoneData.GetCPUFrequencyAsync();

    private async Task<GPUInfo> GetGPUInfoAsync()
    {
        if (gpuController.IsSupported())
            await gpuController.StartAsync().ConfigureAwait(false);

        if (await gpuController.GetLastKnownStateAsync().ConfigureAwait(false) is GPUState.PoweredOff or GPUState.Unknown)
            return GPUInfo.Empty;

        try
        {
            // Ensure NvAPI is initialized
            NVAPI.Initialize();

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
                currentPerformanceState = gpu.PerformanceStatesInfo.CurrentPerformanceState.StateId;
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"GPU performance state: {currentPerformanceState}");
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
                maxTemperature);
        }
        catch
        {
            return GPUInfo.Empty;
        }
    }
}
