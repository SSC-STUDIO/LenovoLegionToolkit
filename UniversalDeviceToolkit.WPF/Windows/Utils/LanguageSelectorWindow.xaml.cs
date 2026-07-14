using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
public partial class LanguageSelectorWindow
{
    private readonly LanguagePackManager _languagePackManager;
    private readonly TaskCompletionSource<LanguageGateOutcome> _taskCompletionSource = new();
    private readonly CultureInfo _fallbackLanguage = new("en");
    private readonly bool _allowOfflineEnglish;
    private CancellationTokenSource? _installCts;
    private bool _isInstalling;
    private CultureInfo? _selectedCulture;

    public Task<LanguageGateOutcome> GateOutcome => _taskCompletionSource.Task;

    /// <summary>Selected culture when outcome is Continue; English when ContinueEnglish.</summary>
    public CultureInfo? SelectedCulture { get; private set; }

    [Obsolete("Use GateOutcome")]
    public Task<CultureInfo?> ShouldContinue => GateOutcome.ContinueWith(task =>
    {
        return task.Result switch
        {
            LanguageGateOutcome.Continue => SelectedCulture,
            LanguageGateOutcome.ContinueEnglish => _fallbackLanguage,
            _ => null
        };
    });

    public LanguageSelectorWindow(IEnumerable<CultureInfo> languages, CultureInfo defaultLanguage)
        : this(languages, defaultLanguage, CreateDefaultLanguagePackManager(), allowOfflineEnglish: false)
    {
    }

    public LanguageSelectorWindow(
        IEnumerable<CultureInfo> languages,
        CultureInfo defaultLanguage,
        LanguagePackManager languagePackManager,
        bool allowOfflineEnglish = false)
    {
        _languagePackManager = languagePackManager;
        _allowOfflineEnglish = allowOfflineEnglish;

        InitializeComponent();

        _languageComboBox.SetItems(languages.OrderBy(LocalizationHelper.LanguageDisplayName, StringComparer.OrdinalIgnoreCase),
            defaultLanguage,
            LocalizationHelper.LanguageDisplayName);

        if (_allowOfflineEnglish)
        {
            _statusText.Text = LocalizationHelper.GetStringOrEnglish(
                Resource.ResourceManager,
                "LanguageSelectorWindow_SafeModeHint",
                "Safe / offline mode: you can continue in English without downloading a language pack.",
                Resource.Culture);
            _statusText.Visibility = Visibility.Visible;
        }
    }

    private void LanguageSelectorWindow_OnClosed(object? sender, EventArgs e)
    {
        CancelInFlightInstall();
        _taskCompletionSource.TrySetResult(LanguageGateOutcome.Exit);
    }

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isInstalling)
                return;

            _languageComboBox.TryGetSelectedItem(out CultureInfo? cultureInfo);
            _selectedCulture = cultureInfo;

            if (cultureInfo is null)
                return;

            if (_allowOfflineEnglish && !_languagePackManager.IsInstalled(cultureInfo) && !_languagePackManager.IsEnglish(cultureInfo))
            {
                Complete(LanguageGateOutcome.ContinueEnglish, _fallbackLanguage);
                return;
            }

            if (!await EnsureLanguageInstalledAsync(cultureInfo))
                return;

            Complete(LanguageGateOutcome.Continue, cultureInfo);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(OK_Click)}.", ex);
        }
    }

    private async void Retry_Click(object sender, RoutedEventArgs e)
    {
        HideFailureActions();
        if (_selectedCulture is not null)
            await EnsureLanguageInstalledAsync(_selectedCulture);
    }

    private void ContinueEnglish_Click(object sender, RoutedEventArgs e)
    {
        CancelInFlightInstall();
        Complete(LanguageGateOutcome.ContinueEnglish, _fallbackLanguage);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        CancelInFlightInstall();
        Complete(LanguageGateOutcome.Exit, null);
    }

    private async Task<bool> EnsureLanguageInstalledAsync(CultureInfo cultureInfo)
    {
        if (_languagePackManager.IsInstalled(cultureInfo))
            return true;

        _isInstalling = true;
        _languageComboBox.IsEnabled = false;
        _okButton.IsEnabled = false;
        HideFailureActions();
        _statusText.Text = Resource.SettingsPage_Language_Installing_Download;
        _statusText.Visibility = Visibility.Visible;
        _installProgressPanel.Visibility = Visibility.Visible;
        _installProgressBar.Value = 0;
        _installProgressBar.IsIndeterminate = true;
        _installPercentText.Text = string.Empty;

        var progress = new Progress<float>(ReportInstallProgress);
        _installCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        try
        {
            await _languagePackManager.InstallAsync(cultureInfo, progress, _installCts.Token);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to install startup language pack '{cultureInfo.Name}'.", ex);

            _installProgressPanel.Visibility = Visibility.Collapsed;
            _statusText.Text = $"{Resource.SettingsPage_Language_InstallFailed} {FormatExceptionMessage(ex)}";
            _statusText.Visibility = Visibility.Visible;
            ShowFailureActions();
            return false;
        }
        finally
        {
            _installProgressPanel.Visibility = Visibility.Collapsed;
            _isInstalling = false;
            _languageComboBox.IsEnabled = true;
            _okButton.IsEnabled = true;
            _installCts?.Dispose();
            _installCts = null;
        }
    }

    private void ShowFailureActions()
    {
        _failureActionsPanel.Visibility = Visibility.Visible;
        _okButton.Visibility = Visibility.Collapsed;
    }

    private void HideFailureActions()
    {
        _failureActionsPanel.Visibility = Visibility.Collapsed;
        _okButton.Visibility = Visibility.Visible;
    }

    private void Complete(LanguageGateOutcome outcome, CultureInfo? culture)
    {
        SelectedCulture = culture;
        if (_taskCompletionSource.TrySetResult(outcome))
            Close();
    }

    private void CancelInFlightInstall()
    {
        try { _installCts?.Cancel(); }
        catch { /* ignore */ }
    }

    private void ReportInstallProgress(float value)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => ReportInstallProgress(value));
            return;
        }

        _installProgressBar.IsIndeterminate = !(value > 0);
        _installProgressBar.Value = value;

        _statusText.Text = value >= 0.85f
            ? Resource.SettingsPage_Language_Installing_Applying
            : Resource.SettingsPage_Language_Installing_Download;

        _installPercentText.Text = value > 0
            ? string.Format(Resource.SettingsPage_Language_Installing_Percent, (int)Math.Round(value * 100))
            : string.Empty;
    }

    private static LanguagePackManager CreateDefaultLanguagePackManager() =>
        new(new OnlineResourceCatalogClient(new HttpClientFactory()));

    private static string FormatExceptionMessage(Exception exception)
    {
        if (exception is LanguagePackException languagePackException)
        {
            return languagePackException.Kind switch
            {
                LanguagePackFailureKind.Cancelled => LocalizationHelper.GetStringOrEnglish(
                    Resource.ResourceManager, "LanguageSelectorWindow_Cancelled", "Download cancelled.", Resource.Culture),
                LanguagePackFailureKind.HashMismatch => LocalizationHelper.GetStringOrEnglish(
                    Resource.ResourceManager, "LanguageSelectorWindow_HashMismatch", "Package integrity check failed.", Resource.Culture),
                LanguagePackFailureKind.CorruptPackage => LocalizationHelper.GetStringOrEnglish(
                    Resource.ResourceManager, "LanguageSelectorWindow_CorruptPackage", "Language package is corrupt.", Resource.Culture),
                _ => languagePackException.Message
            };
        }

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
