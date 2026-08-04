using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Avalonia automation editor. The page keeps a local draft and sends the complete
/// ordered pipeline (including step and trigger payloads) to the shared processor only
/// when Save is pressed.
/// </summary>
public partial class AutomationPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly List<PipelineRow> _rows = [];
    private IReadOnlyList<AutomationTriggerOption> _triggerOptions = Array.Empty<AutomationTriggerOption>();
    private IReadOnlyList<AutomationStepOption> _stepOptions = Array.Empty<AutomationStepOption>();
    private bool _isRefreshing;
    private bool _isDirty;

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
            _triggerOptions = await _platformServices.GetAutomationTriggerOptionsAsync();
            _stepOptions = await _platformServices.GetAutomationStepOptionsAsync();
            var state = await _platformServices.GetAutomationWorkspaceAsync();
            EnabledToggle.IsChecked = state.IsEnabled;
            PipelineList.Children.Clear();
            _rows.Clear();

            foreach (var pipeline in state.Pipelines)
            {
                var row = CreateRow(pipeline);
                _rows.Add(row);
                PipelineList.Children.Add(row.Card);
            }

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

        ComboBox? triggerEditor = null;
        TextBox? triggerConfigEditor = null;
        if (pipeline.IsAutomatic)
        {
            triggerEditor = new ComboBox
            {
                ItemsSource = _triggerOptions,
                SelectedItem = FindTriggerOption(pipeline),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinWidth = 180,
            };
            copy.Children.Add(triggerEditor);

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
        foreach (var step in pipeline.Steps)
        {
            var option = FindStepOption(step);
            var stepRow = CreateStepRow(option, step.ConfigurationJson, stepsPanel, stepRows);
            stepRows.Add(stepRow);
            stepsPanel.Children.Add(stepRow.Card);
        }
        var addStepEditor = new ComboBox
        {
            ItemsSource = _stepOptions,
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
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
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

        var row = new PipelineRow(pipeline.Id, pipeline.IsAutomatic, pipeline.IconName, nameEditor, card, triggerEditor, triggerConfigEditor, exclusiveEditor, stepsPanel, stepRows);
        nameEditor.TextChanged += (_, _) => { if (!_isRefreshing) MarkDirty(); };
        if (triggerConfigEditor is not null)
            triggerConfigEditor.TextChanged += (_, _) => { if (!_isRefreshing) MarkDirty(); };
        exclusiveEditor.IsCheckedChanged += (_, _) => { if (!_isRefreshing) MarkDirty(); };
        triggerEditor?.SelectionChanged += (_, _) =>
        {
            if (_isRefreshing)
                return;
            if (triggerEditor.SelectedItem is AutomationTriggerOption option)
            {
                triggerConfigEditor!.Text = option.DefaultConfigurationJson ?? string.Empty;
                summary.Text = FormatSummary(option.DisplayName, true, stepRows.Count);
            }
            MarkDirty();
        };
        addStepButton.Click += (_, _) =>
        {
            if (addStepEditor.SelectedItem is not AutomationStepOption option)
                return;
            var stepRow = CreateStepRow(option, option.DefaultConfigurationJson, stepsPanel, stepRows);
            stepRows.Add(stepRow);
            stepsPanel.Children.Add(stepRow.Card);
            summary.Text = FormatSummary(triggerEditor?.SelectedItem is AutomationTriggerOption t ? t.DisplayName : pipeline.Trigger, pipeline.IsAutomatic, stepRows.Count);
            runButton.IsEnabled = true;
            addStepEditor.SelectedItem = null;
            MarkDirty();
        };
        runButton.Click += async (_, _) => await RunPipelineAsync(row);
        deleteButton.Click += (_, _) => DeleteRow(row);
        return row;
    }

    private StepRow CreateStepRow(AutomationStepOption option, string configurationJson, Panel panel, List<StepRow> rows)
    {
        var typeEditor = new ComboBox { ItemsSource = _stepOptions, SelectedItem = option, MinWidth = 190 };
        var configEditor = new TextBox
        {
            Text = configurationJson,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 46,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var upButton = new Button { Content = "↑", MinWidth = 30 };
        var downButton = new Button { Content = "↓", MinWidth = 30 };
        var deleteButton = new Button { Content = Get("Delete", "Delete"), MinWidth = 64 };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        buttons.Children.Add(upButton);
        buttons.Children.Add(downButton);
        buttons.Children.Add(deleteButton);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 6 };
        grid.Children.Add(typeEditor);
        Grid.SetColumn(configEditor, 1);
        grid.Children.Add(configEditor);
        Grid.SetColumn(buttons, 2);
        grid.Children.Add(buttons);
        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Child = grid,
        };
        var row = new StepRow(option, typeEditor, configEditor, card);
        configEditor.TextChanged += (_, _) => { if (!_isRefreshing) MarkDirty(); };
        typeEditor.SelectionChanged += (_, _) =>
        {
            if (_isRefreshing)
                return;
            if (typeEditor.SelectedItem is AutomationStepOption selected)
                configEditor.Text = selected.DefaultConfigurationJson;
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
        row.NameEditor.Focus();
        row.NameEditor.SelectAll();
    }

    private void AddAutomaticButton_Click(object? sender, RoutedEventArgs e)
    {
        var option = _triggerOptions.FirstOrDefault();
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
        row.NameEditor.Focus();
        row.NameEditor.SelectAll();
    }

    private void DeleteRow(PipelineRow row)
    {
        _rows.Remove(row);
        PipelineList.Children.Remove(row.Card);
        EmptyText.IsVisible = _rows.Count == 0;
        MarkDirty();
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
        var drafts = _rows.Select(row => new AutomationPipelineDraft(row.IsNew ? null : row.Id, row.NameEditor.Text, row.IconName, row.IsAutomatic)
        {
            TriggerKey = row.TriggerEditor?.SelectedItem is AutomationTriggerOption option ? option.Key : null,
            TriggerConfigurationJson = row.TriggerConfigEditor?.Text,
            IsExclusive = row.ExclusiveEditor.IsChecked ?? true,
            Steps = row.StepRows.Select(step => new AutomationStepItem(
                step.TypeEditor.SelectedItem is AutomationStepOption option ? option.TypeKey : step.Option.TypeKey,
                step.TypeEditor.SelectedItem is AutomationStepOption selected ? selected.DisplayName : step.Option.DisplayName,
                step.ConfigEditor.Text ?? string.Empty)).ToArray(),
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
            SetFeedback(Get("AutomationPage_Saved_Message", "Automation pipelines saved."));
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

    private void SetFeedback(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            ToolTip.SetTip(SaveButton, message);
    }

    private string FormatSummary(string trigger, bool automatic, int stepCount)
    {
        var kind = automatic ? trigger : Get("AutomationPage_QuickActions_Title", "Manual quick action");
        return $"{kind} | {stepCount} {Get("AutomationPipelineControl_Step", "step(s)")}";
    }

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

    private sealed class PipelineRow(
        Guid id,
        bool isAutomatic,
        string? iconName,
        TextBox nameEditor,
        Border card,
        ComboBox? triggerEditor,
        TextBox? triggerConfigEditor,
        CheckBox exclusiveEditor,
        StackPanel stepsPanel,
        List<StepRow> stepRows)
    {
        public Guid Id { get; } = id;
        public bool IsAutomatic { get; } = isAutomatic;
        public string? IconName { get; } = iconName;
        public TextBox NameEditor { get; } = nameEditor;
        public Border Card { get; } = card;
        public ComboBox? TriggerEditor { get; } = triggerEditor;
        public TextBox? TriggerConfigEditor { get; } = triggerConfigEditor;
        public CheckBox ExclusiveEditor { get; } = exclusiveEditor;
        public StackPanel StepsPanel { get; } = stepsPanel;
        public List<StepRow> StepRows { get; } = stepRows;
        public bool IsNew { get; set; }
    }

    private sealed record StepRow(
        AutomationStepOption Option,
        ComboBox TypeEditor,
        TextBox ConfigEditor,
        Border Card);
}
