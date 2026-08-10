using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows;

namespace UniversalDeviceToolkit.Avalonia.Controls.Settings
{
public partial class SettingsUpdateControl : global::Avalonia.Controls.UserControl
{
    private readonly UpdateChecker _updateChecker = IoCContainer.Resolve<UpdateChecker>();
    private readonly UpdateCheckSettings _updateCheckSettings = IoCContainer.Resolve<UpdateCheckSettings>();
    private readonly DebounceDispatcher _repositorySettingsDebouncer = new();
    private bool _isRefreshing;

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public SettingsUpdateControl()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        IsVisible = true;

        _isRefreshing = true;

        _updateCheckFrequencyComboBox.SetItems(Enum.GetValues<UpdateCheckFrequency>(), _updateCheckSettings.Store.UpdateCheckFrequency, t => t.GetDisplayName());

        // Load update repository settings only when update checking is enabled
        _updateRepositoryOwnerTextBox.Text = _updateCheckSettings.Store.UpdateRepositoryOwner ?? Constants.UpdateRepositoryOwner;
        _updateRepositoryNameTextBox.Text = _updateCheckSettings.Store.UpdateRepositoryName ?? Constants.UpdateRepositoryName;

        var isDisabledByStartupFlag = _updateChecker.Disable;
        var disabledReason = _updateChecker.DisableReason ?? Flags.DisableUpdateCheckerSwitch;

        _updateDisabledInfoBar.Title = T("SettingsPage_UpdateDisabled_Title", "Update checks are disabled for this session");
        _updateDisabledInfoBar.Message = string.Format(
            T("SettingsPage_UpdateDisabled_Message",
                "Universal Device Toolkit was started with {0}. Automatic and manual update checks are disabled until you relaunch without that startup argument."),
            disabledReason);
        _updateDisabledInfoBar.IsOpen = isDisabledByStartupFlag;
        _updateDisabledInfoBar.IsVisible = isDisabledByStartupFlag ? true : false;

        _checkUpdatesCard.IsEnabled = !isDisabledByStartupFlag;
        _updateCheckFrequencyCard.IsEnabled = !isDisabledByStartupFlag;
        _updateChannelCard.IsEnabled = !isDisabledByStartupFlag;
        _updateRepositoryCard.IsEnabled = !isDisabledByStartupFlag;

        _includePrereleaseUpdatesToggle.IsChecked = _updateCheckSettings.Store.IncludePrereleaseUpdates;

        _isRefreshing = false;
    }

    private void IncludePrereleaseUpdatesToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _updateCheckSettings.Store.IncludePrereleaseUpdates = _includePrereleaseUpdatesToggle.IsChecked is true;
        _updateCheckSettings.SynchronizeStore();
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isRefreshing)
                return;

            // AVALONIA: App has no MainWindow property; resolve it via UdtAppContext.
            if (UdtAppContext.MainWindow is not MainWindow mainWindow)
                return;

            await mainWindow.CheckForUpdates(true);
            await SnackbarHelper.ShowAsync(Resource.SettingsPage_CheckUpdates_Started_Title, type: SnackbarType.Info);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(CheckUpdates_Click)}.", ex);
        }
    }

    private void UpdateCheckFrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_updateCheckFrequencyComboBox.TryGetSelectedItem(out UpdateCheckFrequency frequency))
            return;

        _updateCheckSettings.Store.UpdateCheckFrequency = frequency;
        _updateCheckSettings.SynchronizeStore();
        _updateChecker.UpdateMinimumTimeSpanForRefresh();
    }

    private void UpdateRepositoryOwnerTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _repositorySettingsDebouncer.Debounce(400, PersistRepositorySettings);
    }

    private void UpdateRepositoryNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _repositorySettingsDebouncer.Debounce(400, PersistRepositorySettings);
    }

    private void PersistRepositorySettings()
    {
        var ownerText = _updateRepositoryOwnerTextBox.Text?.Trim();
        _updateCheckSettings.Store.UpdateRepositoryOwner = string.IsNullOrWhiteSpace(ownerText) ? null : ownerText;

        var nameText = _updateRepositoryNameTextBox.Text?.Trim();
        _updateCheckSettings.Store.UpdateRepositoryName = string.IsNullOrWhiteSpace(nameText) ? null : nameText;

        _updateCheckSettings.SynchronizeStore();
    }
}
}
