using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Humanizer;
using Humanizer.Localisation;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Behaviors;
using LenovoLegionToolkit.WPF.Controls.Dashboard;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Settings;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Windows.Dashboard;
using Microsoft.Xaml.Behaviors;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class DashboardPage
{
    private readonly DashboardSettings _dashboardSettings = IoCContainer.Resolve<DashboardSettings>();
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();

    private readonly List<DashboardGroupControl> _dashboardGroupControls = [];

    private CancellationTokenSource? _batteryCts;
    private Task? _batteryRefreshTask;

    public DashboardPage()
    {
        InitializeComponent();
        IsVisibleChanged += DashboardPage_IsVisibleChanged;
    }

    private async void DashboardPage_Initialized(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async void DashboardPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            RefreshBattery();
            return;
        }

        if (_batteryCts is not null)
            await _batteryCts.CancelAsync();

        _batteryCts = null;

        if (_batteryRefreshTask is not null)
            await _batteryRefreshTask;

        _batteryRefreshTask = null;
    }

    private async Task RefreshAsync()
    {
        _loader.IsLoading = true;

        var initializedTasks = new List<Task> { Task.Delay(TimeSpan.FromSeconds(1)) };

        ScrollHost?.ScrollToTop();

        _sensors.Visibility = _dashboardSettings.Store.ShowSensors ? Visibility.Visible : Visibility.Collapsed;

        _dashboardGroupControls.Clear();
        _content.ColumnDefinitions.Clear();
        _content.RowDefinitions.Clear();
        _content.Children.Clear();

        var groups = _dashboardSettings.Store.Groups ?? DashboardGroup.DefaultGroups;

        if (Log.Instance.IsTraceEnabled)
        {
            Log.Instance.Trace($"Groups:");
            foreach (var group in groups)
                Log.Instance.Trace($" - {group}");
        }

        _content.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1, GridUnitType.Star) });
        _content.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1, GridUnitType.Star) });

        foreach (var group in groups)
        {
            _content.RowDefinitions.Add(new RowDefinition { Height = new(1, GridUnitType.Auto) });

            var control = new DashboardGroupControl(group);
            _content.Children.Add(control);
            _dashboardGroupControls.Add(control);
            initializedTasks.Add(control.InitializedTask);
        }

        _content.RowDefinitions.Add(new RowDefinition { Height = new(1, GridUnitType.Auto) });

        var editDashboardHyperlink = new Hyperlink
        {
            Icon = SymbolRegular.Edit24,
            Content = Resource.DashboardPage_Customize,
            Margin = new(0, 16, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        editDashboardHyperlink.Click += (_, _) =>
        {
            var window = new EditDashboardWindow { Owner = Window.GetWindow(this) };
            window.Apply += async (_, _) => await RefreshAsync();
            window.ShowDialog();
        };

        Grid.SetRow(editDashboardHyperlink, groups.Length);
        Grid.SetColumn(editDashboardHyperlink, 0);
        Grid.SetColumnSpan(editDashboardHyperlink, 2);

        _content.Children.Add(editDashboardHyperlink);

        LayoutGroups(ActualWidth);

        await Task.WhenAll(initializedTasks);

        _loader.IsLoading = false;
    }

    private void DashboardPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged)
            return;

        LayoutGroups(e.NewSize.Width);
    }

    private void LayoutGroups(double width)
    {
        if (width > 1000)
            Expand();
        else
            Collapse();
    }

    private void Expand()
    {
        var lastColumn = _content.ColumnDefinitions.LastOrDefault();
        if (lastColumn is not null)
            lastColumn.Width = new(1, GridUnitType.Star);

        for (var index = 0; index < _dashboardGroupControls.Count; index++)
        {
            var control = _dashboardGroupControls[index];
            Grid.SetRow(control, index - (index % 2));
            Grid.SetColumn(control, index % 2);
        }
    }

    private void Collapse()
    {
        var lastColumn = _content.ColumnDefinitions.LastOrDefault();
        if (lastColumn is not null)
            lastColumn.Width = new(0, GridUnitType.Pixel);

        for (var index = 0; index < _dashboardGroupControls.Count; index++)
        {
            var control = _dashboardGroupControls[index];
            Grid.SetRow(control, index);
            Grid.SetColumn(control, 0);
        }
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
        // Battery percentage chart
        // Clear any ongoing animation to ensure the progress bar displays correctly,
        // especially when charging at 100% where the value might not change
        _batteryPercentageBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
        _batteryPercentageBar.Value = batteryInfo.BatteryPercentage;
        if (_percentRemaining is TextBlock percentText)
        {
            percentText.Text = $"{batteryInfo.BatteryPercentage}%";
            if (batteryInfo.IsLowBattery)
                percentText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 196, 0));
            else
                percentText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        }

        // Update battery icon based on percentage
        if (FindName("_batteryPercentageIcon") is Wpf.Ui.Controls.SymbolIcon batteryIcon)
        {
            SymbolRegular symbol;
            if (batteryInfo.IsCharging)
            {
                symbol = SymbolRegular.BatteryCharge24;
            }
            else
            {
                var number = (int)Math.Round(batteryInfo.BatteryPercentage / 10.0);
                symbol = number switch
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
            batteryIcon.Symbol = symbol;
        }

        _status.Text = GetBatteryStatusText(batteryInfo);
        _lowBattery.Visibility = batteryInfo.IsLowBattery ? Visibility.Visible : Visibility.Collapsed;
        _lowWattageCharger.Visibility = powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage ? Visibility.Visible : Visibility.Collapsed;
        
        // Hide status text if warning is shown
        if (batteryInfo.IsLowBattery || powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage)
        {
            _status.Visibility = Visibility.Collapsed;
        }
        else
        {
            _status.Visibility = Visibility.Visible;
        }

        // Battery health chart - hide if design capacity is invalid
        if (batteryInfo.DesignCapacity > 0)
        {
            _batteryHealthBar.Value = batteryInfo.BatteryHealth;
            if (_batteryHealthText is TextBlock healthText)
            {
                healthText.Text = $"{batteryInfo.BatteryHealth:0.00}%";
            }
            if (FindName("_batteryHealthCard") is System.Windows.FrameworkElement healthCard)
            {
                healthCard.Visibility = Visibility.Visible;
            }
        }
        else
        {
            if (FindName("_batteryHealthCard") is System.Windows.FrameworkElement healthCard)
            {
                healthCard.Visibility = Visibility.Collapsed;
            }
        }

        if (batteryInfo.BatteryTemperatureC is not null)
        {
            _batteryTemperatureText.Text = GetTemperatureText(batteryInfo.BatteryTemperatureC);
            _batteryTemperatureText.Visibility = Visibility.Visible;
            if (FindName("_batteryTemperatureCard") is System.Windows.FrameworkElement temperatureCard)
            {
                temperatureCard.Visibility = Visibility.Visible;
            }
        }
        else
        {
            _batteryTemperatureText.Visibility = Visibility.Collapsed;
            if (FindName("_batteryTemperatureCard") is System.Windows.FrameworkElement temperatureCard)
            {
                temperatureCard.Visibility = Visibility.Collapsed;
            }
        }

        if (!batteryInfo.IsCharging && onBatterySince.HasValue)
        {
            var onBatterySinceValue = onBatterySince.Value;
            var dateText = onBatterySinceValue.ToString("G", Resource.Culture);
            var duration = DateTime.Now.Subtract(onBatterySinceValue);
            _onBatterySinceText.Text = $"{dateText} ({duration.Humanize(2, Resource.Culture, minUnit: TimeUnit.Second)})";
            // Show the card when we have valid data
            var onBatterySinceCard = this.FindName("_onBatterySinceCard") as System.Windows.FrameworkElement;
            if (onBatterySinceCard != null)
            {
                onBatterySinceCard.Visibility = Visibility.Visible;
            }
        }
        else
        {
            _onBatterySinceText.Text = "-";
            // Hide the card when showing "-"
            var onBatterySinceCard = this.FindName("_onBatterySinceCard") as System.Windows.FrameworkElement;
            if (onBatterySinceCard != null)
            {
                onBatterySinceCard.Visibility = Visibility.Collapsed;
            }
        }

        // Discharge rate - no progress bars, just display values
        if (_batteryDischargeRateText is TextBlock dischargeRateText)
            dischargeRateText.Text = $"{batteryInfo.DischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        if (_batteryMinDischargeRateText is TextBlock minDischargeRateText)
            minDischargeRateText.Text = $"{batteryInfo.MinDischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        if (_batteryMaxDischargeRateText is TextBlock maxDischargeRateText)
            maxDischargeRateText.Text = $"{batteryInfo.MaxDischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        
        // Hide discharge rate cards if values are less than 0 (invalid)
        var dischargeRateCard = this.FindName("_batteryDischargeRateCard") as System.Windows.FrameworkElement;
        if (dischargeRateCard != null)
        {
            // Hide if discharge rate is less than 0 (no meaningful data)
            dischargeRateCard.Visibility = (batteryInfo.DischargeRate < 0) ? Visibility.Collapsed : Visibility.Visible;
        }
        
        var minDischargeRateCard = this.FindName("_batteryMinDischargeRateCard") as System.Windows.FrameworkElement;
        if (minDischargeRateCard != null)
        {
            // Hide if min discharge rate is less than 0 (no meaningful data)
            minDischargeRateCard.Visibility = (batteryInfo.MinDischargeRate < 0) ? Visibility.Collapsed : Visibility.Visible;
        }
        
        var maxDischargeRateCard = this.FindName("_batteryMaxDischargeRateCard") as System.Windows.FrameworkElement;
        if (maxDischargeRateCard != null)
        {
            // Hide if max discharge rate is less than 0 (no meaningful data)
            maxDischargeRateCard.Visibility = (batteryInfo.MaxDischargeRate < 0) ? Visibility.Collapsed : Visibility.Visible;
        }

        // Capacity charts (relative to design capacity)
        if (batteryInfo.DesignCapacity > 0)
        {
            var currentCapacityPercent = (batteryInfo.EstimateChargeRemaining / (double)batteryInfo.DesignCapacity) * 100.0;
            var fullChargeCapacityPercent = (batteryInfo.FullChargeCapacity / (double)batteryInfo.DesignCapacity) * 100.0;
            
            _batteryCurrentCapacityBar.Maximum = 100;
            _batteryCurrentCapacityBar.Value = currentCapacityPercent;
            _batteryFullChargeCapacityBar.Maximum = 100;
            _batteryFullChargeCapacityBar.Value = fullChargeCapacityPercent;
            _batteryDesignCapacityBar.Maximum = 100;
            _batteryDesignCapacityBar.Value = 100;
            
            if (_batteryCapacityText is TextBlock capacityText)
                capacityText.Text = $"{batteryInfo.EstimateChargeRemaining / 1000.0:0.00} Wh";
            if (_batteryFullChargeCapacityText is TextBlock fullChargeCapacityText)
                fullChargeCapacityText.Text = $"{batteryInfo.FullChargeCapacity / 1000.0:0.00} Wh";
            if (_batteryDesignCapacityText is TextBlock designCapacityText)
                designCapacityText.Text = $"{batteryInfo.DesignCapacity / 1000.0:0.00} Wh";
            
            // Show capacity cards when data is valid
            if (FindName("_batteryCurrentCapacityCard") is System.Windows.FrameworkElement currentCapacityCard)
                currentCapacityCard.Visibility = Visibility.Visible;
            if (FindName("_batteryFullChargeCapacityCard") is System.Windows.FrameworkElement fullChargeCapacityCard)
                fullChargeCapacityCard.Visibility = Visibility.Visible;
            if (FindName("_batteryDesignCapacityCard") is System.Windows.FrameworkElement designCapacityCard)
                designCapacityCard.Visibility = Visibility.Visible;
        }
        else
        {
            _batteryCurrentCapacityBar.Value = 0;
            _batteryFullChargeCapacityBar.Value = 0;
            _batteryDesignCapacityBar.Value = 0;
            
            // Hide capacity cards when design capacity is invalid
            if (FindName("_batteryCurrentCapacityCard") is System.Windows.FrameworkElement currentCapacityCard)
                currentCapacityCard.Visibility = Visibility.Collapsed;
            if (FindName("_batteryFullChargeCapacityCard") is System.Windows.FrameworkElement fullChargeCapacityCard)
                fullChargeCapacityCard.Visibility = Visibility.Collapsed;
            if (FindName("_batteryDesignCapacityCard") is System.Windows.FrameworkElement designCapacityCard)
                designCapacityCard.Visibility = Visibility.Collapsed;
        }

        if (batteryInfo.ManufactureDate is not null)
        {
            _batteryManufactureDateText.Text = batteryInfo.ManufactureDate?.ToString(LocalizationHelper.ShortDateFormat) ?? "-";
            _batteryManufactureDateText.Visibility = Visibility.Visible;
            var manufactureDateCard = this.FindName("_batteryManufactureDateCard") as System.Windows.FrameworkElement;
            if (manufactureDateCard != null)
            {
                manufactureDateCard.Visibility = Visibility.Visible;
            }
        }
        else
        {
            _batteryManufactureDateText.Visibility = Visibility.Collapsed;
            var manufactureDateCard = this.FindName("_batteryManufactureDateCard") as System.Windows.FrameworkElement;
            if (manufactureDateCard != null)
            {
                manufactureDateCard.Visibility = Visibility.Collapsed;
            }
        }

        if (batteryInfo.FirstUseDate is not null)
        {
            _batteryFirstUseDateText.Text = batteryInfo.FirstUseDate?.ToString(LocalizationHelper.ShortDateFormat) ?? "-";
            _batteryFirstUseDateText.Visibility = Visibility.Visible;
            var firstUseDateCard = this.FindName("_batteryFirstUseDateCard") as System.Windows.FrameworkElement;
            if (firstUseDateCard != null)
            {
                firstUseDateCard.Visibility = Visibility.Visible;
            }
        }
        else
        {
            _batteryFirstUseDateText.Visibility = Visibility.Collapsed;
            var firstUseDateCard = this.FindName("_batteryFirstUseDateCard") as System.Windows.FrameworkElement;
            if (firstUseDateCard != null)
            {
                firstUseDateCard.Visibility = Visibility.Collapsed;
            }
        }

        // Cycle count - hide if invalid (0 or negative)
        if (batteryInfo.CycleCount > 0)
        {
            _batteryCycleCountText.Text = $"{batteryInfo.CycleCount}";
            if (FindName("_batteryCycleCountCard") is System.Windows.FrameworkElement cycleCountCard)
            {
                cycleCountCard.Visibility = Visibility.Visible;
            }
        }
        else
        {
            if (FindName("_batteryCycleCountCard") is System.Windows.FrameworkElement cycleCountCard)
            {
                cycleCountCard.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static string GetBatteryStatusText(BatteryInformation batteryInfo)
    {
        if (batteryInfo.IsCharging)
        {
            if (batteryInfo.DischargeRate > 0)
                return Resource.BatteryPage_ACAdapterConnectedAndCharging;

            return Resource.BatteryPage_ACAdapterConnectedNotCharging;
        }

        if (batteryInfo.BatteryLifeRemaining < 0)
            return Resource.BatteryPage_EstimatingBatteryLife;

        var time = TimeSpan.FromSeconds(batteryInfo.BatteryLifeRemaining).Humanize(2, Resource.Culture);
        return string.Format(Resource.BatteryPage_EstimatedBatteryLifeRemaining, time);
    }

    private string GetTemperatureText(double? temperature)
    {
        if (temperature is null)
            return "—";

        if (_settings.Store.TemperatureUnit == TemperatureUnit.F)
        {
            temperature *= 9.0 / 5.0;
            temperature += 32;
            return $"{temperature:0.0} {Resource.Fahrenheit}";
        }

        return $"{temperature:0.0} {Resource.Celsius}";
    }
}
