using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Lightweight, self-drawn radial (ring) gauge. Renders a background track and a
/// foreground arc proportional to <see cref="Value"/> / <see cref="Maximum"/>, with a
/// large centered value and an optional caption underneath. The sweep starts at the
/// lower-left and leaves an open gap at the bottom, matching the WPF RadialGaugeControl.
/// </summary>
public sealed class RadialGauge : Control
{
    private const double StartAngle = 135.0;
    private const double SweepAngle = 270.0;

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(Value));

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(Maximum), 100.0);

    /// <summary>Explicit center text. When empty, the numeric value is shown.</summary>
    public static readonly StyledProperty<string> ValueTextProperty =
        AvaloniaProperty.Register<RadialGauge, string>(nameof(ValueText), string.Empty);

    public static readonly StyledProperty<string> CaptionProperty =
        AvaloniaProperty.Register<RadialGauge, string>(nameof(Caption), string.Empty);

    public static readonly StyledProperty<IBrush?> RingBrushProperty =
        AvaloniaProperty.Register<RadialGauge, IBrush?>(nameof(RingBrush));

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<RadialGauge, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<double> RingThicknessProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(RingThickness), 5.0);

    /// <summary>Stroke thickness for the colored value arc. 0 = auto (RingThickness x 1.6).</summary>
    public static readonly StyledProperty<double> ValueThicknessProperty =
        AvaloniaProperty.Register<RadialGauge, double>(nameof(ValueThickness), 0.0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string ValueText
    {
        get => GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public IBrush? RingBrush
    {
        get => GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public double RingThickness
    {
        get => GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    public double ValueThickness
    {
        get => GetValue(ValueThicknessProperty);
        set => SetValue(ValueThicknessProperty, value);
    }

    private double EffectiveValueThickness => ValueThickness > 0 ? ValueThickness : RingThickness * 1.6;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty
            || change.Property == MinimumProperty
            || change.Property == MaximumProperty
            || change.Property == ValueTextProperty
            || change.Property == CaptionProperty
            || change.Property == RingBrushProperty
            || change.Property == TrackBrushProperty
            || change.Property == RingThicknessProperty
            || change.Property == ValueThicknessProperty
            || change.Property == BoundsProperty)
        {
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Maps <paramref name="value"/> into the 0..1 arc ratio for the current range,
    /// clamped to the unit interval. Non-finite values and inverted ranges yield 0.
    /// </summary>
    public static double NormalizeRatio(double value, double minimum, double maximum)
    {
        var range = maximum - minimum;
        if (range <= 0 || double.IsNaN(value) || double.IsInfinity(value))
            return 0;
        return Math.Clamp((value - minimum) / range, 0.0, 1.0);
    }

    public static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        return new Point(
            center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }

    public static StreamGeometry BuildArc(Point center, double radius, double startAngle, double sweepAngle)
    {
        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, startAngle + sweepAngle);
        var isLargeArc = sweepAngle > 180.0;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false);
            context.ArcTo(
                end,
                new Size(radius, radius),
                0.0,
                isLargeArc,
                SweepDirection.Clockwise,
                true);
        }
        return geometry;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
            return;

        var ringThickness = Math.Max(1.0, RingThickness);
        var valueThickness = Math.Max(1.0, EffectiveValueThickness);
        var maxStroke = Math.Max(ringThickness, valueThickness);
        var radius = Math.Max(0, (size - maxStroke) / 2.0 - 1);
        if (radius <= 0)
            return;

        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);
        var trackBrush = TrackBrush ?? ResolveBrush("ChartTrackBrush", new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)));
        var trackPen = new Pen(trackBrush, ringThickness, lineCap: PenLineCap.Round);
        context.DrawGeometry(null, trackPen, BuildArc(center, radius, StartAngle, SweepAngle));

        var ratio = NormalizeRatio(Value, Minimum, Maximum);
        var sweep = SweepAngle * ratio;
        if (sweep >= 0.5)
        {
            var ringBrush = RingBrush ?? ResolveBrush("ChartUtilizationBrush", Brushes.DodgerBlue);
            var arc = BuildArc(center, radius, StartAngle, sweep);

            var glowPen = new Pen(ExtractTranslucent(ringBrush, 96), valueThickness + 5.0, lineCap: PenLineCap.Round);
            context.DrawGeometry(null, glowPen, arc);

            var valuePen = new Pen(BuildArcStroke(ringBrush), valueThickness, lineCap: PenLineCap.Round);
            context.DrawGeometry(null, valuePen, arc);

            var tipCenter = PointOnCircle(center, radius, StartAngle + sweep);
            var tipDiameter = Math.Max(5.0, valueThickness + 3.0);
            context.DrawEllipse(
                Brushes.White,
                new Pen(ringBrush, Math.Max(1.5, valueThickness * 0.35)),
                new Rect(tipCenter.X - tipDiameter / 2.0, tipCenter.Y - tipDiameter / 2.0, tipDiameter, tipDiameter));
        }

        DrawCenterText(context, center, radius);
    }

    private void DrawCenterText(DrawingContext context, Point center, double radius)
    {
        var text = string.IsNullOrEmpty(ValueText)
            ? Math.Round(Value).ToString("0", CultureInfo.CurrentCulture)
            : ValueText;
        var primaryBrush = ResolveBrush("TextFillColorPrimaryBrush", Brushes.White);
        var valueSize = Math.Clamp(radius * 0.32, 14.0, 40.0);
        var valueText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
            valueSize,
            primaryBrush);
        var hasCaption = !string.IsNullOrEmpty(Caption);
        var verticalOffset = hasCaption ? 4.0 : 0.0;
        context.DrawText(
            valueText,
            new Point(
                center.X - valueText.Width / 2.0,
                center.Y - valueText.Height / 2.0 - verticalOffset));

        if (!hasCaption)
            return;

        var captionBrush = ResolveBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)));
        var captionSize = Math.Clamp(radius * 0.14, 10.0, 18.0);
        var captionText = new FormattedText(
            Caption!,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal),
            captionSize,
            captionBrush);
        context.DrawText(
            captionText,
            new Point(
                center.X - captionText.Width / 2.0,
                center.Y - captionText.Height / 2.0 + valueSize * 0.28));
    }

    private static IBrush BuildArcStroke(IBrush baseBrush)
    {
        var color = baseBrush is SolidColorBrush solid ? solid.Color : Colors.DodgerBlue;
        var gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        };
        gradient.GradientStops.Add(new GradientStop(Lighten(color, 0.35), 0.0));
        gradient.GradientStops.Add(new GradientStop(color, 1.0));
        return gradient;
    }

    private static IBrush ExtractTranslucent(IBrush baseBrush, byte alpha)
    {
        var color = baseBrush is SolidColorBrush solid ? solid.Color : Colors.DodgerBlue;
        return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
    }

    private static Color Lighten(Color color, double amount)
    {
        amount = Math.Clamp(amount, 0.0, 1.0);
        return Color.FromArgb(
            color.A,
            (byte)(color.R + (255 - color.R) * amount),
            (byte)(color.G + (255 - color.G) * amount),
            (byte)(color.B + (255 - color.B) * amount));
    }

    private IBrush ResolveBrush(string resourceKey, IBrush fallback) =>
        this.TryFindResource(resourceKey, out var value) && value is IBrush brush
            ? brush
            : fallback;
}
