using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using Wpf.Ui.Appearance;

namespace LenovoLegionToolkit.WPF.Utils;

internal static class RenderingCompatibilityHelper
{
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

    public static BackgroundType GetPreferredBackgroundType(ApplicationSettings? settings = null)
    {
        if (ShouldDisableBackdrop(settings))
            return BackgroundType.None;

        return settings?.Store.WindowBackdropStyle == WindowBackdropStyle.macOS
            ? BackgroundType.Acrylic
            : BackgroundType.Mica;
    }

    public static bool ShouldDisableBackdrop(ApplicationSettings? settings = null)
        => ShouldForceSoftwareRendering(settings);

    public static void ApplyOpaqueWindowFallback(Window window, ApplicationSettings? settings = null)
    {
        if (ShouldForceSoftwareRendering(settings))
        {
            window.SetResourceReference(Window.BackgroundProperty, "ApplicationBackgroundBrush");
            return;
        }

        window.ClearValue(Window.BackgroundProperty);
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
            Wpf.Ui.Appearance.Background.RestoreContentBackground(window);
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

        WindowChrome.SetWindowChrome(window,
            new WindowChrome
            {
                CaptionHeight = 1,
                CornerRadius = new CornerRadius(4),
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
