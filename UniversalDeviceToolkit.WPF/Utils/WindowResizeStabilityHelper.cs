using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Reduces flicker when the user live-resizes a Fluent/Mica window (top/left edge drag).
///
/// Intentionally softer than a full freeze:
/// - Keep DWM maximize/snap/resize transitions enabled (Explorer-like motion).
/// - Do NOT snapshot the UI with BitmapCache (that freezes content and feels stiff).
/// - Only pause Mica/Acrylic during the drag and set SWP_NOCOPYBITS to avoid bit-blit tearing.
/// </summary>
internal static class WindowResizeStabilityHelper
{
    private const int WmEnterSizeMove = 0x0231;
    private const int WmExitSizeMove = 0x0232;
    private const int WmWindowPosChanging = 0x0046;
    private const int SwpNoCopyBits = 0x0100;

    // Per-window state (weak-ish: cleaned on closed).
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Window, ResizeSession> Sessions = new();

    /// <summary>True while the user is in a live move/size loop for this window.</summary>
    public static bool IsLiveResizing(Window window) =>
        window is not null
        && Sessions.TryGetValue(window, out var session)
        && session.IsResizing;

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
                    session.Begin(window, hwnd);
                    break;

                case WmExitSizeMove:
                    session.End(window, hwnd);
                    break;

                case WmWindowPosChanging when session.IsResizing && lParam != IntPtr.Zero:
                    // Skip reusing previous client bits when Top/Left move (top/left edge drag).
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

        private WindowBackdropType? _savedBackdrop;
        private object? _savedBackground;
        private bool _hadSolidBackground;

        public void Begin(Window window, IntPtr hwnd)
        {
            if (IsResizing)
                return;
            IsResizing = true;

            try
            {
                // Pause only expensive backdrop recomposition — keep DWM window transitions on.
                if (window is FluentWindow fluent)
                {
                    _savedBackdrop = fluent.WindowBackdropType;
                    if (fluent.WindowBackdropType is not WindowBackdropType.None)
                        fluent.WindowBackdropType = WindowBackdropType.None;
                }

                // Solid fill while backdrop is off — avoids transparent flash mid-drag.
                _hadSolidBackground = window.ReadLocalValue(Window.BackgroundProperty) != DependencyProperty.UnsetValue;
                if (!_hadSolidBackground)
                {
                    _savedBackground = null;
                    window.SetResourceReference(Window.BackgroundProperty, "ApplicationBackgroundBrush");
                }
                else
                {
                    _savedBackground = window.Background;
                }

                window.UseLayoutRounding = true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Resize-stability begin failed.", ex);
            }
        }

        public void End(Window window, IntPtr hwnd) => RestoreIfNeeded(window, hwnd);

        public void RestoreIfNeeded(Window window, IntPtr? hwnd = null)
        {
            if (!IsResizing)
                return;
            IsResizing = false;

            try
            {
                if (window is FluentWindow fluent && _savedBackdrop is { } backdrop)
                    fluent.WindowBackdropType = backdrop;

                if (!_hadSolidBackground)
                    window.ClearValue(Window.BackgroundProperty);
                else if (_savedBackground is Brush brush)
                    window.Background = brush;

                // One clean pass after live resize (navigation rail, metrics, etc.).
                window.InvalidateVisual();
                if (window.Content is UIElement root)
                    root.InvalidateVisual();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Resize-stability end failed.", ex);
            }
            finally
            {
                _savedBackdrop = null;
                _savedBackground = null;
                _hadSolidBackground = false;
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
