using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.System.Management;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows.Settings;

namespace UniversalDeviceToolkit.Avalonia.Controls.Settings
{
public partial class SettingsPowerControl : global::Avalonia.Controls.UserControl
{
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly PowerModeFeature _powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
    private bool _isRefreshing;

    public SettingsPowerControl()
    {
        InitializeComponent();
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        // Run all async operations in parallel
        var miTask = MachineCompatibility.GetMachineInformationAsync();
        var powerModeSupportedTask = _powerModeFeature.IsSupportedAsync();

        await Task.WhenAll(miTask, powerModeSupportedTask);

        var mi = await miTask;
        var isPowerModeFeatureSupported = await powerModeSupportedTask;

        // Check GodModeFnQSwitchable capability and get value if supported
        // Note: If WMI call fails, the card will be hidden to avoid showing broken UI
        var hasGodModeFnQ = mi.Features[CapabilityID.GodModeFnQSwitchable];
        int? fnQValue = null;

        if (hasGodModeFnQ)
        {
            try
            {
                fnQValue = await WMI.LenovoOtherMethod.GetFeatureValueAsync(CapabilityID.GodModeFnQSwitchable);
            }
            catch (Exception ex)
            {
                // Log failure but continue - card will be hidden since fnQValue remains null
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to get GodModeFnQSwitchable status.", ex);
            }
        }

        _godModeFnQSwitchableCard.IsVisible = hasGodModeFnQ && fnQValue.HasValue ? true : false;
        _godModeFnQSwitchableToggle.IsChecked = fnQValue == 1;

        _powerModeMappingComboBox.SetItems(Enum.GetValues<PowerModeMappingMode>(), _settings.Store.PowerModeMappingMode, t => t.GetDisplayName());

        _powerModeMappingCard.IsVisible = isPowerModeFeatureSupported ? true : false;
        _powerModesCard.IsVisible = _settings.Store.PowerModeMappingMode == PowerModeMappingMode.WindowsPowerMode && isPowerModeFeatureSupported ? true : false;
        _windowsPowerPlansCard.IsVisible = _settings.Store.PowerModeMappingMode == PowerModeMappingMode.WindowsPowerPlan && isPowerModeFeatureSupported ? true : false;
        _windowsPowerPlansControlPanelCard.IsVisible = _settings.Store.PowerModeMappingMode == PowerModeMappingMode.WindowsPowerPlan && isPowerModeFeatureSupported ? true : false;

        _onBatterySinceResetToggle.IsChecked = _settings.Store.ResetBatteryOnSinceTimerOnReboot;

        _isRefreshing = false;
    }

    private async void GodModeFnQSwitchableToggle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isRefreshing)
                return;

            var state = _godModeFnQSwitchableToggle.IsChecked;
            if (state is null)
                return;

            _godModeFnQSwitchableToggle.IsEnabled = false;

            await WMI.LenovoOtherMethod.SetFeatureValueAsync(CapabilityID.GodModeFnQSwitchable, state.Value ? 1 : 0);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to set GodModeFnQSwitchable.", ex);
        }
        finally
        {
            _godModeFnQSwitchableToggle.IsEnabled = true;
        }
    }

    private async void PowerModeMappingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_isRefreshing)
                return;

            if (!_powerModeMappingComboBox.TryGetSelectedItem(out PowerModeMappingMode powerModeMappingMode))
                return;

            _settings.Store.PowerModeMappingMode = powerModeMappingMode;
            _settings.SynchronizeStore();

            var isPowerModeFeatureSupported = await _powerModeFeature.IsSupportedAsync();
            _powerModesCard.IsVisible = _settings.Store.PowerModeMappingMode == PowerModeMappingMode.WindowsPowerMode && isPowerModeFeatureSupported ? true : false;
            _windowsPowerPlansCard.IsVisible = _settings.Store.PowerModeMappingMode == PowerModeMappingMode.WindowsPowerPlan && isPowerModeFeatureSupported ? true : false;
            _windowsPowerPlansControlPanelCard.IsVisible = _settings.Store.PowerModeMappingMode == PowerModeMappingMode.WindowsPowerPlan && isPowerModeFeatureSupported ? true : false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(PowerModeMappingComboBox_SelectionChanged)}.", ex);
        }
    }

    private void WindowsPowerPlans_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is null)
            return;

        var window = new WindowsPowerPlansWindow();
        window.ShowDialog(owner);
    }

    private void PowerModes_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is null)
            return;

        var window = new WindowsPowerModesWindow();
        window.ShowDialog(owner);
    }

    private void WindowsPowerPlansControlPanel_Click(object sender, RoutedEventArgs e)
    {
        using var process = Process.Start("control", "/name Microsoft.PowerOptions");
    }

    private void OnBatterySinceResetToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _onBatterySinceResetToggle.IsChecked;
        if (state is null)
            return;

        _settings.Store.ResetBatteryOnSinceTimerOnReboot = state.Value;
        _settings.SynchronizeStore();
    }
}
}
