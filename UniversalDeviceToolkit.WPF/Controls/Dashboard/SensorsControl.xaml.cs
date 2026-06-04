using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    private const string CelsiusUnit = "\u00B0C";
    private const string FahrenheitUnit = "\u00B0F";
    private const string GigahertzUnit = "GHz";
    private const string MegahertzUnit = "MHz";
    private const string RpmUnit = "RPM";
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
    private DateTime _lastDetailsToggleClick = DateTime.MinValue;

    private CancellationTokenSource? _cts;
    private Task? _refreshTask;

    private CancellationTokenSource? _batteryCts;
    private Task? _batteryRefreshTask;
    private readonly TaskCompletionSource _firstSensorDataTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
    }

    public Task FirstSensorDataReadyTask => _firstSensorDataTaskCompletionSource.Task;

    internal static bool HasSummarySensorData(SensorsData data) =>
        HasSummarySensorData(data.CPU) && HasSummarySensorData(data.GPU);

    internal static bool HasAnySummarySensorData(SensorsData data) =>
        HasSummarySensorData(data.CPU) || HasSummarySensorData(data.GPU);

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

        if (FindName("_batteryStatusLabel") is ContentControl statusLabel)
        {
            statusLabel.Content = GetBatteryStatusText(batteryInfo);
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

    private void UpdateDetailText(string name, string text)
    {
        if (FindName(name) is TextBlock tb) 
        {
            tb.Text = text == "-" ? string.Empty : text;
        }
        else if (FindName(name) is Label lbl) lbl.Content = text == "-" ? string.Empty : text;
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
                    _firstSensorDataTaskCompletionSource.TrySetResult();
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
        var shouldCompleteInitialLoad = completesInitialLoad && HasSummarySensorData(data);
        _lastRenderedSensorData = data;
        CacheSessionSensorDataForDisplay(data);

        if (!_hasRenderedSensorData && shouldCompleteInitialLoad)
        {
            _hasRenderedSensorData = true;
            _firstSensorDataTaskCompletionSource.TrySetResult();
        }

        UpdateValue(_cpuUtilizationBar, _cpuUtilizationLabel, data.CPU.MaxUtilization, data.CPU.Utilization,
            $"{data.CPU.Utilization}%");
        UpdateValue(_cpuCoreClockBar, _cpuCoreClockLabel, data.CPU.MaxCoreClock, data.CPU.CoreClock,
            $"{data.CPU.CoreClock / 1000.0:0.0} {GigahertzUnit}", $"{data.CPU.MaxCoreClock / 1000.0:0.0} {GigahertzUnit}");
        UpdateValue(_cpuTemperatureBar, _cpuTemperatureLabel, data.CPU.MaxTemperature, data.CPU.Temperature,
            GetTemperatureText(data.CPU.Temperature), GetTemperatureText(data.CPU.MaxTemperature));
        UpdateValue(_cpuFanSpeedBar, _cpuFanSpeedLabel, data.CPU.MaxFanSpeed, data.CPU.FanSpeed,
            $"{data.CPU.FanSpeed} {RpmUnit}", $"{data.CPU.MaxFanSpeed} {RpmUnit}");

        if (FindName("_cpuWattage") is TextBlock cpuWattage)
        {
            cpuWattage.Text = data.CPU.Wattage >= 0 ? $"{data.CPU.Wattage} W" : NotAvailableText();
        }

        if (FindName("_cpuTempRange") is TextBlock cpuTempRange)
        {
             if (IsTemperatureRangeAvailable(data.CPU.MinTemperature, data.CPU.MaxTemperatureRecord))
                 cpuTempRange.Text = $"{data.CPU.MinTemperature}{CelsiusUnit} ~ {data.CPU.MaxTemperatureRecord}{CelsiusUnit}";
             else
                 cpuTempRange.Text = NotAvailableText();
        }

        if (FindName("_cpuVoltage") is TextBlock cpuVoltage)
        {
            cpuVoltage.Text = data.CPU.Voltage > 0 ? $"{data.CPU.Voltage:0.000} V" : NotAvailableText();
        }

        if (FindName("_cpuVoltageRange") is TextBlock cpuVoltageRange)
        {
             if (IsVoltageRangeAvailable(data.CPU.MinVoltage, data.CPU.MaxVoltage))
                 cpuVoltageRange.Text = $"{data.CPU.MinVoltage:0.000} V ~ {data.CPU.MaxVoltage:0.000} V";
             else
                 cpuVoltageRange.Text = NotAvailableText();
        }

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
        _detailsExpanded = !AreDetailsVisible();
        var newState = _detailsExpanded ? Visibility.Visible : Visibility.Collapsed;

        if (_sensorRuntimeAvailable)
        {
            SetVisibility("_cpuDetailsPanel", newState == Visibility.Visible);
            SetVisibility("_gpuDetailsPanel", newState == Visibility.Visible);
        }

        SetVisibility("_batteryDetailsPanel", newState == Visibility.Visible);

        if (newState == Visibility.Visible)
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
            return "-";

        return FormatTemperature(temperature.Value, _applicationSettings.Store.TemperatureUnit);
    }

    private static void UpdateValue(RangeBase bar, ContentControl label, double max, double value, string text, string? toolTipText = null)
    {
        if (max < 0 || value < 0)
        {
            bar.Minimum = 0;
            bar.Maximum = 1;
            bar.Value = 0;
            label.Content = "-";
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

    private static bool HasSummarySensorData(SensorData data) =>
        data.Utilization >= 0
        || data.CoreClock >= 0
        || data.Temperature >= 0
        || data.FanSpeed >= 0;

    private static bool IsNonNegative(int value) => value >= 0;

    private static bool IsTemperatureRangeAvailable(int minTemperature, int maxTemperature) =>
        minTemperature > 0 && maxTemperature > 0;

    private static bool IsVoltageRangeAvailable(double minVoltage, double maxVoltage) =>
        minVoltage > 0 && maxVoltage > 0;

    private void SetInitialSensorPlaceholders()
    {
        UpdateDetailText("_cpuWattage", NotAvailableText());
        UpdateDetailText("_cpuVoltage", NotAvailableText());
        UpdateDetailText("_cpuPCoreClockTitle", T("SensorsControl_PCoreClock_Title", "P Core Clock"));
        UpdateDetailText("_cpuPCoreClock", NotAvailableText());
        UpdateDetailText("_cpuECoreClockTitle", T("SensorsControl_ECoreClock_Title", "E Core Clock"));
        UpdateDetailText("_cpuECoreClock", NotAvailableText());
        UpdateDetailText("_cpuTempRange", NotAvailableText());
        UpdateDetailText("_cpuVoltageRange", NotAvailableText());
        UpdateDetailText("_cpuMemoryUsageTitle", T("SensorsControl_MemoryUsage_Title", "Memory Usage"));
        UpdateDetailText("_cpuMemoryUsage", NotAvailableText());
        UpdateDetailText("_cpuMemoryTemperatureTitle", T("SensorsControl_MemoryTemperature_Title", "Memory Temperature"));
        UpdateDetailText("_cpuMemoryTemperature", NotAvailableText());
        UpdateDetailText("_cpuSsdTemperatureTitle", T("SensorsControl_SsdTemperature_Title", "SSD Temperature"));
        UpdateDetailText("_cpuSsdTemperature", NotAvailableText());
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
                UpdateDetailText("_cpuWattage", FormatCpuPowerBreakdown(cpuPowerTask.Result, cpuComponentPowersTask.Result));
                UpdateDetailText("_cpuVoltage", FormatVoltage(cpuVoltageTask.Result));
                UpdateDetailText("_cpuPCoreClock", FormatFrequency(cpuPCoreClockTask.Result));
                UpdateDetailText("_cpuECoreClock", FormatFrequency(cpuECoreClockTask.Result));
                UpdateDetailText("_cpuMemoryUsage", FormatUsageInGigabytes(memoryUsedTask.Result, memoryTotalTask.Result, memoryUsageTask.Result));
                UpdateDetailText("_cpuMemoryTemperature", GetTemperatureText(memoryTemperatureTask.Result > 0 ? memoryTemperatureTask.Result : null));
                UpdateDetailText("_cpuSsdTemperature", FormatTemperaturePair(ssdTemperaturesTask.Result, _applicationSettings.Store.TemperatureUnit));
                UpdateDetailText("_gpuVoltage", FormatVoltage(gpuVoltageTask.Result));
                UpdateDetailText("_cpuTempRange", FormatTemperatureRangeText(_cpuTemperatureLabel?.Content?.ToString(), _cpuTempRange.Text));
                UpdateDetailText("_cpuVoltageRange", FormatFallbackRangeText(_cpuVoltage.Text, _cpuVoltageRange.Text));
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
                        gpuMemoryClockText.Text = NotAvailableText();
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
                : "-";
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
            _ => "-"
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
            _ => "-"
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
