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
using Avalonia.Styling;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Controls;

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
    private TextBlock? _label;
    private TranslateTransform? _scrollTransform;
    private DispatcherTimer? _scrollTimer;
    private bool _isOverflowing;
    private bool _hasScrolledForCurrentHover;

    static MarqueeMetricBar()
    {
        HorizontalAlignmentProperty.OverrideDefaultValue<MarqueeMetricBar>(HorizontalAlignment.Stretch);

        ValueProperty.Changed.AddClassHandler<MarqueeMetricBar>((bar, _) => bar.SyncRangeToBar());
        MaximumProperty.Changed.AddClassHandler<MarqueeMetricBar>((bar, _) => bar.SyncRangeToBar());
        MinimumProperty.Changed.AddClassHandler<MarqueeMetricBar>((bar, _) => bar.SyncRangeToBar());
        TextProperty.Changed.AddClassHandler<MarqueeMetricBar>(OnTextChanged);
        BarStyleProperty.Changed.AddClassHandler<MarqueeMetricBar>(OnBarStyleChanged);
        TextStyleProperty.Changed.AddClassHandler<MarqueeMetricBar>(OnTextStyleChanged);
        BarMinWidthProperty.Changed.AddClassHandler<MarqueeMetricBar>(OnLayoutParameterChanged);
        BarMaxWidthProperty.Changed.AddClassHandler<MarqueeMetricBar>(OnLayoutParameterChanged);
    }

    public MarqueeMetricBar()
    {
        Template = new FuncControlTemplate<MarqueeMetricBar>(BuildTemplate);
        Loaded += (_, _) => RecalculateLayout();
        SizeChanged += (_, _) => RecalculateLayout();
        Unloaded += (_, _) =>
        {
            _hasScrolledForCurrentHover = false;
            StopScroll();
        };
    }

    // ── Dependency properties ─────────────────────────────────────────

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MarqueeMetricBar, double>(nameof(Value), 0.0);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MarqueeMetricBar, double>(nameof(Maximum), 100.0);

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MarqueeMetricBar, double>(nameof(Minimum), 0.0);

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MarqueeMetricBar, string>(nameof(Text), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Style applied to the inner ProgressBar (the app-wide MetricProgressBarStyle).</summary>
    public static readonly StyledProperty<Style?> BarStyleProperty =
        AvaloniaProperty.Register<MarqueeMetricBar, Style?>(nameof(BarStyle), null);

    public Style? BarStyle
    {
        get => (Style?)GetValue(BarStyleProperty);
        set => SetValue(BarStyleProperty, value);
    }

    /// <summary>Style applied to the value TextBlock (the app-wide StatValueTextBlockStyle).</summary>
    public static readonly StyledProperty<Style?> TextStyleProperty =
        AvaloniaProperty.Register<MarqueeMetricBar, Style?>(nameof(TextStyle), null);

    public Style? TextStyle
    {
        get => (Style?)GetValue(TextStyleProperty);
        set => SetValue(TextStyleProperty, value);
    }

    /// <summary>Lower bound for the bar width so small readings stay clearly visible.</summary>
    public static readonly StyledProperty<double> BarMinWidthProperty =
        AvaloniaProperty.Register<MarqueeMetricBar, double>(nameof(BarMinWidth), 100.0);

    public double BarMinWidth
    {
        get => (double)GetValue(BarMinWidthProperty);
        set => SetValue(BarMinWidthProperty, value);
    }

    /// <summary>Upper bound for the bar width on wide surfaces (mirrors the old MaxWidth switch).</summary>
    public static readonly StyledProperty<double> BarMaxWidthProperty =
        AvaloniaProperty.Register<MarqueeMetricBar, double>(nameof(BarMaxWidth), 260.0);

    public double BarMaxWidth
    {
        get => (double)GetValue(BarMaxWidthProperty);
        set => SetValue(BarMaxWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the tooltip shown for the control. Wraps the <c>ToolTip.Tip</c> attached
    /// property to keep the WPF <c>FrameworkElement.ToolTip</c> contract (SensorsControl sets
    /// <c>metricToolTip.SetTip(this, ...</c>).
    /// </summary>
    public object? ToolTip
    {
        get => global::Avalonia.Controls.ToolTip.GetTip(this);
        set => global::Avalonia.Controls.ToolTip.SetTip(this, value);
    }

    // ── Template (built in code; no resource dictionary dependency) ─────

    private static Control BuildTemplate(MarqueeMetricBar owner, INameScope _)
    {
        var root = new Grid
        {
            Background = Brushes.Transparent,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            }
        };
        // Route Padding into the template so available-width math (ActualWidth minus
        // Padding) matches the space the bar actually gets.
        root[!Layoutable.MarginProperty] = new TemplateBinding(TemplatedControl.PaddingProperty);

        // Keep the whole metric row hit-testable. Without a transparent surface,
        // Avalonia only reports hover over painted children such as the blue fill.
        var viewport = new Grid
        {
            ClipToBounds = true,
            Background = Brushes.Transparent,
        };
        Grid.SetColumn(viewport, 0);

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var bar = new ProgressBar { VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(bar);

        var text = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(BarTextGap, 0, 0, 0),
        };
        Grid.SetColumn(text, 1);

        viewport.Children.Add(content);
        root.Children.Add(viewport);
        root.Children.Add(text);

        owner._viewport = viewport;
        owner._content = content;
        owner._bar = bar;
        owner._label = text;
        return root;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

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
                ApplyStyle(_bar, BarStyle);
            SyncRangeToBar();
            // AVALONIA: removed ProgressBarAnimateBehavior (Microsoft.Xaml.Behaviors is not
            // available in Avalonia); the bar value updates instantly instead of animating.
        }

        if (_label is not null)
        {
            if (TextStyle is not null)
                ApplyStyle(_label, TextStyle);
            _label.Text = Text;
        }

        RecalculateLayout();
    }

    // ── Property-change plumbing ──────────────────────────────────────

    private static void OnTextChanged(MarqueeMetricBar bar, AvaloniaPropertyChangedEventArgs e)
    {
        if (bar._label is not null)
        {
            bar._label.Text = bar.Text;
            bar.RecalculateLayout();
        }
    }

    private static void OnBarStyleChanged(MarqueeMetricBar bar, AvaloniaPropertyChangedEventArgs e)
    {
        if (bar._bar is not null && bar.BarStyle is not null)
            ApplyStyle(bar._bar, bar.BarStyle);
    }

    private static void OnTextStyleChanged(MarqueeMetricBar bar, AvaloniaPropertyChangedEventArgs e)
    {
        if (bar._label is not null && bar.TextStyle is not null)
            ApplyStyle(bar._label, bar.TextStyle);
    }

    // AVALONIA: controls have no Style property; register the keyed style on the
    // control's own Styles collection (selector matches the control itself).
    private static void ApplyStyle(Control target, Style style)
    {
        if (!target.Styles.Contains(style))
            target.Styles.Add(style);
    }

    private static void OnLayoutParameterChanged(MarqueeMetricBar bar, AvaloniaPropertyChangedEventArgs e)
    {
        if (bar._bar is not null)
            bar._bar.MinWidth = bar.BarMinWidth;
        bar.RecalculateLayout();
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
        return _label.Text ?? string.Empty;
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
            Brushes.Black);
        return formatted.Width + _label.Padding.Left + _label.Padding.Right;
    }

    private double AvailableWidth => Bounds.Width - Padding.Left - Padding.Right;

    private double GetBarViewportWidth(double fallback)
    {
        var width = _viewport?.Bounds.Width ?? 0;
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
        // The label has its own non-clipped slot. If the bar is too wide for
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
            if (!IsPointerOver)
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
            if (!IsPointerOver)
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
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.Black, 0.0),
                new GradientStop(Colors.Black, fadeStop),
                new GradientStop(Colors.Transparent, 1.0)
            }
        };
        _viewport.OpacityMask = mask;
    }

    private void ClearFadeMask()
    {
        if (_viewport is not null)
            _viewport.OpacityMask = null;
    }

    // ── Hover scroll (back-and-forth while hovered, reset on leave) ────

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (_isOverflowing && !_hasScrolledForCurrentHover)
            _hasScrolledForCurrentHover = StartScroll();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
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
        var start = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var progress = Math.Min(1.0, (DateTime.UtcNow - start).TotalSeconds / seconds);
            if (_scrollTransform is not null)
                _scrollTransform.X = -overflow * progress;

            if (progress >= 1.0 && ReferenceEquals(_scrollTimer, timer))
            {
                timer.Stop();
                _scrollTimer = null;
            }
        };
        _scrollTimer = timer;
        timer.Start();
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
        if (_scrollTimer is not null)
        {
            _scrollTimer.Stop();
            _scrollTimer = null;
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
