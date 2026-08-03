using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;
using UniversalDeviceToolkit.WPF.Behaviors;

namespace UniversalDeviceToolkit.WPF.Controls;

/// <summary>
/// Truncation-aware metric readout = (progress bar + value text) treated as a single unit.
///
/// Layout guarantees:
/// - The bar never shrinks below <see cref="BarMinWidth"/> (default 100px), so even tiny
///   readings keep a clearly visible track.
/// - Bar and value text stay on the same line, tightly adjacent (fixed <see cref="BarTextGap"/>),
///   and are never split apart.
///
/// Overflow behaviour (mirrors <see cref="MarqueeTextBlock"/> so all truncated content in the
/// app shares the same visual language):
/// - When (bar + gap + text) is wider than the available width, the combination is clipped by
///   the viewport and a right-edge fade (opaque → transparent) hints that content is truncated.
///   The fade only ever renders while genuinely clipped; content that fits stays crisp.
/// - Hovering anywhere (bar or text) slides the whole combination left once to reveal the
///   hidden tail, then holds at the end; leaving stops the scroll, resets to the start, and
///   allows the next hover to play again.
///
/// Fully self-contained: the template is built in code (no Generic.xaml needed). The bar keeps
/// the app-wide MetricProgressBarStyle look (passed via <see cref="BarStyle"/>) and the
/// value-animate behaviour; the text uses <see cref="TextStyle"/>.
/// </summary>
public class MarqueeMetricBar : ContentControl
{
    private const double FadeWidth = 24.0;
    private const double ScrollPixelsPerSecond = 36.0;
    private const double BarTextGap = 8.0;

    // Tolerance absorbing sub-pixel differences between FormattedText metrics and the
    // layout engine's own width calculation (they can differ by ~1px on some fonts/DPI).
    private const double OverflowEpsilon = 2.0;

    private Grid? _viewport;
    private StackPanel? _content;
    private ProgressBar? _bar;
    private Label? _label;
    private TranslateTransform? _scrollTransform;
    private Storyboard? _activeStoryboard;
    private bool _isOverflowing;
    private bool _hasScrolledForCurrentHover;

    static MarqueeMetricBar()
    {
        HorizontalAlignmentProperty.OverrideMetadata(
            typeof(MarqueeMetricBar),
            new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
    }

    public MarqueeMetricBar()
    {
        Template = BuildTemplate();
        Loaded += (_, _) => RecalculateLayout();
        SizeChanged += (_, _) => RecalculateLayout();
        Unloaded += (_, _) =>
        {
            _hasScrolledForCurrentHover = false;
            StopScroll();
        };
    }

    // ── Dependency properties ─────────────────────────────────────────

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(MarqueeMetricBar),
        new FrameworkPropertyMetadata(0.0, OnRangeValueChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(MarqueeMetricBar),
        new FrameworkPropertyMetadata(100.0, OnRangeValueChanged));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(MarqueeMetricBar),
        new FrameworkPropertyMetadata(0.0, OnRangeValueChanged));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(MarqueeMetricBar),
        new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Style applied to the inner ProgressBar (the app-wide MetricProgressBarStyle).</summary>
    public static readonly DependencyProperty BarStyleProperty = DependencyProperty.Register(
        nameof(BarStyle), typeof(Style), typeof(MarqueeMetricBar),
        new FrameworkPropertyMetadata(null, OnBarStyleChanged));

    public Style? BarStyle
    {
        get => (Style?)GetValue(BarStyleProperty);
        set => SetValue(BarStyleProperty, value);
    }

    /// <summary>Style applied to the value Label (the app-wide StatValueStyle).</summary>
    public static readonly DependencyProperty TextStyleProperty = DependencyProperty.Register(
        nameof(TextStyle), typeof(Style), typeof(MarqueeMetricBar),
        new FrameworkPropertyMetadata(null, OnTextStyleChanged));

    public Style? TextStyle
    {
        get => (Style?)GetValue(TextStyleProperty);
        set => SetValue(TextStyleProperty, value);
    }

    /// <summary>Lower bound for the bar width so small readings stay clearly visible.</summary>
    public static readonly DependencyProperty BarMinWidthProperty = DependencyProperty.Register(
        nameof(BarMinWidth), typeof(double), typeof(MarqueeMetricBar),
        new FrameworkPropertyMetadata(100.0, OnLayoutParameterChanged));

    public double BarMinWidth
    {
        get => (double)GetValue(BarMinWidthProperty);
        set => SetValue(BarMinWidthProperty, value);
    }

    /// <summary>Upper bound for the bar width on wide surfaces (mirrors the old MaxWidth switch).</summary>
    public static readonly DependencyProperty BarMaxWidthProperty = DependencyProperty.Register(
        nameof(BarMaxWidth), typeof(double), typeof(MarqueeMetricBar),
        new FrameworkPropertyMetadata(260.0, OnLayoutParameterChanged));

    public double BarMaxWidth
    {
        get => (double)GetValue(BarMaxWidthProperty);
        set => SetValue(BarMaxWidthProperty, value);
    }

    // ── Template ──────────────────────────────────────────────────────

    private static ControlTemplate BuildTemplate()
    {
        var template = new ControlTemplate(typeof(MarqueeMetricBar));

        var root = new FrameworkElementFactory(typeof(DockPanel));
        root.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(PaddingProperty));
        root.SetValue(DockPanel.LastChildFillProperty, true);
        // Keep the whole metric row hit-testable. Without a transparent surface,
        // WPF only reports hover over painted children such as the blue fill.
        root.SetValue(Panel.BackgroundProperty, Brushes.Transparent);

        var viewport = new FrameworkElementFactory(typeof(Grid));
        viewport.Name = "PART_Viewport";
        viewport.SetValue(Grid.ClipToBoundsProperty, true);
        viewport.SetValue(Panel.BackgroundProperty, Brushes.Transparent);

        var content = new FrameworkElementFactory(typeof(StackPanel));
        content.Name = "PART_Content";
        content.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var bar = new FrameworkElementFactory(typeof(ProgressBar));
        bar.Name = "PART_Bar";
        bar.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.AppendChild(bar);

        var text = new FrameworkElementFactory(typeof(Label));
        text.Name = "PART_Text";
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        text.SetValue(FrameworkElement.MarginProperty, new Thickness(BarTextGap, 0, 0, 0));

        text.SetValue(DockPanel.DockProperty, Dock.Right);
        root.AppendChild(text);
        viewport.AppendChild(content);
        root.AppendChild(viewport);
        template.VisualTree = root;
        template.Seal();
        return template;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _viewport = Template.FindName("PART_Viewport", this) as Grid;
        _content = Template.FindName("PART_Content", this) as StackPanel;
        _bar = Template.FindName("PART_Bar", this) as ProgressBar;
        _label = Template.FindName("PART_Text", this) as Label;

        if (_content is not null)
        {
            _scrollTransform = new TranslateTransform();
            _content.RenderTransform = _scrollTransform;
        }

        if (_bar is not null)
        {
            _bar.MinWidth = BarMinWidth;
            // The style supplies the visual chrome (height, track/indicator brushes, corner radius);
            // width is owned by this control's layout pass.
            _bar.MaxWidth = double.PositiveInfinity;
            _bar.Margin = new Thickness(0);
            if (BarStyle is not null)
                _bar.Style = BarStyle;
            SyncRangeToBar();
            // Preserve the value-change animation the standalone bars had.
            Interaction.GetBehaviors(_bar).Add(new ProgressBarAnimateBehavior());
        }

        if (_label is not null)
        {
            if (TextStyle is not null)
                _label.Style = TextStyle;
            _label.Content = Text;
        }

        RecalculateLayout();
    }

    // ── Property-change plumbing ──────────────────────────────────────

    private static void OnRangeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (MarqueeMetricBar)d;
        self.SyncRangeToBar();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (MarqueeMetricBar)d;
        if (self._label is not null)
        {
            self._label.Content = self.Text;
            self.RecalculateLayout();
        }
    }

    private static void OnBarStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (MarqueeMetricBar)d;
        if (self._bar is not null && self.BarStyle is not null)
            self._bar.Style = self.BarStyle;
    }

    private static void OnTextStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (MarqueeMetricBar)d;
        if (self._label is not null && self.TextStyle is not null)
            self._label.Style = self.TextStyle;
    }

    private static void OnLayoutParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (MarqueeMetricBar)d;
        if (self._bar is not null)
            self._bar.MinWidth = self.BarMinWidth;
        self.RecalculateLayout();
    }

    private void SyncRangeToBar()
    {
        if (_bar is null)
            return;

        _bar.Minimum = Minimum;
        _bar.Maximum = Maximum;
        _bar.Value = Value;
    }

    // ── Overflow detection + conditional fade ─────────────────────────

    private string GetLabelText()
    {
        if (_label is null)
            return string.Empty;
        return _label.Content as string ?? _label.Content?.ToString() ?? string.Empty;
    }

    private double MeasureNaturalTextWidth()
    {
        var text = GetLabelText();
        if (_label is null || string.IsNullOrEmpty(text))
            return 0;

        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(_label.FontFamily, _label.FontStyle, _label.FontWeight, _label.FontStretch),
            _label.FontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        return formatted.Width + _label.Padding.Left + _label.Padding.Right;
    }

    private double AvailableWidth => ActualWidth - Padding.Left - Padding.Right;

    private double GetBarViewportWidth(double fallback)
    {
        var width = _viewport?.ActualWidth ?? 0;
        return width > 0 ? width : fallback;
    }

    private void RecalculateLayout()
    {
        if (_viewport is null || _content is null || _bar is null || _label is null)
            return;

        var available = AvailableWidth;
        if (available <= 0)
            return; // not arranged yet; Loaded/SizeChanged will re-evaluate

        var textWidth = MeasureNaturalTextWidth();
        var barViewportWidth = Math.Max(0, available - BarTextGap - textWidth);
        var barMax = Math.Max(BarMaxWidth, BarMinWidth);
        // The label has its own non-clipped DockPanel slot. If the bar is too wide for
        // the remaining slot, only the bar viewport can scroll.
        var barWidth = Math.Clamp(Math.Max(BarMinWidth, barViewportWidth), BarMinWidth, barMax);
        _bar.Width = barWidth;

        var viewportWidth = GetBarViewportWidth(barViewportWidth);
        var overflowing = barWidth > viewportWidth + OverflowEpsilon;
        _isOverflowing = overflowing;

        // Re-layouts and live sensor updates must always reset the marquee position.
        StopScroll();

        if (overflowing)
        {
            if (!IsMouseOver)
            {
                _hasScrolledForCurrentHover = false;
                ApplyFadeMask(viewportWidth);
            }
            else if (!_hasScrolledForCurrentHover)
            {
                _hasScrolledForCurrentHover = StartScroll();
            }
            else
            {
                // A live value/layout update must not replay the animation while the
                // pointer remains over the metric. Keep the already-revealed tail visible.
                SetScrollToEnd();
            }
        }
        else
        {
            if (!IsMouseOver)
                _hasScrolledForCurrentHover = false;
            ClearFadeMask();
        }
    }

    private void ApplyFadeMask(double containerWidth)
    {
        if (_viewport is null || containerWidth <= 0)
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
        _viewport.OpacityMask = mask;
    }

    private void ClearFadeMask()
    {
        if (_viewport is not null)
            _viewport.OpacityMask = null;
    }

    // ── Hover scroll (back-and-forth while hovered, reset on leave) ────

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (_isOverflowing && !_hasScrolledForCurrentHover)
            _hasScrolledForCurrentHover = StartScroll();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hasScrolledForCurrentHover = false;
        StopScroll();
    }

    private bool StartScroll()
    {
        StopScroll();
        if (_content is null || _bar is null)
            return false;

        var fallback = Math.Max(0, AvailableWidth - BarTextGap - MeasureNaturalTextWidth());
        var available = GetBarViewportWidth(fallback);
        // Never translate past the exact right edge of the bar. The previous
        // tail padding could move a short/low-value bar completely outside the
        // viewport, making the metric look empty while it was hovered.
        if (available <= OverflowEpsilon)
            return false;

        var overflow = Math.Max(0, _bar.Width - available);
        if (overflow <= 1)
            return false;

        // The fade mask travels with the content while scrolling, which would look
        // wrong — hide it during the scroll; StopScroll restores it if still clipped.
        ClearFadeMask();

        var seconds = Math.Max(1.2, overflow / ScrollPixelsPerSecond);
        var animation = new DoubleAnimation
        {
            From = 0.0,
            To = -overflow,
            Duration = new Duration(TimeSpan.FromSeconds(seconds)),
            AutoReverse = false,
            RepeatBehavior = new RepeatBehavior(1.0),
            FillBehavior = FillBehavior.HoldEnd,
            // Linear speed keeps the content readable while it moves.
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        Storyboard.SetTarget(animation, _content);
        Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        storyboard.Begin(this, true);
        _activeStoryboard = storyboard;
        return true;
    }

    private void SetScrollToEnd()
    {
        if (_scrollTransform is null || _bar is null)
            return;

        var fallback = Math.Max(0, AvailableWidth - BarTextGap - MeasureNaturalTextWidth());
        var available = GetBarViewportWidth(fallback);
        if (available <= OverflowEpsilon)
            return;

        var overflow = Math.Max(0, _bar.Width - available);
        if (overflow <= 1)
        {
            _scrollTransform.X = 0;
            ApplyFadeMask(available);
            return;
        }

        _scrollTransform.X = -overflow;
        ClearFadeMask();
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

        // Restore the fade hint if the content is still clipped after resetting.
        if (_isOverflowing)
        {
            var fallback = Math.Max(0, AvailableWidth - BarTextGap - MeasureNaturalTextWidth());
            var available = GetBarViewportWidth(fallback);
            if (available > 0)
                ApplyFadeMask(available);
        }
    }
}
