using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows.Settings;

namespace UniversalDeviceToolkit.Avalonia.Controls.Settings
{
public partial class SettingsSmartKeysControl : global::Avalonia.Controls.UserControl
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

        _smartFnLockComboBox.SetItems([ModifierKey.None, ModifierKey.Alt, ModifierKey.Alt | ModifierKey.Ctrl | ModifierKey.Shift],
            _settings.Store.SmartFnLockFlags,
            m => m is ModifierKey.None ? Resource.Off : m.GetFlagsDisplayName(ModifierKey.None));

        var fnKeysStatus = await _fnKeysDisabler.GetStatusAsync();
        _smartKeySinglePressActionCard.IsVisible = fnKeysStatus != SoftwareStatus.Enabled ? true : false;
        _smartKeyDoublePressActionCard.IsVisible = fnKeysStatus != SoftwareStatus.Enabled ? true : false;

        _isRefreshing = false;
    }

    public void UpdateVisibilityBasedOnFnKeys(SoftwareStatus fnKeysStatus)
    {
        _smartKeySinglePressActionCard.IsVisible = fnKeysStatus != SoftwareStatus.Enabled ? true : false;
        _smartKeyDoublePressActionCard.IsVisible = fnKeysStatus != SoftwareStatus.Enabled ? true : false;
    }

    private void SmartFnLockComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is null)
            return;

        var window = new SelectSmartKeyPipelinesWindow();
        window.ShowDialog(owner);
    }

    private void SmartKeyDoublePressActionCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is null)
            return;

        var window = new SelectSmartKeyPipelinesWindow(isDoublePress: true);
        window.ShowDialog(owner);
    }
}
}
