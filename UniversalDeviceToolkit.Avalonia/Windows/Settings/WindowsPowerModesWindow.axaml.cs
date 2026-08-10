using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Settings
{
public partial class WindowsPowerModesWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly PowerModeFeature _powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();

    private bool IsRefreshing => _loader.IsLoading;

    public WindowsPowerModesWindow()
    {
        InitializeComponent();

        PropertyChanged += PowerModesWindow_PropertyChanged;
    }

    private async void PowerModesWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
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
                Log.Instance.Trace($"Error in {nameof(PowerModesWindow_PropertyChanged)}: {ex.Message}", ex);
        }
    }

    private async Task RefreshAsync()
    {
        _loader.IsLoading = true;

        var loadingTask = Task.Delay(500);

        var powerModes = Enum.GetValues<WindowsPowerMode>();
        Refresh(_quietModeComboBox, powerModes, PowerModeState.Quiet);
        Refresh(_balanceModeComboBox, powerModes, PowerModeState.Balance);
        Refresh(_performanceModeComboBox, powerModes, PowerModeState.Performance);

        var allStates = await _powerModeFeature.GetAllStatesAsync();
        if (allStates.Contains(PowerModeState.GodMode))
            Refresh(_godModeComboBox, powerModes, PowerModeState.GodMode);
        else
            _godModeCardControl.IsVisible = false;

        await loadingTask;

        _loader.IsLoading = false;
    }

    private void Refresh(ComboBox comboBox, WindowsPowerMode[] windowsPowerPlans, PowerModeState powerModeState)
    {
        var selectedValue = _settings.Store.PowerModes.GetValueOrDefault(powerModeState, WindowsPowerMode.Balanced);
        comboBox.SetItems(windowsPowerPlans, selectedValue, pm => pm.GetDisplayName());
    }

    private async Task WindowsPowerModeChangedAsync(WindowsPowerMode windowsPowerMode, PowerModeState powerModeState)
    {
        if (IsRefreshing)
            return;

        _settings.Store.PowerModes[powerModeState] = windowsPowerMode;
        _settings.SynchronizeStore();

        await _powerModeFeature.EnsureCorrectWindowsPowerSettingsAreSetAsync();
    }

    private async void QuietModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_quietModeComboBox.TryGetSelectedItem(out WindowsPowerMode windowsPowerMode))
                await WindowsPowerModeChangedAsync(windowsPowerMode, PowerModeState.Quiet);
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
            if (_balanceModeComboBox.TryGetSelectedItem(out WindowsPowerMode windowsPowerMode))
                await WindowsPowerModeChangedAsync(windowsPowerMode, PowerModeState.Balance);
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
            if (_performanceModeComboBox.TryGetSelectedItem(out WindowsPowerMode windowsPowerMode))
                await WindowsPowerModeChangedAsync(windowsPowerMode, PowerModeState.Performance);
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
            if (_godModeComboBox.TryGetSelectedItem(out WindowsPowerMode windowsPowerMode))
                await WindowsPowerModeChangedAsync(windowsPowerMode, PowerModeState.GodMode);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(GodModeComboBox_SelectionChanged)}: {ex.Message}", ex);
        }
    }
}
}
