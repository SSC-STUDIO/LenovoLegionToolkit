using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class AboutPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _versionText;

    [ObservableProperty]
    private string _buildText;

    [ObservableProperty]
    private string _copyrightText;

    [ObservableProperty]
    private bool _showTranslationCredit;

    public IReadOnlyList<LibraryInfo> Libraries { get; } = new[]
    {
        new LibraryInfo("AsyncLock", "https://github.com/neosmart/AsyncLock"),
        new LibraryInfo("Autofac", "https://github.com/autofac/Autofac"),
        new LibraryInfo("Avalonia", "https://github.com/AvaloniaUI/Avalonia"),
        new LibraryInfo("Ben.Demystifier", "https://github.com/benaadams/Ben.Demystifier"),
        new LibraryInfo("CommunityToolkit.Mvvm", "https://github.com/CommunityToolkit/dotnet"),
        new LibraryInfo("CsWin32", "https://github.com/microsoft/CsWin32"),
        new LibraryInfo("Humanizer", "https://github.com/Humanizr/Humanizer"),
        new LibraryInfo("ManagedNativeWifi", "https://github.com/emoacht/ManagedNativeWifi"),
        new LibraryInfo("Markdig", "https://github.com/xoofx/markdig"),
        new LibraryInfo("Microsoft.CSharp", "https://github.com/dotnet/runtime"),
        new LibraryInfo("NAudio.Wasapi", "https://github.com/naudio/NAudio"),
        new LibraryInfo("Newtonsoft.Json", "https://github.com/JamesNK/Newtonsoft.Json"),
        new LibraryInfo("Octokit", "https://github.com/octokit/octokit.net"),
        new LibraryInfo("System.Management", "https://github.com/dotnet/runtime"),
        new LibraryInfo("TaskScheduler", "https://github.com/dahall/TaskScheduler"),
        new LibraryInfo("WindowsDisplayAPI", "https://github.com/falahati/WindowsDisplayAPI")
    };

    public AboutPageViewModel()
    {
        var version = GetVersionText();
        VersionText = version ?? string.Empty;

        var build = GetBuildDateTime();
        BuildText = string.IsNullOrWhiteSpace(build) ? string.Empty : $"Build: {build}";

        var copyright = GetCopyrightText();
        CopyrightText = copyright ?? string.Empty;

        // Show translation credit for non-English locales
        ShowTranslationCredit = !System.Globalization.CultureInfo.CurrentUICulture.Equals(new System.Globalization.CultureInfo("en"));
    }

    private static string? GetVersionText()
    {
        var version = typeof(AboutPageViewModel).Assembly.GetName().Version;
        if (version == null)
            return string.Empty;
            
        var informationalVersion = typeof(AboutPageViewModel).Assembly
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var isBeta = informationalVersion?.Contains("beta", StringComparison.OrdinalIgnoreCase) == true
            || informationalVersion?.Contains("-") == true;
        
        if (isBeta)
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision} BETA";
            
        return version.ToString(4);
    }

    private static string? GetBuildDateTime()
    {
        try
        {
            var assemblyLocation = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(assemblyLocation))
                return string.Empty;

            var fileTime = File.GetLastWriteTimeUtc(assemblyLocation);
            return fileTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? GetCopyrightText()
    {
        try
        {
            var location = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(location))
                return string.Empty;

            var info = FileVersionInfo.GetVersionInfo(location);
            return info.LegalCopyright ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    [RelayCommand]
    public void OpenProjectWebsite()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/chenrunsen/UniversalDeviceToolkit",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public void OpenLatestRelease()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/chenrunsen/UniversalDeviceToolkit/releases/latest",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public void OpenAppDataFolder()
    {
        try
        {
            var appData = GetAppDataPath();
            if (string.IsNullOrEmpty(appData) || !Directory.Exists(appData))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = appData,
                UseShellExecute = true
            });
        }
        catch
        {
            // Handle error silently or show notification
        }
    }

    [RelayCommand]
    public void OpenTempFolder()
    {
        try
        {
            var tempPath = GetTempPath();
            if (string.IsNullOrEmpty(tempPath) || !Directory.Exists(tempPath))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Handle error silently or show notification
        }
    }

    private static string GetAppDataPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "UDT");
    }

    private static string GetTempPath()
    {
        var tempPath = Path.GetTempPath();
        return Path.Combine(tempPath, "UDT");
    }

    public ICommand OpenLibraryLinkCommand => new RelayCommand<string>(url =>
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    });
}

public record LibraryInfo(string Name, string Url);
