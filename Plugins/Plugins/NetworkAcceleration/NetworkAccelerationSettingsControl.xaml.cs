using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public partial class NetworkAccelerationSettingsControl : UserControl
{
    private readonly NetworkAccelerationPlugin _plugin;

    public NetworkAccelerationSettingsControl(NetworkAccelerationPlugin plugin)
    {
        _plugin = plugin;
        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
        LoadCurrentValues();
        UpdateSummary();
        SetStatus(NetworkAccelerationText.SettingsSummaryDescription, false);
    }

    private void BuildFallbackUi()
    {
        _autoOptimizeOnStartupCheckBox = CreateSettingCheckBox(
            NetworkAccelerationText.AutoOptimizeOnStartup,
            "NetworkAcceleration_AutoOptimizeCheckBox",
            addBottomMargin: true);
        _resetWinsockCheckBox = CreateSettingCheckBox(
            NetworkAccelerationText.ResetWinsockOnOptimize,
            "NetworkAcceleration_ResetWinsockCheckBox",
            addBottomMargin: true);
        _resetTcpIpCheckBox = CreateSettingCheckBox(
            NetworkAccelerationText.ResetTcpIpOnOptimize,
            "NetworkAcceleration_ResetTcpIpCheckBox",
            addBottomMargin: false);

        _statusTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        AutomationProperties.SetAutomationId(_statusTextBlock, "NetworkAcceleration_SettingsStatusText");

        _modeSummaryTextBlock = CreateValueTextBlock();
        _startupSummaryTextBlock = CreateValueTextBlock();
        _winsockSummaryTextBlock = CreateValueTextBlock();
        _tcpSummaryTextBlock = CreateValueTextBlock();

        var root = new Grid { Margin = new Thickness(20) };
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_SettingsRoot");
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });

        var leftPanel = new Border
        {
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(20),
            BorderThickness = new Thickness(1)
        };
        ApplyCardChrome(leftPanel, "ControlFillColorDefaultBrush");

        var leftStack = new StackPanel();
        leftPanel.Child = leftStack;

        var heroBorder = new Border
        {
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#142B579A"))
        };
        var heroGrid = new Grid();
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });

        var heroText = new StackPanel();
        heroText.Children.Add(CreateTitleText(NetworkAccelerationText.SettingsTitle, new Thickness(0, 12, 0, 0)));
        heroText.Children.Add(CreateSecondaryText(NetworkAccelerationText.SettingsDescription, new Thickness(0, 8, 0, 0)));

        var heroAside = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        heroAside.Children.Add(CreateTrafficIllustration());
        heroAside.Children.Add(CreateSecondaryText(NetworkAccelerationText.AdminHint, new Thickness(0, 12, 0, 0)));

        Grid.SetColumn(heroAside, 1);
        heroGrid.Children.Add(heroText);
        heroGrid.Children.Add(heroAside);
        heroBorder.Child = heroGrid;
        leftStack.Children.Add(heroBorder);

        var optionsCard = new Border
        {
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(18)
        };
        ApplyCardChrome(optionsCard, "ControlFillColorSecondaryBrush", borderKey: null);

        var optionsStack = new StackPanel();
        foreach (var checkBox in GetSettingCheckBoxes())
            optionsStack.Children.Add(checkBox);
        optionsCard.Child = optionsStack;
        leftStack.Children.Add(optionsCard);

        var saveButton = new Wpf.Ui.Controls.Button
        {
            Content = NetworkAccelerationText.SaveSettingsButton,
            Width = 120,
            Margin = new Thickness(0, 12, 0, 0)
        };
        AutomationProperties.SetAutomationId(saveButton, "NetworkAcceleration_SaveSettingsButton");
        saveButton.Click += SaveButton_Click;
        leftStack.Children.Add(saveButton);

        var statusCard = new Border
        {
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(18)
        };
        ApplyCardChrome(statusCard, "ControlFillColorSecondaryBrush", borderKey: null);
        statusCard.Child = _statusTextBlock;
        leftStack.Children.Add(statusCard);

        Grid.SetColumn(leftPanel, 0);
        root.Children.Add(leftPanel);

        var rightPanel = new Border
        {
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(20),
            BorderThickness = new Thickness(1)
        };
        ApplyCardChrome(rightPanel, "ControlFillColorDefaultBrush");

        var rightStack = new StackPanel();
        rightPanel.Child = rightStack;
        rightStack.Children.Add(CreateSectionHeader(NetworkAccelerationText.SettingsSummaryTitle, NetworkAccelerationText.SettingsSummaryDescription));

        var summaryGrid = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition());
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition());

        foreach (var summaryTile in GetSummaryTileDefinitions())
        {
            var tile = CreateSummaryTile(summaryTile.BadgeText, summaryTile.BadgeHex, summaryTile.Label, summaryTile.ValueTextBlock);
            Grid.SetRow(tile, summaryTile.Row);
            Grid.SetColumn(tile, summaryTile.Column);
            summaryGrid.Children.Add(tile);
        }
        rightStack.Children.Add(summaryGrid);

        var hintCard = new Border
        {
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(18)
        };
        ApplyCardChrome(hintCard, "ControlFillColorSecondaryBrush", borderKey: null);

        var hintGrid = new Grid();
        hintGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        hintGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var smallVisual = CreateTrafficIllustration();
        smallVisual.Margin = new Thickness(0, 0, 12, 0);
        Grid.SetColumn(smallVisual, 0);
        hintGrid.Children.Add(smallVisual);

        var hintText = new StackPanel();
        hintText.Children.Add(CreateTitleText(NetworkAccelerationText.QuickActionsTitle));
        hintText.Children.Add(CreateSecondaryText(NetworkAccelerationText.QuickActionsDescription, new Thickness(0, 6, 0, 0)));
        Grid.SetColumn(hintText, 1);
        hintGrid.Children.Add(hintText);
        hintCard.Child = hintGrid;
        rightStack.Children.Add(hintCard);

        Grid.SetColumn(rightPanel, 1);
        root.Children.Add(rightPanel);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private static void ApplyCardChrome(Border border, string backgroundKey, string? borderKey = "ControlStrokeColorDefaultBrush")
    {
        border.SetResourceReference(Border.BackgroundProperty, backgroundKey);
        if (!string.IsNullOrWhiteSpace(borderKey))
            border.SetResourceReference(Border.BorderBrushProperty, borderKey);
    }

    private CheckBox CreateSettingCheckBox(string content, string automationId, bool addBottomMargin)
    {
        var checkBox = new CheckBox
        {
            Content = content,
            Margin = addBottomMargin ? new Thickness(0, 0, 0, 8) : new Thickness(0)
        };
        checkBox.Checked += SettingsCheckBox_Changed;
        checkBox.Unchecked += SettingsCheckBox_Changed;
        AutomationProperties.SetAutomationId(checkBox, automationId);
        return checkBox;
    }

    private static TextBlock CreateTitleText(string text, Thickness? margin = null)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 18,
            TextWrapping = TextWrapping.Wrap
        };
        if (margin != null)
            textBlock.Margin = margin.Value;
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        return textBlock;
    }

    private static TextBlock CreateSecondaryText(string text, Thickness? margin = null)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap
        };
        if (margin != null)
            textBlock.Margin = margin.Value;
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return textBlock;
    }

    private static Border CreateBadge(string text, string foregroundHex, string backgroundHex)
    {
        return new Border
        {
            Padding = new Thickness(10, 4, 10, 4),
            CornerRadius = new CornerRadius(999),
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundHex)),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foregroundHex))
            }
        };
    }

    private static Border CreateSummaryTile(string badgeText, string badgeHex, string label, TextBlock valueTextBlock)
    {
        var tile = new Border
        {
            Margin = new Thickness(0, 0, 12, 12),
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(16)
        };
        ApplyCardChrome(tile, "ControlFillColorSecondaryBrush", borderKey: null);

        var stack = new StackPanel();
        stack.Children.Add(CreateSecondaryText(label, new Thickness(0, 10, 0, 0)));
        valueTextBlock.Margin = new Thickness(0, 8, 0, 0);
        stack.Children.Add(valueTextBlock);
        tile.Child = stack;
        return tile;
    }

    private static TextBlock CreateValueTextBlock()
    {
        var textBlock = new TextBlock();
        StyleValueText(textBlock);
        return textBlock;
    }

    private static FrameworkElement CreateTrafficIllustration()
    {
        var canvas = new Canvas
        {
            Width = 92,
            Height = 72
        };

        canvas.Children.Add(new Line
        {
            X1 = 12,
            Y1 = 36,
            X2 = 42,
            Y2 = 16,
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4F9CFF")),
            StrokeThickness = 3
        });
        canvas.Children.Add(new Line
        {
            X1 = 12,
            Y1 = 36,
            X2 = 42,
            Y2 = 56,
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF27C7A8")),
            StrokeThickness = 3
        });
        canvas.Children.Add(new Line
        {
            X1 = 42,
            Y1 = 16,
            X2 = 78,
            Y2 = 16,
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5CA9FF")),
            StrokeThickness = 3
        });
        canvas.Children.Add(new Line
        {
            X1 = 42,
            Y1 = 56,
            X2 = 78,
            Y2 = 56,
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6AE2C6")),
            StrokeThickness = 3
        });

        foreach (var point in new[]
                 {
                     (Left: 4d, Top: 28d, Fill: "#FF4F9CFF"),
                     (Left: 35d, Top: 9d, Fill: "#FF5CA9FF"),
                     (Left: 35d, Top: 49d, Fill: "#FF27C7A8"),
                     (Left: 72d, Top: 9d, Fill: "#FF5CA9FF"),
                     (Left: 72d, Top: 49d, Fill: "#FF6AE2C6")
                 })
        {
            var ellipse = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(point.Fill))
            };
            Canvas.SetLeft(ellipse, point.Left);
            Canvas.SetTop(ellipse, point.Top);
            canvas.Children.Add(ellipse);
        }

        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16000000")),
            Child = canvas
        };
    }

    private static StackPanel CreateSectionHeader(string title, string description)
    {
        var stack = new StackPanel();
        stack.Children.Add(CreateTitleText(title));
        stack.Children.Add(CreateSecondaryText(description, new Thickness(0, 6, 0, 0)));
        return stack;
    }

    private static void StyleValueText(TextBlock textBlock)
    {
        textBlock.FontSize = 18;
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
    }

    private void LoadCurrentValues()
    {
        if (!NetworkAccelerationSettingsBinding.HasToggleCheckBoxes(
                _autoOptimizeOnStartupCheckBox,
                _resetWinsockCheckBox,
                _resetTcpIpCheckBox))
            return;

        NetworkAccelerationSettingsBinding.ApplyToggleSettings(
            _plugin.Settings,
            _autoOptimizeOnStartupCheckBox!,
            _resetWinsockCheckBox!,
            _resetTcpIpCheckBox!);
    }

    private void SettingsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (_modeSummaryTextBlock != null)
            _modeSummaryTextBlock.Text = NetworkAccelerationPresentation.GetModePresentation(_plugin.Settings.PreferredMode).DisplayName;

        foreach (var toggleSummary in GetToggleSummaries())
            toggleSummary.SummaryTextBlock.Text = NetworkAccelerationPresentation.GetToggleLabel(toggleSummary.CheckBox?.IsChecked == true);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!NetworkAccelerationSettingsBinding.HasToggleCheckBoxes(
                _autoOptimizeOnStartupCheckBox,
                _resetWinsockCheckBox,
                _resetTcpIpCheckBox))
            return;

        try
        {
            var updatedSettings = NetworkAccelerationSettingsBinding.BuildUpdatedSettings(
                _plugin.Settings,
                _autoOptimizeOnStartupCheckBox,
                _resetWinsockCheckBox,
                _resetTcpIpCheckBox);

            await _plugin.ApplySettingsAsync(updatedSettings).ConfigureAwait(true);
            UpdateSummary();
            SetStatus(NetworkAccelerationText.SettingsSaved, false);
        }
        catch (Exception ex)
        {
            SetStatus($"{NetworkAccelerationText.ErrorPrefix}: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"SaveButton_Click error: {ex.Message}", ex);
        }
    }

    private void SetStatus(string text, bool isError)
    {
        if (_statusTextBlock is null)
            return;

        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC42B1C"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0F7B5A"));

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = isError
                ? Wpf.Ui.Common.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Common.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock.Foreground;
        }
    }

    private CheckBox[] GetSettingCheckBoxes()
    {
        return [_autoOptimizeOnStartupCheckBox!, _resetWinsockCheckBox!, _resetTcpIpCheckBox!];
    }

    private SummaryTileDefinition[] GetSummaryTileDefinitions()
    {
        return
        [
            new SummaryTileDefinition("MODE", "#224F9CFF", NetworkAccelerationText.CurrentModeLabel, _modeSummaryTextBlock!, Row: 0, Column: 0),
            new SummaryTileDefinition("AUTO", "#1D27C7A8", NetworkAccelerationText.AutoOptimizeOnStartup, _startupSummaryTextBlock!, Row: 0, Column: 1),
            new SummaryTileDefinition("WSK", "#22FF9C4F", NetworkAccelerationText.ResetWinsockOnOptimize, _winsockSummaryTextBlock!, Row: 1, Column: 0),
            new SummaryTileDefinition("TCP", "#229A7BFF", NetworkAccelerationText.ResetTcpIpOnOptimize, _tcpSummaryTextBlock!, Row: 1, Column: 1)
        ];
    }

    private ToggleSummaryDefinition[] GetToggleSummaries()
    {
        return
        [
            new ToggleSummaryDefinition(_startupSummaryTextBlock!, _autoOptimizeOnStartupCheckBox),
            new ToggleSummaryDefinition(_winsockSummaryTextBlock!, _resetWinsockCheckBox),
            new ToggleSummaryDefinition(_tcpSummaryTextBlock!, _resetTcpIpCheckBox)
        ];
    }

    private sealed record SummaryTileDefinition(string BadgeText, string BadgeHex, string Label, TextBlock ValueTextBlock, int Row, int Column);

    private sealed record ToggleSummaryDefinition(TextBlock SummaryTextBlock, CheckBox? CheckBox);
}
