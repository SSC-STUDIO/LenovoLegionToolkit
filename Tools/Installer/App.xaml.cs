using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace UniversalDeviceToolkit.Installer;

public sealed class InstallerArguments
{
    public bool Uninstall { get; private set; }
    public bool Silent { get; private set; }
    public bool DeleteAppData { get; private set; }
    public bool DesktopShortcut { get; private set; }
    public string? InstallDir { get; private set; }

    public static InstallerArguments Parse(string[] args)
    {
        var result = new InstallerArguments();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--uninstall":
                case "/uninstall":
                    result.Uninstall = true;
                    break;
                case "/silent":
                case "--silent":
                case "/verysilent":
                    result.Silent = true;
                    break;
                case "/delete-data":
                case "--delete-data":
                    result.DeleteAppData = true;
                    break;
                case "/desktop":
                case "--desktop":
                    result.DesktopShortcut = true;
                    break;
                default:
                    if (arg.StartsWith("/dir=", StringComparison.OrdinalIgnoreCase) ||
                        arg.StartsWith("--dir=", StringComparison.OrdinalIgnoreCase))
                    {
                        result.InstallDir = arg[(arg.IndexOf('=') + 1)..].Trim('"');
                    }
                    else if ((arg.Equals("/dir", StringComparison.OrdinalIgnoreCase) ||
                              arg.Equals("--dir", StringComparison.OrdinalIgnoreCase)) &&
                             i + 1 < args.Length)
                    {
                        result.InstallDir = args[++i].Trim('"');
                    }
                    break;
            }
        }

        return result;
    }
}

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        InstallerLog.Enable();
        ApplySystemTheme();
        var args = InstallerArguments.Parse(e.Args);

        if (args.Silent)
        {
            var exitCode = RunSilent(args);
            Shutdown(exitCode);
            return;
        }

        var window = new MainWindow(args);
        window.Show();
    }

    /// <summary>
    /// Follows the Windows app color mode: resources default to the dark palette
    /// (main app's default); when AppsUseLightTheme=1 they are overwritten with the
    /// light palette before any window is created, so StaticResource lookups resolve
    /// to the themed values.
    /// </summary>
    private static void ApplySystemTheme()
    {
        var light = false;
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            light = key?.GetValue("AppsUseLightTheme") is int value && value != 0;
        }
        catch
        {
            // Keep the dark default.
        }

        if (!light)
            return;

        void Set(string name, string color) =>
            Current.Resources[name] = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));

        Set("TextPrimaryBrush", "#1B1B1B");
        Set("TextSecondaryBrush", "#5D5D5D");
        Set("SurfaceBrush", "#FFFFFF");
        Set("PageBrush", "#F9F9F9");
        Set("BorderBrush", "#E0E0E0");
        Set("WindowBorderBrush", "#D0D0D0");
        Set("TitleBarBrush", "#F3F3F3");
        Set("TitleBarTextBrush", "#1B1B1B");
        Set("CaptionForegroundBrush", "#1B1B1B");
        Set("CaptionHoverBrush", "#1A000000");
        Set("CaptionPressedBrush", "#0D000000");
        Set("SecondaryButtonBrush", "#FFFFFF");
        Set("SecondaryButtonHoverBrush", "#F0F0F0");
        Set("SecondaryButtonBorderBrush", "#D0D0D0");
        Set("InputBrush", "#FFFFFF");
        Set("InputBorderBrush", "#D0D0D0");
        Set("ProgressTrackBrush", "#E6E6E6");
        Set("NoticeInfoBrush", "#EFF6FC");
        Set("NoticeInfoBorderBrush", "#B4D6FA");
        Set("NoticeInfoTextBrush", "#1B4B7A");
        Set("NoticeWarnBrush", "#FFF4CE");
        Set("NoticeWarnBorderBrush", "#F2C94C");
        Set("NoticeWarnTextBrush", "#5D4A00");
    }

    private static int RunSilent(InstallerArguments args)
    {
        InstallerLog.Enable();
        InstallerLog.Info($"Silent run started (uninstall={args.Uninstall}, dir={args.InstallDir ?? "<default>"}).");
        try
        {
            var progress = new Progress<EngineProgress>(p => InstallerLog.Info(p.Status));
            // Task.Run detaches the engine from the WPF SynchronizationContext:
            // blocking here via GetResult() would otherwise deadlock any engine
            // await that captures the dispatcher context.
            if (args.Uninstall)
            {
                var installDir = args.InstallDir ?? DetectInstallDir();
                Task.Run(() => InstallerEngine.UninstallAsync(
                    new UninstallOptions { InstallDir = installDir, DeleteAppData = args.DeleteAppData },
                    progress, CancellationToken.None)).GetAwaiter().GetResult();
            }
            else
            {
                Task.Run(() => InstallerEngine.InstallAsync(
                    new InstallOptions
                    {
                        InstallDir = args.InstallDir ?? InstallerConstants.DefaultInstallDir,
                        CreateDesktopShortcut = args.DesktopShortcut,
                        LaunchAfterInstall = false,
                    },
                    progress, CancellationToken.None)).GetAwaiter().GetResult();
            }

            InstallerLog.Info("Silent run finished successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            InstallerLog.Error("Silent run failed", ex);
            return 1;
        }
    }

    /// <summary>
    /// Locates the install directory for silent uninstalls: our own registry entry
    /// first, then the legacy Inno entry, then the default location.
    /// </summary>
    private static string DetectInstallDir()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{InstallerConstants.UninstallKeyName}");
            var location = key?.GetValue("InstallLocation") as string;
            if (!string.IsNullOrWhiteSpace(location))
                return location;
        }
        catch
        {
            // Fall through to the default.
        }

        return InstallerConstants.DefaultInstallDir;
    }
}
