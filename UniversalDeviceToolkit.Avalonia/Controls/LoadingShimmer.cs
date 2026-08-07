using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Lightweight loading skeleton shown while the dashboard refreshes. Renders a few
/// card-shaped placeholder blocks that pulse with a built-in Avalonia opacity
/// animation. A sweep-gradient shimmer was intentionally not ported: the pulsing
/// pulse is theme-safe, dependency-free and visually subtle.
/// </summary>
public sealed class LoadingShimmer : Control
{
    public static readonly StyledProperty<int> RowCountProperty =
        AvaloniaProperty.Register<LoadingShimmer, int>(nameof(RowCount), 3);

    public static readonly StyledProperty<double> CardHeightProperty =
        AvaloniaProperty.Register<LoadingShimmer, double>(nameof(CardHeight), 120.0);

    private const double CardSpacing = 12.0;
    private const double CardRadius = 12.0;

    private CancellationTokenSource? _pulseCancellation;

    public int RowCount
    {
        get => GetValue(RowCountProperty);
        set => SetValue(RowCountProperty, value);
    }

    public double CardHeight
    {
        get => GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        StartPulse();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        StopPulse();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RowCountProperty
            || change.Property == CardHeightProperty
            || change.Property == BoundsProperty)
        {
            InvalidateVisual();
        }
    }

    private void StartPulse()
    {
        if (_pulseCancellation is not null)
            return;

        var cancellation = new CancellationTokenSource();
        try
        {
            var animation = new Animation
            {
                Duration = TimeSpan.FromSeconds(1.4),
                IterationCount = IterationCount.Infinite,
                FillMode = FillMode.Both,
            };
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0.0),
                Setters = { new Setter(OpacityProperty, 0.35d) },
            });
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0.5),
                Setters = { new Setter(OpacityProperty, 1.0d) },
            });
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters = { new Setter(OpacityProperty, 0.35d) },
            });
            _ = animation.RunAsync(this, cancellation.Token);
            _pulseCancellation = cancellation;
        }
        catch
        {
            cancellation.Dispose();
            // The pulse is decorative; a static skeleton still reads as loading.
        }
    }

    private void StopPulse()
    {
        _pulseCancellation?.Cancel();
        _pulseCancellation?.Dispose();
        _pulseCancellation = null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var width = Bounds.Width;
        if (width <= 0)
            return;

        var cardBackground = ResolveBrush("CardBackgroundBrush", new SolidColorBrush(Color.FromArgb(0xFF, 0x2B, 0x2B, 0x2B)));
        var barBrush = ResolveBrush("ChartTrackBrush", new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)));
        var rows = Math.Max(1, RowCount);
        for (var row = 0; row < rows; row++)
        {
            var top = row * (CardHeight + CardSpacing);
            context.DrawRectangle(
                cardBackground,
                null,
                new Rect(0, top, width, CardHeight),
                CardRadius,
                CardRadius);

            context.DrawRectangle(barBrush, null, new Rect(14, top + 14, width * 0.35, 12), 6, 6);
            context.DrawRectangle(barBrush, null, new Rect(14, top + 40, width - 28, 10), 5, 5);
            context.DrawRectangle(barBrush, null, new Rect(14, top + 58, width * 0.7, 10), 5, 5);
            context.DrawRectangle(barBrush, null, new Rect(14, top + 82, width * 0.85, 10), 5, 5);
        }
    }

    private IBrush ResolveBrush(string resourceKey, IBrush fallback) =>
        this.TryFindResource(resourceKey, out var value) && value is IBrush brush
            ? brush
            : fallback;
}
