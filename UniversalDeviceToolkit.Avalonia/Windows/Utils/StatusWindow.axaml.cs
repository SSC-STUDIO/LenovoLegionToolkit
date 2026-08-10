using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Windows.Forms;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Controllers.GodMode;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Dashboard;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils
{
public partial class StatusWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private readonly struct StatusWindowData(
        PowerModeState? powerModeState,
        string? godModePresetName,
        (float temperature, float power, float voltage)? cpuSensors,
        (float used, float total, float percentage, double temperature)? memorySensors,
        (float first, float second)? ssdTemperatures,
        GPUStatus? gpuStatus,
        (float temperature, float power, float voltage)? gpuSensors,
        BatteryInformation? batteryInformation,
        BatteryState? batteryState,
        DateTime? onBatterySince,
        bool hasUpdate,
        bool isCompatibilityMode)
    {
        public PowerModeState? PowerModeState { get; } = powerModeState;
        public string? GodModePresetName { get; } = godModePresetName;
        public (float temperature, float power, float voltage)? CpuSensors { get; } = cpuSensors;
        public (float used, float total, float percentage, double temperature)? MemorySensors { get; } = memorySensors;
        public (float first, float second)? SsdTemperatures { get; } = ssdTemperatures;
        public GPUStatus? GPUStatus { get; } = gpuStatus;
        public (float temperature, float power, float voltage)? GpuSensors { get; } = gpuSensors;
        public BatteryInformation? BatteryInformation { get; } = batteryInformation;
        public BatteryState? BatteryState { get; } = batteryState;
        public DateTime? OnBatterySince { get; } = onBatterySince;
        public bool HasUpdate { get; } = hasUpdate;
        public bool IsCompatibilityMode { get; } = isCompatibilityMode;
    }

    public static async Task<StatusWindow> CreateAsync() => new(await GetStatusWindowDataAsync());

    private static async Task<StatusWindowData> GetStatusWindowDataAsync()
    {
        var powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
        var godModeController = IoCContainer.Resolve<GodModeController>();
        var gpuController = IoCContainer.Resolve<GPUController>();
        var sensorsGroupController = IoCContainer.Resolve<SensorsGroupController>();
        var batteryFeature = IoCContainer.Resolve<BatteryFeature>();
        var updateChecker = IoCContainer.Resolve<UpdateChecker>();

        PowerModeState? state = null;
        string? godModePresetName = null;
        (float temperature, float power, float voltage)? cpuSensors = null;
        (float used, float total, float percentage, double temperature)? memorySensors = null;
        (float first, float second)? ssdTemperatures = null;
        GPUStatus? gpuStatus = null;
        (float temperature, float power, float voltage)? gpuSensors = null;
        BatteryInformation? batteryInformation = null;
        BatteryState? batteryState = null;
        DateTime? onBatterySince = null;
        var hasUpdate = false;
        var isCompatibilityMode = false;

        try
        {
            var machineInfo = await MachineCompatibility.GetMachineInformationAsync();
            isCompatibilityMode = !MachineCompatibility.IsSupportedLegionMachine(machineInfo);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to detect compatibility mode for StatusWindow.", ex);
        }

        try
        {
            if (await powerModeFeature.IsSupportedAsync())
            {
                state = await powerModeFeature.GetStateAsync();

                if (state == PowerModeState.GodMode)
                    godModePresetName = await godModeController.GetActivePresetNameAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to get power mode state for StatusWindow.", ex);
        }

        try
        {
            if (await sensorsGroupController.IsSupportedAsync() is LibreHardwareMonitorInitialState.Initialized or LibreHardwareMonitorInitialState.Success)
            {
                await sensorsGroupController.UpdateAsync();
                cpuSensors = (
                    await sensorsGroupController.GetCpuTemperatureAsync(),
                    await sensorsGroupController.GetCpuPowerAsync(),
                    await sensorsGroupController.GetCpuVoltageAsync());
                memorySensors = (
                    await sensorsGroupController.GetMemoryUsedAsync(),
                    await sensorsGroupController.GetMemoryTotalAsync(),
                    await sensorsGroupController.GetMemoryUsageAsync(),
                    await sensorsGroupController.GetHighestMemoryTemperatureAsync());
                ssdTemperatures = await sensorsGroupController.GetSsdTemperaturesAsync();
                gpuSensors = (
                    await sensorsGroupController.GetGpuTemperatureAsync(),
                    await sensorsGroupController.GetGpuPowerAsync(),
                    await sensorsGroupController.GetGpuVoltageAsync());
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to get sensor data for StatusWindow.", ex);
        }

        try
        {
            if (await gpuController.IsSupportedAsync())
                gpuStatus = await gpuController.RefreshNowAsync();

        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to refresh GPU status for StatusWindow.", ex);
        }

        try
        {
            batteryInformation = Battery.GetBatteryInformation();
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to get battery information for StatusWindow.", ex);
        }

        try
        {
            if (await batteryFeature.IsSupportedAsync())
                batteryState = await batteryFeature.GetStateAsync();

        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to get battery state for StatusWindow.", ex);
        }

        try
        {
            onBatterySince = Battery.GetOnBatterySince();
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to get battery usage time for StatusWindow.", ex);
        }

        try
        {
            hasUpdate = await updateChecker.CheckAsync(false) is not null;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to check for updates for StatusWindow.", ex);
        }

        return new(state, godModePresetName, cpuSensors, memorySensors, ssdTemperatures, gpuStatus, gpuSensors, batteryInformation, batteryState, onBatterySince, hasUpdate, isCompatibilityMode);
    }

    private StatusWindow(StatusWindowData data)
    {
        InitializeComponent();

        Loaded += StatusWindow_Loaded;

        SystemDecorations = SystemDecorations.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        CanResize = false;
        SizeToContent = SizeToContent.WidthAndHeight;

        Focusable = false;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;

#if DEBUG
        _title.Text += " [DEBUG]";
#else
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        if (version == new Version(0, 0, 1, 0) || version?.Build == 99)
            _title.Text += " [BETA]";
#endif

        if (Log.Instance.IsTraceEnabled)
            _title.Text += " [LOGGING ENABLED]";

        RefreshPowerMode(data.PowerModeState, data.GodModePresetName, data.CpuSensors, data.MemorySensors, data.SsdTemperatures);
        RefreshDiscreteGpu(data.GPUStatus, data.GpuSensors);
        RefreshBattery(data.BatteryInformation, data.BatteryState, data.OnBatterySince, data.IsCompatibilityMode);
        RefreshUpdate(data.HasUpdate);
    }

    private void StatusWindow_Loaded(object sender, RoutedEventArgs e) => MoveBottomRightEdgeOfWindowToMousePosition();

    private void MoveBottomRightEdgeOfWindowToMousePosition()
    {
        const double OFFSET = 8;

        ScreenHelper.UpdateScreenInfos();
        if (ScreenHelper.PrimaryScreen is not { } primary)
            return;

        // WinForms reports the cursor in physical pixels; Avalonia layout uses 96-DPI units.
        var mousePoint = System.Windows.Forms.Control.MousePosition;
        var dpiScale = primary.DpiX / 96d;
        var mouseX = mousePoint.X / dpiScale;
        var mouseY = mousePoint.Y / dpiScale;
        var workArea = primary.WorkArea;
        var workAreaWidth = workArea.Width;
        var workAreaHeight = workArea.Height;

        if (mouseX + OFFSET + Bounds.Width > workAreaWidth)
            Position = new PixelPoint((int)(mouseX - Bounds.Width - OFFSET), Position.Y);
        else
            Position = new PixelPoint((int)(mouseX + OFFSET), Position.Y);

        if (mouseY + OFFSET + Bounds.Height > workAreaHeight)
            Position = new PixelPoint(Position.X, (int)(mouseY - Bounds.Height - OFFSET));
        else
            Position = new PixelPoint(Position.X, (int)(mouseY + OFFSET));
    }

    private void RefreshPowerMode(
        PowerModeState? powerModeState,
        string? godModePresetName,
        (float temperature, float power, float voltage)? cpuSensors,
        (float used, float total, float percentage, double temperature)? memorySensors,
        (float first, float second)? ssdTemperatures)
    {
        _powerModeValueLabel.Text = powerModeState?.GetDisplayName() ?? "-";
        _powerModeValueIndicator.Fill = powerModeState?.GetSolidColorBrush() ?? new SolidColorBrush(Colors.Transparent);

        if (powerModeState == PowerModeState.GodMode)
        {
            _powerModePresetValueLabel.Text = godModePresetName ?? "-";

            _powerModePresetLabel.IsVisible = true;
            _powerModePresetValueLabel.IsVisible = true;
        }
        else
        {
            _powerModePresetLabel.IsVisible = false;
            _powerModePresetValueLabel.IsVisible = false;
        }

        RefreshSensorSummary(_cpuSensorsLabel, _cpuSensorsValueLabel, Resource.SensorsControl_CPU_Title, cpuSensors);
        RefreshMemorySummary(_memoryLabel, _memoryValueLabel, Resource.DeviceInformationWindow_Memory_Title, memorySensors);
        RefreshSsdSummary(_ssdTemperatureLabel, _ssdTemperatureValueLabel, T("SensorsControl_SsdTemperature_Title", "SSD Temperature"), ssdTemperatures);
    }

    private void RefreshDiscreteGpu(GPUStatus? status, (float temperature, float power, float voltage)? gpuSensors)
    {
        if (!status.HasValue)
        {
            var hasSensorSummary = !string.IsNullOrWhiteSpace(FormatSensorSummary(gpuSensors, IoCContainer.Resolve<ApplicationSettings>().Store.TemperatureUnit));
            if (!hasSensorSummary)
            {
                _gpuGrid.IsVisible = false;
                return;
            }

            _gpuTitleLabel.Text = Resource.SensorsControl_GPU_Title;
            _gpuActive.IsVisible = false;
            _gpuInactive.IsVisible = false;
            _gpuPoweredOff.IsVisible = false;
            _gpuPowerStateValue.IsVisible = false;
            _gpuPowerStateValueLabel.IsVisible = false;
            _gpuPowerStateValueLabel.Text = null;
            RefreshSensorSummary(_gpuSensorsLabel, _gpuSensorsValueLabel, Resource.SensorsControl_GPU_Title, gpuSensors);
            _gpuGrid.IsVisible = true;
            return;
        }

        _gpuTitleLabel.Text = Resource.StatusTrayPopup_DiscreteGPU;

        if (status.Value.State is GPUState.Active or GPUState.MonitorConnected)
        {
            _gpuPowerStateValueLabel.Text = status.Value.PerformanceState ?? "-";

            _gpuActive.IsVisible = true;
            _gpuInactive.IsVisible = false;
            _gpuPoweredOff.IsVisible = false;
            _gpuPowerStateValue.IsVisible = true;
            _gpuPowerStateValueLabel.IsVisible = true;
        }
        else if (status.Value.State is GPUState.PoweredOff)
        {
            _gpuPowerStateValueLabel.Text = null;

            _gpuActive.IsVisible = false;
            _gpuInactive.IsVisible = false;
            _gpuPoweredOff.IsVisible = true;
            _gpuPowerStateValue.IsVisible = false;
            _gpuPowerStateValueLabel.IsVisible = false;
        }
        else
        {
            _gpuPowerStateValueLabel.Text = status.Value.PerformanceState ?? "-";

            _gpuActive.IsVisible = false;
            _gpuInactive.IsVisible = true;
            _gpuPoweredOff.IsVisible = false;
            _gpuPowerStateValue.IsVisible = true;
            _gpuPowerStateValueLabel.IsVisible = true;
        }

        RefreshSensorSummary(_gpuSensorsLabel, _gpuSensorsValueLabel, Resource.SensorsControl_GPU_Title, gpuSensors);
        _gpuGrid.IsVisible = true;
    }

    private void RefreshBattery(BatteryInformation? batteryInformation, BatteryState? batteryState, DateTime? onBatterySince, bool isCompatibilityMode)
    {
        SetBatteryRateRowsVisibility(!isCompatibilityMode);

        if (!batteryInformation.HasValue || !batteryState.HasValue)
        {
            _batteryIcon.Symbol = SymbolRegular.Battery024;
            _batteryValueLabel.Text = "-";
            _batteryModeValueLabel.Text = "-";
            _batteryDischargeValueLabel.Text = "-";
            _batteryMinDischargeValueLabel.Text = "-";
            _batteryMaxDischargeValueLabel.Text = "-";
            if (_batteryUsageTimeValueLabel != null)
                _batteryUsageTimeValueLabel.Text = "-";
            return;
        }

        var symbol = (int)Math.Round(batteryInformation.Value.BatteryPercentage / 10.0) switch
        {
            10 => SymbolRegular.Battery1024,
            9 => SymbolRegular.Battery924,
            8 => SymbolRegular.Battery824,
            7 => SymbolRegular.Battery724,
            6 => SymbolRegular.Battery624,
            5 => SymbolRegular.Battery524,
            4 => SymbolRegular.Battery424,
            3 => SymbolRegular.Battery324,
            2 => SymbolRegular.Battery224,
            1 => SymbolRegular.Battery124,
            _ => SymbolRegular.Battery024,
        };

        if (batteryInformation.Value.IsCharging)
            symbol = batteryState == BatteryState.Conservation ? SymbolRegular.BatterySaver24 : SymbolRegular.BatteryCharge24;

        if (batteryInformation.Value.IsLowBattery)
            _batteryValueLabel.SetResourceReference(TextBlock.ForegroundProperty, "StatusWarningBrush");

        _batteryIcon.Symbol = symbol;
        _batteryValueLabel.Text = $"{batteryInformation.Value.BatteryPercentage:N0}%";
        _batteryModeValueLabel.Text = batteryState.GetDisplayName();
        _batteryDischargeValueLabel.Text = $"{batteryInformation.Value.DischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        _batteryMinDischargeValueLabel.Text = $"{batteryInformation.Value.MinDischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        _batteryMaxDischargeValueLabel.Text = $"{batteryInformation.Value.MaxDischargeRate / 1000.0:+0.00;-0.00;0.00} W";

        // Add battery usage time with proper formatting
        if (!batteryInformation.Value.IsCharging && onBatterySince.HasValue && _batteryUsageTimeValueLabel != null)
        {
            var duration = DateTime.Now.Subtract(onBatterySince.Value);
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            var seconds = duration.Seconds;
            _batteryUsageTimeValueLabel.Text = $"{hours:N0}:{minutes:D2}:{seconds:D2}";
        }
        else if (_batteryUsageTimeValueLabel != null)
        {
            _batteryUsageTimeValueLabel.Text = "-";
        }
    }

    private void SetBatteryRateRowsVisibility(bool isVisible)
    {
        var visibility = isVisible ? true : false;
        _batteryDischargeLabel.IsVisible = visibility;
        _batteryDischargeValueLabel.IsVisible = visibility;
        _batteryMinDischargeLabel.IsVisible = visibility;
        _batteryMinDischargeValueLabel.IsVisible = visibility;
        _batteryMaxDischargeLabel.IsVisible = visibility;
        _batteryMaxDischargeValueLabel.IsVisible = visibility;
    }

    private void RefreshUpdate(bool hasUpdate) => _updateIndicator.IsVisible = hasUpdate ? true : false;

    private void RefreshSensorSummary(TextBlock titleLabel, TextBlock valueLabel, string title, (float temperature, float power, float voltage)? sensors)
    {
        var text = FormatSensorSummary(sensors, IoCContainer.Resolve<ApplicationSettings>().Store.TemperatureUnit);
        var visibility = string.IsNullOrWhiteSpace(text) ? false : true;

        titleLabel.Text = title;
        titleLabel.IsVisible = visibility;
        valueLabel.Text = text;
        valueLabel.IsVisible = visibility;
    }

    private void RefreshMemorySummary(TextBlock titleLabel, TextBlock valueLabel, string title, (float used, float total, float percentage, double temperature)? memorySensors)
    {
        var text = FormatMemorySummary(memorySensors, IoCContainer.Resolve<ApplicationSettings>().Store.TemperatureUnit);
        var visibility = string.IsNullOrWhiteSpace(text) ? false : true;

        titleLabel.Text = title;
        titleLabel.IsVisible = visibility;
        valueLabel.Text = text;
        valueLabel.IsVisible = visibility;
    }

    private void RefreshSsdSummary(TextBlock titleLabel, TextBlock valueLabel, string title, (float first, float second)? ssdTemperatures)
    {
        var text = FormatSsdSummary(ssdTemperatures, IoCContainer.Resolve<ApplicationSettings>().Store.TemperatureUnit);
        var visibility = string.IsNullOrWhiteSpace(text) ? false : true;

        titleLabel.Text = title;
        titleLabel.IsVisible = visibility;
        valueLabel.Text = text;
        valueLabel.IsVisible = visibility;
    }

    internal static string? FormatSensorSummary((float temperature, float power, float voltage)? sensors, TemperatureUnit temperatureUnit)
    {
        if (!sensors.HasValue)
            return null;

        var temperature = sensors.Value.temperature >= 0
            ? SensorsControl.FormatTemperature(sensors.Value.temperature, temperatureUnit)
            : null;
        var power = sensors.Value.power >= 0
            ? SensorsControl.FormatPower(sensors.Value.power)
            : null;
        var voltage = sensors.Value.voltage >= 0
            ? SensorsControl.FormatVoltage(sensors.Value.voltage)
            : null;
        var parts = new[] { temperature, power, voltage }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0
            ? null
            : string.Join(" | ", parts);
    }

    internal static string? FormatMemorySummary((float used, float total, float percentage, double temperature)? memorySensors, TemperatureUnit temperatureUnit)
    {
        if (!memorySensors.HasValue)
            return null;

        var usage = SensorsControl.FormatUsageInGigabytes(memorySensors.Value.used, memorySensors.Value.total, memorySensors.Value.percentage);
        var temperature = memorySensors.Value.temperature > 0
            ? SensorsControl.FormatTemperature((float)memorySensors.Value.temperature, temperatureUnit)
            : null;

        var parts = new[] { usage, temperature }
            .Where(SensorsControl.IsUsefulDetailValue)
            .ToArray();

        return parts.Length == 0
            ? null
            : string.Join(" | ", parts);
    }

    internal static string? FormatSsdSummary((float first, float second)? ssdTemperatures, TemperatureUnit temperatureUnit)
    {
        if (!ssdTemperatures.HasValue)
            return null;

        var text = SensorsControl.FormatTemperaturePair(ssdTemperatures.Value, temperatureUnit);
        return SensorsControl.IsUsefulDetailValue(text)
            ? text
            : null;
    }
}
}
