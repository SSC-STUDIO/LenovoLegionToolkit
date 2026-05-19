using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;

namespace LenovoLegionToolkit.WPF.Controls.Settings
{
public partial class SettingsAppearanceControl
{
    private const string CelsiusUnit = "°C";
    private const string FahrenheitUnit = "°F";

    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly ThemeManager _themeManager = IoCContainer.Resolve<ThemeManager>();
    private readonly LanguagePackManager _languagePackManager = IoCContainer.Resolve<LanguagePackManager>();
    private bool _isRefreshing;
    private bool _isLanguagePackOperationInProgress;
    private CultureInfo? _currentLanguage;

    public SettingsAppearanceControl()
    {
        InitializeComponent();
        _themeManager.ThemeApplied += ThemeManager_ThemeApplied;
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        var languages = LocalizationHelper.Languages.OrderBy(LocalizationHelper.LanguageDisplayName, StringComparer.InvariantCultureIgnoreCase).ToArray();
        var languageTask = LocalizationHelper.GetLanguageAsync();

        _temperatureComboBox.SetItems(Enum.GetValues<TemperatureUnit>(), _settings.Store.TemperatureUnit, t => t switch
        {
            TemperatureUnit.C => CelsiusUnit,
            TemperatureUnit.F => FahrenheitUnit,
            _ => new ArgumentOutOfRangeException(nameof(t))
        });
        _themeComboBox.SetItems(Enum.GetValues<Theme>(), _settings.Store.Theme, t => t.GetDisplayName());
        _themeStylePresetComboBox.SetItems(Enum.GetValues<ThemeStylePreset>(), _settings.Store.ThemeStylePreset, t => t.GetDisplayName());

        UpdateAccentColorPicker();
        _accentColorSourceComboBox.SetItems(Enum.GetValues<AccentColorSource>(), _settings.Store.AccentColorSource, t => t.GetDisplayName());

        // Show controls immediately
        _temperatureComboBox.Visibility = Visibility.Visible;
        _themeComboBox.Visibility = Visibility.Visible;
        _themeStylePresetComboBox.Visibility = Visibility.Visible;

        var language = await languageTask;
        _currentLanguage = language;
        if (languages.Length > 1)
        {
            _langComboBox.SetItems(languages, language, LocalizationHelper.LanguageDisplayName);
            _langComboBox.Visibility = Visibility.Visible;
        }
        else
        {
            _langCardControl.Visibility = Visibility.Collapsed;
        }

        UpdateLanguagePackButtons();
        _isRefreshing = false;
    }

    private void ThemeManager_ThemeApplied(object? sender, EventArgs e)
    {
        if (!_isRefreshing)
            UpdateAccentColorPicker();
    }

    private async void LangComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_langComboBox.TryGetSelectedItem(out CultureInfo? cultureInfo) || cultureInfo is null)
            return;

        UpdateLanguagePackButtons();

        if (!_languagePackManager.IsInstalled(cultureInfo))
        {
            await SnackbarHelper.ShowAsync(
                Resource.SettingsPage_Language_NotInstalled_Title,
                Resource.SettingsPage_Language_NotInstalled_Message,
                SnackbarType.Info);
            return;
        }

        await LocalizationHelper.SetLanguageAsync(cultureInfo);
        App.Current.RestartMainWindow();
    }

    private async void InstallLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLanguagePackOperationInProgress)
            return;

        if (!_langComboBox.TryGetSelectedItem(out CultureInfo? cultureInfo) || cultureInfo is null)
            return;

        if (_languagePackManager.IsInstalled(cultureInfo))
            return;

        await RunLanguagePackOperationAsync(
            cultureInfo,
            Resource.SettingsPage_Language_Installing,
            async token =>
            {
                await _languagePackManager.InstallAsync(cultureInfo, token: token);
                await LocalizationHelper.SetLanguageAsync(cultureInfo);
                App.Current.RestartMainWindow();
            },
            Resource.SettingsPage_Language_InstallFailed);
    }

    private async void UninstallLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLanguagePackOperationInProgress)
            return;

        if (!_langComboBox.TryGetSelectedItem(out CultureInfo? cultureInfo) || cultureInfo is null)
            return;

        if (_languagePackManager.IsEnglish(cultureInfo) || !_languagePackManager.IsInstalled(cultureInfo))
            return;

        await RunLanguagePackOperationAsync(
            cultureInfo,
            Resource.SettingsPage_Language_Uninstalling,
            async _ =>
            {
                if (_currentLanguage is not null && _currentLanguage.Name.Equals(cultureInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _languagePackManager.QueueUninstall(cultureInfo);
                    await LocalizationHelper.SetLanguageAsync(new CultureInfo("en"));
                    App.Current.RestartMainWindow();
                    return;
                }

                _languagePackManager.Uninstall(cultureInfo);
                await RefreshAsync();
            },
            Resource.SettingsPage_Language_UninstallFailed);
    }

    private void TemperatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_temperatureComboBox.TryGetSelectedItem(out TemperatureUnit temperatureUnit))
            return;

        _settings.Store.TemperatureUnit = temperatureUnit;
        _settings.SynchronizeStore();
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_themeComboBox.TryGetSelectedItem(out Theme state))
            return;

        _settings.Store.Theme = state;
        _settings.SynchronizeStore();
        _themeManager.Apply();
    }

    private void AccentColorPicker_Changed(object sender, EventArgs e)
    {
        if (_isRefreshing)
            return;

        if (_settings.Store.AccentColorSource != AccentColorSource.Custom)
            return;

        _settings.Store.AccentColor = _accentColorPicker.SelectedColor.ToRGBColor();
        _settings.SynchronizeStore();
        _themeManager.Apply();
    }

    private void ThemeStylePresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_themeStylePresetComboBox.TryGetSelectedItem(out ThemeStylePreset state))
            return;

        _settings.Store.ThemeStylePreset = state;
        _settings.SynchronizeStore();
        _themeManager.Apply();
    }

    private void AccentColorSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_accentColorSourceComboBox.TryGetSelectedItem(out AccentColorSource state))
            return;

        _settings.Store.AccentColorSource = state;
        _settings.SynchronizeStore();
        UpdateAccentColorPicker();
        _themeManager.Apply();
    }

    private void UpdateAccentColorPicker()
    {
        _accentColorPicker.Visibility = _settings.Store.AccentColorSource == AccentColorSource.Custom ? Visibility.Visible : Visibility.Collapsed;
        _accentColorPicker.SelectedColor = _themeManager.GetAccentColor().ToColor();
    }

    private async Task RunLanguagePackOperationAsync(CultureInfo cultureInfo, string title, Func<CancellationToken, Task> operation, string errorTitle)
    {
        _isLanguagePackOperationInProgress = true;
        SetLanguagePackControlsEnabled(false);

        try
        {
            await SnackbarHelper.ShowAsync(title, LocalizationHelper.LanguageDisplayName(cultureInfo), SnackbarType.Info);
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await operation(cts.Token);
        }
        catch (Exception ex)
        {
            await SnackbarHelper.ShowAsync(errorTitle, ex.Message, SnackbarType.Error);
            RestoreCurrentLanguageSelection();
        }
        finally
        {
            _isLanguagePackOperationInProgress = false;
            UpdateLanguagePackButtons();
        }
    }

    private void UpdateLanguagePackButtons()
    {
        if (!_langComboBox.TryGetSelectedItem(out CultureInfo? cultureInfo) || cultureInfo is null)
        {
            SetLanguagePackControlsEnabled(false);
            return;
        }

        var isEnglish = _languagePackManager.IsEnglish(cultureInfo);
        var isInstalled = _languagePackManager.IsInstalled(cultureInfo);

        _langComboBox.IsEnabled = !_isLanguagePackOperationInProgress;
        _installLanguageButton.IsEnabled = !_isLanguagePackOperationInProgress && !isEnglish && !isInstalled;
        _uninstallLanguageButton.IsEnabled = !_isLanguagePackOperationInProgress && !isEnglish && isInstalled;
    }

    private void SetLanguagePackControlsEnabled(bool isEnabled)
    {
        _langComboBox.IsEnabled = isEnabled;
        _installLanguageButton.IsEnabled = isEnabled;
        _uninstallLanguageButton.IsEnabled = isEnabled;
    }

    private void RestoreCurrentLanguageSelection()
    {
        if (_currentLanguage is null)
            return;

        _isRefreshing = true;
        _langComboBox.SetItems(
            LocalizationHelper.Languages.OrderBy(LocalizationHelper.LanguageDisplayName, StringComparer.InvariantCultureIgnoreCase).ToArray(),
            _currentLanguage,
            LocalizationHelper.LanguageDisplayName);
        _isRefreshing = false;
        UpdateLanguagePackButtons();
    }
}
}
