using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Windows;

namespace LenovoLegionToolkit.WPF.Controls.Settings
{
public partial class SettingsUpdateControl
{
    private readonly UpdateChecker _updateChecker = IoCContainer.Resolve<UpdateChecker>();
    private readonly UpdateCheckSettings _updateCheckSettings = IoCContainer.Resolve<UpdateCheckSettings>();
    private bool _isRefreshing;

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public SettingsUpdateControl()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        Visibility = Visibility.Visible;

        _isRefreshing = true;

        _updateCheckFrequencyComboBox.SetItems(Enum.GetValues<UpdateCheckFrequency>(), _updateCheckSettings.Store.UpdateCheckFrequency, t => t.GetDisplayName());

        // Load update repository settings only when update checking is enabled
        _updateRepositoryOwnerTextBox.Text = _updateCheckSettings.Store.UpdateRepositoryOwner ?? Constants.UpdateRepositoryOwner;
        _updateRepositoryNameTextBox.Text = _updateCheckSettings.Store.UpdateRepositoryName ?? string.Empty;

        var isDisabledByStartupFlag = _updateChecker.Disable;
        var disabledReason = _updateChecker.DisableReason ?? Flags.DisableUpdateCheckerSwitch;

        _updateDisabledInfoBar.Title = T("SettingsPage_UpdateDisabled_Title", "Update checks are disabled for this session");
        _updateDisabledInfoBar.Message = string.Format(
            T("SettingsPage_UpdateDisabled_Message",
                "Lenovo Legion Toolkit was started with {0}. Automatic and manual update checks are disabled until you relaunch without that startup argument."),
            disabledReason);
        _updateDisabledInfoBar.IsOpen = isDisabledByStartupFlag;
        _updateDisabledInfoBar.Visibility = isDisabledByStartupFlag ? Visibility.Visible : Visibility.Collapsed;

        _checkUpdatesCard.IsEnabled = !isDisabledByStartupFlag;
        _updateCheckFrequencyCard.IsEnabled = !isDisabledByStartupFlag;
        _updateRepositoryCard.IsEnabled = !isDisabledByStartupFlag;

        _isRefreshing = false;
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (App.Current.MainWindow is not MainWindow mainWindow)
            return;

        await mainWindow.CheckForUpdates(true);
        await SnackbarHelper.ShowAsync(Resource.SettingsPage_CheckUpdates_Started_Title, type: SnackbarType.Info);
    }

    private void UpdateCheckFrequencyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_updateCheckFrequencyComboBox.TryGetSelectedItem(out UpdateCheckFrequency frequency))
            return;

        _updateCheckSettings.Store.UpdateCheckFrequency = frequency;
        _updateCheckSettings.SynchronizeStore();
        _updateChecker.UpdateMinimumTimeSpanForRefresh();
    }

    private void UpdateRepositoryOwnerTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (sender is not Wpf.Ui.Controls.TextBox textBox)
            return;

        var text = textBox.Text?.Trim();
        _updateCheckSettings.Store.UpdateRepositoryOwner = string.IsNullOrWhiteSpace(text) ? null : text;
        _updateCheckSettings.SynchronizeStore();
    }

    private void UpdateRepositoryNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (sender is not Wpf.Ui.Controls.TextBox textBox)
            return;

        var text = textBox.Text?.Trim();
        _updateCheckSettings.Store.UpdateRepositoryName = string.IsNullOrWhiteSpace(text) ? null : text;
        _updateCheckSettings.SynchronizeStore();
    }
}
}
