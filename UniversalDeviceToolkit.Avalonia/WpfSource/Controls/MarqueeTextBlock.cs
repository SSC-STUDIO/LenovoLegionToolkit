using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UniversalDeviceToolkit.WPF.Controls;

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
public class MarqueeTextBlock : Label
{
    private const double FadeWidth = 24.0;
    private const double ScrollPixelsPerSecond = 36.0;
    private const double ScrollTailPadding = 8.0;

    // Tolerance absorbing sub-pixel differences between FormattedText metrics and the
    // layout engine's own width calculation (they can differ by ~1px on some fonts/DPI).
    private const double OverflowEpsilon = 2.0;

    private TextBlock? _textBlock;
    private TranslateTransform? _scrollTransform;
    private Storyboard? _activeStoryboard;
    private bool _isOverflowing;

    public MarqueeTextBlock()
    {
        Template = BuildTemplate();
        Loaded += (_, _) => UpdateOverflowState();
        SizeChanged += (_, _) => UpdateOverflowState();
        Unloaded += (_, _) => StopScroll();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        if (_textBlock is not null)
        {
            _textBlock.Text = newContent as string ?? newContent?.ToString() ?? string.Empty;
            UpdateOverflowState();

            // Live-updating values (e.g. sensor readings) may change the overflow length
            // mid-scroll; restart so the animation range matches the new text.
            if (_activeStoryboard is not null && _isOverflowing)
                StartScroll();
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _textBlock = Template.FindName("PART_Text", this) as TextBlock;

        if (_textBlock is not null)
        {
            _scrollTransform = new TranslateTransform();
            _textBlock.RenderTransform = _scrollTransform;
            _textBlock.Text = Content as string ?? Content?.ToString() ?? string.Empty;
        }

        UpdateOverflowState();
    }

    // ── Template (built in code; no resource dictionary dependency) ─────

    private static ControlTemplate BuildTemplate()
    {
        var template = new ControlTemplate(typeof(MarqueeTextBlock));

        var grid = new FrameworkElementFactory(typeof(Grid));
        grid.SetValue(Grid.ClipToBoundsProperty, true);
        // Route Label.Padding into the template so available-width math (ActualWidth minus
        // Padding) matches the space the text actually gets; without this the fade logic
        // saw the text as clipped by exactly the padding amount.
        grid.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(PaddingProperty));

        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.Name = "PART_Text";
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        grid.AppendChild(text);

        template.VisualTree = grid;
        template.Seal();
        return template;
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
            Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        return formatted.Width;
    }

    private void ApplyFadeMask(double containerWidth)
    {
        if (_textBlock is null)
            return;

        var fadeStop = Math.Max(0.0, 1.0 - FadeWidth / containerWidth);
        var mask = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            GradientStops =
            {
                new GradientStop(Colors.Black, 0.0),
                new GradientStop(Colors.Black, fadeStop),
                new GradientStop(Colors.Transparent, 1.0)
            }
        };
        mask.Freeze();
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

        var available = ActualWidth - Padding.Left - Padding.Right;
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
            if (IsMouseOver)
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

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (_isOverflowing)
            StartScroll();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        StopScroll();
    }

    private void StartScroll()
    {
        StopScroll();
        if (_textBlock is null)
            return;

        var available = ActualWidth - Padding.Left - Padding.Right;
        var overflow = MeasureNaturalTextWidth() - available + ScrollTailPadding;
        if (overflow <= 1)
            return;

        // The fade mask travels with the text while scrolling, which would look
        // wrong — hide it during the scroll; StopScroll restores it if still clipped.
        ClearFadeMask();

        var seconds = Math.Max(1.2, overflow / ScrollPixelsPerSecond);
        var animation = new DoubleAnimation
        {
            From = 0.0,
            To = -overflow,
            Duration = new Duration(TimeSpan.FromSeconds(seconds)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            // Linear speed keeps the text readable while it moves.
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        Storyboard.SetTarget(animation, _textBlock);
        Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        storyboard.Begin(this, true);
        _activeStoryboard = storyboard;
    }

    private void StopScroll()
    {
        if (_activeStoryboard is not null)
        {
            _activeStoryboard.Stop(this);
            _activeStoryboard = null;
        }
        if (_scrollTransform is not null)
            _scrollTransform.X = 0;

        // Restore the fade hint if the text is still clipped after resetting.
        if (_isOverflowing)
        {
            var available = ActualWidth - Padding.Left - Padding.Right;
            if (available > 0)
                ApplyFadeMask(available);
        }
    }
}
