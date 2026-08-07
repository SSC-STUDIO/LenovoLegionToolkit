using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Ten-point fan curve editor: draws the chart surface, grid, polyline and draggable
/// points, mirroring the WPF FanCurveControl's 10-point non-decreasing curve contract.
/// Points are dragged with the pointer; the curve stays monotonic by default.
/// </summary>
public sealed class FanCurveEditor : Control
{
    private const double PlotPadding = 18.0;
    private const double BottomLabelHeight = 24.0;
    private const double HitRadius = 14.0;
    private const int ValueTickCount = 5;

    private double[] _values = new double[10];
    private int? _dragIndex;

    public FanCurveEditor()
    {
        ClipToBounds = true;
    }

    /// <summary>Smallest editable value. Defaults to 0.</summary>
    public double Minimum { get; set; }

    /// <summary>Largest editable value. Defaults to <see cref="ushort.MaxValue"/>.</summary>
    public double Maximum { get; set; } = ushort.MaxValue;

    /// <summary>
    /// When true, dragging a point also clamps earlier points to it and raises later
    /// points to it so the curve never decreases with temperature (WPF parity).
    /// </summary>
    public bool IsMonotonic { get; set; } = true;

    public int PointCount => _values.Length;

    public IReadOnlyList<double> Values => _values;

    public event EventHandler? CurveChanged;

    public void SetValues(IReadOnlyList<ushort> values)
    {
        if (values is null || values.Count == 0)
            return;

        if (_values.Length != values.Count)
            _values = new double[values.Count];
        for (var index = 0; index < _values.Length; index++)
            _values[index] = Math.Clamp(values[index], Minimum, Maximum);
        InvalidateVisual();
        CurveChanged?.Invoke(this, EventArgs.Empty);
    }

    public double GetPoint(int index) =>
        index >= 0 && index < _values.Length ? _values[index] : 0;

    /// <summary>Maps a curve value onto the plot's Y axis (top = maximum).</summary>
    public static double GetValueY(double value, double minimum, double maximum, double top, double bottom)
    {
        if (maximum <= minimum || bottom <= top)
            return bottom;

        var ratio = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);
        return bottom - ratio * (bottom - top);
    }

    /// <summary>Inverse of <see cref="GetValueY"/> for pointer dragging.</summary>
    public static double GetPointValue(double y, double minimum, double maximum, double top, double bottom)
    {
        if (maximum <= minimum || bottom <= top)
            return minimum;

        var ratio = Math.Clamp((bottom - y) / (bottom - top), 0.0, 1.0);
        return minimum + ratio * (maximum - minimum);
    }

    public static double GetPointX(int index, int count, double left, double right)
    {
        if (count <= 1)
            return left;
        return left + index / (double)(count - 1) * (right - left);
    }

    /// <summary>
    /// Places <paramref name="value"/> at <paramref name="index"/> and keeps the curve
    /// non-decreasing: earlier points are capped at the value and later points are
    /// raised to it (WPF FanCurveControl.VerifyValues parity). Returns the clamped value.
    /// </summary>
    public static double ClampPoint(double[] values, int index, double value, double minimum, double maximum)
    {
        if (values is null || index < 0 || index >= values.Length)
            return value;

        var clamped = Math.Clamp(value, minimum, maximum);
        for (var i = 0; i < index; i++)
        {
            if (values[i] > clamped)
                values[i] = clamped;
        }

        for (var i = index + 1; i < values.Length; i++)
        {
            if (values[i] < clamped)
                values[i] = clamped;
        }

        values[index] = clamped;
        return clamped;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var position = e.GetPosition(this);
        var index = HitTestPoint(position);
        if (index is null)
            return;

        _dragIndex = index;
        e.Pointer.Capture(this);
        UpdatePoint(index.Value, position.Y);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragIndex is not { } index)
            return;

        UpdatePoint(index, e.GetPosition(this).Y);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragIndex is null)
            return;

        _dragIndex = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private int? HitTestPoint(Point position)
    {
        var (left, right, top, bottom) = GetPlotBounds();
        for (var index = 0; index < _values.Length; index++)
        {
            var x = GetPointX(index, _values.Length, left, right);
            var y = GetValueY(_values[index], Minimum, Maximum, top, bottom);
            var distance = Math.Sqrt(Math.Pow(position.X - x, 2) + Math.Pow(position.Y - y, 2));
            if (distance <= HitRadius)
                return index;
        }

        return null;
    }

    private void UpdatePoint(int index, double pointerY)
    {
        var (_, _, top, bottom) = GetPlotBounds();
        var value = GetPointValue(pointerY, Minimum, Maximum, top, bottom);
        if (IsMonotonic)
            ClampPoint(_values, index, value, Minimum, Maximum);
        else
            _values[index] = Math.Clamp(value, Minimum, Maximum);
        InvalidateVisual();
        CurveChanged?.Invoke(this, EventArgs.Empty);
    }

    private (double Left, double Right, double Top, double Bottom) GetPlotBounds()
    {
        var left = PlotPadding;
        var right = Math.Max(left + 1, Bounds.Width - PlotPadding);
        var top = PlotPadding;
        var bottom = Math.Max(top + 1, Bounds.Height - PlotPadding - BottomLabelHeight);
        return (left, right, top, bottom);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= 0 || Bounds.Height <= 0 || _values.Length < 2)
            return;

        var (left, right, top, bottom) = GetPlotBounds();
        var plotWidth = right - left;
        var plotHeight = bottom - top;
        if (plotWidth <= 1 || plotHeight <= 1)
            return;

        var surfaceBrush = ResolveBrush("ChartSurfaceBrush", new SolidColorBrush(Color.FromArgb(0x0C, 0x00, 0x00, 0x00)));
        var surfaceBorderBrush = ResolveBrush("ChartSurfaceBorderBrush", new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x80, 0x80)));
        var gridBrush = ResolveBrush("ChartGridlineBrush", new SolidColorBrush(Color.FromArgb(0x1A, 0x80, 0x80, 0x80)));
        var accentBrush = ResolveBrush("AccentBackgroundBrush", new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x78, 0xD4)));
        var textBrush = ResolveBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF)));
        var accentColor = accentBrush is SolidColorBrush accentSolid ? accentSolid.Color : Colors.DodgerBlue;

        context.DrawRectangle(
            surfaceBrush,
            new Pen(surfaceBorderBrush, 1),
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            12,
            12);

        var gridPen = new Pen(gridBrush, 0.75);
        for (var tick = 0; tick <= ValueTickCount; tick++)
        {
            var y = top + plotHeight * tick / ValueTickCount;
            context.DrawLine(gridPen, new Point(left, y), new Point(right, y));
        }

        var linePoints = new Point[_values.Length];
        for (var index = 0; index < _values.Length; index++)
        {
            linePoints[index] = new Point(
                GetPointX(index, _values.Length, left, right),
                GetValueY(_values[index], Minimum, Maximum, top, bottom));
        }

        var areaGeometry = new StreamGeometry();
        using (var areaContext = areaGeometry.Open())
        {
            areaContext.BeginFigure(new Point(linePoints[0].X, bottom), true);
            foreach (var point in linePoints)
                areaContext.LineTo(point);
            areaContext.LineTo(new Point(linePoints[^1].X, bottom));
            areaContext.EndFigure(true);
        }
        context.DrawGeometry(
            new SolidColorBrush(Color.FromArgb(0x24, accentColor.R, accentColor.G, accentColor.B)),
            null,
            areaGeometry);

        var lineGeometry = new StreamGeometry();
        using (var lineContext = lineGeometry.Open())
        {
            lineContext.BeginFigure(linePoints[0], false);
            for (var index = 1; index < linePoints.Length; index++)
                lineContext.LineTo(linePoints[index]);
        }
        context.DrawGeometry(null, new Pen(accentBrush, 2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), lineGeometry);

        const double pointRadius = 5.0;
        foreach (var point in linePoints)
        {
            context.DrawEllipse(
                accentBrush,
                new Pen(Brushes.White, 1.5),
                new Rect(point.X - pointRadius, point.Y - pointRadius, pointRadius * 2, pointRadius * 2));
        }

        var labelSize = 11.0;
        var minimumLabel = new FormattedText(
            Minimum.ToString("0", CultureInfo.CurrentCulture),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            labelSize,
            textBrush);
        var maximumLabel = new FormattedText(
            Maximum.ToString("0", CultureInfo.CurrentCulture),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default),
            labelSize,
            textBrush);
        context.DrawText(maximumLabel, new Point(left - maximumLabel.Width - 4, top - maximumLabel.Height / 2));
        context.DrawText(minimumLabel, new Point(left - minimumLabel.Width - 4, bottom - minimumLabel.Height / 2));

        for (var index = 0; index < _values.Length; index++)
        {
            var indexLabel = new FormattedText(
                $"{index + 1}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default),
                labelSize,
                textBrush);
            var labelX = GetPointX(index, _values.Length, left, right);
            context.DrawText(indexLabel, new Point(labelX - indexLabel.Width / 2, bottom + 4));
        }
    }

    private IBrush ResolveBrush(string resourceKey, IBrush fallback) =>
        this.TryFindResource(resourceKey, out var value) && value is IBrush brush
            ? brush
            : fallback;
}
