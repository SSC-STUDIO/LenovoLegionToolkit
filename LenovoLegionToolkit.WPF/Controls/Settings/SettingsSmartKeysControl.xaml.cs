using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Windows.Settings;

namespace LenovoLegionToolkit.WPF.Controls.Settings;

public partial class SettingsSmartKeysControl
{
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly FnKeysDisabler _fnKeysDisabler = IoCContainer.Resolve<FnKeysDisabler>();
    private bool _isRefreshing;

    public SettingsSmartKeysControl()
    {
        InitializeComponent();
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        var loadingTask = Task.Delay(TimeSpan.FromMilliseconds(500));

        _smartFnLockComboBox.SetItems([ModifierKey.None, ModifierKey.Alt, ModifierKey.Alt | ModifierKey.Ctrl | ModifierKey.Shift],
            _settings.Store.SmartFnLockFlags,
            m => m is ModifierKey.None ? Resource.Off : m.GetFlagsDisplayName(ModifierKey.None));

        var fnKeysStatus = await _fnKeysDisabler.GetStatusAsync();
        _smartKeySinglePressActionCard.Visibility = fnKeysStatus != SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
        _smartKeyDoublePressActionCard.Visibility = fnKeysStatus != SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;

        await loadingTask;

        _smartFnLockComboBox.Visibility = Visibility.Visible;

        _isRefreshing = false;
    }

    public void UpdateVisibilityBasedOnFnKeys(SoftwareStatus fnKeysStatus)
    {
        _smartKeySinglePressActionCard.Visibility = fnKeysStatus != SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
        _smartKeyDoublePressActionCard.Visibility = fnKeysStatus != SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SmartFnLockComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_smartFnLockComboBox.TryGetSelectedItem(out ModifierKey modifierKey))
            return;

        _settings.Store.SmartFnLockFlags = modifierKey;
        _settings.SynchronizeStore();
    }

    private void SmartKeySinglePressActionCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var window = new SelectSmartKeyPipelinesWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void SmartKeyDoublePressActionCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var window = new SelectSmartKeyPipelinesWindow(isDoublePress: true) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }
}
