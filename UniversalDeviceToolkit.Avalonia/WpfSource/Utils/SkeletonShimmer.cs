using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Appearance;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Provides the existing attached-property API for per-instance skeleton shimmer animation.
/// </summary>
public static class SkeletonShimmer
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SkeletonShimmer),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty DelaySecondsProperty =
        DependencyProperty.RegisterAttached(
            "DelaySeconds",
            typeof(double),
            typeof(SkeletonShimmer),
            new PropertyMetadata(-1d));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static double GetDelaySeconds(DependencyObject obj) => (double)obj.GetValue(DelaySecondsProperty);

    public static void SetDelaySeconds(DependencyObject obj, double value) => obj.SetValue(DelaySecondsProperty, value);

    public static void RestartSubtree(DependencyObject? root) =>
        RestartSubtree(root, force: false);

    /// <summary>
    /// Restarts shimmer under <paramref name="root"/>. Prefer non-force for hot re-entry
    /// so already-running sweeps keep phase (classic 4.x felt smooth because it did not thrash).
    /// </summary>
    public static void RestartSubtree(DependencyObject? root, bool force)
    {
        if (root is not null)
            SkeletonShimmerCoordinator.Restart(root, force);
    }

    public static void StopSubtree(DependencyObject? root)
    {
        if (root is not null)
            SkeletonShimmerCoordinator.Stop(root);
    }

    /// <param name="force">
    /// When false, borders that already have a running sweep are left alone (avoids Stop/Start jank
    /// on plugin-page re-entry). Theme refresh passes true.
    /// </param>
    internal static void RestartSubtreeCore(DependencyObject root, ref int index, bool force = false)
    {
        if (root is Border border && GetIsEnabled(border) && border.IsLoaded && border.IsVisible)
        {
            var automaticDelay = TimeSpan.FromSeconds(Math.Min(
                index * SkeletonAnimationTokens.StaggerStepSeconds,
                SkeletonAnimationTokens.StaggerMaxSeconds));
            index++;
            SkeletonShimmerBehavior.Start(border, automaticDelay, forceRestart: force);
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var childIndex = 0; childIndex < count; childIndex++)
            RestartSubtreeCore(VisualTreeHelper.GetChild(root, childIndex), ref index, force);
    }

    internal static void StopSubtreeCore(DependencyObject root)
    {
        if (root is Border border && GetIsEnabled(border))
            SkeletonShimmerBehavior.Stop(border);

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var childIndex = 0; childIndex < count; childIndex++)
            StopSubtreeCore(VisualTreeHelper.GetChild(root, childIndex));
    }

    internal static Duration ResolveDuration(FrameworkElement element)
    {
        try
        {
            if (element.TryFindResource("AnimationDurationShimmer") is Duration duration)
                return duration;
        }
        catch (InvalidOperationException)
        {
        }

        return new Duration(TimeSpan.FromSeconds(SkeletonAnimationTokens.DurationSeconds));
    }

    internal static Color ResolveBaseColor(Border border)
    {
        var isLight = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Light;

        try
        {
            if (border.Background is SolidColorBrush local && local.Color.A > 0)
                return local.Color;

            // Tertiary reads better on light cards; secondary keeps dark-theme contrast.
            if (isLight)
            {
                if (border.TryFindResource("ControlFillColorTertiaryBrush") is SolidColorBrush tertiary && tertiary.Color.A > 0)
                    return tertiary.Color;
                if (border.TryFindResource("ControlFillColorSecondaryBrush") is SolidColorBrush secondary && secondary.Color.A > 0)
                    return secondary.Color;
            }
            else
            {
                if (border.TryFindResource("ControlFillColorSecondaryBrush") is SolidColorBrush secondary && secondary.Color.A > 0)
                    return secondary.Color;
                if (border.TryFindResource("ControlFillColorTertiaryBrush") is SolidColorBrush tertiary)
                    return tertiary.Color;
            }
        }
        catch (InvalidOperationException)
        {
        }

        return SystemParameters.HighContrast
            ? SystemColors.ControlColor
            : isLight
                ? Color.FromRgb(0xE8, 0xE8, 0xE8)
                : Color.FromRgb(0x5A, 0x5A, 0x5A);
    }

    internal static (Color Start, Color Peak) ResolveShimmerOverlayColors(FrameworkElement element)
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
        var isLight = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Light;
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
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            RelativeTransform = new TranslateTransform(SkeletonAnimationTokens.SweepFrom, 0)
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

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border)
            return;

        if (e.NewValue is true)
            SkeletonShimmerBehavior.Attach(border);
        else
            SkeletonShimmerBehavior.Detach(border);
    }

    private static byte Lerp(byte from, byte to, double amount) =>
        (byte)Math.Clamp((int)Math.Round(from + ((to - from) * amount)), 0, 255);
}
