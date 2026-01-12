using System;
using System.Globalization;
using System.Linq;
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

namespace LenovoLegionToolkit.WPF.Controls.Settings;

public partial class SettingsAppearanceControl
{
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly ThemeManager _themeManager = IoCContainer.Resolve<ThemeManager>();
    private bool _isRefreshing;

    public SettingsAppearanceControl()
    {
        InitializeComponent();
        _themeManager.ThemeApplied += ThemeManager_ThemeApplied;
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        // Reduced delay for faster initial display
        var loadingTask = Task.Delay(TimeSpan.FromMilliseconds(100));

        var languages = LocalizationHelper.Languages.OrderBy(LocalizationHelper.LanguageDisplayName, StringComparer.InvariantCultureIgnoreCase).ToArray();
        var language = await LocalizationHelper.GetLanguageAsync();
        if (languages.Length > 1)
        {
            _langComboBox.SetItems(languages, language, LocalizationHelper.LanguageDisplayName);
            _langComboBox.Visibility = Visibility.Visible;
        }
        else
        {
            _langCardControl.Visibility = Visibility.Collapsed;
        }

        _temperatureComboBox.SetItems(Enum.GetValues<TemperatureUnit>(), _settings.Store.TemperatureUnit, t => t switch
        {
            TemperatureUnit.C => Resource.Celsius,
            TemperatureUnit.F => Resource.Fahrenheit,
            _ => new ArgumentOutOfRangeException(nameof(t))
        });
        _themeComboBox.SetItems(Enum.GetValues<Theme>(), _settings.Store.Theme, t => t.GetDisplayName());

        UpdateAccentColorPicker();
        _accentColorSourceComboBox.SetItems(Enum.GetValues<AccentColorSource>(), _settings.Store.AccentColorSource, t => t.GetDisplayName());

        // Show controls immediately
        _temperatureComboBox.Visibility = Visibility.Visible;
        _themeComboBox.Visibility = Visibility.Visible;

        await loadingTask;

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

        await LocalizationHelper.SetLanguageAsync(cultureInfo);
        App.Current.RestartMainWindow();
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
}
