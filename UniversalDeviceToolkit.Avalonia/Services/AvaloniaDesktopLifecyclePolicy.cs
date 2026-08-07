namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Resolves a main-window close request before Avalonia tears down the desktop
/// lifetime. Keeping this decision independent from <see cref="App"/> makes
/// the tray and shutdown semantics deterministic and testable.
/// </summary>
internal static class AvaloniaDesktopLifecyclePolicy
{
    internal static MainWindowCloseAction ResolveCloseAction(
        bool isExiting,
        bool minimizeOnClose,
        bool minimizeToTray)
    {
        if (isExiting)
            return MainWindowCloseAction.AllowClose;

        if (minimizeOnClose)
            return MainWindowCloseAction.Minimize;

        return minimizeToTray
            ? MainWindowCloseAction.HideToTray
            : MainWindowCloseAction.ExitApplication;
    }
}

internal enum MainWindowCloseAction
{
    AllowClose,
    Minimize,
    HideToTray,
    ExitApplication,
}
