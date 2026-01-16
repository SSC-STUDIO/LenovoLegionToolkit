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
        _batteryPercentageBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
        _batteryPercentageBar.Value = batteryInfo.BatteryPercentage;
        SetPercentageText(batteryInfo);
        SetBatteryIcon(batteryInfo);
        
        _status.Text = GetBatteryStatusText(batteryInfo);
        _lowBattery.Visibility = batteryInfo.IsLowBattery ? Visibility.Visible : Visibility.Collapsed;
        _lowWattageCharger.Visibility = powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage ? Visibility.Visible : Visibility.Collapsed;
        _status.Visibility = (batteryInfo.IsLowBattery || powerAdapterStatus == PowerAdapterStatus.ConnectedLowWattage) 
            ? Visibility.Collapsed 
            : Visibility.Visible;

        SetBatteryHealth(batteryInfo);
        SetBatteryTemperature(batteryInfo);
        SetOnBatterySince(batteryInfo, onBatterySince);
        SetDischargeRates(batteryInfo);
        SetCapacityInfo(batteryInfo);
        SetManufactureDate(batteryInfo);
        SetFirstUseDate(batteryInfo);
        SetCycleCount(batteryInfo);
    }

    private void SetPercentageText(BatteryInformation batteryInfo)
    {
        if (_percentRemaining is TextBlock percentText)
        {
            percentText.Text = $"{batteryInfo.BatteryPercentage:N0}%";
            if (batteryInfo.IsLowBattery)
                percentText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 196, 0));
            else
                percentText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        }
    }

    private void SetBatteryIcon(BatteryInformation batteryInfo)
    {
        if (FindName("_batteryPercentageIcon") is Wpf.Ui.Controls.SymbolIcon batteryIcon)
        {
            batteryIcon.Symbol = batteryInfo.IsCharging 
                ? SymbolRegular.BatteryCharge24 
                : GetBatteryIconSymbol(batteryInfo.BatteryPercentage);
        }
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

    private void SetBatteryHealth(BatteryInformation batteryInfo)
    {
        if (batteryInfo.DesignCapacity > 0)
        {
            _batteryHealthBar.Value = batteryInfo.BatteryHealth;
            if (_batteryHealthText is TextBlock healthText)
                healthText.Text = $"{batteryInfo.BatteryHealth:0.00}%";
            SetCardVisibility("_batteryHealthCard", true);
        }
        else
        {
            SetCardVisibility("_batteryHealthCard", false);
        }
    }

    private void SetBatteryTemperature(BatteryInformation batteryInfo)
    {
        if (batteryInfo.BatteryTemperatureC is not null)
        {
            _batteryTemperatureText.Text = GetTemperatureText(batteryInfo.BatteryTemperatureC);
            _batteryTemperatureText.Visibility = Visibility.Visible;
            SetCardVisibility("_batteryTemperatureCard", true);
        }
        else
        {
            _batteryTemperatureText.Visibility = Visibility.Collapsed;
            SetCardVisibility("_batteryTemperatureCard", false);
        }
    }

    private void SetOnBatterySince(BatteryInformation batteryInfo, DateTime? onBatterySince)
    {
        if (!batteryInfo.IsCharging && onBatterySince.HasValue)
        {
            var onBatterySinceValue = onBatterySince.Value;
            var dateText = onBatterySinceValue.ToString("G", Resource.Culture);
            var duration = DateTime.Now.Subtract(onBatterySinceValue);
            _onBatterySinceText.Text = $"{dateText} ({duration.Humanize(2, Resource.Culture, minUnit: TimeUnit.Second)})";
            SetCardVisibility("_onBatterySinceCard", true);
        }
        else
        {
            _onBatterySinceText.Text = "-";
            SetCardVisibility("_onBatterySinceCard", false);
        }
    }

    private void SetDischargeRates(BatteryInformation batteryInfo)
    {
        if (_batteryDischargeRateText is TextBlock dischargeRateText)
            dischargeRateText.Text = $"{batteryInfo.DischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        if (_batteryMinDischargeRateText is TextBlock minDischargeRateText)
            minDischargeRateText.Text = $"{batteryInfo.MinDischargeRate / 1000.0:+0.00;-0.00;0.00} W";
        if (_batteryMaxDischargeRateText is TextBlock maxDischargeRateText)
            maxDischargeRateText.Text = $"{batteryInfo.MaxDischargeRate / 1000.0:+0.00;-0.00;0.00} W";

        SetCardVisibility("_batteryDischargeRateCard", batteryInfo.DischargeRate >= 0);
        SetCardVisibility("_batteryMinDischargeRateCard", batteryInfo.MinDischargeRate >= 0);
        SetCardVisibility("_batteryMaxDischargeRateCard", batteryInfo.MaxDischargeRate >= 0);
    }

    private void SetCapacityInfo(BatteryInformation batteryInfo)
    {
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
            
            SetCardVisibility("_batteryCurrentCapacityCard", true);
            SetCardVisibility("_batteryFullChargeCapacityCard", true);
            SetCardVisibility("_batteryDesignCapacityCard", true);
        }
        else
        {
            _batteryCurrentCapacityBar.Value = 0;
            _batteryFullChargeCapacityBar.Value = 0;
            _batteryDesignCapacityBar.Value = 0;
            
            SetCardVisibility("_batteryCurrentCapacityCard", false);
            SetCardVisibility("_batteryFullChargeCapacityCard", false);
            SetCardVisibility("_batteryDesignCapacityCard", false);
        }
    }

    private void SetManufactureDate(BatteryInformation batteryInfo)
    {
        if (batteryInfo.ManufactureDate is not null)
        {
            _batteryManufactureDateText.Text = batteryInfo.ManufactureDate?.ToString(LocalizationHelper.ShortDateFormat) ?? "-";
            _batteryManufactureDateText.Visibility = Visibility.Visible;
            SetCardVisibility("_batteryManufactureDateCard", true);
        }
        else
        {
            _batteryManufactureDateText.Visibility = Visibility.Collapsed;
            SetCardVisibility("_batteryManufactureDateCard", false);
        }
    }

    private void SetFirstUseDate(BatteryInformation batteryInfo)
    {
        if (batteryInfo.FirstUseDate is not null)
        {
            _batteryFirstUseDateText.Text = batteryInfo.FirstUseDate?.ToString(LocalizationHelper.ShortDateFormat) ?? "-";
            _batteryFirstUseDateText.Visibility = Visibility.Visible;
            SetCardVisibility("_batteryFirstUseDateCard", true);
        }
        else
        {
            _batteryFirstUseDateText.Visibility = Visibility.Collapsed;
            SetCardVisibility("_batteryFirstUseDateCard", false);
        }
    }

    private void SetCycleCount(BatteryInformation batteryInfo)
    {
        if (batteryInfo.CycleCount > 0)
        {
            _batteryCycleCountText.Text = $"{batteryInfo.CycleCount:N0}";
            SetCardVisibility("_batteryCycleCountCard", true);
        }
        else
        {
            SetCardVisibility("_batteryCycleCountCard", false);
        }
    }

    private void SetCardVisibility(string cardName, bool visible)
    {
        if (FindName(cardName) is System.Windows.FrameworkElement card)
            card.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
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
