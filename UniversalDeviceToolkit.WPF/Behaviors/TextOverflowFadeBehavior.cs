using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UniversalDeviceToolkit.WPF.Behaviors;

/// <summary>
/// Attached behavior that adds a right-edge fade hint to a <see cref="TextBlock"/> whose
/// content is clipped by its container:
/// <list type="bullet">
/// <item>While the text overflows, an <c>OpacityMask</c> gradient (opaque → transparent)
/// fades the last ~24px so users can see content is truncated.</item>
/// <item>The mask appears ONLY while the text is actually truncated — fully visible
/// text renders crisp with no mask at all.</item>
/// <item>Reacts to layout size changes and text updates. Each measurement samples the
/// element's current DPI via VisualTreeHelper.GetDpi, so scaled displays stay correct.</item>
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

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TextOverflowFadeBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    /// <summary>Marks that the current ToolTip was auto-filled by this behavior (never clears user ToolTips).</summary>
    private static readonly DependencyProperty OwnsToolTipProperty =
        DependencyProperty.RegisterAttached(
            "OwnsToolTip",
            typeof(bool),
            typeof(TextOverflowFadeBehavior),
            new PropertyMetadata(false));

    private static bool GetOwnsToolTip(DependencyObject obj) => (bool)obj.GetValue(OwnsToolTipProperty);

    private static void SetOwnsToolTip(DependencyObject obj, bool value) => obj.SetValue(OwnsToolTipProperty, value);

    /// <summary>Marks that the current OpacityMask was applied by this behavior (never clears user masks).</summary>
    private static readonly DependencyProperty OwnsMaskProperty =
        DependencyProperty.RegisterAttached(
            "OwnsMask",
            typeof(bool),
            typeof(TextOverflowFadeBehavior),
            new PropertyMetadata(false));

    private static bool GetOwnsMask(DependencyObject obj) => (bool)obj.GetValue(OwnsMaskProperty);

    private static void SetOwnsMask(DependencyObject obj, bool value) => obj.SetValue(OwnsMaskProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        var textDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));

        if ((bool)e.NewValue)
        {
            // Size changes (window resize / layout) and text updates drive re-evaluation.
            textBlock.Loaded += OnUpdated;
            textBlock.SizeChanged += OnUpdated;
            textDescriptor?.AddValueChanged(textBlock, OnUpdated);
            Update(textBlock);
        }
        else
        {
            textBlock.Loaded -= OnUpdated;
            textBlock.SizeChanged -= OnUpdated;
            textDescriptor?.RemoveValueChanged(textBlock, OnUpdated);
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

        var availableWidth = textBlock.ActualWidth;
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
        textBlock.OpacityMask = mask;
        SetOwnsMask(textBlock, true);

        if (textBlock.ToolTip is null)
        {
            textBlock.ToolTip = textBlock.Text;
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
            if (textBlock.ToolTip is string)
                textBlock.ToolTip = null;
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
            Brushes.Black,
            VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);
        return formatted.Width;
    }
}
