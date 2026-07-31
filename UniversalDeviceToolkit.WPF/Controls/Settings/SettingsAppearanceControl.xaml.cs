using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Controls.Settings
{
public partial class SettingsAppearanceControl
{
    private const string CelsiusUnit = "\u00B0C";
    private const string FahrenheitUnit = "\u00B0F";

    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly ThemeManager _themeManager = IoCContainer.Resolve<ThemeManager>();
    private readonly LanguagePackManager _languagePackManager = IoCContainer.Resolve<LanguagePackManager>();
    private readonly LanguagePackInstallCoordinator _languagePackInstallCoordinator = IoCContainer.Resolve<LanguagePackInstallCoordinator>();
    private bool _isRefreshing;
    private bool _isLanguagePackOperationInProgress;
    private CultureInfo? _currentLanguage;

    public SettingsAppearanceControl()
    {
        InitializeComponent();
        Loaded += SettingsAppearanceControl_Loaded;
        Unloaded += SettingsAppearanceControl_Unloaded;
        IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
                SyncLanguageInstallUi();
        };
    }

    private void SettingsAppearanceControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Control is reused when Settings content is swapped; re-subscribe after Unloaded.
        _themeManager.ThemeApplied -= ThemeManager_ThemeApplied;
        _themeManager.ThemeApplied += ThemeManager_ThemeApplied;
        _languagePackInstallCoordinator.Changed -= LanguagePackInstallCoordinator_Changed;
        _languagePackInstallCoordinator.Changed += LanguagePackInstallCoordinator_Changed;
        SyncLanguageInstallUi();
    }

    private void SettingsAppearanceControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _themeManager.ThemeApplied -= ThemeManager_ThemeApplied;
        _languagePackInstallCoordinator.Changed -= LanguagePackInstallCoordinator_Changed;
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        var languages = LocalizationHelper.Languages.OrderBy(LocalizationHelper.LanguageDisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
        var languageTask = LocalizationHelper.GetLanguageAsync();

        _temperatureComboBox.SetItems(Enum.GetValues<TemperatureUnit>(), _settings.Store.TemperatureUnit, t => t switch
        {
            TemperatureUnit.C => CelsiusUnit,
            TemperatureUnit.F => FahrenheitUnit,
            _ => new ArgumentOutOfRangeException(nameof(t))
        });
        _themeStylePresetComboBox.SetItems(Enum.GetValues<ThemeStylePreset>(), _settings.Store.ThemeStylePreset, t => t.GetDisplayName());
        _fontComboBox.SetItems(Enum.GetValues<AppFontStyle>(), _settings.Store.AppFontStyle, GetFontStyleDisplayName);
        _uiScaleComboBox.SetItems(UiScaleSteps, GetCurrentUiScaleStep(), GetUiScaleDisplayName);

        UpdateAccentColorPicker();
        _accentColorSourceComboBox.SetItems(Enum.GetValues<AccentColorSource>(), _settings.Store.AccentColorSource, t => t.GetDisplayName());

        // Show controls immediately
        _temperatureComboBox.Visibility = Visibility.Visible;
        UpdateThemeCardSelection();
        _themeStylePresetComboBox.Visibility = Visibility.Visible;
        _fontComboBox.Visibility = Visibility.Visible;
        _uiScaleComboBox.Visibility = Visibility.Visible;

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

        if (_languagePackInstallCoordinator.IsActive)
            SyncLanguageInstallUi();
        else
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
        try
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
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(LangComboBox_SelectionChanged)}.", ex);
        }
    }

    private async void InstallLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isLanguagePackOperationInProgress || _languagePackInstallCoordinator.IsActive)
                return;

            if (!_langComboBox.TryGetSelectedItem(out CultureInfo? cultureInfo) || cultureInfo is null)
                return;

            if (_languagePackManager.IsInstalled(cultureInfo))
                return;

            await RunLanguagePackOperationAsync(
                cultureInfo,
                Resource.SettingsPage_Language_Installing,
                async (_, token) =>
                {
                    await _languagePackInstallCoordinator.InstallAsync(cultureInfo, token);
                    _currentLanguage = cultureInfo;
                    await LocalizationHelper.SetLanguageAsync(cultureInfo);
                    UpdateLanguagePackButtons();
                    App.Current.RestartMainWindow();
                },
                Resource.SettingsPage_Language_InstallFailed,
                reportInstallProgress: true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(InstallLanguageButton_Click)}.", ex);
        }
    }

    private async void UninstallLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        try
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
                async (_, _) =>
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
                Resource.SettingsPage_Language_UninstallFailed,
                reportInstallProgress: false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(UninstallLanguageButton_Click)}.", ex);
        }
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

    private void ThemeCard_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing || sender is not System.Windows.Controls.Button { Tag: string tag })
            return;

        if (!Enum.TryParse<Theme>(tag, out var theme))
            return;

        _settings.Store.Theme = theme;
        _settings.SynchronizeStore();
        _themeManager.Apply();
        UpdateThemeCardSelection();
    }

    private void UpdateThemeCardSelection()
    {
        var current = _settings.Store.Theme;
        UpdateCardBorder(_themeLightCard, current == Theme.Light);
        UpdateCardBorder(_themeDarkCard, current == Theme.Dark);
        UpdateCardBorder(_themeAutoCard, current == Theme.Auto);
        UpdateCardBorder(_themeSystemCard, current == Theme.System);
    }

    private static void UpdateCardBorder(System.Windows.Controls.Button? card, bool selected)
    {
        if (card?.Content is not StackPanel sp || sp.Children.Count < 1 || sp.Children[0] is not Border border)
            return;

        border.BorderThickness = selected ? new Thickness(2.5) : new Thickness(1);
        border.BorderBrush = selected
            ? (Brush)card.FindResource("AccentFillColorDefaultBrush")
            : (Brush)card.FindResource("ControlStrokeColorDefaultBrush");
    }

    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_fontComboBox.TryGetSelectedItem(out AppFontStyle state))
            return;

        _settings.Store.AppFontStyle = state;
        _settings.SynchronizeStore();
        AppFontManager.Apply(state);
    }

    // Font stack names are proper nouns; only the "Default" option is localized.
    private static object GetFontStyleDisplayName(AppFontStyle style) => style switch
    {
        AppFontStyle.Default => Resource.SettingsPage_Font_Default,
        AppFontStyle.FluentVariable => "Segoe UI Variable",
        AppFontStyle.YaHeiUI => "Microsoft YaHei UI",
        AppFontStyle.DengXian => "DengXian",
        AppFontStyle.NotoSans => "Noto Sans CJK SC",
        AppFontStyle.SimHei => "SimHei (黑体)",
        AppFontStyle.SimSun => "SimSun (宋体)",
        AppFontStyle.KaiTi => "KaiTi (楷体)",
        _ => style.ToString()
    };

    // 界面缩放 steps drive text size and layout scale together (merged from the
    // former separate 文本大小 / 应用缩放 combos, which users found redundant).
    private static readonly (AppTextSize TextSize, AppScale Scale)[] UiScaleSteps =
    [
        (AppTextSize.Compact, AppScale.Small),
        (AppTextSize.Standard, AppScale.Standard),
        (AppTextSize.Large, AppScale.Large),
        (AppTextSize.ExtraLarge, AppScale.ExtraLarge),
    ];

    private (AppTextSize TextSize, AppScale Scale) GetCurrentUiScaleStep()
    {
        var textSize = _settings.Store.AppTextSize;
        var scale = _settings.Store.AppScale;
        var match = UiScaleSteps.FirstOrDefault(s => s.TextSize == textSize && s.Scale == scale);
        return match == default ? UiScaleSteps[1] : match;
    }

    private void UiScaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_uiScaleComboBox.TryGetSelectedItem(out (AppTextSize TextSize, AppScale Scale) step))
            return;

        _settings.Store.AppTextSize = step.TextSize;
        _settings.Store.AppScale = step.Scale;
        _settings.SynchronizeStore();
        AppTextSizeManager.Apply(step.TextSize);
        AppScaleManager.Apply(step.Scale);
    }

    private static object GetUiScaleDisplayName((AppTextSize TextSize, AppScale Scale) step) =>
        step.Scale == AppScale.Standard ? $"{(int)step.Scale}% ({Resource.SettingsPage_Font_Default})" : $"{(int)step.Scale}%";

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

    private async Task RunLanguagePackOperationAsync(
        CultureInfo cultureInfo,
        string title,
        Func<IProgress<float>?, CancellationToken, Task> operation,
        string errorTitle,
        bool reportInstallProgress)
    {
        _isLanguagePackOperationInProgress = true;
        SetLanguagePackControlsEnabled(false);
        ShowLanguageOperationProgress(title, reportInstallProgress);

        var progress = reportInstallProgress ? new Progress<float>(ReportLanguageOperationProgress) : null;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await operation(progress, cts.Token);
        }
        catch (Exception ex)
        {
            await SnackbarHelper.ShowAsync(errorTitle, FormatExceptionMessage(ex), SnackbarType.Error);
            RestoreCurrentLanguageSelection();
        }
        finally
        {
            if (!_languagePackInstallCoordinator.IsActive)
            {
                HideLanguageOperationProgress();
                _isLanguagePackOperationInProgress = false;
                UpdateLanguagePackButtons();
            }
        }
    }

    private void LanguagePackInstallCoordinator_Changed(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => LanguagePackInstallCoordinator_Changed(sender, e));
            return;
        }

        SyncLanguageInstallUi();
    }

    private void SyncLanguageInstallUi()
    {
        if (_languagePackInstallCoordinator.IsActive)
        {
            _isLanguagePackOperationInProgress = true;
            SetLanguagePackControlsEnabled(false);

            if (_languageOperationPanel.Visibility != Visibility.Visible)
                ShowLanguageOperationProgress(Resource.SettingsPage_Language_Installing, reportInstallProgress: true);

            ReportLanguageOperationProgress(_languagePackInstallCoordinator.Progress);
            return;
        }

        if (_isLanguagePackOperationInProgress)
        {
            HideLanguageOperationProgress();
            _isLanguagePackOperationInProgress = false;
        }

        UpdateLanguagePackButtons();
    }

    private void ShowLanguageOperationProgress(string statusText, bool reportInstallProgress)
    {
        _languageOperationStatusText.Text = statusText;
        _languageOperationProgressBar.Value = 0;
        _languageOperationProgressBar.IsIndeterminate = !reportInstallProgress;
        _languageOperationPercentText.Visibility = reportInstallProgress ? Visibility.Visible : Visibility.Collapsed;
        _languageOperationPercentText.Text = string.Empty;
        _languageOperationPanel.Visibility = Visibility.Visible;
    }

    private void HideLanguageOperationProgress() => _languageOperationPanel.Visibility = Visibility.Collapsed;

    private void ReportLanguageOperationProgress(float value)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ReportLanguageOperationProgress(value));
            return;
        }

        _languageOperationProgressBar.IsIndeterminate = !(value > 0);
        _languageOperationProgressBar.Value = value;

        _languageOperationStatusText.Text = value >= 0.85f
            ? Resource.SettingsPage_Language_Installing_Applying
            : Resource.SettingsPage_Language_Installing_Download;

        if (value > 0)
            _languageOperationPercentText.Text = string.Format(Resource.SettingsPage_Language_Installing_Percent, (int)Math.Round(value * 100));
        else
            _languageOperationPercentText.Text = string.Empty;
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

        var operationInProgress = _isLanguagePackOperationInProgress || _languagePackInstallCoordinator.IsActive;

        _langComboBox.IsEnabled = !operationInProgress;
        _installLanguageButton.IsEnabled = !operationInProgress && !isEnglish && !isInstalled;
        _uninstallLanguageButton.IsEnabled = !operationInProgress && !isEnglish && isInstalled;
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
            LocalizationHelper.Languages.OrderBy(LocalizationHelper.LanguageDisplayName, StringComparer.OrdinalIgnoreCase).ToArray(),
            _currentLanguage,
            LocalizationHelper.LanguageDisplayName);
        _isRefreshing = false;
        UpdateLanguagePackButtons();
    }

    private static string FormatExceptionMessage(Exception exception)
    {
        if (exception is HttpRequestException || exception.GetBaseException() is HttpRequestException)
            return LocalizationHelper.GetStringOrEnglish(
                Resource.ResourceManager,
                "SettingsPage_Language_DownloadFailed_Message",
                "Could not download the language pack. Check your network connection, proxy, or TLS settings, then try again.",
                Resource.Culture);

        var baseException = exception.GetBaseException();
        if (!ReferenceEquals(baseException, exception) && !string.IsNullOrWhiteSpace(baseException.Message))
            return $"{exception.Message} ({baseException.Message})";

        return exception.Message;
    }
}
}
