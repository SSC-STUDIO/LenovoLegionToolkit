using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class RenderingCompatibilityHelper
{
    internal readonly record struct BackdropSurfaceOpacities(double Shell, double Content, double Card);

    public static RenderMode GetPreferredRenderMode(ApplicationSettings? settings = null)
    {
        try
        {
            if (ShouldForceSoftwareRendering(settings))
                return RenderMode.SoftwareOnly;

            var tier = RenderCapability.Tier >> 16;
            return tier >= 2 ? RenderMode.Default : RenderMode.SoftwareOnly;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Falling back to software rendering.", ex);

            return RenderMode.SoftwareOnly;
        }
    }

    public static WindowBackdropType GetPreferredBackgroundType(ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings))
            return WindowBackdropType.None;

        return settings?.Store.WindowBackdropStyle switch
        {
            WindowBackdropStyle.macOS => WindowBackdropType.Acrylic,
            WindowBackdropStyle.Off => WindowBackdropType.None,
            _ => WindowBackdropType.Mica
        };
    }

    public static bool ShouldDisableBackdrop(ApplicationSettings? settings = null)
        => ShouldForceSoftwareRendering(settings) || settings?.Store.WindowBackdropStyle == WindowBackdropStyle.Off;

    public static bool IsBackdropActive(ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings))
            return false;

        var backdropType = GetPreferredBackgroundType(settings);
        return backdropType != WindowBackdropType.None && WindowBackdrop.IsSupported(backdropType);
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

    public static void ApplyBackdrop(Window window, WindowBackdropType backdropType, ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings) || backdropType == WindowBackdropType.None)
        {
            WindowBackdrop.RemoveBackdrop(window);
            return;
        }

        try
        {
            if (!WindowBackdrop.IsSupported(backdropType))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Window backdrop type {backdropType} is not supported on this platform.");

                ApplyOpaqueWindowFallback(window, settings);
                return;
            }

            // DWM renders the backdrop behind the WPF client surface. The
            // WPF-UI helper must first make both the Window and its HwndSource
            // transparent; setting only DWMWA_SYSTEMBACKDROP_TYPE is hidden by
            // the default opaque window background.
            WindowBackdrop.RemoveBackground(window);

            if (!WindowBackdrop.ApplyBackdrop(window, backdropType) && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to apply window backdrop type {backdropType}.");

            if (window.IsLoaded)
                WindowBackdrop.RemoveTitlebarBackground(window);
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
            !WindowBackdrop.IsSupported(GetPreferredBackgroundType(settings)))
        {
            window.SetResourceReference(Window.BackgroundProperty, "ApplicationBackgroundBrush");
            return;
        }

        // Keep the client surface transparent while Mica/Acrylic is active.
        // ClearValue can restore an opaque FluentWindow style background and
        // make a successfully applied DWM backdrop appear to do nothing.
        WindowBackdrop.RemoveBackground(window);
    }

    public static void ApplyWindowRenderingCompatibility(Window window, HwndSource? hwndSource, ApplicationSettings? settings = null)
    {
        if (!ShouldForceSoftwareRendering(settings))
            return;

        try
        {
            if (hwndSource?.CompositionTarget is HwndTarget hwndTarget)
                hwndTarget.RenderMode = RenderMode.SoftwareOnly;

            ApplyCompatibleWindowChrome(window);
            window.SetResourceReference(Window.BackgroundProperty, "ApplicationBackgroundBrush");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to apply window rendering compatibility fallback.", ex);
        }
    }

    public static void ApplyCompatibleWindowChrome(Window window)
    {
        window.WindowStyle = WindowStyle.None;

        // WindowChrome corner is OS chrome (not app card chrome). Keep small for DWM.
        var chromeRadius = System.Windows.Application.Current?.TryFindResource("CornerRadiusProgressBar") is CornerRadius token
            ? token
            : new CornerRadius(3);
        WindowChrome.SetWindowChrome(window,
            new WindowChrome
            {
                CaptionHeight = 1,
                CornerRadius = chromeRadius,
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = window.ResizeMode == ResizeMode.NoResize ? new Thickness(0) : new Thickness(4),
                UseAeroCaptionButtons = false
            });
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
}
