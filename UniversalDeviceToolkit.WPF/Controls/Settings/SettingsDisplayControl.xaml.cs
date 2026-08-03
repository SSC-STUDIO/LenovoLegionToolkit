using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.Settings;

namespace UniversalDeviceToolkit.WPF.Controls.Settings
{
public partial class SettingsDisplayControl
{
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly ThemeManager _themeManager = IoCContainer.Resolve<ThemeManager>();
    private readonly FnKeysDisabler _fnKeysDisabler = IoCContainer.Resolve<FnKeysDisabler>();
    private bool _isRefreshing;

    public SettingsDisplayControl()
    {
        InitializeComponent();
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        var fnKeysTask = _fnKeysDisabler.GetStatusAsync();
        var bootLogoTask = BootLogo.IsSupportedAsync();

        _synchronizeBrightnessToAllPowerPlansToggle.IsChecked = _settings.Store.SynchronizeBrightnessToAllPowerPlans;
        _forceSoftwareRenderingToggle.IsChecked = _settings.Store.ForceSoftwareRendering;
        _windowBackdropComboBox.SetItems(
            [WindowBackdropStyle.Windows, WindowBackdropStyle.macOS, WindowBackdropStyle.Off],
            _settings.Store.WindowBackdropStyle,
            GetBackdropDisplayName);

        await Task.WhenAll(fnKeysTask, bootLogoTask);

        var fnKeysStatus = await fnKeysTask;
        _notificationsCard.Visibility = fnKeysStatus != SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
        _excludeRefreshRatesCard.Visibility = fnKeysStatus != SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;

        _bootLogoCard.Visibility = await bootLogoTask ? Visibility.Visible : Visibility.Collapsed;

        _isRefreshing = false;
    }

    public void UpdateVisibilityBasedOnFnKeys(SoftwareStatus fnKeysStatus)
    {
        _notificationsCard.Visibility = fnKeysStatus != SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
        _excludeRefreshRatesCard.Visibility = fnKeysStatus != SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NotificationsCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var window = new NotificationsSettingsWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void ExcludeRefreshRates_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var window = new ExcludeRefreshRatesWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void NavigationItemsSettingsCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var window = new NavigationItemsSettingsWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void SynchronizeBrightnessToAllPowerPlansToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _synchronizeBrightnessToAllPowerPlansToggle.IsChecked;
        if (state is null)
            return;

        _settings.Store.SynchronizeBrightnessToAllPowerPlans = state.Value;
        _settings.SynchronizeStore();
    }

    private void ForceSoftwareRenderingToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _forceSoftwareRenderingToggle.IsChecked;
        if (state is null)
            return;

        _settings.Store.ForceSoftwareRendering = state.Value;
        _settings.SynchronizeStore();
    }

    private void WindowBackdropComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_windowBackdropComboBox.TryGetSelectedItem(out WindowBackdropStyle style))
            return;

        _settings.Store.WindowBackdropStyle = style;
        _settings.SynchronizeStore();
        _themeManager.Apply();
    }

    private static object GetBackdropDisplayName(WindowBackdropStyle style) => style switch
    {
        WindowBackdropStyle.Windows => Resource.SettingsPage_WindowBackdrop_Mica,
        WindowBackdropStyle.macOS => Resource.SettingsPage_WindowBackdrop_Acrylic,
        WindowBackdropStyle.Off => Resource.SettingsPage_WindowBackdrop_Off,
        _ => style.ToString()
    };

    private void BootLogo_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var window = new BootLogoWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }
}
}
