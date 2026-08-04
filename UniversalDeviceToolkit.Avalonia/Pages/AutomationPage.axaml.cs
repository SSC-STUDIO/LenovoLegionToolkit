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
/// Avalonia automation workspace backed by the shared AutomationProcessor.
/// Existing pipelines are edited as drafts and only reach the processor on Save;
/// Revert simply reloads the persisted snapshot, matching WPF's dirty-buffer model.
/// </summary>
public partial class AutomationPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly List<PipelineRow> _rows = [];
    private IReadOnlyList<AutomationTriggerOption> _triggerOptions = Array.Empty<AutomationTriggerOption>();
    private bool _isRefreshing;
    private bool _isDirty;

    public AutomationPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();

        PageTitle.Text = Get("AutomationPage_Title", "Actions");
        PageDescription.Text = Get(
            "AutomationPage_Actions_Message",
            "Configure automation pipelines and run quick actions.");
        EnabledTitle.Text = Get("AutomationPage_ActionsEnabled_Title", "Automation service");
        EnabledDescription.Text = Get(
            "AutomationPage_ActionsEnable_Message",
            "Enable or disable automation event listeners.");
        PipelinesTitle.Text = Get("AutomationPage_QuickActions_Title", "Quick actions");
        PipelinesDescription.Text = Get(
            "AutomationPage_QuickActions_Message",
            "Run configured pipelines on demand.");
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
            Text = FormatSummary(pipeline),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        };
        var copy = new StackPanel { Spacing = 4, MinWidth = 0 };
        copy.Children.Add(nameEditor);

        ComboBox? triggerEditor = null;
        if (pipeline.IsAutomatic && _triggerOptions.Count > 0 && pipeline.TriggerKey is not null)
        {
            triggerEditor = new ComboBox
            {
                ItemsSource = _triggerOptions,
                SelectedItem = _triggerOptions.FirstOrDefault(option =>
                    string.Equals(option.Key, pipeline.TriggerKey, StringComparison.OrdinalIgnoreCase)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinWidth = 180,
            };
            copy.Children.Add(triggerEditor);
        }

        copy.Children.Add(summary);

        var runButton = new Button
        {
            Content = Get("Run", "Run"),
            IsEnabled = pipeline.IsAutomatic || pipeline.StepCount > 0,
            MinWidth = 72,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(runButton, $"AutomationPipeline_{pipeline.Id:N}_RunButton");
        AutomationProperties.SetName(runButton, $"{Get("Run", "Run")} {pipeline.Name ?? pipeline.Id.ToString()}");
        ToolTip.SetTip(runButton, summary.Text);

        var deleteButton = new Button
        {
            Content = Get("Delete", "Delete"),
            MinWidth = 72,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(deleteButton, $"AutomationPipeline_{pipeline.Id:N}_DeleteButton");
        AutomationProperties.SetName(deleteButton, $"{Get("Delete", "Delete")} {pipeline.Name ?? pipeline.Id.ToString()}");

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        actions.Children.Add(runButton);
        actions.Children.Add(deleteButton);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 14,
        };
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

        var row = new PipelineRow(pipeline.Id, pipeline.IsAutomatic, pipeline.IconName, nameEditor, card, triggerEditor);
        nameEditor.TextChanged += (_, _) =>
        {
            if (!_isRefreshing)
                MarkDirty();
        };
        if (triggerEditor is not null)
        {
            triggerEditor.SelectionChanged += (_, _) =>
            {
                if (_isRefreshing)
                    return;

                if (triggerEditor.SelectedItem is AutomationTriggerOption option)
                    summary.Text = FormatSummary(pipeline with { Trigger = option.DisplayName });
                MarkDirty();
            };
        }
        runButton.Click += async (_, _) => await RunPipelineAsync(row);
        deleteButton.Click += (_, _) => DeleteRow(row);
        return row;
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var item = new AutomationPipelineItem(
            Guid.Empty,
            Get("AutomationPage_AddManualPipeline_Placeholder", "New quick action"),
            null,
            Get("AutomationPage_QuickActions_Title", "Manual quick action"),
            0,
            false);
        var row = CreateRow(item) with { IsNew = true };
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

        var item = new AutomationPipelineItem(
            Guid.Empty,
            Get("AutomationPage_AddAutomaticPipeline_Placeholder", "New automatic pipeline"),
            null,
            option.DisplayName,
            0,
            true)
        {
            TriggerKey = option.Key,
        };
        var row = CreateRow(item) with { IsNew = true };
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

        var accepted = await _platformServices.SetFeatureActionAsync(
            "Actions",
            $"automation-pipeline:{row.Id:D}",
            true);
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
                row.IconName,
                row.IsAutomatic)
            {
                TriggerKey = row.TriggerEditor?.SelectedItem is AutomationTriggerOption option
                    ? option.Key
                    : null,
            })
            .ToArray();
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

    private string FormatSummary(AutomationPipelineItem pipeline)
    {
        var kind = pipeline.IsAutomatic
            ? pipeline.Trigger
            : Get("AutomationPage_QuickActions_Title", "Manual quick action");
        return $"{kind} | {pipeline.StepCount} {Get("AutomationPipelineControl_Step", "step(s)")}";
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

    private sealed record PipelineRow(
        Guid Id,
        bool IsAutomatic,
        string? IconName,
        TextBox NameEditor,
        Border Card,
        ComboBox? TriggerEditor = null,
        bool IsNew = false);
}
