using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Explorer-like live resize stability for Fluent/Mica windows.
///
/// Why Explorer does not jitter:
/// - Native shell content is composed by DWM; client content is not a full WPF layout tree
///   re-measuring every mouse move.
///
/// What we do during edge drag (WM_ENTERSIZEMOVE … WM_EXITSIZEMOVE):
/// 1. <see cref="BitmapCache"/> the window content so WPF does not re-layout the whole tree
///    every pixel (content scales as a snapshot, like a frozen layer).
/// 2. Pause Mica/Acrylic (expensive to recompute each frame).
/// 3. Force <c>SWP_NOCOPYBITS</c> so Win32 does not bit-blit stale client pixels when
///    Top/Left change together (top/left edge resize).
/// 4. Expose <see cref="IsLiveResizing"/> so pages can skip thrashy SizeChanged work.
///
/// On mouse-up we drop the cache, restore backdrop, and invalidate once for a sharp frame.
/// </summary>
internal static class WindowResizeStabilityHelper
{
    private const int WmEnterSizeMove = 0x0231;
    private const int WmExitSizeMove = 0x0232;
    private const int WmWindowPosChanging = 0x0046;
    private const int WmSizing = 0x0214;
    private const int SwpNoCopyBits = 0x0100;

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
                    session.Begin(window);
                    break;

                case WmExitSizeMove:
                    session.End(window);
                    break;

                // Also mark live-resize on first WM_SIZING in case ENTERSIZEMOVE was missed
                // (some shell hooks / multi-monitor edge cases).
                case WmSizing:
                    if (!session.IsResizing)
                        session.Begin(window);
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

        private WindowBackdropType? _savedBackdrop;
        private object? _savedBackground;
        private bool _hadSolidBackground;
        private CacheMode? _savedContentCacheMode;
        private bool _contentCacheApplied;
        private UIElement? _cachedContent;

        public void Begin(Window window)
        {
            if (IsResizing)
                return;
            IsResizing = true;

            try
            {
                window.UseLayoutRounding = true;
                window.SnapsToDevicePixels = true;

                // 1) Snapshot content so the layout tree is not re-measured every mouse move.
                //    Explorer does not reflow a WPF tree; this is the closest client-side equivalent.
                if (window.Content is UIElement content)
                {
                    _cachedContent = content;
                    _savedContentCacheMode = content.CacheMode;
                    content.CacheMode = new BitmapCache
                    {
                        // ClearType over a scaling bitmap looks muddy during drag; restore after.
                        EnableClearType = false,
                        RenderAtScale = 1.0,
                        SnapsToDevicePixels = true,
                    };
                    _contentCacheApplied = true;
                    RenderOptions.SetBitmapScalingMode(content, BitmapScalingMode.LowQuality);
                }

                // 2) Pause expensive backdrop recomposition (Mica/Acrylic).
                if (window is FluentWindow fluent)
                {
                    _savedBackdrop = fluent.WindowBackdropType;
                    if (fluent.WindowBackdropType is not WindowBackdropType.None)
                        fluent.WindowBackdropType = WindowBackdropType.None;
                }

                // 3) Solid fill while backdrop is off — avoids transparent flash mid-drag.
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
                if (_contentCacheApplied && _cachedContent is not null)
                {
                    if (_savedContentCacheMode is null)
                        _cachedContent.ClearValue(UIElement.CacheModeProperty);
                    else
                        _cachedContent.CacheMode = _savedContentCacheMode;

                    RenderOptions.SetBitmapScalingMode(_cachedContent, BitmapScalingMode.Unspecified);
                }

                if (window is FluentWindow fluent && _savedBackdrop is { } backdrop)
                    fluent.WindowBackdropType = backdrop;

                if (!_hadSolidBackground)
                    window.ClearValue(Window.BackgroundProperty);
                else if (_savedBackground is Brush brush)
                    window.Background = brush;

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
            finally
            {
                _savedBackdrop = null;
                _savedBackground = null;
                _hadSolidBackground = false;
                _savedContentCacheMode = null;
                _contentCacheApplied = false;
                _cachedContent = null;
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
