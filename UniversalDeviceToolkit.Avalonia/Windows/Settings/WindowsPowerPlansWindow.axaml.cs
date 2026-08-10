using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Settings
{
public partial class WindowsPowerPlansWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private static readonly WindowsPowerPlan DefaultValue = new(Guid.Empty, Resource.WindowsPowerPlansWindow_DefaultPowerPlan, false);

    private readonly WindowsPowerPlanController _windowsPowerPlanController = IoCContainer.Resolve<WindowsPowerPlanController>();
    private readonly PowerModeFeature _powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();

    private bool IsRefreshing => _loader.IsLoading;

    public WindowsPowerPlansWindow()
    {
        InitializeComponent();

        PropertyChanged += PowerPlansWindow_PropertyChanged;
    }

    private async void PowerPlansWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        try
        {
            if (IsVisible)
                await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(PowerPlansWindow_PropertyChanged)}: {ex.Message}", ex);
        }
    }

    private async Task RefreshAsync()
    {
        _loader.IsLoading = true;

        var loadingTask = Task.Delay(500);

        var compatibility = await MachineCompatibility.GetMachineInformationAsync();
        _aoAcWarningCard.IsVisible = compatibility.Properties.SupportsAlwaysOnAc.status
            ? true
            : false;

        var powerPlans = _windowsPowerPlanController.GetPowerPlans().OrderBy(x => x.Name).Prepend(DefaultValue).ToArray();
        Refresh(_quietModeComboBox, powerPlans, PowerModeState.Quiet);
        Refresh(_balanceModeComboBox, powerPlans, PowerModeState.Balance);
        Refresh(_performanceModeComboBox, powerPlans, PowerModeState.Performance);

        var allStates = await _powerModeFeature.GetAllStatesAsync();
        if (allStates.Contains(PowerModeState.GodMode))
            Refresh(_godModeComboBox, powerPlans, PowerModeState.GodMode);
        else
            _godModeCardControl.IsVisible = false;

        await loadingTask;

        _loader.IsLoading = false;
    }

    private void Refresh(ComboBox comboBox, WindowsPowerPlan[] windowsPowerPlans, PowerModeState powerModeState)
    {
        var settingsPowerPlanGuid = _settings.Store.PowerPlans.GetValueOrDefault(powerModeState);
        var selectedValue = windowsPowerPlans.FirstOrDefault(pp => pp.Guid == settingsPowerPlanGuid);
        comboBox.SetItems(windowsPowerPlans, selectedValue, pp => pp.Name);
    }

    private async Task WindowsPowerPlanChangedAsync(WindowsPowerPlan windowsPowerPlan, PowerModeState powerModeState)
    {
        if (IsRefreshing)
            return;

        _settings.Store.PowerPlans[powerModeState] = windowsPowerPlan.Guid;
        _settings.SynchronizeStore();

        await _powerModeFeature.EnsureCorrectWindowsPowerSettingsAreSetAsync();
    }

    private async void QuietModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_quietModeComboBox.TryGetSelectedItem(out WindowsPowerPlan windowsPowerPlan))
                await WindowsPowerPlanChangedAsync(windowsPowerPlan, PowerModeState.Quiet);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(QuietModeComboBox_SelectionChanged)}: {ex.Message}", ex);
        }
    }

    private async void BalanceModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_balanceModeComboBox.TryGetSelectedItem(out WindowsPowerPlan windowsPowerPlan))
                await WindowsPowerPlanChangedAsync(windowsPowerPlan, PowerModeState.Balance);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(BalanceModeComboBox_SelectionChanged)}: {ex.Message}", ex);
        }
    }

    private async void PerformanceModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_performanceModeComboBox.TryGetSelectedItem(out WindowsPowerPlan windowsPowerPlan))
                await WindowsPowerPlanChangedAsync(windowsPowerPlan, PowerModeState.Performance);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(PerformanceModeComboBox_SelectionChanged)}: {ex.Message}", ex);
        }
    }

    private async void GodModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_godModeComboBox.TryGetSelectedItem(out WindowsPowerPlan windowsPowerPlan))
                await WindowsPowerPlanChangedAsync(windowsPowerPlan, PowerModeState.GodMode);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(GodModeComboBox_SelectionChanged)}: {ex.Message}", ex);
        }
    }
}
}
