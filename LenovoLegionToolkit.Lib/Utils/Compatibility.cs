using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.DeviceSupport;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.System.Management;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;

// ReSharper disable StringLiteralTypo

namespace LenovoLegionToolkit.Lib.Utils;

public static partial class Compatibility
{
    [GeneratedRegex("^[A-Z0-9]{4}")]
    private static partial Regex BiosPrefixRegex();

    [GeneratedRegex("[0-9]{2}")]
    private static partial Regex BiosVersionRegex();

    private static readonly Dictionary<string, LegionSeries> MachineTypeMap = new()
    {
        { "83F0", LegionSeries.Legion_5 }, { "83F1", LegionSeries.Legion_5 }, { "83M0", LegionSeries.Legion_5 },
        { "83NX", LegionSeries.Legion_5 }, { "83N2", LegionSeries.Legion_5 }, { "83LY", LegionSeries.Legion_5 },
        { "83DG", LegionSeries.Legion_5 }, { "83EW", LegionSeries.Legion_5 }, { "83EG", LegionSeries.Legion_5 },
        { "83JJ", LegionSeries.Legion_5 }, { "82RC", LegionSeries.Legion_5 }, { "82RB", LegionSeries.Legion_5 },
        { "82TB", LegionSeries.Legion_5 }, { "83EF", LegionSeries.Legion_5 }, { "82RE", LegionSeries.Legion_5 },
        { "82RD", LegionSeries.Legion_5 },

        { "83DH", LegionSeries.Legion_Slim_5 }, { "83EX", LegionSeries.Legion_Slim_5 }, { "82Y5", LegionSeries.Legion_Slim_5 },
        { "82Y9", LegionSeries.Legion_Slim_5 }, { "82YA", LegionSeries.Legion_Slim_5 }, { "83D6", LegionSeries.Legion_Slim_5 },

        { "83LT", LegionSeries.Legion_Pro_5 }, { "83F3", LegionSeries.Legion_Pro_5 }, { "83DF", LegionSeries.Legion_Pro_5 },
        { "83F2", LegionSeries.Legion_Pro_5 }, { "83LU", LegionSeries.Legion_Pro_5 }, { "82WM", LegionSeries.Legion_Pro_5 },
        { "83NN", LegionSeries.Legion_Pro_5 }, { "82WK", LegionSeries.Legion_Pro_5 }, { "82JQ", LegionSeries.Legion_Pro_5 },

        { "83KY", LegionSeries.Legion_7 }, { "83FD", LegionSeries.Legion_7 }, { "82UH", LegionSeries.Legion_7 },
        { "82TD", LegionSeries.Legion_7 }, { "82N6", LegionSeries.Legion_7 },

        { "83RU", LegionSeries.Legion_Pro_7 }, { "83F5", LegionSeries.Legion_Pro_7 }, { "83DE", LegionSeries.Legion_Pro_7 },
        { "82WR", LegionSeries.Legion_Pro_7 }, { "82WQ", LegionSeries.Legion_Pro_7 }, { "82WS", LegionSeries.Legion_Pro_7 },

        { "83G0", LegionSeries.Legion_9 }, { "83EY", LegionSeries.Legion_9 },
        { "83E1", LegionSeries.Legion_Go }
    };

    private static readonly (string Keyword, LegionSeries Series)[] ModelKeywordMap =
    [
        ("LOQ", LegionSeries.LOQ),
        ("IdeaPad Gaming", LegionSeries.IdeaPad_Gaming),
        ("IdeaPad", LegionSeries.IdeaPad),
        ("XiaoXin", LegionSeries.IdeaPad),
        ("YOGA", LegionSeries.YOGA),
        ("Lenovo Slim", LegionSeries.Lenovo_Slim),
        ("ThinkBook", LegionSeries.ThinkBook),
        ("Legion", LegionSeries.Legion_Legacy)
    ];

    private static MachineInformation? _machineInformation;
    private static bool? _isCompatible;

    public const string SmokeSimulateLegionEnvironmentVariable = "LLT_SMOKE_SIMULATE_LEGION";

    public static bool IsSmokeLegionSimulationEnabled =>
        string.Equals(Environment.GetEnvironmentVariable(SmokeSimulateLegionEnvironmentVariable), "1", StringComparison.OrdinalIgnoreCase);

    public static Task<bool> CheckBasicCompatibilityAsync() => IsSmokeLegionSimulationEnabled
        ? Task.FromResult(true)
        : WMI.LenovoGameZoneData.ExistsAsync();

    public static DeviceFeatureAvailability GetDeviceFeatureAvailability(MachineInformation machineInformation) =>
        LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

    public static bool IsSupportedDevice(MachineInformation machineInformation) =>
        GetDeviceFeatureAvailability(machineInformation).IsSupported;

    public static bool IsSupportedLegionMachine(MachineInformation machineInformation) =>
        IsSupportedDevice(machineInformation);

    public static async Task<(bool isCompatible, MachineInformation machineInformation)> IsCompatibleAsync()
    {
        var mi = await GetMachineInformationAsync().ConfigureAwait(false);

        if (_isCompatible.HasValue)
            return (_isCompatible.Value, mi);

        if (IsSmokeLegionSimulationEnabled)
        {
            _isCompatible = true;
            return (true, mi);
        }

        var isSupportedLenovoDevice = IsSupportedLegionMachine(mi);
        if (isSupportedLenovoDevice && !await CheckBasicCompatibilityAsync().ConfigureAwait(false))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Supported Lenovo device detected without LenovoGameZoneData; continuing in basic mode.");
        }

        _isCompatible = true;
        return (_isCompatible.Value, mi);
    }

    public static bool IsCompatible => _isCompatible ?? false;

    public static async Task<MachineInformation> GetMachineInformationAsync()
    {
        if (_machineInformation.HasValue)
            return _machineInformation.Value;

        if (IsSmokeLegionSimulationEnabled)
            return (_machineInformation = GetSmokeMachineInformation()).Value;

        var (vendor, machineType, model, serialNumber) = await GetModelDataAsync().ConfigureAwait(false);
        var hardware = await HardwareInventoryProvider.ReadAsync().ConfigureAwait(false);
        var generation = GetMachineGeneration(model);
        var legionSeries = GetLegionSeries(model, machineType);
        var (biosVersion, biosVersionRaw) = GetBIOSVersion();
        var supportedPowerModes = (await GetSupportedPowerModesAsync().ConfigureAwait(false)).ToArray();
        var smartFanVersion = await GetSmartFanVersionAsync().ConfigureAwait(false);
        var legionZoneVersion = await GetLegionZoneVersionAsync().ConfigureAwait(false);
        var features = await GetFeaturesAsync().ConfigureAwait(false);

        var machineInformation = new MachineInformation
        {
            Generation = generation,
            LegionSeries = legionSeries,
            Vendor = vendor,
            MachineType = machineType,
            Model = model,
            SerialNumber = serialNumber,
            BiosVersion = biosVersion,
            BiosVersionRaw = biosVersionRaw,
            SupportedPowerModes = supportedPowerModes,
            SmartFanVersion = smartFanVersion,
            LegionZoneVersion = legionZoneVersion,
            Features = features,
            Hardware = hardware,
            Properties = new()
            {
                SupportsAlwaysOnAc = GetAlwaysOnAcStatus(),
                SupportsExtremeMode = GetSupportsExtremeMode(supportedPowerModes, smartFanVersion, legionZoneVersion),
                SupportsGodModeV1 = GetSupportsGodModeV1(supportedPowerModes, smartFanVersion, legionZoneVersion, biosVersion),
                SupportsGodModeV2 = GetSupportsGodModeV2(supportedPowerModes, smartFanVersion, legionZoneVersion),
                SupportsGodModeV3 = GetSupportsGodModeV3(supportedPowerModes, smartFanVersion, legionZoneVersion, generation, model, machineType),
                SupportsGodModeV4 = GetSupportsGodModeV4(supportedPowerModes, smartFanVersion, legionZoneVersion),
                SupportsGSync = await GetSupportsGSyncAsync().ConfigureAwait(false),
                SupportsIGPUMode = await GetSupportsIGPUModeAsync().ConfigureAwait(false),
                SupportsAIMode = await GetSupportsAIModeAsync().ConfigureAwait(false),
                SupportBootLogoChange = GetSupportBootLogoChange(smartFanVersion),
                SupportsITSMode = GetSupportITSMode(model),
                HasQuietToPerformanceModeSwitchingBug = GetHasQuietToPerformanceModeSwitchingBug(biosVersion),
                HasGodModeToOtherModeSwitchingBug = GetHasGodModeToOtherModeSwitchingBug(biosVersion),
                HasReapplyParameterIssue = GetHasReapplyParameterIssue(model, machineType),
                HasSpectrumProfileSwitchingBug = GetHasSpectrumProfileSwitchingBug(model, machineType),
                IsExcludedFromLenovoLighting = GetIsExcludedFromLenovoLighting(biosVersion, generation, legionSeries),
                IsExcludedFromPanelLogoLenovoLighting = GetIsExcludedFromPanelLenovoLighting(machineType, model),
                HasAlternativeFullSpectrumLayout = GetHasAlternativeFullSpectrumLayout(machineType),
                IsAmdDevice = GetIsAmdDevice(model),
                IsChineseModel = GetIsChineseModel(model),
            }
        };

        if (Log.Instance.IsTraceEnabled)
        {
            Log.Instance.Trace($"Retrieved machine information:");
            Log.Instance.Trace($" * Vendor: '{machineInformation.Vendor}'");
            Log.Instance.Trace($" * Machine Type: '{machineInformation.MachineType}'");
            Log.Instance.Trace($" * Model: '{machineInformation.Model}'");
            Log.Instance.Trace($" * BIOS: '{machineInformation.BiosVersion}' [{machineInformation.BiosVersionRaw}]");
            Log.Instance.Trace($" * SupportedPowerModes: '{string.Join(",", machineInformation.SupportedPowerModes)}'");
            Log.Instance.Trace($" * SmartFanVersion: '{machineInformation.SmartFanVersion}'");
            Log.Instance.Trace($" * LegionZoneVersion: '{machineInformation.LegionZoneVersion}'");
            Log.Instance.Trace($" * Features: {machineInformation.Features.Source}:{string.Join(',', machineInformation.Features.All)}");
            LogHardwareInventory(machineInformation.Hardware);
            Log.Instance.Trace($" * Properties:");
            Log.Instance.Trace($"     * SupportsAlwaysOnAc: '{machineInformation.Properties.SupportsAlwaysOnAc.status}, {machineInformation.Properties.SupportsAlwaysOnAc.connectivity}'");
            Log.Instance.Trace($"     * SupportsExtremeMode: '{machineInformation.Properties.SupportsExtremeMode}'");
            Log.Instance.Trace($"     * SupportsGodModeV1: '{machineInformation.Properties.SupportsGodModeV1}'");
            Log.Instance.Trace($"     * SupportsGodModeV2: '{machineInformation.Properties.SupportsGodModeV2}'");
            Log.Instance.Trace($"     * SupportsGodModeV3: '{machineInformation.Properties.SupportsGodModeV3}'");
            Log.Instance.Trace($"     * SupportsGodModeV4: '{machineInformation.Properties.SupportsGodModeV4}'");
            Log.Instance.Trace($"     * SupportsGSync: '{machineInformation.Properties.SupportsGSync}'");
            Log.Instance.Trace($"     * SupportsIGPUMode: '{machineInformation.Properties.SupportsIGPUMode}'");
            Log.Instance.Trace($"     * SupportsAIMode: '{machineInformation.Properties.SupportsAIMode}'");
            Log.Instance.Trace($"     * SupportsITSMode: '{machineInformation.Properties.SupportsITSMode}'");
            Log.Instance.Trace($"     * SupportBootLogoChange: '{machineInformation.Properties.SupportBootLogoChange}'");
            Log.Instance.Trace($"     * HasQuietToPerformanceModeSwitchingBug: '{machineInformation.Properties.HasQuietToPerformanceModeSwitchingBug}'");
            Log.Instance.Trace($"     * HasGodModeToOtherModeSwitchingBug: '{machineInformation.Properties.HasGodModeToOtherModeSwitchingBug}'");
            Log.Instance.Trace($"     * HasReapplyParameterIssue: '{machineInformation.Properties.HasReapplyParameterIssue}'");
            Log.Instance.Trace($"     * HasSpectrumProfileSwitchingBug: '{machineInformation.Properties.HasSpectrumProfileSwitchingBug}'");
            Log.Instance.Trace($"     * IsExcludedFromLenovoLighting: '{machineInformation.Properties.IsExcludedFromLenovoLighting}'");
            Log.Instance.Trace($"     * IsExcludedFromPanelLogoLenovoLighting: '{machineInformation.Properties.IsExcludedFromPanelLogoLenovoLighting}'");
            Log.Instance.Trace($"     * HasAlternativeFullSpectrumLayout: '{machineInformation.Properties.HasAlternativeFullSpectrumLayout}'");
            Log.Instance.Trace($"     * IsAmdDevice: '{machineInformation.Properties.IsAmdDevice}'");
            Log.Instance.Trace($"     * IsChineseModel: '{machineInformation.Properties.IsChineseModel}'");
        }

        return (_machineInformation = machineInformation).Value;
    }

    private static void LogHardwareInventory(HardwareInventory hardware)
    {
        Log.Instance.Trace($" * Hardware:");
        Log.Instance.Trace($"     * ComputerSystem: '{hardware.ComputerSystem.Manufacturer}' '{hardware.ComputerSystem.Model}' '{hardware.ComputerSystem.SystemFamily}' '{hardware.ComputerSystem.SystemType}'");
        Log.Instance.Trace($"     * BaseBoard: '{hardware.BaseBoard.Manufacturer}' '{hardware.BaseBoard.Product}' '{hardware.BaseBoard.Version}'");
        Log.Instance.Trace($"     * Chassis: '{hardware.Chassis.Manufacturer}' '{string.Join(",", hardware.Chassis.ChassisTypeNames)}'");
        Log.Instance.Trace($"     * CPU: '{string.Join(" | ", hardware.Processors.Select(processor => processor.Name).Where(name => !string.IsNullOrWhiteSpace(name)))}'");
        Log.Instance.Trace($"     * GPU: '{string.Join(" | ", hardware.VideoControllers.Select(videoController => videoController.Name).Where(name => !string.IsNullOrWhiteSpace(name)))}'");
        Log.Instance.Trace($"     * Memory: '{FormatCapacity(hardware.Memory.TotalCapacityBytes)}' modules='{hardware.Memory.ModuleCount}' speed='{hardware.Memory.ConfiguredClockSpeedMHz ?? hardware.Memory.SpeedMHz}'");
        Log.Instance.Trace($"     * Battery: '{string.Join(" | ", hardware.Batteries.Select(battery => battery.Name).Where(name => !string.IsNullOrWhiteSpace(name)))}'");
    }

    private static MachineInformation GetSmokeMachineInformation() => new()
    {
        Generation = 9,
        LegionSeries = LegionSeries.Legion_Pro_7,
        Vendor = "LENOVO",
        MachineType = "83DE",
        Model = "Legion Y9000P IRX9",
        SerialNumber = "SMOKE-LEGION",
        BiosVersion = new("NMCN", 32),
        BiosVersionRaw = "NMCN32WW",
        SupportedPowerModes =
        [
            PowerModeState.Quiet,
            PowerModeState.Balance,
            PowerModeState.Performance,
            PowerModeState.Extreme,
            PowerModeState.GodMode
        ],
        SmartFanVersion = 6,
        LegionZoneVersion = 3,
        Features = MachineInformation.FeatureData.Unknown,
        Hardware = new()
        {
            ComputerSystem = new()
            {
                Manufacturer = "LENOVO",
                Model = "Legion Y9000P IRX9",
                SystemFamily = "Legion"
            },
            BaseBoard = new()
            {
                Manufacturer = "LENOVO",
                Product = "83DE"
            },
            Chassis = new()
            {
                Manufacturer = "LENOVO",
                ChassisTypes = [10]
            },
            Processors =
            [
                new()
                {
                    Name = "Intel(R) Core(TM) i9-14900HX",
                    Manufacturer = "GenuineIntel",
                    NumberOfCores = 24,
                    NumberOfLogicalProcessors = 32
                }
            ],
            VideoControllers =
            [
                new()
                {
                    Name = "NVIDIA GeForce RTX 4060 Laptop GPU",
                    AdapterCompatibility = "NVIDIA"
                }
            ],
            Memory = new()
            {
                TotalCapacityBytes = 32UL * 1024 * 1024 * 1024,
                ModuleCount = 2,
                ConfiguredClockSpeedMHz = 5600
            },
            Batteries =
            [
                new()
                {
                    Name = "L22B4PC0",
                    Status = "OK"
                }
            ]
        },
        Properties = new()
        {
            SupportsAlwaysOnAc = (false, false),
            SupportsExtremeMode = true,
            SupportsGodModeV2 = true,
            SupportsGSync = true,
            SupportsIGPUMode = true,
            SupportsAIMode = true,
            SupportBootLogoChange = true,
            IsChineseModel = true,
        }
    };

    private static Task<(string, string, string, string)> GetModelDataAsync() => WMI.Win32.ComputerSystemProduct.ReadAsync();

    private static (BiosVersion?, string?) GetBIOSVersion()
    {
        var result = Registry.GetValue("HKEY_LOCAL_MACHINE", "HARDWARE\\DESCRIPTION\\System\\BIOS", "BIOSVersion", string.Empty).Trim();

        var prefixRegex = BiosPrefixRegex();
        var versionRegex = BiosVersionRegex();

        var prefix = prefixRegex.Match(result).Value;
        var versionString = versionRegex.Match(result).Value;

        if (!int.TryParse(versionRegex.Match(versionString).Value, out var version))
            return (null, null);

        return (new(prefix, version), result);
    }

    private static bool GetIsChineseModel(string model)
    {
        string[] chineseModelIndicators =
        [
            "R7000",
            "R9000",
            "Y7000",
            "Y9000"
        ];

        return chineseModelIndicators.Any(model.Contains);
    }

    private static bool GetIsAmdDevice(string model)
    {
        if (string.IsNullOrEmpty(model))
            return false;

        var normalizedModel = model.ToUpperInvariant();
        var match = Regex.Match(normalizedModel, @"([AI][A-Z]{2}\d+|\bR\d{4})", RegexOptions.RightToLeft);

        if (!match.Success)
            return false;

        var value = match.Value;
        return value.StartsWith("A") || value.StartsWith("R");
    }

    private static string FormatCapacity(ulong bytes)
    {
        if (bytes == 0)
            return string.Empty;

        const double gibibyte = 1024d * 1024d * 1024d;
        return $"{bytes / gibibyte:0.#} GiB";
    }

    private static async Task<MachineInformation.FeatureData> GetFeaturesAsync()
    {
        try
        {
            var capabilities = await WMI.LenovoCapabilityData00.ReadAsync().ConfigureAwait(false);
            return new(MachineInformation.FeatureData.SourceType.CapabilityData, capabilities);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"LenovoCapabilityData00 read failed, falling back to feature flags", ex);
        }

        try
        {
            var featureFlags = await WMI.LenovoOtherMethod.GetLegionDeviceSupportFeatureAsync().ConfigureAwait(false);

            return new(MachineInformation.FeatureData.SourceType.Flags)
            {
                [CapabilityID.IGPUMode] = featureFlags.IsBitSet(0),
                [CapabilityID.NvidiaGPUDynamicDisplaySwitching] = featureFlags.IsBitSet(4),
                [CapabilityID.InstantBootAc] = featureFlags.IsBitSet(5),
                [CapabilityID.InstantBootUsbPowerDelivery] = featureFlags.IsBitSet(6),
                [CapabilityID.AMDSmartShiftMode] = featureFlags.IsBitSet(7),
                [CapabilityID.AMDSkinTemperatureTracking] = featureFlags.IsBitSet(8),
                [CapabilityID.FlipToStart] = true,
                [CapabilityID.OverDrive] = true
            };
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GetLegionDeviceSupportFeature read failed, returning Unknown features", ex);
        }

        return MachineInformation.FeatureData.Unknown;
    }

    private static async Task<IEnumerable<PowerModeState>> GetSupportedPowerModesAsync()
    {
        try
        {
            var powerModes = new List<PowerModeState>();

            var value = await WMI.LenovoOtherMethod.GetFeatureValueAsync(CapabilityID.SupportedPowerModes).ConfigureAwait(false);

            if (value.IsBitSet(0))
                powerModes.Add(PowerModeState.Quiet);
            if (value.IsBitSet(1))
                powerModes.Add(PowerModeState.Balance);
            if (value.IsBitSet(2))
                powerModes.Add(PowerModeState.Performance);
            if (value.IsBitSet(3))
                powerModes.Add(PowerModeState.Extreme);
            if (value.IsBitSet(16))
                powerModes.Add(PowerModeState.GodMode);

            return powerModes;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GetFeatureValue(SupportedPowerModes) read failed, falling back to GetSupportThermalMode", ex);
        }

        try
        {
            var powerModes = new List<PowerModeState>();

            var result = await WMI.LenovoOtherMethod.GetSupportThermalModeAsync().ConfigureAwait(false);

            if (result.IsBitSet(0))
                powerModes.Add(PowerModeState.Quiet);
            if (result.IsBitSet(1))
                powerModes.Add(PowerModeState.Balance);
            if (result.IsBitSet(2))
                powerModes.Add(PowerModeState.Performance);
            if (result.IsBitSet(3))
                powerModes.Add(PowerModeState.Extreme);
            if (result.IsBitSet(16))
                powerModes.Add(PowerModeState.GodMode);

            return powerModes;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GetSupportThermalMode read failed, returning empty power modes", ex);
        }

        return [];
    }

    private static async Task<int> GetSmartFanVersionAsync()
    {
        try
        {
            return await WMI.LenovoGameZoneData.IsSupportSmartFanAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"IsSupportSmartFan read failed, returning -1", ex);
        }

        return -1;
    }

    private static async Task<int> GetLegionZoneVersionAsync()
    {
        try
        {
            return await WMI.LenovoOtherMethod.GetFeatureValueAsync(CapabilityID.LegionZoneSupportVersion).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GetFeatureValue(LegionZoneSupportVersion) read failed, falling back to GetSupportLegionZoneVersion", ex);
        }

        try
        {
            return await WMI.LenovoOtherMethod.GetSupportLegionZoneVersionAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GetSupportLegionZoneVersion read failed, returning -1", ex);
        }

        return -1;
    }

    private static unsafe (bool status, bool connectivity) GetAlwaysOnAcStatus()
    {
        var capabilities = new SYSTEM_POWER_CAPABILITIES();
        var result = PInvoke.CallNtPowerInformation(POWER_INFORMATION_LEVEL.SystemPowerCapabilities,
            null,
            0,
            &capabilities,
            (uint)Marshal.SizeOf<SYSTEM_POWER_CAPABILITIES>());

        if (result.SeverityCode == NTSTATUS.Severity.Success)
            return (false, false);

        return (capabilities.AoAc, capabilities.AoAcConnectivitySupported);
    }

    private static bool GetSupportsExtremeMode(IEnumerable<PowerModeState> supportedPowerModes, int smartFanVersion, int legionZoneVersion)
    {
        if (!supportedPowerModes.Contains(PowerModeState.Extreme))
            return false;

        return smartFanVersion is 6 or 7 or 8 || legionZoneVersion is 3 or 4 or 5;
    }

    private static bool GetSupportsGodModeV1(IEnumerable<PowerModeState> supportedPowerModes, int smartFanVersion, int legionZoneVersion, BiosVersion? biosVersion)
    {
        if (!supportedPowerModes.Contains(PowerModeState.GodMode))
            return false;

        var affectedBiosVersions = new BiosVersion[]
        {
            new("G9CN", 24),
            new("GKCN", 46),
            new("H1CN", 39),
            new("HACN", 31),
            new("HHCN", 20)
        };

        if (affectedBiosVersions.Any(bv => biosVersion?.IsLowerThan(bv) ?? false))
            return false;

        return smartFanVersion is 4 or 5 || legionZoneVersion is 1 or 2;
    }

    private static bool GetSupportsGodModeV2(IEnumerable<PowerModeState> supportedPowerModes, int smartFanVersion, int legionZoneVersion)
    {
        if (!supportedPowerModes.Contains(PowerModeState.GodMode))
            return false;

        return smartFanVersion is 6 or 7 || legionZoneVersion is 3 or 4;
    }

    private static bool GetSupportsGodModeV3(IEnumerable<PowerModeState> supportedPowerModes, int smartFanVersion, int legionZoneVersion, int generation, string model, string machineType)
    {
        if (!supportedPowerModes.Contains(PowerModeState.GodMode))
            return false;

        var affectedSeries = new[]
        {
            LegionSeries.Legion_5,
            LegionSeries.Legion_7
        };

        var affectedModels = new[]
        {
            "Legion 5",
            "Legion 7",
            "Legion Pro 5 16IAX10H",
            "LOQ",
            "Y7000",
            "R7000"
        };

        var isAffectedSeries = affectedSeries.Any(series => GetLegionSeries(model, machineType) == series);
        var isAffectedModel = affectedModels.Any(model.Contains);
        var isSupportedVersion = smartFanVersion is 8 or 9 || legionZoneVersion is 5 or 6;

        return (isAffectedSeries || isAffectedModel) && isSupportedVersion && generation >= 10;
    }

    private static bool GetSupportsGodModeV4(IEnumerable<PowerModeState> supportedPowerModes, int smartFanVersion, int legionZoneVersion)
    {
        if (!supportedPowerModes.Contains(PowerModeState.GodMode))
            return false;

        return smartFanVersion is 8 or 9 || legionZoneVersion is 5 or 6;
    }

    private static async Task<bool> GetSupportsGSyncAsync()
    {
        try
        {
            return await WMI.LenovoGameZoneData.IsSupportGSyncAsync().ConfigureAwait(false) > 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> GetSupportsIGPUModeAsync()
    {
        try
        {
            return await WMI.LenovoGameZoneData.IsSupportIGPUModeAsync().ConfigureAwait(false) > 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> GetSupportsAIModeAsync()
    {
        try
        {
            await WMI.LenovoGameZoneData.GetIntelligentSubModeAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool GetSupportBootLogoChange(int smartFanVersion) => smartFanVersion < 8;

    private static bool GetSupportITSMode(string model)
    {
        var lower = model.ToLowerInvariant();

        if (lower.Contains("IdeaPad Gaming".ToLowerInvariant()))
            return false;

        return lower.Contains("IdeaPad".ToLowerInvariant())
            || lower.Contains("ThinkBook".ToLowerInvariant())
            || lower.Contains("Lenovo Slim".ToLowerInvariant());
    }

    private static int GetMachineGeneration(string model)
    {
        var platformMatch = Regex.Match(model, @"(?<=[A-Z]{3})(?<gen>\d{1,2})", RegexOptions.IgnoreCase);
        if (platformMatch.Success)
            return int.Parse(platformMatch.Groups["gen"].Value);

        var generationMatch = Regex.Match(model, @"g(?<gen>\d+)", RegexOptions.IgnoreCase);
        if (generationMatch.Success)
            return int.Parse(generationMatch.Groups["gen"].Value);

        var matches = Regex.Matches(model, @"(?<!\d)\d{1,2}(?!\d)");
        foreach (Match match in matches)
        {
            var value = int.Parse(match.Value);
            if (value >= 14 && value <= 18)
                continue;

            return value;
        }

        return 0;
    }

    private static LegionSeries GetLegionSeries(string model, string machineType)
    {
        if (MachineTypeMap.TryGetValue(machineType, out var series))
            return series;

        foreach (var (keyword, legionSeries) in ModelKeywordMap)
        {
            if (model.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return legionSeries;
        }

        return LegionSeries.Unknown;
    }

    private static bool GetHasQuietToPerformanceModeSwitchingBug(BiosVersion? biosVersion)
    {
        var affectedBiosVersions = new BiosVersion[]
        {
            new("J2CN", null)
        };

        return affectedBiosVersions.Any(bv => biosVersion?.IsHigherOrEqualThan(bv) ?? false);
    }

    private static bool GetHasGodModeToOtherModeSwitchingBug(BiosVersion? biosVersion)
    {
        var affectedBiosVersions = new BiosVersion[]
        {
            new("K1CN", null)
        };

        return affectedBiosVersions.Any(bv => biosVersion?.IsHigherOrEqualThan(bv) ?? false);
    }

    private static bool GetHasReapplyParameterIssue(string? machineModel, string machineType)
    {
        if (string.IsNullOrEmpty(machineModel))
            return false;

        var affectedSeries = new[]
        {
            LegionSeries.Legion_5,
            LegionSeries.Legion_7,
            LegionSeries.Legion_9,
        };

        return affectedSeries.Any(series => GetLegionSeries(machineModel, machineType) == series);
    }

    private static bool GetHasSpectrumProfileSwitchingBug(string? machineModel, string machineType)
    {
        if (string.IsNullOrEmpty(machineModel))
            return false;

        var affectedSeries = new[]
        {
            LegionSeries.Legion_5,
            LegionSeries.Legion_Pro_5,
        };

        var affectedModels = new List<string>
        {
            "16IRX10",
            "16IAX10",
            "16IAX10H",
            "15IRX10",
            "15AHP10"
        };

        var isAffectedModel = affectedModels.Any(model => machineModel.Contains(model, StringComparison.OrdinalIgnoreCase));
        var isAffectedSeries = affectedSeries.Any(series => GetLegionSeries(machineModel, machineType) == series);

        return isAffectedModel && isAffectedSeries;
    }

    private static bool GetIsExcludedFromLenovoLighting(BiosVersion? biosVersion, int generation, LegionSeries series)
    {
        if (series == LegionSeries.Legion_7 && generation == 6)
            return true;

        var affectedBiosVersions = new BiosVersion[]
        {
            new("GKCN", 54)
        };

        return affectedBiosVersions.Any(bv => biosVersion?.IsLowerThan(bv) ?? false);
    }

    private static bool GetIsExcludedFromPanelLenovoLighting(string machineType, string model)
    {
        (string machineType, string model)[] excludedModels =
        [
            ("82JH", "15ITH6H"),
            ("82JK", "15ITH6"),
            ("82JM", "17ITH6H"),
            ("82JN", "17ITH6"),
            ("82JU", "15ACH6H"),
            ("82JW", "15ACH6"),
            ("82JY", "17ACH6H"),
            ("82K0", "17ACH6"),
            ("82K1", "15IHU6"),
            ("82K2", "15ACH6"),
            ("82NW", "15ACH6A")
        ];

        return excludedModels.Where(m =>
        {
            var result = machineType.Contains(m.machineType);
            result &= model.Contains(m.model);
            return result;
        }).Any();
    }

    private static bool GetHasAlternativeFullSpectrumLayout(string machineType)
    {
        var machineTypes = new[]
        {
            "83G0", // Gen 9
            "83AG"  // Gen 8
        };
        return machineTypes.Contains(machineType);
    }

    public static bool IsLegion(LegionSeries series)
    {
        return series switch
        {
            LegionSeries.Legion_5 => true,
            LegionSeries.Legion_Pro_5 => true,
            LegionSeries.Lenovo_Slim => true,
            LegionSeries.Legion_Slim_5 => true,
            LegionSeries.Legion_7 => true,
            LegionSeries.Legion_Pro_7 => true,
            LegionSeries.Legion_9 => true,
            LegionSeries.Legion_Go => true,
            LegionSeries.LOQ => true,
            LegionSeries.Legion_Legacy => true,
            _ => false
        };
    }

    public static bool GetIsOverdriverSupported()
    {
        var generation = _machineInformation?.Generation;
        var series = _machineInformation?.LegionSeries;

        return series is not (LegionSeries.Legion_7 or LegionSeries.Legion_Pro_7) || generation < 10;
    }
}
