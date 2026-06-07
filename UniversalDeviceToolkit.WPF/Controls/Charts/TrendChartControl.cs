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

    public TrendSeries(string key, int capacity)
    {
        Key = key;
        _samples = new double[Math.Max(2, capacity)];
    }

    public string Key { get; }

    /// <summary>Stroke / fill color for this series.</summary>
    public Color Color { get; set; }

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

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 1 || height <= 1)
            return;

        // Subtle baseline so an empty chart still reads as a chart surface.
        var baseline = new Pen(new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)), 1);
        baseline.Freeze();
        dc.DrawLine(baseline, new Point(0, height - 0.5), new Point(width, height - 0.5));

        foreach (var series in _orderedSeries)
            DrawSeries(dc, series, width, height);
    }

    private static void DrawSeries(DrawingContext dc, TrendSeries series, double width, double height)
    {
        if (series.Count < 2)
            return;

        var max = series.Maximum ?? Math.Max(1.0, series.ObservedMaximum() * 1.1);
        if (max <= 0)
            max = 1.0;

        // Map samples across the full width using the buffer capacity so the line scrolls
        // in from the right as data accumulates.
        var capacity = Math.Max(2, series.Capacity);
        var stepX = width / (capacity - 1);
        var startIndex = capacity - series.Count;

        var lineGeometry = new StreamGeometry();
        var areaGeometry = new StreamGeometry();

        using (var lineCtx = lineGeometry.Open())
        using (var areaCtx = areaGeometry.Open())
        {
            var i = 0;
            Point first = default;
            Point last = default;
            foreach (var sample in series.EnumerateOrdered())
            {
                var x = (startIndex + i) * stepX;
                var ratio = Math.Clamp(sample / max, 0.0, 1.0);
                var y = height - ratio * (height - 2) - 1;
                var point = new Point(x, y);

                if (i == 0)
                {
                    first = point;
                    lineCtx.BeginFigure(point, false, false);
                    areaCtx.BeginFigure(new Point(x, height), true, true);
                    areaCtx.LineTo(point, true, false);
                }
                else
                {
                    lineCtx.LineTo(point, true, true);
                    areaCtx.LineTo(point, true, true);
                }

                last = point;
                i++;
            }

            // Close the area polygon back down to the baseline.
            areaCtx.LineTo(new Point(last.X, height), true, false);
            areaCtx.LineTo(new Point(first.X, height), true, false);
        }

        lineGeometry.Freeze();
        areaGeometry.Freeze();

        var fill = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(72, series.Color.R, series.Color.G, series.Color.B), 0));
        fill.GradientStops.Add(new GradientStop(Color.FromArgb(8, series.Color.R, series.Color.G, series.Color.B), 1));
        fill.Freeze();

        var stroke = new Pen(new SolidColorBrush(series.Color), 1.6)
        {
            LineJoin = PenLineJoin.Round
        };
        stroke.Freeze();

        dc.DrawGeometry(fill, null, areaGeometry);
        dc.DrawGeometry(null, stroke, lineGeometry);
    }
}
