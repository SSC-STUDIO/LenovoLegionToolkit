using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Per-instance skeleton 流光 (sweep highlight). Border is a Decorator and has no
/// Control.Template — brush + TranslateTransform must be created per element so the
/// Freezable is not shared/frozen by a Style setter.
/// </summary>
public static class SkeletonShimmer
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SkeletonShimmer),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border)
            return;

        if (e.NewValue is true)
        {
            border.Loaded -= BorderOnLoaded;
            border.Unloaded -= BorderOnUnloaded;
            border.Loaded += BorderOnLoaded;
            border.Unloaded += BorderOnUnloaded;

            if (border.IsLoaded)
                Start(border);
        }
        else
        {
            border.Loaded -= BorderOnLoaded;
            border.Unloaded -= BorderOnUnloaded;
            Stop(border);
        }
    }

    private static void BorderOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border)
            Start(border);
    }

    private static void BorderOnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border)
            Stop(border);
    }

    private static void Start(Border border)
    {
        Stop(border);

        var baseColor = ResolveBaseColor(border);
        var brush = CreateShimmerBrush(baseColor);
        border.Background = brush;

        if (brush.RelativeTransform is not TranslateTransform transform)
            return;

        var duration = ResolveDuration(border);
        // Soft continuous wash — Sine + longer travel, no sharp flash at the peak.
        var animation = new DoubleAnimation
        {
            From = -1.35,
            To = 1.35,
            Duration = duration,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        transform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private static void Stop(Border border)
    {
        if (border.Background is LinearGradientBrush { RelativeTransform: TranslateTransform transform })
            transform.BeginAnimation(TranslateTransform.XProperty, null);
    }

    private static Duration ResolveDuration(FrameworkElement element)
    {
        try
        {
            if (element.TryFindResource("AnimationDurationShimmer") is Duration duration
                && duration.HasTimeSpan
                && duration.TimeSpan > TimeSpan.Zero)
                return duration;
        }
        catch
        {
            // resource dictionary not ready
        }

        // Fallback when token missing: ~3.5s one-way sweep (matches calm default).
        return new Duration(TimeSpan.FromMilliseconds(3500));
    }

    private static Color ResolveBaseColor(Border border)
    {
        try
        {
            if (border.Background is SolidColorBrush solid && solid.Color.A > 0)
                return solid.Color;

            if (border.TryFindResource("ControlFillColorTertiaryBrush") is SolidColorBrush tertiary)
                return tertiary.Color;

            if (border.TryFindResource("ControlFillColorTertiary") is Color color)
                return color;
        }
        catch
        {
            // fall through
        }

        return Color.FromRgb(0x5A, 0x5A, 0x5A);
    }

    private static LinearGradientBrush CreateShimmerBrush(Color baseColor)
    {
        // No pure white overlay — only a gentle lift of the same hue family as the base fill
        // so the sweep feels like soft surface motion, not a white glare.
        var peak = LightenTowardNeutral(baseColor, amount: 0.07);
        var soft = LightenTowardNeutral(baseColor, amount: 0.035);
        var softEdge = LightenTowardNeutral(baseColor, amount: 0.015);

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            RelativeTransform = new TranslateTransform(-1.35, 0)
        };

        // Very wide, low-contrast band (matte wash)
        brush.GradientStops.Add(new GradientStop(baseColor, 0.00));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.18));
        brush.GradientStops.Add(new GradientStop(softEdge, 0.34));
        brush.GradientStops.Add(new GradientStop(soft, 0.44));
        brush.GradientStops.Add(new GradientStop(peak, 0.50));
        brush.GradientStops.Add(new GradientStop(soft, 0.56));
        brush.GradientStops.Add(new GradientStop(softEdge, 0.66));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.82));
        brush.GradientStops.Add(new GradientStop(baseColor, 1.00));

        return brush;
    }

    /// <summary>
    /// Lift base color slightly toward a muted neutral gray (not white), preserving mood of dark/light themes.
    /// </summary>
    private static Color LightenTowardNeutral(Color baseColor, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        // Neutral target sits a bit above mid-gray so dark skeletons lift gently;
        // for already-light bases the mix stays subtle.
        var target = Color.FromRgb(0x9A, 0x9A, 0x9A);
        return Color.FromRgb(
            Lerp(baseColor.R, target.R, amount),
            Lerp(baseColor.G, target.G, amount),
            Lerp(baseColor.B, target.B, amount));
    }

    private static byte Lerp(byte from, byte to, double t) =>
        (byte)Math.Clamp((int)Math.Round(from + (to - from) * t), 0, 255);
}
