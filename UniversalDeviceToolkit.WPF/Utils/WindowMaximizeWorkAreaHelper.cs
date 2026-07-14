using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Constrains maximized window size to the monitor work area so custom-chrome WPF windows
/// do not cover the taskbar / MyDockFinder dock / MyFinder menu bar.
/// Full-monitor maximize is often treated as exclusive fullscreen by desktop tools (e.g. MyDockFinder),
/// which hides Dock/Finder and causes wrong layout.
/// </summary>
internal static class WindowMaximizeWorkAreaHelper
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;

    public static void Attach(Window window)
    {
        if (window is null)
            return;

        void OnSourceInitialized(object? sender, EventArgs e)
        {
            window.SourceInitialized -= OnSourceInitialized;
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                var source = HwndSource.FromHwnd(hwnd);
                source?.AddHook(WndProc);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Failed to attach maximize work-area hook.", ex);
            }
        }

        // If already initialized, attach immediately; otherwise wait.
        if (PresentationSource.FromVisual(window) is HwndSource existing)
        {
            existing.AddHook(WndProc);
            return;
        }

        window.SourceInitialized += OnSourceInitialized;
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmGetMinMaxInfo || lParam == IntPtr.Zero)
            return IntPtr.Zero;

        try
        {
            ApplyWorkAreaMaxBounds(hwnd, lParam);
            // Do not set handled=true — default processing still needs the filled MINMAXINFO.
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("WM_GETMINMAXINFO work-area adjustment failed.", ex);
        }

        return IntPtr.Zero;
    }

    private static void ApplyWorkAreaMaxBounds(IntPtr hwnd, IntPtr lParam)
    {
        var mmi = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
            return;

        var monitorInfo = new MonitorInfo { cbSize = (uint)Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
            return;

        var work = monitorInfo.rcWork;
        var monitorRect = monitorInfo.rcMonitor;

        // Max position is relative to the monitor's top-left.
        mmi.ptMaxPosition.X = Math.Abs(work.Left - monitorRect.Left);
        mmi.ptMaxPosition.Y = Math.Abs(work.Top - monitorRect.Top);
        mmi.ptMaxSize.X = Math.Abs(work.Right - work.Left);
        mmi.ptMaxSize.Y = Math.Abs(work.Bottom - work.Top);

        // Keep max track size aligned so drag-maximize / Aero snap also respects work area.
        mmi.ptMaxTrackSize.X = mmi.ptMaxSize.X;
        mmi.ptMaxTrackSize.Y = mmi.ptMaxSize.Y;

        // Respect app MinWidth/MinHeight if larger than work area (multi-monitor edge cases).
        if (mmi.ptMinTrackSize.X > mmi.ptMaxTrackSize.X)
            mmi.ptMaxTrackSize.X = mmi.ptMinTrackSize.X;
        if (mmi.ptMinTrackSize.Y > mmi.ptMaxTrackSize.Y)
            mmi.ptMaxTrackSize.Y = mmi.ptMinTrackSize.Y;

        Marshal.StructureToPtr(mmi, lParam, fDeleteOld: true);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);
}
