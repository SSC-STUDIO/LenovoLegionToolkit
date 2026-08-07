using Avalonia;
#if WINDOWS
using UniversalDeviceToolkit.Lib.Automation.Optimization;
#endif
using UniversalDeviceToolkit.Avalonia;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AvaloniaStartupFlags.Current = AvaloniaStartupFlags.Parse(args);

#if WINDOWS
        var elevatedWorkerExitCode = WindowsOptimizationElevationBridge
            .TryRunWorkerAsync(args)
            .GetAwaiter()
            .GetResult();
        if (elevatedWorkerExitCode.HasValue)
        {
            Environment.ExitCode = elevatedWorkerExitCode.Value;
            return;
        }
#endif

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>();
        // Software rendering must be decided before the windowing subsystem
        // initializes (UseWin32 captures the options inside UsePlatformDetect);
        // the helper is a no-op on non-Windows platforms.
        AvaloniaRenderingCompatibilityHelper.Configure(builder);
        return builder.UsePlatformDetect().LogToTrace();
    }
}
