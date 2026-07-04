using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Pipeline;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.Lib.Extensions;
using UniversalDeviceToolkit.WPF.Controls.Automation;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.Automation;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using Wpf.Ui.Controls;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace UniversalDeviceToolkit.WPF.Pages
{
public partial class AutomationPage
{
    private readonly AutomationProcessor _automationProcessor = IoCContainer.Resolve<AutomationProcessor>();

    private IAutomationStep[] _supportedAutomationSteps = [];

    internal static TimeSpan GetAutomationFallbackLoadingDelay() => TimeSpan.FromMilliseconds(120);

    public AutomationPage()
    {
        Initialized += AutomationPage_Initialized;
        Unloaded += AutomationPage_Unloaded;

        InitializeComponent();
    }

    private async void AutomationPage_Initialized(object? sender, EventArgs e)
    {
        await RefreshAsync().ConfigureAwait(false);
    }

    private async void EnableAutomaticPipelinesToggle_Click(object sender, RoutedEventArgs e)
    {
        var isChecked = _enableAutomaticPipelinesToggle.IsChecked;
        if (isChecked.HasValue)
            await _automationProcessor.SetEnabledAsync(isChecked.Value).ConfigureAwait(false);
    }

    private void NewAutomaticPipelineButton_Click(object sender, RoutedEventArgs e)
    {
        var existingTriggersTypes = _automaticPipelinesStackPanel.Children.ToArray()
            .OfType<AutomationPipelineControl>()
            .Select(c => c.AutomationPipeline.Trigger)
            .Where(t => t is not null)
            .Select(t => t!.GetType())
            .ToHashSet();

        var window = new CreateAutomationPipelineWindow(existingTriggersTypes, AddAutomaticPipeline) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private async void NewManualPipelineButton_Click(object sender, RoutedEventArgs e)
    {
        await AddManualPipelineAsync().ConfigureAwait(false);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _saveButton.IsEnabled = false;
            _saveButton.Content = Resource.Saving;

            var automaticPipelines = _automaticPipelinesStackPanel.Children.ToArray()
                .OfType<AutomationPipelineControl>()
                .Select(c => c.CreateAutomationPipeline())
                .ToList();

            var manualPipelines = _manualPipelinesStackPanel.Children.ToArray()
                .OfType<AutomationPipelineControl>()
                .Select(c => c.CreateAutomationPipeline())
                .ToList();

            var pipelines = new List<AutomationPipeline>();
            pipelines.AddRange(automaticPipelines);
            pipelines.AddRange(manualPipelines);

            await _automationProcessor.ReloadPipelinesAsync(pipelines).ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);

            await SnackbarHelper.ShowAsync(Resource.AutomationPage_Saved_Title, Resource.AutomationPage_Saved_Message).ConfigureAwait(false);
        }
        finally
        {
            _saveButton.Content = Resource.Save;
            _saveButton.IsEnabled = true;
        }
    }

    private async void RevertButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync().ConfigureAwait(false);

        await SnackbarHelper.ShowAsync(Resource.AutomationPage_Reverted_Title, Resource.AutomationPage_Reverted_Message).ConfigureAwait(false);
    }

    private async Task RefreshAsync()
    {
        _scrollViewer.ScrollToTop();

        _loaderAutomatic.IsLoading = true;
        _loaderManual.IsLoading = true;

        var initializedTasks = new List<Task> { Task.Delay(GetAutomationFallbackLoadingDelay()) };

        _enableAutomaticPipelinesToggle.IsChecked = _automationProcessor.IsEnabled;

        _automaticPipelinesStackPanel.Children.Clear();
        _manualPipelinesStackPanel.Children.Clear();

        var pipelines = await _automationProcessor.GetPipelinesAsync().ConfigureAwait(false);

        if (_supportedAutomationSteps.IsEmpty())
            _supportedAutomationSteps = await GetSupportedAutomationStepsAsync().ConfigureAwait(false);

        foreach (var pipeline in pipelines.Where(p => p.Trigger is not null))
        {
            var control = GenerateControl(pipeline, _automaticPipelinesStackPanel);
            _automaticPipelinesStackPanel.Children.Add(control);
            initializedTasks.Add(control.InitializedTask);
        }

        foreach (var pipeline in pipelines.Where(p => p.Trigger is null))
        {
            var control = GenerateControl(pipeline, _manualPipelinesStackPanel, false);
            _manualPipelinesStackPanel.Children.Add(control);
            initializedTasks.Add(control.InitializedTask);
        }

        _saveRevertStackPanel.Visibility = Visibility.Collapsed;

        _noAutomaticActionsText.Visibility = _automaticPipelinesStackPanel.Children.Count < 1
            ? Visibility.Visible
            : Visibility.Collapsed;
        _noManualActionsText.Visibility = _manualPipelinesStackPanel.Children.Count < 1
            ? Visibility.Visible
            : Visibility.Collapsed;

        await Task.WhenAll(initializedTasks).ConfigureAwait(false);

        _loaderAutomatic.IsLoading = false;
        _loaderManual.IsLoading = false;
    }

    private static async Task<IAutomationStep[]> GetSupportedAutomationStepsAsync()
    {
        var steps = new IAutomationStep[]
        {
            new AlwaysOnUsbAutomationStep(default),
            new BatteryAutomationStep(default),
            new BatteryNightChargeAutomationStep(default),
            new DeactivateGPUAutomationStep(default),
            new DelayAutomationStep(default),
            new DisplayBrightnessAutomationStep(50),
            new DpiScaleAutomationStep(default),
            new FlipToStartAutomationStep(default),
            new FnLockAutomationStep(default),
            new GodModePresetAutomationStep(default),
            new HDRAutomationStep(default),
            new InstantBootAutomationStep(default),
            new MacroAutomationStep(default),
            new MicrophoneAutomationStep(default),
            new SpeakerAutomationStep(default),
            new NotificationAutomationStep(default),
            new OsdAutomationStep(default),
            new OneLevelWhiteKeyboardBacklightAutomationStep(default),
            new OverclockDiscreteGPUAutomationStep(default),
            new OverDriveAutomationStep(default),
            new PanelLogoBacklightAutomationStep(default),
            new PlaySoundAutomationStep(default),
            new PortsBacklightAutomationStep(default),
            new PowerModeAutomationStep(default),
            new QuickActionAutomationStep(default),
            new RefreshRateAutomationStep(default),
            new ResolutionAutomationStep(default),
            new RGBKeyboardBacklightAutomationStep(default),
            new RunAutomationStep(default, default, default, default),
            new SpectrumKeyboardBacklightBrightnessAutomationStep(0),
            new SpectrumKeyboardBacklightProfileAutomationStep(1),
            new SpectrumKeyboardBacklightImportProfileAutomationStep(default),
            new TouchpadLockAutomationStep(default),
            new TurnOffMonitorsAutomationStep(),
            new TurnOffWiFiAutomationStep(),
            new TurnOnWiFiAutomationStep(),
            new HybridModeAutomationStep(default),
            new WhiteKeyboardBacklightAutomationStep(default),
            new WinKeyAutomationStep(default)
        };

        var supportTasks = steps.Select(async step => new { Step = step, Supported = await step.IsSupportedAsync().ConfigureAwait(false) });
        var results = await Task.WhenAll(supportTasks).ConfigureAwait(false);

        return results.Where(r => r.Supported).Select(r => r.Step).ToArray();
    }

    private AutomationPipelineControl GenerateControl(AutomationPipeline pipeline, Panel stackPanel, bool allowQuickActionAutomationStep = true)
    {
        var supportedSteps = _supportedAutomationSteps;
        if (!allowQuickActionAutomationStep)
        {
            supportedSteps = Array.FindAll(supportedSteps, s => s is not QuickActionAutomationStep);
        }

        var control = new AutomationPipelineControl(pipeline, supportedSteps);
        control.MouseRightButtonUp += PipelineControl_MouseRightButtonUp;
        control.OnChanged += PipelineControl_OnChanged;
        control.OnDelete += PipelineControl_OnDelete;
        return control;
    }

    private void PipelineControl_MouseRightButtonUp(object? sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is AutomationPipelineControl control)
        {
            var stackPanel = _automaticPipelinesStackPanel.Children.Contains(control)
                ? _automaticPipelinesStackPanel
                : _manualPipelinesStackPanel.Children.Contains(control)
                    ? _manualPipelinesStackPanel
                    : null;

            if (stackPanel is not null)
                ShowPipelineContextMenu(control, stackPanel);
            e.Handled = true;
        }
    }

    private void PipelineControl_OnChanged(object? sender, EventArgs e)
    {
        PipelinesChanged();
    }

    private void PipelineControl_OnDelete(object? sender, EventArgs e)
    {
        if (sender is AutomationPipelineControl c)
        {
            var stackPanel = _automaticPipelinesStackPanel.Children.Contains(c)
                ? _automaticPipelinesStackPanel
                : _manualPipelinesStackPanel.Children.Contains(c)
                    ? _manualPipelinesStackPanel
                    : null;

            if (stackPanel is not null)
                DeletePipeline(c, stackPanel);
        }
    }

    private void PipelinesChanged()
    {
        _saveRevertStackPanel.Visibility = Visibility.Visible;
    }

    private void ShowPipelineContextMenu(AutomationPipelineControl control, Panel stackPanel)
    {
        var menuItems = new List<MenuItem>();

        var index = stackPanel.Children.IndexOf(control);
        var maxIndex = stackPanel.Children.Count - 1;

        var moveUpMenuItem = new MenuItem { Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowUp24 }, Header = Resource.MoveUp };
        if (index > 0)
            moveUpMenuItem.Click += (_, _) => MovePipeline(control, stackPanel, index - 1);
        else
            moveUpMenuItem.IsEnabled = false;
        menuItems.Add(moveUpMenuItem);

        var moveDownMenuItem = new MenuItem { Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowDown24 }, Header = Resource.MoveDown };
        if (index < maxIndex)
            moveDownMenuItem.Click += (_, _) => MovePipeline(control, stackPanel, index + 1);
        else
            moveDownMenuItem.IsEnabled = false;
        menuItems.Add(moveDownMenuItem);

        var renameMenuItem = new MenuItem { Icon = new SymbolIcon { Symbol = SymbolRegular.Edit24 }, Header = Resource.Rename };
        renameMenuItem.Click += async (_, _) => await RenamePipelineAsync(control).ConfigureAwait(false);
        menuItems.Add(renameMenuItem);

        if (control.AutomationPipeline.Trigger is null)
        {
            var changeIconMenuItem = new MenuItem { Icon = new SymbolIcon { Symbol = SymbolRegular.Edit24 }, Header = Resource.AutomationPage_ChangeIcon };
            changeIconMenuItem.Click += async (_, _) => await ChangePipelineIconAsync(control).ConfigureAwait(false);
            menuItems.Add(changeIconMenuItem);
        }

        var deleteMenuItem = new MenuItem { Icon = new SymbolIcon { Symbol = SymbolRegular.Delete24 }, Header = Resource.Delete };
        deleteMenuItem.Click += (_, _) => DeletePipeline(control, stackPanel);
        menuItems.Add(deleteMenuItem);

        control.ContextMenu = new();
        control.ContextMenu.Items.AddRange(menuItems);
        control.ContextMenu.IsOpen = true;
    }

    private void MovePipeline(UIElement control, Panel stackPanel, int index)
    {
        stackPanel.Children.Remove(control);
        stackPanel.Children.Insert(index, control);

        PipelinesChanged();
    }

    private void AddAutomaticPipeline(IAutomationPipelineTrigger trigger)
    {
        var pipeline = new AutomationPipeline(trigger);
        var control = GenerateControl(pipeline, _automaticPipelinesStackPanel);
        _automaticPipelinesStackPanel.Children.Insert(0, control);

        _noAutomaticActionsText.Visibility = _automaticPipelinesStackPanel.Children.Count < 1
            ? Visibility.Visible
            : Visibility.Collapsed;

        PipelinesChanged();
    }

    private async Task AddManualPipelineAsync()
    {
        var newName = await MessageBoxHelper.ShowInputAsync(this,
            Resource.AutomationPage_AddManualPipeline_Title,
            Resource.AutomationPage_AddManualPipeline_Placeholder).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(newName))
            return;

        var pipeline = new AutomationPipeline(newName);
        var control = GenerateControl(pipeline, _manualPipelinesStackPanel, false);
        _manualPipelinesStackPanel.Children.Insert(0, control);

        _noManualActionsText.Visibility = _manualPipelinesStackPanel.Children.Count < 1
            ? Visibility.Visible
            : Visibility.Collapsed;

        PipelinesChanged();
    }

    private async Task RenamePipelineAsync(AutomationPipelineControl control)
    {
        var name = control.GetName();
        var newName = await MessageBoxHelper.ShowInputAsync(this,
            Resource.AutomationPage_RenamePipeline_Title,
            Resource.AutomationPage_RenamePipeline_Placeholder,
            name,
            allowEmpty: true).ConfigureAwait(false);

        control.SetName(newName);
    }

    private async Task ChangePipelineIconAsync(AutomationPipelineControl control)
    {
        try
        {
            var window = new SymbolRegularPicker { Owner = Window.GetWindow(this) };
            window.ShowDialog();

            var icon = await window.SymbolRegularTask.ConfigureAwait(false);
            control.SetIcon(icon);
        }
        catch (OperationCanceledException)
        {
            // Expected when user cancels icon selection, no action needed
        }
    }

    private void DeletePipeline(UIElement control, Panel stackPanel)
    {
        stackPanel.Children.Remove(control);

        _noAutomaticActionsText.Visibility = _automaticPipelinesStackPanel.Children.Count < 1
            ? Visibility.Visible
            : Visibility.Collapsed;
        _noManualActionsText.Visibility = _manualPipelinesStackPanel.Children.Count < 1
            ? Visibility.Visible
            : Visibility.Collapsed;

        PipelinesChanged();
    }

    private void AutomationPage_Unloaded(object? sender, RoutedEventArgs e)
    {
        Initialized -= AutomationPage_Initialized;
        Unloaded -= AutomationPage_Unloaded;
    }
}
}
