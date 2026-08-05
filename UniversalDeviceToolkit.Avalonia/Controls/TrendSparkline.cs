using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// A named, bounded trend series consumed by <see cref="TrendSparkline"/>.
/// Keeping the samples in the view model makes the chart renderer host-neutral
/// while still allowing one chart to draw several independently scaled metrics.
/// </summary>
public sealed class TrendSparklineSeries
{
    public const int DefaultCapacity = 60;

    public TrendSparklineSeries(
        string key,
        string label,
        IBrush stroke,
        double? maximum = null,
        int capacity = DefaultCapacity)
    {
        Key = key;
        Label = label;
        Stroke = stroke;
        Maximum = maximum;
        Capacity = Math.Max(2, capacity);
    }

    public string Key { get; }
    public string Label { get; }
    public IBrush Stroke { get; }
    public double? Maximum { get; }
    public int Capacity { get; }
    public ObservableCollection<double> Values { get; } = new();

    public void Add(double value)
    {
        if (!double.IsFinite(value))
            return;

        Values.Add(Math.Max(0, value));
        while (Values.Count > Capacity)
            Values.RemoveAt(0);
    }

    public void Clear() => Values.Clear();
}


/// <summary>
/// Small dependency-free trend renderer used by dashboard cards. It intentionally has a stable
/// height so a localized sensor name or a changing sample count cannot reflow the dashboard grid.
/// </summary>
public sealed class TrendSparkline : Control
{
    private const int DefaultCapacity = 60;

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<TrendSparkline, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<int> CapacityProperty =
        AvaloniaProperty.Register<TrendSparkline, int>(nameof(Capacity), DefaultCapacity);

    public static readonly StyledProperty<double?> MaximumProperty =
        AvaloniaProperty.Register<TrendSparkline, double?>(nameof(Maximum));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<TrendSparkline, IBrush?>(nameof(Stroke), Brushes.DodgerBlue);

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<TrendSparkline, IBrush?>(nameof(Fill), new SolidColorBrush(Color.FromArgb(0x35, 0x4F, 0x9D, 0xF7)));

    public static readonly StyledProperty<IEnumerable?> SeriesSourceProperty =
        AvaloniaProperty.Register<TrendSparkline, IEnumerable?>(nameof(SeriesSource));

    private INotifyCollectionChanged? _observableSource;
    private INotifyCollectionChanged? _seriesCollection;
    private readonly List<INotifyCollectionChanged> _seriesSources = new();
    private double _smoothedAutoMaximum = 1.0;
    private readonly Dictionary<string, double> _seriesSmoothedAutoMaximums = new(StringComparer.Ordinal);

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int Capacity
    {
        get => GetValue(CapacityProperty);
        set => SetValue(CapacityProperty, value);
    }

    /// <summary>
    /// Optional fixed Y-axis maximum. When omitted, the maximum adapts gradually
    /// to avoid the chart jumping on every telemetry sample.
    /// </summary>
    public double? Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>
    /// Optional collection of named series. When omitted, <see cref="ItemsSource"/>
    /// remains the backwards-compatible single-series input.
    /// </summary>
    public IEnumerable? SeriesSource
    {
        get => GetValue(SeriesSourceProperty);
        set => SetValue(SeriesSourceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            if (_observableSource is not null)
                _observableSource.CollectionChanged -= OnCollectionChanged;
            _observableSource = change.NewValue is INotifyCollectionChanged source ? source : null;
            if (_observableSource is not null)
                _observableSource.CollectionChanged += OnCollectionChanged;
            InvalidateVisual();
        }
        else if (change.Property == SeriesSourceProperty)
        {
            RebindSeriesSources();
            InvalidateVisual();
        }
        else if (change.Property == BoundsProperty
                 || change.Property == StrokeProperty
                 || change.Property == FillProperty
                 || change.Property == CapacityProperty
                 || change.Property == MaximumProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observableSource is not null)
            _observableSource.CollectionChanged -= OnCollectionChanged;
        _observableSource = null;
        UnbindSeriesSources();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebindSeriesSources();
        InvalidateVisual();
    }

    private void RebindSeriesSources()
    {
        UnbindSeriesSources();

        _seriesCollection = SeriesSource as INotifyCollectionChanged;
        if (_seriesCollection is not null)
            _seriesCollection.CollectionChanged += OnSeriesCollectionChanged;

        foreach (var series in EnumerateSeries())
        {
            series.Values.CollectionChanged += OnCollectionChanged;
            _seriesSources.Add(series.Values);
        }
    }

    private void UnbindSeriesSources()
    {
        if (_seriesCollection is not null)
            _seriesCollection.CollectionChanged -= OnSeriesCollectionChanged;
        _seriesCollection = null;

        foreach (var source in _seriesSources)
            source.CollectionChanged -= OnCollectionChanged;
        _seriesSources.Clear();
    }

    private IEnumerable<TrendSparklineSeries> EnumerateSeries() =>
        SeriesSource?.OfType<TrendSparklineSeries>() ?? Enumerable.Empty<TrendSparklineSeries>();

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var series = EnumerateSeries().ToArray();
        if (series.Length > 0)
        {
            RenderSeries(context, series);
            return;
        }

        var capacity = Math.Max(2, Capacity);
        var values = ItemsSource?.Cast<object>()
            .Select(value => value is IConvertible convertible
                ? Convert.ToDouble(convertible, System.Globalization.CultureInfo.InvariantCulture)
                : double.NaN)
            .Where(double.IsFinite)
            .Select(value => Math.Max(0, value))
            .TakeLast(capacity)
            .ToArray() ?? Array.Empty<double>();
        if (values.Length == 0 || Bounds.Width <= 1 || Bounds.Height <= 1)
            return;

        var maximum = ResolvePlotMaximum(values);
        var points = BuildPlotPoints(values, capacity, Bounds.Width, Bounds.Height, maximum);
        if (points.Count < 2)
            return;

        var lineGeometry = new StreamGeometry();
        var areaGeometry = new StreamGeometry();
        using (var lineContext = lineGeometry.Open())
        using (var areaContext = areaGeometry.Open())
        {
            lineContext.BeginFigure(points[0], false);
            areaContext.BeginFigure(new Point(points[0].X, Bounds.Height), true);
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
            var tailBottom = new Point(Math.Max(points[0].X, lastPoint.X - tailWidth), Bounds.Height);
            var tailHeight = Math.Max(1.0, Bounds.Height - lastPoint.Y);
            areaContext.CubicBezierTo(
                new Point(lastPoint.X - tailWidth * 0.18, lastPoint.Y + tailHeight * 0.30),
                new Point(lastPoint.X - tailWidth * 0.72, Bounds.Height),
                tailBottom);
            areaContext.EndFigure(true);
        }

        context.DrawGeometry(Fill, null, areaGeometry);
        context.DrawGeometry(null, new Pen(Stroke ?? Brushes.Transparent, 1.5), lineGeometry);
    }

    private void RenderSeries(DrawingContext context, IReadOnlyList<TrendSparklineSeries> series)
    {
        var capacity = Math.Max(2, Capacity);
        if (Bounds.Width <= 1 || Bounds.Height <= 1)
            return;

        var plots = new List<(TrendSparklineSeries Series, IReadOnlyList<Point> Points)>();
        foreach (var item in series)
        {
            var values = item.Values
                .Where(double.IsFinite)
                .Select(value => Math.Max(0, value))
                .TakeLast(Math.Min(capacity, item.Capacity))
                .ToArray();
            if (values.Length == 0)
                continue;

            var maximum = ResolvePlotMaximum(item.Key, values, item.Maximum);
            var points = BuildPlotPoints(values, capacity, Bounds.Width, Bounds.Height, maximum);
            if (points.Count >= 2)
                plots.Add((item, points));
        }

        foreach (var plot in plots)
        {
            var areaGeometry = BuildAreaGeometry(plot.Points, Bounds.Height);
            context.DrawGeometry(ResolveFill(plot.Series), null, areaGeometry);
        }

        foreach (var plot in plots)
        {
            var lineGeometry = BuildLineGeometry(plot.Points);
            context.DrawGeometry(null, new Pen(plot.Series.Stroke, 1.5), lineGeometry);
        }
    }

    private static StreamGeometry BuildLineGeometry(IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using var builder = geometry.Open();
        builder.BeginFigure(points[0], false);
        AddSmoothSegments(builder, points);
        return geometry;
    }

    private static StreamGeometry BuildAreaGeometry(IReadOnlyList<Point> points, double height)
    {
        var geometry = new StreamGeometry();
        using var builder = geometry.Open();
        builder.BeginFigure(new Point(points[0].X, height), true);
        builder.LineTo(points[0]);
        AddSmoothSegments(builder, points);

        var last = points[^1];
        var previous = points[^2];
        var step = Math.Max(1.0, last.X - previous.X);
        var tailWidth = Math.Clamp(step * 0.72, 6.0, 20.0);
        var tailBottom = new Point(Math.Max(points[0].X, last.X - tailWidth), height);
        var tailHeight = Math.Max(1.0, height - last.Y);
        builder.CubicBezierTo(
            new Point(last.X - tailWidth * 0.18, last.Y + tailHeight * 0.30),
            new Point(last.X - tailWidth * 0.72, height),
            tailBottom);
        builder.EndFigure(true);
        return geometry;
    }

    private static void AddSmoothSegments(StreamGeometryContext builder, IReadOnlyList<Point> points)
    {
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
            builder.CubicBezierTo(c1, c2, p2);
        }
    }

    private IBrush ResolveFill(TrendSparklineSeries series)
    {
        if (series.Stroke is not SolidColorBrush solid)
            return Fill ?? Brushes.Transparent;

        return new SolidColorBrush(Color.FromArgb(
            0x35,
            solid.Color.R,
            solid.Color.G,
            solid.Color.B));
    }

    private double ResolvePlotMaximum(string key, IReadOnlyList<double> values, double? fixedMaximum)
    {
        if (fixedMaximum is > 0 and var maximum && double.IsFinite(maximum))
            return maximum;

        var observed = Math.Max(1.0, values.Max() * 1.08);
        _seriesSmoothedAutoMaximums.TryGetValue(key, out var smoothed);
        smoothed = smoothed <= 1.0
            ? observed
            : smoothed * 0.85 + observed * 0.15;
        if (smoothed < observed * 0.92)
            smoothed = observed;
        _seriesSmoothedAutoMaximums[key] = smoothed;
        return Math.Max(1.0, smoothed);
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

    private double ResolvePlotMaximum(IReadOnlyList<double> values)
    {
        if (Maximum is { } fixedMaximum && fixedMaximum > 0 && double.IsFinite(fixedMaximum))
            return fixedMaximum;

        var observed = Math.Max(1.0, values.Max() * 1.08);
        _smoothedAutoMaximum = _smoothedAutoMaximum <= 1.0
            ? observed
            : _smoothedAutoMaximum * 0.85 + observed * 0.15;
        if (_smoothedAutoMaximum < observed * 0.92)
            _smoothedAutoMaximum = observed;
        return Math.Max(1.0, _smoothedAutoMaximum);
    }
}
