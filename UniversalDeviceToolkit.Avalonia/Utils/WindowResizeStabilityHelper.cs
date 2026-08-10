using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Windows;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Explorer-like live resize stability for Mica/Acrylic windows.
///
/// Why Explorer does not jitter:
/// - Native shell content is composed by DWM; client content is not a full layout tree
///   re-measuring every mouse move.
///
/// What we do during edge drag (WM_ENTERSIZEMOVE … WM_EXITSIZEMOVE):
/// 1. Keep content live. Caching the complete visual tree can leave a blank client
///    area when third-party desktop shells move or restore the window.
/// 2. Keep Mica/Acrylic connected so translucent shell chrome never flashes black.
/// 3. Force <c>SWP_NOCOPYBITS</c> so Win32 does not bit-blit stale client pixels when
///    Top/Left change together (top/left edge resize).
/// 4. Expose <see cref="IsLiveResizing"/> so pages can skip thrashy SizeChanged work.
///
/// On mouse-up we invalidate once for a sharp frame.
/// </summary>
internal static class WindowResizeStabilityHelper
{
    private const int WmEnterSizeMove = 0x0231;
    private const int WmExitSizeMove = 0x0232;
    private const int WmWindowPosChanging = 0x0046;
    private const int SwpNoCopyBits = 0x0100;

    private const int GWLP_WNDPROC = -4;
    private const int WM_NCDESTROY = 0x0082;

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    // Native wndproc subclassing replaces the WPF HwndSource.AddHook mechanism.
    // The delegate is kept alive in _wndProcs for the lifetime of the window.
    private static readonly Dictionary<IntPtr, WndProcDelegate> WndProcs = new();
    private static readonly Dictionary<IntPtr, IntPtr> PreviousProcs = new();
    private static readonly object HookLock = new();

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Window, ResizeSession> Sessions = new();

    /// <summary>True while the user is in a live move/size loop for this window.</summary>
    public static bool IsLiveResizing(Window window) =>
        window is not null
        && Sessions.TryGetValue(window, out var session)
        && session.IsResizing;

    public static void RestoreIfNeeded(Window window)
    {
        if (window is not null && Sessions.TryGetValue(window, out var session))
            session.RestoreIfNeeded(window);
    }

    public static void Attach(Window window)
    {
        if (window is null)
            return;

        void OnOpened(object? sender, EventArgs e)
        {
            window.Opened -= OnOpened;
            TryAddHook(window);
        }

        if (window.TryGetPlatformHandle() is { } platformHandle && platformHandle.Handle != IntPtr.Zero)
            TryAddHook(window);
        else
            window.Opened += OnOpened;

        window.Closed += (_, _) =>
        {
            if (Sessions.TryGetValue(window, out var session))
            {
                session.RestoreIfNeeded(window);
                Sessions.Remove(window);
            }
        };
    }

    private static void TryAddHook(Window window)
    {
        try
        {
            var hwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero)
                return;

            if (!Sessions.TryGetValue(window, out _))
                Sessions.Add(window, new ResizeSession());

            HookWndProc(hwnd, window);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to attach resize-stability hook.", ex);
        }
    }

    private static void HookWndProc(IntPtr hwnd, Window window)
    {
        lock (HookLock)
        {
            if (WndProcs.ContainsKey(hwnd))
                return;

            // The delegate captures the owning window so the session (keyed by Window)
            // can be resolved from the hwnd message stream.
            var proc = new WndProcDelegate((h, m, w, l) => WndProcBridge(window, h, m, w, l));
            var previous = SetWindowLongPtrW(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(proc));
            if (previous == IntPtr.Zero)
                return;

            WndProcs[hwnd] = proc;
            PreviousProcs[hwnd] = previous;
        }
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

    private static IntPtr WndProcBridge(Window window, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NCDESTROY)
            RestoreWndProc(hwnd);

        if (Sessions.TryGetValue(window, out var session))
            HandleMessage(window, session, hwnd, msg, wParam, lParam);

        return CallPreviousWndProc(hwnd, msg, wParam, lParam);
    }

    private static void HandleMessage(Window window, ResizeSession session, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            switch (msg)
            {
                case WmEnterSizeMove:
                    session.Begin(window);
                    break;

                case WmExitSizeMove:
                    session.End(window);
                    break;

                case WmWindowPosChanging when session.IsResizing && lParam != IntPtr.Zero:
                    // Skip reusing previous client bits when Top/Left move (any edge drag).
                    // Prevents tearing/jitter from partial bit-blits during simultaneous move+size.
                    var pos = Marshal.PtrToStructure<WindowPos>(lParam);
                    if ((pos.flags & SwpNoCopyBits) == 0)
                    {
                        pos.flags |= SwpNoCopyBits;
                        Marshal.StructureToPtr(pos, lParam, fDeleteOld: false);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Resize-stability WndProc failed.", ex);
        }
    }

    private sealed class ResizeSession
    {
        public bool IsResizing { get; private set; }

        public void Begin(Window window)
        {
            if (IsResizing)
                return;
            IsResizing = true;

            try
            {
                window.UseLayoutRounding = true;
                // AVALONIA: removed SnapsToDevicePixels — no equivalent property.
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Resize-stability begin failed.", ex);
            }
        }

        public void End(Window window) => RestoreIfNeeded(window);

        public void RestoreIfNeeded(Window window)
        {
            if (!IsResizing)
                return;
            IsResizing = false;

            try
            {
                // One clean, sharp layout pass after the drag ends (nav rail, sensors, etc.).
                if (window.Content is Layoutable root)
                {
                    root.InvalidateVisual();
                    root.InvalidateMeasure();
                    root.InvalidateArrange();
                    // AVALONIA: removed UpdateLayout — invalidations are flushed by the
                    // automatic layout pass on the UI thread.
                }

                // Apply deferred chrome layout (nav rail width, etc.) once, at final size.
                Dispatcher.UIThread.Post(() =>
                {
                    if (window is MainWindow main)
                        main.RefreshChromeAfterLiveResize();
                });
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Resize-stability end failed.", ex);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPos
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
