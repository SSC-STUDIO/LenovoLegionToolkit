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
    // Cool-slate wash; alphas sized so LiftWithinBaseHue produces a visible band on dark + light bones.
    private static readonly Color DefaultShimmerStart = Color.FromArgb(0x2A, 0x8E, 0x99, 0xAA);
    private static readonly Color DefaultShimmerPeak = Color.FromArgb(0x52, 0xA8, 0xB2, 0xC0);
    private static readonly Color DefaultShimmerStartLight = Color.FromArgb(0x1E, 0x70, 0x78, 0x88);
    private static readonly Color DefaultShimmerPeakLight = Color.FromArgb(0x3A, 0x70, 0x78, 0x88);

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

    public static void RestartSubtree(DependencyObject? root)
    {
        if (root is not null)
            SkeletonShimmerCoordinator.Restart(root);
    }

    public static void StopSubtree(DependencyObject? root)
    {
        if (root is not null)
            SkeletonShimmerCoordinator.Stop(root);
    }

    internal static void RestartSubtreeCore(DependencyObject root, ref int index)
    {
        if (root is Border border && GetIsEnabled(border) && border.IsLoaded && border.IsVisible)
        {
            var automaticDelay = TimeSpan.FromSeconds(Math.Min(
                index * SkeletonAnimationTokens.StaggerStepSeconds,
                SkeletonAnimationTokens.StaggerMaxSeconds));
            index++;
            SkeletonShimmerBehavior.Start(border, automaticDelay);
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var childIndex = 0; childIndex < count; childIndex++)
            RestartSubtreeCore(VisualTreeHelper.GetChild(root, childIndex), ref index);
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
        try
        {
            if (border.Background is SolidColorBrush local && local.Color.A > 0)
                return local.Color;
            if (border.TryFindResource("ControlFillColorSecondaryBrush") is SolidColorBrush secondary && secondary.Color.A > 0)
                return secondary.Color;
            if (border.TryFindResource("ControlFillColorTertiaryBrush") is SolidColorBrush tertiary)
                return tertiary.Color;
        }
        catch (InvalidOperationException)
        {
        }

        return SystemParameters.HighContrast ? SystemColors.ControlColor : Color.FromRgb(0x5A, 0x5A, 0x5A);
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
        // Map overlay alpha → lift amount. Floor so pale theme brushes still show a readable band.
        var edgeLift = Math.Max(0.07, shimmerStart.A / 255.0 * 0.55);
        var softLift = Math.Max(0.12, shimmerStart.A / 255.0 * 0.85);
        var peakLift = Math.Max(0.18, shimmerPeak.A / 255.0 * 0.95);

        var edge = LiftWithinBaseHue(baseColor, edgeLift);
        var soft = LiftWithinBaseHue(baseColor, softLift);
        var peak = LiftWithinBaseHue(baseColor, peakLift);
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            RelativeTransform = new TranslateTransform(SkeletonAnimationTokens.SweepFrom, 0)
        };

        // Narrower, cleaner 流光 core so the sweep reads as a single highlight, not a muddy wash.
        brush.GradientStops.Add(new GradientStop(baseColor, 0.00));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.22));
        brush.GradientStops.Add(new GradientStop(edge, 0.36));
        brush.GradientStops.Add(new GradientStop(soft, 0.44));
        brush.GradientStops.Add(new GradientStop(peak, 0.50));
        brush.GradientStops.Add(new GradientStop(soft, 0.56));
        brush.GradientStops.Add(new GradientStop(edge, 0.64));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.78));
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

    private static Color LiftWithinBaseHue(Color baseColor, double amount)
    {
        // Allow enough lift for a visible cool-slate highlight (old 0.08 cap made 流光 nearly invisible).
        amount = Math.Clamp(amount, 0, 0.28);
        return Color.FromRgb(
            Lerp(baseColor.R, byte.MaxValue, amount),
            Lerp(baseColor.G, byte.MaxValue, amount),
            Lerp(baseColor.B, byte.MaxValue, amount));
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

    private static Color WithAlpha(Color color, double scale) =>
        Color.FromArgb((byte)Math.Clamp((int)Math.Round(color.A * Math.Clamp(scale, 0, 1)), 0, 255), color.R, color.G, color.B);

    private static byte Lerp(byte from, byte to, double amount) =>
        (byte)Math.Clamp((int)Math.Round(from + ((to - from) * amount)), 0, 255);
}
