using Avalonia;
#if WINDOWS
using UniversalDeviceToolkit.Lib.Automation.Optimization;
#endif
using UniversalDeviceToolkit.Avalonia;

namespace UniversalDeviceToolkit.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
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
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
