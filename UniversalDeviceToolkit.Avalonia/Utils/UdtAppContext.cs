// App-level helpers for Avalonia (replaces WPF Application.Current.MainWindow / Shutdown / resources).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Styling;

namespace UniversalDeviceToolkit.Avalonia.Utils;

public static class UdtAppContext
{
    public static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public static void SetMainWindow(Window? window)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = window;
    }

    public static void Shutdown() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

    public static IClipboard? Clipboard => MainWindow?.Clipboard;

    /// <summary>Reads an application-level resource for the current theme (null when missing).</summary>
    public static object? GetResource(object key)
    {
        var app = Application.Current;
        if (app is null)
            return null;
        var theme = app.RequestedThemeVariant ?? ThemeVariant.Dark;
        return app.Resources.TryGetResource(key, theme, out var value) ? value : null;
    }
}
