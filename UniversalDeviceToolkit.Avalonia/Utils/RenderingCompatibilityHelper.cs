using System;
using System.Windows.Forms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

internal static class RenderingCompatibilityHelper
{
    internal readonly record struct BackdropSurfaceOpacities(double Shell, double Content, double Card);

    /// <summary>
    /// Avalonia has no per-window render-mode switch like WPF's <c>HwndTarget.RenderMode</c>;
    /// software rendering is configured at startup via the Win32 platform options. The enum
    /// keeps the startup decision API shape (see StartupOrchestrator).
    /// </summary>
    public enum RenderMode
    {
        Default,
        SoftwareOnly
    }

    public static RenderMode GetPreferredRenderMode(ApplicationSettings? settings = null)
    {
        try
        {
            if (ShouldForceSoftwareRendering(settings))
                return RenderMode.SoftwareOnly;

            // AVALONIA: removed WPF RenderCapability.Tier check — Avalonia has no render-tier API.
            return RenderMode.Default;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Falling back to software rendering.", ex);

            return RenderMode.SoftwareOnly;
        }
    }

    public static WindowTransparencyLevel GetPreferredBackgroundType(ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings))
            return WindowTransparencyLevel.None;

        return settings?.Store.WindowBackdropStyle switch
        {
            WindowBackdropStyle.macOS => WindowTransparencyLevel.AcrylicBlur,
            WindowBackdropStyle.Off => WindowTransparencyLevel.None,
            _ => WindowTransparencyLevel.Mica
        };
    }

    public static bool ShouldDisableBackdrop(ApplicationSettings? settings = null)
        => ShouldForceSoftwareRendering(settings) || settings?.Store.WindowBackdropStyle == WindowBackdropStyle.Off;

    public static bool IsBackdropActive(ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings))
            return false;

        var backdropType = GetPreferredBackgroundType(settings);
        return backdropType != WindowTransparencyLevel.None && IsPlatformBackdropSupported(backdropType);
    }

    public static BackdropSurfaceOpacities GetBackdropSurfaceOpacities(ApplicationSettings? settings = null)
        => GetBackdropSurfaceOpacities(settings?.Store.WindowBackdropStyle ?? WindowBackdropStyle.Windows,
            IsBackdropActive(settings));

    internal static BackdropSurfaceOpacities GetBackdropSurfaceOpacities(WindowBackdropStyle style, bool isBackdropActive)
    {
        if (!isBackdropActive)
            return new(1.0, 1.0, 1.0);

        // The native material belongs to shell chrome only. The page surface stays opaque
        // so content remains stable while switching applications or window states.
        return style == WindowBackdropStyle.macOS
            ? new(0.08, 1.0, 1.0)
            : new(0.18, 1.0, 1.0);
    }

    public static void ApplyBackdrop(Window window, WindowTransparencyLevel backdropType, ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings) || backdropType == WindowTransparencyLevel.None)
        {
            window.TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
            ApplyOpaqueWindowFallback(window, settings);
            return;
        }

        try
        {
            if (!IsPlatformBackdropSupported(backdropType))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Window backdrop type {backdropType} is not supported on this platform.");

                ApplyOpaqueWindowFallback(window, settings);
                return;
            }

            // Avalonia's Win32 platform applies the DWM backdrop natively from
            // TransparencyLevelHint (DWMWA_SYSTEMBACKDROP_TYPE on Windows 11). The client
            // surface must stay transparent so the material shows through the window chrome.
            window.TransparencyLevelHint = new[] { backdropType };
            window.Background = Brushes.Transparent;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to apply window backdrop type {backdropType}.", ex);
        }
    }

    public static void ApplyOpaqueWindowFallback(Window window, ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings) ||
            !IsPlatformBackdropSupported(GetPreferredBackgroundType(settings)))
        {
            window.Background = GetApplicationBackgroundBrush();
            window.TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
            return;
        }

        // Keep the client surface transparent while Mica/Acrylic is active so the
        // DWM backdrop remains visible behind the window chrome.
        window.Background = Brushes.Transparent;
    }

    public static void ApplyWindowRenderingCompatibility(Window window, IntPtr hwnd, ApplicationSettings? settings = null)
    {
        if (!ShouldForceSoftwareRendering(settings))
            return;

        try
        {
            // AVALONIA: removed HwndTarget.RenderMode = SoftwareOnly — Avalonia exposes no
            // per-window software render mode; compatibility is configured at startup.
            // Keep the chrome + opaque-surface fallbacks so content stays readable on
            // remote sessions and software-rendered configurations.
            ApplyCompatibleWindowChrome(window);
            window.TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
            window.Background = GetApplicationBackgroundBrush();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to apply window rendering compatibility fallback.", ex);
        }
    }

    public static void ApplyCompatibleWindowChrome(Window window)
    {
        window.SystemDecorations = SystemDecorations.None;
        window.ExtendClientAreaToDecorationsHint = false;

        // AVALONIA: removed WPF WindowChrome (CaptionHeight/GlassFrame/ResizeBorder) — Avalonia
        // draws client chrome natively; DWM corner radius preference is not exposed and OS defaults are used.
    }

    public static bool ShouldForceSoftwareRendering(ApplicationSettings? settings = null)
    {
        try
        {
            if (settings?.Store.ForceSoftwareRendering == true)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Window backdrop disabled because software rendering is forced.");

                return true;
            }

            if (SystemInformation.TerminalServerSession)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Remote desktop session detected. Window backdrop disabled.");

                return true;
            }

            if (RemoteSessionHelper.IsRemoteSession)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Remote session detected via RemoteSessionHelper. Enabling software rendering.");

                return true;
            }

            var screens = Screen.AllScreens;
            if (screens == null || screens.Length == 0)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("No active displays detected. Enabling compatibility rendering.");

                return true;
            }

            var primaryBounds = Screen.PrimaryScreen?.Bounds;
            if (primaryBounds is not { Width: > 0, Height: > 0 })
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Primary display bounds invalid. Enabling compatibility rendering.");

                return true;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to determine window-backdrop compatibility; disabling backdrop.", ex);

            return true;
        }

        return false;
    }

    private static bool IsPlatformBackdropSupported(WindowTransparencyLevel type)
    {
        // AVALONIA: WindowTransparencyLevel is a struct with static properties, so the
        // constants cannot be used in switch patterns; compare with equality instead.
        if (type == WindowTransparencyLevel.Mica || type == WindowTransparencyLevel.AcrylicBlur)
            return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
        if (type == WindowTransparencyLevel.Blur)
            return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240);
        return false;
    }

    private static IBrush GetApplicationBackgroundBrush()
    {
        if (Application.Current?.TryFindResource("ApplicationBackgroundBrush", out var value) == true
            && value is IBrush brush)
            return brush;

        return new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));
    }
}
