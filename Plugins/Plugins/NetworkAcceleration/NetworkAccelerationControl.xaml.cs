using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public partial class NetworkAccelerationControl : UserControl
{
    private const int MaxTelemetrySamples = 24;
    private static readonly string[] DataSizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    private readonly NetworkAccelerationPlugin _plugin;
    private readonly NetworkAccelerationTelemetryService _telemetryService = new();
    private readonly DispatcherTimer _telemetryTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };
    private readonly List<NetworkAccelerationTelemetrySnapshot> _telemetrySamples = [];

    private bool _isServiceRunning;
    private DateTimeOffset? _sessionStartedAt;
    private bool _isSelectionSyncInProgress;

    private static CultureInfo Culture => NetworkAccelerationText.Culture;

    public NetworkAccelerationControl(NetworkAccelerationPlugin plugin)
    {
        _plugin = plugin;
        _telemetryTimer.Tick += TelemetryTimer_Tick;

        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
        LoadSavedSettings();
        LoadCurrentMode();
        SetPresetSelectionFromMode(_plugin.Settings.PreferredMode);
        UpdateModeDescription();
        UpdatePresetDetails();
        UpdateSavedModeSummary();

        var initialSnapshot = new NetworkAccelerationTelemetrySnapshot(
            DateTimeOffset.Now,
            NetworkAccelerationText.NoActiveAdapter,
            0,
            0,
            0,
            0);
        UpdateTelemetrySummary(initialSnapshot);
        SynchronizeRuntimeState();
        UpdateSessionPresentation();

        SetStatus(NetworkAccelerationText.MonitoringStatus, false);
    }

    private void BuildFallbackUi()
    {
        _serviceStateTextBlock = CreateValueTextBlock();
        _savedModeSummaryTextBlock = CreateValueTextBlock();
        _sessionValueTextBlock = CreateValueTextBlock();
        _presetTitleTextBlock = CreateSectionTextBlock();
        _presetSubtitleTextBlock = CreateDescriptionTextBlock();
        _presetStateTextBlock = CreateValueTextBlock();
        _presetRecommendationTextBlock = CreateValueTextBlock();
        _presetActionsTextBlock = CreateValueTextBlock();
        _detailSessionTextBlock = CreateValueTextBlock();
        _modeDescriptionTextBlock = CreateDescriptionTextBlock();
        _downloadValueTextBlock = CreateMetricValueTextBlock();
        _uploadValueTextBlock = CreateMetricValueTextBlock();
        _peakValueTextBlock = CreateMetricValueTextBlock();
        _downloadTotalTextBlock = CreateValueTextBlock();
        _uploadTotalTextBlock = CreateValueTextBlock();
        _adapterValueTextBlock = CreateValueTextBlock();
        _updatedValueTextBlock = CreateValueTextBlock();
        _statusTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _statusIcon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24,
            FontSize = 16,
            Margin = new Thickness(0, 1, 8, 0),
            VerticalAlignment = VerticalAlignment.Top
        };
        _statusIcon.SetResourceReference(Control.ForegroundProperty, "TextFillColorSecondaryBrush");

        _serviceToggleButton = CreateButton(174, null, "NetworkAcceleration_ServiceToggleButton", ServiceToggleButton_Click);
        var refreshButton = CreateButton(122, NetworkAccelerationText.RefreshButton, "NetworkAcceleration_RefreshButton", RefreshButton_Click);
        var menuButton = CreateButton(112, NetworkAccelerationText.MenuButton, "NetworkAcceleration_MenuButton", MenuButton_Click);

        _presetListBox = new ListBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent
        };
        _presetListBox.SelectionChanged += PresetListBox_SelectionChanged;
        AutomationProperties.SetAutomationId(_presetListBox, "NetworkAcceleration_TargetsList");
        _presetListBox.Items.Add(CreatePresetItem(
            "Balanced",
            NetworkAccelerationText.ModeBalancedTargetTitle,
            NetworkAccelerationText.ModeBalancedTargetDescription,
            NetworkAccelerationText.ModeBalanced,
            addBottomBorder: true));
        _presetListBox.Items.Add(CreatePresetItem(
            "Gaming",
            NetworkAccelerationText.ModeGamingTargetTitle,
            NetworkAccelerationText.ModeGamingTargetDescription,
            NetworkAccelerationText.ModeGaming,
            addBottomBorder: true));
        _presetListBox.Items.Add(CreatePresetItem(
            "Streaming",
            NetworkAccelerationText.ModeStreamingTargetTitle,
            NetworkAccelerationText.ModeStreamingTargetDescription,
            NetworkAccelerationText.ModeStreaming,
            addBottomBorder: false));

        _modeComboBox = new ComboBox
        {
            Width = 230,
            Margin = new Thickness(0, 14, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeBalanced, Tag = "Balanced" });
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeGaming, Tag = "Gaming" });
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeStreaming, Tag = "Streaming" });
        _modeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;
        AutomationProperties.SetAutomationId(_modeComboBox, "NetworkAcceleration_ModeComboBox");

        _autoOptimizeOnStartupCheckBox = CreateSettingsCheckBox(
            NetworkAccelerationText.AutoOptimizeOnStartup,
            "NetworkAcceleration_AutoOptimizeCheckBox",
            new Thickness(0, 14, 0, 10));
        _resetWinsockCheckBox = CreateSettingsCheckBox(
            NetworkAccelerationText.ResetWinsockOnOptimize,
            "NetworkAcceleration_ResetWinsockCheckBox",
            new Thickness(0, 0, 0, 10));
        _resetTcpIpCheckBox = CreateSettingsCheckBox(
            NetworkAccelerationText.ResetTcpIpOnOptimize,
            "NetworkAcceleration_ResetTcpIpCheckBox",
            new Thickness(0));

        var root = new Grid();
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_FeatureRoot");
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(BuildFallbackStatusStrip(refreshButton, menuButton));
        root.Children.Add(BuildFallbackPresetAndSettingsArea());
        root.Children.Add(BuildFallbackTelemetryArea());
        root.Children.Add(BuildFallbackStatusFooter());

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private Border BuildFallbackStatusStrip(params Wpf.Ui.Controls.Button[] buttons)
    {
        var border = CreateStripBorder(new Thickness(0, 0, 0, 20));
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var summary = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        summary.Children.Add(CreateLabelValue(NetworkAccelerationText.ServiceStateLabel, _serviceStateTextBlock, new Thickness(0, 0, 24, 0)));
        summary.Children.Add(CreateLabelValue(NetworkAccelerationText.CurrentModeLabel, _savedModeSummaryTextBlock, new Thickness(0, 0, 24, 0)));
        summary.Children.Add(CreateLabelValue(NetworkAccelerationText.SessionLabel, _sessionValueTextBlock, new Thickness(0)));
        grid.Children.Add(summary);

        var commandRow = new WrapPanel
        {
            Margin = new Thickness(20, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        commandRow.Children.Add(_serviceToggleButton);
        foreach (var button in buttons)
            commandRow.Children.Add(button);
        Grid.SetColumn(commandRow, 1);
        grid.Children.Add(commandRow);

        border.Child = grid;
        return border;
    }

    private Grid BuildFallbackPresetAndSettingsArea()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
        Grid.SetRow(grid, 1);
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star) });

        var presetStack = new StackPanel();
        presetStack.Children.Add(CreateSectionTextBlock(NetworkAccelerationText.AccelerationTargetsTitle));
        presetStack.Children.Add(CreateDescriptionTextBlock(
            NetworkAccelerationText.AccelerationTargetsDescription,
            new Thickness(0, 6, 0, 14)));
        presetStack.Children.Add(_presetListBox);
        grid.Children.Add(presetStack);

        var divider = new Border
        {
            Width = 1,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        divider.SetResourceReference(Border.BackgroundProperty, "ControlStrokeColorDefaultBrush");
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);

        var detailStack = new StackPanel();
        Grid.SetColumn(detailStack, 2);
        detailStack.Children.Add(BuildFallbackPresetDetailHeader());
        detailStack.Children.Add(BuildFallbackPresetDetailRows());
        detailStack.Children.Add(BuildFallbackActionButtons());

        _settingsExpander = new Expander
        {
            Margin = new Thickness(0, 16, 0, 0),
            IsExpanded = true,
            Header = new TextBlock
            {
                Text = NetworkAccelerationText.SettingsTitle
            }
        };
        ((TextBlock)_settingsExpander.Header).SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        _settingsExpander.Content = BuildFallbackSettingsStack();
        detailStack.Children.Add(_settingsExpander);
        grid.Children.Add(detailStack);

        return grid;
    }

    private Grid BuildFallbackPresetDetailHeader()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        textStack.Children.Add(_presetTitleTextBlock);
        _presetSubtitleTextBlock.Margin = new Thickness(0, 6, 0, 0);
        textStack.Children.Add(_presetSubtitleTextBlock);
        grid.Children.Add(textStack);

        _presetStateTextBlock.FontWeight = FontWeights.SemiBold;
        _presetStateTextBlock.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(_presetStateTextBlock, 1);
        grid.Children.Add(_presetStateTextBlock);
        return grid;
    }

    private Grid BuildFallbackPresetDetailRows()
    {
        var grid = CreateTwoColumnRowsGrid(rowCount: 3, new Thickness(0, 18, 0, 0));
        AddTwoColumnRow(grid, 0, NetworkAccelerationText.RecommendedForLabel, _presetRecommendationTextBlock);
        AddTwoColumnRow(grid, 1, NetworkAccelerationText.OptimizationFocusLabel, _presetActionsTextBlock);
        AddTwoColumnRow(grid, 2, NetworkAccelerationText.SessionLabel, _detailSessionTextBlock);
        return grid;
    }

    private WrapPanel BuildFallbackActionButtons()
    {
        var panel = new WrapPanel { Margin = new Thickness(0, 18, 0, 0) };
        panel.Children.Add(CreateButton(
            196,
            NetworkAccelerationText.RunQuickOptimizationButton,
            "NetworkAcceleration_QuickOptimizeButton",
            QuickOptimizeButton_Click));
        panel.Children.Add(CreateButton(
            178,
            NetworkAccelerationText.ResetNetworkStackButton,
            "NetworkAcceleration_ResetStackButton",
            ResetStackButton_Click));
        panel.Children.Add(CreateButton(
            124,
            NetworkAccelerationText.SaveModeButton,
            "NetworkAcceleration_SaveModeButton",
            SaveModeButton_Click,
            new Thickness(0, 0, 0, 8)));
        return panel;
    }

    private StackPanel BuildFallbackSettingsStack()
    {
        var stack = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        stack.Children.Add(CreateDescriptionTextBlock(NetworkAccelerationText.SettingsDescription));
        stack.Children.Add(_modeComboBox);
        _modeDescriptionTextBlock.Margin = new Thickness(0, 10, 0, 0);
        stack.Children.Add(_modeDescriptionTextBlock);
        stack.Children.Add(_autoOptimizeOnStartupCheckBox);
        stack.Children.Add(_resetWinsockCheckBox);
        stack.Children.Add(_resetTcpIpCheckBox);
        return stack;
    }

    private Border BuildFallbackTelemetryArea()
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 1, 0, 1),
            Padding = new Thickness(0, 16, 0, 16),
            Margin = new Thickness(0, 0, 0, 16)
        };
        border.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        Grid.SetRow(border, 2);

        var stack = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Margin = new Thickness(0, 0, 24, 0) };
        titleStack.Children.Add(CreateSectionTextBlock(NetworkAccelerationText.LiveTelemetryTitle));
        titleStack.Children.Add(CreateDescriptionTextBlock(
            NetworkAccelerationText.LiveTelemetryDescription,
            new Thickness(0, 6, 0, 0)));
        header.Children.Add(titleStack);

        var updated = CreateLabelValue(NetworkAccelerationText.UpdatedLabel, _updatedValueTextBlock, new Thickness(0));
        Grid.SetColumn(updated, 1);
        header.Children.Add(updated);
        stack.Children.Add(header);

        var metrics = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        metrics.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        metrics.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        metrics.ColumnDefinitions.Add(new ColumnDefinition());
        metrics.ColumnDefinitions.Add(new ColumnDefinition());
        metrics.ColumnDefinitions.Add(new ColumnDefinition());
        AddMetric(metrics, 0, 0, NetworkAccelerationText.CurrentDownloadLabel, _downloadValueTextBlock, new Thickness(0, 0, 18, 16));
        AddMetric(metrics, 0, 1, NetworkAccelerationText.CurrentUploadLabel, _uploadValueTextBlock, new Thickness(0, 0, 18, 16));
        AddMetric(metrics, 0, 2, NetworkAccelerationText.PeakTrafficLabel, _peakValueTextBlock, new Thickness(0, 0, 0, 16));
        AddMetric(metrics, 1, 0, NetworkAccelerationText.DownloadTotalLabel, _downloadTotalTextBlock, new Thickness(0, 0, 18, 0));
        AddMetric(metrics, 1, 1, NetworkAccelerationText.UploadTotalLabel, _uploadTotalTextBlock, new Thickness(0, 0, 18, 0));
        AddMetric(metrics, 1, 2, NetworkAccelerationText.ActiveAdapterLabel, _adapterValueTextBlock, new Thickness(0));
        stack.Children.Add(metrics);

        border.Child = stack;
        return border;
    }

    private StackPanel BuildFallbackStatusFooter()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        Grid.SetRow(panel, 3);
        panel.Children.Add(_statusIcon);
        panel.Children.Add(_statusTextBlock);
        AutomationProperties.SetAutomationId(_statusTextBlock, "NetworkAcceleration_StatusText");
        return panel;
    }

    private static Border CreateStripBorder(Thickness margin)
    {
        var border = new Border
        {
            Margin = margin,
            Padding = new Thickness(12, 9, 12, 9),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        border.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        return border;
    }

    private static StackPanel CreateLabelValue(string label, TextBlock value, Thickness margin)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = margin,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(CreateSecondaryTextBlock(label));
        value.Margin = new Thickness(8, 0, 0, 0);
        value.FontWeight = FontWeights.SemiBold;
        panel.Children.Add(value);
        return panel;
    }

    private static ListBoxItem CreatePresetItem(string tag, string title, string description, string label, bool addBottomBorder)
    {
        var row = new Border
        {
            Padding = new Thickness(0, 10, 0, 12),
            Background = Brushes.Transparent,
            BorderThickness = addBottomBorder ? new Thickness(0, 0, 0, 1) : new Thickness(0)
        };
        row.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        textStack.Children.Add(CreatePrimaryTextBlock(title));
        textStack.Children.Add(CreateSecondaryTextBlock(description, new Thickness(0, 5, 0, 0)));
        grid.Children.Add(textStack);

        var labelText = CreateSecondaryTextBlock(label);
        labelText.FontWeight = FontWeights.SemiBold;
        labelText.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(labelText, 1);
        grid.Children.Add(labelText);

        row.Child = grid;
        return new ListBoxItem
        {
            Tag = tag,
            Content = row,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };
    }

    private static Grid CreateTwoColumnRowsGrid(int rowCount, Thickness margin)
    {
        var grid = new Grid { Margin = margin };
        for (var i = 0; i < rowCount; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        return grid;
    }

    private static void AddTwoColumnRow(Grid grid, int row, string label, TextBlock value)
    {
        var top = row == 0 ? 0 : 14;
        var labelText = CreateSecondaryTextBlock(label, new Thickness(0, top, 0, 0));
        Grid.SetRow(labelText, row);
        grid.Children.Add(labelText);

        value.Margin = new Thickness(12, top, 0, 0);
        value.FontWeight = FontWeights.SemiBold;
        Grid.SetRow(value, row);
        Grid.SetColumn(value, 1);
        grid.Children.Add(value);
    }

    private static void AddMetric(Grid grid, int row, int column, string label, TextBlock value, Thickness margin)
    {
        var panel = new StackPanel { Margin = margin };
        panel.Children.Add(CreateSecondaryTextBlock(label));
        value.Margin = new Thickness(0, 5, 0, 0);
        value.FontWeight = FontWeights.SemiBold;
        panel.Children.Add(value);
        Grid.SetRow(panel, row);
        Grid.SetColumn(panel, column);
        grid.Children.Add(panel);
    }

    private CheckBox CreateSettingsCheckBox(string content, string automationId, Thickness margin)
    {
        var checkBox = new CheckBox
        {
            Content = content,
            Margin = margin
        };
        checkBox.Checked += SettingsCheckBox_Changed;
        checkBox.Unchecked += SettingsCheckBox_Changed;
        AutomationProperties.SetAutomationId(checkBox, automationId);
        return checkBox;
    }

    private static Wpf.Ui.Controls.Button CreateButton(
        double minWidth,
        string? content,
        string automationId,
        RoutedEventHandler clickHandler,
        Thickness? margin = null)
    {
        var button = new Wpf.Ui.Controls.Button
        {
            MinWidth = minWidth,
            Content = content,
            Margin = margin ?? new Thickness(0, 0, 8, 8)
        };
        button.Click += clickHandler;
        AutomationProperties.SetAutomationId(button, automationId);
        return button;
    }

    private static TextBlock CreateSectionTextBlock(string? text = null)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 18,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        return textBlock;
    }

    private static TextBlock CreateMetricValueTextBlock()
    {
        var textBlock = CreateValueTextBlock();
        textBlock.FontSize = 18;
        return textBlock;
    }

    private static TextBlock CreateValueTextBlock()
    {
        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        return textBlock;
    }

    private static TextBlock CreatePrimaryTextBlock(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        return textBlock;
    }

    private static TextBlock CreateDescriptionTextBlock(string? text = null, Thickness? margin = null)
    {
        return CreateSecondaryTextBlock(text ?? string.Empty, margin);
    }

    private static TextBlock CreateSecondaryTextBlock(string text, Thickness? margin = null)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap
        };
        if (margin is not null)
            textBlock.Margin = margin.Value;
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return textBlock;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        SynchronizeRuntimeState();
        RefreshTelemetry();
        _telemetryTimer.Start();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _telemetryTimer.Stop();
        _telemetryService.Dispose();
    }

    private void TelemetryTimer_Tick(object? sender, EventArgs e)
    {
        RefreshTelemetry();
        UpdateSessionPresentation();
    }

    private void LoadSavedSettings()
    {
        NetworkAccelerationSettingsBinding.ApplyToggleSettings(
            _plugin.Settings,
            _autoOptimizeOnStartupCheckBox,
            _resetWinsockCheckBox,
            _resetTcpIpCheckBox);
    }

    private void LoadCurrentMode()
    {
        var modeValue = _plugin.Settings.PreferredMode.ToString();
        foreach (var item in _modeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && tag.Equals(modeValue, StringComparison.OrdinalIgnoreCase))
            {
                _modeComboBox.SelectedItem = item;
                return;
            }
        }

        if (_modeComboBox.Items.Count > 0)
            _modeComboBox.SelectedIndex = 0;
    }

    private void SetPresetSelectionFromMode(NetworkAccelerationMode mode)
    {
        _isSelectionSyncInProgress = true;
        try
        {
            foreach (var item in _presetListBox.Items.OfType<ListBoxItem>())
            {
                if (item.Tag is string tag && tag.Equals(mode.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _presetListBox.SelectedItem = item;
                    return;
                }
            }
        }
        finally
        {
            _isSelectionSyncInProgress = false;
        }
    }

    private void SetModeSelection(NetworkAccelerationMode mode)
    {
        _isSelectionSyncInProgress = true;
        try
        {
            foreach (var item in _modeComboBox.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag is string tag && tag.Equals(mode.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _modeComboBox.SelectedItem = item;
                    return;
                }
            }
        }
        finally
        {
            _isSelectionSyncInProgress = false;
        }
    }

    private NetworkAccelerationMode? ParseSelectedMode()
    {
        if (_modeComboBox.SelectedItem is ComboBoxItem comboItem &&
            comboItem.Tag is string modeTag &&
            Enum.TryParse(modeTag, true, out NetworkAccelerationMode mode))
        {
            return mode;
        }

        if (_presetListBox.SelectedItem is ListBoxItem listItem &&
            listItem.Tag is string listTag &&
            Enum.TryParse(listTag, true, out mode))
        {
            return mode;
        }

        return null;
    }

    private void PresetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSelectionSyncInProgress)
            return;

        if (_presetListBox.SelectedItem is ListBoxItem item &&
            item.Tag is string modeTag &&
            Enum.TryParse(modeTag, true, out NetworkAccelerationMode mode))
        {
            SetModeSelection(mode);
        }

        UpdateModeDescription();
        UpdatePresetDetails();
    }

    private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSelectionSyncInProgress)
            return;

        var mode = ParseSelectedMode();
        if (mode is not null)
            SetPresetSelectionFromMode(mode.Value);

        UpdateModeDescription();
        UpdatePresetDetails();
    }

    private void SettingsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SetStatus(NetworkAccelerationText.SettingsPendingSave, false);
        UpdatePresetDetails();
    }

    private async void ServiceToggleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (IsRuntimeRunning())
            {
                _plugin.Runtime.Stop();
                _isServiceRunning = false;
                _sessionStartedAt = null;
                SetStatus(NetworkAccelerationText.StatusServiceStopped, false);
            }
            else
            {
                _plugin.Runtime.Start();
                _isServiceRunning = true;
                _sessionStartedAt = GetRuntimeSessionStartTime() ?? DateTimeOffset.Now;
                SetStatus(NetworkAccelerationText.StatusServiceStarted, false);
                await Task.Yield();
            }

            SynchronizeRuntimeState();
            UpdateSessionPresentation();
        }
        catch (Exception ex)
        {
            SetStatus($"{NetworkAccelerationText.ErrorPrefix}: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ServiceToggleButton_Click error: {ex.Message}", ex);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshTelemetry();
        UpdatePresetDetails();
        SetStatus(NetworkAccelerationText.StatusRefreshed, false);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsExpander.IsExpanded = !_settingsExpander.IsExpanded;
    }

    private async void QuickOptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var success = await _plugin.RunQuickOptimizationAsync().ConfigureAwait(true);
            SetStatus(
                success ? NetworkAccelerationText.StatusQuickOptimizationCompleted : NetworkAccelerationText.StatusQuickOptimizationFailed,
                !success);
            RefreshTelemetry();
        }
        catch (Exception ex)
        {
            SetStatus($"{NetworkAccelerationText.ErrorPrefix}: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"QuickOptimizeButton_Click error: {ex.Message}", ex);
        }
    }

    private async void ResetStackButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var success = await _plugin.ResetNetworkStackAsync().ConfigureAwait(true);
            SetStatus(
                success ? NetworkAccelerationText.StatusResetCompleted : NetworkAccelerationText.StatusResetFailed,
                !success);
            RefreshTelemetry();
        }
        catch (Exception ex)
        {
            SetStatus($"{NetworkAccelerationText.ErrorPrefix}: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ResetStackButton_Click error: {ex.Message}", ex);
        }
    }

    private async void SaveModeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = ParseSelectedMode();
            if (mode is null)
            {
                SetStatus(NetworkAccelerationText.StatusSelectValidMode, true);
                return;
            }

            var updatedSettings = NetworkAccelerationSettingsBinding.BuildUpdatedSettings(
                _plugin.Settings,
                _autoOptimizeOnStartupCheckBox,
                _resetWinsockCheckBox,
                _resetTcpIpCheckBox,
                preferredMode: mode.Value);

            await _plugin.ApplySettingsAsync(updatedSettings).ConfigureAwait(true);

            UpdateModeDescription();
            UpdateSavedModeSummary();
            UpdatePresetDetails();
            SetStatus(NetworkAccelerationText.StatusModeSaved, false);
        }
        catch (Exception ex)
        {
            SetStatus($"{NetworkAccelerationText.ErrorPrefix}: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"SaveModeButton_Click error: {ex.Message}", ex);
        }
    }

    private void RefreshTelemetry()
    {
        SynchronizeRuntimeState();
        var snapshot = _telemetryService.Capture();
        _telemetrySamples.Add(snapshot);
        if (_telemetrySamples.Count > MaxTelemetrySamples)
            _telemetrySamples.RemoveAt(0);

        UpdateTelemetrySummary(snapshot);
    }

    private void UpdateTelemetrySummary(NetworkAccelerationTelemetrySnapshot snapshot)
    {
        _downloadValueTextBlock.Text = FormatRate(snapshot.DownloadMbps);
        _uploadValueTextBlock.Text = FormatRate(snapshot.UploadMbps);
        _peakValueTextBlock.Text = FormatRate(_telemetrySamples.Count == 0
            ? 0
            : _telemetrySamples.Max(sample => Math.Max(sample.DownloadMbps, sample.UploadMbps)));
        _adapterValueTextBlock.Text = snapshot.InterfaceName;
        _downloadTotalTextBlock.Text = FormatDataSize(snapshot.TotalReceivedBytes);
        _uploadTotalTextBlock.Text = FormatDataSize(snapshot.TotalSentBytes);
        _updatedValueTextBlock.Text = snapshot.Timestamp.ToLocalTime().ToString("HH:mm:ss", Culture);
    }

    private void UpdateModeDescription()
    {
        var mode = ParseSelectedMode() ?? NetworkAccelerationMode.Balanced;
        _modeDescriptionTextBlock.Text = NetworkAccelerationPresentation.GetModePresentation(mode).Description;
    }

    private void UpdateSavedModeSummary()
    {
        _savedModeSummaryTextBlock.Text = GetModeDisplayName(_plugin.Settings.PreferredMode);
    }

    private void UpdatePresetDetails()
    {
        var mode = ParseSelectedMode() ?? NetworkAccelerationMode.Balanced;
        var presentation = NetworkAccelerationPresentation.GetModePresentation(mode);
        _presetTitleTextBlock.Text = presentation.TargetTitle;
        _presetSubtitleTextBlock.Text = presentation.TargetDescription;
        _presetRecommendationTextBlock.Text = presentation.RecommendedFor;
        _presetActionsTextBlock.Text = presentation.Focus;
        _presetStateTextBlock.Text = _isServiceRunning
            ? NetworkAccelerationText.PresetStateActive
            : NetworkAccelerationText.PresetStateReady;

        UpdateSessionPresentation();
    }

    private void UpdateSessionPresentation()
    {
        _serviceStateTextBlock.Text = _isServiceRunning
            ? NetworkAccelerationText.ServiceStateRunning
            : NetworkAccelerationText.ServiceStateStopped;

        _serviceToggleButton.Content = _isServiceRunning
            ? NetworkAccelerationText.StopServiceButton
            : NetworkAccelerationText.StartServiceButton;

        var sessionText = NetworkAccelerationText.SessionNotStarted;
        if (_isServiceRunning && _sessionStartedAt is not null)
        {
            var elapsed = DateTimeOffset.Now - _sessionStartedAt.Value;
            if (elapsed < TimeSpan.Zero)
                elapsed = TimeSpan.Zero;
            sessionText = FormatDuration(elapsed);
        }

        _sessionValueTextBlock.Text = sessionText;
        _detailSessionTextBlock.Text = sessionText;
        _presetStateTextBlock.Text = _isServiceRunning
            ? NetworkAccelerationText.PresetStateActive
            : NetworkAccelerationText.PresetStateReady;
    }

    private void SynchronizeRuntimeState()
    {
        _isServiceRunning = IsRuntimeRunning();

        if (_isServiceRunning)
        {
            _sessionStartedAt ??= GetRuntimeSessionStartTime() ?? DateTimeOffset.Now;
        }
        else
        {
            _sessionStartedAt = null;
        }
    }

    private bool IsRuntimeRunning()
    {
        return _plugin.Runtime.IsRunning;
    }

    private DateTimeOffset? GetRuntimeSessionStartTime()
    {
        var earliestSampleUtc = _plugin.Runtime.GetRecentSamples()
            .Select(sample => sample.TimestampUtc)
            .DefaultIfEmpty()
            .Min();

        if (earliestSampleUtc == default)
            return null;

        return new DateTimeOffset(DateTime.SpecifyKind(earliestSampleUtc, DateTimeKind.Utc));
    }

    private void SetStatus(string text, bool isError)
    {
        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? ResolveBrush("SystemFillColorCriticalBrush", SystemColors.ControlTextBrush)
            : ResolveBrush("SystemFillColorSuccessBrush", SystemColors.ControlTextBrush);

        _statusIcon.Symbol = isError
            ? Wpf.Ui.Controls.SymbolRegular.ErrorCircle24
            : Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
        _statusIcon.Foreground = _statusTextBlock.Foreground;
    }

    private static Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }

    private static string GetModeDisplayName(NetworkAccelerationMode mode)
    {
        return NetworkAccelerationPresentation.GetModePresentation(mode).DisplayName;
    }

    private static string FormatRate(double mbps)
    {
        return string.Format(Culture, NetworkAccelerationText.MbpsValueFormat, mbps);
    }

    private static string FormatDataSize(long bytes)
    {
        double value = bytes;
        var suffixIndex = 0;

        while (value >= 1024 && suffixIndex < DataSizeSuffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return string.Format(Culture, "{0:0.#} {1}", value, DataSizeSuffixes[suffixIndex]);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}",
                (int)duration.TotalHours,
                duration.Minutes,
                duration.Seconds);

        return duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }
}
