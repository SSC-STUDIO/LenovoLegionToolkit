using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace UniversalDeviceToolkit.WPF.Controls.Charts;

/// <summary>
/// A single named data series rendered by <see cref="TrendChartControl"/>.
/// Holds a fixed-capacity ring buffer of recent samples and its own normalization range.
/// </summary>
public sealed class TrendSeries
{
    private readonly double[] _samples;
    private int _count;
    private int _head;

    // Cached rendering resources (invalidated when Color changes)
    private Color _cachedColor;
    private LinearGradientBrush? _cachedFill;
    private Pen? _cachedLinePen;

    public TrendSeries(string key, int capacity)
    {
        Key = key;
        _samples = new double[Math.Max(2, capacity)];
    }

    public string Key { get; }

    /// <summary>Stroke / fill color for this series.</summary>
    public Color Color
    {
        get => _cachedColor;
        set
        {
            if (_cachedColor == value && _cachedFill is not null)
                return;

            _cachedColor = value;
            _cachedFill = null;
            _cachedLinePen = null;
        }
    }

    /// <summary>
    /// Upper bound used to normalize this series into the 0..1 chart area. When null, the
    /// chart auto-scales to the maximum observed sample (with a small headroom).
    /// </summary>
    public double? Maximum { get; set; }

    /// <summary>Slowly adapts when <see cref="Maximum"/> is null so the plot does not jump each sample.</summary>
    private double _smoothedAutoMax = 1.0;

    public int Capacity => _samples.Length;
    public int Count => _count;

    /// <summary>Resolve Y-axis max with optional slow auto-scale (no zero-spike when empty).</summary>
    public double ResolvePlotMaximum()
    {
        if (Maximum is { } fixedMax && fixedMax > 0)
            return fixedMax;

        if (_count < 1)
            return Math.Max(1.0, _smoothedAutoMax);

        var observed = Math.Max(1.0, ObservedMaximum() * 1.08);
        // Ease toward observed max (slow zoom out/in) — prevents axis thrash.
        _smoothedAutoMax = _smoothedAutoMax <= 1.0
            ? observed
            : (_smoothedAutoMax * 0.85) + (observed * 0.15);
        if (_smoothedAutoMax < observed * 0.92)
            _smoothedAutoMax = observed; // snap up when values spike past the smoothed band
        return Math.Max(1.0, _smoothedAutoMax);
    }

    public void Add(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            value = 0;

        if (value < 0)
            value = 0;

        _samples[_head] = value;
        _head = (_head + 1) % _samples.Length;
        if (_count < _samples.Length)
            _count++;
    }

    public void Clear()
    {
        _count = 0;
        _head = 0;
    }

    /// <summary>Enumerates samples from oldest to newest.</summary>
    public IEnumerable<double> EnumerateOrdered()
    {
        for (var i = 0; i < _count; i++)
        {
            var index = (_head - _count + i + _samples.Length * 2) % _samples.Length;
            yield return _samples[index];
        }
    }

    public double ObservedMaximum()
    {
        var max = 0.0;
        foreach (var sample in EnumerateOrdered())
            if (sample > max)
                max = sample;
        return max;
    }

    public LinearGradientBrush GetOrCreateFill()
    {
        if (_cachedFill is not null)
            return _cachedFill;

        // Keep multi-series fills translucent so later series do not obscure earlier data.
        var fill = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(76, _cachedColor.R, _cachedColor.G, _cachedColor.B), 0.0));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(42, _cachedColor.R, _cachedColor.G, _cachedColor.B), 0.48));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(10, _cachedColor.R, _cachedColor.G, _cachedColor.B), 1.0));
        fill.Freeze();
        _cachedFill = fill;
        return fill;
    }

    public Pen GetOrCreateLinePen()
    {
        if (_cachedLinePen is not null)
            return _cachedLinePen;

        // Keep crest stroke slim so multi-series charts stay readable (was 1.9).
        var pen = new Pen(new SolidColorBrush(_cachedColor), 1.15)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };
        pen.Freeze();
        _cachedLinePen = pen;
        return pen;
    }
}

/// <summary>
/// Lightweight, self-drawn multi-series trend chart (line + soft area fill). Designed to
/// mirror the dashboard sensor history graph without any external charting dependency.
/// Series are appended via <see cref="AddSample"/> and the visual is invalidated on each push.
/// </summary>
public class TrendChartControl : FrameworkElement
{
    private const int DefaultCapacity = 60;
    private readonly Dictionary<string, TrendSeries> _series = new(StringComparer.Ordinal);
    private readonly List<TrendSeries> _orderedSeries = [];

    public static readonly DependencyProperty CapacityProperty = DependencyProperty.Register(
        nameof(Capacity), typeof(int), typeof(TrendChartControl),
        new FrameworkPropertyMetadata(DefaultCapacity));

    public int Capacity
    {
        get => (int)GetValue(CapacityProperty);
        set => SetValue(CapacityProperty, value);
    }

    /// <summary>Registers (or replaces) a series. Returns the series for fluent setup.</summary>
    public TrendSeries DefineSeries(string key, Color color, double? maximum = null)
    {
        var series = new TrendSeries(key, Capacity) { Color = color, Maximum = maximum };
        if (_series.TryGetValue(key, out var existing))
            _orderedSeries.Remove(existing);

        _series[key] = series;
        _orderedSeries.Add(series);
        return series;
    }

    /// <summary>Appends one sample to a named series and requests a redraw.</summary>
    public void AddSample(string key, double value)
    {
        if (!_series.TryGetValue(key, out var series))
            return;

        series.Add(value);
        InvalidateVisual();
    }

    public void ClearAll()
    {
        foreach (var series in _orderedSeries)
            series.Clear();
        InvalidateVisual();
    }

    public static readonly DependencyProperty GridlineBrushProperty = DependencyProperty.Register(
        nameof(GridlineBrush), typeof(Brush), typeof(TrendChartControl),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush GridlineBrush
    {
        get => (Brush)GetValue(GridlineBrushProperty);
        set => SetValue(GridlineBrushProperty, value);
    }

    public static readonly DependencyProperty BaselineBrushProperty = DependencyProperty.Register(
        nameof(BaselineBrush), typeof(Brush), typeof(TrendChartControl),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush BaselineBrush
    {
        get => (Brush)GetValue(BaselineBrushProperty);
        set => SetValue(BaselineBrushProperty, value);
    }

    /// <summary>Brush for Y-axis scale labels (100%, 75%, 50%, 25%).</summary>
    public static readonly DependencyProperty ScaleLabelBrushProperty = DependencyProperty.Register(
        nameof(ScaleLabelBrush), typeof(Brush), typeof(TrendChartControl),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush ScaleLabelBrush
    {
        get => (Brush)GetValue(ScaleLabelBrushProperty);
        set => SetValue(ScaleLabelBrushProperty, value);
    }

    /// <summary>Padding around the plot area so scale labels and strokes don't clip at edges.</summary>
    public static readonly DependencyProperty PlotPaddingProperty = DependencyProperty.Register(
        nameof(PlotPadding), typeof(Thickness), typeof(TrendChartControl),
        new FrameworkPropertyMetadata(new Thickness(4, 4, 4, 4), FrameworkPropertyMetadataOptions.AffectsRender));

    public Thickness PlotPadding
    {
        get => (Thickness)GetValue(PlotPaddingProperty);
        set => SetValue(PlotPaddingProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var pad = PlotPadding;
        var width = ActualWidth - pad.Left - pad.Right;
        var height = ActualHeight - pad.Top - pad.Bottom;
        if (width <= 1 || height <= 1)
            return;

        var dpiScale = VisualTreeHelper.GetDpi(this);
        var translate = new TranslateTransform(pad.Left, pad.Top);
        dc.PushTransform(translate);

        DrawGridlines(dc, width, height, GridlineBrush, BaselineBrush, ScaleLabelBrush, dpiScale.PixelsPerDip);

        foreach (var series in _orderedSeries)
            DrawSeries(dc, series, width, height, drawFill: true, drawLine: false);

        // Draw every crest after all fills so no later series can cover an earlier line.
        foreach (var series in _orderedSeries)
            DrawSeries(dc, series, width, height, drawFill: false, drawLine: true);

        dc.Pop();
    }

    /// <summary>
    /// Soft horizontal guides and a warmer baseline accent (reference band chart).
    /// Y-axis scale labels at 100%, 75%, 50%, 25% positions (Watt Toolkit reference).
    /// No outer rectangular frame.
    /// </summary>
    private static void DrawGridlines(DrawingContext dc, double width, double height, Brush gridlineBrush, Brush baselineBrush, Brush scaleLabelBrush, double pixelsPerDip)
    {
        var grid = new Pen(gridlineBrush, 0.5);
        // Baseline is the warm accent edge under the filled band (see reference chart).
        var baseline = new Pen(baselineBrush, 1.0);

        // Scale label positions: fraction of height → label text.
        // 0% at top (height=0), 100% at bottom (height=H). But charts plot
        // value-up, so fraction 0.25 from top = 75% value, etc.
        var scaleMarks = new (double fraction, string label)[]
        {
            (0.0, "100%"),
            (0.25, "75%"),
            (0.50, "50%"),
            (0.75, "25%"),
        };

        foreach (var (fraction, label) in scaleMarks)
        {
            var y = Math.Round(height * fraction) + 0.5;

            // Draw gridline (skip the very top — 100% has no gridline to avoid clipping).
            if (fraction > 0.01 && fraction < 0.99)
                dc.DrawLine(grid, new Point(0, y), new Point(width, y));

            // Draw scale label (small text pinned to the left edge).
            var ft = new FormattedText(
                label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI, Segoe UI Variable Text, sans-serif"),
                9.0,
                scaleLabelBrush,
                pixelsPerDip > 0 ? pixelsPerDip : 1.0);
            // Offset: 2px from left, vertically centered on the gridline.
            dc.DrawText(ft, new Point(3, y - ft.Height / 2.0));
        }

        dc.DrawLine(baseline, new Point(0, height - 0.5), new Point(width, height - 0.5));
    }

    private static void DrawSeries(
        DrawingContext dc,
        TrendSeries series,
        double width,
        double height,
        bool drawFill,
        bool drawLine)
    {
        // No samples: do not draw a path to zero (that caused vertical spikes on first paint).
        if (series.Count < 1)
            return;

        var max = series.ResolvePlotMaximum();
        if (max <= 0)
            max = 1.0;

        var points = BuildPlotPoints(series, width, height, max);
        if (points.Count < 2)
            return;

        var lineGeometry = new StreamGeometry();
        var areaGeometry = new StreamGeometry();

        using (var lineCtx = lineGeometry.Open())
        using (var areaCtx = areaGeometry.Open())
        {
            // Closed area: baseline → left crest → curve → right crest → baseline.
            // Right-aligned samples grow the band from the right edge leftward.
            lineCtx.BeginFigure(points[0], false, false);
            areaCtx.BeginFigure(new Point(points[0].X, height), true, true);
            areaCtx.LineTo(points[0], true, false);

            for (var p = 0; p < points.Count - 1; p++)
            {
                var p0 = points[Math.Max(0, p - 1)];
                var p1 = points[p];
                var p2 = points[p + 1];
                var p3 = points[Math.Min(points.Count - 1, p + 2)];

                var c1 = new Point(p1.X + (p2.X - p0.X) / 6.0, p1.Y + (p2.Y - p0.Y) / 6.0);
                var c2 = new Point(p2.X - (p3.X - p1.X) / 6.0, p2.Y - (p3.Y - p1.Y) / 6.0);

                lineCtx.BezierTo(c1, c2, p2, true, false);
                areaCtx.BezierTo(c1, c2, p2, true, false);
            }

            var lastPoint = points[^1];
            var previousPoint = points[^2];
            var sampleStep = Math.Max(1.0, lastPoint.X - previousPoint.X);
            var tailWidth = Math.Clamp(sampleStep * 0.72, 6.0, 20.0);
            var tailBottom = new Point(Math.Max(points[0].X, lastPoint.X - tailWidth), height);
            var tailHeight = Math.Max(1.0, height - lastPoint.Y);

            // Curve the filled band back into the baseline instead of closing it with a
            // hard vertical wall at the newest sample. The crest line still reaches the
            // right edge, while the translucent area gets a softer, tapered silhouette.
            areaCtx.BezierTo(
                new Point(lastPoint.X - tailWidth * 0.18, lastPoint.Y + tailHeight * 0.30),
                new Point(lastPoint.X - tailWidth * 0.72, height),
                tailBottom,
                true,
                false);
        }

        lineGeometry.Freeze();
        areaGeometry.Freeze();

        if (drawFill)
            dc.DrawGeometry(series.GetOrCreateFill(), null, areaGeometry);
        if (drawLine)
            dc.DrawGeometry(null, series.GetOrCreateLinePen(), lineGeometry);
    }

    /// <summary>
    /// Builds screen-space points for a series. Samples are <b>right-aligned</b>: the newest
    /// sample sits on the right edge and history grows leftward (right → left) as capacity fills.
    /// A single sample expands one step left so the band still has visible width.
    /// </summary>
    internal static List<Point> BuildPlotPoints(TrendSeries series, double width, double height, double max)
    {
        var capacity = Math.Max(2, series.Capacity);
        var stepX = width / (capacity - 1);
        var count = series.Count;
        var points = new List<Point>(Math.Max(2, count));

        // Right-align into the capacity grid: first (oldest) sample at slot (capacity - count).
        var startSlot = capacity - count;
        var i = 0;
        foreach (var sample in series.EnumerateOrdered())
        {
            var x = (startSlot + i) * stepX;
            var ratio = Math.Clamp(sample / max, 0.0, 1.0);
            // 1px padding top/bottom keeps the stroke fully inside the plot area.
            var y = height - ratio * (height - 2) - 1;
            points.Add(new Point(x, y));
            i++;
        }

        // One sample: extend a flat segment one step to the left of the right edge.
        if (points.Count == 1)
        {
            var right = points[0];
            points.Insert(0, new Point(Math.Max(0, right.X - stepX), right.Y));
        }

        return points;
    }
}
