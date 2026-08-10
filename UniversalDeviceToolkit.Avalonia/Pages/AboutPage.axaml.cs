using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Pages
{
public partial class AboutPage : global::Avalonia.Controls.UserControl
{
    private static string VersionText
    {
        get
        {
            // Use current assembly to get version, not Assembly.GetEntryAssembly()
            var version = typeof(AboutPage).Assembly.GetName().Version;
            if (version is null)
                return string.Empty;
            if (version.IsBeta())
                return $"{version.ToString(4)} BETA";
            return version.ToString(4);
        }
    }

    private static string BuildText => Assembly.GetEntryAssembly()?.GetBuildDateTimeString() ?? string.Empty;

    private static string CopyrightText
    {
        get
        {
            var location = Assembly.GetEntryAssembly()?.Location;
            if (location is null)
                return string.Empty;
            return FileVersionInfo.GetVersionInfo(location).LegalCopyright ?? string.Empty;
        }
    }

    public AboutPage()
    {
        InitializeComponent();

        _version.Text = $"{Resource.AboutPage_Version} {VersionText}".Trim();
        var build = BuildText;
        if (string.IsNullOrWhiteSpace(build))
        {
            _build.IsVisible = false;
        }
        else
        {
            _build.Text = $"{Resource.AboutPage_Build} {build}".Trim();
            _build.IsVisible = true;
        }

        _copyright.Text = CopyrightText;

        _translationCredit.IsVisible = !Resource.Culture.Equals(new CultureInfo("en"));
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
}
}
