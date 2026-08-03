using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Explorer-like live resize stability for Fluent/Mica windows.
///
/// Why Explorer does not jitter:
/// - Native shell content is composed by DWM; client content is not a full WPF layout tree
///   re-measuring every mouse move.
///
/// What we do during edge drag (WM_ENTERSIZEMOVE … WM_EXITSIZEMOVE):
/// 1. Keep WPF's content live. Caching the complete visual tree can leave a blank client
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

        void OnSourceInitialized(object? sender, EventArgs e)
        {
            window.SourceInitialized -= OnSourceInitialized;
            TryAddHook(window);
        }

        if (PresentationSource.FromVisual(window) is HwndSource existing)
            TryAddHook(window, existing);
        else
            window.SourceInitialized += OnSourceInitialized;

        window.Closed += (_, _) =>
        {
            if (Sessions.TryGetValue(window, out var session))
            {
                session.RestoreIfNeeded(window);
                Sessions.Remove(window);
            }
        };
    }

    private static void TryAddHook(Window window, HwndSource? source = null)
    {
        try
        {
            source ??= PresentationSource.FromVisual(window) as HwndSource
                       ?? HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            if (source is null)
                return;

            if (!Sessions.TryGetValue(window, out _))
                Sessions.Add(window, new ResizeSession());

            source.AddHook((hwnd, msg, wParam, lParam, ref handled) =>
                WndProc(window, hwnd, msg, wParam, lParam, ref handled));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to attach resize-stability hook.", ex);
        }
    }

    private static IntPtr WndProc(Window window, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!Sessions.TryGetValue(window, out var session))
            return IntPtr.Zero;

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

        return IntPtr.Zero;
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
                window.SnapsToDevicePixels = true;
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
                window.InvalidateVisual();
                if (window.Content is UIElement root)
                {
                    root.InvalidateVisual();
                    root.InvalidateMeasure();
                    root.InvalidateArrange();
                    root.UpdateLayout();
                }

                // Apply deferred chrome layout (nav rail width, etc.) once, at final size.
                window.Dispatcher.BeginInvoke(
                    () =>
                    {
                        if (window is Windows.MainWindow main)
                            main.RefreshChromeAfterLiveResize();
                    },
                    System.Windows.Threading.DispatcherPriority.Loaded);
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
}
