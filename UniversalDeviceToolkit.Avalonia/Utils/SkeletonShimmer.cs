using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Provides the existing attached-property API for per-instance skeleton shimmer animation.
/// </summary>
public static class SkeletonShimmer
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Border, bool>("IsEnabled", typeof(SkeletonShimmer));

    public static readonly AttachedProperty<double> DelaySecondsProperty =
        AvaloniaProperty.RegisterAttached<Border, double>("DelaySeconds", typeof(SkeletonShimmer), -1d);

    static SkeletonShimmer()
    {
        IsEnabledProperty.Changed.AddClassHandler<Border>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(AvaloniaObject obj) => obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(AvaloniaObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static double GetDelaySeconds(AvaloniaObject obj) => obj.GetValue(DelaySecondsProperty);

    public static void SetDelaySeconds(AvaloniaObject obj, double value) => obj.SetValue(DelaySecondsProperty, value);

    public static void RestartSubtree(Visual? root) =>
        RestartSubtree(root, force: false);

    /// <summary>
    /// Restarts shimmer under <paramref name="root"/>. Prefer non-force for hot re-entry
    /// so already-running sweeps keep phase (classic 4.x felt smooth because it did not thrash).
    /// </summary>
    public static void RestartSubtree(Visual? root, bool force)
    {
        if (root is not null)
            SkeletonShimmerCoordinator.Restart(root, force);
    }

    public static void StopSubtree(Visual? root)
    {
        if (root is not null)
            SkeletonShimmerCoordinator.Stop(root);
    }

    /// <param name="force">
    /// When false, borders that already have a running sweep are left alone (avoids Stop/Start jank
    /// on plugin-page re-entry). Theme refresh passes true.
    /// </param>
    internal static void RestartSubtreeCore(Visual root, ref int index, bool force = false)
    {
        if (root is Border border && GetIsEnabled(border) && border.IsLoaded && border.IsVisible)
        {
            var automaticDelay = TimeSpan.FromSeconds(Math.Min(
                index * SkeletonAnimationTokens.StaggerStepSeconds,
                SkeletonAnimationTokens.StaggerMaxSeconds));
            index++;
            SkeletonShimmerBehavior.Start(border, automaticDelay, forceRestart: force);
        }

        foreach (var child in root.GetVisualChildren())
            RestartSubtreeCore(child, ref index, force);
    }

    internal static void StopSubtreeCore(Visual root)
    {
        if (root is Border border && GetIsEnabled(border))
            SkeletonShimmerBehavior.Stop(border);

        foreach (var child in root.GetVisualChildren())
            StopSubtreeCore(child);
    }

    internal static TimeSpan ResolveDuration(Control element)
    {
        try
        {
            if (element.TryFindResource("AnimationDurationShimmer", out var durationValue)
                && durationValue is TimeSpan duration && duration > TimeSpan.Zero)
                return duration;
        }
        catch (InvalidOperationException)
        {
        }

        return TimeSpan.FromSeconds(SkeletonAnimationTokens.DurationSeconds);
    }

    internal static Color ResolveBaseColor(Border border)
    {
        var isLight = Application.Current?.RequestedThemeVariant == ThemeVariant.Light;

        try
        {
            if (border.Background is SolidColorBrush local && local.Color.A > 0)
                return local.Color;

            // Tertiary reads better on light cards; secondary keeps dark-theme contrast.
            if (isLight)
            {
                if (border.TryFindResource("ControlFillColorTertiaryBrush", out var tertiaryValue)
                    && tertiaryValue is SolidColorBrush tertiary && tertiary.Color.A > 0)
                    return tertiary.Color;
                if (border.TryFindResource("ControlFillColorSecondaryBrush", out var secondaryValue)
                    && secondaryValue is SolidColorBrush secondary && secondary.Color.A > 0)
                    return secondary.Color;
            }
            else
            {
                if (border.TryFindResource("ControlFillColorSecondaryBrush", out var secondaryValue2)
                    && secondaryValue2 is SolidColorBrush secondary2 && secondary2.Color.A > 0)
                    return secondary2.Color;
                if (border.TryFindResource("ControlFillColorTertiaryBrush", out var tertiaryValue2)
                    && tertiaryValue2 is SolidColorBrush tertiary2)
                    return tertiary2.Color;
            }
        }
        catch (InvalidOperationException)
        {
        }

        // AVALONIA: removed SystemParameters.HighContrast / SystemColors.ControlColor fallback.
        return isLight
            ? Color.FromRgb(0xE8, 0xE8, 0xE8)
            : Color.FromRgb(0x5A, 0x5A, 0x5A);
    }

    internal static (Color Start, Color Peak) ResolveShimmerOverlayColors(Control element)
    {
        var baseColor = element is Border border
            ? ResolveBaseColor(border)
            : Color.FromRgb(0x80, 0x80, 0x80);
        return ResolveShimmerOverlayColors(baseColor);
    }

    internal static (Color Start, Color Peak) ResolveShimmerOverlayColors(Color baseColor)
    {
        // Skeleton surfaces can be translucent and are not always tied to the app theme.
        // Contrast against the resolved surface instead of assuming Light means pale and
        // Dark means dark; this keeps custom accent and high-contrast surfaces readable too.
        var isLight = Application.Current?.RequestedThemeVariant == ThemeVariant.Light;
        var luminance = (0.2126 * baseColor.R + 0.7152 * baseColor.G + 0.0722 * baseColor.B) / 255.0;

        // Use theme-aware threshold: light theme needs lower threshold for better contrast
        var useDarkOverlay = isLight ? luminance >= 0.50 : luminance >= 0.58;

        if (useDarkOverlay)
        {
            // Dark overlay (for light backgrounds) - enhanced contrast in light mode
            var edgeAlpha = isLight ? (byte)0x28 : (byte)0x1C;
            var peakAlpha = isLight ? (byte)0x58 : (byte)0x46;
            return (Color.FromArgb(edgeAlpha, 0x00, 0x00, 0x00),
                    Color.FromArgb(peakAlpha, 0x00, 0x00, 0x00));
        }
        else
        {
            // Light overlay (for dark backgrounds)
            return (Color.FromArgb(0x1C, 0xFF, 0xFF, 0xFF),
                    Color.FromArgb(0x46, 0xFF, 0xFF, 0xFF));
        }
    }

    internal static LinearGradientBrush CreateShimmerBrush(Color baseColor)
    {
        var (start, peak) = ResolveShimmerOverlayColors(baseColor);
        return CreateShimmerBrush(baseColor, start, peak);
    }

    internal static LinearGradientBrush CreateShimmerBrush(Color baseColor, Color shimmerStart, Color shimmerPeak)
    {
        var edge = CompositeOverlay(baseColor, shimmerStart);
        var peak = CompositeOverlay(baseColor, shimmerPeak);

        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            // Sweep offset in relative units (-1.25..1.25); SkeletonShimmerBehavior scales it
            // by the border width each tick (WPF RelativeTransform parity).
            Transform = new TranslateTransform(SkeletonAnimationTokens.SweepFrom, 0)
        };

        // Wide soft-shoulder band (peak @ 0.48): the sweep reads as a smooth wave rather
        // than a narrow stripe, especially on wide card skeletons.
        brush.GradientStops.Add(new GradientStop(baseColor, 0.00));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.14));
        brush.GradientStops.Add(new GradientStop(edge, 0.30));
        brush.GradientStops.Add(new GradientStop(peak, 0.48));
        brush.GradientStops.Add(new GradientStop(edge, 0.66));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.84));
        brush.GradientStops.Add(new GradientStop(baseColor, 1.00));
        return brush;
    }

    internal static Color CompositeOverlay(Color baseColor, Color overlay)
    {
        var alpha = overlay.A / 255.0;
        return alpha <= 0
            ? baseColor
            : Color.FromRgb(
                Lerp(baseColor.R, overlay.R, alpha),
                Lerp(baseColor.G, overlay.G, alpha),
                Lerp(baseColor.B, overlay.B, alpha));
    }

    private static void OnIsEnabledChanged(Border border, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            SkeletonShimmerBehavior.Attach(border);
        else
            SkeletonShimmerBehavior.Detach(border);
    }

    private static byte Lerp(byte from, byte to, double amount) =>
        (byte)Math.Clamp((int)Math.Round(from + ((to - from) * amount)), 0, 255);
}
