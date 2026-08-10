using System;
using System.Diagnostics;
using System.Windows.Forms;
using UniversalDeviceToolkit.Lib.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace UniversalDeviceToolkit.Avalonia.Utils;

public static class FullscreenHelper
{
    public static unsafe bool IsAnyApplicationFullscreen()
    {
        try
        {
            var desktopWindowHandle = PInvoke.GetDesktopWindow();
            var shellWindowHandle = PInvoke.GetShellWindow();

            var foregroundWindowHandle = PInvoke.GetForegroundWindow();
            if (foregroundWindowHandle == HWND.Null)
                return false;
            if (foregroundWindowHandle == desktopWindowHandle)
                return false;
            if (foregroundWindowHandle == shellWindowHandle)
                return false;

            if (!PInvoke.GetWindowRect(foregroundWindowHandle, out var appBounds))
                return false;

            // Exclusive fullscreen covers the full monitor Bounds (not just WorkingArea).
            // Work-area maximize (taskbar / MyDockFinder dock reserved) must NOT count as FS.
            var screen = Screen.FromHandle(foregroundWindowHandle);
            var screenBounds = screen.Bounds;
            var coversFullScreen =
                appBounds.bottom - appBounds.top == screenBounds.Height
                && appBounds.right - appBounds.left == screenBounds.Width;
            if (!coversFullScreen)
                return false;

            var processId = 0u;
            _ = PInvoke.GetWindowThreadProcessId(foregroundWindowHandle, &processId);
            var process = Process.GetProcessById((int)processId);
            return process.ProcessName != "explorer";
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't check if application is full screen.", ex);

            return false;
        }
    }
}
