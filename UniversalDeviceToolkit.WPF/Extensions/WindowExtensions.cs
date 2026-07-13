using LenovoLegionToolkit.Lib.Utils;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace UniversalDeviceToolkit.WPF.Extensions;

public static class WindowExtensions
{
    private static readonly HWND HWND_TOPMOST = new HWND(-1);

    private const int WM_STYLECHANGING = 0x007D;
    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    /// <summary>
    /// Resolved once via GetProcAddress — avoids EntryPointNotFoundException first-chance spam
    /// on Windows builds that do not export GetWindowBand.
    /// </summary>
    private static readonly GetWindowBandDelegate? GetWindowBandFn = ResolveGetWindowBand();

    private delegate bool GetWindowBandDelegate(IntPtr hWnd, out uint pdwBand);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    private static GetWindowBandDelegate? ResolveGetWindowBand()
    {
        try
        {
            var module = GetModuleHandle("user32.dll");
            if (module == IntPtr.Zero)
                return null;
            var proc = GetProcAddress(module, "GetWindowBand");
            if (proc == IntPtr.Zero)
                return null;
            return Marshal.GetDelegateForFunctionPointer<GetWindowBandDelegate>(proc);
        }
        catch
        {
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STYLESTRUCT
    {
        public int styleOld;
        public int styleNew;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    public static void EscalateZBand(this Window window)
    {
        if (PresentationSource.FromVisual(window) is not HwndSource source)
            return;

        var hwnd = (HWND)source.Handle;
        try
        {
            PInvoke.SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

            // Optional API — resolved via GetProcAddress so missing export never throws.
            if (GetWindowBandFn is { } getBand && getBand(source.Handle, out uint currentBand))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"EscalateZBand executed for {window.GetType().Name}. Current Band: {currentBand}");
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Exception for HWND {hwnd}", ex);
        }
    }

    public static void SetClickThrough(this Window window, bool clickThrough)
    {
        if (PresentationSource.FromVisual(window) is not HwndSource source)
            return;

        var hwnd = source.Handle;
        var extendedStyle = GetExtendedStyle(hwnd);

        extendedStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;

        if (clickThrough)
            extendedStyle |= WS_EX_TRANSPARENT;
        else
            extendedStyle &= ~WS_EX_TRANSPARENT;

        SetExtendedStyle(hwnd, extendedStyle);
    }

    public static void BringToForeground(this Window window)
    {
        window.ShowInTaskbar = true;

        if (window.WindowState == WindowState.Minimized || window.Visibility == Visibility.Hidden)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
    }

    public static IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_STYLECHANGING)
        {
            if (wParam.ToInt32() == GWL_EXSTYLE)
            {
                var styleStruct = Marshal.PtrToStructure<STYLESTRUCT>(lParam);
                styleStruct.styleNew |= unchecked((int)(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
                Marshal.StructureToPtr(styleStruct, lParam, false);
                handled = true;
            }
        }
        else if (msg == WM_WINDOWPOSCHANGING)
        {
            var wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            wp.flags |= SWP_NOACTIVATE;
            Marshal.StructureToPtr(wp, lParam, false);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static int GetExtendedStyle(IntPtr hwnd) =>
        (int)(IntPtr.Size == 8 ? GetWindowLongPtr(hwnd, GWL_EXSTYLE) : GetWindowLong32(hwnd, GWL_EXSTYLE));

    private static void SetExtendedStyle(IntPtr hwnd, int extendedStyle)
    {
        if (IntPtr.Size == 8)
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle));
        else
            SetWindowLong32(hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle));
    }
}
