using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// A fixed-capacity ring buffer of trend samples consumed by <see cref="TrendChart"/>.
/// The oldest sample is trimmed once the capacity is exceeded and non-finite or
/// negative values are normalized to zero so renderers never spike out of bounds.
/// </summary>
public sealed class TrendChartSeries
{
    private readonly double[] _samples;
    private int _count;
    private int _head;
    private double _smoothedAutoMaximum = 1.0;

    public TrendChartSeries(
        string key,
        IBrush stroke,
        int capacity,
        double? maximum = null)
    {
        Key = key;
        Stroke = stroke;
        Maximum = maximum;
        _samples = new double[Math.Max(2, capacity)];
    }

    public string Key { get; }
    public IBrush Stroke { get; }

    /// <summary>
    /// Upper bound used to normalize this series into the 0..1 plot area. When null,
    /// the chart auto-scales to the maximum observed sample with a small headroom.
    /// </summary>
    public double? Maximum { get; }

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
        for (var index = 0; index < _count; index++)
        {
            var position = (_head - _count + index + _samples.Length * 2) % _samples.Length;
            yield return _samples[position];
        }
    }

    public double ObservedMaximum()
    {
        var maximum = 0.0;
        foreach (var sample in EnumerateOrdered())
        {
            if (sample > maximum)
                maximum = sample;
        }

        return maximum;
    }

    /// <summary>
    /// Resolves the Y-axis maximum used to normalize this series. A fixed
    /// <see cref="Maximum"/> wins; otherwise the auto-scale eases toward the
    /// observed maximum so the plot does not jump on every sample.
    /// </summary>
    public double ResolvePlotMaximum()
    {
        if (Maximum is { } fixedMaximum && fixedMaximum > 0 && double.IsFinite(fixedMaximum))
            return fixedMaximum;

        if (_count < 1)
            return Math.Max(1.0, _smoothedAutoMaximum);

        var observed = Math.Max(1.0, ObservedMaximum() * 1.08);
        _smoothedAutoMaximum = _smoothedAutoMaximum <= 1.0
            ? observed
            : _smoothedAutoMaximum * 0.85 + observed * 0.15;
        if (_smoothedAutoMaximum < observed * 0.92)
            _smoothedAutoMaximum = observed;
        return Math.Max(1.0, _smoothedAutoMaximum);
    }
}

/// <summary>
/// Self-drawn multi-series line chart with X/Y axes, soft gridlines and percentage
/// scale labels. Series are appended through <see cref="AddSample"/> and the visual is
/// invalidated on each push; samples are right-aligned against a fixed-capacity grid.
/// </summary>
public sealed class TrendChart : Control
{
    private const int DefaultCapacity = 60;
    private readonly Dictionary<string, TrendChartSeries> _series = new(StringComparer.Ordinal);
    private readonly List<TrendChartSeries> _orderedSeries = new();

    public static readonly StyledProperty<int> CapacityProperty =
        AvaloniaProperty.Register<TrendChart, int>(nameof(Capacity), DefaultCapacity);

    public static readonly StyledProperty<IBrush?> GridlineBrushProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(GridlineBrush));

    public static readonly StyledProperty<IBrush?> BaselineBrushProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(BaselineBrush));

    public static readonly StyledProperty<IBrush?> ScaleLabelBrushProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(ScaleLabelBrush));

    public static readonly StyledProperty<Thickness> PlotPaddingProperty =
        AvaloniaProperty.Register<TrendChart, Thickness>(
            nameof(PlotPadding),
            new Thickness(30, 6, 6, 8));

    public int Capacity
    {
        get => GetValue(CapacityProperty);
        set => SetValue(CapacityProperty, value);
    }

    public IBrush? GridlineBrush
    {
        get => GetValue(GridlineBrushProperty);
        set => SetValue(GridlineBrushProperty, value);
    }

    public IBrush? BaselineBrush
    {
        get => GetValue(BaselineBrushProperty);
        set => SetValue(BaselineBrushProperty, value);
    }

    public IBrush? ScaleLabelBrush
    {
        get => GetValue(ScaleLabelBrushProperty);
        set => SetValue(ScaleLabelBrushProperty, value);
    }

    /// <summary>Padding around the plot area so scale labels and strokes do not clip.</summary>
    public Thickness PlotPadding
    {
        get => GetValue(PlotPaddingProperty);
        set => SetValue(PlotPaddingProperty, value);
    }

    /// <summary>Registers (or replaces) a named series and returns it for inspection.</summary>
    public TrendChartSeries DefineSeries(string key, IBrush stroke, double? maximum = null, int? capacity = null)
    {
        var series = new TrendChartSeries(key, stroke, capacity ?? Capacity, maximum);
        if (_series.TryGetValue(key, out var existing))
            _orderedSeries.Remove(existing);

        _series[key] = series;
        _orderedSeries.Add(series);
        InvalidateVisual();
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

    public bool HasSamples => _orderedSeries.Any(series => series.Count > 0);

    public TrendChartSeries? GetSeries(string key) =>
        _series.TryGetValue(key, out var series) ? series : null;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CapacityProperty
            || change.Property == GridlineBrushProperty
            || change.Property == BaselineBrushProperty
            || change.Property == ScaleLabelBrushProperty
            || change.Property == PlotPaddingProperty
            || change.Property == BoundsProperty)
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var padding = PlotPadding;
        var width = Bounds.Width - padding.Left - padding.Right;
        var height = Bounds.Height - padding.Top - padding.Bottom;
        if (width <= 1 || height <= 1)
            return;

        using (context.PushTransform(Matrix.CreateTranslation(padding.Left, padding.Top)))
        {
            DrawGridlines(context, width, height);

            foreach (var series in _orderedSeries)
                DrawSeries(context, series, width, height, drawFill: true, drawLine: false);

            foreach (var series in _orderedSeries)
                DrawSeries(context, series, width, height, drawFill: false, drawLine: true);
        }
    }

    private void DrawGridlines(DrawingContext context, double width, double height)
    {
        var gridline = new Pen(GridlineBrush ?? new SolidColorBrush(Colors.Transparent), 0.5);
        var baseline = new Pen(BaselineBrush ?? new SolidColorBrush(Colors.Transparent), 1.0);
        var labelBrush = ScaleLabelBrush ?? Brushes.Gray;

        var scaleMarks = new (double Fraction, string Label)[]
        {
            (0.0, "100%"),
            (0.25, "75%"),
            (0.50, "50%"),
            (0.75, "25%"),
        };

        foreach (var (fraction, label) in scaleMarks)
        {
            var y = Math.Round(height * fraction) + 0.5;

            if (fraction > 0.01 && fraction < 0.99)
                context.DrawLine(gridline, new Point(0, y), new Point(width, y));

            var formatted = new FormattedText(
                label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default),
                9,
                labelBrush);
            context.DrawText(formatted, new Point(2, y - formatted.Height / 2));
        }

        context.DrawLine(baseline, new Point(0, height - 0.5), new Point(width, height - 0.5));
        context.DrawLine(gridline, new Point(0.5, 0), new Point(0.5, height));
    }

    private static void DrawSeries(
        DrawingContext context,
        TrendChartSeries series,
        double width,
        double height,
        bool drawFill,
        bool drawLine)
    {
        if (series.Count < 1)
            return;

        var maximum = series.ResolvePlotMaximum();
        if (maximum <= 0)
            maximum = 1.0;

        var points = BuildPlotPoints(
            series.EnumerateOrdered().ToArray(),
            series.Capacity,
            width,
            height,
            maximum);
        if (points.Count < 2)
            return;

        var lineGeometry = new StreamGeometry();
        var areaGeometry = new StreamGeometry();
        using (var lineContext = lineGeometry.Open())
        using (var areaContext = areaGeometry.Open())
        {
            lineContext.BeginFigure(points[0], false);
            areaContext.BeginFigure(new Point(points[0].X, height), true);
            areaContext.LineTo(points[0]);

            for (var index = 0; index < points.Count - 1; index++)
            {
                var p0 = points[Math.Max(0, index - 1)];
                var p1 = points[index];
                var p2 = points[index + 1];
                var p3 = points[Math.Min(points.Count - 1, index + 2)];
                var c1 = new Point(
                    p1.X + (p2.X - p0.X) / 6.0,
                    p1.Y + (p2.Y - p0.Y) / 6.0);
                var c2 = new Point(
                    p2.X - (p3.X - p1.X) / 6.0,
                    p2.Y - (p3.Y - p1.Y) / 6.0);

                lineContext.CubicBezierTo(c1, c2, p2);
                areaContext.CubicBezierTo(c1, c2, p2);
            }

            var lastPoint = points[^1];
            var previousPoint = points[^2];
            var sampleStep = Math.Max(1.0, lastPoint.X - previousPoint.X);
            var tailWidth = Math.Clamp(sampleStep * 0.72, 6.0, 20.0);
            var tailBottom = new Point(Math.Max(points[0].X, lastPoint.X - tailWidth), height);
            var tailHeight = Math.Max(1.0, height - lastPoint.Y);
            areaContext.CubicBezierTo(
                new Point(lastPoint.X - tailWidth * 0.18, lastPoint.Y + tailHeight * 0.30),
                new Point(lastPoint.X - tailWidth * 0.72, height),
                tailBottom);
            areaContext.EndFigure(true);
        }

        if (drawFill)
            context.DrawGeometry(ResolveFill(series), null, areaGeometry);
        if (drawLine)
            context.DrawGeometry(null, ResolveLinePen(series), lineGeometry);
    }

    /// <summary>
    /// Maps the newest sample to the right edge of a fixed-capacity history grid.
    /// Keeping this deterministic prevents the line and fill from shifting as the
    /// history grows from one sample to the full window.
    /// </summary>
    public static IReadOnlyList<Point> BuildPlotPoints(
        IReadOnlyList<double> values,
        int capacity,
        double width,
        double height,
        double maximum)
    {
        capacity = Math.Max(2, capacity);
        maximum = maximum > 0 && double.IsFinite(maximum) ? maximum : 1.0;
        if (values.Count == 0 || width <= 0 || height <= 0)
            return Array.Empty<Point>();

        var count = Math.Min(capacity, values.Count);
        var start = values.Count - count;
        var stepX = width / (capacity - 1);
        var points = new List<Point>(Math.Max(2, count));
        for (var index = 0; index < count; index++)
        {
            var value = values[start + index];
            var ratio = Math.Clamp(value / maximum, 0.0, 1.0);
            points.Add(new Point(
                (capacity - count + index) * stepX,
                height - ratio * Math.Max(1, height - 2) - 1));
        }

        if (points.Count == 1)
        {
            var right = points[0];
            points.Insert(0, new Point(Math.Max(0, right.X - stepX), right.Y));
        }

        return points;
    }

    private static IBrush ResolveFill(TrendChartSeries series)
    {
        if (series.Stroke is not SolidColorBrush solid)
            return new SolidColorBrush(Color.FromArgb(0x1A, 0x88, 0x88, 0x88));

        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(76, solid.Color.R, solid.Color.G, solid.Color.B), 0.0),
                new GradientStop(Color.FromArgb(42, solid.Color.R, solid.Color.G, solid.Color.B), 0.48),
                new GradientStop(Color.FromArgb(10, solid.Color.R, solid.Color.G, solid.Color.B), 1.0),
            },
        };
    }

    private static Pen ResolveLinePen(TrendChartSeries series) =>
        new(series.Stroke ?? Brushes.Transparent, 1.15)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
}
