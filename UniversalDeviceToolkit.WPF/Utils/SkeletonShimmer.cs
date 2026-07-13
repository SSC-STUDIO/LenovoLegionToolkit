using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Polished skeleton 流光: sweeping highlight + soft breathing opacity.
/// Per-element Freezable brushes (Border has no Template to share animated state).
/// </summary>
public static class SkeletonShimmer
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SkeletonShimmer),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>
    /// Optional start delay (seconds) for stagger. Negative = auto-assign by visual tree order.
    /// </summary>
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

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border)
            return;

        if (e.NewValue is true)
        {
            border.Loaded -= BorderOnLoaded;
            border.Unloaded -= BorderOnUnloaded;
            border.IsVisibleChanged -= BorderOnIsVisibleChanged;
            border.Loaded += BorderOnLoaded;
            border.Unloaded += BorderOnUnloaded;
            border.IsVisibleChanged += BorderOnIsVisibleChanged;

            if (border.IsLoaded)
                QueueStart(border);
        }
        else
        {
            border.Loaded -= BorderOnLoaded;
            border.Unloaded -= BorderOnUnloaded;
            border.IsVisibleChanged -= BorderOnIsVisibleChanged;
            Stop(border);
        }
    }

    private static void BorderOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border)
            QueueStart(border);
    }

    private static void BorderOnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border)
            Stop(border);
    }

    private static void BorderOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is Border border && e.NewValue is true)
            QueueStart(border);
    }

    /// <summary>
    /// Restart 流光 on every shimmer block under <paramref name="root"/> with staggered phases.
    /// </summary>
    public static void RestartSubtree(DependencyObject? root)
    {
        if (root is null)
            return;

        var index = 0;
        RestartSubtreeCore(root, ref index);
    }

    private static void RestartSubtreeCore(DependencyObject root, ref int index)
    {
        if (root is Border border && GetIsEnabled(border) && border.IsLoaded && border.IsVisible)
        {
            // Auto stagger: ~90ms per bone so rows cascade instead of flashing as one slab.
            if (GetDelaySeconds(border) < 0)
                SetDelaySeconds(border, Math.Min(index * 0.09, 0.72));
            index++;
            QueueStart(border);
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
            RestartSubtreeCore(VisualTreeHelper.GetChild(root, i), ref index);
    }

    private static void QueueStart(Border border)
    {
        border.Dispatcher.BeginInvoke(() =>
        {
            if (border.IsLoaded && border.IsVisible && GetIsEnabled(border))
                Start(border);
        }, DispatcherPriority.Render);
    }

    private static void Start(Border border)
    {
        Stop(border);

        var baseColor = ResolveBaseColor(border);
        var duration = ResolveDuration(border);

        // Animations disabled (Duration.Zero) — keep high-contrast static bones, no flicker.
        if (!duration.HasTimeSpan || duration.TimeSpan <= TimeSpan.Zero)
        {
            border.Background = new SolidColorBrush(baseColor);
            border.Opacity = 1.0;
            return;
        }

        // Bake a slightly deeper bone so the shimmer band has room to lift.
        border.Background = CreateShimmerBrush(baseColor);

        if (border.Background is not LinearGradientBrush { RelativeTransform: TranslateTransform transform })
            return;

        var begin = ResolveBeginTime(border);

        // Diagonal sweep with ease-in-out — reads as polished motion, not a hard wipe.
        var sweep = new DoubleAnimation
        {
            From = -1.35,
            To = 1.35,
            Duration = duration,
            BeginTime = begin,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        transform.BeginAnimation(TranslateTransform.XProperty, sweep);

        // Soft breathing so bones never look like static white bricks when sweep is subtle.
        var pulse = new DoubleAnimation
        {
            From = 0.82,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(1100)),
            BeginTime = begin,
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        border.BeginAnimation(UIElement.OpacityProperty, pulse);
    }

    private static void Stop(Border border)
    {
        if (border.Background is LinearGradientBrush { RelativeTransform: TranslateTransform transform })
            transform.BeginAnimation(TranslateTransform.XProperty, null);

        border.BeginAnimation(UIElement.OpacityProperty, null);
        border.Opacity = 1.0;
    }

    private static TimeSpan ResolveBeginTime(Border border)
    {
        var seconds = GetDelaySeconds(border);
        if (seconds <= 0)
            return TimeSpan.Zero;
        return TimeSpan.FromSeconds(Math.Min(seconds, 1.2));
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

        // Snappier modern skeleton (~1.55s one-way).
        return new Duration(TimeSpan.FromMilliseconds(1550));
    }

    private static Color ResolveBaseColor(Border border)
    {
        try
        {
            // Prefer Secondary over Tertiary — Tertiary on light cards is often nearly invisible.
            if (border.TryFindResource("ControlFillColorSecondaryBrush") is SolidColorBrush secondary
                && secondary.Color.A > 0)
                return EnsureBoneContrast(secondary.Color);

            if (border.Background is SolidColorBrush solid && solid.Color.A > 0)
                return EnsureBoneContrast(solid.Color);

            if (border.TryFindResource("ControlFillColorTertiaryBrush") is SolidColorBrush tertiary)
                return EnsureBoneContrast(tertiary.Color);

            if (border.TryFindResource("ControlFillColorTertiary") is Color color)
                return EnsureBoneContrast(color);
        }
        catch
        {
            // fall through
        }

        // Neutral mid-gray works on both themes when resources are missing.
        return Color.FromRgb(0x9A, 0x9A, 0x9A);
    }

    /// <summary>
    /// Bones on near-white cards must not blend into the card surface.
    /// </summary>
    private static Color EnsureBoneContrast(Color color)
    {
        var luminance = (color.R * 0.2126) + (color.G * 0.7152) + (color.B * 0.0722);

        // Too light (white-ish tertiary on light theme) → push down to a readable slate.
        if (luminance >= 220)
            return Color.FromRgb(0xC6, 0xC8, 0xCC);

        // Too dark (near-black on dark theme cards that are already dark) → lift slightly.
        if (luminance <= 40)
            return Color.FromRgb(0x4A, 0x4A, 0x4E);

        return color;
    }

    private static LinearGradientBrush CreateShimmerBrush(Color baseColor)
    {
        var luminance = (baseColor.R * 0.2126) + (baseColor.G * 0.7152) + (baseColor.B * 0.0722);
        var lightThemeBone = luminance >= 140;

        // Light theme: bright silver sheen. Dark theme: soft luminous lift (not pure white flash).
        Color peak;
        Color soft;
        Color edge;
        if (lightThemeBone)
        {
            peak = Color.FromArgb(0xFF, 0xF7, 0xF8, 0xFA);
            soft = Blend(baseColor, peak, 0.72);
            edge = Blend(baseColor, peak, 0.28);
        }
        else
        {
            peak = BlendTowardHighlight(baseColor, amount: 0.72);
            soft = BlendTowardHighlight(baseColor, amount: 0.42);
            edge = BlendTowardHighlight(baseColor, amount: 0.16);
        }

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.0),
            EndPoint = new Point(1, 1.0),
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            RelativeTransform = new TranslateTransform(-1.35, 0)
        };

        // Wider highlight band so the sweep is obvious at a glance (LinkedIn/Fluent style).
        brush.GradientStops.Add(new GradientStop(baseColor, 0.00));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.28));
        brush.GradientStops.Add(new GradientStop(edge, 0.36));
        brush.GradientStops.Add(new GradientStop(soft, 0.44));
        brush.GradientStops.Add(new GradientStop(peak, 0.50));
        brush.GradientStops.Add(new GradientStop(soft, 0.56));
        brush.GradientStops.Add(new GradientStop(edge, 0.64));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.72));
        brush.GradientStops.Add(new GradientStop(baseColor, 1.00));

        return brush;
    }

    private static Color BlendTowardHighlight(Color baseColor, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        var luminance = (baseColor.R * 0.2126) + (baseColor.G * 0.7152) + (baseColor.B * 0.0722);
        var target = luminance >= 190
            ? Color.FromRgb(0xFF, 0xFF, 0xFF)
            : Color.FromRgb(0xD8, 0xDA, 0xDE);
        return Blend(baseColor, target, amount);
    }

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            Lerp(from.R, to.R, amount),
            Lerp(from.G, to.G, amount),
            Lerp(from.B, to.B, amount));
    }

    private static byte Lerp(byte from, byte to, double t) =>
        (byte)Math.Clamp((int)Math.Round(from + (to - from) * t), 0, 255);
}
