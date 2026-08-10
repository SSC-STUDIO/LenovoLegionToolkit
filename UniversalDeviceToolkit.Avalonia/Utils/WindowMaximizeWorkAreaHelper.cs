using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Constrains maximized window size to the monitor work area so custom-chrome windows
/// do not cover the taskbar / MyDockFinder dock / MyFinder menu bar.
/// Full-monitor maximize is often treated as exclusive fullscreen by desktop tools (e.g. MyDockFinder),
/// which hides Dock/Finder and causes wrong layout.
/// </summary>
internal static class WindowMaximizeWorkAreaHelper
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;

    private const int GWLP_WNDPROC = -4;
    private const int WM_NCDESTROY = 0x0082;

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    // Native wndproc subclassing replaces the WPF HwndSource.AddHook mechanism.
    // The delegate is kept alive in _wndProcs for the lifetime of the window.
    private static readonly Dictionary<IntPtr, WndProcDelegate> WndProcs = new();
    private static readonly Dictionary<IntPtr, IntPtr> PreviousProcs = new();
    private static readonly object HookLock = new();

    public static void Attach(Window window)
    {
        if (window is null)
            return;

        void OnOpened(object? sender, EventArgs e)
        {
            window.Opened -= OnOpened;
            try
            {
                var hwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (hwnd == IntPtr.Zero)
                    return;

                HookWndProc(hwnd);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Failed to attach maximize work-area hook.", ex);
            }
        }

        // If already initialized, attach immediately; otherwise wait for Opened.
        if (window.TryGetPlatformHandle() is { } platformHandle && platformHandle.Handle != IntPtr.Zero)
        {
            HookWndProc(platformHandle.Handle);
            return;
        }

        window.Opened += OnOpened;
    }

    private static void HookWndProc(IntPtr hwnd)
    {
        lock (HookLock)
        {
            if (WndProcs.ContainsKey(hwnd))
                return;

            var proc = new WndProcDelegate(WndProcBridge);
            var previous = SetWindowLongPtrW(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(proc));
            if (previous == IntPtr.Zero)
                return;

            WndProcs[hwnd] = proc;
            PreviousProcs[hwnd] = previous;
        }
    }

    private static IntPtr WndProcBridge(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmGetMinMaxInfo && lParam != IntPtr.Zero)
        {
            try
            {
                ApplyWorkAreaMaxBounds(hwnd, lParam);
                // Do not mark handled — default processing still needs the filled MINMAXINFO.
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("WM_GETMINMAXINFO work-area adjustment failed.", ex);
            }
        }

        if (msg == WM_NCDESTROY)
            RestoreWndProc(hwnd);

        return CallPreviousWndProc(hwnd, msg, wParam, lParam);
    }

    private static IntPtr CallPreviousWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        IntPtr previous;
        lock (HookLock)
        {
            if (!PreviousProcs.TryGetValue(hwnd, out previous))
                return IntPtr.Zero;
        }

        return CallWindowProcW(previous, hwnd, (uint)msg, wParam, lParam);
    }

    private static void RestoreWndProc(IntPtr hwnd)
    {
        lock (HookLock)
        {
            if (!PreviousProcs.Remove(hwnd, out var previous))
                return;

            WndProcs.Remove(hwnd);
            SetWindowLongPtrW(hwnd, GWLP_WNDPROC, previous);
        }
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
    private static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);
}
