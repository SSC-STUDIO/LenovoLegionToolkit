using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
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
        // Initialize all field references first
        _serviceStateTextBlock = CreateValueTextBlock();
        _savedModeSummaryTextBlock = CreateValueTextBlock();
        _sessionValueTextBlock = CreateValueTextBlock();
        _presetTitleTextBlock = CreateSectionTextBlock();
        _presetSubtitleTextBlock = CreateDescriptionTextBlock();
        _presetStateTextBlock = CreateValueTextBlock();
        _presetRecommendationTextBlock = CreateValueTextBlock();
        _presetActionsTextBlock = CreateValueTextBlock();
        _plannedStepsTextBlock = CreateValueTextBlock();
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

        _serviceToggleButton = CreateButton(140, null, "NetworkAcceleration_ServiceToggleButton", ServiceToggleButton_Click);
        var refreshButton = CreateButton(36, string.Empty, "NetworkAcceleration_RefreshButton", RefreshButton_Click);
        var menuButton = CreateButton(36, string.Empty, "NetworkAcceleration_MenuButton", MenuButton_Click);

        _presetListBox = new ListBox
        {
            BorderThickness = new Thickness(0),
            Background = null,
            ItemContainerStyle = CreatePresetItemContainerStyle()
        };
        _presetListBox.SelectionChanged += PresetListBox_SelectionChanged;
        AutomationProperties.SetAutomationId(_presetListBox, "NetworkAcceleration_TargetsList");
        _presetListBox.Items.Add(CreatePresetItem(
            "Balanced",
            NetworkAccelerationText.ModeBalancedTargetTitle,
            NetworkAccelerationText.ModeBalancedTargetDescription,
            NetworkAccelerationText.ModeBalanced));
        _presetListBox.Items.Add(CreatePresetItem(
            "Gaming",
            NetworkAccelerationText.ModeGamingTargetTitle,
            NetworkAccelerationText.ModeGamingTargetDescription,
            NetworkAccelerationText.ModeGaming));
        _presetListBox.Items.Add(CreatePresetItem(
            "Streaming",
            NetworkAccelerationText.ModeStreamingTargetTitle,
            NetworkAccelerationText.ModeStreamingTargetDescription,
            NetworkAccelerationText.ModeStreaming));

        _modeComboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 12)
        };
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeBalanced, Tag = "Balanced" });
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeGaming, Tag = "Gaming" });
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeStreaming, Tag = "Streaming" });
        _modeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;
        AutomationProperties.SetAutomationId(_modeComboBox, "NetworkAcceleration_ModeComboBox");

        _autoOptimizeOnStartupCheckBox = CreateSettingsCheckBox(
            NetworkAccelerationText.AutoOptimizeOnStartup,
            "NetworkAcceleration_AutoOptimizeCheckBox",
            new Thickness(0, 0, 0, 12));
        _resetWinsockCheckBox = CreateSettingsCheckBox(
            NetworkAccelerationText.ResetWinsockOnOptimize,
            "NetworkAcceleration_ResetWinsockCheckBox",
            new Thickness(0, 0, 0, 12));
        _resetTcpIpCheckBox = CreateSettingsCheckBox(
            NetworkAccelerationText.ResetTcpIpOnOptimize,
            "NetworkAcceleration_ResetTcpIpCheckBox",
            new Thickness(0));

        // Build root Grid with 3 rows: Hero, TabControl, StatusBar
        var root = new Grid();
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_FeatureRoot");
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(BuildFallbackHeroBanner(refreshButton, menuButton));
        root.Children.Add(BuildFallbackTabControl());
        root.Children.Add(BuildFallbackStatusBar());

        Content = root;
    }

    private Border BuildFallbackHeroBanner(params Wpf.Ui.Controls.Button[] buttons)
    {
        var border = CreateStripBorder(new Thickness(0, 0, 0, 16));
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var summary = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        summary.Children.Add(CreateLabelValue(NetworkAccelerationText.ServiceStateLabel, _serviceStateTextBlock, new Thickness(0, 0, 18, 4)));
        summary.Children.Add(CreateLabelValue(NetworkAccelerationText.CurrentModeLabel, _savedModeSummaryTextBlock, new Thickness(0, 0, 18, 4)));
        summary.Children.Add(CreateLabelValue(NetworkAccelerationText.SessionLabel, _sessionValueTextBlock, new Thickness(0, 0, 18, 4)));
        grid.Children.Add(summary);

        var commandRow = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        commandRow.Children.Add(_serviceToggleButton);
        foreach (var button in buttons)
        {
            commandRow.Children.Add(button);
        }

        Grid.SetColumn(commandRow, 1);
        grid.Children.Add(commandRow);

        border.Child = grid;
        return border;
    }

    private TabControl BuildFallbackTabControl()
    {
        var tabControl = new TabControl
        {
            BorderThickness = new Thickness(0),
            Background = null
        };
        AutomationProperties.SetAutomationId(tabControl, "NetworkAcceleration_TabControl");

        tabControl.Items.Add(BuildFallbackDashboardTab());
        tabControl.Items.Add(BuildFallbackOptimizationTab());

        Grid.SetRow(tabControl, 1);
        return tabControl;
    }

    private TabItem BuildFallbackDashboardTab()
    {
        var tab = new TabItem();
        tab.Header = NetworkAccelerationText.FeatureOverviewTitle;
        AutomationProperties.SetAutomationId(tab, "NetworkAcceleration_DashboardTab");

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var stack = new StackPanel { Margin = new Thickness(0) };

        // Overview card
        stack.Children.Add(BuildFallbackOverviewCard());

        // Download + Upload metric cards
        stack.Children.Add(BuildFallbackMetricCardsGrid());

        // Peak + Adapter cards
        stack.Children.Add(BuildFallbackPeakAndAdapterGrid());

        // Quick actions card
        stack.Children.Add(BuildFallbackQuickActionsCard());

        scrollViewer.Content = stack;
        tab.Content = scrollViewer;
        return tab;
    }

    private Border BuildFallbackOverviewCard()
    {
        var border = CreateCardBorder();
        var stack = new StackPanel();
        stack.Children.Add(CreateSectionTextBlock(NetworkAccelerationText.FeatureOverviewTitle));
        stack.Children.Add(CreateDescriptionTextBlock(
            NetworkAccelerationText.FeatureOverviewDescription,
            new Thickness(0, 6, 0, 0)));
        border.Child = stack;
        return border;
    }

    private Grid BuildFallbackMetricCardsGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Download card
        var dlBorder = CreateCardBorder();
        var dlStack = new StackPanel();
        var dlHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        dlHeader.Children.Add(CreateSecondaryTextBlock(NetworkAccelerationText.CurrentDownloadLabel));
        dlStack.Children.Add(dlHeader);
        _downloadValueTextBlock.FontSize = 28;
        dlStack.Children.Add(_downloadValueTextBlock);
        dlStack.Children.Add(CreateSecondaryTextBlock(NetworkAccelerationText.DownloadTotalLabel, new Thickness(0, 10, 0, 0)));
        _downloadTotalTextBlock.FontSize = 14;
        _downloadTotalTextBlock.FontWeight = FontWeights.SemiBold;
        dlStack.Children.Add(_downloadTotalTextBlock);
        dlBorder.Child = dlStack;
        Grid.SetColumn(dlBorder, 0);
        grid.Children.Add(dlBorder);

        // Upload card
        var ulBorder = CreateCardBorder();
        var ulStack = new StackPanel();
        var ulHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        ulHeader.Children.Add(CreateSecondaryTextBlock(NetworkAccelerationText.CurrentUploadLabel));
        ulStack.Children.Add(ulHeader);
        _uploadValueTextBlock.FontSize = 28;
        ulStack.Children.Add(_uploadValueTextBlock);
        ulStack.Children.Add(CreateSecondaryTextBlock(NetworkAccelerationText.UploadTotalLabel, new Thickness(0, 10, 0, 0)));
        _uploadTotalTextBlock.FontSize = 14;
        _uploadTotalTextBlock.FontWeight = FontWeights.SemiBold;
        ulStack.Children.Add(_uploadTotalTextBlock);
        ulBorder.Child = ulStack;
        Grid.SetColumn(ulBorder, 2);
        grid.Children.Add(ulBorder);

        return grid;
    }

    private Grid BuildFallbackPeakAndAdapterGrid()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Peak card
        var peakBorder = CreateCardBorder();
        var peakStack = new StackPanel();
        peakStack.Children.Add(CreateSecondaryTextBlock(NetworkAccelerationText.PeakTrafficLabel, new Thickness(0, 0, 0, 10)));
        _peakValueTextBlock.FontSize = 28;
        peakStack.Children.Add(_peakValueTextBlock);
        peakStack.Children.Add(CreateLabelValue(NetworkAccelerationText.UpdatedLabel, _updatedValueTextBlock, new Thickness(0, 10, 0, 0)));
        peakBorder.Child = peakStack;
        Grid.SetColumn(peakBorder, 0);
        grid.Children.Add(peakBorder);

        // Adapter card
        var adapterBorder = CreateCardBorder();
        var adapterStack = new StackPanel();
        adapterStack.Children.Add(CreateSecondaryTextBlock(NetworkAccelerationText.ActiveAdapterLabel, new Thickness(0, 0, 0, 10)));
        _adapterValueTextBlock.FontSize = 18;
        _adapterValueTextBlock.FontWeight = FontWeights.SemiBold;
        adapterStack.Children.Add(_adapterValueTextBlock);
        adapterStack.Children.Add(CreateSecondaryTextBlock(NetworkAccelerationText.LiveTelemetryDescription, new Thickness(0, 10, 0, 0)));
        adapterBorder.Child = adapterStack;
        Grid.SetColumn(adapterBorder, 2);
        grid.Children.Add(adapterBorder);

        return grid;
    }

    private Border BuildFallbackQuickActionsCard()
    {
        var border = CreateCardBorder();
        var stack = new StackPanel();
        stack.Children.Add(CreateSectionTextBlock(NetworkAccelerationText.QuickActionsTitle));
        stack.Children.Add(CreateDescriptionTextBlock(
            NetworkAccelerationText.QuickActionsDescription,
            new Thickness(0, 6, 0, 14)));
        var wrap = new WrapPanel();
        wrap.Children.Add(CreateButton(
            170,
            NetworkAccelerationText.RunQuickOptimizationButton,
            "NetworkAcceleration_QuickOptimizeButton",
            QuickOptimizeButton_Click,
            new Thickness(0, 0, 8, 8)));
        wrap.Children.Add(CreateButton(
            170,
            NetworkAccelerationText.ResetNetworkStackButton,
            "NetworkAcceleration_ResetStackButton",
            ResetStackButton_Click,
            new Thickness(0, 0, 8, 8)));
        stack.Children.Add(wrap);
        border.Child = stack;
        return border;
    }

    private TabItem BuildFallbackOptimizationTab()
    {
        var tab = new TabItem();
        tab.Header = NetworkAccelerationText.AccelerationTargetsTitle;
        AutomationProperties.SetAutomationId(tab, "NetworkAcceleration_OptimizationTab");

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var grid = new Grid { Margin = new Thickness(0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left: Targets
        var leftBorder = CreateCardBorder();
        var leftStack = new StackPanel();
        leftStack.Children.Add(CreateSectionTextBlock(NetworkAccelerationText.AccelerationTargetsTitle));
        leftStack.Children.Add(CreateDescriptionTextBlock(
            NetworkAccelerationText.AccelerationTargetsDescription,
            new Thickness(0, 6, 0, 12)));
        leftStack.Children.Add(_presetListBox);
        leftBorder.Child = leftStack;
        Grid.SetColumn(leftBorder, 0);
        grid.Children.Add(leftBorder);

        // Right: Details
        var rightBorder = CreateCardBorder();
        var rightStack = new StackPanel();

        // Preset detail header
        var detailHeaderGrid = new Grid();
        detailHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        detailHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var detailTextStack = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        detailTextStack.Children.Add(_presetTitleTextBlock);
        _presetSubtitleTextBlock.Margin = new Thickness(0, 4, 0, 0);
        detailTextStack.Children.Add(_presetSubtitleTextBlock);
        detailHeaderGrid.Children.Add(detailTextStack);
        _presetStateTextBlock.FontWeight = FontWeights.SemiBold;
        _presetStateTextBlock.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetColumn(_presetStateTextBlock, 1);
        detailHeaderGrid.Children.Add(_presetStateTextBlock);
        rightStack.Children.Add(detailHeaderGrid);

        // Preset detail rows
        var detailRowsGrid = CreateTwoColumnRowsGrid(3, new Thickness(0, 14, 0, 0));
        AddTwoColumnRow(detailRowsGrid, 0, NetworkAccelerationText.RecommendedForLabel, _presetRecommendationTextBlock);
        AddTwoColumnRow(detailRowsGrid, 1, NetworkAccelerationText.OptimizationFocusLabel, _presetActionsTextBlock);
        AddTwoColumnRow(detailRowsGrid, 2, NetworkAccelerationText.SessionLabel, _detailSessionTextBlock);
        rightStack.Children.Add(detailRowsGrid);

        // Action buttons
        var actionWrap = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        actionWrap.Children.Add(CreateButton(
            170,
            NetworkAccelerationText.RunQuickOptimizationButton,
            "NetworkAcceleration_QuickOptimizeButton_Fallback",
            QuickOptimizeButton_Click,
            new Thickness(0, 0, 8, 8)));
        actionWrap.Children.Add(CreateButton(
            170,
            NetworkAccelerationText.ResetNetworkStackButton,
            "NetworkAcceleration_ResetStackButton_Fallback",
            ResetStackButton_Click,
            new Thickness(0, 0, 8, 8)));
        actionWrap.Children.Add(CreateButton(
            120,
            NetworkAccelerationText.SaveModeButton,
            "NetworkAcceleration_SaveModeButton_Fallback",
            SaveModeButton_Click));
        rightStack.Children.Add(actionWrap);

        // Settings expander
        _settingsExpander = new Expander
        {
            Margin = new Thickness(0, 16, 0, 0),
            IsExpanded = false
        };
        _settingsExpander.Header = new TextBlock { Text = NetworkAccelerationText.SettingsTitle };
        ((TextBlock)_settingsExpander.Header).SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        _settingsExpander.Content = BuildFallbackSettingsStack();
        rightStack.Children.Add(_settingsExpander);

        rightBorder.Child = rightStack;
        Grid.SetColumn(rightBorder, 2);
        grid.Children.Add(rightBorder);

        scrollViewer.Content = grid;
        tab.Content = scrollViewer;
        return tab;
    }

    private Border BuildFallbackStatusBar()
    {
        var border = CreateStripBorder(new Thickness(0));
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(_statusIcon);
        panel.Children.Add(_statusTextBlock);
        AutomationProperties.SetAutomationId(_statusTextBlock, "NetworkAcceleration_StatusText");
        border.Child = panel;
        Grid.SetRow(border, 2);
        return border;
    }

    private static Border CreateCardBorder()
    {
        var border = new Border
        {
            Padding = new Thickness(18, 16, 18, 16),
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 14)
        };
        border.SetResourceReference(Border.BackgroundProperty, "ControlFillColorDefaultBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        return border;
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

    private static ListBoxItem CreatePresetItem(string tag, string title, string description, string label)
    {
        var row = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        row.SetBinding(Border.BackgroundProperty, new Binding(nameof(Control.Background))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1)
        });
        row.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Control.BorderBrush))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1)
        });

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        textStack.Children.Add(CreatePrimaryTextBlock(title));
        textStack.Children.Add(CreateSecondaryTextBlock(description, new Thickness(0, 4, 0, 0)));
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
            Margin = new Thickness(0, 0, 0, 8)
        };
    }

    private static Style CreatePresetItemContainerStyle()
    {
        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 8)));
        style.Setters.Add(new Setter(Control.BackgroundProperty, null));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension("ControlStrokeColorDefaultBrush")));
        style.Setters.Add(new Setter(Control.TemplateProperty, CreatePresetItemContainerTemplate()));

        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("ControlFillColorSecondaryBrush")));
        hoverTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension("SystemAccentColorPrimaryBrush")));
        style.Triggers.Add(hoverTrigger);

        var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("ControlFillColorSecondaryBrush")));
        selectedTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension("SystemAccentColorPrimaryBrush")));
        style.Triggers.Add(selectedTrigger);

        return style;
    }

    private static ControlTemplate CreatePresetItemContainerTemplate()
    {
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        presenter.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var template = new ControlTemplate(typeof(ListBoxItem))
        {
            VisualTree = presenter
        };

        var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.56));
        template.Triggers.Add(disabledTrigger);

        return template;
    }

    private static Grid CreateTwoColumnRowsGrid(int rowCount, Thickness margin)
    {
        var grid = new Grid { Margin = margin };
        for (var i = 0; i < rowCount; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

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

    private StackPanel BuildFallbackSettingsStack()
    {
        var stack = new StackPanel { Margin = new Thickness(12, 10, 12, 0) };
        stack.Children.Add(_modeComboBox);
        _modeDescriptionTextBlock.Margin = new Thickness(0, 10, 0, 12);
        stack.Children.Add(_modeDescriptionTextBlock);
        stack.Children.Add(_autoOptimizeOnStartupCheckBox);
        stack.Children.Add(_resetWinsockCheckBox);
        stack.Children.Add(_resetTcpIpCheckBox);
        var plannedLabel = CreateSecondaryTextBlock(NetworkAccelerationText.PlannedStepsTitle, new Thickness(0, 14, 0, 6));
        stack.Children.Add(plannedLabel);
        _plannedStepsTextBlock.Margin = new Thickness(0);
        stack.Children.Add(_plannedStepsTextBlock);
        return stack;
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
        {
            textBlock.Margin = margin.Value;
        }

        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return textBlock;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _telemetryTimer.Tick -= TelemetryTimer_Tick;
        _telemetryTimer.Tick += TelemetryTimer_Tick;
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
        {
            _modeComboBox.SelectedIndex = 0;
        }
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
        {
            return;
        }

        if (_presetListBox.SelectedItem is ListBoxItem item &&
            item.Tag is string modeTag &&
            Enum.TryParse(modeTag, true, out NetworkAccelerationMode mode))
        {
            SetModeSelection(mode);
        }

        UpdateModeDescription();
        UpdatePresetDetails();
        UpdateOptimizationPlanPreview();
    }

    private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSelectionSyncInProgress)
        {
            return;
        }

        var mode = ParseSelectedMode();
        if (mode is not null)
        {
            SetPresetSelectionFromMode(mode.Value);
        }

        UpdateModeDescription();
        UpdatePresetDetails();
        UpdateOptimizationPlanPreview();
    }

    private void SettingsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SetStatus(NetworkAccelerationText.SettingsPendingSave, false);
        UpdatePresetDetails();
        UpdateOptimizationPlanPreview();
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
            PluginLog.Trace($"ServiceToggleButton_Click error: {ex.Message}", ex);
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
            PluginLog.Trace($"QuickOptimizeButton_Click error: {ex.Message}", ex);
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
            PluginLog.Trace($"ResetStackButton_Click error: {ex.Message}", ex);
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
            UpdateOptimizationPlanPreview();
            SetStatus(NetworkAccelerationText.StatusModeSaved, false);
        }
        catch (Exception ex)
        {
            SetStatus($"{NetworkAccelerationText.ErrorPrefix}: {ex.Message}", true);
            PluginLog.Trace($"SaveModeButton_Click error: {ex.Message}", ex);
        }
    }

    private void RefreshTelemetry()
    {
        SynchronizeRuntimeState();
        var snapshot = _telemetryService.Capture();
        _telemetrySamples.Add(snapshot);
        if (_telemetrySamples.Count > MaxTelemetrySamples)
        {
            _telemetrySamples.RemoveAt(0);
        }

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

    private void UpdateOptimizationPlanPreview()
    {
        if (_plannedStepsTextBlock is null)
        {
            return;
        }

        var mode = ParseSelectedMode() ?? _plugin.Settings.PreferredMode;
        var settings = NetworkAccelerationSettingsBinding.BuildUpdatedSettings(
            _plugin.Settings,
            _autoOptimizeOnStartupCheckBox,
            _resetWinsockCheckBox,
            _resetTcpIpCheckBox,
            preferredMode: mode);
        var plan = NetworkAccelerationPlugin.GetOptimizationPlan(settings);
        _plannedStepsTextBlock.Text = NetworkAccelerationPresentation.GetPlanSummary(plan);
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
            {
                elapsed = TimeSpan.Zero;
            }

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
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(earliestSampleUtc, DateTimeKind.Utc));
    }

    private void SetStatus(string text, bool isError)
    {
        if (_statusTextBlock is not null)
        {
            _statusTextBlock.Text = text;
            _statusTextBlock.Foreground = isError
                ? ResolveBrush("SystemFillColorCriticalBrush", SystemColors.ControlTextBrush)
                : ResolveBrush("SystemFillColorSuccessBrush", SystemColors.ControlTextBrush);
        }

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = isError
                ? Wpf.Ui.Controls.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock?.Foreground ?? SystemColors.ControlTextBrush;
        }
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
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}",
                (int)duration.TotalHours,
                duration.Minutes,
                duration.Seconds);
        }

        return duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }
}
