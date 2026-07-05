using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
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
    private readonly TaskCompletionSource<CultureInfo?> _taskCompletionSource = new();
    private readonly CultureInfo _fallbackLanguage = new("en");
    private bool _isInstalling;

    public Task<CultureInfo?> ShouldContinue => _taskCompletionSource.Task;

    public LanguageSelectorWindow(IEnumerable<CultureInfo> languages, CultureInfo defaultLanguage)
        : this(languages, defaultLanguage, CreateDefaultLanguagePackManager())
    {
    }

    public LanguageSelectorWindow(IEnumerable<CultureInfo> languages, CultureInfo defaultLanguage, LanguagePackManager languagePackManager)
    {
        _languagePackManager = languagePackManager;

        InitializeComponent();

        _languageComboBox.SetItems(languages.OrderBy(LocalizationHelper.LanguageDisplayName, StringComparer.OrdinalIgnoreCase),
            defaultLanguage,
            LocalizationHelper.LanguageDisplayName);
    }

    private void LanguageSelectorWindow_OnClosed(object? sender, EventArgs e) => _taskCompletionSource.TrySetResult(null);

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isInstalling)
                return;

            _languageComboBox.TryGetSelectedItem(out CultureInfo? cultureInfo);

            if (cultureInfo is not null && !await EnsureLanguageInstalledAsync(cultureInfo))
                return;

            _taskCompletionSource.TrySetResult(cultureInfo);
            Close();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(OK_Click)}.", ex);
        }
    }

    private async Task<bool> EnsureLanguageInstalledAsync(CultureInfo cultureInfo)
    {
        if (_languagePackManager.IsInstalled(cultureInfo))
            return true;

        _isInstalling = true;
        _languageComboBox.IsEnabled = false;
        _okButton.IsEnabled = false;
        _statusText.Text = Resource.SettingsPage_Language_Installing_Download;
        _statusText.Visibility = Visibility.Visible;
        _installProgressPanel.Visibility = Visibility.Visible;
        _installProgressBar.Value = 0;
        _installProgressBar.IsIndeterminate = true;
        _installPercentText.Text = string.Empty;

        var progress = new Progress<float>(ReportInstallProgress);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await _languagePackManager.InstallAsync(cultureInfo, progress, cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to install startup language pack '{cultureInfo.Name}'.", ex);

            _installProgressPanel.Visibility = Visibility.Collapsed;
            _statusText.Text = $"{Resource.SettingsPage_Language_InstallFailed} {FormatExceptionMessage(ex)}";
            _taskCompletionSource.TrySetResult(_fallbackLanguage);
            Close();
            return true;
        }
        finally
        {
            _installProgressPanel.Visibility = Visibility.Collapsed;
            _isInstalling = false;
            _languageComboBox.IsEnabled = true;
            _okButton.IsEnabled = true;
        }
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

