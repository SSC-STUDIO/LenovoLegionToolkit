using System;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Avalonia.Controls;

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
    private string? _automationText;

    // Avalonia's TextBlock already defines a MaxLinesProperty; the WPF control had to
    // re-register its own because WPF's TextBlock lacks MaxLines. Here we shadow the
    // inherited one and use it purely as a constraint input (never as a base layout hint).
    public static readonly new StyledProperty<int> MaxLinesProperty =
        AvaloniaProperty.Register<AdaptiveTextBlock, int>(nameof(MaxLines), 0);

    public static readonly StyledProperty<double> MinFontSizeProperty =
        AvaloniaProperty.Register<AdaptiveTextBlock, double>(nameof(MinFontSize), DefaultMinFontSize);

    public static readonly StyledProperty<bool> AutoToolTipProperty =
        AvaloniaProperty.Register<AdaptiveTextBlock, bool>(nameof(AutoToolTip), true);

    public static readonly StyledProperty<LocalizedOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<AdaptiveTextBlock, LocalizedOverflowMode>(
            nameof(OverflowMode),
            LocalizedOverflowMode.Wrap);

    static AdaptiveTextBlock()
    {
        // Mirror the WPF FrameworkPropertyMetadataOptions.AffectsMeasure flags:
        // constraint changes schedule a re-measure so the layout pass re-runs Adapt.
        AffectsMeasure<AdaptiveTextBlock>(MaxLinesProperty, MinFontSizeProperty, OverflowModeProperty);
    }

    public AdaptiveTextBlock()
    {
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;

        // Replace the WPF DependencyPropertyDescriptor.AddValueChanged hook for the
        // Text property with an Avalonia property-changed observable.
        this.GetObservable(TextProperty).Subscribe(new AnonymousObserver<string>(_ => OnTextChanged()));
    }

    /// <summary>
    /// Maximum number of lines the text should occupy at the base font size.
    /// If zero, the constraint is taken from <see cref="MaxHeight"/>.
    /// </summary>
    public new int MaxLines
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Skip changes caused by Adapt itself (e.g. the scaled FontSize it applies).
        if (_isAdapting)
            return;

        if (change.Property == OverflowModeProperty)
        {
            ApplyOverflowMode();
            Adapt();
        }
        else if (change.Property == MaxLinesProperty || change.Property == MinFontSizeProperty
                 || change.Property == AutoToolTipProperty || change.Property == MaxHeightProperty)
        {
            Adapt();
        }
        else if (change.Property == FontSizeProperty)
        {
            // Keep the base font size in sync with external FontSize changes;
            // the scaled values Adapt applies itself are guarded by _isAdapting.
            if (IsValidFontSize(FontSize))
                _baseFontSize = FontSize;

            Adapt();
        }
        else if (change.Property == FontFamilyProperty || change.Property == FontStyleProperty
                 || change.Property == FontWeightProperty || change.Property == FontStretchProperty
                 || change.Property == ForegroundProperty || change.Property == TextWrappingProperty
                 || change.Property == TextTrimmingProperty || change.Property == PaddingProperty)
        {
            // Re-run adaptation when the inherited text properties change.
            Adapt();
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _baseFontSize = NormalizeFontSize(FontSize);
        ApplyOverflowMode();
        UpdateAutomationName();
        Adapt();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e) => Adapt();

    private void OnUnloaded(object? sender, RoutedEventArgs e) => RestoreOwnedToolTip();

    private void OnTextChanged()
    {
        ApplyOverflowMode();
        UpdateAutomationName();
        Adapt();
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

        var availableWidth = Math.Max(0, Bounds.Width - Padding.Left - Padding.Right);
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
                // A font-size change affects measure; the next layout pass will fire
                // SizeChanged again, but _isAdapting prevents recursion.
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
        var foreground = Foreground ?? Brushes.Black;

        var formatted = new FormattedText(
            Text ?? string.Empty,
            CultureInfo.CurrentUICulture,
            FlowDirection,
            typeface,
            fontSize,
            foreground)
        {
            // Keep the formatter's paragraph finite even for single-line mode. The
            // no-wrap setting prevents reflow; the natural-width check handles the
            // ellipsis decision separately.
            MaxTextWidth = NormalizeMeasureWidth(maxWidth),
            Trimming = TextTrimming,
        };

        return formatted.Height;
    }

    private static double NormalizeMeasureWidth(double width) =>
        double.IsNaN(width) || double.IsInfinity(width) ? 1.0 : Math.Max(1.0, width);

    private static bool IsValidFontSize(double fontSize) =>
        !double.IsNaN(fontSize) && !double.IsInfinity(fontSize) && fontSize > 0;

    private static double NormalizeFontSize(double fontSize) =>
        IsValidFontSize(fontSize) ? fontSize : DefaultFontSize;

    private bool ShouldWrap() =>
        OverflowMode == LocalizedOverflowMode.Wrap
            || TextWrapping == TextWrapping.Wrap
            || TextWrapping == TextWrapping.WrapWithOverflow;

    private double GetPixelsPerDip() => VisualRoot?.RenderScaling ?? 1.0;

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
            if (!_ownsToolTip && ToolTip.GetTip(this) is null)
                _ownsToolTip = true;
            if (_ownsToolTip)
                ToolTip.SetTip(this, Text);
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
            Text ?? string.Empty,
            CultureInfo.CurrentUICulture,
            FlowDirection,
            typeface,
            NormalizeFontSize(fontSize),
            Foreground ?? Brushes.Black);
        return formatted.WidthIncludingTrailingWhitespace;
    }

    private void UpdateAutomationName()
    {
        var currentName = AutomationProperties.GetName(this);
        if (!string.IsNullOrWhiteSpace(Text)
            && (string.IsNullOrWhiteSpace(currentName)
                || string.Equals(currentName, _automationText, StringComparison.Ordinal)))
        {
            AutomationProperties.SetName(this, Text);
            _automationText = Text;
        }
    }

    private void RestoreOwnedToolTip()
    {
        if (!_ownsToolTip)
            return;

        ToolTip.SetTip(this, null);
        _ownsToolTip = false;
    }
}
