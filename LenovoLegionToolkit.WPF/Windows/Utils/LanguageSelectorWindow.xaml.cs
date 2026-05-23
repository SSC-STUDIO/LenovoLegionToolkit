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
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;

namespace LenovoLegionToolkit.WPF.Windows.Utils
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

        _languageComboBox.SetItems(languages.OrderBy(LocalizationHelper.LanguageDisplayName, StringComparer.InvariantCultureIgnoreCase),
            defaultLanguage,
            LocalizationHelper.LanguageDisplayName);
    }

    private void LanguageSelectorWindow_OnClosed(object? sender, EventArgs e) => _taskCompletionSource.TrySetResult(null);

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        if (_isInstalling)
            return;

        _languageComboBox.TryGetSelectedItem(out CultureInfo? cultureInfo);

        if (cultureInfo is not null && !await EnsureLanguageInstalledAsync(cultureInfo))
            return;

        _taskCompletionSource.TrySetResult(cultureInfo);
        Close();
    }

    private async Task<bool> EnsureLanguageInstalledAsync(CultureInfo cultureInfo)
    {
        if (_languagePackManager.IsInstalled(cultureInfo))
            return true;

        _isInstalling = true;
        _languageComboBox.IsEnabled = false;
        _okButton.IsEnabled = false;
        _statusText.Text = Resource.SettingsPage_Language_Installing;
        _statusText.Visibility = Visibility.Visible;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await _languagePackManager.InstallAsync(cultureInfo, token: cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to install startup language pack '{cultureInfo.Name}'.", ex);

            _statusText.Text = $"{Resource.SettingsPage_Language_InstallFailed} {FormatExceptionMessage(ex)}";
            _taskCompletionSource.TrySetResult(_fallbackLanguage);
            Close();
            return true;
        }
        finally
        {
            _isInstalling = false;
            _languageComboBox.IsEnabled = true;
            _okButton.IsEnabled = true;
        }
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
