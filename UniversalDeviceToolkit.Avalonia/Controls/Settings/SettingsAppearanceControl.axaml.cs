using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using LibResource = UniversalDeviceToolkit.Lib.Resources.Resource;

namespace UniversalDeviceToolkit.Avalonia.Controls.Settings
{
public partial class SettingsAppearanceControl : global::Avalonia.Controls.UserControl
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
    private readonly List<global::Avalonia.Controls.Button> _accentSwatchButtons = [];
    private ColorPickerControl? _customAccentColorPicker;

    private const double AccentSwatchButtonSize = 40;
    private const double AccentSwatchDotSize = 36;

    public SettingsAppearanceControl()
    {
        InitializeComponent();
        Loaded += SettingsAppearanceControl_Loaded;
        Unloaded += SettingsAppearanceControl_Unloaded;
        // AVALONIA: no IsVisibleChanged event; observe the IsVisible property instead.
        PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.IsVisibleProperty && e.GetNewValue<bool>())
                SyncLanguageInstallUi();
        };
    }

    private void SettingsAppearanceControl_Loaded(object sender, RoutedEventArgs e)
    {
        _languagePackInstallCoordinator.Changed -= LanguagePackInstallCoordinator_Changed;
        _languagePackInstallCoordinator.Changed += LanguagePackInstallCoordinator_Changed;
        SyncLanguageInstallUi();
    }

    private void SettingsAppearanceControl_Unloaded(object sender, RoutedEventArgs e)
    {
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
        _fontComboBox.SetItems(Enum.GetValues<AppFontStyle>(), _settings.Store.AppFontStyle, GetFontStyleDisplayName);
        _uiScaleComboBox.SetItems(UiScaleSteps, GetCurrentUiScaleStep(), GetUiScaleDisplayName);

        _applyAccentColorToSystemCheckBox.IsChecked = _settings.Store.ApplyAccentColorToSystem;
        _applyAccentColorToThemeCheckBox.IsChecked = _settings.Store.ApplyAccentColorToTheme;
        InitializeAccentColorSwatches();

        // Show controls immediately
        _temperatureComboBox.IsVisible = true;
        UpdateThemeCardSelection();
        _fontComboBox.IsVisible = true;
        _uiScaleComboBox.IsVisible = true;

        var language = await languageTask;
        _currentLanguage = language;
        if (languages.Length > 1)
        {
            _langComboBox.SetItems(languages, language, LocalizationHelper.LanguageDisplayName);
            _langComboBox.IsVisible = true;
        }
        else
        {
            _langCardControl.IsVisible = false;
        }

        if (_languagePackInstallCoordinator.IsActive)
            SyncLanguageInstallUi();
        else
            UpdateLanguagePackButtons();

        _isRefreshing = false;
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
        if (_isRefreshing || sender is not global::Avalonia.Controls.Button { Tag: string tag })
            return;

        if (!Enum.TryParse<UniversalDeviceToolkit.Lib.Theme>(tag, out var theme))
            return;

        _settings.Store.Theme = theme;
        _settings.SynchronizeStore();
        _themeManager.Apply();
        UpdateThemeCardSelection();
    }

    private void UpdateThemeCardSelection()
    {
        var current = _settings.Store.Theme;
        UpdateCardBorder(_themeLightCard, current == UniversalDeviceToolkit.Lib.Theme.Light);
        UpdateCardBorder(_themeDarkCard, current == UniversalDeviceToolkit.Lib.Theme.Dark);
        UpdateCardBorder(_themeSystemCard, current == UniversalDeviceToolkit.Lib.Theme.System);
    }

    private static void UpdateCardBorder(global::Avalonia.Controls.Button? card, bool selected)
    {
        if (card?.Content is not StackPanel sp || sp.Children.Count < 1 || sp.Children[0] is not Border border)
            return;

        border.BorderThickness = selected ? new Thickness(2.5) : new Thickness(1);
        border.BorderBrush = selected
            ? (IBrush)card.FindResource("AccentFillColorDefaultBrush")
            : (IBrush)card.FindResource("CardStrokeColorDefaultBrush");
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

    // Sentinel Tag for the "follow system" swatch (all other preset swatches carry an RGBColor Tag).
    private const string SystemAccentSwatchTag = "System";

    private void InitializeAccentColorSwatches()
    {
        _accentColorSwatchesPanel.Children.Clear();
        _accentSwatchButtons.Clear();
        _customAccentColorPicker = null;

        // The system source is represented by the same multicolor mark used by the reference UI.
        var systemColor = GetSystemAccentColor();
        _accentColorSwatchesPanel.Children.Add(CreateAccentSwatch(
            CreateSystemAccentBrush(),
            SystemAccentSwatchTag,
            LibResource.AccentColorSource_System));

        foreach (var (color, _) in AccentColorPresets.Swatches)
            _accentColorSwatchesPanel.Children.Add(CreateAccentSwatch(
                new SolidColorBrush(color.ToColor()),
                color,
                Resource.SettingsPage_AccentColor_Title));

        var customColor = _settings.Store.AccentColorSource == AccentColorSource.Custom &&
                          _settings.Store.AccentColor is RGBColor storedCustomColor
            ? storedCustomColor.ToColor()
            : systemColor.ToColor();
        var customPicker = new ColorPickerControl
        {
            Margin = new Thickness(0, 0, 2, 8),
            ButtonSize = AccentSwatchButtonSize,
            ButtonContent = new SymbolIcon
            {
                Symbol = SymbolRegular.Eyedropper24,
                FontSize = 21,
                Foreground = Brushes.White,
            },
            ButtonToolTip = LibResource.AccentColorSource_Custom,
            SelectedColor = customColor,
        };
        customPicker.SetValue(AutomationProperties.NameProperty, LibResource.AccentColorSource_Custom);
        customPicker.ColorChangedDelayed += CustomAccentColorPicker_ColorChangedDelayed;
        _customAccentColorPicker = customPicker;
        _accentColorSwatchesPanel.Children.Add(customPicker);

        UpdateAccentColorSwatchSelection();
    }

    private global::Avalonia.Controls.Button CreateAccentSwatch(IBrush fill, object tag, string toolTip)
    {
        var dot = new Border
        {
            Width = AccentSwatchDotSize,
            Height = AccentSwatchDotSize,
            CornerRadius = new CornerRadius(AccentSwatchDotSize / 2),
            Background = fill,
            BorderBrush = GetSwatchStrokeBrush(),
            BorderThickness = new Thickness(1),
        };

        var button = new global::Avalonia.Controls.Button
        {
            Width = AccentSwatchButtonSize,
            Height = AccentSwatchButtonSize,
            Margin = new Thickness(0, 0, 2, 8),
            Padding = new Thickness(0),
            Background = global::Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand),
            Content = dot,
            Tag = tag,
        };
        ToolTip.SetTip(button, toolTip);
        button.SetValue(AutomationProperties.NameProperty, toolTip);
        button.Click += AccentColorSwatch_Click;
        _accentSwatchButtons.Add(button);
        return button;
    }

    private static IBrush CreateSystemAccentBrush() => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Absolute),
        GradientStops =
        {
            new GradientStop(Color.FromRgb(0xF1, 0x3B, 0x50), 0.0),
            new GradientStop(Color.FromRgb(0x74, 0x2A, 0xC4), 0.28),
            new GradientStop(Color.FromRgb(0x1A, 0x98, 0xF2), 0.52),
            new GradientStop(Color.FromRgb(0x06, 0xD3, 0xA5), 0.76),
            new GradientStop(Color.FromRgb(0xFF, 0xD6, 0x2E), 1.0),
        },
    };

    private void AccentColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (sender is not global::Avalonia.Controls.Button { Tag: { } tag })
            return;

        if (tag is string systemTag && systemTag == SystemAccentSwatchTag)
        {
            _settings.Store.AccentColorSource = AccentColorSource.System;
        }
        else if (tag is RGBColor rgbColor)
        {
            _settings.Store.AccentColorSource = AccentColorSource.Custom;
            _settings.Store.AccentColor = rgbColor;
        }
        else
        {
            return;
        }

        _settings.Store.ThemeStylePreset = ThemeStylePreset.Default;
        _settings.SynchronizeStore();
        if (tag is RGBColor selectedColor)
            ApplyAccentColorToSystemIfEnabled(selectedColor);
        _themeManager.Apply();
        UpdateAccentColorSwatchSelection();
    }

    private void CustomAccentColorPicker_ColorChangedDelayed(object? sender, EventArgs e)
    {
        if (_isRefreshing || _customAccentColorPicker is null)
            return;

        var selectedColor = _customAccentColorPicker.SelectedColor.ToRGBColor();
        _settings.Store.AccentColorSource = AccentColorSource.Custom;
        _settings.Store.AccentColor = selectedColor;
        _settings.Store.ThemeStylePreset = ThemeStylePreset.Default;
        _settings.SynchronizeStore();
        ApplyAccentColorToSystemIfEnabled(selectedColor);
        _themeManager.Apply();
        UpdateAccentColorSwatchSelection();
    }

    private RGBColor GetSystemAccentColor()
    {
        try
        {
            return SystemTheme.GetAccentColor();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Couldn't read the Windows accent color; using the default blue swatch.", ex);

            return AccentColorPresets.Swatches[0].Color;
        }
    }

    // AVALONIA: Application is not a StyledElement, so the 1-arg resource helper does
    // not apply; use the IResourceHost overload instead.
    private static IBrush? TryGetResourceBrush(string key)
    {
        var app = Application.Current;
        if (app is not null && app.TryFindResource(key, out var value))
            return value as IBrush;
        return null;
    }

    private IBrush GetAccentSelectionBrush() =>
        TryGetResourceBrush("SystemAccentColorPrimaryBrush")
        ?? Brushes.DodgerBlue;

    private IBrush GetSwatchStrokeBrush() =>
        TryGetResourceBrush("ControlStrokeColorDefaultBrush")
        ?? new SolidColorBrush(Color.FromArgb(90, 90, 90, 90));

    private void UpdateAccentColorSwatchSelection()
    {
        var isSystem = _settings.Store.AccentColorSource == AccentColorSource.System;
        var selectedColor = _settings.Store.AccentColor;
        var selectionBrush = GetAccentSelectionBrush();

        foreach (var button in _accentSwatchButtons)
        {
            if (button.Content is not Border dot)
                continue;

            var selected = button.Tag is string systemTag && systemTag == SystemAccentSwatchTag
                ? isSystem
                : !isSystem && button.Tag is RGBColor preset && selectedColor is RGBColor custom && preset == custom;

            dot.BorderBrush = selected ? selectionBrush : GetSwatchStrokeBrush();
            dot.BorderThickness = selected ? new Thickness(3) : new Thickness(1);
        }

        if (_customAccentColorPicker is not null)
        {
            var customSelected = _settings.Store.AccentColorSource == AccentColorSource.Custom &&
                                 selectedColor is RGBColor custom &&
                                 !AccentColorPresets.Swatches.Any(s => s.Color == custom);
            _customAccentColorPicker.ButtonBorderBrush = customSelected ? (Brush)selectionBrush : (Brush)GetSwatchStrokeBrush();
            _customAccentColorPicker.ButtonBorderThickness = customSelected ? new Thickness(3) : new Thickness(1);
            _customAccentColorPicker.ButtonEffect = customSelected
                ? new DropShadowEffect
                {
                    Color = Color.FromArgb(120, 0, 120, 255),
                    BlurRadius = 12,
                    OffsetX = 0,
                    OffsetY = 0,
                    Opacity = 0.8,
                }
                : null;
        }
    }

    private void ApplyAccentColorToThemeCheckBox_Checked(object sender, RoutedEventArgs e) => OnApplyAccentColorToThemeChanged();

    private void ApplyAccentColorToThemeCheckBox_Unchecked(object sender, RoutedEventArgs e) => OnApplyAccentColorToThemeChanged();

    private void OnApplyAccentColorToThemeChanged()
    {
        if (_isRefreshing)
            return;

        _settings.Store.ApplyAccentColorToTheme = _applyAccentColorToThemeCheckBox.IsChecked == true;
        _settings.SynchronizeStore();
        _themeManager.Apply();
    }

    private void ApplyAccentColorToSystemCheckBox_Checked(object sender, RoutedEventArgs e) => OnApplyAccentColorToSystemChanged();

    private void ApplyAccentColorToSystemCheckBox_Unchecked(object sender, RoutedEventArgs e) => OnApplyAccentColorToSystemChanged();

    private void OnApplyAccentColorToSystemChanged()
    {
        if (_isRefreshing)
            return;

        _settings.Store.ApplyAccentColorToSystem = _applyAccentColorToSystemCheckBox.IsChecked == true;
        _settings.SynchronizeStore();
    }

    private void ApplyAccentColorToSystemIfEnabled(RGBColor? color)
    {
        if (!_settings.Store.ApplyAccentColorToSystem || color is not RGBColor selectedColor)
            return;

        try
        {
            SystemTheme.SetAccentColor(selectedColor);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Couldn't apply the selected accent color to Windows.", ex);
        }
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
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => LanguagePackInstallCoordinator_Changed(sender, e));
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

            if (_languageOperationPanel.IsVisible != true)
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
        _languageOperationPercentText.IsVisible = reportInstallProgress ? true : false;
        _languageOperationPercentText.Text = string.Empty;
        _languageOperationPanel.IsVisible = true;
    }

    private void HideLanguageOperationProgress() => _languageOperationPanel.IsVisible = false;

    private void ReportLanguageOperationProgress(float value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ReportLanguageOperationProgress(value));
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
