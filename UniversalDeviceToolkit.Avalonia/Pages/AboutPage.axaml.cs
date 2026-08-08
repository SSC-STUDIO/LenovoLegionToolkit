using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Shared.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class AboutPage : UserControl
{
    private static string VersionText
    {
        get
        {
            // Use the current assembly to get the version, not Assembly.GetEntryAssembly()
            var version = typeof(AboutPage).Assembly.GetName().Version;
            if (version is null)
                return string.Empty;

            var informational = typeof(AboutPage).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var isBeta = informational?.Contains("beta", StringComparison.OrdinalIgnoreCase) == true
                || informational?.Contains("-", StringComparison.Ordinal) == true;
            if (isBeta)
                return $"{version.ToString(4)} BETA";
            return version.ToString(4);
        }
    }

    private static string BuildText
    {
        get
        {
            var location = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(location))
                return string.Empty;
            return File.GetLastWriteTimeUtc(location)
                .ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture);
        }
    }

    private static string CopyrightText
    {
        get
        {
            var location = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(location))
                return string.Empty;
            return FileVersionInfo.GetVersionInfo(location).LegalCopyright ?? string.Empty;
        }
    }

    public AboutPage()
    {
        InitializeComponent();

        _version.Text = $"{Get("AboutPage_Version", "Version")} {VersionText}".Trim();
        var build = BuildText;
        if (string.IsNullOrWhiteSpace(build))
        {
            _build.IsVisible = false;
        }
        else
        {
            _build.Text = $"{Get("AboutPage_Build", "Build")} {build}".Trim();
            _build.IsVisible = true;
        }

        _copyright.Text = CopyrightText;

        _translationCredit.IsVisible = !LocalizationCatalog.NormalizeCulture(
            LocalizationRuntime.CurrentCulture)
            .Name.Equals("en", StringComparison.OrdinalIgnoreCase);
    }

    private void OpenApplicationDataFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(Folders.AppData))
            return;

        using var process = Process.Start("explorer", Folders.AppData);
    }

    private void OpenApplicationTempFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(Folders.Temp))
            return;

        using var process = Process.Start("explorer", Folders.Temp);
    }

    private void OpenProjectWebsite_Click(object? sender, RoutedEventArgs e) =>
        OpenUrl(AppIdentity.RepositoryUrl);

    private void OpenLatestRelease_Click(object? sender, RoutedEventArgs e) =>
        OpenUrl($"{AppIdentity.RepositoryUrl}/releases/latest");

    private void OpenLibraryLink_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url })
            OpenUrl(url);
    }

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true,
        });
    }

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);
}
