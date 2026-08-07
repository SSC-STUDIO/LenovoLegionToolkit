using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
#if WINDOWS
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Lib.Automation.Serialization;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.System;
#endif

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Avalonia automation editor. The page keeps a local draft and sends the complete
/// ordered pipeline (including step and trigger payloads) to the shared processor only
/// when Save is pressed. On Windows hosts, steps and triggers are edited with typed
/// controls that serialize back through the shared automation serializer.
/// </summary>
public partial class AutomationPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly List<PipelineRow> _rows = [];
    private IReadOnlyList<AutomationPipelineItem> _workspacePipelines = Array.Empty<AutomationPipelineItem>();
    private IReadOnlyList<AutomationTriggerOption> _triggerOptions = Array.Empty<AutomationTriggerOption>();
    private IReadOnlyList<AutomationStepOption> _stepOptions = Array.Empty<AutomationStepOption>();
    private static readonly string[] ManualPipelineIcons =
    [
        "Play24", "Rocket24", "Star24", "Bolt24", "Keyboard24", "Settings24", "Apps24", "Heart24",
    ];
    private bool _isRefreshing;
    private bool _isDirty;

    internal static readonly string[] PipelineIconNames =
    [
        "Play24", "Rocket24", "Star24", "Bolt24", "Keyboard24", "Settings24", "Apps24", "Heart24",
        "Home24", "Info24", "PaintBrush24", "ReceiptPlay24", "Gauge24", "Desktop24", "ArrowSync24",
        "ArrowLeft24", "ArrowRight24", "ArrowUp24", "ArrowDown24", "ArrowReset24", "ArrowClockwise24",
        "ArrowExportLtr24", "ArrowImport24", "ArrowRepeatAll24", "ChevronDown24", "ChevronUp24", "Add24",
        "Edit24", "Delete24", "Save24", "Battery024", "PlugConnected24", "BatteryCharge24", "WeatherMoon24",
        "UsbStick24", "PlugDisconnected24", "LeafOne24", "DeveloperBoard24", "DeveloperBoardLightning20",
        "ScaleFill24", "DesktopPulse24", "TextFontSize24", "Hdr24", "TopSpeed24", "LightbulbCircle24",
        "UsbPlug24", "Mic24", "Tablet24", "Power24", "Checkmark24", "CheckmarkCircle24", "Warning24",
        "ErrorCircle24", "Dismiss24", "ToggleRight24",
        "Flow20", "SquareMultiple24", "ChevronRight24", "Folder20", "MusicNote2Play20", "MoreHorizontal24",
        "WindowConsole20", "BrightnessHigh24", "BrightnessHigh48", "Wifi124", "WifiOff24", "Window16",
        "Window24", "Clock24", "DismissCircle24", "Document24", "Emoji24", "Eye24", "HandRight24",
        "Lightbulb24", "Lock24", "MusicNote124", "Search24", "Share24", "Shield24", "ShoppingBag24",
        "Sleep24", "SportsSoccer24", "StarEmphasis24", "Tag24", "Timer24", "Video24", "WeatherCloudy24",
        "WeatherRain24", "WeatherSunny24", "Bluetooth24", "Call24", "Camera24", "Cloud24", "Cut24",
        "DataLine24", "Flag24", "GameController24", "Globe24", "Headphones24", "History24", "Image24",
        "Library24", "Link24", "Mail24", "Map24", "Moon24", "People24", "Person24", "Phone24", "Pin24",
        "Printer24", "ProjectionScreen24", "Recycle24", "Robot24", "Scanner24", "Wand24",
    ];

    public AutomationPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();

        PageTitle.Text = Get("AutomationPage_Title", "Actions");
        PageDescription.Text = Get("AutomationPage_Actions_Message", "Configure automation pipelines and run quick actions.");
        EnabledTitle.Text = Get("AutomationPage_ActionsEnabled_Title", "Automation service");
        EnabledDescription.Text = Get("AutomationPage_ActionsEnable_Message", "Enable or disable automation event listeners.");
        PipelinesTitle.Text = Get("AutomationPage_QuickActions_Title", "Quick actions");
        PipelinesDescription.Text = Get("AutomationPage_QuickActions_Message", "Run configured pipelines on demand.");
        AddButton.Content = Get("AddNew", "Add new");
        AddAutomaticButton.Content = Get("AutomationPage_AddAutomaticPipeline", "Add automatic");
        SaveButton.Content = Get("Save", "Save");
        RevertButton.Content = Get("Revert", "Revert");
        EmptyText.Text = Get("AutomationPage_QuickActions_Empty", "No automation pipelines configured.");

        AddButton.Click += AddButton_Click;
        AddAutomaticButton.Click += AddAutomaticButton_Click;
        SaveButton.Click += SaveButton_Click;
        RevertButton.Click += RevertButton_Click;
        EnabledToggle.IsCheckedChanged += EnabledToggle_IsCheckedChanged;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            _isRefreshing = true;
            SetFeedback(null);
#if WINDOWS
            _triggerOptions = MergeTriggerOptions(await _platformServices.GetAutomationTriggerOptionsAsync());
#else
            _triggerOptions = await _platformServices.GetAutomationTriggerOptionsAsync();
#endif
            _stepOptions = await _platformServices.GetAutomationStepOptionsAsync();
            var state = await _platformServices.GetAutomationWorkspaceAsync();
            _workspacePipelines = state.Pipelines;
            EnabledToggle.IsChecked = state.IsEnabled;
            PipelineList.Children.Clear();
            _rows.Clear();

            foreach (var pipeline in state.Pipelines)
            {
                var row = CreateRow(pipeline);
                _rows.Add(row);
                PipelineList.Children.Add(row.Card);
            }

#if WINDOWS
            RefreshAutomaticTriggerEditors();
            RefreshQuickActionTargetEditors();
#endif

            EmptyText.IsVisible = _rows.Count == 0;
            _isDirty = false;
            UpdateDirtyState();
        }
        catch (Exception ex)
        {
            SetFeedback(ex.Message);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private PipelineRow CreateRow(AutomationPipelineItem pipeline)
    {
        var nameEditor = new TextBox
        {
            Text = pipeline.Name ?? string.Empty,
            Watermark = Get("AutomationPage_RenamePipeline_Placeholder", "Pipeline name"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 120,
        };
        var summary = new LocalizedTextBlock
        {
            Text = FormatSummary(pipeline.Trigger, pipeline.IsAutomatic, pipeline.Steps.Count),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        };
        var copy = new StackPanel { Spacing = 6, MinWidth = 0 };
        copy.Children.Add(nameEditor);

        TextBox? iconEditor = null;
        if (!pipeline.IsAutomatic)
        {
            var iconPreview = new NavigationIcon
            {
                IconIdentifier = pipeline.IconName ?? "Play24",
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
            };
            iconEditor = new TextBox
            {
                Text = pipeline.IconName ?? string.Empty,
                Watermark = Get("AutomationPage_ChangeIcon", "Icon name"),
                MinWidth = 160,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            ToolTip.SetTip(iconEditor, $"{Get("AutomationPage_ChangeIcon", "Enter a Fluent icon name.")} Examples: {string.Join(", ", ManualPipelineIcons)}");
            AutomationProperties.SetAutomationId(iconEditor, $"AutomationPipeline_{pipeline.Id:N}_Icon");
            var browseIconsButton = new Button
            {
                Content = new NavigationIcon { IconIdentifier = "Folder20", FontSize = 16 },
                MinWidth = 34,
                MinHeight = 30,
                Padding = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Center,
            };
            AutomationProperties.SetName(browseIconsButton, Get("AutomationPage_BrowseIcons", "Browse icons"));
            ToolTip.SetTip(browseIconsButton, Get("AutomationPage_BrowseIcons", "Browse icons"));
            AutomationProperties.SetAutomationId(browseIconsButton, $"AutomationPipeline_{pipeline.Id:N}_BrowseIcons");
            var iconPicker = CreateIconPickerPopup(iconEditor, browseIconsButton);
            var iconRow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 8 };
            iconRow.Children.Add(iconPreview);
            Grid.SetColumn(iconEditor, 1);
            iconRow.Children.Add(iconEditor);
            Grid.SetColumn(browseIconsButton, 2);
            iconRow.Children.Add(browseIconsButton);
            copy.Children.Add(iconRow);
            copy.Children.Add(iconPicker);
            iconEditor.TextChanged += (_, _) => iconPreview.IconIdentifier = NormalizeIconName(iconEditor.Text) ?? "Play24";
        }

        ComboBox? triggerEditor = null;
        TextBox? triggerConfigEditor = null;
#if WINDOWS
        StackPanel? triggerListPanel = null;
        ComboBox? addTriggerEditor = null;
        Button? addTriggerButton = null;
#endif
        if (pipeline.IsAutomatic)
        {
            triggerEditor = new ComboBox
            {
                ItemsSource = _triggerOptions,
                SelectedItem = FindTriggerOption(pipeline),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinWidth = 180,
            };
            AutomationProperties.SetAutomationId(triggerEditor, $"AutomationPipeline_{pipeline.Id:N}_TriggerType");
            copy.Children.Add(triggerEditor);

#if WINDOWS
            triggerListPanel = new StackPanel { Spacing = 5 };
            addTriggerEditor = new ComboBox
            {
                PlaceholderText = Get("AutomationPage_AddTrigger", "Add trigger"),
                MinWidth = 200,
            };
            addTriggerButton = new Button { Content = Get("AutomationPage_AddTrigger", "Add trigger"), MinWidth = 90 };
            AutomationProperties.SetAutomationId(addTriggerEditor, $"AutomationPipeline_{pipeline.Id:N}_AddTrigger");
            AutomationProperties.SetName(addTriggerButton, Get("AutomationPage_AddTrigger", "Add trigger"));
            var addTriggerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            addTriggerPanel.Children.Add(addTriggerEditor);
            addTriggerPanel.Children.Add(addTriggerButton);
            var compositionHint = new LocalizedTextBlock
            {
                Text = Get("AutomationPage_TriggerCompositionHint", "Multiple triggers are combined with AND."),
                Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                FontSize = 12,
            };
            copy.Children.Add(triggerListPanel);
            copy.Children.Add(addTriggerPanel);
            copy.Children.Add(compositionHint);
#else
            triggerConfigEditor = new TextBox
            {
                Text = pipeline.TriggerConfigurationJson ?? (triggerEditor.SelectedItem as AutomationTriggerOption)?.DefaultConfigurationJson ?? string.Empty,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 54,
                Watermark = Get("AutomationPipelineTriggerConfigurationWindow_Title", "Advanced trigger configuration (JSON)"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            ToolTip.SetTip(triggerConfigEditor, Get("AutomationPipelineTriggerConfigurationWindow_Title", "Edit the complete trigger configuration JSON."));
            copy.Children.Add(triggerConfigEditor);
#endif
        }

        var exclusiveEditor = new CheckBox
        {
            Content = Get("AutomationPipelineControl_Exclusive", "Exclusive"),
            IsChecked = pipeline.IsExclusive,
            IsVisible = pipeline.IsAutomatic,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        if (pipeline.IsAutomatic)
            copy.Children.Add(exclusiveEditor);

        var stepsPanel = new StackPanel { Spacing = 5 };
        var stepRows = new List<StepRow>();
#if WINDOWS
        var manualPipelines = pipeline.IsAutomatic
            ? GetManualQuickActionTargets()
            : Array.Empty<AutomationPipelineItem>();
#else
        var manualPipelines = Array.Empty<AutomationPipelineItem>();
#endif
        foreach (var step in pipeline.Steps)
        {
            var option = FindStepOption(step);
            var stepRow = CreateStepRow(option, step.ConfigurationJson, stepsPanel, stepRows, pipeline.IsAutomatic, manualPipelines);
            stepRows.Add(stepRow);
            stepsPanel.Children.Add(stepRow.Card);
        }
        var addStepEditor = new ComboBox
        {
            ItemsSource = GetAvailableStepOptions(pipeline.IsAutomatic),
            PlaceholderText = Get("AutomationPipelineControl_AddStep", "Add step"),
            MinWidth = 220,
        };
        var addStepButton = new Button { Content = Get("AutomationPipelineControl_AddStep", "Add step"), MinWidth = 90 };
        var addStepPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        addStepPanel.Children.Add(addStepEditor);
        addStepPanel.Children.Add(addStepButton);
        copy.Children.Add(summary);
        copy.Children.Add(stepsPanel);
        copy.Children.Add(addStepPanel);

        var runButton = new Button
        {
            Content = Get("Run", "Run"),
            IsEnabled = pipeline.IsAutomatic || pipeline.Steps.Count > 0,
            MinWidth = 72,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var deleteButton = new Button
        {
            Content = Get("Delete", "Delete"),
            MinWidth = 72,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var moveUpButton = CreatePipelineMoveButton("ArrowUp24", "MoveUp", "Move pipeline up");
        var moveDownButton = CreatePipelineMoveButton("ArrowDown24", "MoveDown", "Move pipeline down");
        var automationIdPrefix = $"AutomationPipeline_{pipeline.Id:N}";
        AutomationProperties.SetAutomationId(moveUpButton, $"{automationIdPrefix}_MoveUp");
        AutomationProperties.SetAutomationId(moveDownButton, $"{automationIdPrefix}_MoveDown");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(moveUpButton);
        actions.Children.Add(moveDownButton);
        actions.Children.Add(runButton);
        actions.Children.Add(deleteButton);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 14 };
        grid.Children.Add(copy);
        Grid.SetColumn(actions, 1);
        grid.Children.Add(actions);

        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusCard"),
            Padding = new Thickness(16),
            Child = grid,
        };
        AutomationProperties.SetName(card, pipeline.Name ?? pipeline.Trigger);

        var row = new PipelineRow(pipeline.Id, pipeline.IsAutomatic, nameEditor, iconEditor, card, triggerEditor, triggerConfigEditor, exclusiveEditor, stepsPanel, stepRows)
        {
            SummaryText = summary,
#if WINDOWS
            TriggerListPanel = triggerListPanel,
            AddTriggerEditor = addTriggerEditor,
            AddTriggerButton = addTriggerButton,
#endif
        };
        nameEditor.TextChanged += (_, _) =>
        {
            if (_isRefreshing)
                return;
            MarkDirty();
#if WINDOWS
            if (!row.IsAutomatic)
                RefreshQuickActionTargetEditors();
#endif
        };
        if (iconEditor is not null)
            iconEditor.TextChanged += (_, _) => { if (!_isRefreshing) MarkDirty(); };
        if (triggerConfigEditor is not null)
            triggerConfigEditor.TextChanged += (_, _) => { if (!_isRefreshing) MarkDirty(); };
        exclusiveEditor.IsCheckedChanged += (_, _) => { if (!_isRefreshing) MarkDirty(); };
        triggerEditor?.SelectionChanged += (_, _) =>
        {
            if (_isRefreshing)
                return;
            if (triggerEditor.SelectedItem is AutomationTriggerOption option)
            {
#if WINDOWS
                if (row.TriggerListPanel is not null)
                {
                    row.TriggerRows.Clear();
                    row.TriggerListPanel.Children.Clear();
                    var trigger = DeserializeTrigger(option.DefaultConfigurationJson ?? string.Empty);
                    if (trigger is not null)
                        AddTriggerRow(row, trigger, option.Key, automationIdPrefix);
                    RefreshAddTriggerOptions(row);
                    OnTriggerRowChanged(row);
                    RefreshAutomaticTriggerEditors();
                }
#else
                triggerConfigEditor!.Text = option.DefaultConfigurationJson ?? string.Empty;
#endif
                summary.Text = FormatSummary(option.DisplayName, true, stepRows.Count);
            }
            MarkDirty();
        };
#if WINDOWS
        if (pipeline.IsAutomatic)
        {
            PopulateTriggerRows(row, pipeline.TriggerConfigurationJson ?? string.Empty, automationIdPrefix);
            if (addTriggerButton is not null)
                addTriggerButton.Click += (_, _) => AddTriggerFromCombo(row);
        }
#endif
        addStepButton.Click += (_, _) =>
        {
            if (addStepEditor.SelectedItem is not AutomationStepOption option)
                return;
            var stepRow = CreateStepRow(option, option.DefaultConfigurationJson, stepsPanel, stepRows, pipeline.IsAutomatic, manualPipelines);
            stepRows.Add(stepRow);
            stepsPanel.Children.Add(stepRow.Card);
            summary.Text = FormatSummary(triggerEditor?.SelectedItem is AutomationTriggerOption t ? t.DisplayName : pipeline.Trigger, pipeline.IsAutomatic, stepRows.Count);
            runButton.IsEnabled = true;
            addStepEditor.SelectedItem = null;
            MarkDirty();
        };
        moveUpButton.Click += (_, _) => MovePipeline(row, -1);
        moveDownButton.Click += (_, _) => MovePipeline(row, 1);
        runButton.Click += async (_, _) => await RunPipelineAsync(row);
        deleteButton.Click += (_, _) => DeleteRow(row);
        return row;
    }

    private StepRow CreateStepRow(
        AutomationStepOption option,
        string configurationJson,
        Panel panel,
        List<StepRow> rows,
        bool isAutomatic,
        IReadOnlyList<AutomationPipelineItem>? manualPipelines = null)
    {
        var typeEditor = new ComboBox
        {
            ItemsSource = GetAvailableStepOptions(isAutomatic),
            SelectedItem = option,
            MinWidth = 190,
        };
        var upButton = CreateStepMoveButton("ArrowUp24", "MoveUp", "Move step up");
        var downButton = CreateStepMoveButton("ArrowDown24", "MoveDown", "Move step down");
        var deleteButton = new Button { Content = Get("Delete", "Delete"), MinWidth = 64 };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        buttons.Children.Add(upButton);
        buttons.Children.Add(downButton);
        buttons.Children.Add(deleteButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
        header.Children.Add(typeEditor);
        Grid.SetColumn(buttons, 1);
        header.Children.Add(buttons);

        var body = new StackPanel { Spacing = 5 };
        var configEditor = new TextBox
        {
            Text = configurationJson,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 46,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var validationText = new LocalizedTextBlock
        {
            Foreground = GetResource<IBrush>("StatusCriticalTextBrush"),
            IsVisible = false,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
            FontSize = 12,
        };
        body.Children.Add(configEditor);
        body.Children.Add(validationText);
        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            Child = body,
        };
        var row = new StepRow(option, typeEditor, configEditor, card)
        {
#if WINDOWS
            ValidationText = validationText,
#endif
        };
#if WINDOWS
        RefreshStepEditor(row, configurationJson, body, configEditor, manualPipelines ?? Array.Empty<AutomationPipelineItem>());
#endif
        configEditor.TextChanged += (_, _) => { if (!_isRefreshing) MarkDirty(); };
        typeEditor.SelectionChanged += (_, _) =>
        {
            if (_isRefreshing)
                return;
            if (typeEditor.SelectedItem is AutomationStepOption selected)
            {
#if WINDOWS
                RefreshStepEditor(row, selected.DefaultConfigurationJson, body, configEditor, manualPipelines ?? Array.Empty<AutomationPipelineItem>());
#else
                configEditor.Text = selected.DefaultConfigurationJson;
#endif
            }
            MarkDirty();
        };
        upButton.Click += (_, _) => MoveStep(row, panel, rows, -1);
        downButton.Click += (_, _) => MoveStep(row, panel, rows, 1);
        deleteButton.Click += (_, _) =>
        {
            rows.Remove(row);
            panel.Children.Remove(row.Card);
            MarkDirty();
        };
        return row;
    }

    private Button CreateStepMoveButton(string iconIdentifier, string resourceKey, string fallback)
    {
        var label = Get(resourceKey, fallback);
        var button = new Button
        {
            Content = new NavigationIcon
            {
                IconIdentifier = iconIdentifier,
                FontSize = 16,
            },
            MinWidth = 30,
            MinHeight = 30,
            Padding = new Thickness(4),
        };
        AutomationProperties.SetName(button, label);
        ToolTip.SetTip(button, label);
        return button;
    }

    private Button CreatePipelineMoveButton(string iconIdentifier, string resourceKey, string fallback)
    {
        return CreateStepMoveButton(iconIdentifier, resourceKey, fallback);
    }

    private void MoveStep(StepRow row, Panel panel, List<StepRow> rows, int delta)
    {
        var index = rows.IndexOf(row);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= rows.Count)
            return;
        rows.RemoveAt(index);
        rows.Insert(target, row);
        panel.Children.Remove(row.Card);
        panel.Children.Insert(target, row.Card);
        MarkDirty();
    }

    private void MovePipeline(PipelineRow row, int delta)
    {
        var matchingRows = _rows.Where(candidate => candidate.IsAutomatic == row.IsAutomatic).ToList();
        var index = matchingRows.IndexOf(row);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= matchingRows.Count)
            return;

        var adjacent = matchingRows[target];
        var sourceIndex = _rows.IndexOf(row);
        var targetIndex = _rows.IndexOf(adjacent);
        (_rows[sourceIndex], _rows[targetIndex]) = (_rows[targetIndex], _rows[sourceIndex]);
        PipelineList.Children.Remove(row.Card);
        PipelineList.Children.Insert(targetIndex, row.Card);
        MarkDirty();
    }

    private AutomationTriggerOption? FindTriggerOption(AutomationPipelineItem pipeline) =>
        _triggerOptions.FirstOrDefault(option => string.Equals(option.Key, pipeline.TriggerKey, StringComparison.OrdinalIgnoreCase))
        ?? _triggerOptions.FirstOrDefault(option => string.Equals(option.DisplayName, pipeline.Trigger, StringComparison.OrdinalIgnoreCase));

    private AutomationStepOption FindStepOption(AutomationStepItem step) =>
        _stepOptions.FirstOrDefault(option => string.Equals(option.TypeKey, step.TypeKey, StringComparison.OrdinalIgnoreCase))
        ?? new AutomationStepOption(step.TypeKey, step.DisplayName, step.ConfigurationJson);

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var item = new AutomationPipelineItem(Guid.Empty, Get("AutomationPage_AddManualPipeline_Placeholder", "New quick action"), null, Get("AutomationPage_QuickActions_Title", "Manual quick action"), 0, false)
        {
            IsExclusive = true,
        };
        var row = CreateRow(item);
        row.IsNew = true;
        _rows.Insert(0, row);
        PipelineList.Children.Insert(0, row.Card);
        EmptyText.IsVisible = false;
        MarkDirty();
#if WINDOWS
        RefreshQuickActionTargetEditors();
#endif
        row.NameEditor.Focus();
        row.NameEditor.SelectAll();
    }

    private void AddAutomaticButton_Click(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        var option = GetAvailableNewTriggerOptions().FirstOrDefault();
#else
        var option = _triggerOptions.FirstOrDefault();
#endif
        if (option is null)
        {
            SetFeedback(Get("AutomationPage_AddAutomaticPipeline_Error", "Automatic triggers are unavailable."));
            return;
        }
        var item = new AutomationPipelineItem(Guid.Empty, Get("AutomationPage_AddAutomaticPipeline_Placeholder", "New automatic pipeline"), null, option.DisplayName, 0, true)
        {
            TriggerKey = option.Key,
            TriggerConfigurationJson = option.DefaultConfigurationJson,
            IsExclusive = true,
        };
        var row = CreateRow(item);
        row.IsNew = true;
        _rows.Insert(0, row);
        PipelineList.Children.Insert(0, row.Card);
        EmptyText.IsVisible = false;
        MarkDirty();
#if WINDOWS
        RefreshAutomaticTriggerEditors();
#endif
        row.NameEditor.Focus();
        row.NameEditor.SelectAll();
    }

    private void DeleteRow(PipelineRow row)
    {
        _rows.Remove(row);
        PipelineList.Children.Remove(row.Card);
        EmptyText.IsVisible = _rows.Count == 0;
        MarkDirty();
#if WINDOWS
        if (!row.IsAutomatic)
            RefreshQuickActionTargetEditors();
#endif
    }

    private async Task RunPipelineAsync(PipelineRow row)
    {
        if (row.IsNew || row.Id == Guid.Empty)
            return;
        var accepted = await _platformServices.SetFeatureActionAsync("Actions", $"automation-pipeline:{row.Id:D}", true);
        SetFeedback(accepted ? null : Get("AutomationPage_Run_Error", "Unable to run this pipeline."));
    }

    private async void EnabledToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || EnabledToggle.IsChecked is not bool enabled)
            return;
        var accepted = await _platformServices.SetAutomationEnabledAsync(enabled);
        if (!accepted)
        {
            SetFeedback(Get("AutomationPage_EnableAutomaticPipelines_Error_Message", "Unable to update automation state."));
            _isRefreshing = true;
            EnabledToggle.IsChecked = !enabled;
            _isRefreshing = false;
        }
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var drafts = _rows.Select(row => new AutomationPipelineDraft(
            row.IsNew ? null : row.Id,
            row.NameEditor.Text,
            NormalizeIconName(row.IconEditor?.Text),
            row.IsAutomatic)
        {
            TriggerKey = row.TriggerEditor?.SelectedItem is AutomationTriggerOption option ? option.Key : null,
#if WINDOWS
            TriggerConfigurationJson = GetPipelineTriggerJson(row),
#else
            TriggerConfigurationJson = row.TriggerConfigEditor?.Text,
#endif
            IsExclusive = row.ExclusiveEditor.IsChecked ?? true,
            Steps = row.StepRows
#if WINDOWS
                // WPF intentionally does not offer QuickAction steps on manual actions.
                // Do not keep an older self-reference alive when the draft is saved.
                .Where(step => row.IsAutomatic || !string.Equals(GetStepTypeKey(step), "QuickAction", StringComparison.Ordinal))
#endif
                .Select(step => new AutomationStepItem(
                step.TypeEditor.SelectedItem is AutomationStepOption option ? option.TypeKey : step.Option.TypeKey,
                step.TypeEditor.SelectedItem is AutomationStepOption selected ? selected.DisplayName : step.Option.DisplayName,
#if WINDOWS
                GetStepConfigurationJson(step))).ToArray(),
#else
                step.ConfigEditor.Text ?? string.Empty)).ToArray(),
#endif
        }).ToArray();
        SaveButton.IsEnabled = false;
        try
        {
            var accepted = await _platformServices.SaveAutomationWorkspaceAsync(drafts);
            if (!accepted)
            {
                SetFeedback(Get("AutomationPage_Save_Error_Message", "Unable to save automation pipelines."));
                return;
            }
            await RefreshAsync();
            SetFeedback(
                Get("AutomationPage_Saved_Message", "Automation pipelines saved."),
                "success");
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private async void RevertButton_Click(object? sender, RoutedEventArgs e) => await RefreshAsync();

    private void MarkDirty()
    {
        _isDirty = true;
        UpdateDirtyState();
    }

    private void UpdateDirtyState() => SaveRevertPanel.IsVisible = _isDirty;

    private void SetFeedback(string? message, string variant = "error")
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            FeedbackBar.IsVisible = false;
            FeedbackMessageBlock.Text = string.Empty;
            return;
        }

        foreach (var className in new[] { "informational", "success", "warning", "error" })
            FeedbackBar.Classes.Remove(className);
        FeedbackBar.Classes.Add(variant);
        FeedbackMessageBlock.Text = message;
        ToolTip.SetTip(FeedbackBar, message);
        AutomationProperties.SetName(FeedbackBar, message);
        FeedbackBar.IsVisible = true;
    }

    private string FormatSummary(string trigger, bool automatic, int stepCount)
    {
        var kind = automatic ? trigger : Get("AutomationPage_QuickActions_Title", "Manual quick action");
        return $"{kind} | {stepCount} {Get("AutomationPipelineControl_Step", "step(s)")}";
    }

    private static string? NormalizeIconName(string? iconName) =>
        string.IsNullOrWhiteSpace(iconName) ? null : iconName.Trim();

    private string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private T GetResource<T>(object key)
    {
        if (this.TryFindResource(key, out var value) && value is T typedValue)
            return typedValue;
        if (typeof(T) == typeof(IBrush))
            return (T)(object)new SolidColorBrush(Colors.Transparent);
        if (typeof(T) == typeof(CornerRadius))
            return (T)(object)new CornerRadius(8);
        throw new InvalidOperationException($"Missing Avalonia resource '{key}'.");
    }

    private Popup CreateIconPickerPopup(TextBox iconEditor, Control anchor)
    {
        var filter = new TextBox { Watermark = Get("AutomationPage_IconFilter", "Filter icons"), MinWidth = 240 };
        var grid = new WrapPanel();
        var scroll = new ScrollViewer
        {
            MaxHeight = 300,
            Content = grid,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(filter);
        content.Children.Add(scroll);
        var panel = new Border
        {
            Padding = new Thickness(12),
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Width = 330,
            Child = content,
        };
        var popup = new Popup
        {
            PlacementTarget = anchor,
            Placement = PlacementMode.Bottom,
            IsLightDismissEnabled = true,
            Child = panel,
        };
        filter.TextChanged += (_, _) => RebuildIconGrid();
        popup.Opened += (_, _) =>
        {
            filter.Text = string.Empty;
            RebuildIconGrid();
        };
        return popup;

        void RebuildIconGrid()
        {
            grid.Children.Clear();
            var query = string.IsNullOrWhiteSpace(filter.Text)
                ? PipelineIconNames
                : PipelineIconNames.Where(name => name.Contains(filter.Text, StringComparison.OrdinalIgnoreCase));
            foreach (var name in query)
            {
                var icon = new NavigationIcon { IconIdentifier = name, FontSize = 20 };
                var button = new Button
                {
                    Content = icon,
                    MinWidth = 46,
                    MinHeight = 38,
                    Padding = new Thickness(4),
                    Margin = new Thickness(2),
                };
                AutomationProperties.SetName(button, name);
                ToolTip.SetTip(button, name);
                button.Click += (_, _) =>
                {
                    iconEditor.Text = name;
                    popup.Close();
                };
                grid.Children.Add(button);
            }
        }
    }

    internal static IReadOnlyList<AutomationStepOption> GetAvailableStepOptions(
        IReadOnlyList<AutomationStepOption> options,
        bool isAutomatic) =>
        isAutomatic
            ? options
            : options.Where(option => !string.Equals(option.TypeKey, "QuickAction", StringComparison.Ordinal)).ToArray();

    private IReadOnlyList<AutomationStepOption> GetAvailableStepOptions(bool isAutomatic) =>
        GetAvailableStepOptions(_stepOptions, isAutomatic);

#if WINDOWS
    internal const string GodModePresetTriggerKey = "god-mode-preset";

    internal static readonly string[] TypedStepTypeKeys =
    [
        "AlwaysOnUsb", "Battery", "BatteryNightCharge", "DeactivateGPU", "Delay", "DisplayBrightness",
        "DpiScale", "FlipToStart", "FnLock", "GodModePreset", "HDR", "HybridMode", "InstantBoot",
        "Macro", "Microphone", "Notification", "OneLevelWhiteKeyboardBacklight", "Osd",
        "OverclockDiscreteGPU", "OverDrive", "PanelLogoBacklight", "PlaySound", "PortsBacklight",
        "PowerMode", "QuickAction", "RefreshRate", "Resolution", "RGBKeyboardBacklight", "Run",
        "SpectrumKeyboardBacklightBrightness", "SpectrumKeyboardBacklightImportProfile",
        "SpectrumKeyboardBacklightProfile", "Speaker", "TouchpadLock", "TurnOffMonitors", "TurnOffWiFi",
        "TurnOnWiFi", "WhiteKeyboardBacklight", "WinKey", "ShowMainWindow", "HideMainWindow",
    ];

    internal static bool HasTypedStepEditor(string typeKey) =>
        TypedStepTypeKeys.Contains(typeKey, StringComparer.Ordinal);

    internal static IReadOnlyList<AutomationTriggerOption> MergeTriggerOptions(IReadOnlyList<AutomationTriggerOption> options)
    {
        if (options.Any(option => string.Equals(option.Key, GodModePresetTriggerKey, StringComparison.OrdinalIgnoreCase)))
            return options;
        var godModeOption = new AutomationTriggerOption(
            GodModePresetTriggerKey,
            AvaloniaLocalization.GetString("GodModePresetChangedAutomationPipelineTrigger_DisplayName", "God Mode preset changed"),
            AutomationSerialization.SerializeTrigger(new GodModePresetChangedAutomationPipelineTrigger(Guid.Empty)));
        return [.. options, godModeOption];
    }

    internal static string ComposeTriggerJson(IReadOnlyList<IAutomationPipelineTrigger> triggers)
    {
        if (triggers.Count == 1)
            return AutomationSerialization.SerializeTrigger(triggers[0]);
        return AutomationSerialization.SerializeTrigger(new AndAutomationPipelineTrigger(triggers.ToArray()));
    }

    internal static IReadOnlyList<AutomationTriggerOption> FilterNewPipelineTriggerOptions(
        IReadOnlyList<AutomationTriggerOption> options,
        IEnumerable<IAutomationPipelineTrigger> existingTriggers)
    {
        var existingTypes = existingTriggers.Select(trigger => trigger.GetType()).ToHashSet();
        return options.Where(option =>
        {
            var trigger = DeserializeTrigger(option.DefaultConfigurationJson ?? string.Empty);
            return trigger is not IDisallowDuplicatesAutomationPipelineTrigger || !existingTypes.Contains(trigger.GetType());
        }).ToArray();
    }

    private IReadOnlyList<AutomationTriggerOption> GetAvailableNewTriggerOptions() =>
        FilterNewPipelineTriggerOptions(
            _triggerOptions,
            _rows.Where(row => row.IsAutomatic)
                .Select(row => DeserializeTrigger(GetPipelineTriggerJson(row) ?? string.Empty))
                .OfType<IAutomationPipelineTrigger>());

    private IReadOnlyList<AutomationTriggerOption> GetAvailableTriggerOptions(PipelineRow currentRow) =>
        FilterNewPipelineTriggerOptions(
            _triggerOptions,
            _rows.Where(row => row.IsAutomatic && !ReferenceEquals(row, currentRow))
                .Select(row => DeserializeTrigger(GetPipelineTriggerJson(row) ?? string.Empty))
                .OfType<IAutomationPipelineTrigger>());

    private void RefreshAutomaticTriggerEditors()
    {
        var wasRefreshing = _isRefreshing;
        _isRefreshing = true;
        try
        {
            foreach (var row in _rows.Where(row => row.IsAutomatic))
            {
                if (row.TriggerEditor?.SelectedItem is not AutomationTriggerOption selected)
                    continue;

                var options = GetAvailableTriggerOptions(row).ToList();
                if (!options.Any(option => string.Equals(option.Key, selected.Key, StringComparison.OrdinalIgnoreCase)))
                    options.Add(selected);
                row.TriggerEditor.ItemsSource = options;
                row.TriggerEditor.SelectedItem = options.First(option =>
                    string.Equals(option.Key, selected.Key, StringComparison.OrdinalIgnoreCase));
            }
        }
        finally
        {
            _isRefreshing = wasRefreshing;
        }
    }

    private static string? GetPipelineTriggerJson(PipelineRow row)
    {
#if WINDOWS
        return row.TriggerRows.Count == 0 ? null : ComposeTriggerJson(row.TriggerRows.Select(t => t.GetTrigger()).ToArray());
#else
        return row.TriggerConfigEditor?.Text;
#endif
    }

    private static string GetStepConfigurationJson(StepRow step)
    {
#if WINDOWS
        if (step.Serialize is not null)
            return step.Serialize();
#endif
        return step.ConfigEditor.Text ?? string.Empty;
    }

    private static string GetStepTypeKey(StepRow step) =>
        step.TypeEditor.SelectedItem is AutomationStepOption option ? option.TypeKey : step.Option.TypeKey;

    private IReadOnlyList<AutomationPipelineItem> GetManualQuickActionTargets()
    {
        var manualRows = _rows.Where(row => !row.IsAutomatic).ToArray();
        if (manualRows.Length == 0)
            return FilterManualQuickActionTargets(_workspacePipelines);

        return FilterManualQuickActionTargets(manualRows.Select(row => new AutomationPipelineItem(
                row.Id,
                row.NameEditor.Text,
                row.IconEditor?.Text,
                Get("AutomationPage_QuickActions_Title", "Manual quick action"),
                row.StepRows.Count,
                false)).ToArray());
    }

    internal static IReadOnlyList<AutomationPipelineItem> FilterManualQuickActionTargets(
        IReadOnlyList<AutomationPipelineItem> pipelines) =>
        pipelines.Where(pipeline => !pipeline.IsAutomatic).ToArray();

    private void RefreshQuickActionTargetEditors()
    {
        foreach (var row in _rows.Where(row => row.IsAutomatic))
        foreach (var step in row.StepRows)
            step.RefreshQuickActionTargets?.Invoke();
    }

    private static IAutomationPipelineTrigger? DeserializeTrigger(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try { return AutomationSerialization.DeserializeTrigger(json); }
        catch { return null; }
    }

    private static IAutomationStep? DeserializeStep(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try { return AutomationSerialization.DeserializeStep(json); }
        catch { return null; }
    }

    private string? ResolveTriggerOptionKey(IAutomationPipelineTrigger trigger)
    {
        if (trigger is GodModePresetChangedAutomationPipelineTrigger)
            return GodModePresetTriggerKey;
        foreach (var option in _triggerOptions)
        {
            var def = DeserializeTrigger(option.DefaultConfigurationJson ?? string.Empty);
            if (def is not null && def.GetType() == trigger.GetType())
                return option.Key;
        }
        return null;
    }

    private void PopulateTriggerRows(PipelineRow row, string configurationJson, string automationIdPrefix)
    {
        row.TriggerRows.Clear();
        row.TriggerListPanel!.Children.Clear();
        var trigger = DeserializeTrigger(configurationJson);
        if (trigger is null && row.TriggerEditor?.SelectedItem is AutomationTriggerOption option)
            trigger = DeserializeTrigger(option.DefaultConfigurationJson ?? string.Empty);
        if (trigger is AndAutomationPipelineTrigger and)
        {
            foreach (var child in and.Triggers)
                AddTriggerRow(row, child, ResolveTriggerOptionKey(child), automationIdPrefix);
        }
        else if (trigger is not null)
        {
            AddTriggerRow(row, trigger, ResolveTriggerOptionKey(trigger), automationIdPrefix);
        }
        RefreshAddTriggerOptions(row);
        UpdateTriggerValidation(row);
    }

    private void AddTriggerRow(PipelineRow row, IAutomationPipelineTrigger trigger, string? optionKey, string automationIdPrefix)
    {
        var changed = () => OnTriggerRowChanged(row);
        var editor = BuildTriggerEditor(trigger, changed);
        var nameText = new LocalizedTextBlock
        {
            Text = trigger.DisplayName,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var removeButton = new Button
        {
            Content = new NavigationIcon { IconIdentifier = "Delete24", FontSize = 14 },
            MinWidth = 30,
            MinHeight = 28,
            Padding = new Thickness(4),
        };
        AutomationProperties.SetName(removeButton, Get("Delete", "Delete"));
        ToolTip.SetTip(removeButton, Get("Delete", "Delete"));
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
        header.Children.Add(nameText);
        Grid.SetColumn(removeButton, 1);
        header.Children.Add(removeButton);

        var validationText = new LocalizedTextBlock
        {
            Foreground = GetResource<IBrush>("StatusCriticalTextBrush"),
            IsVisible = false,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
            FontSize = 12,
        };
        var body = new StackPanel { Spacing = 5 };
        body.Children.Add(header);
        if (editor.Editor is not null)
            body.Children.Add(editor.Editor);
        else
            body.Children.Add(new LocalizedTextBlock
            {
                Text = Get("AutomationPage_Trigger_NoConfiguration", "No additional configuration needed."),
                Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                FontSize = 12,
            });
        body.Children.Add(validationText);

        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            Child = body,
        };
        AutomationProperties.SetAutomationId(card, $"{automationIdPrefix}_Trigger_{row.TriggerRows.Count}");
        var triggerRow = new TriggerRow(optionKey, editor.GetTrigger, editor.Validate, card, validationText);
        row.TriggerRows.Add(triggerRow);
        row.TriggerListPanel!.Children.Add(card);
        removeButton.Click += (_, _) =>
        {
            row.TriggerRows.Remove(triggerRow);
            row.TriggerListPanel.Children.Remove(card);
            OnTriggerRowChanged(row);
        };
    }

    private void AddTriggerFromCombo(PipelineRow row)
    {
        if (row.AddTriggerEditor?.SelectedItem is not AutomationTriggerOption option)
            return;
        var trigger = DeserializeTrigger(option.DefaultConfigurationJson ?? string.Empty);
        if (trigger is null)
            return;
        AddTriggerRow(row, trigger, option.Key, $"AutomationPipeline_{row.Id:N}");
        RefreshAddTriggerOptions(row);
        row.AddTriggerEditor.SelectedItem = null;
        OnTriggerRowChanged(row);
    }

    private void RefreshAddTriggerOptions(PipelineRow row)
    {
        if (row.AddTriggerEditor is null)
            return;
        var usedKeys = row.TriggerRows
            .Select(t => t.OptionKey)
            .Where(key => key is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        row.AddTriggerEditor.ItemsSource = _triggerOptions
            .Where(option => !usedKeys.Contains(option.Key))
            .ToArray();
    }

    private void UpdateTriggerValidation(PipelineRow row)
    {
        foreach (var triggerRow in row.TriggerRows)
        {
            var message = triggerRow.Validate?.Invoke();
            triggerRow.ValidationText.Text = message;
            triggerRow.ValidationText.IsVisible = message is not null;
        }
    }

    private void OnTriggerRowChanged(PipelineRow row)
    {
        UpdateTriggerValidation(row);
        RefreshAddTriggerOptions(row);
        if (_isRefreshing)
            return;
        var triggerText = row.TriggerRows.Count switch
        {
            0 => string.Empty,
            1 => row.TriggerRows[0].GetTrigger().DisplayName,
            _ => string.Join($" {Get("AutomationPage_Trigger_AndSeparator", "AND")} ", row.TriggerRows.Select(t => t.GetTrigger().DisplayName)),
        };
        row.SummaryText.Text = FormatSummary(triggerText, true, row.StepRows.Count);
        MarkDirty();
    }

    private static void UpdateStepValidation(StepRow row)
    {
        if (row.ValidationText is null)
            return;
        var message = row.Validate?.Invoke();
        row.ValidationText.Text = message;
        row.ValidationText.IsVisible = message is not null;
    }

    private void RefreshStepEditor(StepRow row, string configurationJson, StackPanel body, TextBox configEditor, IReadOnlyList<AutomationPipelineItem> manualPipelines)
    {
        if (body.Children.Count > 0 && body.Children[0] is not TextBox)
            body.Children.RemoveAt(0);
        row.Serialize = null;
        row.Validate = null;
        row.RefreshQuickActionTargets = null;
        configEditor.IsVisible = true;
        configEditor.Text = configurationJson;
        var step = DeserializeStep(configurationJson);
        if (step is null)
            return;
        var editor = BuildStepEditor(step, manualPipelines, () =>
        {
            if (!_isRefreshing)
            {
                UpdateStepValidation(row);
                MarkDirty();
            }
        });
        if (editor.Editor is null)
            return;
        row.Serialize = editor.Serialize;
        row.Validate = editor.Validate;
        row.RefreshQuickActionTargets = editor.Refresh;
        configEditor.IsVisible = false;
        body.Children.Insert(0, editor.Editor);
        UpdateStepValidation(row);
    }

    private TypedStepEditor BuildStepEditor(IAutomationStep step, IReadOnlyList<AutomationPipelineItem> manualPipelines, Action changed) => step switch
    {
        AlwaysOnUsbAutomationStep s => BuildStateStepEditor(s, null, changed),
        BatteryAutomationStep s => BuildStateStepEditor(s, null, changed),
        BatteryNightChargeAutomationStep s => BuildStateStepEditor(s, null, changed),
        DeactivateGPUAutomationStep s => BuildStateStepEditor(s, null, changed),
        DelayAutomationStep s => BuildStateStepEditor(s, null, changed),
        DpiScaleAutomationStep s => BuildStateStepEditor(s, null, changed),
        FlipToStartAutomationStep s => BuildStateStepEditor(s, null, changed),
        FnLockAutomationStep s => BuildStateStepEditor(s, null, changed),
        HDRAutomationStep s => BuildStateStepEditor(s, null, changed),
        HybridModeAutomationStep s => BuildStateStepEditor(s, null, changed),
        InstantBootAutomationStep s => BuildStateStepEditor(s, null, changed),
        MacroAutomationStep s => BuildStateStepEditor(s, null, changed),
        MicrophoneAutomationStep s => BuildStateStepEditor(s, null, changed),
        OneLevelWhiteKeyboardBacklightAutomationStep s => BuildStateStepEditor(s, null, changed),
        OsdAutomationStep s => BuildStateStepEditor(s, null, changed),
        OverclockDiscreteGPUAutomationStep s => BuildStateStepEditor(s, null, changed),
        OverDriveAutomationStep s => BuildStateStepEditor(s, null, changed),
        PanelLogoBacklightAutomationStep s => BuildStateStepEditor(s, null, changed),
        PortsBacklightAutomationStep s => BuildStateStepEditor(s, null, changed),
        PowerModeAutomationStep s => BuildStateStepEditor(s, null, changed),
        RefreshRateAutomationStep s => BuildStateStepEditor(s, null, changed),
        ResolutionAutomationStep s => BuildStateStepEditor(s, null, changed),
        RGBKeyboardBacklightAutomationStep s => BuildStateStepEditor(s, null, changed),
        SpeakerAutomationStep s => BuildStateStepEditor(s, null, changed),
        SpectrumKeyboardBacklightBrightnessAutomationStep s => BuildStateStepEditor(s,
            value => value == 0 ? Get("SpectrumKeyboardBacklightBrightnessAutomationStepControl_Off", "Off") : value.ToString(), changed),
        SpectrumKeyboardBacklightProfileAutomationStep s => BuildStateStepEditor(s, null, changed),
        TouchpadLockAutomationStep s => BuildStateStepEditor(s, null, changed),
        WhiteKeyboardBacklightAutomationStep s => BuildStateStepEditor(s, null, changed),
        WinKeyAutomationStep s => BuildStateStepEditor(s, null, changed),
        RunAutomationStep s => BuildRunStepEditor(s, changed),
        NotificationAutomationStep s => BuildNotificationStepEditor(s, changed),
        DisplayBrightnessAutomationStep s => BuildBrightnessStepEditor(s, changed),
        PlaySoundAutomationStep s => BuildPlaySoundStepEditor(s, changed),
        SpectrumKeyboardBacklightImportProfileAutomationStep s => BuildImportProfileStepEditor(s, changed),
        GodModePresetAutomationStep s => BuildGodModePresetStepEditor(s, changed),
        QuickActionAutomationStep s => BuildQuickActionStepEditor(s, manualPipelines, changed),
        _ => new TypedStepEditor { Serialize = () => AutomationSerialization.SerializeStep(step) },
    };

    private TypedStepEditor BuildStateStepEditor<T>(IAutomationStep<T> step, Func<T, string>? displayName, Action changed) where T : struct
    {
        var combo = new StateComboBox<T>(step, displayName, changed);
        return new TypedStepEditor { Editor = combo.Editor, Serialize = combo.Serialize, Validate = combo.Validate };
    }

    private TypedStepEditor BuildRunStepEditor(RunAutomationStep step, Action changed)
    {
        var path = new TextBox
        {
            Text = step.ScriptPath ?? string.Empty,
            Watermark = Get("RunAutomationStepControl_ExePath", "Executable or script path"),
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var arguments = new TextBox
        {
            Text = step.ScriptArguments ?? string.Empty,
            Watermark = Get("RunAutomationStepControl_ExeArguments", "Arguments"),
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var runSilently = new CheckBox
        {
            Content = Get("RunAutomationStepControl_ProcessRunSilently", "Run silently"),
            IsChecked = step.RunSilently,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        var waitUntilFinished = new CheckBox
        {
            Content = Get("RunAutomationStepControl_ProcessWaitUntilFinished", "Wait until finished"),
            IsChecked = step.WaitUntilFinished,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        var toggles = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        toggles.Children.Add(runSilently);
        toggles.Children.Add(waitUntilFinished);
        var panel = new StackPanel { Spacing = 5 };
        panel.Children.Add(CreateBrowseRow(path, Get("AutomationPage_OpenExecutable", "Select an executable"), "Executable", "*.exe;*.bat;*.cmd;*.ps1"));
        panel.Children.Add(arguments);
        panel.Children.Add(toggles);
        path.TextChanged += (_, _) => changed();
        arguments.TextChanged += (_, _) => changed();
        runSilently.IsCheckedChanged += (_, _) => changed();
        waitUntilFinished.IsCheckedChanged += (_, _) => changed();
        return new TypedStepEditor
        {
            Editor = panel,
            Serialize = () => AutomationSerialization.SerializeStep(new RunAutomationStep(
                path.Text,
                arguments.Text,
                runSilently.IsChecked ?? true,
                waitUntilFinished.IsChecked ?? true)),
            Validate = () => string.IsNullOrWhiteSpace(path.Text)
                ? Get("AutomationPage_StepRun_Validation", "Set the executable or script path.")
                : null,
        };
    }

    private TypedStepEditor BuildNotificationStepEditor(NotificationAutomationStep step, Action changed)
    {
        var text = new TextBox
        {
            Text = step.Text ?? string.Empty,
            Watermark = Get("NotificationAutomationStepControl_NotificationText", "Notification text"),
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        text.TextChanged += (_, _) => changed();
        return new TypedStepEditor
        {
            Editor = text,
            Serialize = () => AutomationSerialization.SerializeStep(new NotificationAutomationStep(text.Text)),
            Validate = () => string.IsNullOrWhiteSpace(text.Text)
                ? Get("AutomationPage_StepNotification_Validation", "Enter notification text.")
                : null,
        };
    }

    private TypedStepEditor BuildBrightnessStepEditor(DisplayBrightnessAutomationStep step, Action changed)
    {
        var number = CreateNumberBox(step.Brightness, 0, 100, 5, "0");
        number.ValueChanged += (_, _) => changed();
        return new TypedStepEditor
        {
            Editor = number,
            Serialize = () => AutomationSerialization.SerializeStep(new DisplayBrightnessAutomationStep((int)(number.Value ?? 0))),
            Validate = () => number.Value is null
                ? Get("AutomationPage_StepBrightness_Validation", "Brightness must be between 0 and 100.")
                : null,
        };
    }

    private TypedStepEditor BuildPlaySoundStepEditor(PlaySoundAutomationStep step, Action changed)
    {
        var path = new TextBox
        {
            Text = step.Path ?? string.Empty,
            Watermark = Get("PlaySoundAutomationStepControl_Path", "Sound file path"),
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var panel = new StackPanel { Spacing = 5 };
        panel.Children.Add(CreateBrowseRow(path, Get("AutomationPage_SelectSound", "Select a sound file"), "Audio", "*.wav"));
        path.TextChanged += (_, _) => changed();
        return new TypedStepEditor
        {
            Editor = panel,
            Serialize = () => AutomationSerialization.SerializeStep(new PlaySoundAutomationStep(
                string.IsNullOrWhiteSpace(path.Text) ? null : path.Text)),
            Validate = () => string.IsNullOrWhiteSpace(path.Text)
                ? Get("AutomationPage_Validation_Path", "Select a file.")
                : null,
        };
    }

    private TypedStepEditor BuildImportProfileStepEditor(SpectrumKeyboardBacklightImportProfileAutomationStep step, Action changed)
    {
        var path = new TextBox
        {
            Text = step.Path ?? string.Empty,
            Watermark = Get("SpectrumKeyboardBacklightImportProfileAutomationStepControl_Path", "Profile JSON file"),
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var panel = new StackPanel { Spacing = 5 };
        panel.Children.Add(CreateBrowseRow(path, Get("AutomationPage_ImportProfile", "Select a profile JSON file"), "JSON", "*.json"));
        path.TextChanged += (_, _) => changed();
        return new TypedStepEditor
        {
            Editor = panel,
            Serialize = () => AutomationSerialization.SerializeStep(new SpectrumKeyboardBacklightImportProfileAutomationStep(path.Text)),
            Validate = () => string.IsNullOrWhiteSpace(path.Text)
                ? Get("AutomationPage_Validation_Path", "Select a file.")
                : null,
        };
    }

    private TypedStepEditor BuildGodModePresetStepEditor(GodModePresetAutomationStep step, Action changed)
    {
        var combo = new AccessibleComboBox { MinWidth = 200, HorizontalAlignment = HorizontalAlignment.Stretch };
        var current = step;
        var presets = new List<GodModePresetState>();
        var applying = false;
        combo.Loaded += async (_, _) =>
        {
            applying = true;
            try
            {
                presets.Clear();
                presets.AddRange(await LoadGodModePresetsAsync());
                var items = presets.Select(preset => new DisplayOption<Guid>(preset.Id, preset.Name)).ToList();
                combo.ItemsSource = items;
                combo.SelectedItem = items.FirstOrDefault(item => item.Value == step.PresetId) ?? items.FirstOrDefault();
                if (combo.SelectedItem is DisplayOption<Guid> selected)
                    current = new GodModePresetAutomationStep(selected.Value);
            }
            finally
            {
                applying = false;
            }
        };
        combo.SelectionChanged += (_, _) =>
        {
            if (applying)
                return;
            if (combo.SelectedItem is DisplayOption<Guid> selected)
            {
                current = new GodModePresetAutomationStep(selected.Value);
                changed();
            }
        };
        return new TypedStepEditor
        {
            Editor = combo,
            Serialize = () => AutomationSerialization.SerializeStep(current),
            Validate = () => presets.Count == 0
                ? Get("AutomationPage_Validation_Preset", "Select a preset.")
                : null,
        };
    }

    private TypedStepEditor BuildQuickActionStepEditor(QuickActionAutomationStep step, IReadOnlyList<AutomationPipelineItem> manualPipelines, Action changed)
    {
        var combo = new AccessibleComboBox { MinWidth = 200, HorizontalAlignment = HorizontalAlignment.Stretch };
        var current = step;
        void RefreshTargets()
        {
            var targets = GetManualQuickActionTargets();
            var items = targets
                .Select(pipeline => new DisplayOption<Guid>(pipeline.Id, string.IsNullOrWhiteSpace(pipeline.Name)
                    ? Get("AutomationPage_QuickActions_Title", "Manual quick action")
                    : pipeline.Name))
                .ToList();
            combo.ItemsSource = items;
            combo.SelectedItem = items.FirstOrDefault(item => item.Value == current.PipelineId);
        }
        RefreshTargets();
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is DisplayOption<Guid> selected)
            {
                current = new QuickActionAutomationStep(selected.Value);
                changed();
            }
        };
        return new TypedStepEditor
        {
            Editor = combo,
            Serialize = () => AutomationSerialization.SerializeStep(current),
            Refresh = RefreshTargets,
        };
    }

    private Grid CreateBrowseRow(TextBox textBox, string dialogTitle, string fileTypeName, string patterns)
    {
        var browseButton = new Button { Content = Get("Browse", "Browse"), MinWidth = 80 };
        AutomationProperties.SetName(browseButton, Get("Browse", "Browse"));
        browseButton.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = dialogTitle,
                FileTypeFilter = [new FilePickerFileType(fileTypeName) { Patterns = [patterns] }],
            });
            var path = files.FirstOrDefault()?.Path.LocalPath;
            if (string.IsNullOrWhiteSpace(path))
                return;
            textBox.Text = path;
        };
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
        row.Children.Add(textBox);
        Grid.SetColumn(browseButton, 1);
        row.Children.Add(browseButton);
        return row;
    }

    private TypedTriggerEditor BuildTriggerEditor(IAutomationPipelineTrigger trigger, Action changed) => trigger switch
    {
        BatteryPercentageAutomationPipelineTrigger t => BuildBatteryTrigger(t, changed),
        DeviceConnectedAutomationPipelineTrigger t => BuildDeviceTrigger(t, changed),
        DeviceDisconnectedAutomationPipelineTrigger t => BuildDeviceTrigger(t, changed),
        GodModePresetChangedAutomationPipelineTrigger t => BuildGodModePresetTrigger(t, changed),
        HardwareSensorAutomationPipelineTrigger t => BuildHardwareSensorTrigger(t, changed),
        PeriodicAutomationPipelineTrigger t => BuildPeriodicTrigger(t, changed),
        PowerModeAutomationPipelineTrigger t => BuildPowerModeTrigger(t, changed),
        ProcessesAreRunningAutomationPipelineTrigger t => BuildProcessTrigger(t, changed),
        ProcessesStopRunningAutomationPipelineTrigger t => BuildProcessTrigger(t, changed),
        TimeAutomationPipelineTrigger t => BuildTimeTrigger(t, changed),
        UserInactivityAutomationPipelineTrigger t => BuildUserInactivityTrigger(t, changed),
        WiFiConnectedAutomationPipelineTrigger t => BuildWiFiTrigger(t, changed),
        _ => new TypedTriggerEditor { GetTrigger = () => trigger },
    };

    private TypedTriggerEditor BuildBatteryTrigger(BatteryPercentageAutomationPipelineTrigger trigger, Action changed)
    {
        var comparison = CreateDisplayCombo(Enum.GetValues<BatteryPercentageComparison>(), trigger.Comparison, value => value.ToString());
        var threshold = CreateNumberBox(trigger.Threshold, 0, 100, 5, "0");
        var chargeFilter = CreateDisplayCombo(Enum.GetValues<BatteryChargeFilter>(), trigger.ChargeFilter, value => value.ToString());
        var duration = CreateNumberBox((decimal)trigger.Duration.TotalSeconds, 0, 86400, 5, "0");
        var cooldown = CreateNumberBox((decimal)trigger.Cooldown.TotalSeconds, 0, 86400, 5, "0");
        var current = trigger;
        void Rebuild()
        {
            current = new BatteryPercentageAutomationPipelineTrigger(
                TryGetDisplayValue(comparison, out BatteryPercentageComparison c) ? c : trigger.Comparison,
                (int)(threshold.Value ?? trigger.Threshold),
                TimeSpan.FromSeconds((double)(duration.Value ?? (decimal)trigger.Duration.TotalSeconds)),
                TimeSpan.FromSeconds((double)(cooldown.Value ?? (decimal)trigger.Cooldown.TotalSeconds)),
                TryGetDisplayValue(chargeFilter, out BatteryChargeFilter f) ? f : trigger.ChargeFilter);
        }
        WireTriggerFields(changed, Rebuild, comparison, threshold, chargeFilter, duration, cooldown);
        return new TypedTriggerEditor
        {
            Editor = CreateTriggerFieldGrid(
                CreateFieldRow(Get("AutomationPage_Trigger_Comparison", "Comparison"), comparison),
                CreateFieldRow(Get("AutomationPage_Trigger_Threshold", "Threshold (%)"), threshold),
                CreateFieldRow(Get("AutomationPage_Trigger_ChargeFilter", "Charge filter"), chargeFilter),
                CreateFieldRow(Get("AutomationPage_Trigger_DurationSeconds", "Duration (seconds)"), duration),
                CreateFieldRow(Get("AutomationPage_Trigger_CooldownSeconds", "Cooldown (seconds)"), cooldown)),
            GetTrigger = () => current,
            Validate = () => threshold.Value is null
                ? Get("AutomationPage_Validation_Threshold", "Enter a threshold value.")
                : null,
        };
    }

    private TypedTriggerEditor BuildHardwareSensorTrigger(HardwareSensorAutomationPipelineTrigger trigger, Action changed)
    {
        var metric = CreateDisplayCombo(Enum.GetValues<HardwareSensorMetric>(), trigger.Metric, value => value.ToString());
        var comparison = CreateDisplayCombo(Enum.GetValues<HardwareSensorComparison>(), trigger.Comparison, value => value.ToString());
        var threshold = CreateNumberBox((decimal)trigger.Threshold, 0, 999, 5, "0.#");
        var duration = CreateNumberBox((decimal)trigger.Duration.TotalSeconds, 0, 86400, 5, "0");
        var cooldown = CreateNumberBox((decimal)trigger.Cooldown.TotalSeconds, 0, 86400, 5, "0");
        var current = trigger;
        void Rebuild()
        {
            current = new HardwareSensorAutomationPipelineTrigger(
                TryGetDisplayValue(metric, out HardwareSensorMetric m) ? m : trigger.Metric,
                TryGetDisplayValue(comparison, out HardwareSensorComparison c) ? c : trigger.Comparison,
                (float)(threshold.Value ?? (decimal)trigger.Threshold),
                TimeSpan.FromSeconds((double)(duration.Value ?? (decimal)trigger.Duration.TotalSeconds)),
                TimeSpan.FromSeconds((double)(cooldown.Value ?? (decimal)trigger.Cooldown.TotalSeconds)));
        }
        WireTriggerFields(changed, Rebuild, metric, comparison, threshold, duration, cooldown);
        return new TypedTriggerEditor
        {
            Editor = CreateTriggerFieldGrid(
                CreateFieldRow(Get("AutomationPage_Trigger_Metric", "Sensor metric"), metric),
                CreateFieldRow(Get("AutomationPage_Trigger_Comparison", "Comparison"), comparison),
                CreateFieldRow(Get("AutomationPage_Trigger_Threshold", "Threshold"), threshold),
                CreateFieldRow(Get("AutomationPage_Trigger_DurationSeconds", "Duration (seconds)"), duration),
                CreateFieldRow(Get("AutomationPage_Trigger_CooldownSeconds", "Cooldown (seconds)"), cooldown)),
            GetTrigger = () => current,
            Validate = () => threshold.Value is null
                ? Get("AutomationPage_Validation_Threshold", "Enter a threshold value.")
                : null,
        };
    }

    private TypedTriggerEditor BuildPeriodicTrigger(PeriodicAutomationPipelineTrigger trigger, Action changed)
    {
        var minutes = CreateNumberBox((decimal)trigger.Period.TotalMinutes, 1, 10080, 5, "0");
        var current = trigger;
        minutes.ValueChanged += (_, _) =>
        {
            current = new PeriodicAutomationPipelineTrigger(TimeSpan.FromMinutes((double)(minutes.Value ?? 1)));
            changed();
        };
        return new TypedTriggerEditor
        {
            Editor = CreateFieldRow(Get("AutomationPage_Trigger_PeriodMinutes", "Period (minutes)"), minutes),
            GetTrigger = () => current,
            Validate = () => (minutes.Value ?? 0) < 1
                ? Get("AutomationPage_Validation_Period", "Period must be at least 1 minute.")
                : null,
        };
    }

    private TypedTriggerEditor BuildPowerModeTrigger(PowerModeAutomationPipelineTrigger trigger, Action changed)
    {
        var combo = CreateDisplayCombo(Enum.GetValues<PowerModeState>(), trigger.PowerModeState, value => value.GetDisplayName());
        var current = trigger;
        combo.SelectionChanged += (_, _) =>
        {
            if (TryGetDisplayValue(combo, out PowerModeState state))
            {
                current = new PowerModeAutomationPipelineTrigger(state);
                changed();
            }
        };
        return new TypedTriggerEditor
        {
            Editor = CreateFieldRow(Get("AutomationPage_Trigger_PowerMode", "Power mode"), combo),
            GetTrigger = () => current,
        };
    }

    private TypedTriggerEditor BuildUserInactivityTrigger(UserInactivityAutomationPipelineTrigger trigger, Action changed)
    {
        var combo = CreateDisplayCombo(InactivityTimeSpans, trigger.InactivityTimeSpan, HumanizeTimeSpan);
        var current = trigger;
        combo.SelectionChanged += (_, _) =>
        {
            if (TryGetDisplayValue(combo, out TimeSpan span))
            {
                current = new UserInactivityAutomationPipelineTrigger(span);
                changed();
            }
        };
        return new TypedTriggerEditor
        {
            Editor = CreateFieldRow(Get("AutomationPage_Trigger_Inactivity", "Inactivity timeout"), combo),
            GetTrigger = () => current,
        };
    }

    private TypedTriggerEditor BuildTimeTrigger(TimeAutomationPipelineTrigger trigger, Action changed)
    {
        var sunrise = new RadioButton { Content = Get("AutomationPage_Trigger_AtSunrise", "At sunrise"), IsChecked = trigger.IsSunrise };
        var sunset = new RadioButton { Content = Get("AutomationPage_Trigger_AtSunset", "At sunset"), IsChecked = trigger.IsSunset };
        var atTime = new RadioButton { Content = Get("AutomationPage_Trigger_AtSpecificTime", "At specific time"), IsChecked = trigger.Time is not null };
        var hours = CreateNumberBox(0, 0, 23, 1, "0");
        var minutes = CreateNumberBox(0, 0, 59, 1, "0");
        if (trigger.Time is Time time)
        {
            var local = DateTimeExtensions.UtcFrom(time.Hour, time.Minute).ToLocalTime();
            hours.Value = local.Hour;
            minutes.Value = local.Minute;
        }
        var dayCheckBoxes = new List<CheckBox>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var checkBox = new CheckBox
            {
                Content = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(day),
                Tag = day,
                IsChecked = trigger.Days.Length == 0 || trigger.Days.Contains(day),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            dayCheckBoxes.Add(checkBox);
        }
        var daysPanel = new WrapPanel();
        foreach (var checkBox in dayCheckBoxes)
        {
            checkBox.Margin = new Thickness(0, 0, 8, 4);
            daysPanel.Children.Add(checkBox);
        }
        var timePanel = new StackPanel { Spacing = 4 };
        timePanel.Children.Add(sunrise);
        timePanel.Children.Add(sunset);
        timePanel.Children.Add(CreateFieldRow(Get("AutomationPage_Trigger_AtSpecificTime", "At specific time"), atTime));
        timePanel.Children.Add(CreateFieldRow(Get("AutomationPage_Trigger_Time", "Time"), CreateTimeRow(hours, minutes)));
        timePanel.Children.Add(CreateFieldRow(Get("AutomationPage_Trigger_Days", "Days"), daysPanel));
        var current = trigger;
        void Rebuild()
        {
            Time? picked = null;
            if (atTime.IsChecked ?? false)
            {
                var local = DateTimeExtensions.LocalFrom((int)(hours.Value ?? 0), (int)(minutes.Value ?? 0)).ToUniversalTime();
                picked = new Time(local.Hour, local.Minute);
            }
            var days = dayCheckBoxes.Where(checkBox => checkBox.IsChecked == true).Select(checkBox => (DayOfWeek)checkBox.Tag!).ToArray();
            if (days.Length == 0)
                days = Enum.GetValues<DayOfWeek>();
            current = new TimeAutomationPipelineTrigger(sunrise.IsChecked ?? false, sunset.IsChecked ?? false, picked, days);
        }
        sunrise.IsCheckedChanged += (_, _) =>
        {
            if (sunrise.IsChecked == true)
                atTime.IsChecked = false;
            Rebuild();
            changed();
        };
        sunset.IsCheckedChanged += (_, _) =>
        {
            if (sunset.IsChecked == true)
                atTime.IsChecked = false;
            Rebuild();
            changed();
        };
        atTime.IsCheckedChanged += (_, _) =>
        {
            if (atTime.IsChecked == true)
            {
                sunrise.IsChecked = false;
                sunset.IsChecked = false;
            }
            Rebuild();
            changed();
        };
        hours.ValueChanged += (_, _) => { Rebuild(); changed(); };
        minutes.ValueChanged += (_, _) => { Rebuild(); changed(); };
        foreach (var checkBox in dayCheckBoxes)
            checkBox.IsCheckedChanged += (_, _) => { Rebuild(); changed(); };
        return new TypedTriggerEditor
        {
            Editor = timePanel,
            GetTrigger = () => current,
            Validate = () => (sunrise.IsChecked ?? false) || (sunset.IsChecked ?? false) || (atTime.IsChecked ?? false)
                ? null
                : Get("AutomationPage_Validation_Time", "Select at least one time option."),
        };
    }

    private TypedTriggerEditor BuildWiFiTrigger(WiFiConnectedAutomationPipelineTrigger trigger, Action changed)
    {
        var ssids = new List<string>([.. trigger.Ssids]);
        var current = trigger;
        var panel = new StackPanel { Spacing = 4 };
        void Rebuild() => current = new WiFiConnectedAutomationPipelineTrigger(ssids.Distinct().Where(ssid => !string.IsNullOrWhiteSpace(ssid)).ToArray());
        void RefreshList()
        {
            panel.Children.Clear();
            for (var index = 0; index < ssids.Count; index++)
            {
                var rowIndex = index;
                var box = new TextBox
                {
                    Text = ssids[rowIndex],
                    Watermark = Get("AutomationPage_Trigger_Ssid", "Network name (SSID)"),
                    MinWidth = 180,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                var removeButton = new Button
                {
                    Content = new NavigationIcon { IconIdentifier = "Delete24", FontSize = 14 },
                    MinWidth = 30,
                    MinHeight = 28,
                    Padding = new Thickness(4),
                };
                AutomationProperties.SetName(removeButton, Get("Delete", "Delete"));
                box.TextChanged += (_, _) =>
                {
                    ssids[rowIndex] = box.Text;
                    Rebuild();
                    changed();
                };
                removeButton.Click += (_, _) =>
                {
                    ssids.RemoveAt(rowIndex);
                    Rebuild();
                    RefreshList();
                    changed();
                };
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
                row.Children.Add(box);
                Grid.SetColumn(removeButton, 1);
                row.Children.Add(removeButton);
                panel.Children.Add(row);
            }
            if (ssids.Count == 0)
            {
                panel.Children.Add(new LocalizedTextBlock
                {
                    Text = Get("AutomationPage_Trigger_EmptySsidsHint", "Empty list matches any connected network."),
                    Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                    FontSize = 12,
                });
            }
        }
        RefreshList();
        var addButton = new Button { Content = Get("AutomationPage_Trigger_AddSsid", "Add network name"), MinWidth = 120 };
        AutomationProperties.SetName(addButton, Get("AutomationPage_Trigger_AddSsid", "Add network name"));
        addButton.Click += (_, _) =>
        {
            ssids.Add(string.Empty);
            RefreshList();
        };
        var editor = new StackPanel { Spacing = 5 };
        editor.Children.Add(panel);
        editor.Children.Add(addButton);
        return new TypedTriggerEditor
        {
            Editor = editor,
            GetTrigger = () => current,
        };
    }

    private TypedTriggerEditor BuildProcessTrigger(IProcessesAutomationPipelineTrigger trigger, Action changed)
    {
        var processes = new List<ProcessInfo>([.. trigger.Processes]);
        var current = trigger;
        var panel = new StackPanel { Spacing = 4 };
        void Rebuild() => current = trigger.DeepCopy(processes.ToArray());
        void RefreshList()
        {
            panel.Children.Clear();
            foreach (var process in processes.OrderBy(process => process))
            {
                var name = new LocalizedTextBlock
                {
                    Text = process.Name,
                    OverflowMode = LocalizedOverflowMode.Ellipsis,
                    MaxLines = 1,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                var path = new LocalizedTextBlock
                {
                    Text = process.ExecutablePath ?? string.Empty,
                    Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                    OverflowMode = LocalizedOverflowMode.Ellipsis,
                    MaxLines = 1,
                    FontSize = 11,
                };
                var removeButton = new Button
                {
                    Content = new NavigationIcon { IconIdentifier = "Delete24", FontSize = 14 },
                    MinWidth = 30,
                    MinHeight = 28,
                    Padding = new Thickness(4),
                };
                AutomationProperties.SetName(removeButton, Get("Delete", "Delete"));
                removeButton.Click += (_, _) =>
                {
                    processes.Remove(process);
                    Rebuild();
                    RefreshList();
                    changed();
                };
                var text = new StackPanel { Spacing = 1 };
                text.Children.Add(name);
                text.Children.Add(path);
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
                row.Children.Add(text);
                Grid.SetColumn(removeButton, 1);
                row.Children.Add(removeButton);
                panel.Children.Add(row);
            }
        }
        RefreshList();
        var addButton = new Button { Content = Get("AutomationPage_Trigger_AddProcess", "Add process"), MinWidth = 120 };
        AutomationProperties.SetName(addButton, Get("AutomationPage_Trigger_AddProcess", "Add process"));
        addButton.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = Get("Open", "Open"),
                FileTypeFilter = [new FilePickerFileType("Executable") { Patterns = ["*.exe"] }],
            });
            var path = files.FirstOrDefault()?.Path.LocalPath;
            if (string.IsNullOrWhiteSpace(path))
                return;
            var processInfo = ProcessInfo.FromPath(path);
            if (processes.Contains(processInfo))
                return;
            processes.Add(processInfo);
            Rebuild();
            RefreshList();
            changed();
        };
        var editor = new StackPanel { Spacing = 5 };
        editor.Children.Add(panel);
        editor.Children.Add(addButton);
        return new TypedTriggerEditor
        {
            Editor = editor,
            GetTrigger = () => current,
            Validate = () => processes.Count == 0
                ? Get("AutomationPage_Validation_Process", "Add at least one process.")
                : null,
        };
    }

    private TypedTriggerEditor BuildDeviceTrigger(IDeviceAutomationPipelineTrigger trigger, Action changed)
    {
        var instanceIds = new HashSet<string>([.. trigger.InstanceIds], StringComparer.OrdinalIgnoreCase);
        var filter = new TextBox { Watermark = Get("AutomationPage_Trigger_DeviceFilter", "Filter devices"), MinWidth = 180 };
        var list = new StackPanel { Spacing = 2 };
        var scroll = new ScrollViewer
        {
            MaxHeight = 220,
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var devices = new List<Device>();
        var current = trigger;
        void Rebuild() => current = trigger.DeepCopy([.. instanceIds]);
        void Reload()
        {
            list.Children.Clear();
            IEnumerable<Device> query = devices.OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(filter.Text))
                query = query.Where(device => device.Index.Contains(filter.Text, StringComparison.OrdinalIgnoreCase));
            var matches = query.Take(60).ToArray();
            foreach (var device in matches)
            {
                var checkBox = new CheckBox
                {
                    Content = device.Name,
                    Tag = device,
                    IsChecked = instanceIds.Contains(device.DeviceInstanceId),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MinWidth = 0,
                };
                var instanceId = new LocalizedTextBlock
                {
                    Text = device.DeviceInstanceId,
                    Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                    OverflowMode = LocalizedOverflowMode.Ellipsis,
                    MaxLines = 1,
                    FontSize = 11,
                };
                var item = new StackPanel { Spacing = 1 };
                item.Children.Add(checkBox);
                item.Children.Add(instanceId);
                checkBox.IsCheckedChanged += (_, _) =>
                {
                    if (checkBox.IsChecked == true)
                        instanceIds.Add(device.DeviceInstanceId);
                    else
                        instanceIds.Remove(device.DeviceInstanceId);
                    Rebuild();
                    changed();
                };
                list.Children.Add(item);
            }
            if (matches.Length == 0)
            {
                list.Children.Add(new LocalizedTextBlock
                {
                    Text = devices.Count == 0
                        ? Get("AutomationPage_Trigger_NoDevices", "No devices detected.")
                        : Get("AutomationPage_Trigger_NoMatchingDevices", "No matching devices."),
                    Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                    FontSize = 12,
                });
            }
        }
        var loaded = false;
        scroll.Loaded += async (_, _) =>
        {
            if (loaded)
                return;
            loaded = true;
            try { devices.AddRange(await Task.Run(Devices.GetAll).ConfigureAwait(true)); }
            catch { }
            Reload();
        };
        filter.TextChanged += (_, _) => Reload();
        var editor = new StackPanel { Spacing = 5 };
        editor.Children.Add(filter);
        editor.Children.Add(scroll);
        return new TypedTriggerEditor
        {
            Editor = editor,
            GetTrigger = () => current,
            Validate = () => instanceIds.Count == 0
                ? Get("AutomationPage_Validation_Device", "Select at least one device.")
                : null,
        };
    }

    private TypedTriggerEditor BuildGodModePresetTrigger(GodModePresetChangedAutomationPipelineTrigger trigger, Action changed)
    {
        var combo = new AccessibleComboBox { MinWidth = 200, HorizontalAlignment = HorizontalAlignment.Stretch };
        var current = trigger;
        var presets = new List<GodModePresetState>();
        var applying = false;
        combo.Loaded += async (_, _) =>
        {
            applying = true;
            try
            {
                presets.Clear();
                presets.AddRange(await LoadGodModePresetsAsync());
                var items = presets.Select(preset => new DisplayOption<Guid>(preset.Id, preset.Name)).ToList();
                combo.ItemsSource = items;
                combo.SelectedItem = items.FirstOrDefault(item => item.Value == trigger.PresetId) ?? items.FirstOrDefault();
                if (combo.SelectedItem is DisplayOption<Guid> selected)
                    current = new GodModePresetChangedAutomationPipelineTrigger(selected.Value);
            }
            finally
            {
                applying = false;
            }
        };
        combo.SelectionChanged += (_, _) =>
        {
            if (applying)
                return;
            if (combo.SelectedItem is DisplayOption<Guid> selected)
            {
                current = new GodModePresetChangedAutomationPipelineTrigger(selected.Value);
                changed();
            }
        };
        return new TypedTriggerEditor
        {
            Editor = CreateFieldRow(Get("AutomationPage_Trigger_Preset", "Preset"), combo),
            GetTrigger = () => current,
            Validate = () => presets.Count == 0
                ? Get("AutomationPage_Validation_Preset", "Select a preset.")
                : null,
        };
    }

    private async Task<IReadOnlyList<GodModePresetState>> LoadGodModePresetsAsync()
    {
        try
        {
            var state = await _platformServices.GetGodModeSettingsAsync().ConfigureAwait(true);
            return state.Presets;
        }
        catch
        {
            return Array.Empty<GodModePresetState>();
        }
    }

    private static void WireTriggerFields(Action changed, Action rebuild, params Control[] fields)
    {
        foreach (var field in fields)
        {
            switch (field)
            {
                case ComboBox combo:
                    combo.SelectionChanged += (_, _) => { rebuild(); changed(); };
                    break;
                case NumericUpDown number:
                    number.ValueChanged += (_, _) => { rebuild(); changed(); };
                    break;
            }
        }
    }

    private static Grid CreateTriggerFieldGrid(params Control[] rows)
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions(string.Join(",", Enumerable.Repeat("Auto", rows.Length))), RowSpacing = 5 };
        for (var index = 0; index < rows.Length; index++)
        {
            Grid.SetRow(rows[index], index);
            grid.Children.Add(rows[index]);
        }
        return grid;
    }

    private static Control CreateTimeRow(NumericUpDown hours, NumericUpDown minutes)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        row.Children.Add(hours);
        row.Children.Add(minutes);
        return row;
    }

    private static Control CreateFieldRow(string label, Control field)
    {
        var labelBlock = new LocalizedTextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 140,
            FontSize = 12,
        };
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 8 };
        row.Children.Add(labelBlock);
        Grid.SetColumn(field, 1);
        row.Children.Add(field);
        return row;
    }

    private static ComboBox CreateDisplayCombo<T>(IEnumerable<T> values, T? selected, Func<T, string> display) where T : struct
    {
        var items = values.Select(value => new DisplayOption<T>(value, display(value))).ToList();
        var combo = new ComboBox
        {
            ItemsSource = items,
            MinWidth = 160,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        combo.SelectedItem = items.FirstOrDefault(item => item.Value.Equals(selected));
        return combo;
    }

    private static bool TryGetDisplayValue<T>(ComboBox combo, out T value) where T : struct
    {
        if (combo.SelectedItem is DisplayOption<T> selected)
        {
            value = selected.Value;
            return true;
        }
        value = default;
        return false;
    }

    private static NumericUpDown CreateNumberBox(decimal? value, decimal minimum, decimal maximum, decimal increment, string format = "0.##")
    {
        return new NumericUpDown
        {
            Value = value,
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            FormatString = format,
            MinWidth = 110,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
    }

    private static readonly TimeSpan[] InactivityTimeSpans =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(3),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
    ];

    private string HumanizeTimeSpan(TimeSpan span)
    {
        if (span.TotalSeconds < 60)
            return $"{span.TotalSeconds:0} {Get("AutomationPage_Unit_Seconds", "seconds")}";
        if (span.TotalMinutes < 60)
            return $"{span.TotalMinutes:0} {Get("AutomationPage_Unit_Minutes", "minutes")}";
        var hours = (int)span.TotalHours;
        if (hours < 24)
            return $"{hours} {Get("AutomationPage_Unit_Hours", "hours")}";
        return $"{span.TotalDays:0} {Get("AutomationPage_Unit_Days", "days")}";
    }

    private sealed class StateComboBox<T> where T : struct
    {
        private readonly IAutomationStep<T> _step;
        private readonly Action _changed;
        private readonly Func<T, string> _displayName;
        private readonly AccessibleComboBox _comboBox = new() { MinWidth = 200, HorizontalAlignment = HorizontalAlignment.Stretch };
        private T _current;
        private IAutomationStep _model;
        private bool _applying;

        public StateComboBox(IAutomationStep<T> step, Func<T, string>? displayName, Action changed)
        {
            _step = step;
            _changed = changed;
            _displayName = displayName ?? (value => value is IDisplayName dn ? dn.DisplayName : value is Enum e ? e.GetDisplayName() : value.ToString() ?? string.Empty);
            _current = step.State;
            _model = step;
            _comboBox.Loaded += ComboBox_Loaded;
            _comboBox.SelectionChanged += ComboBox_SelectionChanged;
        }

        public AccessibleComboBox Editor => _comboBox;
        public Func<string> Serialize => () => AutomationSerialization.SerializeStep(_model);
        public Func<string?> Validate => () => _comboBox.SelectedItem is null
            ? AvaloniaLocalization.GetString("AutomationPage_Validation_StepState", "Select a value.")
            : null;

        private async void ComboBox_Loaded(object? sender, RoutedEventArgs e)
        {
            _applying = true;
            try
            {
                T[] states;
                try { states = await _step.GetAllStatesAsync().ConfigureAwait(true); }
                catch { states = []; }
                var items = states.Select(value => (object)value).ToList();
                _comboBox.ItemsSource = items;
                _comboBox.SelectedItem = items.FirstOrDefault(value => ((T)value).Equals(_current)) ?? items.FirstOrDefault();
            }
            finally
            {
                _applying = false;
            }
        }

        private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_applying || _comboBox.SelectedItem is not T state)
                return;
            if (state.Equals(_current))
                return;
            _current = state;
            _model = (IAutomationStep)Activator.CreateInstance(_step.GetType(), state)!;
            _changed();
        }
    }

    private sealed record DisplayOption<T>(T Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed class TypedStepEditor
    {
        public Control? Editor { get; init; }
        public Func<string> Serialize { get; init; } = static () => string.Empty;
        public Func<string?>? Validate { get; init; }
        public Action? Refresh { get; init; }
    }

    private sealed class TypedTriggerEditor
    {
        public Control? Editor { get; init; }
        public Func<IAutomationPipelineTrigger> GetTrigger { get; init; } = static () => throw new InvalidOperationException("Missing trigger factory.");
        public Func<string?>? Validate { get; init; }
    }
#endif

    private sealed class PipelineRow(
        Guid id,
        bool isAutomatic,
        TextBox nameEditor,
        TextBox? iconEditor,
        Border card,
        ComboBox? triggerEditor,
        TextBox? triggerConfigEditor,
        CheckBox exclusiveEditor,
        StackPanel stepsPanel,
        List<StepRow> stepRows)
    {
        public Guid Id { get; } = id;
        public bool IsAutomatic { get; } = isAutomatic;
        public TextBox NameEditor { get; } = nameEditor;
        public TextBox? IconEditor { get; } = iconEditor;
        public Border Card { get; } = card;
        public ComboBox? TriggerEditor { get; } = triggerEditor;
        public TextBox? TriggerConfigEditor { get; } = triggerConfigEditor;
        public CheckBox ExclusiveEditor { get; } = exclusiveEditor;
        public StackPanel StepsPanel { get; } = stepsPanel;
        public List<StepRow> StepRows { get; } = stepRows;
        public bool IsNew { get; set; }
        public LocalizedTextBlock SummaryText { get; set; } = null!;
#if WINDOWS
        public StackPanel? TriggerListPanel { get; set; }
        public ComboBox? AddTriggerEditor { get; set; }
        public Button? AddTriggerButton { get; set; }
        public List<TriggerRow> TriggerRows { get; } = [];
#endif
    }

    private sealed class StepRow(AutomationStepOption option, ComboBox typeEditor, TextBox configEditor, Border card)
    {
        public AutomationStepOption Option { get; } = option;
        public ComboBox TypeEditor { get; } = typeEditor;
        public TextBox ConfigEditor { get; } = configEditor;
        public Border Card { get; } = card;
#if WINDOWS
        public Func<string>? Serialize { get; set; }
        public Func<string?>? Validate { get; set; }
        public Action? RefreshQuickActionTargets { get; set; }
        public LocalizedTextBlock? ValidationText { get; set; }
#endif
    }

#if WINDOWS
    private sealed class TriggerRow(
        string? optionKey,
        Func<IAutomationPipelineTrigger> getTrigger,
        Func<string?>? validate,
        Border card,
        LocalizedTextBlock validationText)
    {
        public string? OptionKey { get; } = optionKey;
        public Func<IAutomationPipelineTrigger> GetTrigger { get; } = getTrigger;
        public Func<string?>? Validate { get; } = validate;
        public Border Card { get; } = card;
        public LocalizedTextBlock ValidationText { get; } = validationText;
    }
#endif
}
