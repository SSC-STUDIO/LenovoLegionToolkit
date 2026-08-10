using UniversalDeviceToolkit.Avalonia.Extensions;
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Controls.Charts;

/// <summary>
/// A lightweight, self-drawn radial (ring) gauge. Renders a background track and a
/// foreground arc proportional to <see cref="Value"/> / <see cref="Maximum"/>, with a
/// large centered value and an optional caption underneath. No external chart library
/// is used; everything is drawn with <see cref="Path"/> + <see cref="StreamGeometry"/>.
/// </summary>
public class RadialGaugeControl : TemplatedControl
{
    private const double StartAngle = 135.0;   // sweep starts at lower-left
    private const double SweepAngle = 270.0;    // open-bottom ring (gap at bottom)
    private const double AnimationMs = 350.0;

    private global::Avalonia.Controls.Shapes.Path? _trackPath;
    private global::Avalonia.Controls.Shapes.Path? _valuePath;
    private global::Avalonia.Controls.Shapes.Path? _glowPath;
    private global::Avalonia.Controls.Shapes.Ellipse? _tip;
    private TextBlock? _valueText;
    private TextBlock? _captionText;
    private double _renderedValue;
    private DispatcherTimer? _animationTimer;
    private DateTime _animStart;
    private TimeSpan _animDuration;
    private double _animFrom;
    private double _animTo;

    private Brush? _cachedRingColor;
    private LinearGradientBrush? _cachedArcStroke;
    private SolidColorBrush? _cachedGlowBrush;
    private SolidColorBrush? _cachedTipBrush;
    private Geometry? _cachedTrackGeometry;
    private Size _cachedTrackSize;
    private double _cachedTrackThickness;

    // Cached per-frame values to skip redundant property sets during animation
    private double _lastAppliedThickness = double.NaN;

    public RadialGaugeControl()
    {
        Loaded += (_, _) => RedrawAll();
        SizeChanged += (_, _) => RedrawAll();
        Unloaded += (_, _) =>
        {
            _animationTimer?.Stop();
            _animationTimer = null;
        };
    }

    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<RadialGaugeControl, double>(
        nameof(Value), 0.0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<RadialGaugeControl, double>(
        nameof(Maximum), 100.0);

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<RadialGaugeControl, double>(
        nameof(Minimum), 0.0);

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly StyledProperty<string> ValueTextProperty = AvaloniaProperty.Register<RadialGaugeControl, string>(
        nameof(ValueText), string.Empty);

    /// <summary>Optional explicit center text. When empty, the numeric value is shown.</summary>
    public string ValueText
    {
        get => GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public static readonly StyledProperty<string> CaptionProperty = AvaloniaProperty.Register<RadialGaugeControl, string>(
        nameof(Caption), string.Empty);

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly StyledProperty<Brush> RingBrushProperty = AvaloniaProperty.Register<RadialGaugeControl, Brush>(
        nameof(RingBrush), new SolidColorBrush(Colors.DodgerBlue));

    public Brush RingBrush
    {
        get => GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    public static readonly StyledProperty<Brush?> TrackBrushProperty = AvaloniaProperty.Register<RadialGaugeControl, Brush?>(
        nameof(TrackBrush));

    public Brush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public static readonly StyledProperty<double> RingThicknessProperty = AvaloniaProperty.Register<RadialGaugeControl, double>(
        nameof(RingThickness), 5.0);

    public double RingThickness
    {
        get => GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    /// <summary>Stroke thickness for the colored value arc (blue). Defaults to 1.6× track.</summary>
    public static readonly StyledProperty<double> ValueThicknessProperty = AvaloniaProperty.Register<RadialGaugeControl, double>(
        nameof(ValueThickness), 0.0); // 0 = auto (RingThickness × 1.6)

    public double ValueThickness
    {
        get => GetValue(ValueThicknessProperty);
        set => SetValue(ValueThicknessProperty, value);
    }

    private double EffectiveValueThickness => ValueThickness > 0 ? ValueThickness : RingThickness * 1.6;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _trackPath = e.NameScope.Find<Path>("PART_Track");
        _valuePath = e.NameScope.Find<Path>("PART_Value");
        _glowPath = e.NameScope.Find<Path>("PART_Glow");
        _tip = e.NameScope.Find<Ellipse>("PART_Tip");
        _valueText = e.NameScope.Find<TextBlock>("PART_ValueText");
        _captionText = e.NameScope.Find<TextBlock>("PART_CaptionText");
        _renderedValue = NormalizedValue();
        RedrawAll();
        UpdateText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            AnimateToValue();
            UpdateText();
        }
        else if (change.Property == MaximumProperty
            || change.Property == MinimumProperty
            || change.Property == RingBrushProperty
            || change.Property == TrackBrushProperty
            || change.Property == RingThicknessProperty
            || change.Property == ValueThicknessProperty)
        {
            if (change.Property == RingBrushProperty)
            {
                _cachedRingColor = null;
                _cachedArcStroke = null;
                _cachedGlowBrush = null;
                _cachedTipBrush = null;
            }
            if (change.Property == RingThicknessProperty || change.Property == ValueThicknessProperty)
                _lastAppliedThickness = double.NaN;
            RedrawAll();
        }
        else if (change.Property == ValueTextProperty || change.Property == CaptionProperty)
        {
            UpdateText();
        }
    }

    private double NormalizedValue()
    {
        var range = Maximum - Minimum;
        if (range <= 0 || double.IsNaN(Value))
            return 0;

        var ratio = (Value - Minimum) / range;
        return Math.Clamp(ratio, 0.0, 1.0);
    }

    private void AnimateToValue()
    {
        var target = NormalizedValue();
        BeginAnimationManually(_renderedValue, target);
    }

    private void BeginAnimationManually(double from, double to)
    {
        _animationTimer?.Stop();
        _animStart = DateTime.UtcNow;
        _animDuration = TimeSpan.FromMilliseconds(AnimationMs);
        _animFrom = from;
        _animTo = to;

        _animationTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _animationTimer.Tick -= AnimationTick;
        _animationTimer.Tick += AnimationTick;
        _animationTimer.Start();
    }

    private void AnimationTick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.UtcNow - _animStart;
        var t = Math.Clamp(elapsed.TotalMilliseconds / _animDuration.TotalMilliseconds, 0.0, 1.0);
        var eased = 1 - Math.Pow(1 - t, 3);
        _renderedValue = _animFrom + (_animTo - _animFrom) * eased;
        DrawValueArc();

        if (t >= 1.0)
        {
            _animationTimer?.Stop();
            if (sender is DispatcherTimer timer)
                timer.Tick -= AnimationTick;
        }
    }

    private void RedrawAll()
    {
        DrawTrackArc();
        DrawValueArc();
        ScaleText();
    }

    /// <summary>
    /// Scales the centered value/caption typography with the gauge diameter so a large
    /// big-screen gauge fills its ring instead of leaving a tiny number adrift in the middle.
    /// </summary>
    private void ScaleText()
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
            return;

        if (_valueText is not null)
            _valueText.FontSize = Math.Clamp(size * 0.2, 14.0, 40.0);

        if (_captionText is not null)
            _captionText.FontSize = Math.Clamp(size * 0.1, 10.0, 18.0);
    }

    private (Point center, double radius) GetGeometry()
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var maxStroke = Math.Max(RingThickness, EffectiveValueThickness);
        var radius = Math.Max(0, (size - maxStroke) / 2.0 - 1);
        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);
        return (center, radius);
    }

    private void DrawTrackArc()
    {
        if (_trackPath is null)
            return;

        var (center, radius) = GetGeometry();
        if (radius <= 0)
        {
            _trackPath.Data = null;
            _cachedTrackGeometry = null;
            return;
        }

        var size = Bounds.Size;
        if (_cachedTrackGeometry is null
            || _cachedTrackSize != size
            || Math.Abs(_cachedTrackThickness - RingThickness) > double.Epsilon)
        {
            _cachedTrackGeometry = BuildArc(center, radius, StartAngle, SweepAngle);
            _cachedTrackSize = size;
            _cachedTrackThickness = RingThickness;
        }

        _trackPath.Data = _cachedTrackGeometry;

        var trackBrush = TrackBrush ?? (this.TryFindResource("ChartTrackBrush") as Brush) ?? new SolidColorBrush(Colors.Gray);
        if (Math.Abs(_trackPath.StrokeThickness - RingThickness) > double.Epsilon)
        {
            _trackPath.StrokeThickness = RingThickness;
            _trackPath.StrokeLineCap = PenLineCap.Round;
        }
        if (!ReferenceEquals(_trackPath.Stroke, trackBrush))
            _trackPath.Stroke = trackBrush;
    }

    private void DrawValueArc()
    {
        if (_valuePath is null)
            return;

        var (center, radius) = GetGeometry();
        if (radius <= 0)
        {
            _valuePath.Data = null;
            if (_glowPath is not null)
                _glowPath.Data = null;
            if (_tip is not null)
                _tip.IsVisible = false;
            return;
        }

        var sweep = SweepAngle * Math.Clamp(_renderedValue, 0.0, 1.0);
        var valueThickness = EffectiveValueThickness;
        var glowThickness = valueThickness + 5.0;

        // Only update stroke properties when thickness changes.
        // NaN (set by OnPropertyChanged) must force an update — NaN comparisons are always false.
        if (double.IsNaN(_lastAppliedThickness) || Math.Abs(_lastAppliedThickness - valueThickness) > double.Epsilon)
        {
            _valuePath.StrokeThickness = valueThickness;
            _valuePath.StrokeLineCap = PenLineCap.Round;

            if (_glowPath is not null)
            {
                _glowPath.StrokeThickness = glowThickness;
                _glowPath.StrokeLineCap = PenLineCap.Round;
            }
            _lastAppliedThickness = valueThickness;
        }

        // Update brushes only when RingBrush changes (cached)
        var arcStroke = BuildArcStroke();
        if (!ReferenceEquals(_valuePath.Stroke, arcStroke))
            _valuePath.Stroke = arcStroke;

        var glowBrush = ExtractRingColorBrush(96);
        if (_glowPath is not null && !ReferenceEquals(_glowPath.Stroke, glowBrush))
            _glowPath.Stroke = glowBrush;

        // Avoid a zero-length arc (renders nothing / a dot) when value is ~0.
        var arc = sweep < 0.5 ? null : BuildArc(center, radius, StartAngle, sweep);
        _valuePath.Data = arc;

        if (_glowPath is not null)
            _glowPath.Data = arc;

        // Bright dot riding the leading edge of the value arc.
        // Theme-adaptive: white fill + colored stroke ensures visibility in both
        // light and dark themes (solid color dot blends into dark backgrounds).
        if (_tip is not null)
        {
            if (arc is null)
            {
                _tip.IsVisible = false;
            }
            else
            {
                var tipCenter = PointOnCircle(center, radius, StartAngle + sweep);
                var tipDiameter = Math.Max(5.0, valueThickness + 3.0);

                _tip.Width = tipDiameter;
                _tip.Height = tipDiameter;
                _tip.Fill = Brushes.White;
                _tip.Stroke = ExtractRingColorBrush(255);
                _tip.StrokeThickness = Math.Max(1.5, valueThickness * 0.35);
                _tip.Margin = new Thickness(
                    tipCenter.X - tipDiameter / 2.0,
                    tipCenter.Y - tipDiameter / 2.0,
                    0,
                    0);
                _tip.IsVisible = true;
            }
        }
    }
    private void UpdateText()
    {
        if (_valueText is not null)
        {
            _valueText.Text = string.IsNullOrEmpty(ValueText)
                ? Value.ToString("0", CultureInfo.CurrentCulture)
                : ValueText;
        }

        if (_captionText is not null)
        {
            _captionText.Text = Caption;
            _captionText.IsVisible = string.IsNullOrEmpty(Caption)
                ? false
                : true;
        }
    }

    private static Geometry BuildArc(Point center, double radius, double startAngle, double sweepAngle)
    {
        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, startAngle + sweepAngle);
        var isLargeArc = sweepAngle > 180.0;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start, false);
            ctx.ArcTo(
                end,
                new Size(radius, radius),
                0.0,
                isLargeArc,
                SweepDirection.Clockwise);
        }
        return geometry;
    }

    private static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        return new Point(
            center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }
    private Brush BuildArcStroke()
    {
        var baseColor = ExtractRingColor();
        if (_cachedArcStroke is not null && ReferenceEquals(_cachedRingColor, RingBrush))
            return _cachedArcStroke;

        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
        };
        brush.GradientStops.Add(new GradientStop(Lighten(baseColor, 0.35), 0.0));
        brush.GradientStops.Add(new GradientStop(baseColor, 1.0));

        _cachedArcStroke = brush;
        _cachedRingColor = RingBrush;
        return brush;
    }

    private SolidColorBrush ExtractRingColorBrush(byte alpha)
    {
        var color = ExtractRingColor();

        if (alpha == 96 && _cachedGlowBrush is not null && ReferenceEquals(_cachedRingColor, RingBrush))
            return _cachedGlowBrush;

        if (alpha == 255 && _cachedTipBrush is not null && ReferenceEquals(_cachedRingColor, RingBrush))
            return _cachedTipBrush;

        var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));

        if (alpha == 96)
        {
            _cachedGlowBrush = brush;
            _cachedRingColor = RingBrush;
        }
        else if (alpha == 255)
        {
            _cachedTipBrush = brush;
            _cachedRingColor = RingBrush;
        }

        return brush;
    }

    private Color ExtractRingColor()
    {
        return RingBrush is SolidColorBrush solid ? solid.Color : Colors.DodgerBlue;
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
}
