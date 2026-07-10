using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
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
        _plannedStepsSummaryTextBlock = CreateValueTextBlock();

        _statusIcon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24,
            FontSize = 16,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusIcon.SetResourceReference(Control.ForegroundProperty, "TextFillColorSecondaryBrush");

        var root = new Grid { Margin = new Thickness(0) };
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_SettingsRoot");
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var summaryStrip = new Border
        {
            Margin = new Thickness(0, 0, 0, 18),
            Padding = new Thickness(12, 9, 12, 9),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(0)
        };
        summaryStrip.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
        summaryStrip.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");

        var stripGrid = new Grid();
        stripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var modeSummary = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        modeSummary.Children.Add(CreateSecondaryText(NetworkAccelerationText.CurrentModeLabel));
        _modeSummaryTextBlock.Margin = new Thickness(8, 0, 0, 0);
        modeSummary.Children.Add(_modeSummaryTextBlock);
        stripGrid.Children.Add(modeSummary);

        var statusSummary = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusSummary.Children.Add(_statusIcon);
        statusSummary.Children.Add(_statusTextBlock);
        Grid.SetColumn(statusSummary, 2);
        stripGrid.Children.Add(statusSummary);
        summaryStrip.Child = stripGrid;
        root.Children.Add(summaryStrip);

        var contentGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
        Grid.SetRow(contentGrid, 1);

        var leftStack = new StackPanel();
        leftStack.Children.Add(CreateTitleText(NetworkAccelerationText.SettingsTitle));
        leftStack.Children.Add(CreateSecondaryText(NetworkAccelerationText.SettingsDescription, new Thickness(0, 6, 0, 18)));
        foreach (var checkBox in GetSettingCheckBoxes())
        {
            leftStack.Children.Add(checkBox);
        }

        contentGrid.Children.Add(leftStack);

        var divider = new Border
        {
            Width = 1,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        divider.SetResourceReference(Border.BackgroundProperty, "ControlStrokeColorDefaultBrush");
        Grid.SetColumn(divider, 1);
        contentGrid.Children.Add(divider);

        var rightStack = new StackPanel();
        rightStack.Children.Add(CreateTitleText(NetworkAccelerationText.SettingsSummaryTitle));
        rightStack.Children.Add(CreateSecondaryText(NetworkAccelerationText.SettingsSummaryDescription, new Thickness(0, 6, 0, 18)));

        var summaryGrid = new Grid();
        summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        AddSummaryRow(summaryGrid, 0, NetworkAccelerationText.AutoOptimizeOnStartup, _startupSummaryTextBlock!);
        AddSummaryRow(summaryGrid, 1, NetworkAccelerationText.ResetWinsockOnOptimize, _winsockSummaryTextBlock!);
        AddSummaryRow(summaryGrid, 2, NetworkAccelerationText.ResetTcpIpOnOptimize, _tcpSummaryTextBlock!);
        AddSummaryRow(summaryGrid, 3, NetworkAccelerationText.PlannedStepsLabel, _plannedStepsSummaryTextBlock!);
        rightStack.Children.Add(summaryGrid);

        Grid.SetColumn(rightStack, 2);
        contentGrid.Children.Add(rightStack);
        root.Children.Add(contentGrid);

        var footer = new Border
        {
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 16, 0, 0)
        };
        footer.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        Grid.SetRow(footer, 2);

        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footerGrid.Children.Add(CreateSecondaryText(NetworkAccelerationText.AdminHint));

        var saveButton = new Wpf.Ui.Controls.Button
        {
            Content = NetworkAccelerationText.SaveSettingsButton,
            Width = 150,
            Margin = new Thickness(18, 0, 0, 0)
        };
        AutomationProperties.SetAutomationId(saveButton, "NetworkAcceleration_SaveSettingsButton");
        saveButton.Click += SaveButton_Click;
        Grid.SetColumn(saveButton, 1);
        footerGrid.Children.Add(saveButton);
        footer.Child = footerGrid;
        root.Children.Add(footer);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
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
        {
            textBlock.Margin = margin.Value;
        }

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
        {
            textBlock.Margin = margin.Value;
        }

        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return textBlock;
    }

    private static TextBlock CreateValueTextBlock()
    {
        var textBlock = new TextBlock();
        StyleValueText(textBlock);
        return textBlock;
    }

    private static void AddSummaryRow(Grid grid, int row, string label, TextBlock valueTextBlock)
    {
        var labelTextBlock = CreateSecondaryText(label, row == 0 ? null : new Thickness(0, 14, 0, 0));
        Grid.SetRow(labelTextBlock, row);
        grid.Children.Add(labelTextBlock);

        valueTextBlock.Margin = row == 0
            ? new Thickness(12, 0, 0, 0)
            : new Thickness(12, 14, 0, 0);
        valueTextBlock.FontSize = 14;
        valueTextBlock.FontWeight = FontWeights.SemiBold;
        Grid.SetRow(valueTextBlock, row);
        Grid.SetColumn(valueTextBlock, 1);
        grid.Children.Add(valueTextBlock);
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
        {
            return;
        }

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
        _modeSummaryTextBlock?.Text = NetworkAccelerationPresentation.GetModePresentation(_plugin.Settings.PreferredMode).DisplayName;

        foreach (var toggleSummary in GetToggleSummaries())
        {
            toggleSummary.SummaryTextBlock.Text = NetworkAccelerationPresentation.GetToggleLabel(toggleSummary.CheckBox?.IsChecked == true);
        }

        if (_plannedStepsSummaryTextBlock != null)
        {
            var updatedSettings = NetworkAccelerationSettingsBinding.BuildUpdatedSettings(
                _plugin.Settings,
                _autoOptimizeOnStartupCheckBox,
                _resetWinsockCheckBox,
                _resetTcpIpCheckBox);
            _plannedStepsSummaryTextBlock.Text = NetworkAccelerationPresentation.GetPlanSummary(
                NetworkAccelerationPlugin.GetOptimizationPlan(updatedSettings));
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!NetworkAccelerationSettingsBinding.HasToggleCheckBoxes(
                _autoOptimizeOnStartupCheckBox,
                _resetWinsockCheckBox,
                _resetTcpIpCheckBox))
        {
            return;
        }

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
            PluginLog.Trace($"SaveButton_Click error: {ex.Message}", ex);
        }
    }

    private void SetStatus(string text, bool isError)
    {
        if (_statusTextBlock is null)
        {
            return;
        }

        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? ResolveBrush("SystemFillColorCriticalBrush", SystemColors.ControlTextBrush)
            : ResolveBrush("SystemFillColorSuccessBrush", SystemColors.ControlTextBrush);

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = isError
                ? Wpf.Ui.Controls.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock.Foreground;
        }
    }

    private static Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }

    private CheckBox[] GetSettingCheckBoxes()
    {
        return [_autoOptimizeOnStartupCheckBox!, _resetWinsockCheckBox!, _resetTcpIpCheckBox!];
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

    private sealed record ToggleSummaryDefinition(TextBlock SummaryTextBlock, CheckBox? CheckBox);
}
