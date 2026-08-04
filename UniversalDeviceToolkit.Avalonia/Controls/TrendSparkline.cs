using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Small dependency-free trend renderer used by dashboard cards. It intentionally has a stable
/// height so a localized sensor name or a changing sample count cannot reflow the dashboard grid.
/// </summary>
public sealed class TrendSparkline : Control
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<TrendSparkline, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<TrendSparkline, IBrush?>(nameof(Stroke), Brushes.DodgerBlue);

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<TrendSparkline, IBrush?>(nameof(Fill), new SolidColorBrush(Color.FromArgb(0x35, 0x4F, 0x9D, 0xF7)));

    private INotifyCollectionChanged? _observableSource;

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
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
        else if (change.Property == BoundsProperty || change.Property == StrokeProperty || change.Property == FillProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observableSource is not null)
            _observableSource.CollectionChanged -= OnCollectionChanged;
        _observableSource = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var values = ItemsSource?.Cast<object>()
            .Select(value => value is IConvertible convertible
                ? Convert.ToDouble(convertible, System.Globalization.CultureInfo.InvariantCulture)
                : double.NaN)
            .Where(double.IsFinite)
            .ToArray();
        if (values is null || values.Length < 2 || Bounds.Width <= 1 || Bounds.Height <= 1)
            return;

        var min = values.Min();
        var max = values.Max();
        var range = Math.Abs(max - min) < 0.001 ? 1 : max - min;
        var points = values.Select((value, index) => new Point(
            index * Bounds.Width / (values.Length - 1),
            Bounds.Height - ((value - min) / range * Math.Max(1, Bounds.Height - 4)) - 2)).ToArray();

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(new Point(0, Bounds.Height), true);
            foreach (var point in points)
                geometryContext.LineTo(point);
            geometryContext.LineTo(new Point(Bounds.Width, Bounds.Height));
            geometryContext.EndFigure(true);
        }
        context.DrawGeometry(Fill, null, geometry);

        var pen = new Pen(Stroke, 1.5);
        for (var index = 1; index < points.Length; index++)
            context.DrawLine(pen, points[index - 1], points[index]);
    }
}
