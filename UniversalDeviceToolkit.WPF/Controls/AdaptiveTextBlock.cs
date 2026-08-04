using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.WPF.Controls;

/// <summary>
/// A <see cref="TextBlock"/> that dynamically scales its font size down to fit
/// within a constrained height, so longer translations can stay readable without
/// clipping or bloating the surrounding card/window.
///
/// How to use:
///  * Set a height constraint (e.g. <c>MaxHeight="60"</c>) or use
///    <see cref="MaxLines"/> to derive the constraint from the base font size.
///  * Set <see cref="MinFontSize"/> to the smallest readable size.
///  * Set <see cref="AutoToolTip"/> = true (default) to show the full text when
///    it is still truncated at the minimum font size.
///
/// The control preserves the base font size whenever the text fits. Scaling only
/// happens when the measured text exceeds the allowed height.
/// </summary>
public class AdaptiveTextBlock : TextBlock
{
    private const double DefaultMinFontSize = LocalizedOverflowPolicy.MinimumReadableFontSize;
    private const double DefaultFontSize = 12.0;
    private const double DefaultScaleStep = 0.5;
    private const double DefaultLineHeightFactor = 1.35;

    private double _baseFontSize;
    private bool _isAdapting;
    private bool _ownsToolTip;
    private DependencyPropertyDescriptor? _textDescriptor;

    public static readonly DependencyProperty MaxLinesProperty =
        DependencyProperty.Register(
            nameof(MaxLines),
            typeof(int),
            typeof(AdaptiveTextBlock),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutConstraintChanged));

    public static readonly DependencyProperty MinFontSizeProperty =
        DependencyProperty.Register(
            nameof(MinFontSize),
            typeof(double),
            typeof(AdaptiveTextBlock),
            new FrameworkPropertyMetadata(DefaultMinFontSize, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutConstraintChanged));

    public static readonly DependencyProperty AutoToolTipProperty =
        DependencyProperty.Register(
            nameof(AutoToolTip),
            typeof(bool),
            typeof(AdaptiveTextBlock),
            new PropertyMetadata(true, OnAutoToolTipChanged));

    public static readonly DependencyProperty OverflowModeProperty =
        DependencyProperty.Register(
            nameof(OverflowMode),
            typeof(LocalizedOverflowMode),
            typeof(AdaptiveTextBlock),
            new FrameworkPropertyMetadata(
                LocalizedOverflowMode.Wrap,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnOverflowModeChanged));

    static AdaptiveTextBlock()
    {
        // Keep the TextBlock default template so styles and resource references
        // continue to work exactly as they do for the base class.
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AdaptiveTextBlock),
            new FrameworkPropertyMetadata(typeof(TextBlock)));

        // Re-run adaptation when the inherited text properties change.
        AddAdaptCallback(TextProperty);
        AddAdaptCallback(FontFamilyProperty);
        AddAdaptCallback(FontStyleProperty);
        AddAdaptCallback(FontWeightProperty);
        AddAdaptCallback(FontStretchProperty);
        AddAdaptCallback(ForegroundProperty);
        AddAdaptCallback(TextWrappingProperty);
        AddAdaptCallback(TextTrimmingProperty);
        AddAdaptCallback(PaddingProperty);
        AddAdaptCallback(LanguageProperty);
    }

    public AdaptiveTextBlock()
    {
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        _textDescriptor = DependencyPropertyDescriptor.FromProperty(TextProperty, typeof(TextBlock));
        _textDescriptor?.AddValueChanged(this, OnTextChanged);
    }

    /// <summary>
    /// Maximum number of lines the text should occupy at the base font size.
    /// If zero, the constraint is taken from <see cref="FrameworkElement.MaxHeight"/>.
    /// </summary>
    public int MaxLines
    {
        get => (int)GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    /// <summary>
    /// Smallest font size the control may use when scaling down.
    /// </summary>
    public double MinFontSize
    {
        get => (double)GetValue(MinFontSizeProperty);
        set => SetValue(MinFontSizeProperty, value);
    }

    /// <summary>
    /// When true (default) and the text is truncated, the full text is shown in a ToolTip.
    /// </summary>
    public bool AutoToolTip
    {
        get => (bool)GetValue(AutoToolTipProperty);
        set => SetValue(AutoToolTipProperty, value);
    }

    /// <summary>
    /// Selects the semantic overflow behavior. Wrap allows a bounded number of lines;
    /// Ellipsis keeps the control compact and single-line.
    /// </summary>
    public LocalizedOverflowMode OverflowMode
    {
        get => (LocalizedOverflowMode)GetValue(OverflowModeProperty);
        set => SetValue(OverflowModeProperty, value);
    }

    private static void AddAdaptCallback(DependencyProperty property)
    {
        property.OverrideMetadata(
            typeof(AdaptiveTextBlock),
            new FrameworkPropertyMetadata(propertyChangedCallback: OnAdaptTriggerChanged));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _baseFontSize = NormalizeFontSize(FontSize);
        ApplyOverflowMode();
        UpdateAutomationName();
        Adapt();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Adapt();

    private void OnUnloaded(object sender, RoutedEventArgs e) => RestoreOwnedToolTip();

    private void OnTextChanged(object? sender, EventArgs e)
    {
        ApplyOverflowMode();
        UpdateAutomationName();
        Adapt();
    }

    private static void OnLayoutConstraintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveTextBlock block)
            block.Adapt();
    }

    private static void OnAutoToolTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveTextBlock block)
            block.Adapt();
    }

    private static void OnOverflowModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveTextBlock block)
        {
            block.ApplyOverflowMode();
            block.Adapt();
        }
    }

    private static void OnAdaptTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveTextBlock block)
        {
            if (!block._isAdapting && e.Property == FontSizeProperty && IsValidFontSize(block.FontSize))
                block._baseFontSize = block.FontSize;

            block.Adapt();
        }
    }

    private void Adapt()
    {
        if (!IsLoaded || _isAdapting)
            return;

        if (!IsValidFontSize(_baseFontSize))
            _baseFontSize = NormalizeFontSize(FontSize);

        var text = Text;
        if (string.IsNullOrEmpty(text))
            return;

        var constraint = GetHeightConstraint();
        if (double.IsNaN(constraint) || constraint <= 0)
            return;

        var availableWidth = Math.Max(0, ActualWidth - Padding.Left - Padding.Right);
        if (availableWidth <= 0)
            return;

        _isAdapting = true;
        try
        {
            var bestSize = _baseFontSize;
            var minFontSize = Math.Min(MinFontSize, _baseFontSize);
            if (minFontSize <= 0)
                minFontSize = DefaultMinFontSize;

            // Fast path: the base font size already fits.
            var baseHeight = MeasureHeight(_baseFontSize, availableWidth);
            if (OverflowMode == LocalizedOverflowMode.Wrap && baseHeight > constraint)
            {
                // Scale down in small steps until the text fits or we hit the minimum.
                for (var size = _baseFontSize - DefaultScaleStep; size >= minFontSize; size -= DefaultScaleStep)
                {
                    var height = MeasureHeight(size, availableWidth);
                    if (height <= constraint)
                    {
                        bestSize = size;
                        break;
                    }
                }

                // If even the minimum font size is still too tall, choose the minimum
                // and let the TextTrimming/MaxHeight clip with ellipsis.
                if (bestSize > minFontSize)
                {
                    var minHeight = MeasureHeight(minFontSize, availableWidth);
                    bestSize = minHeight <= constraint ? minFontSize : bestSize;
                }
                else
                {
                    bestSize = minFontSize;
                }
            }

            if (Math.Abs(FontSize - bestSize) > 0.01)
            {
                FontSize = bestSize;
                // A font-size change affects measure; the next layout pass will call
                // OnSizeChanged again, but _isAdapting prevents recursion.
            }

            UpdateToolTip(constraint, availableWidth);
            UpdateAutomationName();
        }
        finally
        {
            _isAdapting = false;
        }
    }

    private double GetHeightConstraint()
    {
        if (MaxHeight > 0 && !double.IsInfinity(MaxHeight))
            return MaxHeight - Padding.Top - Padding.Bottom;

        if (MaxLines > 0)
        {
            // Height of one line at the base font size, multiplied by the line count.
            var lineHeight = _baseFontSize * DefaultLineHeightFactor;
            return MaxLines * lineHeight;
        }

        return OverflowMode == LocalizedOverflowMode.Ellipsis
            ? _baseFontSize * DefaultLineHeightFactor
            : double.NaN;
    }

    private double MeasureHeight(double fontSize, double maxWidth)
    {
        fontSize = NormalizeFontSize(fontSize);

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        var pixelsPerDip = GetPixelsPerDip();
        var foreground = Foreground ?? Brushes.Black;

        var formatted = new FormattedText(
            Text,
            CultureInfo.CurrentUICulture,
            FlowDirection,
            typeface,
            fontSize,
            foreground,
            pixelsPerDip)
        {
            MaxTextWidth = ShouldWrap() ? Math.Max(0, maxWidth) : double.PositiveInfinity,
            Trimming = TextTrimming
        };

        return formatted.Height;
    }

    private static bool IsValidFontSize(double fontSize) =>
        !double.IsNaN(fontSize) && !double.IsInfinity(fontSize) && fontSize > 0;

    private static double NormalizeFontSize(double fontSize) =>
        IsValidFontSize(fontSize) ? fontSize : DefaultFontSize;

    private bool ShouldWrap() =>
        OverflowMode == LocalizedOverflowMode.Wrap
            || TextWrapping == TextWrapping.Wrap
            || TextWrapping == TextWrapping.WrapWithOverflow;

    private double GetPixelsPerDip()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is not null)
        {
            var transform = source.CompositionTarget.TransformToDevice;
            return Math.Max(transform.M11, transform.M22);
        }

        return 1.0;
    }

    private void UpdateToolTip(double constraint, double availableWidth)
    {
        if (!AutoToolTip)
        {
            RestoreOwnedToolTip();
            return;
        }

        var truncated = ShouldWrap()
            ? MeasureHeight(_baseFontSize, availableWidth) > constraint + 0.5
            : MeasureNaturalWidth(_baseFontSize) > availableWidth + 1.0;

        if (truncated)
        {
            if (!_ownsToolTip && ToolTip is null)
                _ownsToolTip = true;
            if (_ownsToolTip)
                ToolTip = Text;
            ToolTipService.SetIsEnabled(this, true);
        }
        else
        {
            RestoreOwnedToolTip();
        }
    }

    private void ApplyOverflowMode()
    {
        if (OverflowMode == LocalizedOverflowMode.Wrap)
        {
            TextWrapping = TextWrapping.Wrap;
            TextTrimming = TextTrimming.CharacterEllipsis;
            if (MaxLines <= 0)
                MaxLines = LocalizedOverflowPolicy.DescriptionMaxLines;
        }
        else
        {
            TextWrapping = TextWrapping.NoWrap;
            TextTrimming = TextTrimming.CharacterEllipsis;
            MaxLines = 1;
        }
    }

    private double MeasureNaturalWidth(double fontSize)
    {
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        var formatted = new FormattedText(
            Text,
            CultureInfo.CurrentUICulture,
            FlowDirection,
            typeface,
            NormalizeFontSize(fontSize),
            Foreground ?? Brushes.Black,
            GetPixelsPerDip());
        return formatted.WidthIncludingTrailingWhitespace;
    }

    private void UpdateAutomationName()
    {
        if (!string.IsNullOrWhiteSpace(Text) && string.IsNullOrWhiteSpace(AutomationProperties.GetName(this)))
            AutomationProperties.SetName(this, Text);
    }

    private void RestoreOwnedToolTip()
    {
        if (!_ownsToolTip)
            return;

        ToolTip = null;
        ToolTipService.SetIsEnabled(this, false);
        _ownsToolTip = false;
    }
}
