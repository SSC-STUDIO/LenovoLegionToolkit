using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Behaviors;

/// <summary>
/// Attached behavior that adds a right-edge fade hint to a <see cref="TextBlock"/> whose
/// content is clipped by its container:
/// <list type="bullet">
/// <item>While the text overflows, an <c>OpacityMask</c> gradient (opaque → transparent)
/// fades the last ~24px so users can see content is truncated.</item>
/// <item>The mask appears ONLY while the text is actually truncated — fully visible
/// text renders crisp with no mask at all.</item>
/// <item>Reacts to layout size changes and text updates.</item>
/// <item>While truncated and no ToolTip is set, the full text is offered as a ToolTip
/// (tracked via a private flag so pre-existing ToolTips are never touched).</item>
/// </list>
/// Usage: <c>behaviors:TextOverflowFadeBehavior.IsEnabled="True"</c> on any TextBlock,
/// including inside Styles and ControlTemplates.
/// </summary>
public static class TextOverflowFadeBehavior
{
    private const double FadeWidth = 24.0;

    // Tolerance absorbing sub-pixel differences between FormattedText metrics and the
    // layout engine's own width calculation (they can differ by ~1px on some fonts/DPI).
    private const double OverflowEpsilon = 2.0;

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("IsEnabled", typeof(TextOverflowFadeBehavior), false);

    public static bool GetIsEnabled(AvaloniaObject obj) => obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(AvaloniaObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    /// <summary>Marks that the current ToolTip was auto-filled by this behavior (never clears user ToolTips).</summary>
    private static readonly AttachedProperty<bool> OwnsToolTipProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("OwnsToolTip", typeof(TextOverflowFadeBehavior), false);

    private static bool GetOwnsToolTip(AvaloniaObject obj) => obj.GetValue(OwnsToolTipProperty);

    private static void SetOwnsToolTip(AvaloniaObject obj, bool value) => obj.SetValue(OwnsToolTipProperty, value);

    /// <summary>Marks that the current OpacityMask was applied by this behavior (never clears user masks).</summary>
    private static readonly AttachedProperty<bool> OwnsMaskProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("OwnsMask", typeof(TextOverflowFadeBehavior), false);

    private static bool GetOwnsMask(AvaloniaObject obj) => obj.GetValue(OwnsMaskProperty);

    private static void SetOwnsMask(AvaloniaObject obj, bool value) => obj.SetValue(OwnsMaskProperty, value);

    static TextOverflowFadeBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<TextBlock>(OnIsEnabledChanged);
    }

    private static void OnIsEnabledChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            // Size changes (window resize / layout) and text updates drive re-evaluation.
            textBlock.Loaded += OnUpdated;
            textBlock.SizeChanged += OnUpdated;
            textBlock.PropertyChanged += OnTextPropertyChanged;
            Update(textBlock);
        }
        else
        {
            textBlock.Loaded -= OnUpdated;
            textBlock.SizeChanged -= OnUpdated;
            textBlock.PropertyChanged -= OnTextPropertyChanged;
            ClearMask(textBlock);
            RestoreToolTip(textBlock);
        }
    }

    // Single handler shape works for Loaded (RoutedEventArgs), SizeChanged (SizeChangedEventArgs)
    // and EventHandler — all derive from EventArgs (contravariance).
    private static void OnUpdated(object? sender, EventArgs e)
    {
        if (sender is TextBlock textBlock)
            Update(textBlock);
    }

    private static void OnTextPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.TextProperty && sender is TextBlock textBlock)
            Update(textBlock);
    }

    private static void Update(TextBlock textBlock)
    {
        // Wrapped text reflows instead of clipping horizontally — fade only applies to
        // no-wrap / overflow-wrapping blocks that can actually truncate.
        if (textBlock.TextWrapping == TextWrapping.Wrap)
        {
            ClearMask(textBlock);
            RestoreToolTip(textBlock);
            return;
        }

        var availableWidth = textBlock.Bounds.Width;
        if (availableWidth <= 0 || string.IsNullOrEmpty(textBlock.Text))
        {
            ClearMask(textBlock);
            RestoreToolTip(textBlock);
            return;
        }

        var truncated = MeasureNaturalWidth(textBlock) > availableWidth + OverflowEpsilon;
        if (!truncated)
        {
            ClearMask(textBlock);
            RestoreToolTip(textBlock);
            return;
        }

        var fadeStop = Math.Max(0.0, 1.0 - FadeWidth / availableWidth);
        var mask = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.Black, 0.0),
                new GradientStop(Colors.Black, fadeStop),
                new GradientStop(Colors.Transparent, 1.0),
            },
        };
        textBlock.OpacityMask = mask;
        SetOwnsMask(textBlock, true);

        if (ToolTip.GetTip(textBlock) is null)
        {
            ToolTip.SetTip(textBlock, textBlock.Text);
            SetOwnsToolTip(textBlock, true);
        }
    }

    private static void ClearMask(TextBlock textBlock)
    {
        if (GetOwnsMask(textBlock) && textBlock.OpacityMask is not null)
            textBlock.OpacityMask = null;
        SetOwnsMask(textBlock, false);
    }

    private static void RestoreToolTip(TextBlock textBlock)
    {
        if (GetOwnsToolTip(textBlock))
        {
            if (ToolTip.GetTip(textBlock) is string)
                ToolTip.SetTip(textBlock, null);
            SetOwnsToolTip(textBlock, false);
        }
    }

    private static double MeasureNaturalWidth(TextBlock textBlock)
    {
        var formatted = new FormattedText(
            textBlock.Text,
            CultureInfo.CurrentCulture,
            textBlock.FlowDirection,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black);
        return formatted.Width;
    }
}
