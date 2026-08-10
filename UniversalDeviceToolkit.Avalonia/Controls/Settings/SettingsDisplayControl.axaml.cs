using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows.Settings;

namespace UniversalDeviceToolkit.Avalonia.Controls.Settings
{
public partial class SettingsDisplayControl : global::Avalonia.Controls.UserControl
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
        _notificationsCard.IsVisible = fnKeysStatus != SoftwareStatus.Enabled ? true : false;
        _excludeRefreshRatesCard.IsVisible = fnKeysStatus != SoftwareStatus.Enabled ? true : false;

        _bootLogoCard.IsVisible = await bootLogoTask ? true : false;

        _isRefreshing = false;
    }

    public void UpdateVisibilityBasedOnFnKeys(SoftwareStatus fnKeysStatus)
    {
        _notificationsCard.IsVisible = fnKeysStatus != SoftwareStatus.Enabled ? true : false;
        _excludeRefreshRatesCard.IsVisible = fnKeysStatus != SoftwareStatus.Enabled ? true : false;
    }

    private void NotificationsCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is null)
            return;

        var window = new NotificationsSettingsWindow();
        window.ShowDialog(owner);
    }

    private void ExcludeRefreshRates_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is null)
            return;

        var window = new ExcludeRefreshRatesWindow();
        window.ShowDialog(owner);
    }

    private void NavigationItemsSettingsCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is null)
            return;

        var window = new NavigationItemsSettingsWindow();
        window.ShowDialog(owner);
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

    private void WindowBackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is null)
            return;

        var window = new BootLogoWindow();
        window.ShowDialog(owner);
    }
}
}
