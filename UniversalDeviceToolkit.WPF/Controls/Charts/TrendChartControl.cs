using System;
using System.Collections.Generic;
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

    public int Capacity => _samples.Length;
    public int Count => _count;

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

        // Reference-style fill: readable solid tint at the top that softens toward the baseline
        // so the area has a clear silhouette even with few samples at first open.
        var fill = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(140, _cachedColor.R, _cachedColor.G, _cachedColor.B), 0.0));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(72, _cachedColor.R, _cachedColor.G, _cachedColor.B), 0.55));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(20, _cachedColor.R, _cachedColor.G, _cachedColor.B), 1.0));
        fill.Freeze();
        _cachedFill = fill;
        return fill;
    }

    public Pen GetOrCreateLinePen()
    {
        if (_cachedLinePen is not null)
            return _cachedLinePen;

        // Slightly thicker stroke so the top edge stays crisp against the soft fill.
        var pen = new Pen(new SolidColorBrush(_cachedColor), 2.25)
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

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 1 || height <= 1)
            return;

        DrawPlotFrame(dc, width, height, GridlineBrush, BaselineBrush);

        foreach (var series in _orderedSeries)
            DrawSeries(dc, series, width, height);
    }

    /// <summary>
    /// Horizontal guides plus a full rectangular frame (left/right/top/baseline) so an empty
    /// chart at first open still reads as a chart surface with clear edges.
    /// </summary>
    private static void DrawPlotFrame(DrawingContext dc, double width, double height, Brush gridlineBrush, Brush baselineBrush)
    {
        var grid = new Pen(gridlineBrush, 1.0);
        var baseline = new Pen(baselineBrush, 1.25);
        var frame = new Pen(baselineBrush, 1.0);

        for (var fraction = 0.25; fraction < 1.0; fraction += 0.25)
        {
            var y = Math.Round(height * fraction) + 0.5;
            dc.DrawLine(grid, new Point(0, y), new Point(width, y));
        }

        // Outer plot edges — always visible even when no series samples exist yet.
        dc.DrawLine(frame, new Point(0.5, 0), new Point(0.5, height));
        dc.DrawLine(frame, new Point(width - 0.5, 0), new Point(width - 0.5, height));
        dc.DrawLine(frame, new Point(0, 0.5), new Point(width, 0.5));
        dc.DrawLine(baseline, new Point(0, height - 0.5), new Point(width, height - 0.5));
    }

    private static void DrawSeries(DrawingContext dc, TrendSeries series, double width, double height)
    {
        if (series.Count < 1)
            return;

        var max = series.Maximum ?? Math.Max(1.0, series.ObservedMaximum() * 1.1);
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
            // Closed area with vertical left/right drops so the fill has explicit edges
            // (matches reference area charts: left wall, curve, right wall, baseline).
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

            areaCtx.LineTo(new Point(points[^1].X, height), true, false);
        }

        lineGeometry.Freeze();
        areaGeometry.Freeze();

        dc.DrawGeometry(series.GetOrCreateFill(), null, areaGeometry);
        dc.DrawGeometry(null, series.GetOrCreateLinePen(), lineGeometry);
    }

    /// <summary>
    /// Builds screen-space points for a series. Samples are left-aligned (oldest at x=0) so
    /// the chart grows a clear left edge from the first open instead of a tiny right-side blip.
    /// A single sample is expanded to a short flat segment so fill/stroke still have width.
    /// </summary>
    internal static List<Point> BuildPlotPoints(TrendSeries series, double width, double height, double max)
    {
        var capacity = Math.Max(2, series.Capacity);
        var stepX = width / (capacity - 1);
        var points = new List<Point>(Math.Max(2, series.Count));

        var i = 0;
        foreach (var sample in series.EnumerateOrdered())
        {
            var x = i * stepX;
            var ratio = Math.Clamp(sample / max, 0.0, 1.0);
            // 1px padding top/bottom keeps the stroke fully inside the plot frame.
            var y = height - ratio * (height - 2) - 1;
            points.Add(new Point(x, y));
            i++;
        }

        // One sample: stretch a flat segment one step wide so the area has a visible edge.
        if (points.Count == 1)
            points.Add(new Point(Math.Min(width, points[0].X + stepX), points[0].Y));

        return points;
    }
}
