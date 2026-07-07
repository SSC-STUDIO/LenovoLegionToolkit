using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.BatteryHealth;

/// <summary>
/// Settings page UI for the Battery Health plugin.
/// </summary>
public partial class BatteryHealthSettingsControl : UserControl
{
    private readonly SettingsManager<BatteryHealthSettings> _settingsManager;
    private bool _isHydrating;

    private static CultureInfo Culture => BatteryHealthText.Culture;

    public BatteryHealthSettingsControl()
    {
        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);

        _settingsManager = new SettingsManager<BatteryHealthSettings>("battery-health");
        LoadCurrentValues();
    }

    private void LoadCurrentValues()
    {
        var settings = _settingsManager.Load();

        _isHydrating = true;
        try
        {
            _enableMonitoringCheckBox?.IsChecked = settings.EnableRealTimeMonitoring;

            _lowHealthThresholdSlider?.Value = settings.LowHealthThreshold;

            _criticalHealthThresholdSlider?.Value = settings.CriticalHealthThreshold;

            _enableNotificationCheckBox?.IsChecked = settings.EnableNotification;

            UpdateThresholdValueLabels();
        }
        finally
        {
            _isHydrating = false;
        }
    }

    private void LowHealthThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isHydrating)
        {
            return;
        }

        UpdateLowHealthThresholdLabel();
    }

    private void CriticalHealthThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isHydrating)
        {
            return;
        }

        UpdateCriticalHealthThresholdLabel();
    }

    private void UpdateThresholdValueLabels()
    {
        UpdateLowHealthThresholdLabel();
        UpdateCriticalHealthThresholdLabel();
    }

    private void UpdateLowHealthThresholdLabel()
    {
        if (_lowHealthThresholdValueLabel == null || _lowHealthThresholdSlider == null)
        {
            return;
        }

        _lowHealthThresholdValueLabel.Text =
            BatteryHealthText.FormatThresholdValue((int)Math.Round(_lowHealthThresholdSlider.Value));
    }

    private void UpdateCriticalHealthThresholdLabel()
    {
        if (_criticalHealthThresholdValueLabel == null || _criticalHealthThresholdSlider == null)
        {
            return;
        }

        _criticalHealthThresholdValueLabel.Text =
            BatteryHealthText.FormatThresholdValue((int)Math.Round(_criticalHealthThresholdSlider.Value));
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_enableMonitoringCheckBox == null
            || _lowHealthThresholdSlider == null
            || _criticalHealthThresholdSlider == null
            || _enableNotificationCheckBox == null)
        {
            return;
        }

        var lowThreshold = (int)Math.Round(_lowHealthThresholdSlider.Value);
        var criticalThreshold = (int)Math.Round(_criticalHealthThresholdSlider.Value);

        if (criticalThreshold >= lowThreshold)
        {
            SetStatus(BatteryHealthText.SettingsInvalidThresholds, true, Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
            return;
        }

        var settings = new BatteryHealthSettings
        {
            EnableRealTimeMonitoring = _enableMonitoringCheckBox.IsChecked == true,
            LowHealthThreshold = lowThreshold,
            CriticalHealthThreshold = criticalThreshold,
            EnableNotification = _enableNotificationCheckBox.IsChecked == true
        };

        var saved = _settingsManager.Save(settings);
        SetStatus(
            saved ? BatteryHealthText.SettingsSaved : BatteryHealthText.RefreshFailedMessage,
            !saved,
            saved ? Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24 : Wpf.Ui.Controls.SymbolRegular.ErrorCircle24);
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        LoadCurrentValues();
        SetStatus(BatteryHealthText.SettingsReloaded, false, Wpf.Ui.Controls.SymbolRegular.ArrowClockwise24);
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
            Text = BatteryHealthText.SettingsSaved,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusPillText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

        _enableMonitoringCheckBox = new CheckBox
        {
            Content = BatteryHealthText.EnableMonitoringLabel
        };
        AutomationProperties.SetAutomationId(_enableMonitoringCheckBox, "BatteryHealth_EnableMonitoringCheckBox");

        _lowHealthThresholdSlider = new Slider
        {
            Minimum = 50,
            Maximum = 95,
            TickFrequency = 1,
            IsSnapToTickEnabled = true
        };
        AutomationProperties.SetAutomationId(_lowHealthThresholdSlider, "BatteryHealth_LowHealthThresholdSlider");
        _lowHealthThresholdSlider.ValueChanged += LowHealthThresholdSlider_ValueChanged;

        _lowHealthThresholdValueLabel = new TextBlock
        {
            MinWidth = 48,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 0, 0),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        _lowHealthThresholdValueLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

        _criticalHealthThresholdSlider = new Slider
        {
            Minimum = 10,
            Maximum = 80,
            TickFrequency = 1,
            IsSnapToTickEnabled = true
        };
        AutomationProperties.SetAutomationId(_criticalHealthThresholdSlider, "BatteryHealth_CriticalHealthThresholdSlider");
        _criticalHealthThresholdSlider.ValueChanged += CriticalHealthThresholdSlider_ValueChanged;

        _criticalHealthThresholdValueLabel = new TextBlock
        {
            MinWidth = 48,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 0, 0),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        _criticalHealthThresholdValueLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

        _enableNotificationCheckBox = new CheckBox
        {
            Content = BatteryHealthText.EnableNotificationLabel
        };
        AutomationProperties.SetAutomationId(_enableNotificationCheckBox, "BatteryHealth_EnableNotificationCheckBox");

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
        AutomationProperties.SetAutomationId(_statusTextBlock, "BatteryHealthSettings_StatusText");

        var root = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };
        AutomationProperties.SetAutomationId(root, "BatteryHealthSettingsRoot");

        root.Children.Add(BuildFallbackOverviewCard());
        root.Children.Add(BuildFallbackMonitoringCard());
        root.Children.Add(BuildFallbackThresholdsCard());
        root.Children.Add(BuildFallbackNotificationsCard());
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
            Text = BatteryHealthText.SettingsPageTitle,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        textPanel.Children.Add(title);
        textPanel.Children.Add(CreateFallbackBodyText(BatteryHealthText.SettingsPageSubtitle, new Thickness(0, 4, 0, 0)));
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

    private Border BuildFallbackMonitoringCard()
    {
        var card = CreateFallbackSurface(new Thickness(18, 16, 18, 16), new Thickness(0, 0, 0, 14));
        var panel = new StackPanel();

        panel.Children.Add(BuildFallbackCardHeader(Wpf.Ui.Controls.SymbolRegular.Eye24, BatteryHealthText.MonitoringCardTitle));
        panel.Children.Add(CreateFallbackBodyText(BatteryHealthText.MonitoringCardDescription, new Thickness(0)));

        var checkboxSurface = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 14, 0, 0),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Child = _enableMonitoringCheckBox
        };
        checkboxSurface.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
        checkboxSurface.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        panel.Children.Add(checkboxSurface);

        card.Child = panel;
        return card;
    }

    private Border BuildFallbackThresholdsCard()
    {
        var card = CreateFallbackSurface(new Thickness(18, 16, 18, 16), new Thickness(0, 0, 0, 14));
        var panel = new StackPanel();

        panel.Children.Add(BuildFallbackCardHeader(Wpf.Ui.Controls.SymbolRegular.Warning24, BatteryHealthText.ThresholdsCardTitle));

        panel.Children.Add(CreateFallbackLabel(BatteryHealthText.LowHealthThresholdLabel));
        var lowGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        lowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        lowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        lowGrid.Children.Add(_lowHealthThresholdSlider);
        Grid.SetColumn(_lowHealthThresholdValueLabel, 1);
        lowGrid.Children.Add(_lowHealthThresholdValueLabel);
        panel.Children.Add(lowGrid);

        panel.Children.Add(CreateFallbackLabel(BatteryHealthText.CriticalHealthThresholdLabel, new Thickness(0, 18, 0, 0)));
        var criticalGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        criticalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        criticalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        criticalGrid.Children.Add(_criticalHealthThresholdSlider);
        Grid.SetColumn(_criticalHealthThresholdValueLabel, 1);
        criticalGrid.Children.Add(_criticalHealthThresholdValueLabel);
        panel.Children.Add(criticalGrid);

        card.Child = panel;
        return card;
    }

    private Border BuildFallbackNotificationsCard()
    {
        var card = CreateFallbackSurface(new Thickness(18, 16, 18, 16), new Thickness(0, 0, 0, 14));
        var panel = new StackPanel();

        panel.Children.Add(BuildFallbackCardHeader(Wpf.Ui.Controls.SymbolRegular.Alert24, BatteryHealthText.NotificationsCardTitle));
        panel.Children.Add(CreateFallbackBodyText(BatteryHealthText.NotificationsCardDescription, new Thickness(0)));

        var checkboxSurface = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 14, 0, 0),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Child = _enableNotificationCheckBox
        };
        checkboxSurface.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
        checkboxSurface.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        panel.Children.Add(checkboxSurface);

        card.Child = panel;
        return card;
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

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(18, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var reloadButton = CreateFallbackButton(BatteryHealthText.ReloadButton, "BatteryHealthSettings_ReloadButton", ReloadButton_Click, false);
        reloadButton.Margin = new Thickness(0, 0, 8, 0);
        buttonPanel.Children.Add(reloadButton);

        var saveButton = CreateFallbackButton(BatteryHealthText.SaveButton, "BatteryHealthSettings_SaveButton", SaveButton_Click, true);
        buttonPanel.Children.Add(saveButton);

        Grid.SetColumn(buttonPanel, 2);
        grid.Children.Add(buttonPanel);

        card.Child = grid;
        return card;
    }

    private StackPanel BuildFallbackCardHeader(Wpf.Ui.Controls.SymbolRegular symbol, string title)
    {
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };
        header.Children.Add(new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = symbol,
            FontSize = 18,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ResolveBrush("SystemAccentColorPrimaryBrush", SystemColors.HighlightBrush)
        });
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        titleText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        header.Children.Add(titleText);
        return header;
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
            LineHeight = 18,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return textBlock;
    }

    private static TextBlock CreateFallbackLabel(string text, Thickness margin = new Thickness())
    {
        var textBlock = new TextBlock
        {
            Text = text,
            Margin = margin,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ResolveBrush("TextFillColorPrimaryBrush", SystemColors.ControlTextBrush)
        };
        return textBlock;
    }

    private static Button CreateFallbackButton(string text, string automationId, RoutedEventHandler handler, bool primary)
    {
        var button = new Button
        {
            Content = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 170
            },
            MinWidth = 96,
            MinHeight = 34,
            Padding = new Thickness(12, 7, 12, 7),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, text);
        if (primary)
        {
            button.SetResourceReference(Control.BackgroundProperty, "SystemAccentColorPrimaryBrush");
            button.SetResourceReference(Control.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
        }
        button.Click += handler;
        return button;
    }

    #endregion
}
