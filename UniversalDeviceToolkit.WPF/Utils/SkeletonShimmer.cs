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
    // Classic 4.x teal-slate overlay; composited over the resolved theme bone fill at runtime.
    private static readonly Color DefaultShimmerStart = Color.FromArgb(0x26, 0x88, 0x91, 0xA0);
    private static readonly Color DefaultShimmerPeak = Color.FromArgb(0x4A, 0x88, 0x91, 0xA0);
    // Light cards need a brighter peak than the bone fill; cool-white keeps the 4.x teal-slate sweep readable.
    private static readonly Color DefaultShimmerStartLight = Color.FromArgb(0x38, 0x88, 0xA8, 0xC0);
    private static readonly Color DefaultShimmerPeakLight = Color.FromArgb(0x90, 0xF0, 0xF6, 0xFC);

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
        var isLight = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Light;
        var startKey = isLight ? "SkeletonShimmerStartColorLight" : "SkeletonShimmerStartColor";
        var peakKey = isLight ? "SkeletonShimmerPeakColorLight" : "SkeletonShimmerPeakColor";

        try
        {
            if (element.TryFindResource(startKey) is Color start && element.TryFindResource(peakKey) is Color peak)
                return (start, peak);
        }
        catch (InvalidOperationException)
        {
        }

        return isLight
            ? (DefaultShimmerStartLight, DefaultShimmerPeakLight)
            : (DefaultShimmerStart, DefaultShimmerPeak);
    }

    internal static LinearGradientBrush CreateShimmerBrush(Color baseColor)
    {
        var isLight = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Light;
        return isLight
            ? CreateShimmerBrush(baseColor, DefaultShimmerStartLight, DefaultShimmerPeakLight)
            : CreateShimmerBrush(baseColor, DefaultShimmerStart, DefaultShimmerPeak);
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