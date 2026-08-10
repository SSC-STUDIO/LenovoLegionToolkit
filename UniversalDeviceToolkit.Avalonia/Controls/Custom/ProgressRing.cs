using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

/// <summary>
/// Circular progress indicator (WPF-UI ProgressRing replacement).
/// Works with a template that provides <c>PART_Arc</c> (an <see cref="Arc"/>) and
/// <c>PART_Rotation</c> (a <see cref="RotateTransform"/>); when no template is applied
/// the ring is drawn directly in <see cref="Render"/>.
/// </summary>
public class ProgressRing : TemplatedControl
{
    /// <summary>Template part: the arc showing progress.</summary>
    public const string PART_Arc = "PART_Arc";

    /// <summary>Template part: the rotation transform used for indeterminate animation.</summary>
    public const string PART_Rotation = "PART_Rotation";

    private const double DefaultSize = 24;
    private const double IndeterminateRotationDurationSeconds = 1.1;

    private static readonly Color DefaultStrokeColor = Color.Parse("#FF0078D4");

    private Arc? _arcPart;
    private RotateTransform? _rotationPart;
    private readonly RotateTransform _fallbackRotation = new();
    private CancellationTokenSource? _rotationCts;
    private DispatcherTimer? _rotationTimer;

    /// <summary>Defines the <see cref="IsActive"/> property.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ProgressRing, bool>(nameof(IsActive), true);

    /// <summary>Defines the <see cref="IsIndeterminate"/> property.</summary>
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressRing, bool>(nameof(IsIndeterminate), false);

    /// <summary>Defines the <see cref="Minimum"/> property.</summary>
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(Minimum), 0);

    /// <summary>Defines the <see cref="Maximum"/> property.</summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(Maximum), 100);

    /// <summary>Defines the <see cref="Progress"/> property.</summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(Progress), 0);

    static ProgressRing()
    {
        AffectsRender<ProgressRing>(
            IsActiveProperty,
            IsIndeterminateProperty,
            MinimumProperty,
            MaximumProperty,
            ProgressProperty);
    }

    public ProgressRing()
    {
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        RenderTransform = _fallbackRotation;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the ring is active (visible and animating).
    /// </summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the ring runs an indeterminate rotation.
    /// </summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum of the progress range.
    /// </summary>
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum of the progress range.
    /// </summary>
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// Gets or sets the current progress within <see cref="Minimum"/>..<see cref="Maximum"/>.
    /// </summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _arcPart = e.NameScope.Find<Arc>(PART_Arc);
        _rotationPart = e.NameScope.Find<RotateTransform>(PART_Rotation);

        UpdateArc();
        UpdateRotation();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);

        if (size.Width <= 0 && size.Height <= 0)
        {
            var length = Math.Min(DefaultSize, Math.Min(availableSize.Width, availableSize.Height));
            if (double.IsNaN(length) || double.IsInfinity(length))
                length = DefaultSize;
            size = new Size(Math.Max(0, length), Math.Max(0, length));
        }

        return size;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        if (_arcPart is not null || !IsActive)
            return;

        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var color = (Foreground as ISolidColorBrush)?.Color ?? DefaultStrokeColor;
        var thickness = Math.Max(2, Math.Min(bounds.Width, bounds.Height) * 0.08);
        var rect = bounds.Deflate(thickness / 2);

        var trackPen = new Pen(
            new SolidColorBrush(new Color((byte)(color.A * 0.12), color.R, color.G, color.B)),
            thickness,
            null,
            PenLineCap.Round,
            PenLineJoin.Round,
            0);
        context.DrawEllipse(null, trackPen, rect);

        var fraction = GetProgressFraction();
        if (fraction <= 0)
            return;

        var pen = new Pen(new SolidColorBrush(color), thickness, null, PenLineCap.Round, PenLineJoin.Round, 0);

        if (fraction >= 1)
        {
            context.DrawEllipse(null, pen, rect);
            return;
        }

        var center = rect.Center;
        var radius = rect.Width / 2;
        var start = PointOnCircle(center, radius, -90);
        var end = PointOnCircle(center, radius, -90 + 360 * fraction);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start, false);
            ctx.ArcTo(end, new Size(radius, radius), 0, fraction > 0.5, SweepDirection.Clockwise);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsActiveProperty || change.Property == IsIndeterminateProperty)
        {
            UpdateArc();
            UpdateRotation();
        }
        else if (change.Property == ProgressProperty
                 || change.Property == MinimumProperty
                 || change.Property == MaximumProperty)
        {
            UpdateArc();
        }
    }

    private void UpdateArc()
    {
        var fraction = GetProgressFraction();

        if (_arcPart is not null)
        {
            _arcPart.StartAngle = 0;
            _arcPart.SweepAngle = fraction * 360;
        }

        InvalidateVisual();
    }

    private void UpdateRotation()
    {
        StopRotation();

        if (!IsActive || !IsIndeterminate)
        {
            _rotationPart?.SetCurrentValue(RotateTransform.AngleProperty, 0d);
            _fallbackRotation.Angle = 0;
            return;
        }

        // AVALONIA: IClock is internal in 11.3.6, so the Animation-clock path is
        // unavailable; drive the rotation with a DispatcherTimer instead.
        var transform = _rotationPart ?? _fallbackRotation;
        _rotationCts = new CancellationTokenSource();

        _rotationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(IndeterminateRotationDurationSeconds * 1000.0 / 360.0)
        };
        _rotationTimer.Tick += (_, _) =>
        {
            if (_rotationCts.IsCancellationRequested)
                return;
            transform.Angle = (transform.Angle + 1) % 360;
        };
        _rotationTimer.Start();
    }

    private void StopRotation()
    {
        _rotationCts?.Cancel();
        _rotationCts?.Dispose();
        _rotationCts = null;
        _rotationTimer?.Stop();
        _rotationTimer = null;
    }

    private double GetProgressFraction()
    {
        var min = Minimum;
        var max = Maximum;

        if (max <= min)
            return 0;

        return Math.Clamp((Progress - min) / (max - min), 0, 1);
    }

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(
            center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }
}
