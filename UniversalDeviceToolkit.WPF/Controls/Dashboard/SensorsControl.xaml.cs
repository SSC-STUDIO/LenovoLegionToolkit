using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Humanizer;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard
{
public partial class SensorsControl
{
    internal readonly record struct SensorChartSample(double Utilization, double Clock, double Temperature);

    private const string CelsiusUnit = "\u00B0C";
    private const string FahrenheitUnit = "\u00B0F";
    private const string GigahertzUnit = "GHz";
    private const string MegahertzUnit = "MHz";
    private const string RpmUnit = "RPM";
    private const double AutoExpandedDetailsMinWidth = 1120;
    private const double AutoExpandedDetailsTallWidth = 920;
    private const double AutoExpandedDetailsMinHeight = 360;
    private const int SensorChartSampleLimit = 48;
    private const double SensorChartHeight = 108;
    private static readonly TimeSpan DoubleClickThreshold = TimeSpan.FromMilliseconds(500);
    private static readonly object SessionSensorDataLock = new();
    private static SensorsData? _sessionSensorData;

    private readonly ISensorsController _controller = IoCContainer.Resolve<ISensorsController>();
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly DashboardSettings _dashboardSettings = IoCContainer.Resolve<DashboardSettings>();
    private readonly SensorsGroupController? _sensorsGroupController = IoCContainer.TryResolve<SensorsGroupController>();
    private bool _sensorRuntimeAvailable = true;
    private volatile bool _forceDetailedRefresh;
    private bool _detailsExpanded;
    private bool _manualDetailsOverride;
    private DateTime _lastDetailsToggleClick = DateTime.MinValue;
    private readonly Queue<SensorChartSample> _cpuChartSamples = new();
    private readonly Queue<SensorChartSample> _gpuChartSamples = new();

    private CancellationTokenSource? _cts;
    private Task? _refreshTask;

    private CancellationTokenSource? _batteryCts;
    private Task? _batteryRefreshTask;
    private readonly object _initialSensorDataLoadLock = new();
    private TaskCompletionSource _firstSensorDataTaskCompletionSource = CreateInitialSensorDataTaskCompletionSource();
    private bool _hasRenderedSensorData;
    private SensorsData? _lastRenderedSensorData;
    private int _extendedDetailsRefreshVersion;
    private Task? _extendedDetailsRefreshTask;

    private string _cpuName = string.Empty;
    private string _gpuName = string.Empty;

    public SensorsControl()
    {
        InitializeComponent();
        InitializeContextMenu();
        ToolTip = T("SensorsControl_DetailsToggleToolTip", "Double-click to show or hide detailed sensor information.");
        SetInitialSensorPlaceholders();
        InitializeFromSessionCache();
        _ = FetchHardwareNamesAsync();

        IsVisibleChanged += SensorsControl_IsVisibleChanged;
        SizeChanged += SensorsControl_SizeChanged;
    }

    public Task FirstSensorDataReadyTask
    {
        get
        {
            lock (_initialSensorDataLoadLock)
                return _firstSensorDataTaskCompletionSource.Task;
        }
    }

    public Task RestartInitialSensorDataLoad()
    {
        lock (_initialSensorDataLoadLock)
        {
            if (_lastRenderedSensorData is { } data && CanCompleteInitialLoadFromCachedSensorData(data))
            {
                _hasRenderedSensorData = true;
                _firstSensorDataTaskCompletionSource.TrySetResult();
                return _firstSensorDataTaskCompletionSource.Task;
            }

            _hasRenderedSensorData = false;
            if (_firstSensorDataTaskCompletionSource.Task.IsCompleted)
                _firstSensorDataTaskCompletionSource = CreateInitialSensorDataTaskCompletionSource();

            return _firstSensorDataTaskCompletionSource.Task;
        }
    }

    internal static bool HasInitialSummarySensorData(SensorsData data) =>
        HasInitialSummarySensorData(data.CPU) && HasInitialSummarySensorData(data.GPU);

    internal static bool HasAnySummarySensorData(SensorsData data) =>
        HasAnySummarySensorData(data.CPU) || HasAnySummarySensorData(data.GPU);

    internal static bool CanCompleteInitialLoadFromCachedSensorData(SensorsData data) =>
        HasInitialSummarySensorData(data);

    private async Task FetchHardwareNamesAsync()
    {
        try
        {
            if (_sensorsGroupController is not null
                && await _sensorsGroupController.IsSupportedAsync().ConfigureAwait(false) is LibreHardwareMonitorInitialState.Initialized or LibreHardwareMonitorInitialState.Success)
            {
                _cpuName = await _sensorsGroupController.GetCpuNameAsync().ConfigureAwait(false);
                _gpuName = await _sensorsGroupController.GetGpuNameAsync().ConfigureAwait(false);
            }

            _cpuName = NormalizeHardwareNameOrFallback(_cpuName, T("SensorsControl_UnknownCpu", "Unknown CPU"));
            _gpuName = NormalizeHardwareNameOrFallback(_gpuName, T("SensorsControl_UnknownGpu", "Unknown GPU"));
        }
        catch
        {
            _cpuName = T("SensorsControl_UnknownCpu", "Unknown CPU");
            _gpuName = T("SensorsControl_UnknownGpu", "Unknown GPU");
        }

        Dispatcher.Invoke(() =>
        {
            UpdateModelNameText("_cpuModelName", _cpuName);
            UpdateModelNameText("_gpuModelName", _gpuName);
        });
    }

    private void InitializeContextMenu()
    {
        ContextMenu = new ContextMenu();
        ContextMenu.Items.Add(new MenuItem { Header = Resource.SensorsControl_RefreshInterval, IsEnabled = false });

        foreach (var interval in new[] { 1, 2, 3, 5 })
        {
            var item = new MenuItem
            {
                Header = TimeSpan.FromSeconds(interval).Humanize(culture: Resource.Culture)
            };
            if (_dashboardSettings.Store.SensorsRefreshIntervalSeconds == interval)
                item.Icon = new SymbolIcon { Symbol = SymbolRegular.Checkmark24 };

            item.Click += (_, _) =>
            {
                _dashboardSettings.Store.SensorsRefreshIntervalSeconds = interval;
                _dashboardSettings.SynchronizeStore();
                InitializeContextMenu();
            };
            ContextMenu.Items.Add(item);
        }
    }

    private async void SensorsControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            ApplyAutomaticDetailExpansion();
            Refresh();
            RefreshBattery();
            return;
        }

        if (_cts is not null)
            await _cts.CancelAsync();
        _cts = null;

        if (_refreshTask is not null)
            await _refreshTask;
        _refreshTask = null;

        if (_batteryCts is not null)
            await _batteryCts.CancelAsync();
        _batteryCts = null;

        if (_batteryRefreshTask is not null)
            await _batteryRefreshTask;
        _batteryRefreshTask = null;
    }

    private void SensorsControl_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyAutomaticDetailExpansion();

    private void ApplyAutomaticDetailExpansion()
    {
        if (_manualDetailsOverride)
            return;

        SetDetailsExpanded(ShouldAutoExpandDetails(), refreshDetailedValues: IsVisible);
    }

    internal static bool ShouldAutoExpandDetails(double width) =>
        ShouldAutoExpandDetails(width, height: 0);

    internal static bool ShouldAutoExpandDetails(double width, double height) =>
        width >= AutoExpandedDetailsMinWidth ||
        (width >= AutoExpandedDetailsTallWidth && height >= AutoExpandedDetailsMinHeight);

    private bool ShouldAutoExpandDetails() => ShouldAutoExpandDetails(ActualWidth, ActualHeight);

    private void RefreshBattery()
    {
        _batteryCts?.Cancel();
        _batteryCts = new CancellationTokenSource();

        var token = _batteryCts.Token;

        _batteryRefreshTask = Task.Run(async () =>
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Battery information refresh started...");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var batteryInfo = Battery.GetBatteryInformation();
                    var powerAdapterStatus = await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false);
                    var onBatterySince = Battery.GetOnBatterySince();
                    Dispatcher.Invoke(() => SetBattery(batteryInfo, powerAdapterStatus, onBatterySince));

                    await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }  // Expected when battery refresh is cancelled, no action needed
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Battery information refresh failed.", ex);
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Battery information refresh stopped.");
        }, token);
    }

    private void SetBattery(BatteryInformation batteryInfo, PowerAdapterStatus powerAdapterStatus, DateTime? onBatterySince)
    {
        if (FindName("_batteryPercentageBar") is not System.Windows.Controls.Primitives.RangeBase bar) return;

        bar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
        bar.Value = batteryInfo.BatteryPercentage;
        
        if (FindName("_batteryPercentageLabel") is ContentControl label)
        {
            label.Content = $"{batteryInfo.BatteryPercentage:N0}%";
            label.Foreground = batteryInfo.IsLowBattery 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 196, 0)) 
                : FindResource("TextFillColorPrimaryBrush") as System.Windows.Media.Brush;
        }

        if (FindName("_batteryStatusLabel") is TextBlock statusLabel)
        {
            statusLabel.Text = GetBatteryStatusText(batteryInfo);
            statusLabel.Visibility = (batteryInfo.IsLowBattery || powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // Warnings
        SetVisibility("_lowBatteryWarning", batteryInfo.IsLowBattery);
        SetVisibility("_lowWattageWarning", powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage);

        // Icon
        if (FindName("_batteryIcon") is Wpf.Ui.Controls.SymbolIcon icon)
        {
            icon.Symbol = batteryInfo.IsCharging 
                ? SymbolRegular.BatteryCharge24 
                : GetBatteryIconSymbol(batteryInfo.BatteryPercentage);
        }

        // Details
        UpdateBatteryDetails(batteryInfo, onBatterySince);
        UpdateBatteryCapacityChart(batteryInfo);
    }

    private void UpdateBatteryDetails(BatteryInformation info, DateTime? onBatterySince)
    {
        // Implement logic to update details UI (ProgressBar/Text)
        // This relies on the UI elements being present in the XAML
        // I will implement this assuming the UI structure I will create
        UpdateDetailText("_batteryHealthText", $"{info.BatteryHealth:0.00}%");
        if (FindName("_batteryHealthBar") is System.Windows.Controls.Primitives.RangeBase healthBar) healthBar.Value = info.BatteryHealth;

        if (FindName("_batteryTemperatureBar") is System.Windows.Controls.Primitives.RangeBase tempBar &&
            FindName("_batteryTempText") is ContentControl tempLabel)
        {
            var temperature = info.BatteryTemperatureC ?? -1;
            UpdateValue(tempBar, tempLabel, 60, temperature, GetTemperatureText(info.BatteryTemperatureC));
        }

        if (FindName("_batteryRateBar") is System.Windows.Controls.Primitives.RangeBase rateBar &&
            FindName("_batteryRateText") is ContentControl rateLabel)
        {
            var rateW = Math.Abs(info.DischargeRate / 1000.0);
            // Assuming 100W is max reasonable charge/discharge rate for bar scaling
            UpdateValue(rateBar, rateLabel, 100, rateW, $"{info.DischargeRate / 1000.0:+0.00;-0.00;0.00} W");
        }

        UpdateModelNameText("_batteryModelName", info.ModelName ?? T("SensorsControl_UnknownBattery", "Unknown battery"));
        UpdateBatterySummaryTiles(info);

        // Advanced Details
        UpdateDetailText("_batteryRateRange", $"{info.MinDischargeRate / 1000.0:+0.0;-0.0;0.0} W ~ {info.MaxDischargeRate / 1000.0:+0.0;-0.0;0.0} W");
        
        if (info.DesignCapacity > 0)
        {
             UpdateDetailText("_batteryCap", $"{info.EstimateChargeRemaining / 1000.0:0.00} Wh");
             UpdateDetailText("_batteryFullCap", $"{info.FullChargeCapacity / 1000.0:0.00} Wh");
             UpdateDetailText("_batteryDesignCap", $"{info.DesignCapacity / 1000.0:0.00} Wh");
             
             if (FindName("_batteryCapBar") is System.Windows.Controls.Primitives.RangeBase capBar) 
                capBar.Value = (info.EstimateChargeRemaining / (double)info.DesignCapacity) * 100.0;
             if (FindName("_batteryFullCapBar") is System.Windows.Controls.Primitives.RangeBase fullBar) 
                fullBar.Value = (info.FullChargeCapacity / (double)info.DesignCapacity) * 100.0;
        }

        UpdateDetailText("_batteryCycles", $"{info.CycleCount:N0}");
        UpdateDetailText("_batteryDate", info.ManufactureDate?.ToString(LocalizationHelper.ShortDateFormat) ?? string.Empty);
        UpdateDetailText("_batteryTemperature", FormatNullableTemperature(info.BatteryTemperatureC, _applicationSettings.Store.TemperatureUnit));

    }

    private void UpdateBatteryCapacityChart(BatteryInformation info)
    {
        var chargePercentage = ClampPercentage(info.BatteryPercentage);
        var healthPercentage = ClampPercentage(info.BatteryHealth);
        var fullCapacityPercentage = info.DesignCapacity > 0
            ? ClampPercentage((info.FullChargeCapacity / (double)info.DesignCapacity) * 100.0)
            : healthPercentage;

        SetRangeValue("_batteryCapBar", chargePercentage);
        SetRangeValue("_batteryFullCapBar", fullCapacityPercentage);
        SetRangeValue("_batteryHealthDetailBar", healthPercentage);
        UpdateDetailText("_batteryCapChartText", $"{chargePercentage:0}%");
        UpdateDetailText("_batteryFullCapChartText", $"{fullCapacityPercentage:0}%");
        UpdateDetailText("_batteryHealthChartText", $"{healthPercentage:0}%");
    }

    private void UpdateBatterySummaryTiles(BatteryInformation info)
    {
        var chargePercentage = ClampPercentage(info.BatteryPercentage);
        var healthPercentage = ClampPercentage(info.BatteryHealth);
        var rateWatts = info.DischargeRate / 1000.0;
        var absoluteRateWatts = Math.Abs(rateWatts);

        UpdateSensorTile("_batteryChargeTileText", "_batteryChargeTileBar", $"{chargePercentage:0}%", chargePercentage);
        UpdateSensorTile("_batteryHealthTileText", "_batteryHealthTileBar", $"{healthPercentage:0}%", healthPercentage);
        UpdateSensorTile("_batteryRateTileText", "_batteryRateTileBar", $"{rateWatts:+0.0;-0.0;0.0} W", ScaleToPercentage(absoluteRateWatts, 100));
    }

    private void UpdateDetailText(string name, string? text)
    {
        var displayText = NormalizeDetailValueText(text);
        if (FindName(name) is TextBlock tb) 
        {
            tb.Text = displayText;
        }
        else if (FindName(name) is Label lbl) lbl.Content = displayText;
    }

    private void UpdateOptionalDetailText(string titleName, string valueName, string? text)
    {
        var displayText = NormalizeDetailValueText(text);
        var visible = IsUsefulDetailValue(displayText);

        if (FindName(titleName) is UIElement titleElement)
            titleElement.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        if (FindName(valueName) is TextBlock textBlock)
        {
            textBlock.Text = displayText;
            textBlock.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
        else if (FindName(valueName) is Label label)
        {
            label.Content = displayText;
            label.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
        else if (FindName(valueName) is UIElement valueElement)
        {
            valueElement.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SetVisibility(string name, bool visible)
    {
        if (FindName(name) is UIElement el) el.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string GetBatteryStatusText(BatteryInformation batteryInfo)
    {
        if (batteryInfo.IsCharging)
        {
            return batteryInfo.DischargeRate > 0 
                ? Resource.BatteryPage_ACAdapterConnectedAndCharging 
                : Resource.BatteryPage_ACAdapterConnectedNotCharging;
        }

        if (batteryInfo.BatteryLifeRemaining < 0)
            return Resource.BatteryPage_EstimatingBatteryLife;

        var time = TimeSpan.FromSeconds(batteryInfo.BatteryLifeRemaining).Humanize(2, Resource.Culture);
        return string.Format(Resource.BatteryPage_EstimatedBatteryLifeRemaining, time);
    }

    private static SymbolRegular GetBatteryIconSymbol(double percentage)
    {
        var number = (int)Math.Round(percentage / 10.0);
        return number switch
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
    }

    private void Refresh()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        var token = _cts.Token;

        _refreshTask = Task.Run(async () =>
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Sensors refresh started...");

            if (!await _controller.IsSupportedAsync().ConfigureAwait(false))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Sensors not supported.");

                Dispatcher.Invoke(() =>
                {
                    _sensorRuntimeAvailable = false;
                    SetSensorSectionsVisible(true);
                    ResetSensorValues();
                    CompleteInitialSensorDataLoad();
                });
                return;
            }

            Dispatcher.Invoke(() =>
            {
                _sensorRuntimeAvailable = true;
                SetSensorSectionsVisible(true);
            });

            await _controller.PrepareAsync().ConfigureAwait(false);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var detailed = Dispatcher.Invoke(() => _cpuDetailsPanel.Visibility == Visibility.Visible) || _forceDetailedRefresh;
                    var data = await _controller.GetDataAsync(detailed).ConfigureAwait(false);
                    if (detailed)
                        _forceDetailedRefresh = false;
                    Dispatcher.Invoke(() => UpdateValues(data, completesInitialLoad: true));
                    await Task.Delay(TimeSpan.FromSeconds(_dashboardSettings.Store.SensorsRefreshIntervalSeconds), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when sensors refresh is cancelled, no action needed
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Sensors refresh failed.", ex);
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Sensors refresh stopped.");
        }, token);
    }

    private void UpdateValues(SensorsData data, bool completesInitialLoad = false)
    {
        data = MergeSensorDataForDisplay(data, _lastRenderedSensorData);
        var shouldCompleteInitialLoad = completesInitialLoad && HasInitialSummarySensorData(data);
        _lastRenderedSensorData = data;
        CacheSessionSensorDataForDisplay(data);

        if (shouldCompleteInitialLoad)
            CompleteInitialSensorDataLoad();

        UpdateSensorChart(
            _cpuChartSamples,
            data.CPU,
            _cpuChartCanvas,
            _cpuUtilizationSparkline,
            _cpuClockSparkline,
            _cpuTemperatureSparkline,
            _cpuUtilizationArea,
            _cpuClockArea,
            _cpuTemperatureArea);
        UpdateSensorChart(
            _gpuChartSamples,
            data.GPU,
            _gpuChartCanvas,
            _gpuUtilizationSparkline,
            _gpuClockSparkline,
            _gpuTemperatureSparkline,
            _gpuUtilizationArea,
            _gpuClockArea,
            _gpuTemperatureArea);

        UpdateValue(_cpuUtilizationBar, _cpuUtilizationLabel, data.CPU.MaxUtilization, data.CPU.Utilization,
            $"{data.CPU.Utilization}%");
        UpdateValue(_cpuCoreClockBar, _cpuCoreClockLabel, data.CPU.MaxCoreClock, data.CPU.CoreClock,
            $"{data.CPU.CoreClock / 1000.0:0.0} {GigahertzUnit}", $"{data.CPU.MaxCoreClock / 1000.0:0.0} {GigahertzUnit}");
        UpdateValue(_cpuTemperatureBar, _cpuTemperatureLabel, data.CPU.MaxTemperature, data.CPU.Temperature,
            GetTemperatureText(data.CPU.Temperature), GetTemperatureText(data.CPU.MaxTemperature));
        UpdateValue(_cpuFanSpeedBar, _cpuFanSpeedLabel, data.CPU.MaxFanSpeed, data.CPU.FanSpeed,
            $"{data.CPU.FanSpeed} {RpmUnit}", $"{data.CPU.MaxFanSpeed} {RpmUnit}");
        UpdateSensorChartMetricText(
            "_cpuChartUtilizationText",
            data.CPU.Utilization >= 0 ? $"{data.CPU.Utilization}%" : NotAvailableText());
        UpdateSensorChartMetricText(
            "_cpuChartClockText",
            data.CPU.CoreClock > 0 ? $"{data.CPU.CoreClock / 1000.0:0.0} {GigahertzUnit}" : NotAvailableText());
        UpdateSensorChartMetricText(
            "_cpuChartTemperatureText",
            data.CPU.Temperature > 0 ? GetTemperatureText(data.CPU.Temperature) : NotAvailableText());
        UpdateSensorTile(
            "_cpuLoadTileText",
            "_cpuLoadTileBar",
            data.CPU.Utilization >= 0 ? $"{data.CPU.Utilization}%" : NotAvailableText(),
            ScaleToPercentage(data.CPU.Utilization, data.CPU.MaxUtilization > 0 ? data.CPU.MaxUtilization : 100));
        UpdateSensorTile(
            "_cpuPowerTileText",
            "_cpuPowerTileBar",
            data.CPU.Wattage >= 0 ? FormatPower(data.CPU.Wattage) : NotAvailableText(),
            ScaleToPercentage(data.CPU.Wattage, 140));
        UpdateSensorTile(
            "_cpuThermalTileText",
            "_cpuThermalTileBar",
            data.CPU.Temperature > 0 ? GetTemperatureText(data.CPU.Temperature) : NotAvailableText(),
            ScaleToPercentage(data.CPU.Temperature, data.CPU.MaxTemperature > 0 ? data.CPU.MaxTemperature : 100));

        UpdateOptionalDetailText("_cpuWattageTitle", "_cpuWattage", data.CPU.Wattage >= 0 ? $"{data.CPU.Wattage} W" : NotAvailableText());

        UpdateOptionalDetailText(
            "_cpuTempRangeTitle",
            "_cpuTempRange",
            IsTemperatureRangeAvailable(data.CPU.MinTemperature, data.CPU.MaxTemperatureRecord)
                ? $"{data.CPU.MinTemperature}{CelsiusUnit} ~ {data.CPU.MaxTemperatureRecord}{CelsiusUnit}"
                : NotAvailableText());

        UpdateOptionalDetailText("_cpuVoltageTitle", "_cpuVoltage", data.CPU.Voltage > 0 ? $"{data.CPU.Voltage:0.000} V" : NotAvailableText());

        UpdateOptionalDetailText(
            "_cpuVoltageRangeTitle",
            "_cpuVoltageRange",
            IsVoltageRangeAvailable(data.CPU.MinVoltage, data.CPU.MaxVoltage)
                ? $"{data.CPU.MinVoltage:0.000} V ~ {data.CPU.MaxVoltage:0.000} V"
                : NotAvailableText());

        UpdateValue(_gpuUtilizationBar, _gpuUtilizationLabel, data.GPU.MaxUtilization, data.GPU.Utilization,
            $"{data.GPU.Utilization} %");
        
        // GPU Core Clock (Main view)
        UpdateValue(_gpuCoreClockBar, _gpuCoreClockLabel, data.GPU.MaxCoreClock, data.GPU.CoreClock,
            $"{data.GPU.CoreClock / 1000.0:0.0} {GigahertzUnit}", $"{data.GPU.MaxCoreClock / 1000.0:0.0} {GigahertzUnit}");

        // GPU Memory Clock (Details view)
        if (FindName("_gpuMemoryClockBar") is System.Windows.Controls.Primitives.RangeBase memBar &&
            FindName("_gpuMemoryClockText") is TextBlock memText)
        {
            if (data.GPU.MaxMemoryClock < 0 || data.GPU.MemoryClock < 0)
            {
                memBar.Value = 0;
                memText.Text = "-";
            }
            else
            {
                memBar.Maximum = data.GPU.MaxMemoryClock;
                memBar.Value = data.GPU.MemoryClock;
                memText.Text = $"{data.GPU.MemoryClock} {MegahertzUnit}";
            }
        }

        UpdateValue(_gpuTemperatureBar, _gpuTemperatureLabel, data.GPU.MaxTemperature, data.GPU.Temperature,
            GetTemperatureText(data.GPU.Temperature), GetTemperatureText(data.GPU.MaxTemperature));
        UpdateValue(_gpuFanSpeedBar, _gpuFanSpeedLabel, data.GPU.MaxFanSpeed, data.GPU.FanSpeed,
            $"{data.GPU.FanSpeed} {RpmUnit}", $"{data.GPU.MaxFanSpeed} {RpmUnit}");
        UpdateSensorChartMetricText(
            "_gpuChartUtilizationText",
            data.GPU.Utilization >= 0 ? $"{data.GPU.Utilization}%" : NotAvailableText());
        UpdateSensorChartMetricText(
            "_gpuChartClockText",
            data.GPU.CoreClock > 0 ? $"{data.GPU.CoreClock / 1000.0:0.0} {GigahertzUnit}" : NotAvailableText());
        UpdateSensorChartMetricText(
            "_gpuChartTemperatureText",
            data.GPU.Temperature > 0 ? GetTemperatureText(data.GPU.Temperature) : NotAvailableText());
        UpdateSensorTile(
            "_gpuLoadTileText",
            "_gpuLoadTileBar",
            data.GPU.Utilization >= 0 ? $"{data.GPU.Utilization}%" : NotAvailableText(),
            ScaleToPercentage(data.GPU.Utilization, data.GPU.MaxUtilization > 0 ? data.GPU.MaxUtilization : 100));
        UpdateSensorTile(
            "_gpuPowerTileText",
            "_gpuPowerTileBar",
            data.GPU.Wattage >= 0 ? FormatPower(data.GPU.Wattage) : NotAvailableText(),
            ScaleToPercentage(data.GPU.Wattage, 175));
        UpdateSensorTile(
            "_gpuThermalTileText",
            "_gpuThermalTileBar",
            data.GPU.Temperature > 0 ? GetTemperatureText(data.GPU.Temperature) : NotAvailableText(),
            ScaleToPercentage(data.GPU.Temperature, data.GPU.MaxTemperature > 0 ? data.GPU.MaxTemperature : 100));

        if (FindName("_gpuWattage") is TextBlock gpuWattage)
        {
            gpuWattage.Text = FormatPower(data.GPU.Wattage);
        }
        
        if (FindName("_gpuTempRange") is TextBlock gpuTempRange)
        {
             if (IsTemperatureRangeAvailable(data.GPU.MinTemperature, data.GPU.MaxTemperatureRecord))
                 gpuTempRange.Text = $"{data.GPU.MinTemperature}{CelsiusUnit} ~ {data.GPU.MaxTemperatureRecord}{CelsiusUnit}";
             else
                 gpuTempRange.Text = NotAvailableText();
        }

        if (FindName("_gpuVoltage") is TextBlock gpuVoltage)
        {
            gpuVoltage.Text = data.GPU.Voltage > 0 ? $"{data.GPU.Voltage:0.000} V" : NotAvailableText();
        }
        
        if (FindName("_gpuVoltageRange") is TextBlock gpuVoltageRange)
        {
             if (IsVoltageRangeAvailable(data.GPU.MinVoltage, data.GPU.MaxVoltage))
                 gpuVoltageRange.Text = $"{data.GPU.MinVoltage:0.000} V ~ {data.GPU.MaxVoltage:0.000} V";
             else
                 gpuVoltageRange.Text = NotAvailableText();
        }

        QueueExtendedDetailValuesRefresh();
    }

    private void ResetSensorValues()
    {
        _lastRenderedSensorData = null;
        ClearSessionSensorData();
        UpdateValues(SensorsData.Empty);
    }

    private void InitializeFromSessionCache()
    {
        var cached = TryGetSessionSensorDataForDisplay();
        if (cached is null)
            return;

        UpdateValues(cached.Value);
        if (CanCompleteInitialLoadFromCachedSensorData(cached.Value))
            CompleteInitialSensorDataLoad();
    }

    private void CompleteInitialSensorDataLoad()
    {
        lock (_initialSensorDataLoadLock)
        {
            if (_hasRenderedSensorData)
                return;

            _hasRenderedSensorData = true;
            _firstSensorDataTaskCompletionSource.TrySetResult();
        }
    }

    internal static SensorsData MergeSensorDataForDisplay(SensorsData current, SensorsData? previous) =>
        previous is { } previousData
            ? new SensorsData(
                MergeSensorDataForDisplay(current.CPU, previousData.CPU),
                MergeSensorDataForDisplay(current.GPU, previousData.GPU))
            : current;

    private static SensorData MergeSensorDataForDisplay(SensorData current, SensorData previous)
    {
        var utilization = KeepCurrentOrPrevious(current.Utilization, previous.Utilization, IsNonNegative);
        var coreClock = KeepCurrentOrPrevious(current.CoreClock, previous.CoreClock, IsNonNegative);
        var memoryClock = KeepCurrentOrPrevious(current.MemoryClock, previous.MemoryClock, IsNonNegative);
        var temperature = KeepCurrentOrPrevious(current.Temperature, previous.Temperature, IsNonNegative);
        var wattage = KeepCurrentOrPrevious(current.Wattage, previous.Wattage, IsNonNegative);
        var voltage = KeepCurrentOrPrevious(current.Voltage, previous.Voltage, value => value > 0);
        var fanSpeed = KeepCurrentOrPrevious(current.FanSpeed, previous.FanSpeed, IsNonNegative);

        var merged = new SensorData(
            utilization,
            ResolveMaximum(current.MaxUtilization, current.Utilization, previous.MaxUtilization),
            coreClock,
            ResolveMaximum(current.MaxCoreClock, current.CoreClock, previous.MaxCoreClock),
            memoryClock,
            ResolveMaximum(current.MaxMemoryClock, current.MemoryClock, previous.MaxMemoryClock),
            temperature,
            ResolveMaximum(current.MaxTemperature, current.Temperature, previous.MaxTemperature),
            wattage,
            voltage,
            fanSpeed,
            ResolveMaximum(current.MaxFanSpeed, current.FanSpeed, previous.MaxFanSpeed));

        var (minVoltage, maxVoltage) = IsVoltageRangeAvailable(current.MinVoltage, current.MaxVoltage)
            ? (current.MinVoltage, current.MaxVoltage)
            : IsVoltageRangeAvailable(previous.MinVoltage, previous.MaxVoltage)
                ? (previous.MinVoltage, previous.MaxVoltage)
                : (current.MinVoltage, current.MaxVoltage);

        var (minTemperature, maxTemperatureRecord) = IsTemperatureRangeAvailable(current.MinTemperature, current.MaxTemperatureRecord)
            ? (current.MinTemperature, current.MaxTemperatureRecord)
            : IsTemperatureRangeAvailable(previous.MinTemperature, previous.MaxTemperatureRecord)
                ? (previous.MinTemperature, previous.MaxTemperatureRecord)
                : (current.MinTemperature, current.MaxTemperatureRecord);

        return merged.WithMinMax(minVoltage, maxVoltage, minTemperature, maxTemperatureRecord);
    }

    private static int ResolveMaximum(int currentMaximum, int currentValue, int previousMaximum)
    {
        if (IsNonNegative(currentMaximum))
            return currentMaximum;

        if (IsNonNegative(previousMaximum))
            return previousMaximum;

        return IsNonNegative(currentValue) ? Math.Max(currentValue, 1) : currentMaximum;
    }

    private static int KeepCurrentOrPrevious(int current, int previous, Func<int, bool> isValid) =>
        isValid(current) ? current : previous;

    private static double KeepCurrentOrPrevious(double current, double previous, Func<double, bool> isValid) =>
        isValid(current) ? current : previous;

    internal static void CacheSessionSensorDataForDisplay(SensorsData data)
    {
        if (!HasAnySummarySensorData(data))
            return;

        lock (SessionSensorDataLock)
            _sessionSensorData = data;
    }

    internal static SensorsData? TryGetSessionSensorDataForDisplay()
    {
        lock (SessionSensorDataLock)
            return _sessionSensorData;
    }

    private static void ClearSessionSensorData()
    {
        lock (SessionSensorDataLock)
            _sessionSensorData = null;
    }

    private void QueueExtendedDetailValuesRefresh()
    {
        if (!_detailsExpanded || !_sensorRuntimeAvailable || _sensorsGroupController is null)
            return;

        if (_extendedDetailsRefreshTask is { IsCompleted: false })
            return;

        _extendedDetailsRefreshTask = UpdateExtendedDetailValuesAsync();
    }

    private void CardControl_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var now = DateTime.UtcNow;
        if (e.ClickCount >= 2 || now - _lastDetailsToggleClick <= DoubleClickThreshold)
        {
            _lastDetailsToggleClick = DateTime.MinValue;
            ToggleDetails();
            e.Handled = true;
            return;
        }

        _lastDetailsToggleClick = now;
    }

    private void ToggleDetails()
    {
        _manualDetailsOverride = true;
        SetDetailsExpanded(!AreDetailsVisible(), refreshDetailedValues: true);
    }

    private void SetDetailsExpanded(bool expanded, bool refreshDetailedValues)
    {
        _detailsExpanded = expanded;
        var newState = _detailsExpanded ? Visibility.Visible : Visibility.Collapsed;

        if (_sensorRuntimeAvailable)
        {
            SetVisibility("_cpuDetailsPanel", newState == Visibility.Visible);
            SetVisibility("_gpuDetailsPanel", newState == Visibility.Visible);
        }

        SetVisibility("_batteryDetailsPanel", newState == Visibility.Visible);

        if (refreshDetailedValues && newState == Visibility.Visible)
        {
            _forceDetailedRefresh = true;
            _ = RefreshDetailedValuesAsync();
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Sensor details toggled: {newState}.");
    }

    private bool AreDetailsVisible() =>
        IsElementVisible("_batteryDetailsPanel") ||
        (_sensorRuntimeAvailable && (IsElementVisible("_cpuDetailsPanel") || IsElementVisible("_gpuDetailsPanel")));

    private bool IsElementVisible(string name) =>
        FindName(name) is FrameworkElement element && element.Visibility == Visibility.Visible;

    private async Task RefreshDetailedValuesAsync()
    {
        if (!_sensorRuntimeAvailable)
            return;

        try
        {
            var data = await _controller.GetDataAsync(true).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() => UpdateValues(data, completesInitialLoad: true));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Immediate detailed sensors refresh failed.", ex);
        }
    }

    private string GetTemperatureText(double? temperature)
    {
        if (temperature is null)
            return NotAvailableText();

        return FormatTemperature(temperature.Value, _applicationSettings.Store.TemperatureUnit);
    }

    private static void UpdateValue(RangeBase bar, ContentControl label, double max, double value, string text, string? toolTipText = null)
    {
        if (max < 0 || value < 0)
        {
            bar.Minimum = 0;
            bar.Maximum = 1;
            bar.Value = 0;
            label.Content = NotAvailableText();
            label.ToolTip = null;
            label.Tag = 0;
        }
        else
        {
            bar.Minimum = 0;
            bar.Maximum = max;
            bar.Value = value;
            label.Content = text;
            label.ToolTip = toolTipText is null ? null : string.Format(Resource.SensorsControl_Maximum, toolTipText);
            label.Tag = value;
        }
    }

    private void UpdateSensorChart(
        Queue<SensorChartSample> samples,
        SensorData data,
        FrameworkElement chartSurface,
        System.Windows.Shapes.Polyline utilizationLine,
        System.Windows.Shapes.Polyline clockLine,
        System.Windows.Shapes.Polyline temperatureLine,
        System.Windows.Shapes.Polygon utilizationArea,
        System.Windows.Shapes.Polygon clockArea,
        System.Windows.Shapes.Polygon temperatureArea)
    {
        if (TryCreateSensorChartSample(data) is not { } sample)
            return;

        samples.Enqueue(sample);
        while (samples.Count > SensorChartSampleLimit)
            samples.Dequeue();

        var width = chartSurface.ActualWidth;
        if (double.IsNaN(width) || width <= 0)
            width = chartSurface.Width;
        if (double.IsNaN(width) || width <= 0)
            width = 420;

        if (chartSurface is Canvas canvas)
        {
            foreach (var guideLine in canvas.Children.OfType<System.Windows.Shapes.Line>())
                guideLine.X2 = width;
        }

        var points = CreateSensorChartPoints(samples, width, SensorChartHeight);
        utilizationLine.Points = points.utilization;
        clockLine.Points = points.clock;
        temperatureLine.Points = points.temperature;
        utilizationArea.Points = CreateSensorChartAreaPoints(points.utilization, width, SensorChartHeight);
        clockArea.Points = CreateSensorChartAreaPoints(points.clock, width, SensorChartHeight);
        temperatureArea.Points = CreateSensorChartAreaPoints(points.temperature, width, SensorChartHeight);
    }

    private static SensorChartSample? TryCreateSensorChartSample(SensorData data)
    {
        if (data.Utilization < 0 && data.CoreClock <= 0 && data.Temperature <= 0)
            return null;

        var utilization = NormalizeSensorChartMetric(data.Utilization, data.MaxUtilization, fallbackMaximum: 100);
        var clock = NormalizeSensorChartMetric(data.CoreClock, data.MaxCoreClock, fallbackMaximum: Math.Max(data.CoreClock, 1));
        var temperature = NormalizeSensorChartMetric(data.Temperature, data.MaxTemperature, fallbackMaximum: 100);

        if (utilization < 0 && clock < 0 && temperature < 0)
            return null;

        return new(
            utilization >= 0 ? utilization : 0,
            clock >= 0 ? clock : 0,
            temperature >= 0 ? temperature : 0);
    }

    internal static (PointCollection utilization, PointCollection clock, PointCollection temperature) CreateSensorChartPoints(
        IEnumerable<SensorChartSample> samples,
        double width,
        double height)
    {
        var sampleArray = samples.ToArray();
        return (
            CreateMetricChartPoints(sampleArray.Select(static sample => sample.Utilization), width, height),
            CreateMetricChartPoints(sampleArray.Select(static sample => sample.Clock), width, height),
            CreateMetricChartPoints(sampleArray.Select(static sample => sample.Temperature), width, height));
    }

    private static PointCollection CreateMetricChartPoints(IEnumerable<double> values, double width, double height)
    {
        var valueArray = values.ToArray();
        var points = new PointCollection(valueArray.Length);

        if (valueArray.Length == 0)
            return points;

        var usableWidth = Math.Max(width, 1);
        var usableHeight = Math.Max(height, 1);
        var step = valueArray.Length > 1 ? usableWidth / (valueArray.Length - 1) : 0;

        for (var index = 0; index < valueArray.Length; index++)
        {
            var value = ClampPercentage(valueArray[index]);
            var x = valueArray.Length == 1 ? usableWidth : index * step;
            var y = usableHeight - (value / 100.0 * usableHeight);
            points.Add(new Point(x, y));
        }

        return points;
    }

    internal static PointCollection CreateSensorChartAreaPoints(PointCollection linePoints, double width, double height)
    {
        var areaPoints = new PointCollection(linePoints.Count + 2);
        if (linePoints.Count == 0)
            return areaPoints;

        var usableWidth = Math.Max(width, 1);
        var usableHeight = Math.Max(height, 1);
        areaPoints.Add(new Point(0, usableHeight));

        foreach (var point in linePoints)
            areaPoints.Add(point);

        areaPoints.Add(new Point(usableWidth, usableHeight));
        return areaPoints;
    }

    private static double NormalizeSensorChartMetric(double value, double maximum, double fallbackMaximum)
    {
        if (value < 0)
            return -1;

        var resolvedMaximum = maximum > 0 ? maximum : fallbackMaximum;
        if (resolvedMaximum <= 0)
            return -1;

        return ClampPercentage((value / resolvedMaximum) * 100.0);
    }

    private void SetRangeValue(string name, double value)
    {
        if (FindName(name) is RangeBase range)
            range.Value = ClampPercentage(value);
    }

    private void UpdateSensorChartMetricText(string name, string? text)
    {
        if (FindName(name) is TextBlock textBlock)
            textBlock.Text = NormalizeDetailValueText(text);
    }

    private void UpdateSensorTile(string textName, string barName, string text, double percentage)
    {
        UpdateSensorChartMetricText(textName, text);
        SetRangeValue(barName, percentage);
    }

    private static double ScaleToPercentage(double value, double maximum)
    {
        if (value < 0 || maximum <= 0)
            return 0;

        return ClampPercentage(value / maximum * 100.0);
    }

    private static double ClampPercentage(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0;

        return Math.Clamp(value, 0, 100);
    }

    private void SetSensorSectionsVisible(bool visible)
    {
        SetVisibility("_cpuSection", visible);
        SetVisibility("_gpuSection", visible);
        SetVisibility("_cpuGpuSeparatorLeft", visible);
        SetVisibility("_cpuGpuSeparatorRight", visible);

        if (!visible)
        {
            _detailsExpanded = false;
            SetVisibility("_cpuDetailsPanel", false);
            SetVisibility("_gpuDetailsPanel", false);
        }

        if (FindName("_batterySectionColumn") is FrameworkElement batterySection)
            batterySection.Visibility = Visibility.Visible;

        Visibility = Visibility.Visible;
    }

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private static string NotAvailableText() => T("SensorsControl_NotAvailable", "N/A");

    private static TaskCompletionSource CreateInitialSensorDataTaskCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static bool HasAnySummarySensorData(SensorData data) =>
        HasRenderableUtilization(data.Utilization, data.MaxUtilization)
        || data.CoreClock > 0
        || data.Temperature > 0
        || data.FanSpeed > 0;

    private static bool HasInitialSummarySensorData(SensorData data) =>
        HasRenderableUtilization(data.Utilization, data.MaxUtilization)
        && HasRenderablePositiveProgressMetric(data.CoreClock, data.MaxCoreClock)
        && HasRenderablePositiveProgressMetric(data.Temperature, data.MaxTemperature);

    private static bool HasRenderableUtilization(int value, int max) =>
        value >= 0 && max > 0;

    private static bool HasRenderablePositiveProgressMetric(int value, int max) =>
        value > 0 && max > 0;

    private static bool IsNonNegative(int value) => value >= 0;

    private static bool IsTemperatureRangeAvailable(int minTemperature, int maxTemperature) =>
        minTemperature > 0 && maxTemperature > 0;

    private static bool IsVoltageRangeAvailable(double minVoltage, double maxVoltage) =>
        minVoltage > 0 && maxVoltage > 0;

    private void SetInitialSensorPlaceholders()
    {
        SetInitialSensorSummaryPlaceholders();
        UpdateOptionalDetailText("_cpuWattageTitle", "_cpuWattage", NotAvailableText());
        UpdateOptionalDetailText("_cpuVoltageTitle", "_cpuVoltage", NotAvailableText());
        UpdateDetailText("_cpuPCoreClockTitle", T("SensorsControl_PCoreClock_Title", "P Core Clock"));
        UpdateOptionalDetailText("_cpuPCoreClockTitle", "_cpuPCoreClock", NotAvailableText());
        UpdateDetailText("_cpuECoreClockTitle", T("SensorsControl_ECoreClock_Title", "E Core Clock"));
        UpdateOptionalDetailText("_cpuECoreClockTitle", "_cpuECoreClock", NotAvailableText());
        UpdateOptionalDetailText("_cpuTempRangeTitle", "_cpuTempRange", NotAvailableText());
        UpdateOptionalDetailText("_cpuVoltageRangeTitle", "_cpuVoltageRange", NotAvailableText());
        UpdateDetailText("_cpuMemoryUsageTitle", T("SensorsControl_MemoryUsage_Title", "Memory Usage"));
        UpdateOptionalDetailText("_cpuMemoryUsageTitle", "_cpuMemoryUsage", NotAvailableText());
        UpdateDetailText("_cpuMemoryTemperatureTitle", T("SensorsControl_MemoryTemperature_Title", "Memory Temperature"));
        UpdateOptionalDetailText("_cpuMemoryTemperatureTitle", "_cpuMemoryTemperature", NotAvailableText());
        UpdateDetailText("_cpuMotherboardTemperatureTitle", T("SensorsControl_Motherboard_Temperature", "Board Temperature"));
        UpdateOptionalDetailText("_cpuMotherboardTemperatureTitle", "_cpuMotherboardTemperature", NotAvailableText());
        UpdateDetailText("_cpuSsdTemperatureTitle", T("SensorsControl_SsdTemperature_Title", "SSD Temperature"));
        UpdateOptionalDetailText("_cpuSsdTemperatureTitle", "_cpuSsdTemperature", NotAvailableText());
        UpdateDetailText("_gpuWattage", NotAvailableText());
        UpdateDetailText("_gpuVoltage", NotAvailableText());
        UpdateDetailText("_gpuTempRange", NotAvailableText());
        UpdateDetailText("_gpuVoltageRange", NotAvailableText());
        UpdateDetailText("_gpuVramUsageTitle", T("SensorsControl_VramUsage_Title", "VRAM Usage"));
        UpdateDetailText("_gpuVramUsage", NotAvailableText());
        UpdateDetailText("_gpuVramTemperatureTitle", T("SensorsControl_VramTemperature_Title", "VRAM Temperature"));
        UpdateDetailText("_gpuVramTemperature", NotAvailableText());
        UpdateDetailText("_gpuHotSpotTemperatureTitle", T("SensorsControl_GpuHotSpotTemperature_Title", "GPU Hot Spot"));
        UpdateDetailText("_gpuHotSpotTemperature", NotAvailableText());
        UpdateDetailText("_gpuPcieThroughputTitle", T("SensorsControl_GpuPcieThroughput_Title", "PCIe Throughput"));
        UpdateDetailText("_gpuPcieThroughput", NotAvailableText());

        UpdateModelNameText("_cpuModelName", _cpuName);
        UpdateModelNameText("_gpuModelName", _gpuName);
        UpdateModelNameText("_batteryModelName", null);
    }

    private void SetInitialSensorSummaryPlaceholders()
    {
        UpdateValue(_cpuUtilizationBar, _cpuUtilizationLabel, -1, -1, NotAvailableText());
        UpdateValue(_cpuCoreClockBar, _cpuCoreClockLabel, -1, -1, NotAvailableText());
        UpdateValue(_cpuTemperatureBar, _cpuTemperatureLabel, -1, -1, NotAvailableText());
        UpdateValue(_cpuFanSpeedBar, _cpuFanSpeedLabel, -1, -1, NotAvailableText());
        UpdateValue(_gpuUtilizationBar, _gpuUtilizationLabel, -1, -1, NotAvailableText());
        UpdateValue(_gpuCoreClockBar, _gpuCoreClockLabel, -1, -1, NotAvailableText());
        UpdateValue(_gpuTemperatureBar, _gpuTemperatureLabel, -1, -1, NotAvailableText());
        UpdateValue(_gpuFanSpeedBar, _gpuFanSpeedLabel, -1, -1, NotAvailableText());
        UpdateSensorChartMetricText("_cpuChartUtilizationText", NotAvailableText());
        UpdateSensorChartMetricText("_cpuChartClockText", NotAvailableText());
        UpdateSensorChartMetricText("_cpuChartTemperatureText", NotAvailableText());
        UpdateSensorChartMetricText("_gpuChartUtilizationText", NotAvailableText());
        UpdateSensorChartMetricText("_gpuChartClockText", NotAvailableText());
        UpdateSensorChartMetricText("_gpuChartTemperatureText", NotAvailableText());
        UpdateSensorTile("_cpuLoadTileText", "_cpuLoadTileBar", NotAvailableText(), 0);
        UpdateSensorTile("_cpuPowerTileText", "_cpuPowerTileBar", NotAvailableText(), 0);
        UpdateSensorTile("_cpuThermalTileText", "_cpuThermalTileBar", NotAvailableText(), 0);
        UpdateSensorTile("_gpuLoadTileText", "_gpuLoadTileBar", NotAvailableText(), 0);
        UpdateSensorTile("_gpuPowerTileText", "_gpuPowerTileBar", NotAvailableText(), 0);
        UpdateSensorTile("_gpuThermalTileText", "_gpuThermalTileBar", NotAvailableText(), 0);
    }

    private async Task UpdateExtendedDetailValuesAsync()
    {
        if (_sensorsGroupController is null)
            return;

        try
        {
            if (!_detailsExpanded || !_sensorRuntimeAvailable)
                return;

            var refreshVersion = Interlocked.Increment(ref _extendedDetailsRefreshVersion);

            if (!_sensorsGroupController.IsLibreHardwareMonitorInitialized())
                _ = await _sensorsGroupController.IsSupportedAsync().ConfigureAwait(false);

            if (!_sensorsGroupController.IsLibreHardwareMonitorInitialized())
                return;

            await _sensorsGroupController.UpdateAsync().ConfigureAwait(false);

            var gpuVramUsedTask = _sensorsGroupController.GetGpuVramUsedAsync();
            var gpuVramTotalTask = _sensorsGroupController.GetGpuVramTotalAsync();
            var gpuVramUtilizationTask = _sensorsGroupController.GetGpuVramUtilizationAsync();
            var gpuVramTemperatureTask = _sensorsGroupController.GetGpuVramTemperatureAsync();
            var gpuHotSpotTemperatureTask = _sensorsGroupController.GetGpuHotSpotTemperatureAsync();
            var gpuPcieRxThroughputTask = _sensorsGroupController.GetGpuPcieRxThroughputAsync();
            var gpuPcieTxThroughputTask = _sensorsGroupController.GetGpuPcieTxThroughputAsync();
            var gpuIsIntegratedTask = _sensorsGroupController.IsCurrentGpuIntegratedAsync();
            var gpuMemoryClockTask = _sensorsGroupController.GetGpuMemoryClockAsync();
            var gpuPowerTask = _sensorsGroupController.GetGpuPowerAsync();
            var gpuVoltageTask = _sensorsGroupController.GetGpuVoltageAsync();
            var cpuPowerTask = _sensorsGroupController.GetCpuPowerAsync();
            var cpuVoltageTask = _sensorsGroupController.GetCpuVoltageAsync();
            var cpuPCoreClockTask = _sensorsGroupController.GetCpuPCoreClockAsync();
            var cpuECoreClockTask = _sensorsGroupController.GetCpuECoreClockAsync();
            var memoryUsageTask = _sensorsGroupController.GetMemoryUsageAsync();
            var memoryUsedTask = _sensorsGroupController.GetMemoryUsedAsync();
            var memoryTotalTask = _sensorsGroupController.GetMemoryTotalAsync();
            var memoryTemperatureTask = _sensorsGroupController.GetHighestMemoryTemperatureAsync();
            var motherboardTemperatureTask = _sensorsGroupController.GetHighestMotherboardTemperatureAsync();
            var ssdTemperaturesTask = _sensorsGroupController.GetSsdTemperaturesAsync();
            var cpuComponentPowersTask = _sensorsGroupController.GetCpuComponentPowersAsync();

            await Task.WhenAll(
                gpuVramUsedTask,
                gpuVramTotalTask,
                gpuVramUtilizationTask,
                gpuVramTemperatureTask,
                gpuHotSpotTemperatureTask,
                gpuPcieRxThroughputTask,
                gpuPcieTxThroughputTask,
                gpuIsIntegratedTask,
                gpuMemoryClockTask,
                gpuPowerTask,
                gpuVoltageTask,
                cpuPowerTask,
                cpuVoltageTask,
                cpuPCoreClockTask,
                cpuECoreClockTask,
                memoryUsageTask,
                memoryUsedTask,
                memoryTotalTask,
                memoryTemperatureTask,
                motherboardTemperatureTask,
                ssdTemperaturesTask,
                cpuComponentPowersTask).ConfigureAwait(false);

            await Dispatcher.InvokeAsync(() =>
            {
                if (refreshVersion != _extendedDetailsRefreshVersion)
                    return;

                UpdateDetailText("_gpuVramUsageTitle", GetGpuMemoryUsageTitle(gpuIsIntegratedTask.Result));
                UpdateDetailText("_gpuVramUsage", FormatUsageInGigabytes(gpuVramUsedTask.Result, gpuVramTotalTask.Result, gpuVramUtilizationTask.Result));
                UpdateDetailText("_gpuVramTemperature", GetTemperatureText(gpuVramTemperatureTask.Result >= 0 ? (double?)gpuVramTemperatureTask.Result : null));
                UpdateDetailText("_gpuHotSpotTemperature", GetTemperatureText(gpuHotSpotTemperatureTask.Result >= 0 ? (double?)gpuHotSpotTemperatureTask.Result : null));
                UpdateDetailText("_gpuPcieThroughput", FormatThroughputPair(gpuPcieRxThroughputTask.Result, gpuPcieTxThroughputTask.Result));
                UpdateDetailText("_gpuWattage", FormatPowerKeepingPrevious(gpuPowerTask.Result, _gpuWattage.Text));
                UpdateOptionalDetailText("_cpuWattageTitle", "_cpuWattage", FormatCpuPowerBreakdown(cpuPowerTask.Result, cpuComponentPowersTask.Result));
                UpdateOptionalDetailText("_cpuVoltageTitle", "_cpuVoltage", FormatVoltage(cpuVoltageTask.Result));
                UpdateOptionalDetailText("_cpuPCoreClockTitle", "_cpuPCoreClock", FormatFrequency(cpuPCoreClockTask.Result));
                UpdateOptionalDetailText("_cpuECoreClockTitle", "_cpuECoreClock", FormatFrequency(cpuECoreClockTask.Result));
                UpdateOptionalDetailText("_cpuMemoryUsageTitle", "_cpuMemoryUsage", FormatUsageInGigabytes(memoryUsedTask.Result, memoryTotalTask.Result, memoryUsageTask.Result));
                UpdateOptionalDetailText("_cpuMemoryTemperatureTitle", "_cpuMemoryTemperature", GetTemperatureText(memoryTemperatureTask.Result > 0 ? memoryTemperatureTask.Result : null));
                UpdateOptionalDetailText("_cpuMotherboardTemperatureTitle", "_cpuMotherboardTemperature", GetTemperatureText(motherboardTemperatureTask.Result > 0 ? motherboardTemperatureTask.Result : null));
                UpdateOptionalDetailText("_cpuSsdTemperatureTitle", "_cpuSsdTemperature", FormatTemperaturePair(ssdTemperaturesTask.Result, _applicationSettings.Store.TemperatureUnit));
                UpdateDetailText("_gpuVoltage", FormatVoltage(gpuVoltageTask.Result));
                UpdateOptionalDetailText("_cpuTempRangeTitle", "_cpuTempRange", FormatTemperatureRangeText(_cpuTemperatureLabel?.Content?.ToString(), _cpuTempRange.Text));
                UpdateOptionalDetailText("_cpuVoltageRangeTitle", "_cpuVoltageRange", FormatFallbackRangeText(_cpuVoltage.Text, _cpuVoltageRange.Text));
                UpdateDetailText("_gpuTempRange", FormatTemperatureRangeText(_gpuTemperatureLabel?.Content?.ToString(), _gpuTempRange.Text));
                UpdateDetailText("_gpuVoltageRange", FormatFallbackRangeText(_gpuVoltage.Text, _gpuVoltageRange.Text));

                if (FindName("_gpuMemoryClockBar") is RangeBase gpuMemoryClockBar
                    && FindName("_gpuMemoryClockText") is TextBlock gpuMemoryClockText)
                {
                    var memoryClock = gpuMemoryClockTask.Result;
                    if (memoryClock > 0)
                    {
                        gpuMemoryClockBar.Maximum = Math.Max(memoryClock, gpuMemoryClockBar.Maximum);
                        gpuMemoryClockBar.Value = memoryClock;
                        gpuMemoryClockText.Text = $"{memoryClock:0} {MegahertzUnit}";
                    }
                    else
                    {
                        gpuMemoryClockBar.Value = 0;
                        gpuMemoryClockText.Text = NormalizeDetailValueText(null);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Extended sensor detail refresh failed.", ex);
        }
    }

    internal static string GetGpuMemoryUsageTitle(bool isIntegratedGpu) =>
        isIntegratedGpu
            ? T("SensorsControl_SharedMemoryUsage_Title", "Shared Memory Usage")
            : T("SensorsControl_VramUsage_Title", "VRAM Usage");

    internal static string FormatUsageInGigabytes(float usedGb, float totalGb, float percentage = -1f)
    {
        if (usedGb < 0)
        {
            return percentage >= 0
                ? $"{percentage:0}%"
                : NotAvailableText();
        }

        if (totalGb <= 0)
        {
            return percentage >= 0
                ? $"{usedGb:0.0} GB ({percentage:0}%)"
                : $"{usedGb:0.0} GB";
        }

        if (percentage < 0)
            percentage = totalGb > 0 ? (usedGb / totalGb) * 100f : -1f;

        return percentage >= 0
            ? $"{usedGb:0.0} / {totalGb:0.0} GB ({percentage:0}%)"
            : $"{usedGb:0.0} / {totalGb:0.0} GB";
    }

    internal static string FormatTemperaturePair((float first, float second) temperatures, TemperatureUnit temperatureUnit)
    {
        var first = temperatures.first >= 0 ? FormatTemperature(temperatures.first, temperatureUnit) : null;
        var second = temperatures.second >= 0 ? FormatTemperature(temperatures.second, temperatureUnit) : null;

        return (first, second) switch
        {
            ({ } a, { } b) => $"{a} / {b}",
            ({ } a, null) => a,
            (null, { } b) => b,
            _ => NotAvailableText()
        };
    }

    internal static string FormatThroughputPair(float rxBytesPerSecond, float txBytesPerSecond)
    {
        var rx = FormatThroughput(rxBytesPerSecond);
        var tx = FormatThroughput(txBytesPerSecond);

        return (rx, tx) switch
        {
            ({ } a, { } b) => $"Rx {a} / Tx {b}",
            ({ } a, null) => $"Rx {a}",
            (null, { } b) => $"Tx {b}",
            _ => NotAvailableText()
        };
    }

    internal static string FormatCpuPowerBreakdown(float totalWatts, (float cores, float memory, float platform) components)
    {
        var parts = new System.Collections.Generic.List<string>();
        var coresLabel = T("SensorsControl_CpuCoresPower_Label", "Cores");
        var memoryLabel = T("SensorsControl_CpuMemoryPower_Label", "Memory");
        var platformLabel = T("SensorsControl_CpuPlatformPower_Label", "Platform");

        if (totalWatts >= 0)
            parts.Add($"{totalWatts} W");

        if (components.cores > 0)
            parts.Add($"{coresLabel} {components.cores:0.#} W");

        if (components.memory > 0)
            parts.Add($"{memoryLabel} {components.memory:0.#} W");

        if (components.platform > 0)
            parts.Add($"{platformLabel} {components.platform:0.#} W");

        return parts.Count > 0 ? string.Join(" | ", parts) : NotAvailableText();
    }

    internal static string FormatVoltage(float voltage) =>
        voltage > 0 ? $"{voltage:0.000} V" : NotAvailableText();

    internal static string FormatPower(float wattage) =>
        wattage >= 0 ? $"{wattage:0.#} W" : NotAvailableText();

    internal static string FormatPowerKeepingPrevious(float wattage, string? previousText) =>
        wattage >= 0
            ? FormatPower(wattage)
            : !string.IsNullOrWhiteSpace(previousText) && previousText != NotAvailableText()
                ? previousText
                : NotAvailableText();

    internal static string FormatNullableTemperature(double? temperature, TemperatureUnit temperatureUnit) =>
        temperature is { } value ? FormatTemperature(value, temperatureUnit) : NotAvailableText();

    internal static string FormatFrequency(float frequencyMHz) =>
        frequencyMHz > 0 ? $"{frequencyMHz / 1000.0:0.0} {GigahertzUnit}" : NotAvailableText();

    internal static string FormatFallbackRangeText(string? primaryValue, string? existingRangeText)
    {
        if (!string.IsNullOrWhiteSpace(existingRangeText) && existingRangeText != NotAvailableText())
            return existingRangeText;

        return !string.IsNullOrWhiteSpace(primaryValue) && primaryValue != NotAvailableText()
            ? primaryValue
            : NotAvailableText();
    }

    internal static string FormatTemperatureRangeText(string? primaryTemperatureText, string? existingRangeText) =>
        FormatFallbackRangeText(primaryTemperatureText, existingRangeText);

    internal static string NormalizeDetailValueText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "-")
            return NotAvailableText();

        return text;
    }

    internal static bool IsUsefulDetailValue(string? text)
    {
        var normalized = NormalizeDetailValueText(text);
        return !normalized.Equals(NotAvailableText(), StringComparison.OrdinalIgnoreCase)
               && !normalized.Equals("N/A", StringComparison.OrdinalIgnoreCase)
               && !normalized.Equals("Unavailable", StringComparison.OrdinalIgnoreCase)
               && !normalized.Equals("不可用", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FormatThroughput(float bytesPerSecond)
    {
        if (bytesPerSecond < 0)
            return null;

        const float kb = 1024f;
        const float mb = kb * 1024f;
        const float gb = mb * 1024f;

        return bytesPerSecond switch
        {
            >= gb => $"{bytesPerSecond / gb:0.00} GB/s",
            >= mb => $"{bytesPerSecond / mb:0.00} MB/s",
            >= kb => $"{bytesPerSecond / kb:0.00} KB/s",
            _ => $"{bytesPerSecond:0} B/s"
        };
    }

    internal static string FormatTemperature(double temperature, TemperatureUnit temperatureUnit)
    {
        if (temperatureUnit == TemperatureUnit.F)
        {
            temperature *= 9.0 / 5.0;
            temperature += 32;
            return $"{temperature:0} {FahrenheitUnit}";
        }

        return $"{temperature:0} {CelsiusUnit}";
    }

    internal static string? NormalizeModelName(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return null;

        return modelName.Trim();
    }

    internal static string NormalizeHardwareNameOrFallback(string? hardwareName, string fallback)
    {
        var normalized = NormalizeModelName(hardwareName);
        return normalized is null || string.Equals(normalized, "UNKNOWN", StringComparison.OrdinalIgnoreCase)
            ? fallback
            : normalized;
    }

    private void UpdateModelNameText(string elementName, string? modelName)
    {
        if (FindName(elementName) is not TextBlock textBlock)
            return;

        var normalizedModelName = NormalizeModelName(modelName);
        textBlock.Text = normalizedModelName ?? string.Empty;
        textBlock.Visibility = normalizedModelName is null ? Visibility.Collapsed : Visibility.Visible;
    }
}
}
