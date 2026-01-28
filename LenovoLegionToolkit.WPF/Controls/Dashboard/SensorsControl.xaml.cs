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
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Settings;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Common;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace LenovoLegionToolkit.WPF.Controls.Dashboard;

public partial class SensorsControl
{
    private readonly ISensorsController _controller = IoCContainer.Resolve<ISensorsController>();
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly DashboardSettings _dashboardSettings = IoCContainer.Resolve<DashboardSettings>();

    private CancellationTokenSource? _cts;
    private Task? _refreshTask;

    private CancellationTokenSource? _batteryCts;
    private Task? _batteryRefreshTask;

    private string _cpuName = "Loading...";
    private string _gpuName = "Loading...";

    public SensorsControl()
    {
        InitializeComponent();
        InitializeContextMenu();
        _ = FetchHardwareNamesAsync();

        IsVisibleChanged += SensorsControl_IsVisibleChanged;
    }

    private async Task FetchHardwareNamesAsync()
    {
        try
        {
            _cpuName = await WMI.Win32.Processor.GetNameAsync();
            _gpuName = await WMI.Win32.VideoController.GetNameAsync();
        }
        catch
        {
            _cpuName = "Unknown CPU";
            _gpuName = "Unknown GPU";
        }

        Dispatcher.Invoke(() => {
            if (FindName("_cpuModelName") is TextBlock cpu) cpu.Text = _cpuName;
            if (FindName("_gpuModelName") is TextBlock gpu) gpu.Text = _gpuName;
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
                SymbolIcon = _dashboardSettings.Store.SensorsRefreshIntervalSeconds == interval ? SymbolRegular.Checkmark24 : SymbolRegular.Empty,
                Header = TimeSpan.FromSeconds(interval).Humanize(culture: Resource.Culture)
            };
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

        UpdateValues(SensorsData.Empty);
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
                    var powerAdapterStatus = await Power.IsPowerAdapterConnectedAsync();
                    var onBatterySince = Battery.GetOnBatterySince();
                    Dispatcher.Invoke(() => SetBattery(batteryInfo, powerAdapterStatus, onBatterySince));

                    await Task.Delay(TimeSpan.FromSeconds(2), token);
                }
                catch (OperationCanceledException) { }
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
            var temp = info.BatteryTemperatureC ?? 0;
            // Assuming 60C is max reasonable battery temp for bar scaling
            UpdateValue(tempBar, tempLabel, 60, temp, GetTemperatureText(info.BatteryTemperatureC));
        }

        if (FindName("_batteryRateBar") is System.Windows.Controls.Primitives.RangeBase rateBar &&
            FindName("_batteryRateText") is ContentControl rateLabel)
        {
            var rateW = Math.Abs(info.DischargeRate / 1000.0);
            // Assuming 100W is max reasonable charge/discharge rate for bar scaling
            UpdateValue(rateBar, rateLabel, 100, rateW, $"{info.DischargeRate / 1000.0:+0.00;-0.00;0.00} W");
        }

        if (FindName("_batteryModelName") is TextBlock batteryModelName)
        {
            batteryModelName.Text = info.ModelName ?? "Unknown";
        }

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

        if (FindName("_batteryAvgTemp") is TextBlock batteryAvgTemp)
        {
            batteryAvgTemp.Text = info.AvgTemperatureC.HasValue ? $"{info.AvgTemperatureC.Value:0.0}°C" : string.Empty;
        }
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

            if (!await _controller.IsSupportedAsync())
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Sensors not supported.");

                Dispatcher.Invoke(() => Visibility = Visibility.Collapsed);
                return;
            }

            await _controller.PrepareAsync();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var detailed = Dispatcher.Invoke(() => _cpuDetailsPanel.Visibility == Visibility.Visible);
                    var data = await _controller.GetDataAsync(detailed);
                    Dispatcher.Invoke(() => UpdateValues(data));
                    await Task.Delay(TimeSpan.FromSeconds(_dashboardSettings.Store.SensorsRefreshIntervalSeconds), token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Sensors refresh failed.", ex);

                    Dispatcher.Invoke(() => UpdateValues(SensorsData.Empty));
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Sensors refresh stopped.");
        }, token);
    }

    private void UpdateValues(SensorsData data)
    {
        UpdateValue(_cpuUtilizationBar, _cpuUtilizationLabel, data.CPU.MaxUtilization, data.CPU.Utilization,
            $"{data.CPU.Utilization}%");
        UpdateValue(_cpuCoreClockBar, _cpuCoreClockLabel, data.CPU.MaxCoreClock, data.CPU.CoreClock,
            $"{data.CPU.CoreClock / 1000.0:0.0} {Resource.GHz}", $"{data.CPU.MaxCoreClock / 1000.0:0.0} {Resource.GHz}");
        UpdateValue(_cpuTemperatureBar, _cpuTemperatureLabel, data.CPU.MaxTemperature, data.CPU.Temperature,
            GetTemperatureText(data.CPU.Temperature), GetTemperatureText(data.CPU.MaxTemperature));
        UpdateValue(_cpuFanSpeedBar, _cpuFanSpeedLabel, data.CPU.MaxFanSpeed, data.CPU.FanSpeed,
            $"{data.CPU.FanSpeed} {Resource.RPM}", $"{data.CPU.MaxFanSpeed} {Resource.RPM}");

        if (FindName("_cpuWattage") is TextBlock cpuWattage)
        {
            cpuWattage.Text = data.CPU.Wattage >= 0 ? $"{data.CPU.Wattage} W" : "N/A";
        }

        if (FindName("_cpuTempRange") is TextBlock cpuTempRange)
        {
             if (data.CPU.MinTemperature < int.MaxValue && data.CPU.MaxTemperatureRecord > int.MinValue)
                 cpuTempRange.Text = $"{data.CPU.MinTemperature}°C ~ {data.CPU.MaxTemperatureRecord}°C";
             else
                 cpuTempRange.Text = "N/A";
        }

        if (FindName("_cpuVoltage") is TextBlock cpuVoltage)
        {
            cpuVoltage.Text = data.CPU.Voltage > 0 ? $"{data.CPU.Voltage:0.000} V" : "N/A";
        }

        if (FindName("_cpuVoltageRange") is TextBlock cpuVoltageRange)
        {
             if (data.CPU.MinVoltage < double.MaxValue && data.CPU.MaxVoltage > double.MinValue)
                 cpuVoltageRange.Text = $"{data.CPU.MinVoltage:0.000} V ~ {data.CPU.MaxVoltage:0.000} V";
             else
                 cpuVoltageRange.Text = "N/A";
        }

        UpdateValue(_gpuUtilizationBar, _gpuUtilizationLabel, data.GPU.MaxUtilization, data.GPU.Utilization,
            $"{data.GPU.Utilization} %");
        
        // GPU Core Clock (Main view)
        UpdateValue(_gpuCoreClockBar, _gpuCoreClockLabel, data.GPU.MaxCoreClock, data.GPU.CoreClock,
            $"{data.GPU.CoreClock / 1000.0:0.0} {Resource.GHz}", $"{data.GPU.MaxCoreClock / 1000.0:0.0} {Resource.GHz}");

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
                memText.Text = $"{data.GPU.MemoryClock} {Resource.MHz}";
            }
        }

        UpdateValue(_gpuTemperatureBar, _gpuTemperatureLabel, data.GPU.MaxTemperature, data.GPU.Temperature,
            GetTemperatureText(data.GPU.Temperature), GetTemperatureText(data.GPU.MaxTemperature));
        UpdateValue(_gpuFanSpeedBar, _gpuFanSpeedLabel, data.GPU.MaxFanSpeed, data.GPU.FanSpeed,
            $"{data.GPU.FanSpeed} {Resource.RPM}", $"{data.GPU.MaxFanSpeed} {Resource.RPM}");

        if (FindName("_gpuWattage") is TextBlock gpuWattage)
        {
            gpuWattage.Text = data.GPU.Wattage >= 0 ? $"{data.GPU.Wattage} W" : "N/A";
        }
        
        if (FindName("_gpuTempRange") is TextBlock gpuTempRange)
        {
             if (data.GPU.MinTemperature < int.MaxValue && data.GPU.MaxTemperatureRecord > int.MinValue)
                 gpuTempRange.Text = $"{data.GPU.MinTemperature}°C ~ {data.GPU.MaxTemperatureRecord}°C";
             else
                 gpuTempRange.Text = "N/A";
        }

        if (FindName("_gpuVoltage") is TextBlock gpuVoltage)
        {
            gpuVoltage.Text = data.GPU.Voltage > 0 ? $"{data.GPU.Voltage:0.000} V" : "N/A";
        }
        
        if (FindName("_gpuVoltageRange") is TextBlock gpuVoltageRange)
        {
             if (data.GPU.MinVoltage < double.MaxValue && data.GPU.MaxVoltage > double.MinValue)
                 gpuVoltageRange.Text = $"{data.GPU.MinVoltage:0.000} V ~ {data.GPU.MaxVoltage:0.000} V";
             else
                 gpuVoltageRange.Text = "N/A";
        }
    }

    private void CardControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var isVisible = _cpuDetailsPanel.Visibility == Visibility.Visible;
        var newState = isVisible ? Visibility.Collapsed : Visibility.Visible;

        SetVisibility("_cpuDetailsPanel", newState == Visibility.Visible);
        SetVisibility("_batteryDetailsPanel", newState == Visibility.Visible);
        SetVisibility("_gpuDetailsPanel", newState == Visibility.Visible);
    }

    private string GetTemperatureText(double? temperature)
    {
        if (temperature is null)
            return "—";

        var temp = temperature.Value;

        if (_applicationSettings.Store.TemperatureUnit == TemperatureUnit.F)
        {
            temp *= 9.0 / 5.0;
            temp += 32;
            return $"{temp:0} {Resource.Fahrenheit}";
        }

        return $"{temp:0} {Resource.Celsius}";
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
}
