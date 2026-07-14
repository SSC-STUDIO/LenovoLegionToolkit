using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Controllers.GodMode;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
public partial class StatusWindow
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

        WindowStyle = System.Windows.WindowStyle.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowBackdropType = WindowBackdropType.None;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;

        Focusable = false;
        Topmost = true;
        ExtendsContentIntoTitleBar = true;
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
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
        if (!transform.HasValue)
        {
            Left = 0;
            Top = 0;
            return;
        }

        const double OFFSET = 8;

        var mousePoint = Control.MousePosition;
        var screenRectangle = Screen.FromPoint(mousePoint).WorkingArea;

        var mouse = transform.Value.Transform(new Point(mousePoint.X, mousePoint.Y));
        var screen = transform.Value.Transform(new Vector(screenRectangle.Width, screenRectangle.Height));

        if (mouse.X + OFFSET + ActualWidth > screen.X)
            Left = mouse.X - ActualWidth - OFFSET;
        else
            Left = mouse.X + OFFSET;

        if (mouse.Y + OFFSET + ActualHeight > screen.Y)
            Top = mouse.Y - ActualHeight - OFFSET;
        else
            Top = mouse.Y + OFFSET;
    }

    private void RefreshPowerMode(
        PowerModeState? powerModeState,
        string? godModePresetName,
        (float temperature, float power, float voltage)? cpuSensors,
        (float used, float total, float percentage, double temperature)? memorySensors,
        (float first, float second)? ssdTemperatures)
    {
        _powerModeValueLabel.Content = powerModeState?.GetDisplayName() ?? "-";
        _powerModeValueIndicator.Fill = powerModeState?.GetSolidColorBrush() ?? new(Colors.Transparent);

        if (powerModeState == PowerModeState.GodMode)
        {
            _powerModePresetValueLabel.Content = godModePresetName ?? "-";

            _powerModePresetLabel.Visibility = Visibility.Visible;
            _powerModePresetValueLabel.Visibility = Visibility.Visible;
        }
        else
        {
            _powerModePresetLabel.Visibility = Visibility.Collapsed;
            _powerModePresetValueLabel.Visibility = Visibility.Collapsed;
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
                _gpuGrid.Visibility = Visibility.Collapsed;
                return;
            }

            _gpuTitleLabel.Content = Resource.SensorsControl_GPU_Title;
            _gpuActive.Visibility = Visibility.Collapsed;
            _gpuInactive.Visibility = Visibility.Collapsed;
            _gpuPoweredOff.Visibility = Visibility.Collapsed;
            _gpuPowerStateValue.Visibility = Visibility.Collapsed;
            _gpuPowerStateValueLabel.Visibility = Visibility.Collapsed;
            _gpuPowerStateValueLabel.Content = null;
            RefreshSensorSummary(_gpuSensorsLabel, _gpuSensorsValueLabel, Resource.SensorsControl_GPU_Title, gpuSensors);
            _gpuGrid.Visibility = Visibility.Visible;
            return;
        }

        _gpuTitleLabel.Content = Resource.StatusTrayPopup_DiscreteGPU;

        if (status.Value.State is GPUState.Active or GPUState.MonitorConnected)
        {
            _gpuPowerStateValueLabel.Content = status.Value.PerformanceState ?? "-";

            _gpuActive.Visibility = Visibility.Visible;
            _gpuInactive.Visibility = Visibility.Collapsed;
            _gpuPoweredOff.Visibility = Visibility.Collapsed;
            _gpuPowerStateValue.Visibility = Visibility.Visible;
            _gpuPowerStateValueLabel.Visibility = Visibility.Visible;
        }
        else if (status.Value.State is GPUState.PoweredOff)
        {
            _gpuPowerStateValueLabel.Content = null;

            _gpuActive.Visibility = Visibility.Collapsed;
            _gpuInactive.Visibility = Visibility.Collapsed;
            _gpuPoweredOff.Visibility = Visibility.Visible;
            _gpuPowerStateValue.Visibility = Visibility.Collapsed;
            _gpuPowerStateValueLabel.Visibility = Visibility.Collapsed;
        }
        else
        {
            _gpuPowerStateValueLabel.Content = status.Value.PerformanceState ?? "-";

            _gpuActive.Visibility = Visibility.Collapsed;
            _gpuInactive.Visibility = Visibility.Visible;
            _gpuPoweredOff.Visibility = Visibility.Collapsed;
            _gpuPowerStateValue.Visibility = Visibility.Visible;
            _gpuPowerStateValueLabel.Visibility = Visibility.Visible;
        }

        RefreshSensorSummary(_gpuSensorsLabel, _gpuSensorsValueLabel, Resource.SensorsControl_GPU_Title, gpuSensors);
        _gpuGrid.Visibility = Visibility.Visible;
    }

    private void RefreshBattery(BatteryInformation? batteryInformation, BatteryState? batteryState, DateTime? onBatterySince, bool isCompatibilityMode)
    {
        SetBatteryRateRowsVisibility(!isCompatibilityMode);

        if (!batteryInformation.HasValue || !batteryState.HasValue)
        {
            _batteryIcon.Symbol = SymbolRegular.Battery024;
            _batteryValueLabel.Content = "-";
            _batteryModeValueLabel.Content = "-";
            _batteryDischargeValueLabel.Content = "-";
            _batteryMinDischargeValueLabel.Content = "-";
            _batteryMaxDischargeValueLabel.Content = "-";
            if (_batteryUsageTimeValueLabel != null)
                _batteryUsageTimeValueLabel.Content = "-";
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
            _batteryValueLabel.SetResourceReference(ForegroundProperty, "StatusWarningBrush");

        _batteryIcon.Symbol = symbol;
        _batteryValueLabel.Content = $"{batteryInformation.Value.BatteryPercentage:N0}%";
        _batteryModeValueLabel.Content = batteryState.GetDisplayName();
        _batteryDischargeValueLabel.Content = $"{batteryInformation.Value.DischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        _batteryMinDischargeValueLabel.Content = $"{batteryInformation.Value.MinDischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        _batteryMaxDischargeValueLabel.Content = $"{batteryInformation.Value.MaxDischargeRate / 1000.0:+0.00;-0.00;0.00} W";

        // Add battery usage time with proper formatting
        if (!batteryInformation.Value.IsCharging && onBatterySince.HasValue && _batteryUsageTimeValueLabel != null)
        {
            var duration = DateTime.Now.Subtract(onBatterySince.Value);
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            var seconds = duration.Seconds;
            _batteryUsageTimeValueLabel.Content = $"{hours:N0}:{minutes:D2}:{seconds:D2}";
        }
        else if (_batteryUsageTimeValueLabel != null)
        {
            _batteryUsageTimeValueLabel.Content = "-";
        }
    }

    private void SetBatteryRateRowsVisibility(bool isVisible)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        _batteryDischargeLabel.Visibility = visibility;
        _batteryDischargeValueLabel.Visibility = visibility;
        _batteryMinDischargeLabel.Visibility = visibility;
        _batteryMinDischargeValueLabel.Visibility = visibility;
        _batteryMaxDischargeLabel.Visibility = visibility;
        _batteryMaxDischargeValueLabel.Visibility = visibility;
    }

    private void RefreshUpdate(bool hasUpdate) => _updateIndicator.Visibility = hasUpdate ? Visibility.Visible : Visibility.Collapsed;

    private void RefreshSensorSummary(System.Windows.Controls.Label titleLabel, System.Windows.Controls.Label valueLabel, string title, (float temperature, float power, float voltage)? sensors)
    {
        var text = FormatSensorSummary(sensors, IoCContainer.Resolve<ApplicationSettings>().Store.TemperatureUnit);
        var visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;

        titleLabel.Content = title;
        titleLabel.Visibility = visibility;
        valueLabel.Content = text;
        valueLabel.Visibility = visibility;
    }

    private void RefreshMemorySummary(System.Windows.Controls.Label titleLabel, System.Windows.Controls.Label valueLabel, string title, (float used, float total, float percentage, double temperature)? memorySensors)
    {
        var text = FormatMemorySummary(memorySensors, IoCContainer.Resolve<ApplicationSettings>().Store.TemperatureUnit);
        var visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;

        titleLabel.Content = title;
        titleLabel.Visibility = visibility;
        valueLabel.Content = text;
        valueLabel.Visibility = visibility;
    }

    private void RefreshSsdSummary(System.Windows.Controls.Label titleLabel, System.Windows.Controls.Label valueLabel, string title, (float first, float second)? ssdTemperatures)
    {
        var text = FormatSsdSummary(ssdTemperatures, IoCContainer.Resolve<ApplicationSettings>().Store.TemperatureUnit);
        var visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;

        titleLabel.Content = title;
        titleLabel.Visibility = visibility;
        valueLabel.Content = text;
        valueLabel.Visibility = visibility;
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

