// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) RAMSPDToolkit and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.
// Derived from Lenovo Legion Toolkit.
// Original project copyright: Copyright (C) Bartosz Cichecki and contributors.
// Upstream sync copyright: Copyright (C) 2026 LenovoLegionToolkit-Team.
// Modifications copyright: Copyright (C) 2026 Universal Device Toolkit Contributors.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LibreHardwareMonitor.Hardware;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public class SensorsGroupController : IDisposable
{
    #region Constants (Magic Words & Numbers)

    private const float INVALID_VALUE_FLOAT = -1f;
    private const double INVALID_VALUE_DOUBLE = 0.0;
    private const string UNKNOWN_NAME = "UNKNOWN";

    private const string SENSOR_NAME_TOTAL_MEMORY = "Total Memory";
    private const string SENSOR_NAME_MEMORY_USED = "Memory Used";
    private const string SENSOR_NAME_MEMORY_AVAILABLE = "Memory Available";
    private const string SENSOR_NAME_PACKAGE = "Package";
    private static readonly string[] CPU_TEMPERATURE_SENSOR_PREFERENCES =
    [
        "CPU Package",
        "CPU Package Temperature",
        "Processor Package",
        "Processor Package Temperature",
        "Package",
        "Tctl/Tdie",
        "Tctl",
        "Tdie",
        "CPU Die",
        "CPU CCD",
        "CCD",
        "Tjunction",
        "Core Max",
        "Core Average",
        "Average",
        "CPU",
        "Core",
    ];
    private static readonly string[] CPU_VOLTAGE_SENSOR_PREFERENCES =
    [
        "CPU Core Voltage",
        "CPU VCore",
        "Core Voltage",
        "Vcore",
        "VCore",
        "Core VID",
        "CPU VID",
        "VDDCR CPU",
        "CPU VDD",
        "VDD CPU",
        "VCC Core",
        "IA Voltage",
        "IA VR Voltage",
        "Core VIDs",
        "CPU Core VID",
        "CPU Input Voltage",
        "VCCIN",
        "VDDCR_VDD",
        "SVI2 TFN CPU",
        "SVI3 TFN CPU",
        "SVI2 TFN",
        "SVI3 TFN",
        "VID",
        "Core",
        "CPU",
        "Voltage",
    ];
    private static readonly string[] CPU_USAGE_SENSOR_PREFERENCES =
    [
        "CPU Total",
        "Total",
        "CPU Usage",
        "CPU Utilization",
        "CPU Utility",
        "Package",
    ];
    private static readonly string[] CPU_PACKAGE_POWER_SENSOR_PREFERENCES =
    [
        "Core+SoC Power",
        "Core + SoC Power",
        "Core and SoC Power",
        "CPU Core+SoC",
        "CPU Core + SoC",
        "APU STAPM",
        "STAPM",
        "APU PPT",
        "APU sPPT",
        "APU Package",
        "APU Power",
        "CPU sPPT",
        "CPU Socket Power",
        "Socket Power",
        "CPU Package",
        "CPU Package Power",
        "Package Power",
        "CPU PPT Power",
        "CPU PPT",
        "sPPT",
        "PPT Limit",
        "Processor Package Power",
        "PPT",
        "Processor Power",
        "Processor Power Draw",
        "CPU Total",
        "Total CPU",
        "CPU Power",
        "CPU Power Draw",
        "Package Power Draw",
        "Package",
    ];
    private static readonly string[] CPU_CORE_POWER_SENSOR_PREFERENCES =
    [
        "IA Cores",
        "IA Power",
        "IA Limit",
        "VDDCR CPU Power",
        "CPU VDD Power",
        "VDD CPU Power",
        "CPU Core Power",
        "Core Power Draw",
        "CPU Cores",
        "CPU Core",
        "Core Power",
        "Cores",
    ];
    private static readonly string[] CPU_MEMORY_POWER_SENSOR_PREFERENCES =
    [
        "CPU Memory",
        "Memory Controller",
        "DRAM",
        "MCH",
    ];
    private static readonly string[] CPU_PLATFORM_POWER_SENSOR_PREFERENCES =
    [
        "CPU Platform",
        "CPU Graphics",
        "GT Cores",
        "GT Power",
        "VDDCR SOC Power",
        "VDDCR SoC Power",
        "VDDCR_SOC Power",
        "CPU SoC",
        "SoC",
        "SOC",
        "SoC Power",
        "System Agent",
        "PCH",
        "CPU Uncore",
        "Uncore",
        "Uncore Power",
        "Ring",
        "EDC",
        "TDC",
    ];
    private static readonly string[] CPU_P_CORE_CLOCK_SENSOR_PREFERENCES =
    [
        "CPU P-Core",
        "P-Core",
        "P Core",
        "Performance Core",
        "Performance-Core",
        "CPU Performance",
    ];
    private static readonly string[] CPU_E_CORE_CLOCK_SENSOR_PREFERENCES =
    [
        "CPU E-Core",
        "E-Core",
        "E Core",
        "Efficient Core",
        "Efficiency Core",
        "Efficient-Core",
        "CPU Efficient",
        "CPU Efficiency",
    ];
    private static readonly string[] GPU_VRAM_TEMPERATURE_SENSOR_PREFERENCES =
    [
        "GPU Memory Junction",
        "Memory Junction",
        "VRAM Junction",
        "VRAM Temperature",
        "Memory Temperature",
        "VRAM",
        "Memory",
    ];
    private static readonly string[] GPU_HOTSPOT_TEMPERATURE_SENSOR_PREFERENCES =
    [
        "GPU Hot Spot",
        "Hot Spot",
        "Hotspot",
    ];
    private static readonly string[] MEMORY_TEMPERATURE_SENSOR_PREFERENCES =
    [
        "DIMM Temperature",
        "DIMM Thermal Sensor",
        "DIMM Thermal",
        "Memory Temperature",
        "Memory Module Temperature",
        "Module Temperature",
        "RAM Temperature",
        "DRAM Temperature",
        "DIMM Module",
        "DIMM #",
        "Memory Slot",
        "DDR Module",
        "DDR5 SPD Hub",
        "DDR4 TSOD",
        "SPD Hub Temperature",
        "SPD Hub",
        "TSOD Temperature",
        "PMIC Temperature",
        "Thermal Sensor on DIMM",
        "DIMM",
        "DRAM",
        "DDR",
        "SPD",
        "TSOD",
        "PMIC",
        "Memory",
        "RAM",
    ];
    private static readonly string[] MOTHERBOARD_TEMPERATURE_SENSOR_PREFERENCES =
    [
        "PCH Temperature",
        "PCH",
        "Chipset Temperature",
        "Chipset",
        "Platform Controller Hub Temperature",
        "Platform Controller Hub",
        "Motherboard Temperature",
        "Motherboard",
        "Mainboard Temperature",
        "Mainboard",
        "Board Temperature",
        "Board",
        "VRM MOS Temperature",
        "VRM Temperature",
        "VRM MOS",
        "VRM",
        "MOSFET",
        "MOS Temperature",
        "MOS",
        "Super I/O",
        "Super IO",
        "System Temperature",
        "Sys Temp",
        "System",
        "T_Sensor",
        "TSensor",
        "SYSTIN",
        "AUXTIN",
        "TMPIN",
        "Temp1",
        "Temp 1",
        "Temperature #1",
        "Temp2",
        "Temp 2",
        "Temperature #2",
        "ACPI Thermal Zone",
        "Thermal Zone",
        "TZ00",
        "TZ01",
        "TZS0",
        "TZS1",
        "EC Temp",
        "EC",
        "Embedded Controller",
    ];
    private static readonly string[] BOARD_SENSOR_HARDWARE_NAME_EXCLUSIONS =
    [
        "Battery",
        "Network",
        "Ethernet",
        "Wi-Fi",
        "WiFi",
        "Wireless",
    ];
    private static readonly string[] GPU_VRAM_USED_SENSOR_PREFERENCES =
    [
        "GPU Memory Used",
        "GPU Dedicated Memory Used",
        "Dedicated Memory Used",
        "Dedicated Video Memory Used",
        "D3D Dedicated Memory Used",
        "D3D Shared Memory Used",
        "Shared Memory Used",
        "VRAM Used",
        "Memory Used",
    ];
    private static readonly string[] GPU_VRAM_TOTAL_SENSOR_PREFERENCES =
    [
        "GPU Memory Total",
        "D3D Shared Memory Total",
        "GPU Dedicated Memory Total",
        "Dedicated Memory Total",
        "Dedicated Video Memory Total",
        "Shared Memory Total",
        "VRAM Total",
        "Memory Total",
        "GPU Memory",
    ];
    private static readonly string[] GPU_VRAM_FREE_SENSOR_PREFERENCES =
    [
        "GPU Memory Free",
        "GPU Dedicated Memory Free",
        "Dedicated Memory Free",
        "Dedicated Video Memory Free",
        "D3D Dedicated Memory Free",
        "D3D Shared Memory Free",
        "Shared Memory Free",
        "VRAM Free",
        "Memory Free",
    ];
    private static readonly string[] GPU_PCIE_RX_THROUGHPUT_SENSOR_PREFERENCES =
    [
        "GPU PCIe Rx",
        "GPU PCIe Read",
        "PCIe Read",
        "PCIe Rx",
        "PCIe RX",
        "Bus Read",
        "Bus Rx",
    ];
    private static readonly string[] GPU_PCIE_TX_THROUGHPUT_SENSOR_PREFERENCES =
    [
        "GPU PCIe Tx",
        "GPU PCIe Write",
        "PCIe Write",
        "PCIe Tx",
        "PCIe TX",
        "Bus Write",
        "Bus Tx",
    ];
    private static readonly string[] GPU_POWER_SENSOR_PREFERENCES =
    [
        "GPU Package",
        "GPU PPT",
        "GPU Power",
        "GPU Power Draw",
        "GPU Power Consumption",
        "GPU Instantaneous Power",
        "Board Power Draw",
        "Board Power",
        "GPU Board Power",
        "GPU Total Board Power",
        "Total Board Power",
        "GPU Total Power",
        "Total Graphics Power",
        "Average GPU Power",
        "Current GPU Power",
        "Graphics Power",
        "GPU Graphics Power",
        "GPU Core Power",
        "GPU ASIC Power",
        "ASIC Power",
        "GPU Chip Power",
        "Chip Power",
        "Core Power",
        "Power Consumption",
        "Instantaneous Power",
        "TGP",
        "PPT",
        "Power Draw",
        "Package Power",
        "Power",
    ];
    private static readonly string[] GPU_VOLTAGE_SENSOR_PREFERENCES =
    [
        "GPU Core Voltage",
        "GPU VDDC",
        "GPU VDD",
        "GPU VCore",
        "Core Voltage",
        "VDDC",
        "VDDCI",
        "VDD",
        "MVDD",
        "NVVDD",
        "GPU Core",
        "GPU Voltage",
        "Voltage",
        "Core",
        "GPU",
    ];
    private static readonly string[] GPU_TEMPERATURE_SENSOR_PREFERENCES =
    [
        "GPU Core",
        "Core Temperature",
        "Core",
        "GPU Temperature",
        "GPU",
        "Temperature",
    ];
    private static readonly string[] GPU_CORE_CLOCK_SENSOR_PREFERENCES =
    [
        "GPU Core",
        "Core Clock",
        "Graphics Clock",
        "Graphics",
        "SM Clock",
        "Shader Clock",
        "GPU Clock",
        "Core",
        "Clock",
    ];
    private static readonly string[] GPU_MEMORY_CLOCK_SENSOR_PREFERENCES =
    [
        "GPU Memory",
        "Memory Clock",
        "FB Clock",
        "VRAM Memory Clock",
        "VRAM Clock",
        "VRAM",
        "Memory",
        "Clock",
    ];
    private static readonly string[] MEMORY_USED_SENSOR_PREFERENCES =
    [
        "Memory Used",
        "Used Memory",
    ];
    private static readonly string[] MEMORY_AVAILABLE_SENSOR_PREFERENCES =
    [
        "Memory Available",
        "Available Memory",
        "Memory Free",
        "Free Memory",
    ];
    private static readonly string[] MEMORY_LOAD_SENSOR_PREFERENCES =
    [
        "Memory",
        "Memory Load",
        "System Memory",
    ];
    private static readonly string[] STORAGE_TEMPERATURE_SENSOR_PREFERENCES =
    [
        "NVMe Composite Temperature",
        "Composite Temperature",
        "Drive Composite Temperature",
        "Composite",
        "NVMe Composite",
        "Drive Temperature 1",
        "Drive Temperature 2",
        "Drive Temperature",
        "SSD Temperature",
        "Disk Temperature",
        "HDD Temperature",
        "Controller Temperature",
        "ASIC Controller Temperature",
        "ASIC Controller",
        "ASIC Temperature",
        "NAND Temperature 1",
        "NAND Temperature 2",
        "NAND Temperature",
        "NAND 1",
        "NAND 2",
        "Temperature 1",
        "Temperature 2",
        "Temperature #1",
        "Temperature #2",
        "Temperature",
    ];
    private static readonly string[] GPU_USAGE_SENSOR_PREFERENCES =
    [
        "D3D 3D",
        "GPU Core",
        "Core Utilization",
        "GPU Utilization",
        "Utilization",
        "3D",
    ];

    private const string HARDWARE_ID_NVIDIA_GPU = "NvidiaGPU";

    private const string REGEX_AMD_GPU_INTEGRATED = @"AMD Radeon\(TM\)\s+\d+M";
    private const string REGEX_STRIP_AMD = @"\s+with\s+Radeon\s+Graphics$";
    private const string REGEX_STRIP_INTEL = @"\s*\d+(?:th|st|nd|rd)?\s+Gen\b";
    private const string REGEX_STRIP_NVIDIA = @"(?i)\b(?:Nvidia\s+)?(GeForce\s+(?:RTX|GTX)\s+\d{3,4}(?:\s+(Ti|SUPER|Ti\s+SUPER|M))?)\b(?:\s+Laptop\s+GPU)?(?!\S)";
    private const string REGEX_CLEAN_SPACES = @"\s+";

    private const float MAX_VALID_CPU_POWER = 400f;
    private const float MIN_VALID_POWER_READING = 0f;
    private const int MAX_CPU_POWER_STUCK_RETRIES = 10;
    private const float MB_PER_GB = 1024f;

    #endregion

    private bool _initialized;
    public LibreHardwareMonitorInitialState InitialState { get; private set; }
    public bool IsHybrid { get; private set; }

    private float _lastGpuPower;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);

    private readonly List<IHardware> _hardware = [];

    private Computer? _computer;
    private IHardware? _cpuHardware;
    private IHardware? _amdGpuHardware;
    private IHardware? _gpuHardware;
    private IHardware? _iGpuHardware;
    private IHardware? _memoryHardware;

    private ISensor? _cpuTempSensor;
    private ISensor? _cpuUsageSensor;
    private ISensor? _cpuCoreVoltageSensor;
    private ISensor? _gpuUsageSensor;
    private ISensor? _gpuTempSensor;
    private ISensor? _gpuClockSensor;
    private ISensor? _gpuMemoryClockSensor;
    private ISensor? _gpuCoreVoltageSensor;
    private ISensor? _iGpuCoreVoltageSensor;

    private ISensor? _iGpuUsageSensor;
    private ISensor? _iGpuTempSensor;
    private ISensor? _iGpuClockSensor;
    private ISensor? _iGpuMemoryClockSensor;
    private ISensor? _iGpuPowerSensor;

    private ISensor? _gpuD3DVramUsedSensor;
    private ISensor? _gpuVramTotalSensor;
    private ISensor? _gpuVramFreeSensor;
    private ISensor? _gpuPcieRxSensor;
    private ISensor? _gpuPcieTxSensor;
    private float _cachedGpuVramTotal = INVALID_VALUE_FLOAT;

    private ISensor? _iGpuD3DVramUsedSensor;
    private ISensor? _iGpuVramTotalSensor;
    private ISensor? _iGpuVramFreeSensor;
    private ISensor? _iGpuPcieRxSensor;
    private ISensor? _iGpuPcieTxSensor;
    private float _cachedIGpuVramTotal = INVALID_VALUE_FLOAT;

    private readonly List<ISensor> _pCoreClockSensors = [];
    private readonly List<ISensor> _eCoreClockSensors = [];
    private ISensor? _cpuPackagePowerSensor;
    private readonly List<ISensor> _cpuComponentPowerSensors = [];
    private readonly List<ISensor> _cpuCoreClockSensors = [];

    private ISensor? _gpuPowerSensor;
    private ISensor? _gpuVramTemperatureSensor;
    private ISensor? _gpuHotSpotSensor;

    private ISensor? _memoryLoadSensor;
    private ISensor? _memoryUsedSensor;
    private ISensor? _memoryAvailableSensor;
    private float _cachedMemoryTotal = INVALID_VALUE_FLOAT;
    private readonly List<ISensor> _memoryTempSensors = [];
    private readonly List<ISensor> _motherboardTempSensors = [];
    private readonly List<ISensor> _storageTempSensors = [];

    private volatile bool _isResetting;
    private bool _needRefreshGpuHardware;

    private bool _selectedGpuIsIgpu;
    public bool SelectedGpuIsIgpu
    {
        get => _selectedGpuIsIgpu;
        set
        {
            lock (_dataLock)
            {
                if (_selectedGpuIsIgpu != value)
                {
                    _selectedGpuIsIgpu = value;
                    _cachedGpuName = string.Empty;
                }
            }
        }
    }

    private bool _showAverageCpuFrequency;
    public bool ShowAverageCpuFrequency
    {
        get => _showAverageCpuFrequency;
        set
        {
            lock (_dataLock)
            {
                _showAverageCpuFrequency = value;
            }
        }
    }

    private bool _isDgpuConnected = true;
    public bool IsDgpuConnected
    {
        get => _isDgpuConnected;
        set
        {
            lock (_dataLock)
            {
                if (_isDgpuConnected != value)
                {
                    _isDgpuConnected = value;
                    _cachedGpuName = string.Empty;
                    if (!_isDgpuConnected)
                    {
                        _gpuHardware = null;
                        _amdGpuHardware = null;
                    }
                }
            }
        }
    }

    private string _cachedCpuName = string.Empty;
    private string _cachedGpuName = string.Empty;

    private float _cachedCpuPower;
    private int _cachedCpuPowerTime;

    private readonly Lock _hardwareLock = new();
    private readonly Lock _dataLock = new();
    private volatile bool _hardwareInitialized;

    private readonly Dictionary<object, TimeSpan> _subscribers = [];
    private CancellationTokenSource? _producerCts;
    private Task? _producerTask;
    public event EventHandler? SensorsUpdated;

    private readonly GPUController _gpuController = IoCContainer.Resolve<GPUController>();
    private readonly IDelayProvider _delayProvider;

    public SensorsGroupController(IDelayProvider delayProvider)
    {
        _delayProvider = delayProvider;
    }

    private float _snapshotCpuTemp = INVALID_VALUE_FLOAT;
    private float _snapshotCpuUsage = INVALID_VALUE_FLOAT;
    private float _snapshotCpuPower = INVALID_VALUE_FLOAT;
    private float _snapshotCpuCoresPower = INVALID_VALUE_FLOAT;
    private float _snapshotCpuMemoryPower = INVALID_VALUE_FLOAT;
    private float _snapshotCpuPlatformPower = INVALID_VALUE_FLOAT;
    private float _snapshotCpuVoltage = INVALID_VALUE_FLOAT;
    private float _snapshotCpuMaxClock = INVALID_VALUE_FLOAT;
    private float _snapshotCpuAvgClock = INVALID_VALUE_FLOAT;
    private float _snapshotCpuPClock = INVALID_VALUE_FLOAT;
    private float _snapshotCpuEClock = INVALID_VALUE_FLOAT;
    private float _snapshotCpuPAvgClock = INVALID_VALUE_FLOAT;
    private float _snapshotCpuEAvgClock = INVALID_VALUE_FLOAT;
    private float _snapshotGpuUsage = INVALID_VALUE_FLOAT;
    private float _snapshotGpuTemp = INVALID_VALUE_FLOAT;
    private float _snapshotGpuClock = INVALID_VALUE_FLOAT;
    private float _snapshotGpuMemoryClock = INVALID_VALUE_FLOAT;
    private float _snapshotGpuPower = INVALID_VALUE_FLOAT;
    private float _snapshotGpuVoltage = INVALID_VALUE_FLOAT;
    private float _snapshotGpuVramTemp = INVALID_VALUE_FLOAT;
    private float _snapshotGpuHotSpotTemp = INVALID_VALUE_FLOAT;
    private float _snapshotGpuVramUsage = INVALID_VALUE_FLOAT;
    private float _snapshotGpuVramUtilization = INVALID_VALUE_FLOAT;
    private float _snapshotGpuPcieRxThroughput = INVALID_VALUE_FLOAT;
    private float _snapshotGpuPcieTxThroughput = INVALID_VALUE_FLOAT;
    private float _snapshotMemUsage = INVALID_VALUE_FLOAT;
    private float _snapshotMemUsed = INVALID_VALUE_FLOAT;
    private float _snapshotMemTotal = INVALID_VALUE_FLOAT;
    private double _snapshotMemMaxTemp = INVALID_VALUE_DOUBLE;
    private double _snapshotMotherboardMaxTemp = INVALID_VALUE_DOUBLE;
    private (float, float) _snapshotSsdTemps = (INVALID_VALUE_FLOAT, INVALID_VALUE_FLOAT);

    public async Task<LibreHardwareMonitorInitialState> IsSupportedAsync()
    {
        LibreHardwareMonitorInitialState result = await InitializeAsync().ConfigureAwait(false);
        try
        {
            bool haveHardware;
            lock (_hardwareLock) { haveHardware = _hardware.Count != 0; }
            if (haveHardware && result is LibreHardwareMonitorInitialState.Initialized or LibreHardwareMonitorInitialState.Success) return result;
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Sensor group check failed: {ex}");
            return result;
        }
        return LibreHardwareMonitorInitialState.Fail;
    }

    private void GetHardware()
    {
        lock (_hardwareLock)
        {
            if (_hardwareInitialized) return;

            var pawnIoInstalled = PawnIOHelper.IsPawnIOInstalled();
            if (!pawnIoInstalled && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("PawnIO not detected; attempting LibreHardwareMonitor initialization without it.");

            try
            {
                _computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = false,
                    IsNetworkEnabled = false,
                    IsStorageEnabled = true
                };

                _computer.Open();
                _computer.Accept(new UpdateVisitor());
                _hardware.AddRange(EnumerateHardwareTree(_computer.Hardware));
                RefreshSensorCache();
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"GetHardware failed: {ex}");
                _computer?.Close();
                _computer = null;
                _hardware.Clear();
                throw;
            }
            finally { _hardwareInitialized = true; }
        }
    }

    private void RefreshSensorCache()
    {
        _cpuHardware = null;
        _amdGpuHardware = null;
        _gpuHardware = null;
        _memoryHardware = null;
        _cpuTempSensor = null;
        _cpuUsageSensor = null;
        _cpuCoreVoltageSensor = null;
        _gpuUsageSensor = null;
        _gpuTempSensor = null;
        _gpuClockSensor = null;
        _gpuMemoryClockSensor = null;
        _gpuCoreVoltageSensor = null;
        _iGpuCoreVoltageSensor = null;

        _iGpuUsageSensor = null;
        _iGpuTempSensor = null;
        _iGpuClockSensor = null;
        _iGpuMemoryClockSensor = null;
        _iGpuPowerSensor = null;

        _gpuD3DVramUsedSensor = null;
        _gpuVramTotalSensor = null;
        _gpuVramFreeSensor = null;
        _gpuPcieRxSensor = null;
        _gpuPcieTxSensor = null;
        _cachedGpuVramTotal = INVALID_VALUE_FLOAT;

        _iGpuD3DVramUsedSensor = null;
        _iGpuVramTotalSensor = null;
        _iGpuVramFreeSensor = null;
        _iGpuPcieRxSensor = null;
        _iGpuPcieTxSensor = null;
        _cachedIGpuVramTotal = INVALID_VALUE_FLOAT;

        _pCoreClockSensors.Clear();
        _eCoreClockSensors.Clear();
        _cpuCoreClockSensors.Clear();
        _cpuComponentPowerSensors.Clear();
        _memoryTempSensors.Clear();
        _motherboardTempSensors.Clear();
        _storageTempSensors.Clear();

        _cpuPackagePowerSensor = null;
        _gpuPowerSensor = null;
        _gpuVramTemperatureSensor = null;
        _gpuHotSpotSensor = null;
        _memoryLoadSensor = null;
        _memoryUsedSensor = null;
        _memoryAvailableSensor = null;
        _cachedMemoryTotal = INVALID_VALUE_FLOAT;

        IsHybrid = false;

        _cpuHardware = _hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        _amdGpuHardware = _hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuAmd && !Regex.IsMatch(h.Name, REGEX_AMD_GPU_INTEGRATED, RegexOptions.IgnoreCase));
        _iGpuHardware = _hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuIntel || (h.HardwareType == HardwareType.GpuAmd && Regex.IsMatch(h.Name, REGEX_AMD_GPU_INTEGRATED, RegexOptions.IgnoreCase)));
        _gpuHardware = _hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
        _memoryHardware = SelectMemoryHardware(_hardware);

        if (_cpuHardware?.Sensors != null)
        {
            foreach (var s in _cpuHardware.Sensors)
            {
                switch (s.SensorType)
                {
                    case SensorType.Temperature when s.Name.Contains(SENSOR_NAME_PACKAGE):
                        _cpuTempSensor = s;
                        break;
                    case SensorType.Load when IsLikelyCpuUsageSensorName(s.Name):
                        _cpuUsageSensor = s;
                        break;
                    case SensorType.Voltage when IsLikelyCpuVoltageSensorName(s.Name):
                        _cpuCoreVoltageSensor ??= s;
                        break;
                    case SensorType.Clock when IsLikelyCpuPCoreClockSensorName(s.Name):
                        _pCoreClockSensors.Add(s);
                        _cpuCoreClockSensors.Add(s);
                        break;
                    case SensorType.Clock when IsLikelyCpuECoreClockSensorName(s.Name):
                        _eCoreClockSensors.Add(s);
                        _cpuCoreClockSensors.Add(s);
                        break;
                    case SensorType.Clock when s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) && !s.Name.Contains("Average") && !s.Name.Contains("Effective"):
                        _cpuCoreClockSensors.Add(s);
                        break;
                    case SensorType.Power when IsLikelyCpuPackagePowerSensorName(s.Name):
                        _cpuPackagePowerSensor ??= s;
                        break;
                    case SensorType.Power when IsLikelyCpuComponentPowerSensorName(s.Name):
                        _cpuComponentPowerSensors.Add(s);
                        break;
                }
            }
            IsHybrid = _pCoreClockSensors.Count > 0 || _eCoreClockSensors.Count > 0;
            _cpuTempSensor ??= SelectCpuTemperatureSensor(_cpuHardware.Sensors);
            _cpuUsageSensor ??= SelectCpuUsageSensor(_cpuHardware.Sensors);
            _cpuCoreVoltageSensor ??= SelectCpuVoltageSensor(_cpuHardware.Sensors);
            _cpuPackagePowerSensor ??= SelectCpuPackagePowerSensor(_cpuHardware.Sensors);
        }

        var mainGpu = _gpuHardware ?? _amdGpuHardware;
        if (mainGpu?.Sensors != null)
        {
            foreach (var s in mainGpu.Sensors)
            {
                switch (s.SensorType)
                {
                    case SensorType.Load when IsLikelyGpuUsageSensorName(s.Name):
                        _gpuUsageSensor = s;
                        break;
                    case SensorType.Temperature when IsLikelyGpuTemperatureSensorName(s.Name):
                        _gpuTempSensor = s;
                        break;
                    case SensorType.Clock when IsLikelyGpuCoreClockSensorName(s.Name):
                        _gpuClockSensor = s;
                        break;
                    case SensorType.Clock when IsLikelyGpuMemoryClockSensorName(s.Name):
                        _gpuMemoryClockSensor ??= s;
                        break;
                    case SensorType.Power when IsLikelyGpuPowerSensorName(s.Name):
                        _gpuPowerSensor ??= s;
                        break;
                    case SensorType.Voltage when IsLikelyGpuVoltageSensorName(s.Name):
                        _gpuCoreVoltageSensor ??= s;
                        break;
                    case SensorType.Temperature when IsLikelyGpuHotSpotTemperatureSensorName(s.Name):
                        _gpuHotSpotSensor ??= s;
                        break;
                    case SensorType.Temperature when IsLikelyGpuVramTemperatureSensorName(s.Name):
                        _gpuVramTemperatureSensor ??= s;
                        break;
                    case SensorType.SmallData or SensorType.Data when IsLikelyGpuVramUsedSensorName(s.Name):
                        _gpuD3DVramUsedSensor = s;
                        break;
                    case SensorType.SmallData or SensorType.Data when IsLikelyGpuVramTotalSensorName(s.Name):
                        _gpuVramTotalSensor = s;
                        break;
                    case SensorType.SmallData or SensorType.Data when IsLikelyGpuVramFreeSensorName(s.Name):
                        _gpuVramFreeSensor = s;
                        break;
                    case SensorType.Throughput when IsLikelyGpuPcieRxThroughputSensorName(s.Name):
                        _gpuPcieRxSensor ??= s;
                        break;
                    case SensorType.Throughput when IsLikelyGpuPcieTxThroughputSensorName(s.Name):
                        _gpuPcieTxSensor ??= s;
                        break;
                }
            }
            _gpuUsageSensor ??= SelectGpuUsageSensor(mainGpu.Sensors);
            _gpuTempSensor ??= SelectGpuTemperatureSensor(mainGpu.Sensors);
            _gpuClockSensor ??= SelectGpuCoreClockSensor(mainGpu.Sensors);
            _gpuMemoryClockSensor ??= SelectGpuMemoryClockSensor(mainGpu.Sensors);
            _gpuPowerSensor ??= SelectGpuPowerSensor(mainGpu.Sensors);
            _gpuCoreVoltageSensor ??= SelectGpuVoltageSensor(mainGpu.Sensors);
            _gpuHotSpotSensor ??= SelectGpuHotSpotTemperatureSensor(mainGpu.Sensors);
            _gpuVramTemperatureSensor ??= SelectGpuVramTemperatureSensor(mainGpu.Sensors);
            _gpuD3DVramUsedSensor ??= SelectGpuVramUsedSensor(mainGpu.Sensors);
            _gpuVramTotalSensor ??= SelectGpuVramTotalSensor(mainGpu.Sensors);
            _gpuVramFreeSensor ??= SelectGpuVramFreeSensor(mainGpu.Sensors);
            _gpuPcieRxSensor ??= SelectGpuPcieRxThroughputSensor(mainGpu.Sensors);
            _gpuPcieTxSensor ??= SelectGpuPcieTxThroughputSensor(mainGpu.Sensors);
        }

        if (_iGpuHardware?.Sensors != null)
        {
            foreach (var s in _iGpuHardware.Sensors)
            {
                switch (s.SensorType)
                {
                    case SensorType.Load when IsLikelyGpuUsageSensorName(s.Name):
                        _iGpuUsageSensor = s;
                        break;
                    case SensorType.Temperature when IsLikelyGpuTemperatureSensorName(s.Name):
                        _iGpuTempSensor = s;
                        break;
                    case SensorType.Clock when IsLikelyGpuCoreClockSensorName(s.Name):
                        _iGpuClockSensor = s;
                        break;
                    case SensorType.Clock when IsLikelyGpuMemoryClockSensorName(s.Name):
                        _iGpuMemoryClockSensor ??= s;
                        break;
                    case SensorType.Power when IsLikelyGpuPowerSensorName(s.Name):
                        _iGpuPowerSensor ??= s;
                        break;
                    case SensorType.Voltage when IsLikelyGpuVoltageSensorName(s.Name):
                        _iGpuCoreVoltageSensor ??= s;
                        break;
                    case SensorType.SmallData or SensorType.Data when IsLikelyGpuVramUsedSensorName(s.Name):
                        _iGpuD3DVramUsedSensor = s;
                        break;
                    case SensorType.SmallData or SensorType.Data when IsLikelyGpuVramTotalSensorName(s.Name):
                        _iGpuVramTotalSensor = s;
                        break;
                    case SensorType.SmallData or SensorType.Data when IsLikelyGpuVramFreeSensorName(s.Name):
                        _iGpuVramFreeSensor = s;
                        break;
                    case SensorType.Throughput when IsLikelyGpuPcieRxThroughputSensorName(s.Name):
                        _iGpuPcieRxSensor ??= s;
                        break;
                    case SensorType.Throughput when IsLikelyGpuPcieTxThroughputSensorName(s.Name):
                        _iGpuPcieTxSensor ??= s;
                        break;
                }
            }
            _iGpuUsageSensor ??= SelectGpuUsageSensor(_iGpuHardware.Sensors);
            _iGpuTempSensor ??= SelectGpuTemperatureSensor(_iGpuHardware.Sensors);
            _iGpuClockSensor ??= SelectGpuCoreClockSensor(_iGpuHardware.Sensors);
            _iGpuMemoryClockSensor ??= SelectGpuMemoryClockSensor(_iGpuHardware.Sensors);
            _iGpuPowerSensor ??= SelectGpuPowerSensor(_iGpuHardware.Sensors);
            _iGpuCoreVoltageSensor ??= SelectGpuVoltageSensor(_iGpuHardware.Sensors);
            _iGpuD3DVramUsedSensor ??= SelectGpuVramUsedSensor(_iGpuHardware.Sensors);
            _iGpuVramTotalSensor ??= SelectGpuVramTotalSensor(_iGpuHardware.Sensors);
            _iGpuVramFreeSensor ??= SelectGpuVramFreeSensor(_iGpuHardware.Sensors);
            _iGpuPcieRxSensor ??= SelectGpuPcieRxThroughputSensor(_iGpuHardware.Sensors);
            _iGpuPcieTxSensor ??= SelectGpuPcieTxThroughputSensor(_iGpuHardware.Sensors);
        }

        _memoryLoadSensor = _memoryHardware?.Sensors is null ? null : SelectMemoryLoadSensor(_memoryHardware.Sensors);
        _memoryUsedSensor = _memoryHardware?.Sensors is null ? null : SelectMemoryUsedSensor(_memoryHardware.Sensors);
        _memoryAvailableSensor = _memoryHardware?.Sensors is null ? null : SelectMemoryAvailableSensor(_memoryHardware.Sensors);

        foreach (var hw in _hardware.Where(h => h.HardwareType == HardwareType.Memory))
        {
            if (hw.Sensors == null) continue;
            _memoryTempSensors.AddRange(SelectMemoryTemperatureSensors(hw.Sensors, requireMemoryKeywords: false));
        }

        foreach (var hw in _hardware.Where(IsBoardTemperatureHardware))
        {
            if (hw.Sensors == null) continue;
            _memoryTempSensors.AddRange(SelectMemoryTemperatureSensors(hw.Sensors, requireMemoryKeywords: true));
            _motherboardTempSensors.AddRange(SelectMotherboardTemperatureSensors(hw.Sensors));
        }

        foreach (var storage in _hardware.Where(h => h.HardwareType == HardwareType.Storage))
        {
            var temp = storage.Sensors is null ? null : SelectStorageTemperatureSensor(storage.Sensors);
            if (temp != null) _storageTempSensors.Add(temp);
        }

        if (Log.Instance.IsTraceEnabled)
        {
            var hardwareSummary = string.Join(", ", _hardware.Select(h => $"{h.HardwareType}:{h.Name}"));
            Log.Instance.Trace($"LibreHardwareMonitor hardware summary: [{hardwareSummary}]");
            Log.Instance.Trace($"LibreHardwareMonitor CPU temperature sensor: {(_cpuTempSensor is null ? "not found" : _cpuTempSensor.Name)}");
            Log.Instance.Trace($"LibreHardwareMonitor CPU package power sensor: {(_cpuPackagePowerSensor is null ? "not found" : _cpuPackagePowerSensor.Name)}");
        }
    }

    internal static IEnumerable<IHardware> EnumerateHardwareTree(IEnumerable<IHardware> hardware)
    {
        foreach (var item in hardware)
        {
            if (item is null)
                continue;

            yield return item;

            foreach (var child in EnumerateHardwareTree(item.SubHardware ?? []))
                yield return child;
        }
    }

    private static ISensor? SelectCpuTemperatureSensor(IEnumerable<ISensor> sensors)
    {
        var temperatureSensors = sensors
            .Where(s => s.SensorType == SensorType.Temperature)
            .ToArray();

        if (temperatureSensors.Length == 0)
            return null;

        var preferredName = SelectCpuTemperatureSensorName(temperatureSensors.Select(sensor => sensor.Name));
        if (preferredName is not null)
            return temperatureSensors.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));

        return temperatureSensors
            .OrderByDescending(sensor => sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(sensor => sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
            .ThenBy(sensor => sensor.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static IHardware? SelectMemoryHardware(IEnumerable<IHardware> hardware)
    {
        var memoryHardware = hardware
            .Where(h => h.HardwareType == HardwareType.Memory)
            .ToArray();

        if (memoryHardware.Length == 0)
            return null;

        var preferredName = SelectMemoryHardwareName(memoryHardware.Select(h => h.Name));
        if (preferredName is not null)
            return memoryHardware.FirstOrDefault(h => string.Equals(h.Name, preferredName, StringComparison.OrdinalIgnoreCase));

        return memoryHardware
            .FirstOrDefault();
    }

    internal static string? SelectMemoryHardwareName(IEnumerable<string> hardwareNames)
    {
        var names = hardwareNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
            return null;

        return names
            .OrderByDescending(name => string.Equals(name, SENSOR_NAME_TOTAL_MEMORY, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(name => name.Contains("Memory", StringComparison.OrdinalIgnoreCase))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    internal static string? SelectCpuTemperatureSensorName(IEnumerable<string> sensorNames)
    {
        var names = sensorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
            return null;

        foreach (var preferredName in CPU_TEMPERATURE_SENSOR_PREFERENCES)
        {
            var preferred = names.FirstOrDefault(name =>
                name.Contains(preferredName, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
                return preferred;
        }

        return names
            .OrderByDescending(name => name.Contains("CPU", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(name => name.Contains("Core", StringComparison.OrdinalIgnoreCase))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static ISensor? SelectCpuUsageSensor(IEnumerable<ISensor> sensors)
    {
        var loadSensors = sensors
            .Where(sensor => sensor.SensorType == SensorType.Load)
            .ToArray();

        if (loadSensors.Length == 0)
            return null;

        var preferredName = SelectCpuUsageSensorName(loadSensors.Select(sensor => sensor.Name));
        if (preferredName is not null)
            return loadSensors.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));

        return loadSensors.FirstOrDefault();
    }

    internal static string? SelectCpuUsageSensorName(IEnumerable<string> sensorNames)
    {
        var names = sensorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
            return null;

        foreach (var preferredName in CPU_USAGE_SENSOR_PREFERENCES)
        {
            var preferred = names.FirstOrDefault(name =>
                name.Contains(preferredName, StringComparison.OrdinalIgnoreCase)
                && !name.Contains("Core Max", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("Thread", StringComparison.OrdinalIgnoreCase)
                && !name.Contains('#'));
            if (preferred is not null)
                return preferred;
        }

        return names
            .Where(name =>
                !name.Contains("Core Max", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("Thread", StringComparison.OrdinalIgnoreCase)
                && !name.Contains('#'))
            .OrderByDescending(name => name.Contains("CPU", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(name => name.Contains("Total", StringComparison.OrdinalIgnoreCase))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static ISensor? SelectCpuVoltageSensor(IEnumerable<ISensor> sensors)
    {
        var voltageSensors = sensors
            .Where(sensor => sensor.SensorType == SensorType.Voltage)
            .ToArray();

        if (voltageSensors.Length == 0)
            return null;

        var preferredName = SelectCpuVoltageSensorName(voltageSensors.Select(sensor => sensor.Name));
        if (preferredName is not null)
            return voltageSensors.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));

        return voltageSensors.FirstOrDefault();
    }

    internal static string? SelectCpuVoltageSensorName(IEnumerable<string> sensorNames)
    {
        var names = sensorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
            return null;

        foreach (var preferredName in CPU_VOLTAGE_SENSOR_PREFERENCES)
        {
            var preferred = names.FirstOrDefault(name =>
                name.Contains(preferredName, StringComparison.OrdinalIgnoreCase)
                && !name.Contains("System Agent", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("SA", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("Cache", StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
                return preferred;
        }

        return names
            .Where(name =>
                !name.Contains("System Agent", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("SA", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("Cache", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(name => name.Contains("Core", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(name => name.Contains("CPU", StringComparison.OrdinalIgnoreCase))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static ISensor? SelectCpuPackagePowerSensor(IEnumerable<ISensor> sensors)
    {
        var powerSensors = sensors
            .Where(sensor => sensor.SensorType == SensorType.Power)
            .ToArray();

        if (powerSensors.Length == 0)
            return null;

        var preferredName = SelectCpuPackagePowerSensorName(powerSensors.Select(sensor => sensor.Name));
        if (preferredName is not null)
            return powerSensors.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));

        return null;
    }

    internal static string? SelectCpuPackagePowerSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(
            sensorNames.Where(IsLikelyCpuPackagePowerCandidateName),
            CPU_PACKAGE_POWER_SENSOR_PREFERENCES);

    private static ISensor? SelectGpuVramTemperatureSensor(IEnumerable<ISensor> sensors)
    {
        var temperatureSensors = sensors
            .Where(sensor => sensor.SensorType == SensorType.Temperature)
            .ToArray();

        if (temperatureSensors.Length == 0)
            return null;

        var preferredName = SelectGpuVramTemperatureSensorName(temperatureSensors.Select(sensor => sensor.Name));
        if (preferredName is null)
            return null;

        return temperatureSensors.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    internal static string? SelectGpuVramTemperatureSensorName(IEnumerable<string> sensorNames)
    {
        var names = sensorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
            return null;

        foreach (var preferredName in GPU_VRAM_TEMPERATURE_SENSOR_PREFERENCES)
        {
            var preferred = names.FirstOrDefault(name =>
                name.Contains(preferredName, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
                return preferred;
        }

        return null;
    }

    private static ISensor? SelectGpuHotSpotTemperatureSensor(IEnumerable<ISensor> sensors)
    {
        var temperatureSensors = sensors
            .Where(sensor => sensor.SensorType == SensorType.Temperature)
            .ToArray();

        if (temperatureSensors.Length == 0)
            return null;

        var preferredName = SelectGpuHotSpotTemperatureSensorName(temperatureSensors.Select(sensor => sensor.Name));
        if (preferredName is null)
            return null;

        return temperatureSensors.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    internal static string? SelectGpuHotSpotTemperatureSensorName(IEnumerable<string> sensorNames)
    {
        var names = sensorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
            return null;

        foreach (var preferredName in GPU_HOTSPOT_TEMPERATURE_SENSOR_PREFERENCES)
        {
            var preferred = names.FirstOrDefault(name =>
                name.Contains(preferredName, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
                return preferred;
        }

        return null;
    }

    private static bool IsLikelyGpuVramTemperatureSensorName(string sensorName) =>
        SelectGpuVramTemperatureSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuHotSpotTemperatureSensorName(string sensorName) =>
        SelectGpuHotSpotTemperatureSensorName([sensorName]) is not null;

    private static IEnumerable<ISensor> SelectMemoryTemperatureSensors(IEnumerable<ISensor> sensors, bool requireMemoryKeywords)
    {
        var temperatureSensors = sensors
            .Where(sensor => sensor.SensorType == SensorType.Temperature)
            .Where(sensor => !requireMemoryKeywords || IsLikelyMemoryTemperatureSensorName(sensor.Name))
            .OrderByDescending(sensor => MEMORY_TEMPERATURE_SENSOR_PREFERENCES.Any(keyword =>
                sensor.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(sensor => sensor.Name.Contains("DIMM", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(sensor => sensor.Name.Contains("DRAM", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(sensor => sensor.Name.Contains("SPD", StringComparison.OrdinalIgnoreCase))
            .ThenBy(sensor => sensor.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return temperatureSensors;
    }

    internal static bool IsLikelyMemoryTemperatureSensorName(string sensorName) =>
        MEMORY_TEMPERATURE_SENSOR_PREFERENCES.Any(keyword =>
            sensorName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    internal static bool IsBoardTemperatureHardware(IHardware hardware)
    {
        if (hardware.HardwareType == HardwareType.Motherboard)
            return true;

        if (IsDedicatedMetricHardwareType(hardware.HardwareType.ToString()))
            return false;

        if (BOARD_SENSOR_HARDWARE_NAME_EXCLUSIONS.Any(keyword =>
            hardware.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            return false;

        var sensors = hardware.Sensors;
        return sensors is not null && sensors.Any(sensor =>
            sensor.SensorType == SensorType.Temperature &&
            (IsLikelyMemoryTemperatureSensorName(sensor.Name) ||
             SelectMotherboardTemperatureSensorName([sensor.Name]) is not null));
    }

    private static bool IsDedicatedMetricHardwareType(string hardwareTypeName) =>
        hardwareTypeName.Contains("Cpu", StringComparison.OrdinalIgnoreCase) ||
        hardwareTypeName.Contains("Gpu", StringComparison.OrdinalIgnoreCase) ||
        hardwareTypeName.Contains("Memory", StringComparison.OrdinalIgnoreCase) ||
        hardwareTypeName.Contains("Storage", StringComparison.OrdinalIgnoreCase) ||
        hardwareTypeName.Contains("Network", StringComparison.OrdinalIgnoreCase) ||
        hardwareTypeName.Contains("Battery", StringComparison.OrdinalIgnoreCase) ||
        hardwareTypeName.Contains("Psu", StringComparison.OrdinalIgnoreCase) ||
        hardwareTypeName.Contains("Cooler", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<ISensor> SelectMotherboardTemperatureSensors(IEnumerable<ISensor> sensors)
    {
        var temperatureSensors = sensors
            .Where(sensor => sensor.SensorType == SensorType.Temperature)
            .ToArray();

        if (temperatureSensors.Length == 0)
            return [];

        var preferredName = SelectMotherboardTemperatureSensorName(temperatureSensors.Select(sensor => sensor.Name));
        if (preferredName is not null)
            return temperatureSensors
                .Where(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        return temperatureSensors
            .Where(sensor => !IsLikelyMemoryTemperatureSensorName(sensor.Name))
            .Where(sensor => !sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase))
            .Where(sensor => !sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase))
            .OrderBy(sensor => sensor.Name, StringComparer.OrdinalIgnoreCase)
            .Take(1)
            .ToArray();
    }

    internal static string? SelectMotherboardTemperatureSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(
            sensorNames.Where(name =>
                !string.IsNullOrWhiteSpace(name) &&
                !IsLikelyMemoryTemperatureSensorName(name) &&
                !name.Contains("CPU", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("GPU", StringComparison.OrdinalIgnoreCase)),
            MOTHERBOARD_TEMPERATURE_SENSOR_PREFERENCES);

    private static ISensor? SelectGpuVramUsedSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType is SensorType.SmallData or SensorType.Data)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuVramUsedSensorName(candidates.Select(sensor => sensor.Name));
        if (preferredName is null)
            return null;

        return candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectGpuVramTotalSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType is SensorType.SmallData or SensorType.Data)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuVramTotalSensorName(candidates.Select(sensor => sensor.Name));
        if (preferredName is null)
            return null;

        return candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectGpuVramFreeSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType is SensorType.SmallData or SensorType.Data)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuVramFreeSensorName(candidates.Select(sensor => sensor.Name));
        if (preferredName is null)
            return null;

        return candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    internal static string? SelectGpuVramUsedSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_VRAM_USED_SENSOR_PREFERENCES);

    internal static string? SelectGpuVramTotalSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_VRAM_TOTAL_SENSOR_PREFERENCES);

    internal static string? SelectGpuVramFreeSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_VRAM_FREE_SENSOR_PREFERENCES);

    private static bool IsLikelyGpuVramUsedSensorName(string sensorName) =>
        SelectGpuVramUsedSensorName([sensorName]) is not null;

    private static bool IsLikelyCpuVoltageSensorName(string sensorName) =>
        SelectCpuVoltageSensorName([sensorName]) is not null;

    private static bool IsLikelyCpuUsageSensorName(string sensorName) =>
        SelectCpuUsageSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuVramTotalSensorName(string sensorName) =>
        SelectGpuVramTotalSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuVramFreeSensorName(string sensorName) =>
        SelectGpuVramFreeSensorName([sensorName]) is not null;

    private static ISensor? SelectGpuPcieRxThroughputSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Throughput)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuPcieRxThroughputSensorName(candidates.Select(sensor => sensor.Name));
        if (preferredName is null)
            return null;

        return candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectGpuPcieTxThroughputSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Throughput)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuPcieTxThroughputSensorName(candidates.Select(sensor => sensor.Name));
        if (preferredName is null)
            return null;

        return candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    internal static string? SelectGpuPcieRxThroughputSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_PCIE_RX_THROUGHPUT_SENSOR_PREFERENCES);

    internal static string? SelectGpuPcieTxThroughputSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_PCIE_TX_THROUGHPUT_SENSOR_PREFERENCES);

    private static ISensor? SelectGpuUsageSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Load)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuUsageSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectMemoryUsedSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Data)
            .Where(sensor => IsLikelySystemMemorySensorName(sensor.Name))
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectMemoryUsedSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault(sensor => sensor.Name.Contains(SENSOR_NAME_MEMORY_USED, StringComparison.OrdinalIgnoreCase))
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectMemoryAvailableSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Data)
            .Where(sensor => IsLikelySystemMemorySensorName(sensor.Name))
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectMemoryAvailableSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault(sensor => sensor.Name.Contains(SENSOR_NAME_MEMORY_AVAILABLE, StringComparison.OrdinalIgnoreCase))
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectMemoryLoadSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Load)
            .Where(sensor => IsLikelySystemMemorySensorName(sensor.Name))
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectMemoryLoadSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectStorageTemperatureSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Temperature)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectStorageTemperatureSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectGpuPowerSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Power)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuPowerSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectGpuTemperatureSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Temperature)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuTemperatureSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectGpuCoreClockSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Clock)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuCoreClockSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectGpuMemoryClockSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Clock)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuMemoryClockSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    private static ISensor? SelectGpuVoltageSensor(IEnumerable<ISensor> sensors)
    {
        var candidates = sensors
            .Where(sensor => sensor.SensorType == SensorType.Voltage)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var preferredName = SelectGpuVoltageSensorName(candidates.Select(sensor => sensor.Name));
        return preferredName is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(sensor => string.Equals(sensor.Name, preferredName, StringComparison.OrdinalIgnoreCase));
    }

    internal static string? SelectGpuPowerSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_POWER_SENSOR_PREFERENCES);

    internal static string? SelectGpuTemperatureSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_TEMPERATURE_SENSOR_PREFERENCES);

    internal static string? SelectGpuCoreClockSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_CORE_CLOCK_SENSOR_PREFERENCES);

    internal static string? SelectGpuMemoryClockSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_MEMORY_CLOCK_SENSOR_PREFERENCES);

    internal static string? SelectGpuVoltageSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_VOLTAGE_SENSOR_PREFERENCES);

    internal static string? SelectMemoryUsedSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames.Where(IsLikelySystemMemorySensorName), MEMORY_USED_SENSOR_PREFERENCES);

    internal static string? SelectMemoryAvailableSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames.Where(IsLikelySystemMemorySensorName), MEMORY_AVAILABLE_SENSOR_PREFERENCES);

    internal static string? SelectMemoryLoadSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames.Where(IsLikelySystemMemorySensorName), MEMORY_LOAD_SENSOR_PREFERENCES);

    internal static string? SelectStorageTemperatureSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, STORAGE_TEMPERATURE_SENSOR_PREFERENCES);

    private static bool IsLikelySystemMemorySensorName(string sensorName) =>
        !sensorName.Contains("GPU", StringComparison.OrdinalIgnoreCase)
        && !sensorName.Contains("VRAM", StringComparison.OrdinalIgnoreCase)
        && !sensorName.Contains("D3D", StringComparison.OrdinalIgnoreCase)
        && !sensorName.Contains("Shared", StringComparison.OrdinalIgnoreCase);

    internal static string? SelectGpuUsageSensorName(IEnumerable<string> sensorNames) =>
        SelectPreferredSensorName(sensorNames, GPU_USAGE_SENSOR_PREFERENCES)
        ?? sensorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(name => name.Trim().StartsWith("D3D", StringComparison.OrdinalIgnoreCase));

    private static bool IsLikelyGpuPcieRxThroughputSensorName(string sensorName) =>
        SelectGpuPcieRxThroughputSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuPcieTxThroughputSensorName(string sensorName) =>
        SelectGpuPcieTxThroughputSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuUsageSensorName(string sensorName) =>
        SelectGpuUsageSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuPowerSensorName(string sensorName) =>
        SelectGpuPowerSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuTemperatureSensorName(string sensorName) =>
        SelectGpuTemperatureSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuCoreClockSensorName(string sensorName) =>
        SelectGpuCoreClockSensorName([sensorName]) is not null;

    internal static bool IsLikelyCpuPCoreClockSensorName(string sensorName) =>
        IsLikelyCpuCoreClockSensorName(sensorName, CPU_P_CORE_CLOCK_SENSOR_PREFERENCES);

    internal static bool IsLikelyCpuECoreClockSensorName(string sensorName) =>
        IsLikelyCpuCoreClockSensorName(sensorName, CPU_E_CORE_CLOCK_SENSOR_PREFERENCES);

    private static bool IsLikelyCpuCoreClockSensorName(string? sensorName, IEnumerable<string> preferences)
    {
        if (string.IsNullOrWhiteSpace(sensorName))
            return false;

        if (sensorName.Contains("Average", StringComparison.OrdinalIgnoreCase) ||
            sensorName.Contains("Effective", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return SelectPreferredSensorName([sensorName], preferences) is not null;
    }

    private static bool IsLikelyGpuMemoryClockSensorName(string sensorName) =>
        SelectGpuMemoryClockSensorName([sensorName]) is not null;

    private static bool IsLikelyGpuVoltageSensorName(string sensorName) =>
        SelectGpuVoltageSensorName([sensorName]) is not null;

    internal static bool IsLikelyCpuPackagePowerSensorName(string sensorName) =>
        SelectCpuPackagePowerSensorName([sensorName]) is not null;

    private static bool IsLikelyCpuPackagePowerCandidateName(string? sensorName)
    {
        if (string.IsNullOrWhiteSpace(sensorName))
            return false;

        if (sensorName.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
            sensorName.Contains("Memory", StringComparison.OrdinalIgnoreCase) ||
            sensorName.Contains("DRAM", StringComparison.OrdinalIgnoreCase) ||
            sensorName.Contains("Platform", StringComparison.OrdinalIgnoreCase) ||
            sensorName.Contains("Uncore", StringComparison.OrdinalIgnoreCase) ||
            sensorName.Contains("Ring", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hasCore = sensorName.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                      sensorName.Contains("Cores", StringComparison.OrdinalIgnoreCase);
        var hasSoc = Regex.IsMatch(sensorName, @"\bSoC\b", RegexOptions.IgnoreCase);

        if (hasCore && hasSoc)
            return true;

        if (hasCore)
        {
            return sensorName.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                   sensorName.Contains("PPT", StringComparison.OrdinalIgnoreCase) ||
                   sensorName.Contains("STAPM", StringComparison.OrdinalIgnoreCase) ||
                   sensorName.Contains("APU", StringComparison.OrdinalIgnoreCase) ||
                   sensorName.Contains("Socket", StringComparison.OrdinalIgnoreCase);
        }

        return !hasSoc;
    }

    internal static bool IsLikelyCpuComponentPowerSensorName(string sensorName) =>
        IsLikelyCpuCorePowerSensorName(sensorName)
        || IsLikelyCpuMemoryPowerSensorName(sensorName)
        || IsLikelyCpuPlatformPowerSensorName(sensorName);

    internal static (float cores, float memory, float platform) ResolveCpuComponentPowers(IEnumerable<(string name, float value)> components)
    {
        float cores = INVALID_VALUE_FLOAT;
        float memory = INVALID_VALUE_FLOAT;
        float platform = INVALID_VALUE_FLOAT;

        foreach (var (name, value) in components)
        {
            if (value <= MIN_VALID_POWER_READING)
                continue;

            if (IsLikelyCpuCorePowerSensorName(name))
                cores = AddComponentPower(cores, value);
            else if (IsLikelyCpuMemoryPowerSensorName(name))
                memory = AddComponentPower(memory, value);
            else if (IsLikelyCpuPlatformPowerSensorName(name))
                platform = AddComponentPower(platform, value);
        }

        return (cores, memory, platform);
    }

    private static float AddComponentPower(float current, float value) =>
        current <= MIN_VALID_POWER_READING ? value : current + value;

    private static bool IsLikelyCpuCorePowerSensorName(string sensorName) =>
        !sensorName.Contains("GT", StringComparison.OrdinalIgnoreCase) &&
        !sensorName.Contains("Graphics", StringComparison.OrdinalIgnoreCase) &&
        !sensorName.Contains("Uncore", StringComparison.OrdinalIgnoreCase) &&
        !sensorName.Contains("Ring", StringComparison.OrdinalIgnoreCase) &&
        CPU_CORE_POWER_SENSOR_PREFERENCES.Any(keyword =>
            sensorName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static bool IsLikelyCpuMemoryPowerSensorName(string sensorName) =>
        CPU_MEMORY_POWER_SENSOR_PREFERENCES.Any(keyword =>
            sensorName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static bool IsLikelyCpuPlatformPowerSensorName(string sensorName) =>
        CPU_PLATFORM_POWER_SENSOR_PREFERENCES.Any(keyword =>
            sensorName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    internal static float ResolveCpuPower(float packagePower, IEnumerable<float> componentPowers)
    {
        if (packagePower > MIN_VALID_POWER_READING)
            return packagePower;

        var validComponentPowers = componentPowers
            .Where(value => value > MIN_VALID_POWER_READING)
            .ToArray();

        if (validComponentPowers.Length == 0)
            return INVALID_VALUE_FLOAT;

        var total = validComponentPowers.Sum();
        return total > MIN_VALID_POWER_READING ? total : INVALID_VALUE_FLOAT;
    }

    internal static float ResolveGpuPower(float currentPower, float previousPower)
    {
        if (currentPower > MIN_VALID_POWER_READING)
            return currentPower;

        return previousPower > MIN_VALID_POWER_READING
            ? previousPower
            : INVALID_VALUE_FLOAT;
    }

    internal static (float used, float total, float utilization) ResolveGpuVramMetrics(float used, float total, float free)
    {
        if (total <= 0 && used >= 0 && free >= 0)
            total = used + free;

        if (used < 0 && total > 0 && free >= 0)
            used = Math.Max(0, total - free);

        var utilization = used >= 0 && total > 0
            ? (used / total) * 100f
            : INVALID_VALUE_FLOAT;

        return (used, total, utilization);
    }

    private bool ShouldUseIntegratedGpuSnapshot(IHardware? dGpu) =>
        SelectedGpuIsIgpu || dGpu == null || !_isDgpuConnected;

    private static string? SelectPreferredSensorName(IEnumerable<string> sensorNames, IEnumerable<string> preferredNames)
    {
        var names = sensorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
            return null;

        foreach (var preferredName in preferredNames)
        {
            var preferred = names.FirstOrDefault(name =>
                name.Contains(preferredName, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
                return preferred;
        }

        return null;
    }

    public Task<float> GetCpuTemperatureAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotCpuTemp);
    }

    public Task<float> GetCpuUsageAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotCpuUsage);
    }

    public Task<float> GetGpuUsageAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuUsage);
    }

    public Task<float> GetGpuTemperatureAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuTemp);
    }

    public Task<float> GetGpuCoreClockAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuClock);
    }

    public Task<float> GetGpuMemoryClockAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuMemoryClock);
    }

    public Task<string> GetCpuNameAsync()
    {
        lock (_dataLock)
        {
            if (_isResetting || !IsLibreHardwareMonitorInitialized() || _cpuHardware == null)
                return Task.FromResult(UNKNOWN_NAME);

            if (!string.IsNullOrEmpty(_cachedCpuName))
                return Task.FromResult(_cachedCpuName);

            _cachedCpuName = StripName(_cpuHardware.Name);
            return Task.FromResult(_cachedCpuName);
        }
    }

    public Task<string> GetGpuNameAsync()
    {
        lock (_dataLock)
        {
            if (_isResetting || !IsLibreHardwareMonitorInitialized())
                return Task.FromResult(UNKNOWN_NAME);

            if (!string.IsNullOrEmpty(_cachedGpuName) && !_needRefreshGpuHardware)
                return Task.FromResult(_cachedGpuName);

            var dGpu = _gpuHardware ?? _amdGpuHardware;
            var gpu = ShouldUseIntegratedGpuSnapshot(dGpu) ? _iGpuHardware : dGpu;
            _cachedGpuName = gpu != null ? StripName(gpu.Name) : UNKNOWN_NAME;
            _needRefreshGpuHardware = false;
            return Task.FromResult(_cachedGpuName);
        }
    }

    public Task<bool> IsCurrentGpuIntegratedAsync()
    {
        lock (_dataLock)
        {
            if (_isResetting || !IsLibreHardwareMonitorInitialized())
                return Task.FromResult(false);

            return Task.FromResult(ShouldUseIntegratedGpuSnapshot(_gpuHardware ?? _amdGpuHardware));
        }
    }

    public Task<float> GetCpuPowerAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotCpuPower);
    }

    public Task<(float cores, float memory, float platform)> GetCpuComponentPowersAsync()
    {
        lock (_dataLock) return Task.FromResult((_snapshotCpuCoresPower, _snapshotCpuMemoryPower, _snapshotCpuPlatformPower));
    }

    public Task<float> GetCpuVoltageAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotCpuVoltage);
    }

    public Task<float> GetCpuCoreClockAsync()
    {
        lock (_dataLock) return Task.FromResult(_showAverageCpuFrequency ? _snapshotCpuAvgClock : _snapshotCpuMaxClock);
    }

    public Task<float> GetCpuPCoreClockAsync()
    {
        lock (_dataLock) return Task.FromResult(_showAverageCpuFrequency ? _snapshotCpuPAvgClock : _snapshotCpuPClock);
    }

    public Task<float> GetCpuECoreClockAsync()
    {
        lock (_dataLock) return Task.FromResult(_showAverageCpuFrequency ? _snapshotCpuEAvgClock : _snapshotCpuEClock);
    }

    public Task<float> GetGpuPowerAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuPower);
    }

    public Task<float> GetGpuVoltageAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuVoltage);
    }

    public Task<float> GetGpuVramTemperatureAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuVramTemp);
    }

    public Task<float> GetGpuHotSpotTemperatureAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuHotSpotTemp);
    }

    public Task<float> GetGpuVramUtilizationAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuVramUtilization);
    }

    public Task<float> GetGpuVramUsedAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuVramUsage > 0 ? _snapshotGpuVramUsage / MB_PER_GB : _snapshotGpuVramUsage);
    }

    public Task<float> GetGpuVramTotalAsync()
    {
        lock (_dataLock)
        {
            var dGpu = _gpuHardware ?? _amdGpuHardware;
            float total = ShouldUseIntegratedGpuSnapshot(dGpu) ? _cachedIGpuVramTotal : _cachedGpuVramTotal;
            return Task.FromResult(total > 0 ? total / MB_PER_GB : INVALID_VALUE_FLOAT);
        }
    }

    public Task<float> GetGpuPcieRxThroughputAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuPcieRxThroughput);
    }

    public Task<float> GetGpuPcieTxThroughputAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotGpuPcieTxThroughput);
    }

    public Task<(float, float)> GetSsdTemperaturesAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotSsdTemps);
    }

    public Task<float> GetMemoryUsageAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotMemUsage);
    }

    public Task<float> GetMemoryUsedAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotMemUsed);
    }

    public Task<float> GetMemoryTotalAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotMemTotal);
    }

    public Task<double> GetHighestMemoryTemperatureAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotMemMaxTemp);
    }

    public Task<double> GetHighestMotherboardTemperatureAsync()
    {
        lock (_dataLock) return Task.FromResult(_snapshotMotherboardMaxTemp);
    }

    private async Task<LibreHardwareMonitorInitialState> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            InitialState = _hardware.Count == 0 ? LibreHardwareMonitorInitialState.Fail : LibreHardwareMonitorInitialState.Initialized;
            return InitialState;
        }
        await _initSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                InitialState = _hardware.Count == 0 ? LibreHardwareMonitorInitialState.Fail : LibreHardwareMonitorInitialState.Initialized;
                return InitialState;
            }
            await Task.Run(GetHardware).ConfigureAwait(false);
            _initialized = true;
            InitialState = _hardware.Count == 0 ? LibreHardwareMonitorInitialState.Fail : LibreHardwareMonitorInitialState.Success;
            return InitialState;
        }
        catch (DllNotFoundException) { HandleInitException("DLL Not Found"); InitialState = LibreHardwareMonitorInitialState.PawnIONotInstalled; return InitialState; }
        catch (Exception ex) { HandleInitException(ex.Message); throw; }
        finally { _initSemaphore.Release(); }
    }

    private void HandleInitException(string reason)
    {
        Log.Instance.Trace($"LibreHardwareMonitor initialization failed: {reason}");
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        settings.Store.EnableHardwareSensors = false;
        settings.SynchronizeStore();
        InitialState = LibreHardwareMonitorInitialState.Fail;
    }

    public void NeedRefreshHardware(string hardwareId)
    {
        if (!IsLibreHardwareMonitorInitialized() || _computer == null || hardwareId != HARDWARE_ID_NVIDIA_GPU) return;
        lock (_hardwareLock)
        {
            ResetSensors();

            try
            {
                NVAPI.Initialize();
            }
            catch
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Failed to initialize NVAPI");
            }

            _needRefreshGpuHardware = true;
        }
    }

    public async Task UpdateAsync()
    {
        if (_isResetting || !IsLibreHardwareMonitorInitialized()) return;

        var gpuState = await _gpuController.GetLastKnownStateAsync().ConfigureAwait(false);
        bool gpuInactive = IsGpuInActive(gpuState);

        await Task.Run(() =>
        {
            lock (_hardwareLock)
            {
                if (_isResetting || _computer == null || !_hardwareInitialized) return;
                try
                {
                    foreach (var h in _hardware)
                    {
                        if (h == null) continue;

                        if (gpuInactive && h.HardwareType == HardwareType.GpuNvidia)
                        {
                            continue;
                        }

                        h.Update();
                    }

                    lock (_dataLock)
                    {
                        _snapshotCpuTemp = _cpuTempSensor?.Value ?? INVALID_VALUE_FLOAT;
                        _snapshotCpuUsage = _cpuUsageSensor?.Value ?? INVALID_VALUE_FLOAT;
                        _snapshotCpuVoltage = _cpuCoreVoltageSensor?.Value ?? INVALID_VALUE_FLOAT;

                        if (_cpuCoreClockSensors.Count > 0)
                        {
                            var (max, avg) = ComputeMaxAndAverage(_cpuCoreClockSensors);
                            _snapshotCpuMaxClock = max;
                            _snapshotCpuAvgClock = avg;
                        }
                        else
                        {
                            _snapshotCpuMaxClock = INVALID_VALUE_FLOAT;
                            _snapshotCpuAvgClock = INVALID_VALUE_FLOAT;
                        }

                        if (IsHybrid)
                        {
                            var (pMax, pAvg) = _pCoreClockSensors.Count > 0
                                ? ComputeMaxAndAverage(_pCoreClockSensors)
                                : (INVALID_VALUE_FLOAT, INVALID_VALUE_FLOAT);
                            var (eMax, eAvg) = _eCoreClockSensors.Count > 0
                                ? ComputeMaxAndAverage(_eCoreClockSensors)
                                : (INVALID_VALUE_FLOAT, INVALID_VALUE_FLOAT);

                            _snapshotCpuPClock = pMax > 0 ? (float)Math.Round(pMax) : pMax;
                            _snapshotCpuEClock = eMax > 0 ? (float)Math.Round(eMax) : eMax;
                            _snapshotCpuPAvgClock = pAvg > 0 ? (float)Math.Round(pAvg) : pAvg;
                            _snapshotCpuEAvgClock = eAvg > 0 ? (float)Math.Round(eAvg) : eAvg;
                        }

                        var cpuPackagePower = _cpuPackagePowerSensor?.Value ?? INVALID_VALUE_FLOAT;
                        var cpuComponentReadings = _cpuComponentPowerSensors
                            .Select(sensor => (sensor.Name, value: sensor.Value ?? INVALID_VALUE_FLOAT))
                            .ToArray();
                        var cpuComponentPower = cpuComponentReadings.Select(reading => reading.value).ToArray();
                        var resolvedCpuPower = ResolveCpuPower(cpuPackagePower, cpuComponentPower);
                        (_snapshotCpuCoresPower, _snapshotCpuMemoryPower, _snapshotCpuPlatformPower) = ResolveCpuComponentPowers(cpuComponentReadings);

                        if (Log.Instance.IsTraceEnabled)
                        {
                            Log.Instance.Trace(
                                $"LibreHardwareMonitor CPU power raw values: package={cpuPackagePower}, components=[{string.Join(", ", cpuComponentReadings.Select(reading => $"{reading.Name}={reading.value}"))}], resolved={resolvedCpuPower}");
                        }

                        if (resolvedCpuPower > MAX_VALID_CPU_POWER) { Task.Run(ResetSensors); _snapshotCpuPower = INVALID_VALUE_FLOAT; }
                        else if (resolvedCpuPower <= MIN_VALID_POWER_READING) { _snapshotCpuPower = INVALID_VALUE_FLOAT; }
                        else
                        {
                            if (Math.Abs(resolvedCpuPower - _cachedCpuPower) < float.Epsilon)
                            {
                                if (++_cachedCpuPowerTime >= MAX_CPU_POWER_STUCK_RETRIES) { Task.Run(ResetSensors); _snapshotCpuPower = INVALID_VALUE_FLOAT; }
                                else _snapshotCpuPower = resolvedCpuPower;
                            }
                            else { _cachedCpuPower = resolvedCpuPower; _cachedCpuPowerTime = 0; _snapshotCpuPower = resolvedCpuPower; }
                        }

                        var dGpu = _gpuHardware ?? _amdGpuHardware;

                        if (ShouldUseIntegratedGpuSnapshot(dGpu))
                        {
                            var iGpuVramTotal = _iGpuVramTotalSensor?.Value ?? _cachedIGpuVramTotal;
                            var iGpuVramUsed = _iGpuD3DVramUsedSensor?.Value ?? INVALID_VALUE_FLOAT;
                            var iGpuVramFree = _iGpuVramFreeSensor?.Value ?? INVALID_VALUE_FLOAT;
                            var iGpuVramMetrics = ResolveGpuVramMetrics(iGpuVramUsed, iGpuVramTotal, iGpuVramFree);

                            _snapshotGpuVramUsage = iGpuVramMetrics.used;
                            _snapshotGpuVramUtilization = iGpuVramMetrics.utilization;
                            _cachedIGpuVramTotal = iGpuVramMetrics.total > 0 ? iGpuVramMetrics.total : _cachedIGpuVramTotal;

                            _snapshotGpuPower = _iGpuPowerSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuUsage = _iGpuUsageSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuTemp = _iGpuTempSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuVoltage = _iGpuCoreVoltageSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuVramTemp = INVALID_VALUE_FLOAT;
                            _snapshotGpuHotSpotTemp = INVALID_VALUE_FLOAT;
                            _snapshotGpuClock = _iGpuClockSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuMemoryClock = _iGpuMemoryClockSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuPcieRxThroughput = _iGpuPcieRxSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuPcieTxThroughput = _iGpuPcieTxSensor?.Value ?? INVALID_VALUE_FLOAT;
                        }
                        else if (gpuInactive)
                        {
                            _snapshotGpuVramUtilization = INVALID_VALUE_FLOAT;
                            _snapshotGpuVramUsage = INVALID_VALUE_FLOAT;
                            _snapshotGpuPower = INVALID_VALUE_FLOAT;
                            _snapshotGpuVoltage = INVALID_VALUE_FLOAT;
                            _snapshotGpuVramTemp = INVALID_VALUE_FLOAT;
                            _snapshotGpuHotSpotTemp = INVALID_VALUE_FLOAT;
                            _snapshotGpuUsage = INVALID_VALUE_FLOAT;
                            _snapshotGpuTemp = INVALID_VALUE_FLOAT;
                            _snapshotGpuClock = INVALID_VALUE_FLOAT;
                            _snapshotGpuMemoryClock = INVALID_VALUE_FLOAT;
                            _snapshotGpuPcieRxThroughput = INVALID_VALUE_FLOAT;
                            _snapshotGpuPcieTxThroughput = INVALID_VALUE_FLOAT;
                        }
                        else
                        {
                            var gpuVramTotal = _gpuVramTotalSensor?.Value ?? _cachedGpuVramTotal;
                            var gpuVramUsed = _gpuD3DVramUsedSensor?.Value ?? INVALID_VALUE_FLOAT;
                            var gpuVramFree = _gpuVramFreeSensor?.Value ?? INVALID_VALUE_FLOAT;
                            var gpuVramMetrics = ResolveGpuVramMetrics(gpuVramUsed, gpuVramTotal, gpuVramFree);

                            _snapshotGpuVramUsage = gpuVramMetrics.used;
                            _snapshotGpuVramUtilization = gpuVramMetrics.utilization;
                            _cachedGpuVramTotal = gpuVramMetrics.total > 0 ? gpuVramMetrics.total : _cachedGpuVramTotal;

                            float gPower = _gpuPowerSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuPower = ResolveGpuPower(gPower, _lastGpuPower);
                            if (_snapshotGpuPower > MIN_VALID_POWER_READING)
                                _lastGpuPower = _snapshotGpuPower;
                            _snapshotGpuVoltage = _gpuCoreVoltageSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuVramTemp = _gpuVramTemperatureSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuHotSpotTemp = _gpuHotSpotSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuUsage = _gpuUsageSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuTemp = _gpuTempSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuClock = _gpuClockSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuMemoryClock = _gpuMemoryClockSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuPcieRxThroughput = _gpuPcieRxSensor?.Value ?? INVALID_VALUE_FLOAT;
                            _snapshotGpuPcieTxThroughput = _gpuPcieTxSensor?.Value ?? INVALID_VALUE_FLOAT;
                        }

                        _snapshotMemUsage = _memoryLoadSensor?.Value ?? INVALID_VALUE_FLOAT;

                        float memoryUsed = _memoryUsedSensor?.Value ?? INVALID_VALUE_FLOAT;
                        float memoryAvailable = _memoryAvailableSensor?.Value ?? INVALID_VALUE_FLOAT;

                        if (memoryUsed >= 0 && memoryAvailable >= 0)
                        {
                            _snapshotMemUsed = memoryUsed;
                            _snapshotMemTotal = memoryUsed + memoryAvailable;
                            _cachedMemoryTotal = _snapshotMemTotal;

                            if (_snapshotMemUsage < 0 && _snapshotMemTotal > 0)
                            {
                                _snapshotMemUsage = (_snapshotMemUsed / _snapshotMemTotal) * 100f;
                            }
                        }
                        else if (_cachedMemoryTotal > 0 && _snapshotMemUsage >= 0)
                        {
                            _snapshotMemTotal = _cachedMemoryTotal;
                            _snapshotMemUsed = (_snapshotMemUsage / 100f) * _snapshotMemTotal;
                        }
                        else
                        {
                            _snapshotMemUsed = INVALID_VALUE_FLOAT;
                            _snapshotMemTotal = INVALID_VALUE_FLOAT;
                        }

                        _snapshotMemMaxTemp = _memoryTempSensors.Count > 0 ? (double)(_memoryTempSensors.Max(s => s.Value) ?? 0) : INVALID_VALUE_DOUBLE;
                        _snapshotMotherboardMaxTemp = _motherboardTempSensors.Count > 0 ? (double)(_motherboardTempSensors.Max(s => s.Value) ?? 0) : INVALID_VALUE_DOUBLE;

                        float t1 = _storageTempSensors.Count > 0 ? _storageTempSensors[0].Value ?? INVALID_VALUE_FLOAT : INVALID_VALUE_FLOAT;
                        float t2 = _storageTempSensors.Count > 1 ? _storageTempSensors[1].Value ?? INVALID_VALUE_FLOAT : INVALID_VALUE_FLOAT;
                        _snapshotSsdTemps = (t1, t2);
                    }
                }
                catch (Exception ex) { if (ex is IndexOutOfRangeException) Task.Run(ResetSensors); }
            }
        }).ConfigureAwait(false);
    }

    private void ResetSensors()
    {
        _isResetting = true;
        try
        {
            lock (_hardwareLock)
            {
                _computer?.Close(); _hardware.Clear();
                _computer?.Open(); _computer?.Accept(new UpdateVisitor()); _computer?.Reset();
                if (_computer == null)
                {
                    return;
                }

                _hardware.AddRange(EnumerateHardwareTree(_computer.Hardware)); RefreshSensorCache();
            }
        }
        finally { _isResetting = false; }
    }

    private static string StripName(string name)
    {
        if (string.IsNullOrEmpty(name)) return UNKNOWN_NAME;
        var cleaned = name.Trim();
        if (cleaned.Contains("AMD", StringComparison.OrdinalIgnoreCase)) cleaned = Regex.Replace(cleaned, REGEX_STRIP_AMD, "", RegexOptions.IgnoreCase);
        else if (cleaned.Contains("Intel", StringComparison.OrdinalIgnoreCase)) cleaned = Regex.Replace(cleaned, REGEX_STRIP_INTEL, "", RegexOptions.IgnoreCase);
        else if (cleaned.Contains("Nvidia", StringComparison.OrdinalIgnoreCase) || cleaned.Contains("GeForce", StringComparison.OrdinalIgnoreCase))
        {
            var m = Regex.Match(cleaned, REGEX_STRIP_NVIDIA);
            if (m.Success) cleaned = m.Groups[1].Value;
        }
        return Regex.Replace(cleaned, REGEX_CLEAN_SPACES, " ").Trim();
    }

    public bool IsGpuInActive(GPUState state) => state is GPUState.Inactive or GPUState.PoweredOff or GPUState.Unknown or GPUState.NvidiaGpuNotFound;
    public bool IsLibreHardwareMonitorInitialized() => InitialState is LibreHardwareMonitorInitialState.Initialized or LibreHardwareMonitorInitialState.Success;

    public void Start(object subscriber, TimeSpan interval)
    {
        lock (_subscribers)
        {
            _subscribers[subscriber] = interval;
            UpdateProducerLoop();
        }
    }

    public void Stop(object subscriber)
    {
        lock (_subscribers)
        {
            if (_subscribers.Remove(subscriber))
            {
                UpdateProducerLoop();
            }
        }
    }

    private void UpdateProducerLoop()
    {
        if (_subscribers.Count == 0)
        {
            StopProducerLoop();
            return;
        }

        StopProducerLoop();

        _producerCts = new CancellationTokenSource();
        var token = _producerCts.Token;
        _producerTask = Task.Run(() => ProducerLoop(token), token);
    }

    private void StopProducerLoop()
    {
        _producerCts?.Cancel();
        _producerCts?.Dispose();
        _producerCts = null;
        _producerTask = null;
    }

    private async Task ProducerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TimeSpan minInterval;
            lock (_subscribers)
            {
                if (_subscribers.Count == 0) return;
                minInterval = _subscribers.Values.Min();
            }

            try
            {
                await UpdateAsync().ConfigureAwait(false);
                SensorsUpdated?.Invoke(this, EventArgs.Empty);

                await _delayProvider.Delay(minInterval, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"ProducerLoop error: {ex}");
                await _delayProvider.Delay(TimeSpan.FromMilliseconds(1000), token).ConfigureAwait(false);
            }
        }
    }

    private static (float max, float avg) ComputeMaxAndAverage(List<ISensor> sensors)
    {
        if (sensors.Count == 0) return (0, 0);
        float max = float.MinValue, sum = 0;
        foreach (var s in sensors)
        {
            var val = s.Value ?? INVALID_VALUE_FLOAT;
            if (val > max) max = val;
            sum += val;
        }
        return (max, sum / sensors.Count);
    }

    public void Dispose()
    {
        lock (_hardwareLock) { _computer?.Close(); _computer = null; _hardwareInitialized = false; }
        _initSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
