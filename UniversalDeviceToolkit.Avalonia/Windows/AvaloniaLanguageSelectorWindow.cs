using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Shared.Logging;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// First-run language gate shown before the main window when no language has
/// been persisted yet. Mirrors the WPF LanguageSelectorWindow flow: list of
/// supported cultures, per-item language-pack download, offline-English
/// fallback, and a Continue button that persists the selection via
/// <see cref="LocalizationRuntime.SetCultureAsync"/>.
/// </summary>
internal sealed class AvaloniaLanguageSelectorWindow : Window
{
    public enum LanguageGateOutcome
    {
        Continue,
        ContinueEnglish,
        Exit,
    }

    private readonly TaskCompletionSource<LanguageGateOutcome> _taskCompletionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IAvaloniaLanguagePackService _languagePackService;
    private readonly ListBox _languageList = new();
    private readonly TextBlock _statusText = new();
    private readonly Button _continueButton;
    private readonly Button _continueEnglishButton;
    private readonly Button _exitButton;
    private CultureInfo? _selectedCulture;
    private bool _isInstalling;

    /// <summary>Completes with the outcome chosen by the user.</summary>
    public Task<LanguageGateOutcome> GateOutcome => _taskCompletionSource.Task;

    public AvaloniaLanguageSelectorWindow(IAvaloniaLanguagePackService? languagePackService = null)
    {
        _languagePackService = languagePackService ?? AvaloniaLanguagePackServiceFactory.Create();

        _continueButton = CreateButton(
            "Continue",
            "Continue",
            "AvaloniaLanguageSelectorContinue",
            Continue_Click);
        _continueEnglishButton = CreateButton(
            "LanguageSelector_ContinueInEnglishButton",
            "Continue in English",
            "AvaloniaLanguageSelectorContinueEnglish",
            ContinueEnglish_Click);
        _continueEnglishButton.IsVisible = false;
        _exitButton = CreateButton(
            "Exit",
            "Exit",
            "AvaloniaLanguageSelectorExit",
            Exit_Click);
        _exitButton.IsVisible = false;

        Title = Get("LanguageSelectorWindow_Title", "Universal Device Toolkit");
        Width = 520;
        Height = 460;
        MinWidth = 480;
        MinHeight = 400;
        MaxWidth = 640;
        MaxHeight = 640;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        CanResize = false;
        AutomationProperties.SetAutomationId(this, "AvaloniaLanguageSelectorWindow");
        AutomationProperties.SetName(this, Title);
        Content = BuildContent();
        Closed += OnClosed;
        _ = LoadLanguagesAsync();
    }

    private Control BuildContent()
    {
        var title = new LocalizedTextBlock
        {
            Text = Get("LanguageSelectorWindow_SelectLanguage", "Select your language"),
            FontWeight = FontWeight.SemiBold,
            FontSize = 18,
            OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = Get("LanguageSelectorWindow_Description", "Choose the language used by Universal Device Toolkit. Languages can be changed later in Settings."),
            Foreground = GetBrush("TextFillColorSecondaryBrush"),
            OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };

        _languageList.SelectionChanged += LanguageList_SelectionChanged;

        _statusText.TextWrapping = TextWrapping.Wrap;
        _statusText.Foreground = GetBrush("StatusWarningBrush");
        _statusText.IsVisible = false;

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        actions.Children.Add(_continueEnglishButton);
        actions.Children.Add(_exitButton);
        actions.Children.Add(_continueButton);

        var content = new StackPanel { Spacing = 14, Margin = new Thickness(28, 24) };
        content.Children.Add(title);
        content.Children.Add(description);
        content.Children.Add(_languageList);
        content.Children.Add(_statusText);
        content.Children.Add(actions);

        return new Border
        {
            Background = GetBrush("AppBackgroundBrush"),
            Padding = new Thickness(0),
            Child = content,
        };
    }

    private void LanguageList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_languageList.SelectedItem is AvaloniaLanguageOption option)
        {
            _selectedCulture = option.Culture;
            _continueEnglishButton.IsVisible = false;
            _exitButton.IsVisible = false;
        }
    }

    private async Task LoadLanguagesAsync()
    {
        try
        {
            var languages = await _languagePackService.GetLanguagesAsync().ConfigureAwait(true);
            var sorted = languages
                .OrderBy(option => option.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            _languageList.ItemsSource = sorted;
            _languageList.SelectedItem = sorted.FirstOrDefault(option =>
                    option.Culture.Name.Equals(LocalizationRuntime.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
                ?? sorted.FirstOrDefault(option => option.IsEnglish)
                ?? sorted.FirstOrDefault();
        }
        catch (Exception ex)
        {
            SharedLog.Warning("Failed to load the language list for the first-run selector.", ex);
            _statusText.Text = Get("LanguageSelectorWindow_CatalogUnavailable", "The language catalog is unavailable. You can continue in English.");
            _statusText.IsVisible = true;
            _continueEnglishButton.IsVisible = true;
        }
    }

    private async void Continue_Click(object? sender, RoutedEventArgs e)
    {
        if (_isInstalling)
            return;

        var culture = _selectedCulture ?? LocalizationRuntime.CurrentCulture;
        if (_languagePackService.IsEnglish(culture) || _languagePackService.IsInstalled(culture))
        {
            await PersistAndCompleteAsync(culture, LanguageGateOutcome.Continue);
            return;
        }

        if (!_languagePackService.IsAvailable)
        {
            // Offline-English fallback: the catalog/host cannot provide the
            // pack, so continue with the built-in English resources.
            await PersistAndCompleteAsync(LocalizationCatalog.DefaultCulture, LanguageGateOutcome.ContinueEnglish);
            return;
        }

        await InstallSelectedAsync(culture);
    }

    private async Task InstallSelectedAsync(CultureInfo culture)
    {
        _isInstalling = true;
        _languageList.IsEnabled = false;
        _continueButton.IsEnabled = false;
        _statusText.Text = Get("SettingsPage_Language_Installing", "Installing language pack...");
        _statusText.IsVisible = true;
        try
        {
            await _languagePackService.InstallAsync(culture).ConfigureAwait(true);
            await PersistAndCompleteAsync(culture, LanguageGateOutcome.Continue);
        }
        catch (Exception ex)
        {
            SharedLog.Warning($"Failed to install startup language pack '{culture.Name}'.", ex);
            _statusText.Text = string.Format(
                Get("SettingsPage_Language_InstallFailed", "Language pack installation failed: {0}"),
                FormatExceptionMessage(ex));
            _statusText.IsVisible = true;
            _continueEnglishButton.IsVisible = true;
            _exitButton.IsVisible = true;
        }
        finally
        {
            _isInstalling = false;
            _languageList.IsEnabled = true;
            _continueButton.IsEnabled = true;
        }
    }

    private async Task PersistAndCompleteAsync(CultureInfo culture, LanguageGateOutcome outcome)
    {
        try
        {
            await LocalizationRuntime.SetCultureAsync(culture, persist: true).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SharedLog.Warning($"Failed to persist the selected language '{culture.Name}'.", ex);
        }

        Complete(outcome);
    }

    private void ContinueEnglish_Click(object? sender, RoutedEventArgs e) =>
        _ = PersistAndCompleteAsync(LocalizationCatalog.DefaultCulture, LanguageGateOutcome.ContinueEnglish);

    private void Exit_Click(object? sender, RoutedEventArgs e) => Complete(LanguageGateOutcome.Exit);

    private void OnClosed(object? sender, EventArgs e)
    {
        _taskCompletionSource.TrySetResult(LanguageGateOutcome.Exit);
    }

    private void Complete(LanguageGateOutcome outcome)
    {
        if (_taskCompletionSource.TrySetResult(outcome))
        {
            Dispatcher.UIThread.Post(Close);
        }
    }

    private static string FormatExceptionMessage(Exception exception)
    {
        if (exception is LanguagePackException languagePackException)
        {
            return languagePackException.Kind switch
            {
                LanguagePackFailureKind.Cancelled => Get("LanguageSelectorWindow_Cancelled", "Download cancelled."),
                LanguagePackFailureKind.HashMismatch => Get("LanguageSelectorWindow_HashMismatch", "Package integrity check failed."),
                LanguagePackFailureKind.CorruptPackage => Get("LanguageSelectorWindow_CorruptPackage", "Language package is corrupt."),
                LanguagePackFailureKind.CatalogUnavailable => Get("LanguageSelectorWindow_CatalogUnavailable", "The language catalog is unavailable."),
                _ => languagePackException.Message,
            };
        }

        if (exception is PlatformNotSupportedException)
            return Get("LanguageSelectorWindow_Unsupported", "Language packs are not supported on this host.");

        if (exception is HttpRequestException || exception.GetBaseException() is HttpRequestException)
            return Get("SettingsPage_Language_DownloadFailed_Message", "Could not download the language pack. Check your network connection, proxy, or TLS settings, then try again.");

        return exception.Message;
    }

    private static Button CreateButton(string key, string fallback, string automationId, EventHandler<RoutedEventArgs> click)
    {
        var label = Get(key, fallback);
        var button = new Button
        {
            Content = label,
            MinWidth = 120,
        };
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, label);
        button.Click += click;
        return button;
    }

    private static IBrush GetBrush(string key)
    {
        if (Application.Current?.TryFindResource(key, out var value) == true && value is IBrush brush)
            return brush;

        return new SolidColorBrush(Colors.Gray);
    }

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);
}
