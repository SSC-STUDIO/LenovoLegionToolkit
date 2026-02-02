using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;

namespace LenovoLegionToolkit.WPF.Controls.Settings;

public partial class SettingsApplicationBehaviorControl
{
    public event EventHandler<SoftwareStatus>? FnKeysStatusChanged;
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly VantageDisabler _vantageDisabler = IoCContainer.Resolve<VantageDisabler>();
    private readonly LegionZoneDisabler _legionZoneDisabler = IoCContainer.Resolve<LegionZoneDisabler>();
    private readonly FnKeysDisabler _fnKeysDisabler = IoCContainer.Resolve<FnKeysDisabler>();
    private readonly RGBKeyboardBacklightController _rgbKeyboardBacklightController = IoCContainer.Resolve<RGBKeyboardBacklightController>();
    private bool _isRefreshing;

    public SettingsApplicationBehaviorControl()
    {
        InitializeComponent();
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        var loadingTask = Task.Delay(TimeSpan.FromMilliseconds(500));

        _autorunComboBox.SetItems(Enum.GetValues<AutorunState>(), Autorun.State, t => t.GetDisplayName());
        _minimizeToTrayToggle.IsChecked = _settings.Store.MinimizeToTray;
        _minimizeOnCloseToggle.IsChecked = _settings.Store.MinimizeOnClose;

        // Only show compatibility warning setting on incompatible devices
        var (isCompatible, _) = await Compatibility.IsCompatibleAsync();
        if (!isCompatible)
        {
            _disableCompatibilityWarningCard.Visibility = Visibility.Visible;
            _disableCompatibilityWarningToggle.IsChecked = _settings.Store.DisableUnsupportedHardwareWarning;
        }
        else
        {
            _disableCompatibilityWarningCard.Visibility = Visibility.Collapsed;
        }

        var mi = await Compatibility.GetMachineInformationAsync();
        var isSupportedLegionMachine = Compatibility.IsSupportedLegionMachine(mi);

        var vantageStatus = await _vantageDisabler.GetStatusAsync();
        _vantageCard.Visibility = isSupportedLegionMachine && vantageStatus != SoftwareStatus.NotFound ? Visibility.Visible : Visibility.Collapsed;
        _vantageToggle.IsChecked = vantageStatus == SoftwareStatus.Disabled;

        var legionZoneStatus = await _legionZoneDisabler.GetStatusAsync();
        _legionZoneCard.Visibility = isSupportedLegionMachine && legionZoneStatus != SoftwareStatus.NotFound ? Visibility.Visible : Visibility.Collapsed;
        _legionZoneToggle.IsChecked = legionZoneStatus == SoftwareStatus.Disabled;

        var fnKeysStatus = await _fnKeysDisabler.GetStatusAsync();
        _fnKeysCard.Visibility = isSupportedLegionMachine && fnKeysStatus != SoftwareStatus.NotFound ? Visibility.Visible : Visibility.Collapsed;
        _fnKeysToggle.IsChecked = fnKeysStatus == SoftwareStatus.Disabled;

        await loadingTask;

        _isRefreshing = false;
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

        _vantageToggle.IsEnabled = false;

        var state = _vantageToggle.IsChecked;
        if (state is null)
            return;

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
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't set light control owner or current preset.", ex);
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
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't start Aurora if needed.", ex);
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
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't set light control owner.", ex);
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
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't stop Aurora.", ex);
            }

            try
            {
                await _vantageDisabler.EnableAsync();
            }
            catch
            {
                await SnackbarHelper.ShowAsync(Resource.SettingsPage_EnableVantage_Error_Title, Resource.SettingsPage_EnableVantage_Error_Message, SnackbarType.Error);
                return;
            }
        }

        _vantageToggle.IsEnabled = true;
    }

    private async void LegionZoneToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _legionZoneToggle.IsEnabled = false;

        var state = _legionZoneToggle.IsChecked;
        if (state is null)
            return;

        if (state.Value)
        {
            try
            {
                await _legionZoneDisabler.DisableAsync();
            }
            catch
            {
                await SnackbarHelper.ShowAsync(Resource.SettingsPage_DisableLegionZone_Error_Title, Resource.SettingsPage_DisableLegionZone_Error_Message, SnackbarType.Error);
                return;
            }
        }
        else
        {
            try
            {
                await _legionZoneDisabler.EnableAsync();
            }
            catch
            {
                await SnackbarHelper.ShowAsync(Resource.SettingsPage_EnableLegionZone_Error_Title, Resource.SettingsPage_EnableLegionZone_Error_Message, SnackbarType.Error);
                return;
            }
        }

        _legionZoneToggle.IsEnabled = true;
    }

    private async void FnKeysToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _fnKeysToggle.IsEnabled = false;

        var state = _fnKeysToggle.IsChecked;
        if (state is null)
            return;

        if (state.Value)
        {
            try
            {
                await _fnKeysDisabler.DisableAsync();
            }
            catch
            {
                await SnackbarHelper.ShowAsync(Resource.SettingsPage_DisableLenovoHotkeys_Error_Title, Resource.SettingsPage_DisableLenovoHotkeys_Error_Message, SnackbarType.Error);
                return;
            }
        }
        else
        {
            try
            {
                await _fnKeysDisabler.EnableAsync();
            }
            catch
            {
                await SnackbarHelper.ShowAsync(Resource.SettingsPage_EnableLenovoHotkeys_Error_Title, Resource.SettingsPage_EnableLenovoHotkeys_Error_Message, SnackbarType.Error);
                return;
            }
        }

        _fnKeysToggle.IsEnabled = true;

        // Notify other controls about FnKeys status change
        var newFnKeysStatus = await _fnKeysDisabler.GetStatusAsync();
        FnKeysStatusChanged?.Invoke(this, newFnKeysStatus);
    }
}
