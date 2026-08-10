using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Truncation-aware text label:
/// - Text that fits renders normally (honors HorizontalContentAlignment);
/// - When the text overflows its bounds, the tail fades out via an OpacityMask
///   gradient (opaque → transparent) — the same fade language as
///   Behaviors.TextOverflowFadeBehavior, so all truncated text in the app looks
///   identical;
/// - Hovering the control scrolls the text horizontally back and forth to reveal
///   the full content (fade hidden while scrolling); leaving stops the scroll,
///   resets the position, and restores the fade if still truncated.
/// Fully self-contained: the template is built in code (no Generic.xaml needed).
/// </summary>
public class MarqueeTextBlock : ContentControl
{
    private const double FadeWidth = 24.0;
    private const double ScrollPixelsPerSecond = 36.0;
    private const double ScrollTailPadding = 8.0;

    // Tolerance absorbing sub-pixel differences between FormattedText metrics and the
    // layout engine's own width calculation (they can differ by ~1px on some fonts/DPI).
    private const double OverflowEpsilon = 2.0;

    private TextBlock? _textBlock;
    private TranslateTransform? _scrollTransform;
    private DispatcherTimer? _scrollTimer;
    private bool _isOverflowing;

    public MarqueeTextBlock()
    {
        Template = new FuncControlTemplate<MarqueeTextBlock>(BuildTemplate);
        Loaded += (_, _) => UpdateOverflowState();
        SizeChanged += (_, _) => UpdateOverflowState();
        Unloaded += (_, _) => StopScroll();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != ContentProperty || _textBlock is null)
            return;

        _textBlock.Text = Content as string ?? Content?.ToString() ?? string.Empty;
        UpdateOverflowState();

        // Live-updating values (e.g. sensor readings) may change the overflow length
        // mid-scroll; restart so the animation range matches the new text.
        if (_scrollTimer is not null && _isOverflowing)
            StartScroll();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_textBlock is not null)
        {
            _scrollTransform = new TranslateTransform();
            _textBlock.RenderTransform = _scrollTransform;
            _textBlock.Text = Content as string ?? Content?.ToString() ?? string.Empty;
        }

        UpdateOverflowState();
    }

    // ── Template (built in code; no resource dictionary dependency) ─────

    private static Control BuildTemplate(MarqueeTextBlock owner, INameScope _)
    {
        var grid = new Grid { ClipToBounds = true };
        // Route Padding into the template so available-width math (ActualWidth minus
        // Padding) matches the space the text actually gets; without this the fade logic
        // saw the text as clipped by exactly the padding amount.
        grid[!Layoutable.MarginProperty] = new TemplateBinding(TemplatedControl.PaddingProperty);

        var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        grid.Children.Add(text);

        owner._textBlock = text;
        return grid;
    }

    // ── Overflow detection + conditional fade (OpacityMask, app-wide language) ──

    private double MeasureNaturalTextWidth()
    {
        if (_textBlock is null || string.IsNullOrEmpty(_textBlock.Text))
            return 0;

        var formatted = new FormattedText(
            _textBlock.Text,
            CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(_textBlock.FontFamily, _textBlock.FontStyle, _textBlock.FontWeight, _textBlock.FontStretch),
            _textBlock.FontSize,
            Brushes.Black);
        return formatted.Width;
    }

    private void ApplyFadeMask(double containerWidth)
    {
        if (_textBlock is null)
            return;

        var fadeStop = Math.Max(0.0, 1.0 - FadeWidth / containerWidth);
        var mask = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.Black, 0.0),
                new GradientStop(Colors.Black, fadeStop),
                new GradientStop(Colors.Transparent, 1.0)
            }
        };
        _textBlock.OpacityMask = mask;
    }

    private void ClearFadeMask()
    {
        if (_textBlock is not null)
            _textBlock.OpacityMask = null;
    }

    private void UpdateOverflowState()
    {
        if (_textBlock is null)
            return;

        var available = Bounds.Width - Padding.Left - Padding.Right;
        if (available <= 0)
            return; // not arranged yet; Loaded/SizeChanged will re-evaluate

        var overflowing = MeasureNaturalTextWidth() > available + OverflowEpsilon;
        if (overflowing == _isOverflowing)
            return;
        _isOverflowing = overflowing;

        // The fade only ever appears while the text is actually clipped;
        // short values that fit render crisp with no mask at all.
        if (overflowing)
        {
            // Left-align so the text start stays visible; hover-scroll reveals the tail.
            _textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            _textBlock.TextAlignment = TextAlignment.Left;
            ApplyFadeMask(available);
            if (IsPointerOver)
                StartScroll();
        }
        else
        {
            StopScroll();
            ClearFadeMask();
            _textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
            _textBlock.TextAlignment = HorizontalContentAlignment switch
            {
                HorizontalAlignment.Right => TextAlignment.Right,
                HorizontalAlignment.Center => TextAlignment.Center,
                _ => TextAlignment.Left
            };
        }
    }

    // ── Hover scroll (back-and-forth while hovered, reset on leave) ────

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (_isOverflowing)
            StartScroll();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        StopScroll();
    }

    private void StartScroll()
    {
        StopScroll();
        if (_textBlock is null)
            return;

        var available = Bounds.Width - Padding.Left - Padding.Right;
        var overflow = MeasureNaturalTextWidth() - available + ScrollTailPadding;
        if (overflow <= 1)
            return;

        // The fade mask travels with the text while scrolling, which would look
        // wrong — hide it during the scroll; StopScroll restores it if still clipped.
        ClearFadeMask();

        var seconds = Math.Max(1.2, overflow / ScrollPixelsPerSecond);
        var start = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            if (_scrollTransform is null)
                return;

            // Triangle wave: 0 → -overflow → 0 (AutoReverse + RepeatBehavior.Forever equivalent).
            var phase = ((DateTime.UtcNow - start).TotalSeconds / seconds) % 2.0;
            var offset = phase < 1.0 ? phase : 2.0 - phase;
            _scrollTransform.X = -overflow * offset;
        };
        _scrollTimer = timer;
        timer.Start();
    }

    private void StopScroll()
    {
        if (_scrollTimer is not null)
        {
            _scrollTimer.Stop();
            _scrollTimer = null;
        }
        if (_scrollTransform is not null)
            _scrollTransform.X = 0;

        // Restore the fade hint if the text is still clipped after resetting.
        if (_isOverflowing)
        {
            var available = Bounds.Width - Padding.Left - Padding.Right;
            if (available > 0)
                ApplyFadeMask(available);
        }
    }
}
