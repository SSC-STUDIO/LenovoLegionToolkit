using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public partial class StatusWindow
{
    private readonly struct StatusWindowData(
        PowerModeState? powerModeState,
        string? godModePresetName,
        GPUStatus? gpuStatus,
        BatteryInformation? batteryInformation,
        BatteryState? batteryState,
        DateTime? onBatterySince,
        bool hasUpdate)
    {
        public PowerModeState? PowerModeState { get; } = powerModeState;
        public string? GodModePresetName { get; } = godModePresetName;
        public GPUStatus? GPUStatus { get; } = gpuStatus;
        public BatteryInformation? BatteryInformation { get; } = batteryInformation;
        public BatteryState? BatteryState { get; } = batteryState;
        public DateTime? OnBatterySince { get; } = onBatterySince;
        public bool HasUpdate { get; } = hasUpdate;
    }

    public static async Task<StatusWindow> CreateAsync() => new(await GetStatusWindowDataAsync());

    private static async Task<StatusWindowData> GetStatusWindowDataAsync()
    {
        var powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
        var godModeController = IoCContainer.Resolve<GodModeController>();
        var gpuController = IoCContainer.Resolve<GPUController>();
        var batteryFeature = IoCContainer.Resolve<BatteryFeature>();
        var updateChecker = IoCContainer.Resolve<UpdateChecker>();

        PowerModeState? state = null;
        string? godModePresetName = null;
        GPUStatus? gpuStatus = null;
        BatteryInformation? batteryInformation = null;
        BatteryState? batteryState = null;
        DateTime? onBatterySince = null;
        var hasUpdate = false;

        try
        {
            if (await powerModeFeature.IsSupportedAsync())
            {
                state = await powerModeFeature.GetStateAsync();

                if (state == PowerModeState.GodMode)
                    godModePresetName = await godModeController.GetActivePresetNameAsync();
            }
        }
        catch { /* Ignored */ }

        try
        {
            if (gpuController.IsSupported())
                gpuStatus = await gpuController.RefreshNowAsync();

        }
        catch { /* Ignored */ }

        try
        {
            batteryInformation = Battery.GetBatteryInformation();
        }
        catch { /* Ignored */ }

        try
        {
            if (await batteryFeature.IsSupportedAsync())
                batteryState = await batteryFeature.GetStateAsync();

        }
        catch { /* Ignored */ }

        try
        {
            onBatterySince = Battery.GetOnBatterySince();
        }
        catch { /* Ignored */ }

        try
        {
            hasUpdate = await updateChecker.CheckAsync(false) is not null;
        }
        catch { /* Ignored */ }

        return new(state, godModePresetName, gpuStatus, batteryInformation, batteryState, onBatterySince, hasUpdate);
    }

    private StatusWindow(StatusWindowData data)
    {
        InitializeComponent();

        Loaded += StatusWindow_Loaded;

        WindowStyle = System.Windows.WindowStyle.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowBackdropType = BackgroundType.None;
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

        RefreshPowerMode(data.PowerModeState, data.GodModePresetName);
        RefreshDiscreteGpu(data.GPUStatus);
        RefreshBattery(data.BatteryInformation, data.BatteryState, data.OnBatterySince);
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

    private void RefreshPowerMode(PowerModeState? powerModeState, string? godModePresetName)
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
    }

    private void RefreshDiscreteGpu(GPUStatus? status)
    {
        if (!status.HasValue)
        {
            _gpuGrid.Visibility = Visibility.Collapsed;
            return;
        }

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

        _gpuGrid.Visibility = Visibility.Visible;
    }

    private void RefreshBattery(BatteryInformation? batteryInformation, BatteryState? batteryState, DateTime? onBatterySince)
    {
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
            _batteryValueLabel.SetResourceReference(ForegroundProperty, "SystemFillColorCautionBrush");

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

    private void RefreshUpdate(bool hasUpdate) => _updateIndicator.Visibility = hasUpdate ? Visibility.Visible : Visibility.Collapsed;
}
