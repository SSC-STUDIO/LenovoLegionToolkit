using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.BatteryHealth;

/// <summary>
/// Feature page UI for the Battery Health plugin.
/// </summary>
public partial class BatteryHealthControl : UserControl
{
    private readonly BatteryHealthService _service;
    private readonly SettingsManager<BatteryHealthSettings> _settingsManager;
    private int _isLoading;

    public BatteryHealthControl()
    {
        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);

        _settingsManager = new SettingsManager<BatteryHealthSettings>("battery-health");
        _service = new BatteryHealthService(_settingsManager);

        Loaded += async (_, _) =>
        {
            try
            {
                await LoadBatteryHealthAsync();
            }
            catch (Exception)
            {
                SetStatus(BatteryHealthText.RefreshFailedMessage, true, Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
            }
        };
    }

    private async Task LoadBatteryHealthAsync()
    {
        if (Interlocked.Exchange(ref _isLoading, 1) == 1)
        {
            return;
        }

        SetStatus(BatteryHealthText.LoadingMessage, false, Wpf.Ui.Controls.SymbolRegular.ArrowClockwise24);

        try
        {
            var report = await _service.GetBatteryHealthReportAsync();
            ApplyReport(report);
        }
        catch (Exception)
        {
            SetStatus(BatteryHealthText.RefreshFailedMessage, true, Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
        }
        finally
        {
            Interlocked.Exchange(ref _isLoading, 0);
        }
    }

    private void ApplyReport(BatteryHealthReport report)
    {
        if (report.Status == BatteryHealthStatus.NoBattery)
        {
            SetStatus(BatteryHealthText.NoBatteryMessage, true, Wpf.Ui.Controls.SymbolRegular.Warning24);
            ClearMetrics();
            UpdateStatusPill(report.Status);
            return;
        }

        _healthValueTextBlock?.Text = BatteryHealthText.FormatHealthPercent(report.HealthPercentage);

        _cycleCountValueTextBlock?.Text = BatteryHealthText.FormatCycleCount(report.CycleCount);

        _chargeRemainingValueTextBlock?.Text = BatteryHealthText.FormatChargeRemaining(report.EstimatedChargeRemaining);

        _wearValueTextBlock?.Text = BatteryHealthText.FormatWearPercent(report.WearPercentage);

        _designCapacityTextBlock?.Text = BatteryHealthText.FormatCapacityMWh(report.DesignCapacity);

        _fullChargeCapacityTextBlock?.Text = BatteryHealthText.FormatCapacityMWh(report.FullChargeCapacity);

        _statusValueTextBlock?.Text = BatteryHealthText.GetStatusText(report.Status);

        UpdateStatusPill(report.Status);

        if (report.Status == BatteryHealthStatus.Unknown)
        {
            SetStatus(BatteryHealthText.RefreshFailedMessage, true, Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
        }
        else
        {
            SetStatus(string.Empty, false, Wpf.Ui.Controls.SymbolRegular.Info24);
        }
    }

    private void ClearMetrics()
    {
        _healthValueTextBlock?.Text = "--";

        _cycleCountValueTextBlock?.Text = "--";

        _chargeRemainingValueTextBlock?.Text = "--";

        _wearValueTextBlock?.Text = "--";

        _designCapacityTextBlock?.Text = "--";

        _fullChargeCapacityTextBlock?.Text = "--";

        _statusValueTextBlock?.Text = "--";
    }

    private void UpdateStatusPill(BatteryHealthStatus status)
    {
        var brushKey = status switch
        {
            BatteryHealthStatus.Healthy => "SystemFillColorSuccessBrush",
            BatteryHealthStatus.Warning => "SystemFillColorCautionBrush",
            BatteryHealthStatus.Critical => "SystemFillColorCriticalBrush",
            _ => "TextFillColorTertiaryBrush"
        };

        var symbol = status switch
        {
            BatteryHealthStatus.Healthy => Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24,
            BatteryHealthStatus.Warning => Wpf.Ui.Controls.SymbolRegular.Warning24,
            BatteryHealthStatus.Critical => Wpf.Ui.Controls.SymbolRegular.ErrorCircle24,
            BatteryHealthStatus.NoBattery => Wpf.Ui.Controls.SymbolRegular.Info24,
            _ => Wpf.Ui.Controls.SymbolRegular.Info24
        };

        _statusPillText?.Text = BatteryHealthText.GetStatusText(status);

        if (_statusIcon != null)
        {
            _statusIcon.Symbol = symbol;
            _statusIcon.Foreground = ResolveBrush(brushKey, SystemColors.ControlTextBrush);
        }
    }

    private void SetStatus(string text, bool isError, Wpf.Ui.Controls.SymbolRegular icon)
    {
        if (_statusTextBlock != null)
        {
            _statusTextBlock.Text = text;
            _statusTextBlock.Foreground = isError
                ? ResolveBrush("SystemFillColorCriticalBrush", SystemColors.ControlTextBrush)
                : ResolveBrush("TextFillColorSecondaryBrush", SystemColors.ControlTextBrush);
        }

        if (_actionBarIcon != null)
        {
            _actionBarIcon.Symbol = icon;
            _actionBarIcon.Foreground = isError
                ? ResolveBrush("SystemFillColorCriticalBrush", SystemColors.ControlTextBrush)
                : ResolveBrush("TextFillColorTertiaryBrush", SystemColors.ControlTextBrush);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadBatteryHealthAsync();
    }

    private static Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }

    #region Fallback UI

    private void BuildFallbackUi()
    {
        _statusIcon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24,
            FontSize = 15,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ResolveBrush("SystemFillColorSuccessBrush", SystemColors.ControlTextBrush)
        };

        _statusPillText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusPillText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

        _healthValueTextBlock = CreateFallbackValueTextBlock("BatteryHealth_HealthValue");
        _cycleCountValueTextBlock = CreateFallbackValueTextBlock("BatteryHealth_CycleCountValue");
        _chargeRemainingValueTextBlock = CreateFallbackValueTextBlock("BatteryHealth_ChargeRemainingValue");
        _wearValueTextBlock = CreateFallbackValueTextBlock("BatteryHealth_WearValue");
        _designCapacityTextBlock = CreateFallbackValueTextBlock("BatteryHealth_DesignCapacity");
        _fullChargeCapacityTextBlock = CreateFallbackValueTextBlock("BatteryHealth_FullChargeCapacity");
        _statusValueTextBlock = CreateFallbackValueTextBlock("BatteryHealth_StatusValue");

        _actionBarIcon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = Wpf.Ui.Controls.SymbolRegular.Info24,
            FontSize = 16,
            Margin = new Thickness(0, 1, 10, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Foreground = ResolveBrush("TextFillColorTertiaryBrush", SystemColors.ControlTextBrush)
        };

        _statusTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18,
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        AutomationProperties.SetAutomationId(_statusTextBlock, "BatteryHealth_StatusText");

        var root = new StackPanel { Margin = new Thickness(4) };
        AutomationProperties.SetAutomationId(root, "BatteryHealthRoot");

        root.Children.Add(BuildFallbackOverviewCard());
        root.Children.Add(BuildFallbackMetricsGrid());
        root.Children.Add(BuildFallbackCapacityGrid());
        root.Children.Add(BuildFallbackActionBar());

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private Border BuildFallbackOverviewCard()
    {
        var card = CreateFallbackSurface(new Thickness(18, 16, 18, 16), new Thickness(0, 0, 0, 14));
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var iconBadge = new Border
        {
            Width = 42,
            Height = 42,
            Margin = new Thickness(0, 0, 16, 0),
            CornerRadius = new CornerRadius(8),
            Background = ResolveBrush("SystemAccentColorPrimaryBrush", SystemColors.HighlightBrush),
            Child = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = Wpf.Ui.Controls.SymbolRegular.BatteryCharge24,
                FontSize = 21,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = ResolveBrush("TextOnAccentFillColorPrimaryBrush", Brushes.White)
            }
        };
        Grid.SetColumn(iconBadge, 0);
        grid.Children.Add(iconBadge);

        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var title = new TextBlock
        {
            Text = BatteryHealthText.OverviewTitle,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        textPanel.Children.Add(title);
        textPanel.Children.Add(CreateFallbackBodyText(BatteryHealthText.OverviewDescription, new Thickness(0, 4, 0, 0)));
        Grid.SetColumn(textPanel, 1);
        grid.Children.Add(textPanel);

        var pill = new Border
        {
            Padding = new Thickness(9, 5, 9, 5),
            Margin = new Thickness(18, 0, 0, 0),
            CornerRadius = new CornerRadius(8),
            VerticalAlignment = VerticalAlignment.Center
        };
        pill.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");

        var pillPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pillPanel.Children.Add(_statusIcon);
        pillPanel.Children.Add(_statusPillText);
        pill.Child = pillPanel;
        Grid.SetColumn(pill, 2);
        grid.Children.Add(pill);

        card.Child = grid;
        return card;
    }

    private Grid BuildFallbackMetricsGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        for (var i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (i < 3)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            }
        }

        var metrics = new (string Label, TextBlock Value)[]
        {
            (BatteryHealthText.HealthPercentLabel, _healthValueTextBlock),
            (BatteryHealthText.CycleCountLabel, _cycleCountValueTextBlock),
            (BatteryHealthText.ChargeRemainingLabel, _chargeRemainingValueTextBlock),
            (BatteryHealthText.WearPercentLabel, _wearValueTextBlock)
        };

        for (var i = 0; i < metrics.Length; i++)
        {
            var card = CreateFallbackMetric(metrics[i].Label, metrics[i].Value);
            Grid.SetColumn(card, i * 2);
            grid.Children.Add(card);
        }

        return grid;
    }

    private Grid BuildFallbackCapacityGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        for (var i = 0; i < 3; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (i < 2)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            }
        }

        var items = new (string Label, TextBlock Value)[]
        {
            (BatteryHealthText.DesignedCapacityLabel, _designCapacityTextBlock),
            (BatteryHealthText.FullChargeCapacityLabel, _fullChargeCapacityTextBlock),
            (BatteryHealthText.HealthStateLabel, _statusValueTextBlock)
        };

        for (var i = 0; i < items.Length; i++)
        {
            var card = CreateFallbackMetric(items[i].Label, items[i].Value);
            Grid.SetColumn(card, i * 2);
            grid.Children.Add(card);
        }

        return grid;
    }

    private Border BuildFallbackActionBar()
    {
        var card = CreateFallbackSurface(new Thickness(16, 12, 16, 12), new Thickness(0, 0, 0, 16));
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_actionBarIcon, 0);
        grid.Children.Add(_actionBarIcon);

        Grid.SetColumn(_statusTextBlock, 1);
        grid.Children.Add(_statusTextBlock);

        var refreshButton = new Wpf.Ui.Controls.Button
        {
            Content = new TextBlock
            {
                Text = BatteryHealthText.RefreshButton,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            },
            MinWidth = 96,
            MinHeight = 34,
            Padding = new Thickness(14, 7, 14, 7),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        refreshButton.SetResourceReference(Control.BackgroundProperty, "SystemAccentColorPrimaryBrush");
        refreshButton.SetResourceReference(Control.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
        AutomationProperties.SetAutomationId(refreshButton, "BatteryHealthRefreshButton");
        refreshButton.Click += RefreshButton_Click;

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(18, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttonPanel.Children.Add(refreshButton);
        Grid.SetColumn(buttonPanel, 2);
        grid.Children.Add(buttonPanel);

        card.Child = grid;
        return card;
    }

    private static Border CreateFallbackSurface(Thickness padding, Thickness margin)
    {
        var border = new Border
        {
            Padding = padding,
            Margin = margin,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        border.SetResourceReference(Border.BackgroundProperty, "ControlFillColorDefaultBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        return border;
    }

    private static TextBlock CreateFallbackBodyText(string text, Thickness margin)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            Margin = margin,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return textBlock;
    }

    private static TextBlock CreateFallbackValueTextBlock(string automationId)
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(0, 5, 0, 0),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Text = "--"
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        AutomationProperties.SetAutomationId(textBlock, automationId);
        return textBlock;
    }

    private static Border CreateFallbackMetric(string label, TextBlock valueTextBlock)
    {
        var labelText = new TextBlock
        {
            Text = label,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        labelText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");

        var panel = new StackPanel();
        panel.Children.Add(labelText);
        panel.Children.Add(valueTextBlock);

        return CreateFallbackSurface(new Thickness(14, 12, 14, 12), new Thickness(0), panel);
    }

    private static Border CreateFallbackSurface(Thickness padding, Thickness margin, UIElement child)
    {
        var border = CreateFallbackSurface(padding, margin);
        border.Child = child;
        return border;
    }

    #endregion
}
