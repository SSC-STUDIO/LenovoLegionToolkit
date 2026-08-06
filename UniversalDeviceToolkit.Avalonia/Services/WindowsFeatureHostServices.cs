#if WINDOWS

using System.Globalization;
using System.Reflection;
using System.IO;
using Avalonia.Controls;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Abstractions.Macro;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Pipeline;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Lib.Automation.Serialization;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Features.FlipToStart;
using UniversalDeviceToolkit.Lib.Features.Hybrid;
using UniversalDeviceToolkit.Lib.Features.InstantBoot;
using UniversalDeviceToolkit.Lib.Features.OverDrive;
using UniversalDeviceToolkit.Lib.Features.PanelLogo;
using UniversalDeviceToolkit.Lib.Features.WhiteKeyboardBacklight;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.PackageDownloader;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Listeners;
using LibResource = UniversalDeviceToolkit.Lib.Resources.Resource;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Bridges Avalonia feature routes to the same Windows services used by the WPF host.
/// The bridge is optional: if the full service container cannot be created, callers retain
/// the read-only device-adapter state instead of preventing the shell from starting.
/// </summary>
internal sealed class WindowsFeatureHostServices
{
    private readonly IKeyboardBacklightDetectionService _keyboard;
    private readonly SpectrumKeyboardBacklightController? _spectrum;
    private readonly RGBKeyboardBacklightController? _rgb;
    private readonly IMacroController _macro;
    private readonly AutomationProcessor _automation;
    private readonly IPluginManager _plugins;
    private readonly WindowsOptimizationService? _optimization;
    private readonly INetworkAccelerationService? _networkAcceleration;
    private readonly INetworkDiagnosticsService? _networkDiagnostics;
    private readonly PackageDownloaderFactory? _packageDownloaderFactory;
    private readonly PackageDownloaderSettings? _packageDownloaderSettings;
    private IPackageDownloader? _driverDownloader;
    private List<Package> _driverPackages = [];
    private readonly object _driverLock = new();
    private readonly SemaphoreSlim _automationInitializationLock = new(1, 1);
    private readonly object _macroRecordingLock = new();
    private readonly HashSet<string> _selectedCleanupActions;
    private readonly ApplicationSettings? _applicationSettings;
    private readonly DashboardSettings? _dashboardSettings;
    private readonly PowerModeFeature? _powerMode;
    private readonly BatteryFeature? _battery;
    private readonly BatteryNightChargeFeature? _batteryNightCharge;
    private readonly AlwaysOnUSBFeature? _alwaysOnUsb;
    private readonly InstantBootFeature? _instantBoot;
    private readonly FlipToStartFeature? _flipToStart;
    private readonly ITSModeFeature? _itsMode;
    private readonly HybridModeFeature? _hybridMode;
    private readonly HDRFeature? _hdr;
    private readonly OverDriveFeature? _overDrive;
    private readonly MicrophoneFeature? _microphone;
    private readonly FnLockFeature? _fnLock;
    private readonly WinKeyFeature? _winKey;
    private readonly TouchpadLockFeature? _touchpadLock;
    private readonly PortsBacklightFeature? _portsBacklight;
    private readonly PanelLogoBacklightFeature? _panelLogoBacklight;
    private readonly WhiteKeyboardBacklightFeature? _whiteKeyboardBacklight;
    private readonly OneLevelWhiteKeyboardBacklightFeature? _oneLevelWhiteKeyboardBacklight;
    private readonly ResolutionFeature? _resolution;
    private readonly RefreshRateFeature? _refreshRate;
    private readonly DpiScaleFeature? _dpiScale;
    private readonly GPUController? _gpuController;
    private readonly GPUOverclockController? _gpuOverclockController;
    private readonly NativeWindowsMessageListener? _nativeWindowsMessageListener;
    private long _estimatedCleanupSize;
    private ulong? _macroRecordingKey;
    private List<MacroEvent>? _macroRecordingEvents;
    private bool _automationInitialized;
    private static readonly ulong[] MacroKeys = [0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69];

    private WindowsFeatureHostServices(
        IKeyboardBacklightDetectionService keyboard,
        SpectrumKeyboardBacklightController? spectrum,
        RGBKeyboardBacklightController? rgb,
        IMacroController macro,
        AutomationProcessor automation,
        IPluginManager plugins,
        WindowsOptimizationService? optimization,
        INetworkAccelerationService? networkAcceleration,
        INetworkDiagnosticsService? networkDiagnostics,
        PackageDownloaderFactory? packageDownloaderFactory,
        PackageDownloaderSettings? packageDownloaderSettings)
    {
        _keyboard = keyboard;
        _spectrum = spectrum;
        _rgb = rgb;
        _macro = macro;
        _automation = automation;
        _plugins = plugins;
        _optimization = optimization;
        _networkAcceleration = networkAcceleration;
        _networkDiagnostics = networkDiagnostics;
        _packageDownloaderFactory = packageDownloaderFactory;
        _packageDownloaderSettings = packageDownloaderSettings;
        _applicationSettings = IoCContainer.TryResolve<ApplicationSettings>();
        _dashboardSettings = IoCContainer.TryResolve<DashboardSettings>();
        _powerMode = IoCContainer.TryResolve<PowerModeFeature>();
        _battery = IoCContainer.TryResolve<BatteryFeature>();
        _batteryNightCharge = IoCContainer.TryResolve<BatteryNightChargeFeature>();
        _alwaysOnUsb = IoCContainer.TryResolve<AlwaysOnUSBFeature>();
        _instantBoot = IoCContainer.TryResolve<InstantBootFeature>();
        _flipToStart = IoCContainer.TryResolve<FlipToStartFeature>();
        _itsMode = IoCContainer.TryResolve<ITSModeFeature>();
        _hybridMode = IoCContainer.TryResolve<HybridModeFeature>();
        _hdr = IoCContainer.TryResolve<HDRFeature>();
        _overDrive = IoCContainer.TryResolve<OverDriveFeature>();
        _microphone = IoCContainer.TryResolve<MicrophoneFeature>();
        _fnLock = IoCContainer.TryResolve<FnLockFeature>();
        _winKey = IoCContainer.TryResolve<WinKeyFeature>();
        _touchpadLock = IoCContainer.TryResolve<TouchpadLockFeature>();
        _portsBacklight = IoCContainer.TryResolve<PortsBacklightFeature>();
        _panelLogoBacklight = IoCContainer.TryResolve<PanelLogoBacklightFeature>();
        _whiteKeyboardBacklight = IoCContainer.TryResolve<WhiteKeyboardBacklightFeature>();
        _oneLevelWhiteKeyboardBacklight = IoCContainer.TryResolve<OneLevelWhiteKeyboardBacklightFeature>();
        _resolution = IoCContainer.TryResolve<ResolutionFeature>();
        _refreshRate = IoCContainer.TryResolve<RefreshRateFeature>();
        _dpiScale = IoCContainer.TryResolve<DpiScaleFeature>();
        _gpuController = IoCContainer.TryResolve<GPUController>();
        _gpuOverclockController = IoCContainer.TryResolve<GPUOverclockController>();
        _nativeWindowsMessageListener = IoCContainer.TryResolve<NativeWindowsMessageListener>();
        _selectedCleanupActions = new HashSet<string>(
            _applicationSettings?.Store.SelectedCleanupActions ?? [],
            StringComparer.OrdinalIgnoreCase);

        if (_macro is MacroController macroController)
        {
            macroController.RecorderReceived += MacroController_RecorderReceived;
            macroController.RecorderStopped += MacroController_RecorderStopped;
        }
    }

    public static WindowsFeatureHostServices? TryCreate()
    {
        try
        {
            return new WindowsFeatureHostServices(
                IoCContainer.Resolve<IKeyboardBacklightDetectionService>(),
                IoCContainer.TryResolve<SpectrumKeyboardBacklightController>(),
                IoCContainer.TryResolve<RGBKeyboardBacklightController>(),
                IoCContainer.Resolve<IMacroController>(),
                IoCContainer.Resolve<AutomationProcessor>(),
                IoCContainer.Resolve<IPluginManager>(),
                IoCContainer.TryResolve<WindowsOptimizationService>(),
                IoCContainer.TryResolve<INetworkAccelerationService>(),
                IoCContainer.TryResolve<INetworkDiagnosticsService>(),
                IoCContainer.TryResolve<PackageDownloaderFactory>(),
                IoCContainer.TryResolve<PackageDownloaderSettings>());
        }
        catch
        {
            return null;
        }
    }

    public async Task<FeaturePageState> GetStateAsync(string routeKey)
    {
        return routeKey switch
        {
            "Keyboard" => await GetKeyboardStateAsync(),
            "Macro" => GetMacroState(),
            "Actions" => await GetAutomationStateAsync(),
            "PluginExtensions" => GetPluginState(),
            "WindowsOptimization" => await GetOptimizationStateAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(routeKey), routeKey, "Unknown feature route."),
        };
    }

    public Task<DashboardLayoutState> GetDashboardLayoutAsync()
    {
        if (_dashboardSettings is null)
        {
            return Task.FromResult(new DashboardLayoutState(
                true,
                1,
                DashboardGroup.DefaultGroups.Select(ToDashboardGroupState).ToArray()));
        }

        var store = _dashboardSettings.Store;
        var groups = (store.Groups ?? DashboardGroup.DefaultGroups)
            .Select(ToDashboardGroupState)
            .ToArray();
        return Task.FromResult(new DashboardLayoutState(
            store.ShowSensors,
            Math.Clamp(store.SensorsRefreshIntervalSeconds, 1, 60),
            groups));
    }

    public Task<bool> SaveDashboardLayoutAsync(DashboardLayoutState layout)
    {
        if (_dashboardSettings is null || layout is null)
            return Task.FromResult(false);

        var groups = new List<DashboardGroup>();
        foreach (var group in layout.Groups ?? [])
        {
            if (!Enum.TryParse<DashboardGroupType>(group.Type, true, out var type))
                continue;

            var items = (group.Items ?? [])
                .Where(item => Enum.TryParse<DashboardItem>(item, true, out _))
                .Select(item => Enum.Parse<DashboardItem>(item, true))
                .Distinct()
                .ToArray();
            groups.Add(new DashboardGroup(type, group.CustomName, items));
        }

        if (groups.Count == 0)
            return Task.FromResult(false);

        _dashboardSettings.Store.ShowSensors = layout.ShowSensors;
        _dashboardSettings.Store.SensorsRefreshIntervalSeconds = Math.Clamp(
            layout.SensorsRefreshIntervalSeconds,
            1,
            60);
        _dashboardSettings.Store.Groups = groups.ToArray();
        _dashboardSettings.SynchronizeStore();
        return Task.FromResult(true);
    }

    private static DashboardGroupState ToDashboardGroupState(DashboardGroup group) =>
        new(
            group.Type.ToString(),
            group.CustomName,
            group.Items.Select(item => item.ToString()).ToArray());

    public async Task<IReadOnlyList<DashboardItemState>> GetDashboardItemStatesAsync(
        IReadOnlyList<string> itemIdentifiers)
    {
        var states = new List<DashboardItemState>();
        foreach (var identifier in itemIdentifiers
                     .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
                     .Where(identifier => !DashboardItemStateRouting.IsDedicatedControl(identifier))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            states.Add(await ReadDashboardItemStateAsync(identifier).ConfigureAwait(false));
        }

        return states;
    }

    public async Task<bool> SetDashboardItemStateAsync(string itemIdentifier, string state)
    {
        return itemIdentifier.ToLowerInvariant() switch
        {
            "powermode" => await SetDashboardFeatureStateAsync(_powerMode, state).ConfigureAwait(false),
            "batterymode" => await SetDashboardFeatureStateAsync(_battery, state).ConfigureAwait(false),
            "batterynightchargemode" => await SetDashboardFeatureStateAsync(_batteryNightCharge, state).ConfigureAwait(false),
            "alwaysonusb" => await SetDashboardFeatureStateAsync(_alwaysOnUsb, state).ConfigureAwait(false),
            "instantboot" => await SetDashboardFeatureStateAsync(_instantBoot, state).ConfigureAwait(false),
            "fliptostart" => await SetDashboardFeatureStateAsync(_flipToStart, state).ConfigureAwait(false),
            "itsmode" => await SetDashboardFeatureStateAsync(_itsMode, state).ConfigureAwait(false),
            "hybridmode" => await SetDashboardFeatureStateAsync(_hybridMode, state).ConfigureAwait(false),
            "hdr" => await SetDashboardFeatureStateAsync(_hdr, state).ConfigureAwait(false),
            "overdrive" => await SetDashboardFeatureStateAsync(_overDrive, state).ConfigureAwait(false),
            "microphone" => await SetDashboardFeatureStateAsync(_microphone, state).ConfigureAwait(false),
            "fnlock" => await SetDashboardFeatureStateAsync(_fnLock, state).ConfigureAwait(false),
            "winkeylock" => await SetDashboardFeatureStateAsync(_winKey, state).ConfigureAwait(false),
            "touchpadlock" => await SetDashboardFeatureStateAsync(_touchpadLock, state).ConfigureAwait(false),
            "portsbacklight" => await SetDashboardFeatureStateAsync(_portsBacklight, state).ConfigureAwait(false),
            "panellogobacklight" => await SetDashboardFeatureStateAsync(_panelLogoBacklight, state).ConfigureAwait(false),
            "whitekeyboardbacklight" => await SetDashboardFeatureStateAsync(_whiteKeyboardBacklight, state).ConfigureAwait(false),
            "onelevelwhitekeyboardbacklight" => await SetDashboardFeatureStateAsync(_oneLevelWhiteKeyboardBacklight, state).ConfigureAwait(false),
            "resolution" => await SetResolutionStateAsync(state).ConfigureAwait(false),
            "refreshrate" => await SetRefreshRateStateAsync(state).ConfigureAwait(false),
            "dpiscale" => await SetDpiScaleStateAsync(state).ConfigureAwait(false),
            _ => false,
        };
    }

    public async Task<DiscreteGpuState> GetDiscreteGpuStateAsync()
    {
        if (_gpuController is null)
            return UnavailableGpuState("The GPU controller is unavailable.");

        try
        {
            if (!await _gpuController.IsSupportedAsync().ConfigureAwait(false))
                return UnavailableGpuState("Discrete GPU monitoring is not supported on this device.");

            if (!_gpuController.IsStarted)
                await _gpuController.StartAsync().ConfigureAwait(false);

            var status = await _gpuController.RefreshNowAsync().ConfigureAwait(false);
            var canKill = status.State is GPUState.Active && status.ProcessCount > 0;
            var canRestart = status.State is GPUState.Active or GPUState.Inactive;
            return new DiscreteGpuState(
                true,
                GetGpuStatusText(status.State),
                status.PerformanceState ?? AvaloniaLocalization.GetString(
                    "DiscreteGPUControl_PerformanceState_Unknown",
                    "Unknown"),
                status.ProcessCount,
                canKill,
                canRestart);
        }
        catch (Exception ex)
        {
            return UnavailableGpuState(ex.Message);
        }
    }

    public async Task<bool> KillDiscreteGpuProcessesAsync()
    {
        if (_gpuController is null)
            return false;

        try
        {
            await _gpuController.KillGPUProcessesAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestartDiscreteGpuAsync()
    {
        if (_gpuController is null)
            return false;

        try
        {
            await _gpuController.RestartGPUAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TurnOffMonitorsAsync()
    {
        if (_nativeWindowsMessageListener is null)
            return false;

        try
        {
            await _nativeWindowsMessageListener.TurnOffMonitorAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<GpuOverclockState> GetGpuOverclockStateAsync()
    {
        if (_gpuOverclockController is null)
            return UnavailableOverclockState("The GPU overclock controller is unavailable.");

        try
        {
            if (!await _gpuOverclockController.IsSupportedAsync().ConfigureAwait(false))
                return UnavailableOverclockState("GPU overclocking is not supported on this device.");

            var (enabled, info) = _gpuOverclockController.GetState();
            return new GpuOverclockState(
                true,
                enabled,
                info.CoreDeltaMhz,
                info.MemoryDeltaMhz,
                GPUOverclockController.GetMaxCoreDeltaMhz(),
                GPUOverclockController.GetMaxMemoryDeltaMhz());
        }
        catch (Exception ex)
        {
            return UnavailableOverclockState(ex.Message);
        }
    }

    public async Task<bool> SetGpuOverclockAsync(bool enabled, int coreDeltaMhz, int memoryDeltaMhz)
    {
        if (_gpuOverclockController is null)
            return false;

        try
        {
            if (!await _gpuOverclockController.IsSupportedAsync().ConfigureAwait(false))
                return false;

            var coreLimit = GPUOverclockController.GetMaxCoreDeltaMhz();
            var memoryLimit = GPUOverclockController.GetMaxMemoryDeltaMhz();
            var info = new GPUOverclockInfo(
                Math.Clamp(coreDeltaMhz, -coreLimit, coreLimit),
                Math.Clamp(memoryDeltaMhz, -memoryLimit, memoryLimit));
            _gpuOverclockController.SaveState(enabled, info);
            await _gpuOverclockController.ApplyStateAsync(true).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static DiscreteGpuState UnavailableGpuState(string error) =>
        new(
            false,
            AvaloniaLocalization.GetString("Dashboard_Status_Unavailable", "Unavailable"),
            string.Empty,
            0,
            false,
            false,
            error);

    private static GpuOverclockState UnavailableOverclockState(string error) =>
        new(false, false, 0, 0, 0, 0, error);

    private static string GetGpuStatusText(GPUState state) => state switch
    {
        GPUState.Active => AvaloniaLocalization.GetString("Active", "Active"),
        GPUState.MonitorConnected => AvaloniaLocalization.GetString(
            "DiscreteGPUControl_MonitorConnected", "External monitor connected"),
        GPUState.Inactive => AvaloniaLocalization.GetString("Inactive", "Inactive"),
        GPUState.PoweredOff => AvaloniaLocalization.GetString("PoweredOff", "Powered off"),
        GPUState.NvidiaGpuNotFound => AvaloniaLocalization.GetString(
            "Dashboard_Status_Unavailable", "Unavailable"),
        _ => AvaloniaLocalization.GetString(
            "DiscreteGPUControl_PerformanceState_Unknown", "Unknown"),
    };

    private async Task<DashboardItemState> ReadDashboardItemStateAsync(string identifier) =>
        identifier.ToLowerInvariant() switch
        {
            "powermode" => await ReadDashboardFeatureStateAsync(identifier, _powerMode).ConfigureAwait(false),
            "batterymode" => await ReadDashboardFeatureStateAsync(identifier, _battery).ConfigureAwait(false),
            "batterynightchargemode" => await ReadDashboardFeatureStateAsync(identifier, _batteryNightCharge).ConfigureAwait(false),
            "alwaysonusb" => await ReadDashboardFeatureStateAsync(identifier, _alwaysOnUsb).ConfigureAwait(false),
            "instantboot" => await ReadDashboardFeatureStateAsync(identifier, _instantBoot).ConfigureAwait(false),
            "fliptostart" => await ReadDashboardFeatureStateAsync(identifier, _flipToStart).ConfigureAwait(false),
            "itsmode" => await ReadDashboardFeatureStateAsync(identifier, _itsMode).ConfigureAwait(false),
            "hybridmode" => await ReadDashboardFeatureStateAsync(identifier, _hybridMode).ConfigureAwait(false),
            "hdr" => await ReadDashboardFeatureStateAsync(identifier, _hdr).ConfigureAwait(false),
            "overdrive" => await ReadDashboardFeatureStateAsync(identifier, _overDrive).ConfigureAwait(false),
            "microphone" => await ReadDashboardFeatureStateAsync(identifier, _microphone).ConfigureAwait(false),
            "fnlock" => await ReadDashboardFeatureStateAsync(identifier, _fnLock).ConfigureAwait(false),
            "winkeylock" => await ReadDashboardFeatureStateAsync(identifier, _winKey).ConfigureAwait(false),
            "touchpadlock" => await ReadDashboardFeatureStateAsync(identifier, _touchpadLock).ConfigureAwait(false),
            "portsbacklight" => await ReadDashboardFeatureStateAsync(identifier, _portsBacklight).ConfigureAwait(false),
            "panellogobacklight" => await ReadDashboardFeatureStateAsync(identifier, _panelLogoBacklight).ConfigureAwait(false),
            "whitekeyboardbacklight" => await ReadDashboardFeatureStateAsync(identifier, _whiteKeyboardBacklight).ConfigureAwait(false),
            "onelevelwhitekeyboardbacklight" => await ReadDashboardFeatureStateAsync(identifier, _oneLevelWhiteKeyboardBacklight).ConfigureAwait(false),
            "resolution" => await ReadDashboardValueStateAsync(identifier, _resolution).ConfigureAwait(false),
            "refreshrate" => await ReadDashboardValueStateAsync(identifier, _refreshRate).ConfigureAwait(false),
            "dpiscale" => await ReadDashboardValueStateAsync(identifier, _dpiScale).ConfigureAwait(false),
            _ => new DashboardItemState(
                identifier,
                false,
                null,
                Array.Empty<string>(),
                AvaloniaLocalization.GetString("Dashboard_Status_Unavailable", "Unavailable")),
        };

    private static async Task<DashboardItemState> ReadDashboardFeatureStateAsync<T>(
        string identifier,
        IFeature<T>? feature)
        where T : struct, Enum
    {
        if (feature is null)
        {
            return new DashboardItemState(
                identifier,
                false,
                null,
                Array.Empty<string>(),
                "The dashboard service is unavailable.");
        }

        try
        {
            if (!await feature.IsSupportedAsync().ConfigureAwait(false))
            {
                return new DashboardItemState(
                    identifier,
                    false,
                    null,
                    Array.Empty<string>(),
                    "This control is not supported on the current device.");
            }

            var current = await feature.GetStateAsync().ConfigureAwait(false);
            var options = await feature.GetAllStatesAsync().ConfigureAwait(false);
            return new DashboardItemState(
                identifier,
                true,
                current.ToString(),
                options.Select(option => option.ToString()).ToArray());
        }
        catch (Exception ex)
        {
            return new DashboardItemState(
                identifier,
                false,
                null,
                Array.Empty<string>(),
                ex.Message);
        }
    }

    private static async Task<bool> SetDashboardFeatureStateAsync<T>(
        IFeature<T>? feature,
        string state)
        where T : struct, Enum
    {
        if (feature is null || !Enum.TryParse<T>(state, true, out var parsed))
            return false;

        try
        {
            await feature.SetStateAsync(parsed).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<DashboardItemState> ReadDashboardValueStateAsync<T>(
        string identifier,
        IFeature<T>? feature)
        where T : struct
    {
        if (feature is null)
        {
            return new DashboardItemState(
                identifier,
                false,
                null,
                Array.Empty<string>(),
                "The dashboard service is unavailable.");
        }

        try
        {
            if (!await feature.IsSupportedAsync().ConfigureAwait(false))
            {
                return new DashboardItemState(
                    identifier,
                    false,
                    null,
                    Array.Empty<string>(),
                    "This control is not supported on the current device.");
            }

            var current = await feature.GetStateAsync().ConfigureAwait(false);
            var options = await feature.GetAllStatesAsync().ConfigureAwait(false);
            return new DashboardItemState(
                identifier,
                true,
                SerializeDashboardValue(current),
                options.Select(SerializeDashboardValue).ToArray());
        }
        catch (Exception ex)
        {
            return new DashboardItemState(identifier, false, null, Array.Empty<string>(), ex.Message);
        }
    }

    private async Task<bool> SetResolutionStateAsync(string state)
    {
        if (_resolution is null || !TryParseResolution(state, out var resolution))
            return false;

        try
        {
            await _resolution.SetStateAsync(resolution).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> SetRefreshRateStateAsync(string state)
    {
        if (_refreshRate is null || !TryParseInteger(state, out var frequency))
            return false;

        try
        {
            await _refreshRate.SetStateAsync(new RefreshRate(frequency)).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> SetDpiScaleStateAsync(string state)
    {
        if (_dpiScale is null || !TryParseInteger(state, out var scale))
            return false;

        try
        {
            await _dpiScale.SetStateAsync(new DpiScale(scale)).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string SerializeDashboardValue<T>(T value)
        where T : struct => value switch
        {
            Resolution resolution => resolution.ToString(),
            RefreshRate refreshRate => refreshRate.Frequency.ToString(CultureInfo.InvariantCulture),
            DpiScale dpiScale => dpiScale.Scale.ToString(CultureInfo.InvariantCulture),
            IDisplayName displayName => displayName.DisplayName,
            _ => value.ToString() ?? string.Empty,
        };

    private static bool TryParseResolution(string value, out Resolution resolution)
    {
        resolution = default;
        var parts = value.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height)
            && width > 0
            && height > 0
            && SetResolution(width, height, ref resolution);
    }

    private static bool SetResolution(int width, int height, ref Resolution resolution)
    {
        resolution = new Resolution(width, height);
        return true;
    }

    private static bool TryParseInteger(string value, out int result)
    {
        var digits = new string(value.Where(character => char.IsDigit(character) || character == '-').ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    public Task<IReadOnlyList<CustomCleanupRuleItem>> GetCustomCleanupRulesAsync()
    {
        if (_applicationSettings is null)
            return Task.FromResult<IReadOnlyList<CustomCleanupRuleItem>>([]);

        var rules = (_applicationSettings.Store.CustomCleanupRules ?? [])
            .Where(rule => rule is not null)
            .Select(rule => new CustomCleanupRuleItem(
                rule.DirectoryPath ?? string.Empty,
                (rule.Extensions ?? []).ToArray(),
                rule.Recursive))
            .ToArray();
        return Task.FromResult<IReadOnlyList<CustomCleanupRuleItem>>(rules);
    }

    public Task<bool> SaveCustomCleanupRulesAsync(IReadOnlyList<CustomCleanupRuleItem> rules)
    {
        if (_applicationSettings is null || rules is null)
            return Task.FromResult(false);

        var normalized = new List<CustomCleanupRule>();
        foreach (var rule in rules)
        {
            if (rule is null || string.IsNullOrWhiteSpace(rule.DirectoryPath))
                continue;

            var extensions = (rule.Extensions ?? [])
                .SelectMany(value => (value ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            normalized.Add(new CustomCleanupRule
            {
                DirectoryPath = rule.DirectoryPath.Trim(),
                Extensions = extensions,
                Recursive = rule.Recursive,
            });
        }

        _applicationSettings.Store.CustomCleanupRules = normalized;
        _applicationSettings.SynchronizeStore();
        return Task.FromResult(true);
    }

    public async Task<bool> SetActionAsync(string routeKey, string actionKey, bool isSelected)
    {
        try
        {
            return await SetActionCoreAsync(routeKey, actionKey, isSelected).ConfigureAwait(false);
        }
        catch
        {
            // Feature cards are user-triggered controls. Report a rejected action
            // to the page so it can keep the current state and show its tooltip;
            // never surface a host-service exception from an async UI event.
            return false;
        }
    }

    public async Task<bool> ImportPluginAsync(string zipFilePath)
    {
        if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
            return false;

        try
        {
            var installer = new PluginInstallationService(_plugins);
            var imported = await installer.ExtractAndInstallPluginAsync(
                zipFilePath,
                PluginPaths.GetPluginsDirectory()).ConfigureAwait(false);
            if (imported)
                await _plugins.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);
            return imported;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PluginCatalogState> GetPluginCatalogAsync(bool forceRefresh = false)
    {
        var manifests = new Dictionary<string, PluginManifest>(StringComparer.OrdinalIgnoreCase);
        var status = AvaloniaLocalization.GetString(
            "PluginExtensionsPage_StoreUnavailableMessage",
            "Installed plugins remain available. Retry when the network is back.");
        var storeAvailable = false;

        var repository = IoCContainer.TryResolve<PluginRepositoryService>();
        if (repository is not null)
        {
            try
            {
                foreach (var manifest in await repository.FetchAvailablePluginsAsync(forceRefresh).ConfigureAwait(false))
                {
                    if (!string.IsNullOrWhiteSpace(manifest.Id))
                        manifests[manifest.Id] = manifest;
                }

                storeAvailable = true;
                status = AvaloniaLocalization.GetString("PluginExtensionsPage_Available", "Available");
            }
            catch
            {
                // Keep installed entries usable when the online catalog is down.
            }
        }

        foreach (var plugin in _plugins.GetRegisteredPlugins())
        {
            if (manifests.ContainsKey(plugin.Id))
                continue;

            var metadata = _plugins.GetPluginMetadata(plugin.Id);
            manifests[plugin.Id] = new PluginManifest
            {
                Id = plugin.Id,
                Name = metadata?.GetDisplayName(LocalizationRuntime.CurrentCulture) ?? plugin.Name ?? plugin.Id,
                Description = metadata?.GetDisplayDescription(LocalizationRuntime.CurrentCulture) ?? plugin.Description ?? string.Empty,
                Version = metadata?.Version ?? "0.0.0",
                Author = metadata?.Author ?? string.Empty,
                IsSystemPlugin = metadata?.IsSystemPlugin == true || plugin.IsSystemPlugin,
                Tags = metadata?.GetDisplayTags(LocalizationRuntime.CurrentCulture).ToArray(),
            };
        }

        foreach (var pluginId in _plugins.GetInstalledPluginIds().Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (manifests.ContainsKey(pluginId))
                continue;

            var metadata = _plugins.GetPluginMetadata(pluginId);
            var installedManifest = PluginUiCapabilityResolver.ReadInstalledManifest(pluginId);
            manifests[pluginId] = installedManifest ?? new PluginManifest
            {
                Id = pluginId,
                Name = metadata?.GetDisplayName(LocalizationRuntime.CurrentCulture) ?? pluginId,
                Description = metadata?.GetDisplayDescription(LocalizationRuntime.CurrentCulture) ?? string.Empty,
                Version = metadata?.Version ?? "0.0.0",
                Author = metadata?.Author ?? string.Empty,
                IsSystemPlugin = metadata?.IsSystemPlugin == true,
                Tags = metadata?.GetDisplayTags(LocalizationRuntime.CurrentCulture).ToArray(),
            };
        }

        Dictionary<string, string> updates;
        try
        {
            updates = await _plugins.CheckForUpdatesAsync().ConfigureAwait(false);
        }
        catch
        {
            updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var items = manifests.Values
            .OrderBy(manifest => manifest.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(manifest =>
            {
                var installed = _plugins.IsInstalled(manifest.Id);
                var capabilities = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
                if (installed)
                {
                    capabilities = capabilities.Merge(PluginUiCapabilityResolver.ResolveFromInstalledManifest(manifest.Id));
                    var runtimePlugin = _plugins.GetRegisteredPlugins()
                        .FirstOrDefault(plugin => plugin.Id.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase));
                    if (runtimePlugin is not null)
                        capabilities = capabilities.Merge(ResolveRuntimePluginCapabilities(runtimePlugin));
                }

                return new PluginCatalogItem(
                    manifest.Id,
                    string.IsNullOrWhiteSpace(manifest.Name) ? manifest.Id : manifest.Name,
                    manifest.Description ?? string.Empty,
                    manifest.Details,
                    string.IsNullOrWhiteSpace(manifest.Version) ? "0.0.0" : manifest.Version,
                    manifest.Author ?? string.Empty,
                    installed,
                    manifest.IsSystemPlugin,
                    updates.TryGetValue(manifest.Id, out var update) ? update : null,
                    capabilities.SupportsSettingsPage,
                    capabilities.SupportsFeaturePage,
                    capabilities.SupportsOptimizationCategory,
                    manifest.Tags ?? Array.Empty<string>());
            })
            .ToArray();

        return new PluginCatalogState(storeAvailable || items.Length > 0, status, items);
    }

    private static PluginUiCapabilities ResolveRuntimePluginCapabilities(IPlugin plugin)
    {
        try
        {
            if (plugin is PluginBase pluginBase)
            {
                return new PluginUiCapabilities
                {
                    SupportsSettingsPage = pluginBase.GetSettingsPage() is not null,
                    SupportsFeaturePage = HasPluginFeaturePage(plugin),
                    SupportsOptimizationCategory = pluginBase.GetOptimizationCategory() is not null,
                };
            }

            var type = plugin.GetType();
            var getSettingsPage = type.GetMethod("GetSettingsPage", BindingFlags.Public | BindingFlags.Instance);
            var getOptimizationCategory = type.GetMethod("GetOptimizationCategory", BindingFlags.Public | BindingFlags.Instance);
            return new PluginUiCapabilities
            {
                SupportsSettingsPage = getSettingsPage?.Invoke(plugin, null) is not null,
                SupportsFeaturePage = HasPluginFeaturePage(plugin),
                SupportsOptimizationCategory = getOptimizationCategory?.Invoke(plugin, null) is not null,
            };
        }
        catch
        {
            return default;
        }
    }

    public async Task<bool> UpdatePluginAsync(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        var repository = IoCContainer.TryResolve<PluginRepositoryService>();
        if (repository is null)
            return false;

        try
        {
            var manifest = (await repository.FetchAvailablePluginsAsync().ConfigureAwait(false))
                .FirstOrDefault(item => item.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
            return manifest is not null
                && await repository.DownloadAndInstallPluginAsync(manifest).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> InstallPluginAsync(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        var repository = IoCContainer.TryResolve<PluginRepositoryService>();
        if (repository is null)
            return false;

        try
        {
            var manifest = (await repository.FetchAvailablePluginsAsync().ConfigureAwait(false))
                .FirstOrDefault(item => item.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
            if (manifest is null)
                return false;

            return await repository.DownloadAndInstallPluginAsync(manifest).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    public Task<NetworkAccelerationState> GetNetworkAccelerationStateAsync()
    {
        if (_networkAcceleration is null)
        {
            return Task.FromResult(new NetworkAccelerationState(
                false,
                false,
                false,
                false,
                "Off",
                "The network acceleration service is not initialized in this host.",
                0,
                Array.Empty<NetworkAccelerationGroupState>()));
        }

        var config = _networkAcceleration.Config;
        var groups = (config.DomainGroups ?? [])
            .Select(group => new NetworkAccelerationGroupState(
                group.Id,
                group.DisplayName,
                group.Description ?? string.Empty,
                group.Enabled,
                group.IsFavorite,
                (group.Domains?.Count ?? 0) + (group.SubItems?.Count ?? 0)))
            .ToArray();
        return Task.FromResult(new NetworkAccelerationState(
            true,
            _networkAcceleration.IsBackendReady,
            config.AccelerationEnabled,
            _networkAcceleration.IsRunning,
            config.Mode.ToString(),
            _networkAcceleration.StatusText,
            config.ListenPort,
            groups));
    }

    public async Task<bool> SetNetworkAccelerationEnabledAsync(bool enabled)
    {
        if (_networkAcceleration is null)
            return false;

        _networkAcceleration.Config.AccelerationEnabled = enabled;
        if (!enabled)
            await _networkAcceleration.StopAsync().ConfigureAwait(false);
        await _networkAcceleration.SaveConfigAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetNetworkAccelerationModeAsync(string mode)
    {
        if (_networkAcceleration is null
            || !Enum.TryParse<NetworkAccelerationMode>(mode, true, out var parsed))
            return false;

        _networkAcceleration.Config.Mode = parsed;
        await _networkAcceleration.SaveConfigAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetNetworkAccelerationGroupEnabledAsync(string groupId, bool enabled)
    {
        if (_networkAcceleration is null || string.IsNullOrWhiteSpace(groupId))
            return false;

        var group = _networkAcceleration.Config.DomainGroups?
            .FirstOrDefault(candidate => candidate.Id.Equals(groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
            return false;

        group.Enabled = enabled;
        await _networkAcceleration.SaveConfigAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> ToggleNetworkAccelerationAsync()
    {
        if (_networkAcceleration is null)
            return false;

        if (_networkAcceleration.IsRunning)
        {
            await _networkAcceleration.StopAsync().ConfigureAwait(false);
            return true;
        }

        return await _networkAcceleration.StartAsync().ConfigureAwait(false);
    }

    public async Task<string> RunNetworkDiagnosticsAsync()
    {
        if (_networkDiagnostics is null)
            return "Network diagnostics are not initialized in this host.";

        var report = await _networkDiagnostics.RunQuickCheckAsync().ConfigureAwait(false);
        return report.Summary;
    }

    public Task<DriverDownloadState> GetDriverDownloadStateAsync()
    {
        lock (_driverLock)
        {
            return Task.FromResult(BuildDriverDownloadState(
                isScanning: false,
                machineType: string.Empty,
                os: string.Empty,
                source: string.Empty));
        }
    }

    public async Task<DriverDownloadState> SearchDriverPackagesAsync(
        string source,
        string machineType,
        string os,
        bool onlyUpdates)
    {
        if (_packageDownloaderFactory is null)
        {
            return new DriverDownloadState(
                false,
                false,
                machineType,
                os,
                source,
                [],
                "The driver download service is not initialized in this host.");
        }

        if (!Enum.TryParse<PackageDownloaderFactory.Type>(source, true, out var sourceType)
            || !Enum.TryParse<OS>(os, true, out var operatingSystem))
        {
            return new DriverDownloadState(false, false, machineType, os, source, [], "Select a valid driver source and operating system.");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(machineType))
            {
                var machine = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
                machineType = machine.MachineType;
            }

            var downloader = _packageDownloaderFactory.GetInstance(sourceType);
            var packages = await downloader.GetPackagesAsync(machineType, operatingSystem).ConfigureAwait(false);
            var hidden = _packageDownloaderSettings?.Store.HiddenPackages ?? [];
            var filtered = packages
                .Where(package => !hidden.Contains(package.Id)
                    && (!onlyUpdates || package.IsUpdate))
                .ToList();
            lock (_driverLock)
            {
                _driverDownloader = downloader;
                _driverPackages = filtered;
            }

            return BuildDriverDownloadState(false, machineType, os, source);
        }
        catch (Exception ex)
        {
            return new DriverDownloadState(true, false, machineType, os, source, [], ex.Message);
        }
    }

    public async Task<bool> DownloadDriverPackageAsync(string packageId, string destinationFolder)
    {
        if (string.IsNullOrWhiteSpace(packageId)
            || string.IsNullOrWhiteSpace(destinationFolder)
            || !Directory.Exists(destinationFolder))
            return false;

        IPackageDownloader? downloader;
        Package package;
        lock (_driverLock)
        {
            downloader = _driverDownloader;
            package = _driverPackages.FirstOrDefault(candidate => candidate.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }

        if (downloader is null || string.IsNullOrWhiteSpace(package.Id))
            return false;

        await downloader.DownloadPackageFileAsync(package, destinationFolder).ConfigureAwait(false);
        return true;
    }

    private DriverDownloadState BuildDriverDownloadState(
        bool isScanning,
        string machineType,
        string os,
        string source)
    {
        var packages = _driverPackages
            .Select(package => new DriverPackageItem(
                package.Id,
                package.Title,
                package.Description,
                package.Version,
                package.Category,
                package.FileSize,
                package.IsUpdate,
                package.Title.Contains("recommended", StringComparison.OrdinalIgnoreCase),
                package.ReleaseDate))
            .ToArray();
        return new DriverDownloadState(
            _packageDownloaderFactory is not null,
            isScanning,
            machineType,
            os,
            source,
            packages);
    }

    public async Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync()
    {
        await EnsureAutomationInitializedAsync().ConfigureAwait(false);
        var pipelines = await _automation.GetPipelinesAsync().ConfigureAwait(false);
        return new AutomationWorkspaceState(
            _automation.IsEnabled,
            pipelines.Select(p => new AutomationPipelineItem(
                    p.Id,
                    p.Name,
                    p.IconName,
                    p.Trigger?.DisplayName ?? "Manual quick action",
                    p.Steps.Count,
                    p.Trigger is not null)
                {
                    TriggerKey = GetAutomationTriggerKey(p.Trigger),
                    TriggerConfigurationJson = p.Trigger is null ? null : AutomationSerialization.SerializeTrigger(p.Trigger),
                    IsExclusive = p.IsExclusive,
                    Steps = p.Steps
                        .Select(CreateAutomationStepItem)
                        .ToArray(),
                })
                .ToArray());
    }

    public async Task<IReadOnlyList<AutomationTriggerOption>> GetAutomationTriggerOptionsAsync()
    {
        await EnsureAutomationInitializedAsync().ConfigureAwait(false);
        return CreateAutomationTriggerDefinitions()
            .Select(definition => new AutomationTriggerOption(
                definition.Key,
                definition.Trigger.DisplayName,
                AutomationSerialization.SerializeTrigger(definition.Trigger)))
            .ToArray();
    }

    public async Task<IReadOnlyList<AutomationStepOption>> GetAutomationStepOptionsAsync()
    {
        var candidates = new IAutomationStep[]
        {
            new AlwaysOnUsbAutomationStep(default),
            new BatteryAutomationStep(default),
            new BatteryNightChargeAutomationStep(default),
            new DeactivateGPUAutomationStep(default),
            new DelayAutomationStep(default),
            new DisplayBrightnessAutomationStep(50),
            new DpiScaleAutomationStep(default),
            new FlipToStartAutomationStep(default),
            new FnLockAutomationStep(default),
            new GodModePresetAutomationStep(default),
            new HDRAutomationStep(default),
            new InstantBootAutomationStep(default),
            new MacroAutomationStep(default),
            new MicrophoneAutomationStep(default),
            new SpeakerAutomationStep(default),
            new NotificationAutomationStep(default),
            new OsdAutomationStep(default),
            new OneLevelWhiteKeyboardBacklightAutomationStep(default),
            new OverclockDiscreteGPUAutomationStep(default),
            new OverDriveAutomationStep(default),
            new PanelLogoBacklightAutomationStep(default),
            new PlaySoundAutomationStep(default),
            new PortsBacklightAutomationStep(default),
            new PowerModeAutomationStep(default),
            new QuickActionAutomationStep(default),
            new RefreshRateAutomationStep(default),
            new ResolutionAutomationStep(default),
            new RGBKeyboardBacklightAutomationStep(default),
            new RunAutomationStep(default, default, default, default),
            new SpectrumKeyboardBacklightBrightnessAutomationStep(0),
            new SpectrumKeyboardBacklightProfileAutomationStep(1),
            new SpectrumKeyboardBacklightImportProfileAutomationStep(default),
            new TouchpadLockAutomationStep(default),
            new TurnOffMonitorsAutomationStep(),
            new TurnOffWiFiAutomationStep(),
            new TurnOnWiFiAutomationStep(),
            new HybridModeAutomationStep(default),
            new WhiteKeyboardBacklightAutomationStep(default),
            new WinKeyAutomationStep(default),
            new ShowMainWindowAutomationStep(),
            new HideMainWindowAutomationStep(),
        };

        var supported = await Task.WhenAll(candidates.Select(async step =>
        {
            try { return new { Step = step, Supported = await step.IsSupportedAsync().ConfigureAwait(false) }; }
            catch { return new { Step = step, Supported = false }; }
        })).ConfigureAwait(false);

        return supported.Where(x => x.Supported).Select(x =>
        {
            var type = x.Step.GetType();
            var key = type.Name.EndsWith("AutomationStep", StringComparison.Ordinal)
                ? type.Name[..^"AutomationStep".Length]
                : type.Name;
            var displayName = AvaloniaLocalization.GetString($"{key}AutomationStepControl_Title", key);
            return new AutomationStepOption(key, displayName, AutomationSerialization.SerializeStep(x.Step));
        }).ToArray();
    }

    public async Task<bool> SetAutomationEnabledAsync(bool enabled)
    {
        try
        {
            await EnsureAutomationInitializedAsync().ConfigureAwait(false);
            await _automation.SetEnabledAsync(enabled).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveAutomationWorkspaceAsync(IReadOnlyList<AutomationPipelineDraft> drafts)
    {
        try
        {
            await EnsureAutomationInitializedAsync().ConfigureAwait(false);
            var existing = await _automation.GetPipelinesAsync().ConfigureAwait(false);
            var byId = existing.ToDictionary(p => p.Id);
            var saved = new List<AutomationPipeline>();
            var seen = new HashSet<Guid>();

            foreach (var draft in drafts)
            {
                if (draft.Id is Guid id && byId.TryGetValue(id, out var pipeline))
                {
                    if (!seen.Add(id))
                        continue;

                    pipeline.Name = NormalizePipelineName(draft.Name);
                    pipeline.IconName = draft.IconName;
                    pipeline.IsExclusive = draft.IsExclusive;
                    pipeline.Steps.Clear();
                    foreach (var stepItem in draft.Steps)
                    {
                        var step = TryDeserializeStep(stepItem.ConfigurationJson);
                        if (step is not null)
                            pipeline.Steps.Add(step);
                    }
                    if (draft.IsAutomatic)
                    {
                        var trigger = TryDeserializeTrigger(draft.TriggerConfigurationJson)
                            ?? CreateAutomationTrigger(draft.TriggerKey);
                        if (trigger is not null)
                            pipeline.Trigger = trigger;
                    }
                    else
                        pipeline.Trigger = null;
                    saved.Add(pipeline);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(draft.Name))
                    continue;

                if (draft.IsAutomatic)
                {
                    var trigger = TryDeserializeTrigger(draft.TriggerConfigurationJson)
                        ?? CreateAutomationTrigger(draft.TriggerKey);
                    if (trigger is null)
                        continue;

                    saved.Add(new AutomationPipeline(trigger)
                    {
                        Name = NormalizePipelineName(draft.Name),
                        IconName = draft.IconName,
                        IsExclusive = draft.IsExclusive,
                        Steps = draft.Steps
                            .Select(item => TryDeserializeStep(item.ConfigurationJson))
                            .OfType<IAutomationStep>()
                            .ToList(),
                    });
                    continue;
                }

                saved.Add(new AutomationPipeline(NormalizePipelineName(draft.Name)!)
                {
                    IconName = draft.IconName,
                    IsExclusive = draft.IsExclusive,
                    Steps = draft.Steps
                        .Select(item => TryDeserializeStep(item.ConfigurationJson))
                        .OfType<IAutomationStep>()
                        .ToList(),
                });
            }

            await _automation.ReloadPipelinesAsync(saved).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? NormalizePipelineName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    private static IReadOnlyList<(string Key, IAutomationPipelineTrigger Trigger)> CreateAutomationTriggerDefinitions() =>
    [
        ("on-startup", new OnStartupAutomationPipelineTrigger()),
        ("on-resume", new OnResumeAutomationPipelineTrigger()),
        ("ac-adapter-connected", new ACAdapterConnectedAutomationPipelineTrigger()),
        ("ac-adapter-disconnected", new ACAdapterDisconnectedAutomationPipelineTrigger()),
        ("display-on", new DisplayOnAutomationPipelineTrigger()),
        ("display-off", new DisplayOffAutomationPipelineTrigger()),
        ("session-lock", new SessionLockAutomationPipelineTrigger()),
        ("session-unlock", new SessionUnlockAutomationPipelineTrigger()),
        ("lid-opened", new LidOpenedAutomationPipelineTrigger()),
        ("lid-closed", new LidClosedAutomationPipelineTrigger()),
        ("hdr-on", new HDROnAutomationPipelineTrigger()),
        ("hdr-off", new HDROffAutomationPipelineTrigger()),
        ("wifi-disconnected", new WiFiDisconnectedAutomationPipelineTrigger()),
        ("external-display-connected", new ExternalDisplayConnectedAutomationPipelineTrigger()),
        ("external-display-disconnected", new ExternalDisplayDisconnectedAutomationPipelineTrigger()),
        ("low-wattage-ac-adapter-connected", new LowWattageACAdapterConnectedAutomationPipelineTrigger()),
        ("game-started", new GamesAreRunningAutomationPipelineTrigger()),
        ("game-stopped", new GamesStopAutomationPipelineTrigger()),
        ("processes-running", new ProcessesAreRunningAutomationPipelineTrigger(Array.Empty<ProcessInfo>())),
        ("processes-stopped", new ProcessesStopRunningAutomationPipelineTrigger(Array.Empty<ProcessInfo>())),
        ("device-connected", new DeviceConnectedAutomationPipelineTrigger(Array.Empty<string>())),
        ("device-disconnected", new DeviceDisconnectedAutomationPipelineTrigger(Array.Empty<string>())),
        ("time", new TimeAutomationPipelineTrigger(false, false, null, Array.Empty<DayOfWeek>())),
        ("periodic", new PeriodicAutomationPipelineTrigger(TimeSpan.FromMinutes(30))),
        ("user-inactivity", new UserInactivityAutomationPipelineTrigger(TimeSpan.FromMinutes(5))),
        ("wifi-connected", new WiFiConnectedAutomationPipelineTrigger(Array.Empty<string>())),
        ("power-mode", new PowerModeAutomationPipelineTrigger(PowerModeState.Balance)),
        ("hardware-sensor", new HardwareSensorAutomationPipelineTrigger(
            HardwareSensorMetric.CpuTemperature,
            HardwareSensorComparison.GreaterThanOrEqual,
            80,
            TimeSpan.Zero,
            TimeSpan.Zero)),
        ("battery-percentage", new BatteryPercentageAutomationPipelineTrigger(
            BatteryPercentageComparison.BelowOrEqual,
            20,
            TimeSpan.Zero,
            TimeSpan.Zero,
            BatteryChargeFilter.Any)),
    ];

    private static IAutomationPipelineTrigger? CreateAutomationTrigger(string? key) =>
        CreateAutomationTriggerDefinitions()
            .FirstOrDefault(definition => string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase))
            .Trigger;

    private static string? GetAutomationTriggerKey(IAutomationPipelineTrigger? trigger) => trigger switch
    {
        OnStartupAutomationPipelineTrigger => "on-startup",
        OnResumeAutomationPipelineTrigger => "on-resume",
        ACAdapterConnectedAutomationPipelineTrigger => "ac-adapter-connected",
        ACAdapterDisconnectedAutomationPipelineTrigger => "ac-adapter-disconnected",
        DisplayOnAutomationPipelineTrigger => "display-on",
        DisplayOffAutomationPipelineTrigger => "display-off",
        SessionLockAutomationPipelineTrigger => "session-lock",
        SessionUnlockAutomationPipelineTrigger => "session-unlock",
        LidOpenedAutomationPipelineTrigger => "lid-opened",
        LidClosedAutomationPipelineTrigger => "lid-closed",
        HDROnAutomationPipelineTrigger => "hdr-on",
        HDROffAutomationPipelineTrigger => "hdr-off",
        WiFiDisconnectedAutomationPipelineTrigger => "wifi-disconnected",
        ExternalDisplayConnectedAutomationPipelineTrigger => "external-display-connected",
        ExternalDisplayDisconnectedAutomationPipelineTrigger => "external-display-disconnected",
        LowWattageACAdapterConnectedAutomationPipelineTrigger => "low-wattage-ac-adapter-connected",
        GamesAreRunningAutomationPipelineTrigger => "game-started",
        GamesStopAutomationPipelineTrigger => "game-stopped",
        ProcessesAreRunningAutomationPipelineTrigger => "processes-running",
        ProcessesStopRunningAutomationPipelineTrigger => "processes-stopped",
        DeviceConnectedAutomationPipelineTrigger => "device-connected",
        DeviceDisconnectedAutomationPipelineTrigger => "device-disconnected",
        TimeAutomationPipelineTrigger => "time",
        PeriodicAutomationPipelineTrigger => "periodic",
        UserInactivityAutomationPipelineTrigger => "user-inactivity",
        WiFiConnectedAutomationPipelineTrigger => "wifi-connected",
        PowerModeAutomationPipelineTrigger => "power-mode",
        HardwareSensorAutomationPipelineTrigger => "hardware-sensor",
        BatteryPercentageAutomationPipelineTrigger => "battery-percentage",
        _ => null,
    };

    private static AutomationStepItem CreateAutomationStepItem(IAutomationStep step)
    {
        var type = step.GetType();
        var key = type.Name.EndsWith("AutomationStep", StringComparison.Ordinal)
            ? type.Name[..^"AutomationStep".Length]
            : type.Name;
        var displayName = AvaloniaLocalization.GetString($"{key}AutomationStepControl_Title", key);
        return new AutomationStepItem(key, displayName, AutomationSerialization.SerializeStep(step));
    }

    private static IAutomationStep? TryDeserializeStep(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try { return AutomationSerialization.DeserializeStep(json); }
        catch { return null; }
    }

    private static IAutomationPipelineTrigger? TryDeserializeTrigger(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try { return AutomationSerialization.DeserializeTrigger(json); }
        catch { return null; }
    }

    public Task<MacroWorkspaceState> GetMacroWorkspaceAsync()
    {
        if (_macro is not MacroController controller)
            return Task.FromResult(new MacroWorkspaceState(_macro.IsEnabled, false, Array.Empty<MacroSlotState>()));

        var sequences = controller.GetSequences();
        var slots = MacroKeys.Select(key =>
        {
            var identifier = new MacroIdentifier(MacroSource.Keyboard, key);
            sequences.TryGetValue(identifier, out var sequence);
            var events = (sequence.Events ?? [])
                .Select(macroEvent => new MacroEventItem(
                    macroEvent.Source.ToString(),
                    macroEvent.Direction.ToString(),
                    macroEvent.Key,
                    macroEvent.Point.X,
                    macroEvent.Point.Y,
                    macroEvent.Delay))
                .ToArray();
            return new MacroSlotState(
                key,
                sequence.Events?.Length ?? 0,
                Math.Clamp(sequence.RepeatCount, 1, 10),
                sequence.IgnoreDelays,
                sequence.InterruptOnOtherKey,
                events);
        }).ToArray();

        return Task.FromResult(new MacroWorkspaceState(controller.IsEnabled, controller.IsRecording, slots));
    }

    public Task<bool> SetMacroEnabledAsync(bool enabled)
    {
        try
        {
            _macro.SetEnabled(enabled);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> StartMacroRecordingAsync(ulong key, MacroRecordingMode mode)
    {
        if (_macro is not MacroController controller || !MacroKeys.Contains(key))
            return Task.FromResult(false);

        return Task.FromResult(StartMacroRecording(controller, key, ToMacroRecorderSettings(mode)));
    }

    public Task<bool> SetMacroSequenceOptionsAsync(
        ulong key,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey)
    {
        if (_macro is not MacroController controller
            || !MacroKeys.Contains(key)
            || !MacroController.AllowedRepeatCounts.Contains(repeatCount))
            return Task.FromResult(false);

        var sequences = controller.GetSequences();
        var identifier = new MacroIdentifier(MacroSource.Keyboard, key);
        sequences.TryGetValue(identifier, out var existing);
        sequences[identifier] = new MacroSequence
        {
            RepeatCount = repeatCount,
            IgnoreDelays = ignoreDelays,
            InterruptOnOtherKey = interruptOnOtherKey,
            Events = existing.Events ?? [],
        };
        controller.SetSequences(sequences);
        return Task.FromResult(true);
    }

    public Task<bool> ClearMacroSequenceAsync(ulong key)
    {
        if (_macro is not MacroController controller || !MacroKeys.Contains(key))
            return Task.FromResult(false);

        var sequences = controller.GetSequences();
        var identifier = new MacroIdentifier(MacroSource.Keyboard, key);
        sequences.TryGetValue(identifier, out var existing);
        sequences[identifier] = new MacroSequence
        {
            RepeatCount = Math.Clamp(existing.RepeatCount, 1, 10),
            IgnoreDelays = existing.IgnoreDelays,
            InterruptOnOtherKey = existing.InterruptOnOtherKey,
            Events = [],
        };
        controller.SetSequences(sequences);
        return Task.FromResult(true);
    }

    private async Task<bool> SetActionCoreAsync(string routeKey, string actionKey, bool isSelected)
    {
        switch (routeKey)
        {
            case "Keyboard" when actionKey == "keyboard-spectrum-brightness-up" && _spectrum is not null:
                await AdjustSpectrumBrightnessAsync(_spectrum, 1).ConfigureAwait(false);
                return true;
            case "Keyboard" when actionKey == "keyboard-spectrum-brightness-down" && _spectrum is not null:
                await AdjustSpectrumBrightnessAsync(_spectrum, -1).ConfigureAwait(false);
                return true;
            case "Keyboard" when actionKey == "keyboard-spectrum-logo" && _spectrum is not null:
                await _spectrum.SetLogoStatusAsync(isSelected).ConfigureAwait(false);
                return true;
            case "Keyboard" when actionKey.StartsWith("keyboard-rgb-preset:", StringComparison.OrdinalIgnoreCase)
                                 && _rgb is not null
                                 && Enum.TryParse<RGBKeyboardBacklightPreset>(actionKey["keyboard-rgb-preset:".Length..], true, out var preset):
                await _rgb.SetPresetAsync(preset).ConfigureAwait(false);
                return true;
            case "Macro" when actionKey == "macro-controller":
                _macro.SetEnabled(isSelected);
                return true;
            case "Macro" when actionKey == "macro-record" && _macro is MacroController recordingController:
                return StartMacroRecording(recordingController, 0x60, MacroRecorderSettings.Keyboard);
            case "Macro" when FeatureActionContract.TryParseMacroRecordKey(actionKey, out var recordingKey)
                                 && _macro is MacroController recordingController:
                return StartMacroRecording(recordingController, recordingKey, MacroRecorderSettings.Keyboard);
            case "Macro" when actionKey == "macro-stop-recording" && _macro is MacroController stoppingController:
                if (!stoppingController.IsRecording)
                    return false;

                stoppingController.StopRecording();
                return true;
            case "Macro" when FeatureActionContract.TryParseMacroPlayKey(actionKey, out var macroKey)
                                 && _macro is MacroController playbackController:
                return playbackController.TryPlaySequence(macroKey);
            case "Actions" when actionKey == "automation-enabled":
                await EnsureAutomationInitializedAsync().ConfigureAwait(false);
                await _automation.SetEnabledAsync(isSelected).ConfigureAwait(false);
                return true;
            case "Actions" when actionKey.StartsWith("automation-pipeline:", StringComparison.OrdinalIgnoreCase):
                if (!Guid.TryParse(actionKey["automation-pipeline:".Length..], out var pipelineId))
                    return false;

                await EnsureAutomationInitializedAsync().ConfigureAwait(false);
                var pipeline = (await _automation.GetPipelinesAsync().ConfigureAwait(false))
                    .FirstOrDefault(candidate => candidate.Id == pipelineId);
                if (pipeline is null)
                    return false;

                await _automation.RunNowAsync(pipeline).ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey == "plugin-refresh":
                await _plugins.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey == "plugin-check-updates":
                await _plugins.CheckForUpdatesAsync().ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey.StartsWith("plugin-open:", StringComparison.OrdinalIgnoreCase):
                var openId = actionKey["plugin-open:".Length..];
                return !string.IsNullOrWhiteSpace(openId)
                       && _plugins.IsInstalled(openId)
                       && _plugins.GetRegisteredPlugins()
                           .FirstOrDefault(plugin => plugin.Id.Equals(openId, StringComparison.OrdinalIgnoreCase)) is { } openPlugin
                       && HasPluginFeaturePage(openPlugin);
            case "PluginExtensions" when actionKey.StartsWith("plugin-reload:", StringComparison.OrdinalIgnoreCase):
                var reloadId = actionKey["plugin-reload:".Length..];
                if (string.IsNullOrWhiteSpace(reloadId) || !_plugins.IsInstalled(reloadId))
                    return false;

                await _plugins.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey.StartsWith("plugin-install:", StringComparison.OrdinalIgnoreCase):
                var installId = actionKey["plugin-install:".Length..];
                if (string.IsNullOrWhiteSpace(installId))
                    return false;

                _plugins.InstallPlugin(installId);
                return true;
            case "PluginExtensions" when actionKey.StartsWith("plugin-uninstall:", StringComparison.OrdinalIgnoreCase):
                var uninstallId = actionKey["plugin-uninstall:".Length..];
                if (string.IsNullOrWhiteSpace(uninstallId) || !_plugins.IsInstalled(uninstallId))
                    return false;

                return _plugins.UninstallPlugin(uninstallId);
            case "WindowsOptimization" when _optimization is not null:
                if (FeatureActionContract.IsCleanupAction(actionKey))
                {
                    if (isSelected)
                        _selectedCleanupActions.Add(actionKey);
                    else
                        _selectedCleanupActions.Remove(actionKey);

                    PersistCleanupSelection();
                    return true;
                }

                if (actionKey.Equals(FeatureActionContract.OptimizationApplyRecommendedActionKey, StringComparison.OrdinalIgnoreCase))
                {
                    var recommended = _optimization.GetCategories()
                        .Where(category => !FeatureActionContract.IsCleanupAction(category.Key))
                        .SelectMany(category => category.Actions)
                        .Where(action => action.Recommended)
                        .Select(action => action.Key)
                        .ToArray();
                    await ExecuteRecommendedOptimizationAsync(recommended, CancellationToken.None).ConfigureAwait(false);
                    return true;
                }

                if (actionKey.Equals(FeatureActionContract.CleanupScanActionKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (_selectedCleanupActions.Count == 0)
                        return false;

                    _estimatedCleanupSize = await _optimization.EstimateCleanupSizeAsync(
                        _selectedCleanupActions,
                        CancellationToken.None).ConfigureAwait(false);
                    return true;
                }

                if (actionKey.Equals(FeatureActionContract.CleanupClearActionKey, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedCleanupActions.Clear();
                    _estimatedCleanupSize = 0;
                    PersistCleanupSelection();
                    return true;
                }

                if (actionKey.Equals(FeatureActionContract.CleanupRunActionKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (_selectedCleanupActions.Count == 0)
                        return false;

                    await ExecuteCleanupAsync(
                        _selectedCleanupActions.ToArray(),
                        CancellationToken.None).ConfigureAwait(false);
                    _selectedCleanupActions.Clear();
                    _estimatedCleanupSize = 0;
                    PersistCleanupSelection();
                    return true;
                }

                var action = _optimization.GetCategories()
                    .SelectMany(category => category.Actions)
                    .FirstOrDefault(candidate => candidate.Key.Equals(actionKey, StringComparison.OrdinalIgnoreCase));
                if (action is null)
                    return false;

                if (isSelected)
                {
                    await ExecuteOptimizationActionAsync(action.Key, apply: true, CancellationToken.None).ConfigureAwait(false);
                    return true;
                }

                if (action.RollbackAsync is null)
                    return false;

                await ExecuteOptimizationActionAsync(action.Key, apply: false, CancellationToken.None).ConfigureAwait(false);
                return true;
            default:
                return false;
        }
    }

    private void PersistCleanupSelection()
    {
        if (_applicationSettings is null)
            return;

        _applicationSettings.Store.SelectedCleanupActions = _selectedCleanupActions.ToList();
        _applicationSettings.SynchronizeStore();
    }

    private Task ExecuteRecommendedOptimizationAsync(
        IReadOnlyList<string> actionKeys,
        CancellationToken cancellationToken)
    {
        return WindowsOptimizationElevationBridge.IsAvailable
            ? WindowsOptimizationElevationBridge.ExecuteRecommendedAsync(actionKeys, cancellationToken)
            : _optimization!.ExecuteActionsAsync(actionKeys, cancellationToken);
    }

    private Task ExecuteCleanupAsync(
        IReadOnlyList<string> actionKeys,
        CancellationToken cancellationToken)
    {
        return WindowsOptimizationElevationBridge.IsAvailable
            ? WindowsOptimizationElevationBridge.ExecuteCleanupAsync(actionKeys, cancellationToken)
            : _optimization!.ExecuteActionsAsync(actionKeys, cancellationToken);
    }

    private Task ExecuteOptimizationActionAsync(
        string actionKey,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (WindowsOptimizationElevationBridge.IsAvailable)
            return WindowsOptimizationElevationBridge.ExecuteActionAsync(actionKey, apply, cancellationToken);

        return apply
            ? _optimization!.ApplyActionAsync(actionKey, cancellationToken)
            : _optimization!.RevertActionAsync(actionKey, cancellationToken);
    }

    private async Task<FeaturePageState> GetKeyboardStateAsync()
    {
        var spectrum = await _keyboard.IsSpectrumSupportedAsync().ConfigureAwait(false);
        var rgb = !spectrum && await _keyboard.IsRgbSupportedAsync().ConfigureAwait(false);
        var status = spectrum ? "Spectrum supported" : rgb ? "RGB supported" : "No compatible keyboard detected";
        var actions = new List<FeatureActionItem>();

        if (spectrum && _spectrum is not null)
        {
            var brightness = -1;
            try
            {
                brightness = await _spectrum.GetBrightnessAsync().ConfigureAwait(false);
            }
            catch
            {
                // Keep controls visible even when a transient device read fails.
            }

            actions.Add(new FeatureActionItem(
                "keyboard-spectrum-brightness-down",
                "Decrease keyboard brightness",
                brightness >= 0 ? $"Current Spectrum brightness: {brightness}/9." : "Decrease the Spectrum keyboard brightness.",
                "Decrease",
                brightness != 0,
                false,
                false));
            actions.Add(new FeatureActionItem(
                "keyboard-spectrum-brightness-up",
                "Increase keyboard brightness",
                brightness >= 0 ? $"Current Spectrum brightness: {brightness}/9." : "Increase the Spectrum keyboard brightness.",
                "Increase",
                brightness < 9,
                false,
                false));

            try
            {
                var logoEnabled = await _spectrum.GetLogoStatusAsync().ConfigureAwait(false);
                actions.Add(new FeatureActionItem(
                    "keyboard-spectrum-logo",
                    "Keyboard logo lighting",
                    "Turn the Spectrum keyboard logo lighting on or off.",
                    logoEnabled ? "On" : "Off",
                    true,
                    logoEnabled,
                    true));
            }
            catch
            {
                // Logo support varies by device generation.
            }
        }
        else if (rgb && _rgb is not null)
        {
            RGBKeyboardBacklightPreset? selectedPreset = null;
            try
            {
                selectedPreset = (await _rgb.GetStateAsync().ConfigureAwait(false)).SelectedPreset;
            }
            catch
            {
                // Presets remain available even when the current state cannot be read.
            }

            foreach (var preset in Enum.GetValues<RGBKeyboardBacklightPreset>())
            {
                actions.Add(new FeatureActionItem(
                    $"keyboard-rgb-preset:{preset}",
                    $"RGB preset: {preset}",
                    "Apply this RGB keyboard backlight preset.",
                    selectedPreset == preset ? "Selected" : "Apply",
                    true,
                    false,
                    false));
            }
        }

        return new FeaturePageState(
            "Keyboard",
            "Keyboard",
            "Configure keyboard backlight and keyboard-specific controls.",
            spectrum || rgb ? "Available" : "Unavailable on this device",
            status,
            spectrum || rgb,
            actions.Count == 0
                ? [new FeatureActionItem(
                    "keyboard-backlight",
                    "Keyboard backlight detection",
                    "The shared Windows keyboard service reports the detected backlight mode.",
                    status,
                    false,
                    false,
                    false)]
                : actions);
    }

    public async Task<KeyboardLightingState?> GetKeyboardLightingStateAsync()
    {
        if (_spectrum is not null && await _spectrum.IsSupportedAsync().ConfigureAwait(false))
        {
            var profile = await _spectrum.GetProfileAsync().ConfigureAwait(false);
            var brightness = await _spectrum.GetBrightnessAsync().ConfigureAwait(false);
            var logoEnabled = await _spectrum.GetLogoStatusAsync().ConfigureAwait(false);
            var (_, effects) = await _spectrum.GetProfileDescriptionAsync(profile).ConfigureAwait(false);
            return new KeyboardLightingState(
                "Spectrum",
                brightness,
                logoEnabled,
                profile,
                effects.Select(effect => new KeyboardSpectrumEffectState(
                    effect.Type.ToString(),
                    effect.Speed.ToString(),
                    effect.Direction.ToString(),
                    effect.ClockwiseDirection.ToString(),
                    effect.Colors.Select(ToKeyboardColor).ToArray(),
                    effect.Keys)).ToArray(),
                []);
        }

        if (_rgb is not null && await _rgb.IsSupportedAsync().ConfigureAwait(false))
        {
            var state = await _rgb.GetStateAsync().ConfigureAwait(false);
            var presets = Enum.GetValues<RGBKeyboardBacklightPreset>()
                .Select(preset =>
                {
                    var description = state.Presets.GetValueOrDefault(
                        preset,
                        RGBKeyboardBacklightBacklightPresetDescription.Default);
                    var zones = preset == RGBKeyboardBacklightPreset.Off
                        ? []
                        : new[]
                        {
                            ToKeyboardColor(description.Zone1),
                            ToKeyboardColor(description.Zone2),
                            ToKeyboardColor(description.Zone3),
                            ToKeyboardColor(description.Zone4),
                        };
                    return new KeyboardRgbPresetState(
                        preset.ToString(),
                        preset.ToString(),
                        state.SelectedPreset == preset,
                        description.Effect.ToString(),
                        description.Speed.ToString(),
                        description.Brightness.ToString(),
                        zones);
                })
                .ToArray();

            return new KeyboardLightingState("RGB", 0, false, 0, [], presets);
        }

        return null;
    }

    public async Task<bool> SetKeyboardLightingAsync(KeyboardLightingUpdate update)
    {
        if (update.Mode.Equals("Spectrum", StringComparison.OrdinalIgnoreCase) && _spectrum is not null)
        {
            if (!await _spectrum.IsSupportedAsync().ConfigureAwait(false))
                return false;

            if (update.SelectedProfile is { } profile && update.SpectrumEffects is null)
                await _spectrum.SetProfileAsync(profile).ConfigureAwait(false);
            if (update.Brightness is { } brightness)
                await _spectrum.SetBrightnessAsync(Math.Clamp(brightness, 0, 9)).ConfigureAwait(false);
            if (update.LogoEnabled is { } logoEnabled)
                await _spectrum.SetLogoStatusAsync(logoEnabled).ConfigureAwait(false);
            if (update.SpectrumEffects is not null)
            {
                var selectedProfile = update.SelectedProfile ?? await _spectrum.GetProfileAsync().ConfigureAwait(false);
                var effects = new List<SpectrumKeyboardBacklightEffect>();
                foreach (var item in update.SpectrumEffects)
                {
                    if (!Enum.TryParse<SpectrumKeyboardBacklightEffectType>(item.Type, true, out var type)
                        || !Enum.TryParse<SpectrumKeyboardBacklightSpeed>(item.Speed, true, out var speed)
                        || !Enum.TryParse<SpectrumKeyboardBacklightDirection>(item.Direction, true, out var direction)
                        || !Enum.TryParse<SpectrumKeyboardBacklightClockwiseDirection>(item.ClockwiseDirection, true, out var clockwise))
                    {
                        return false;
                    }

                    effects.Add(new SpectrumKeyboardBacklightEffect(
                        type,
                        speed,
                        direction,
                        clockwise,
                        item.Colors.Select(ToRgbColor).ToArray(),
                        item.Keys.ToArray()));
                }

                await _spectrum.SetProfileDescriptionAsync(selectedProfile, effects.ToArray()).ConfigureAwait(false);
            }

            return true;
        }

        if (update.Mode.Equals("RGB", StringComparison.OrdinalIgnoreCase) && _rgb is not null)
        {
            if (!await _rgb.IsSupportedAsync().ConfigureAwait(false))
                return false;

            var state = await _rgb.GetStateAsync().ConfigureAwait(false);
            var selected = state.SelectedPreset;
            if (!string.IsNullOrWhiteSpace(update.RgbPreset)
                && !Enum.TryParse(update.RgbPreset, true, out selected))
            {
                return false;
            }

            if (update.RgbEffect is null && update.RgbSpeed is null
                && update.RgbBrightness is null && update.RgbZones is null)
            {
                await _rgb.SetPresetAsync(selected).ConfigureAwait(false);
                return true;
            }

            var current = state.Presets.GetValueOrDefault(
                selected,
                RGBKeyboardBacklightBacklightPresetDescription.Default);
            if (!Enum.TryParse(update.RgbEffect ?? current.Effect.ToString(), true, out RGBKeyboardBacklightEffect effect)
                || !Enum.TryParse(update.RgbSpeed ?? current.Speed.ToString(), true, out RGBKeyboardBacklightSpeed speed)
                || !Enum.TryParse(update.RgbBrightness ?? current.Brightness.ToString(), true, out RGBKeyboardBacklightBrightness brightness))
            {
                return false;
            }

            var zones = update.RgbZones?.Count >= 4
                ? update.RgbZones.Take(4).Select(ToRgbColor).ToArray()
                : [current.Zone1, current.Zone2, current.Zone3, current.Zone4];
            var presets = new Dictionary<RGBKeyboardBacklightPreset, RGBKeyboardBacklightBacklightPresetDescription>(state.Presets)
            {
                [selected] = new(effect, speed, brightness, zones[0], zones[1], zones[2], zones[3]),
            };
            await _rgb.SetStateAsync(new(selected, presets)).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public async Task<bool> ResetKeyboardSpectrumProfileAsync()
    {
        if (_spectrum is null || !await _spectrum.IsSupportedAsync().ConfigureAwait(false))
            return false;

        var profile = await _spectrum.GetProfileAsync().ConfigureAwait(false);
        await _spectrum.SetProfileDefaultAsync(profile).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> ExportKeyboardSpectrumProfileAsync(string filePath)
    {
        if (_spectrum is null || string.IsNullOrWhiteSpace(filePath)
            || !await _spectrum.IsSupportedAsync().ConfigureAwait(false))
            return false;

        var profile = await _spectrum.GetProfileAsync().ConfigureAwait(false);
        await _spectrum.ExportProfileDescriptionAsync(profile, filePath).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> ImportKeyboardSpectrumProfileAsync(string filePath)
    {
        if (_spectrum is null || string.IsNullOrWhiteSpace(filePath)
            || !File.Exists(filePath)
            || !await _spectrum.IsSupportedAsync().ConfigureAwait(false))
            return false;

        var profile = await _spectrum.GetProfileAsync().ConfigureAwait(false);
        await _spectrum.ImportProfileDescription(profile, filePath).ConfigureAwait(false);
        return true;
    }

    private static KeyboardColorState ToKeyboardColor(RGBColor color) => new(color.R, color.G, color.B);

    private static RGBColor ToRgbColor(KeyboardColorState color) => new(color.R, color.G, color.B);

    private static async Task AdjustSpectrumBrightnessAsync(SpectrumKeyboardBacklightController controller, int delta)
    {
        var current = await controller.GetBrightnessAsync().ConfigureAwait(false);
        await controller.SetBrightnessAsync(Math.Clamp(current + delta, 0, 9)).ConfigureAwait(false);
    }

    private bool StartMacroRecording(
        MacroController controller,
        ulong key,
        MacroRecorderSettings settings)
    {
        if (controller.IsRecording)
            return false;

        lock (_macroRecordingLock)
        {
            _macroRecordingKey = key;
            _macroRecordingEvents = [];
        }

        controller.StartRecording(settings);
        if (controller.IsRecording)
            return true;

        lock (_macroRecordingLock)
        {
            _macroRecordingKey = null;
            _macroRecordingEvents = null;
        }

        return false;
    }

    private static MacroRecorderSettings ToMacroRecorderSettings(MacroRecordingMode mode) => mode switch
    {
        MacroRecordingMode.KeyboardMouse => MacroRecorderSettings.Keyboard | MacroRecorderSettings.Mouse,
        MacroRecordingMode.KeyboardMouseMovement => MacroRecorderSettings.Keyboard
            | MacroRecorderSettings.Mouse
            | MacroRecorderSettings.Movement,
        _ => MacroRecorderSettings.Keyboard,
    };

    private void MacroController_RecorderReceived(object? sender, MacroController.RecorderReceivedEventArgs e)
    {
        lock (_macroRecordingLock)
        {
            if (_macroRecordingKey is not null)
                _macroRecordingEvents?.Add(e.MacroEvent);
        }
    }

    private void MacroController_RecorderStopped(object? sender, MacroController.RecorderStoppedEventArgs e)
    {
        ulong? key;
        List<MacroEvent>? events;
        lock (_macroRecordingLock)
        {
            key = _macroRecordingKey;
            events = _macroRecordingEvents;
            _macroRecordingKey = null;
            _macroRecordingEvents = null;
        }

        if (e.Interrupted || key is not { } macroKey || events is null)
            return;

        var controller = _macro as MacroController;
        if (controller is null)
            return;

        var sequences = controller.GetSequences();
        var identifier = new MacroIdentifier(MacroSource.Keyboard, macroKey);
        sequences.TryGetValue(identifier, out var existing);
        sequences[identifier] = new MacroSequence
        {
            RepeatCount = Math.Max(1, existing.RepeatCount),
            IgnoreDelays = existing.IgnoreDelays,
            InterruptOnOtherKey = existing.InterruptOnOtherKey,
            Events = [.. events],
        };
        controller.SetSequences(sequences);
    }

    private FeaturePageState GetMacroState()
    {
        var controller = _macro as MacroController;
        var sequences = controller?.GetSequences();
        var actions = new List<FeatureActionItem>
        {
            new FeatureActionItem(
                "macro-controller",
                "Enable macro input",
                "Enable or disable the global macro input hook used by the macro workspace.",
                _macro.IsEnabled ? "Enabled" : "Disabled",
                true,
                _macro.IsEnabled,
                true),
        };

        if (controller is null)
        {
            actions.Add(new FeatureActionItem(
                "macro-controller-status",
                "Macro workspace",
                "The host macro controller does not expose sequence editing on this adapter.",
                "Unavailable",
                false,
                false,
                false));
        }
        else
        {
            actions.Add(new FeatureActionItem(
                "macro-stop-recording",
                "Stop recording",
                "Stop the active macro recording and persist the captured sequence.",
                "Stop",
                controller.IsRecording,
                false,
                false));

            foreach (var key in MacroKeys)
            {
                var identifier = new MacroIdentifier(MacroSource.Keyboard, key);
                sequences!.TryGetValue(identifier, out var sequence);
                var eventCount = sequence.Events?.Length ?? 0;
                var digit = key - 0x60;
                var title = $"Numpad {digit}";
                var description = eventCount == 0
                    ? "No sequence is stored for this macro slot."
                    : $"{eventCount} recorded event(s), repeats {Math.Max(1, sequence.RepeatCount)} time(s). Click Play to send it through the shared macro player.";
                actions.Add(new FeatureActionItem(
                    $"macro-key:{key:X}",
                    title,
                    description,
                    eventCount == 0 ? "Empty" : "Play",
                    eventCount > 0,
                    false,
                    false));
                actions.Add(new FeatureActionItem(
                    $"macro-record:{key:X}",
                    $"Record Numpad {digit}",
                    $"Capture keyboard input into Numpad {digit}. Stop recording to save the sequence.",
                    controller.IsRecording ? "Recording" : "Record",
                    !controller.IsRecording,
                    false,
                    false));
            }
        }

        var populated = sequences?.Count ?? 0;
        return new FeaturePageState(
            "Macro",
            "Macro",
            "Create and manage device macros.",
            "Available",
            $"The shared macro controller is connected. {populated} keyboard sequence(s) are stored.",
            true,
            actions);
    }

    private async Task<FeaturePageState> GetAutomationStateAsync()
    {
        await EnsureAutomationInitializedAsync().ConfigureAwait(false);
        var pipelines = await _automation.GetPipelinesAsync().ConfigureAwait(false);
        var actions = new List<FeatureActionItem>
        {
            new FeatureActionItem(
                "automation-enabled",
                "Automation service",
                "Enable or disable automation event listeners.",
                _automation.IsEnabled ? "Enabled" : "Disabled",
                true,
                _automation.IsEnabled,
                true),
            new FeatureActionItem(
                "pipeline-count",
                "Configured pipelines",
                "Pipelines are loaded from the same automation store used by WPF.",
                pipelines.Count.ToString(CultureInfo.InvariantCulture),
                false,
                false,
                false),
        };

        foreach (var pipeline in pipelines)
        {
            var name = string.IsNullOrWhiteSpace(pipeline.Name)
                ? $"Pipeline {pipeline.Id.ToString()[..8]}"
                : pipeline.Name!;
            var trigger = pipeline.Trigger?.DisplayName ?? "Manual quick action";
            var stepCount = pipeline.Steps.Count;
            actions.Add(new FeatureActionItem(
                $"automation-pipeline:{pipeline.Id:D}",
                name,
                $"{trigger}. {stepCount} step(s). Run this pipeline using the shared automation processor.",
                "Run",
                true,
                false,
                false));
        }

        return new FeaturePageState(
            "Actions",
            "Actions",
            "Review and run configured automation pipelines.",
            "Available",
            $"{pipelines.Count} automation pipeline(s) loaded from the shared settings store.",
            true,
            actions);
    }

    private async Task EnsureAutomationInitializedAsync()
    {
        if (_automationInitialized)
            return;

        await _automationInitializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_automationInitialized)
            {
                await _automation.InitializeAsync().ConfigureAwait(false);
                _automationInitialized = true;
            }
        }
        finally
        {
            _automationInitializationLock.Release();
        }
    }

    private FeaturePageState GetPluginState()
    {
        var registered = _plugins.GetRegisteredPlugins().ToArray();
        var installedIds = _plugins.GetInstalledPluginIds()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var pluginIds = registered.Select(plugin => plugin.Id)
            .Concat(installedIds)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var actions = new List<FeatureActionItem>
        {
            new FeatureActionItem(
                "plugin-refresh",
                "Refresh plugin extensions",
                "Scan the plugin directory and load installed extensions.",
                "Refresh",
                true,
                false,
                false),
            new FeatureActionItem(
                "plugin-check-updates",
                "Check for plugin updates",
                "Ask the shared plugin manager for available updates.",
                "Check",
                true,
                false,
                false),
        };

        foreach (var pluginId in pluginIds)
        {
            var plugin = registered.FirstOrDefault(candidate =>
                candidate.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
            var metadata = _plugins.GetPluginMetadata(pluginId);
            var name = metadata?.GetDisplayName(LocalizationRuntime.CurrentCulture)
                ?? plugin?.Name
                ?? pluginId;
            var description = metadata?.GetDisplayDescription(LocalizationRuntime.CurrentCulture)
                ?? plugin?.Description
                ?? "No plugin description was provided by the host.";
            var version = metadata?.Version;
            var author = metadata?.Author;
            var details = string.Join(" ", new[]
            {
                string.IsNullOrWhiteSpace(version) ? null : $"Version {version}.",
                string.IsNullOrWhiteSpace(author) ? null : $"Author: {author}.",
                description,
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
            var installed = _plugins.IsInstalled(pluginId);
            var systemPlugin = metadata?.IsSystemPlugin == true || plugin?.IsSystemPlugin == true;
            actions.Add(new FeatureActionItem(
                installed ? $"plugin-uninstall:{pluginId}" : $"plugin-install:{pluginId}",
                name,
                details,
                installed ? (systemPlugin ? "System" : "Uninstall") : "Install",
                installed ? !systemPlugin : true,
                false,
                false));
            if (installed)
            {
                if (plugin is not null && HasPluginFeaturePage(plugin))
                {
                    actions.Add(new FeatureActionItem(
                        $"plugin-open:{pluginId}",
                        $"Open {name}",
                        "Open this installed extension in the host plugin page route.",
                        "Open",
                        true,
                        false,
                        false));
                }

                actions.Add(new FeatureActionItem(
                    $"plugin-reload:{pluginId}",
                    $"Reload {name}",
                    "Rescan the plugin directory and reload this installed extension through the shared plugin manager.",
                    "Reload",
                    true,
                    false,
                    false));
            }
        }

        var installedCount = installedIds.Length;
        return new FeaturePageState(
            "PluginExtensions",
            "Plugin Extensions",
            "Discover and manage optional plugin extensions.",
            "Available",
            $"{installedCount} installed plugin extension(s) loaded by the shared plugin manager.",
            true,
            actions);
    }

    public Task<PluginPageState> GetPluginPageStateAsync(string pluginId)
    {
        var normalizedId = pluginId?.Trim() ?? string.Empty;
        var plugin = _plugins.GetRegisteredPlugins()
            .FirstOrDefault(candidate => candidate.Id.Equals(normalizedId, StringComparison.OrdinalIgnoreCase));
        var metadata = string.IsNullOrWhiteSpace(normalizedId)
            ? null
            : _plugins.GetPluginMetadata(normalizedId);
        var title = metadata?.GetDisplayName(LocalizationRuntime.CurrentCulture)
            ?? plugin?.Name
            ?? normalizedId;
        var description = metadata?.GetDisplayDescription(LocalizationRuntime.CurrentCulture)
            ?? plugin?.Description
            ?? "No plugin description was provided by the host.";
        var icon = metadata?.Icon ?? plugin?.Icon;
        var installed = !string.IsNullOrWhiteSpace(normalizedId) && _plugins.IsInstalled(normalizedId);

        if (plugin is null)
        {
            return Task.FromResult(new PluginPageState(
                normalizedId,
                title,
                description,
                icon,
                installed,
                false,
                false,
                "The plugin is not loaded by the host plugin manager."));
        }

        string? pageTitle;
        string? pageIcon;
        Func<object> createPage;
        try
        {
            if (!TryResolvePluginPage(plugin, out pageTitle, out pageIcon, out createPage))
            {
                return Task.FromResult(new PluginPageState(
                    normalizedId,
                    title,
                    description,
                    icon,
                    installed,
                    false,
                    false,
                    "This plugin does not provide a feature page."));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PluginPageState(
                normalizedId,
                title,
                description,
                icon,
                installed,
                true,
                false,
                $"The plugin feature page could not be resolved: {ex.Message}"));
        }

        try
        {
            var content = createPage();
            var isAvaloniaPage = content is Control;
            return Task.FromResult(new PluginPageState(
                normalizedId,
                string.IsNullOrWhiteSpace(pageTitle) ? title : pageTitle,
                description,
                string.IsNullOrWhiteSpace(pageIcon) ? icon : pageIcon,
                installed,
                true,
                isAvaloniaPage,
                isAvaloniaPage
                    ? "The plugin provided an Avalonia page and it is hosted below."
                    : "This plugin provides a WPF page. Avalonia keeps the route visible but cannot embed WPF controls.",
                content));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PluginPageState(
                normalizedId,
                string.IsNullOrWhiteSpace(pageTitle) ? title : pageTitle,
                description,
                string.IsNullOrWhiteSpace(pageIcon) ? icon : pageIcon,
                installed,
                true,
                false,
                $"The plugin page could not be created: {ex.Message}"));
        }
    }

    public Task<PluginPageState> GetPluginSettingsPageStateAsync(string pluginId)
    {
        var normalizedId = pluginId?.Trim() ?? string.Empty;
        var plugin = _plugins.GetRegisteredPlugins()
            .FirstOrDefault(candidate => candidate.Id.Equals(normalizedId, StringComparison.OrdinalIgnoreCase));
        var metadata = string.IsNullOrWhiteSpace(normalizedId)
            ? null
            : _plugins.GetPluginMetadata(normalizedId);
        var title = metadata?.GetDisplayName(LocalizationRuntime.CurrentCulture)
            ?? plugin?.Name
            ?? normalizedId;
        var description = metadata?.GetDisplayDescription(LocalizationRuntime.CurrentCulture)
            ?? plugin?.Description
            ?? "No plugin description was provided by the host.";
        var icon = metadata?.Icon ?? plugin?.Icon;
        var installed = !string.IsNullOrWhiteSpace(normalizedId) && _plugins.IsInstalled(normalizedId);

        if (plugin is null)
        {
            return Task.FromResult(new PluginPageState(
                normalizedId,
                title,
                description,
                icon,
                installed,
                false,
                false,
                "The plugin is not loaded by the host plugin manager."));
        }

        try
        {
            var getSettingsPage = plugin.GetType().GetMethod(
                "GetSettingsPage",
                BindingFlags.Public | BindingFlags.Instance,
                Type.EmptyTypes);
            var settingsPage = getSettingsPage?.Invoke(plugin, null);
            if (settingsPage is null)
            {
                return Task.FromResult(new PluginPageState(
                    normalizedId,
                    title,
                    description,
                    icon,
                    installed,
                    false,
                    false,
                    "This plugin does not provide a settings page."));
            }

            string? pageTitle = null;
            string? pageIcon = null;
            Func<object> createPage;
            if (settingsPage is IPluginPage pluginPage)
            {
                pageTitle = pluginPage.PageTitle;
                pageIcon = pluginPage.PageIcon;
                createPage = pluginPage.CreatePage;
            }
            else if (settingsPage is Control)
            {
                createPage = () => settingsPage;
            }
            else
            {
                var pageType = settingsPage.GetType();
                var createPageMethod = pageType.GetMethod(
                    "CreatePage",
                    BindingFlags.Public | BindingFlags.Instance,
                    Type.EmptyTypes);
                if (createPageMethod is null)
                {
                    return Task.FromResult(new PluginPageState(
                        normalizedId,
                        title,
                        description,
                        icon,
                        installed,
                        true,
                        false,
                        "The plugin settings page format is not supported by the Avalonia host."));
                }

                pageTitle = pageType.GetProperty("PageTitle", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(settingsPage) as string;
                pageIcon = pageType.GetProperty("PageIcon", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(settingsPage) as string;
                createPage = () => createPageMethod.Invoke(settingsPage, null) ?? new object();
            }

            var content = createPage();
            var isAvaloniaPage = content is Control;
            return Task.FromResult(new PluginPageState(
                normalizedId,
                string.IsNullOrWhiteSpace(pageTitle) ? title : pageTitle,
                description,
                string.IsNullOrWhiteSpace(pageIcon) ? icon : pageIcon,
                installed,
                true,
                isAvaloniaPage,
                isAvaloniaPage
                    ? "The plugin settings page is hosted below."
                    : "This plugin provides a WPF settings page. Avalonia keeps the route visible but cannot embed WPF controls.",
                content));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PluginPageState(
                normalizedId,
                title,
                description,
                icon,
                installed,
                true,
                false,
                $"The plugin settings page could not be created: {ex.Message}"));
        }
    }

    private static bool HasPluginFeaturePage(IPlugin plugin)
    {
        try
        {
            return TryResolvePluginPage(plugin, out _, out _, out _);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryResolvePluginPage(
        IPlugin plugin,
        out string? title,
        out string? icon,
        out Func<object> createPage)
    {
        title = null;
        icon = null;
        createPage = null!;

        var getFeatureExtension = plugin.GetType().GetMethod(
            "GetFeatureExtension",
            BindingFlags.Public | BindingFlags.Instance);
        if (getFeatureExtension is null)
            return false;

        object? featureExtension;
        try
        {
            featureExtension = getFeatureExtension.Invoke(plugin, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(ex.InnerException.Message, ex.InnerException);
        }

        if (featureExtension is null)
            return false;

        if (featureExtension is IPluginPage pluginPage)
        {
            title = pluginPage.PageTitle;
            icon = pluginPage.PageIcon;
            createPage = pluginPage.CreatePage;
            return true;
        }

        var pageType = featureExtension.GetType();
        var createPageMethod = pageType.GetMethod(
            "CreatePage",
            BindingFlags.Public | BindingFlags.Instance,
            Type.EmptyTypes);
        if (createPageMethod is null)
            return false;

        title = pageType.GetProperty("PageTitle", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(featureExtension) as string;
        icon = pageType.GetProperty("PageIcon", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(featureExtension) as string;
        createPage = () => createPageMethod.Invoke(featureExtension, null) ?? new object();
        return true;
    }

    private async Task<FeaturePageState> GetOptimizationStateAsync()
    {
        if (_optimization is null)
        {
            return new FeaturePageState(
                "WindowsOptimization",
                "System optimization",
                "Review Windows optimization actions and their current state.",
                "Unavailable on this device",
                "The Windows optimization service could not be resolved by the host container.",
                false,
                []);
        }

        var actions = new List<FeatureActionItem>
        {
            new FeatureActionItem(
                FeatureActionContract.OptimizationApplyRecommendedActionKey,
                "Apply recommended optimizations",
                "Apply all recommended non-cleanup Windows optimization actions as one batch.",
                "Apply",
                true,
                false,
                false,
                "Batch operations"),
            new FeatureActionItem(
                FeatureActionContract.CleanupScanActionKey,
                "Estimate selected cleanup",
                "Estimate the space that the selected cleanup actions can reclaim.",
                _selectedCleanupActions.Count == 0 ? "Select items" : "Scan",
                _selectedCleanupActions.Count > 0,
                false,
                false,
                "Cleanup"),
            new FeatureActionItem(
                FeatureActionContract.CleanupRunActionKey,
                "Run selected cleanup",
                "Execute the selected cleanup actions and clear their saved selection.",
                "Run",
                _selectedCleanupActions.Count > 0,
                false,
                false,
                "Cleanup"),
            new FeatureActionItem(
                FeatureActionContract.CleanupClearActionKey,
                "Clear cleanup selection",
                "Remove all selected cleanup actions without running them.",
                "Clear",
                _selectedCleanupActions.Count > 0,
                false,
                false,
                "Cleanup"),
        };
        foreach (var category in _optimization.GetCategories())
        {
            foreach (var action in category.Actions)
            {
                var isCleanup = FeatureActionContract.IsCleanupAction(action.Key);
                var applied = false;
                if (!isCleanup && action.IsAppliedAsync is not null)
                {
                    try
                    {
                        applied = await action.IsAppliedAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                        // A failed probe keeps the action visible and safely unselected.
                    }
                }

                actions.Add(new FeatureActionItem(
                    action.Key,
                    ResolveResource(action.TitleResourceKey),
                    ResolveResource(action.DescriptionResourceKey),
                    isCleanup
                        ? (_selectedCleanupActions.Contains(action.Key) ? "Selected" : "Select")
                        : applied ? "Applied" : action.Recommended ? "Recommended" : "Available",
                    true,
                    isCleanup ? _selectedCleanupActions.Contains(action.Key) : applied,
                    isCleanup || FeatureActionContract.IsToggleAction(action.RollbackAsync is not null),
                    ResolveResource(category.TitleResourceKey)));
            }
        }

        return new FeaturePageState(
            "WindowsOptimization",
            "System optimization",
            "Review Windows optimization actions and their current state.",
            "Available",
            $"{actions.Count} Windows optimization action(s) loaded from the shared service. "
            + (_estimatedCleanupSize > 0
                ? $"Estimated cleanup: {FormatBytes(_estimatedCleanupSize)}."
                : string.Empty),
            true,
            actions,
            (_applicationSettings?.Store.CustomCleanupRules ?? [])
                .Select(rule => new CustomCleanupRuleItem(
                    rule.DirectoryPath ?? string.Empty,
                    (rule.Extensions ?? []).ToArray(),
                    rule.Recursive))
                .ToArray());
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        var units = new[] { "B", "KB", "MB", "GB", "TB" };
        var value = (double)bytes;
        var index = 0;
        while (value >= 1024 && index < units.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return $"{value:0.##} {units[index]}";
    }

    private static string ResolveResource(string key) =>
        LibResource.ResourceManager.GetString(key, LocalizationRuntime.CurrentCulture) ?? key;
}

internal sealed class AvaloniaMainThreadDispatcher : IMainThreadDispatcher
{
    public void Dispatch(Action callback) => global::Avalonia.Threading.Dispatcher.UIThread.Invoke(callback);

    public Task DispatchAsync(Func<Task> callback)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        global::Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await callback().ConfigureAwait(true);
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });
        return completion.Task;
    }
}

#endif
