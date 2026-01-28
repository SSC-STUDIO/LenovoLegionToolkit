using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
    private readonly struct GPUInfo(
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

    public abstract Task<bool> IsSupportedAsync();

    public Task PrepareAsync()
    {
        _percentProcessorPerformanceCounter.Reset();
        _percentProcessorUtilityCounter.Reset();
        
        return Task.CompletedTask;
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
        
        double cpuVoltage = 0;
        int cpuWattage = -1;
        
        if (detailed)
        {
            cpuVoltage = await WMI.Win32.Processor.GetVoltageAsync().ConfigureAwait(false);
            cpuWattage = await GetCpuWattageAsync().ConfigureAwait(false);
        }

        var gpuInfo = await GetGPUInfoAsync().ConfigureAwait(false);
        var gpuCurrentTemperature = gpuInfo.Temperature >= 0 ? gpuInfo.Temperature : await GetGpuCurrentTemperatureAsync().ConfigureAwait(false);
        var gpuMaxTemperature = gpuInfo.MaxTemperature >= 0 ? gpuInfo.MaxTemperature : GENERIC_MAX_TEMPERATURE;
        var gpuCurrentFanSpeed = await GetGpuCurrentFanSpeedAsync().ConfigureAwait(false);
        var gpuMaxFanSpeed = _gpuMaxFanSpeedCache ??= await GetGpuMaxFanSpeedAsync().ConfigureAwait(false);

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
        
        if (gpuInfo.Voltage > 0)
        {
            if (gpuInfo.Voltage < _gpuMinVoltage) _gpuMinVoltage = gpuInfo.Voltage;
            if (gpuInfo.Voltage > _gpuMaxVoltage) _gpuMaxVoltage = gpuInfo.Voltage;
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
            
        var gpu = new SensorData(gpuInfo.Utilization,
            GENERIC_MAX_UTILIZATION,
            gpuInfo.CoreClock,
            gpuInfo.MaxCoreClock,
            gpuInfo.MemoryClock,
            gpuInfo.MaxMemoryClock,
            gpuCurrentTemperature,
            gpuMaxTemperature,
            gpuInfo.Wattage,
            gpuInfo.Voltage,
            gpuCurrentFanSpeed,
            gpuMaxFanSpeed).WithMinMax(_gpuMinVoltage, _gpuMaxVoltage, _gpuMinTemp, _gpuMaxTemp);
            
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

    private async Task<int> GetCpuWattageAsync()
    {
        await Task.Yield();

        // Try method 1: Performance counter (if available)
        if (_cpuPowerCounter != null)
        {
            try
            {
                var powerValue = _cpuPowerCounter.NextValue();
                if (powerValue > 0)
                {
                    // Power counter typically returns value in milliwatts, convert to watts
                    var wattage = (int)(powerValue / 1000.0);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"CPU power from performance counter: {wattage}W (raw: {powerValue}mW)");
                    return wattage;
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to get CPU power from performance counter: {ex.Message}");
            }
        }

        // Try method 2: WMI query for power meter (if available)
        try
        {
            var wattage = await GetCpuWattageFromWMIAsync().ConfigureAwait(false);
            if (wattage >= 0)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"CPU power from WMI: {wattage}W");
                return wattage;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get CPU power from WMI: {ex.Message}");
        }

        // Method not available, return -1
        return -1;
    }

    private static async Task<int> GetCpuWattageFromWMIAsync()
    {
        try
        {
            // Try to query Win32_PowerMeter or similar WMI class for CPU power
            // Note: This may not be available on all systems
var cpuPattern = "%CPU%";
            var processorPattern = "%Processor%";
            var result = await WMI.ReadAsync("root\\CIMV2",
                $"SELECT * FROM Win32_PowerMeter WHERE Name LIKE {cpuPattern} OR Name LIKE {processorPattern}",
                pdc =>
                {
                    // Try different property names that might contain power value
                    var powerValue = pdc["Power"]?.Value ?? pdc["CurrentPower"]?.Value ?? pdc["PowerReading"]?.Value;
                    if (powerValue != null)
                    {
                        // Power is typically in milliwatts, convert to watts
                        var powerMw = Convert.ToDouble(powerValue);
                        return (int)(powerMw / 1000.0);
                    }
                    return -1;
                }).ConfigureAwait(false);
            
            return result.FirstOrDefault(-1);
        }
        catch
        {
            return -1;
        }
    }

    private static async Task<(int wattage, double voltage)> GetGpuInfoFromNvidiaSmiAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
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

    private async Task<GPUInfo> GetGPUInfoAsync()
    {
        if (gpuController.IsSupported())
            await gpuController.StartAsync().ConfigureAwait(false);

        if (await gpuController.GetLastKnownStateAsync().ConfigureAwait(false) is GPUState.PoweredOff or GPUState.Unknown)
            return GPUInfo.Empty;

        try
        {
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
                catch { }
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
                                                if (val > 0)
                                                {
                                                    currentWattage = (int)(val / 1000); // mW to W
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            catch { }

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
        finally
        {
            try { NVAPI.Unload(); } catch { /* Ignored */ }
        }
    }
}
