using System;
using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;
using Microsoft.Win32;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Settings;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.Osd;
using WpfHardwareSensorSettings = UniversalDeviceToolkit.WPF.Settings.HardwareSensorSettings;

namespace UniversalDeviceToolkit.WPF.Controls.Settings
{
public partial class SettingsApplicationBehaviorControl
{
    public event EventHandler<SoftwareStatus>? FnKeysStatusChanged;
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly VantageDisabler _vantageDisabler = IoCContainer.Resolve<VantageDisabler>();
    private readonly LegionZoneDisabler _legionZoneDisabler = IoCContainer.Resolve<LegionZoneDisabler>();
    private readonly FnKeysDisabler _fnKeysDisabler = IoCContainer.Resolve<FnKeysDisabler>();
    private readonly RGBKeyboardBacklightController _rgbKeyboardBacklightController = IoCContainer.Resolve<RGBKeyboardBacklightController>();
    private readonly OsdSettings _osdSettings = IoCContainer.Resolve<OsdSettings>();
    private readonly WpfHardwareSensorSettings _hardwareSensorSettings = IoCContainer.Resolve<WpfHardwareSensorSettings>();
    private readonly HardwareSensorsFeature _hardwareSensorsFeature = IoCContainer.Resolve<HardwareSensorsFeature>();
    private readonly SettingsBackupService _settingsBackupService = new();
    private bool _isRefreshing;

    public SettingsApplicationBehaviorControl()
    {
        InitializeComponent();
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        _autorunComboBox.SetItems(Enum.GetValues<AutorunState>(), Autorun.State, t => t.GetDisplayName());
        _minimizeToTrayToggle.IsChecked = _settings.Store.MinimizeToTray;
        _minimizeOnCloseToggle.IsChecked = _settings.Store.MinimizeOnClose;

        // Run all async operations in parallel
        var compatibilityTask = MachineCompatibility.IsCompatibleAsync();
        var miTask = MachineCompatibility.GetMachineInformationAsync();
        var vantageTask = _vantageDisabler.GetStatusAsync();
        var legionZoneTask = _legionZoneDisabler.GetStatusAsync();
        var fnKeysTask = _fnKeysDisabler.GetStatusAsync();

        await Task.WhenAll(compatibilityTask, miTask, vantageTask, legionZoneTask, fnKeysTask);

        var (isCompatible, _) = await compatibilityTask;
        if (!isCompatible)
        {
            _disableCompatibilityWarningCard.Visibility = Visibility.Visible;
            _disableCompatibilityWarningToggle.IsChecked = _settings.Store.DisableUnsupportedHardwareWarning;
        }
        else
        {
            _disableCompatibilityWarningCard.Visibility = Visibility.Collapsed;
        }

        var mi = await miTask;
        var isSupportedLegionMachine = MachineCompatibility.IsSupportedLegionMachine(mi);

        var vantageStatus = await vantageTask;
        _vantageCard.Visibility = isSupportedLegionMachine && vantageStatus != SoftwareStatus.NotFound ? Visibility.Visible : Visibility.Collapsed;
        _vantageToggle.IsChecked = vantageStatus == SoftwareStatus.Disabled;

        var legionZoneStatus = await legionZoneTask;
        _legionZoneCard.Visibility = isSupportedLegionMachine && legionZoneStatus != SoftwareStatus.NotFound ? Visibility.Visible : Visibility.Collapsed;
        _legionZoneToggle.IsChecked = legionZoneStatus == SoftwareStatus.Disabled;

        var fnKeysStatus = await fnKeysTask;
        _fnKeysCard.Visibility = isSupportedLegionMachine && fnKeysStatus != SoftwareStatus.NotFound ? Visibility.Visible : Visibility.Collapsed;
        _fnKeysToggle.IsChecked = fnKeysStatus == SoftwareStatus.Disabled;

        _osdToggle.IsChecked = _osdSettings.Store.ShowOsd;
        _hardwareSensorsToggle.IsChecked = _settings.Store.EnableHardwareSensors;
        _osdCardControl.Visibility = _settings.Store.EnableHardwareSensors ? Visibility.Visible : Visibility.Collapsed;

        if (PawnIOHelper.IsPawnIOInstalled())
        {
            _hardwareSensorsCardHeader.Warning = string.Empty;
        }
        else
        {
            _hardwareSensorsCardHeader.Warning = Resource.SettingsPage_HardwareSensors_PawnIOWarning;
        }

        _isRefreshing = false;
    }

    private static async Task RunWithToggleDisabledAsync(UIElement toggle, Func<Task> action)
    {
        toggle.IsEnabled = false;

        try
        {
            await action();
        }
        finally
        {
            toggle.IsEnabled = true;
        }
    }

    private void AutorunComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_autorunComboBox.TryGetSelectedItem(out AutorunState state))
            return;

        Autorun.Set(state);
    }

    private void MinimizeToTrayToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _minimizeToTrayToggle.IsChecked;
        if (state is null)
            return;

        _settings.Store.MinimizeToTray = state.Value;
        _settings.SynchronizeStore();
    }

    private void MinimizeOnCloseToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _minimizeOnCloseToggle.IsChecked;
        if (state is null)
            return;

        _settings.Store.MinimizeOnClose = state.Value;
        _settings.SynchronizeStore();
    }

    private void DisableCompatibilityWarningToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _disableCompatibilityWarningToggle.IsChecked;
        if (state is null)
            return;

        _settings.Store.DisableUnsupportedHardwareWarning = state.Value;
        _settings.SynchronizeStore();
    }

    private async void VantageToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _vantageToggle.IsChecked;
        if (state is null)
            return;

        try
        {
            await RunWithToggleDisabledAsync(_vantageToggle, async () =>
            {
                if (state.Value)
                {
                    try
                    {
                        await _vantageDisabler.DisableAsync();
                    }
                    catch
                    {
                        await SnackbarHelper.ShowAsync(Resource.SettingsPage_DisableVantage_Error_Title, Resource.SettingsPage_DisableVantage_Error_Message, SnackbarType.Error);
                        return;
                    }

                    try
                    {
                        if (await _rgbKeyboardBacklightController.IsSupportedAsync())
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Setting light control owner and restoring preset...");

                            await _rgbKeyboardBacklightController.SetLightControlOwnerAsync(true, true);
                        }
                    }
                    catch (System.Exception ex)
                    {
                    Log.Instance.Warning($"Couldn't set light control owner or current preset.", ex);
                    }

                    try
                    {
                        var controller = IoCContainer.Resolve<SpectrumKeyboardBacklightController>();
                        if (await controller.IsSupportedAsync())
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Starting Aurora if needed...");

                            var result = await controller.StartAuroraIfNeededAsync();
                            if (result)
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Aurora started.");
                            }
                            else
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Aurora not needed.");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                    Log.Instance.Warning($"Couldn't start Aurora if needed.", ex);
                    }
                }
                else
                {
                    try
                    {
                        if (await _rgbKeyboardBacklightController.IsSupportedAsync())
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Setting light control owner...");

                            await _rgbKeyboardBacklightController.SetLightControlOwnerAsync(false);
                        }
                    }
                    catch (System.Exception ex)
                    {
                    Log.Instance.Warning($"Couldn't set light control owner.", ex);
                    }

                    try
                    {
                        if (IoCContainer.TryResolve<SpectrumKeyboardBacklightController>() is { } spectrumKeyboardBacklightController)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Making sure Aurora is stopped...");

                            if (await spectrumKeyboardBacklightController.IsSupportedAsync())
                                await spectrumKeyboardBacklightController.StopAuroraIfNeededAsync();
                        }
                    }
                    catch (System.Exception ex)
                    {
                    Log.Instance.Warning($"Couldn't stop Aurora.", ex);
                    }

                    try
                    {
                        await _vantageDisabler.EnableAsync();
                    }
                    catch
                    {
                        await SnackbarHelper.ShowAsync(Resource.SettingsPage_EnableVantage_Error_Title, Resource.SettingsPage_EnableVantage_Error_Message, SnackbarType.Error);
                    }
                }
            });
        }
        catch (Exception ex)
        {
                    Log.Instance.Warning($"Unexpected failure while toggling Vantage state.", ex);
        }
    }

    private async void LegionZoneToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _legionZoneToggle.IsChecked;
        if (state is null)
            return;

        try
        {
            await RunWithToggleDisabledAsync(_legionZoneToggle, async () =>
            {
                try
                {
                    if (state.Value)
                        await _legionZoneDisabler.DisableAsync();
                    else
                        await _legionZoneDisabler.EnableAsync();
                }
                catch
                {
                    await SnackbarHelper.ShowAsync(
                        state.Value ? Resource.SettingsPage_DisableLegionZone_Error_Title : Resource.SettingsPage_EnableLegionZone_Error_Title,
                        state.Value ? Resource.SettingsPage_DisableLegionZone_Error_Message : Resource.SettingsPage_EnableLegionZone_Error_Message,
                        SnackbarType.Error);
                }
            });
        }
        catch (Exception ex)
        {
                    Log.Instance.Warning($"Unexpected failure while toggling Legion Zone state.", ex);
        }
    }

    private async void FnKeysToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _fnKeysToggle.IsChecked;
        if (state is null)
            return;

        try
        {
            await RunWithToggleDisabledAsync(_fnKeysToggle, async () =>
            {
                try
                {
                    if (state.Value)
                        await _fnKeysDisabler.DisableAsync();
                    else
                        await _fnKeysDisabler.EnableAsync();
                }
                catch
                {
                    await SnackbarHelper.ShowAsync(
                        state.Value ? Resource.SettingsPage_DisableLenovoHotkeys_Error_Title : Resource.SettingsPage_EnableLenovoHotkeys_Error_Title,
                        state.Value ? Resource.SettingsPage_DisableLenovoHotkeys_Error_Message : Resource.SettingsPage_EnableLenovoHotkeys_Error_Message,
                        SnackbarType.Error);
                    return;
                }

                // Notify other controls about FnKeys status change
                var newFnKeysStatus = await _fnKeysDisabler.GetStatusAsync();
                FnKeysStatusChanged?.Invoke(this, newFnKeysStatus);
            });
        }
        catch (Exception ex)
        {
                    Log.Instance.Warning($"Unexpected failure while toggling FnKeys state.", ex);
        }
    }

    private void HardwareSensorsCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing || !IsLoaded)
            return;

        if (_hardwareSensorsToggle.IsChecked == true)
        {
            var window = new UniversalDeviceToolkit.WPF.Windows.Settings.HardwareSensorSectionsWindow
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
            return;
        }

        _hardwareSensorsToggle.IsChecked = !(_hardwareSensorsToggle.IsChecked ?? false);
    }

    private async void HardwareSensorsToggle_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (_isRefreshing || !IsLoaded)
            return;

        var state = _hardwareSensorsToggle.IsChecked;
        if (state is null)
            return;

        try
        {
            if (state.Value && !PawnIOHelper.IsPawnIOInstalled())
            {
                await PawnIOHelper.TryShowPawnIONotFoundDialogAsync();
                _hardwareSensorsToggle.IsChecked = false;
                return;
            }

            await RunWithToggleDisabledAsync(_hardwareSensorsToggle, async () =>
            {
                await _hardwareSensorsFeature.SetStateAsync(
                    state.Value ? HardwareSensorsState.On : HardwareSensorsState.Off);

                if (state.Value)
                {
                    _hardwareSensorSettings.SynchronizeStore();
                }

                _osdCardControl.Visibility = state.Value ? Visibility.Visible : Visibility.Collapsed;

                if (!state.Value)
                {
                    _osdToggle.IsChecked = false;
                }
            });
        }
        catch (Exception ex)
        {
                    Log.Instance.Warning($"Unexpected failure while toggling hardware sensors state.", ex);
        }
    }

    private void OsdSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing || !IsLoaded)
            return;

        OsdSettingsWindow.ShowInstance();
    }

    private void OsdToggle_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (_isRefreshing || !IsLoaded)
            return;

        try
        {
            var state = _osdToggle.IsChecked;
            if (state is null)
                return;

            MessagingCenter.Publish(new OsdChangedMessage(state.Value ? OsdState.Show : OsdState.Hidden));
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"OsdToggle_Click error: {ex.Message}", ex);

            _osdToggle.IsChecked = false;
            _osdSettings.Store.ShowOsd = false;
            _osdSettings.SynchronizeStore();
        }
    }

    private async void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing || !IsLoaded)
            return;

        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "UDT settings backup (*.udtbackup)|*.udtbackup",
                DefaultExt = ".udtbackup",
                FileName = $"udt-settings-{DateTime.Now:yyyyMMdd-HHmmss}.udtbackup"
            };
            if (dialog.ShowDialog() != true)
                return;

            _settingsBackupService.Export(dialog.FileName);
            await SnackbarHelper.ShowAsync(Resource.SettingsPage_SettingsBackup_ExportSuccess_Title, Resource.SettingsPage_SettingsBackup_ExportSuccess_Message);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning("Failed to export settings backup.", ex);
            await SnackbarHelper.ShowAsync(Resource.SettingsPage_SettingsBackup_ExportError_Title, Resource.SettingsPage_SettingsBackup_ExportError_Message, SnackbarType.Error);
        }
    }

    private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing || !IsLoaded)
            return;

        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "UDT settings backup (*.udtbackup)|*.udtbackup",
                DefaultExt = ".udtbackup",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() != true)
                return;

            _settingsBackupService.Import(dialog.FileName);
            await SnackbarHelper.ShowAsync(Resource.SettingsPage_SettingsBackup_ImportSuccess_Title, Resource.SettingsPage_SettingsBackup_ImportSuccess_Message);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning("Failed to import settings backup.", ex);
            await SnackbarHelper.ShowAsync(Resource.SettingsPage_SettingsBackup_ImportError_Title, Resource.SettingsPage_SettingsBackup_ImportError_Message, SnackbarType.Error);
        }
    }
}
}
