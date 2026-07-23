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
