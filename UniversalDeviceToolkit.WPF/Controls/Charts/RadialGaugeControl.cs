using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UniversalDeviceToolkit.WPF.Controls.Charts;

/// <summary>
/// A lightweight, self-drawn radial (ring) gauge. Renders a background track and a
/// foreground arc proportional to <see cref="Value"/> / <see cref="Maximum"/>, with a
/// large centered value and an optional caption underneath. No external chart library
/// is used; everything is drawn with <see cref="Path"/> + <see cref="ArcSegment"/>.
/// </summary>
public class RadialGaugeControl : Control
{
    private const double StartAngle = 135.0;   // sweep starts at lower-left
    private const double SweepAngle = 270.0;    // open-bottom ring (gap at bottom)
    private const double AnimationMs = 350.0;

    private Path? _trackPath;
    private Path? _valuePath;
    private Path? _glowPath;
    private Ellipse? _tip;
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

    static RadialGaugeControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RadialGaugeControl),
            new FrameworkPropertyMetadata(typeof(RadialGaugeControl)));
    }

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

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RadialGaugeControl),
        new FrameworkPropertyMetadata(0.0, OnValueChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RadialGaugeControl),
        new FrameworkPropertyMetadata(100.0, OnVisualChanged));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(RadialGaugeControl),
        new FrameworkPropertyMetadata(0.0, OnVisualChanged));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText), typeof(string), typeof(RadialGaugeControl),
        new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>Optional explicit center text. When empty, the numeric value is shown.</summary>
    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(RadialGaugeControl),
        new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly DependencyProperty RingBrushProperty = DependencyProperty.Register(
        nameof(RingBrush), typeof(Brush), typeof(RadialGaugeControl),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, OnVisualChanged));

    public Brush RingBrush
    {
        get => (Brush)GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(RadialGaugeControl),
        new FrameworkPropertyMetadata(OnVisualChanged));

    public Brush? TrackBrush
    {
        get => (Brush?)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public static readonly DependencyProperty RingThicknessProperty = DependencyProperty.Register(
        nameof(RingThickness), typeof(double), typeof(RadialGaugeControl),
        new FrameworkPropertyMetadata(8.0, OnVisualChanged));

    public double RingThickness
    {
        get => (double)GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _trackPath = GetTemplateChild("PART_Track") as Path;
        _valuePath = GetTemplateChild("PART_Value") as Path;
        _glowPath = GetTemplateChild("PART_Glow") as Path;
        _tip = GetTemplateChild("PART_Tip") as Ellipse;
        _valueText = GetTemplateChild("PART_ValueText") as TextBlock;
        _captionText = GetTemplateChild("PART_CaptionText") as TextBlock;
        _renderedValue = NormalizedValue();
        RedrawAll();
        UpdateText();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (RadialGaugeControl)d;
        gauge.AnimateToValue();
        gauge.UpdateText();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (RadialGaugeControl)d;
        if (e.Property == RingBrushProperty)
        {
            gauge._cachedRingColor = null;
            gauge._cachedArcStroke = null;
            gauge._cachedGlowBrush = null;
            gauge._cachedTipBrush = null;
        }
        gauge.RedrawAll();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RadialGaugeControl)d).UpdateText();

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
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0)
            return;

        if (_valueText is not null)
            _valueText.FontSize = Math.Clamp(size * 0.2, 14.0, 40.0);

        if (_captionText is not null)
            _captionText.FontSize = Math.Clamp(size * 0.1, 10.0, 18.0);
    }

    private (Point center, double radius) GetGeometry()
    {
        var size = Math.Min(ActualWidth, ActualHeight);
        var radius = Math.Max(0, (size - RingThickness) / 2.0 - 1);
        var center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
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

        var size = new Size(ActualWidth, ActualHeight);
        if (_cachedTrackGeometry is null
            || _cachedTrackSize != size
            || Math.Abs(_cachedTrackThickness - RingThickness) > double.Epsilon)
        {
            _cachedTrackGeometry = BuildArc(center, radius, StartAngle, SweepAngle);
            _cachedTrackSize = size;
            _cachedTrackThickness = RingThickness;
        }

        _trackPath.Data = _cachedTrackGeometry;
        _trackPath.StrokeThickness = RingThickness;
        _trackPath.Stroke = TrackBrush ?? (TryFindResource("ChartTrackBrush") as Brush) ?? Brushes.Gray;
        _trackPath.StrokeStartLineCap = PenLineCap.Round;
        _trackPath.StrokeEndLineCap = PenLineCap.Round;
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
                _tip.Visibility = Visibility.Collapsed;
            return;
        }

        var sweep = SweepAngle * Math.Clamp(_renderedValue, 0.0, 1.0);

        _valuePath.StrokeThickness = RingThickness;
        _valuePath.Stroke = BuildArcStroke();
        _valuePath.StrokeStartLineCap = PenLineCap.Round;
        _valuePath.StrokeEndLineCap = PenLineCap.Round;

        // Avoid a zero-length arc (renders nothing / a dot) when value is ~0.
        var arc = sweep < 0.5 ? null : BuildArc(center, radius, StartAngle, sweep);
        _valuePath.Data = arc;

        // Soft glow underlay traces the same arc with a thicker, translucent, blurred stroke.
        if (_glowPath is not null)
        {
            _glowPath.Data = arc;
            _glowPath.StrokeThickness = RingThickness + 6;
            _glowPath.Stroke = ExtractRingColorBrush(96);
            _glowPath.StrokeStartLineCap = PenLineCap.Round;
            _glowPath.StrokeEndLineCap = PenLineCap.Round;
        }

        // Bright dot riding the leading edge of the value arc.
        if (_tip is not null)
        {
            if (arc is null)
            {
                _tip.Visibility = Visibility.Collapsed;
            }
            else
            {
                var tipCenter = PointOnCircle(center, radius, StartAngle + sweep);
                var tipDiameter = Math.Max(4.0, RingThickness + 2.0);

                _tip.Width = tipDiameter;
                _tip.Height = tipDiameter;
                _tip.Fill = ExtractRingColorBrush(255);
                _tip.Margin = new Thickness(
                    tipCenter.X - tipDiameter / 2.0,
                    tipCenter.Y - tipDiameter / 2.0,
                    0,
                    0);
                _tip.Visibility = Visibility.Visible;
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
            _captionText.Visibility = string.IsNullOrEmpty(Caption)
                ? Visibility.Collapsed
                : Visibility.Visible;
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
            ctx.BeginFigure(start, false, false);
            ctx.ArcTo(
                end,
                new Size(radius, radius),
                0.0,
                isLargeArc,
                SweepDirection.Clockwise,
                true,
                false);
        }
        geometry.Freeze();
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
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop(Lighten(baseColor, 0.35), 0.0));
        brush.GradientStops.Add(new GradientStop(baseColor, 1.0));
        brush.Freeze();

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
        brush.Freeze();

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