using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.WPF.Controls;

namespace UniversalDeviceToolkit.WPF.Behaviors;

/// <summary>
/// Applies the shared localized overflow policy to an existing WPF TextBlock without
/// requiring callers to replace the control type in a template.
/// </summary>
public static class LocalizedOverflowBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(LocalizedOverflowBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.RegisterAttached(
            "Mode", typeof(LocalizedOverflowMode), typeof(LocalizedOverflowBehavior),
            new PropertyMetadata(LocalizedOverflowMode.Ellipsis, OnPolicyChanged));

    public static readonly DependencyProperty MaxLinesProperty =
        DependencyProperty.RegisterAttached(
            "MaxLines", typeof(int), typeof(LocalizedOverflowBehavior),
            new PropertyMetadata(1, OnPolicyChanged));

    public static readonly DependencyProperty AutoToolTipProperty =
        DependencyProperty.RegisterAttached(
            "AutoToolTip", typeof(bool), typeof(LocalizedOverflowBehavior),
            new PropertyMetadata(true, OnPolicyChanged));

    private static readonly DependencyProperty OwnsToolTipProperty =
        DependencyProperty.RegisterAttached("OwnsToolTip", typeof(bool), typeof(LocalizedOverflowBehavior));

    private static readonly DependencyProperty OwnsMaxHeightProperty =
        DependencyProperty.RegisterAttached("OwnsMaxHeight", typeof(bool), typeof(LocalizedOverflowBehavior));

    private static readonly DependencyProperty OriginalMaxHeightProperty =
        DependencyProperty.RegisterAttached(
            "OriginalMaxHeight",
            typeof(double),
            typeof(LocalizedOverflowBehavior),
            new PropertyMetadata(double.PositiveInfinity));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);
    public static LocalizedOverflowMode GetMode(DependencyObject obj) => (LocalizedOverflowMode)obj.GetValue(ModeProperty);
    public static void SetMode(DependencyObject obj, LocalizedOverflowMode value) => obj.SetValue(ModeProperty, value);
    public static int GetMaxLines(DependencyObject obj) => (int)obj.GetValue(MaxLinesProperty);
    public static void SetMaxLines(DependencyObject obj, int value) => obj.SetValue(MaxLinesProperty, value);
    public static bool GetAutoToolTip(DependencyObject obj) => (bool)obj.GetValue(AutoToolTipProperty);
    public static void SetAutoToolTip(DependencyObject obj, bool value) => obj.SetValue(AutoToolTipProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        if ((bool)e.NewValue)
        {
            textBlock.Loaded += OnUpdated;
            textBlock.SizeChanged += OnUpdated;
            descriptor?.AddValueChanged(textBlock, OnUpdated);
            Update(textBlock);
        }
        else
        {
            textBlock.Loaded -= OnUpdated;
            textBlock.SizeChanged -= OnUpdated;
            descriptor?.RemoveValueChanged(textBlock, OnUpdated);
            RestoreToolTip(textBlock);
            RestoreMaxHeight(textBlock);
        }
    }

    private static void OnPolicyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock && GetIsEnabled(textBlock))
            Update(textBlock);
    }

    private static void OnUpdated(object? sender, EventArgs e)
    {
        if (sender is TextBlock textBlock)
            Update(textBlock);
    }

    private static void Update(TextBlock textBlock)
    {
        if (!string.IsNullOrWhiteSpace(textBlock.Text)
            && string.IsNullOrWhiteSpace(AutomationProperties.GetName(textBlock)))
        {
            AutomationProperties.SetName(textBlock, textBlock.Text);
        }

        if (textBlock is AdaptiveTextBlock adaptive)
        {
            adaptive.OverflowMode = GetMode(textBlock);
            adaptive.MaxLines = GetMaxLines(textBlock);
            adaptive.AutoToolTip = GetAutoToolTip(textBlock);
            return;
        }

        var mode = GetMode(textBlock);
        textBlock.TextWrapping = mode == LocalizedOverflowMode.Wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        textBlock.TextTrimming = TextTrimming.CharacterEllipsis;

        if (mode == LocalizedOverflowMode.Wrap && GetMaxLines(textBlock) > 0)
        {
            if (!GetValue(textBlock, OwnsMaxHeightProperty) && double.IsInfinity(textBlock.MaxHeight))
            {
                textBlock.SetValue(OriginalMaxHeightProperty, textBlock.MaxHeight);
                SetValue(textBlock, OwnsMaxHeightProperty, true);
            }

            if (GetValue(textBlock, OwnsMaxHeightProperty))
            {
                var maxLineHeightForWrap = Math.Max(1, textBlock.FontSize * 1.35);
                textBlock.MaxHeight = maxLineHeightForWrap * GetMaxLines(textBlock)
                    + textBlock.Padding.Top + textBlock.Padding.Bottom;
            }
        }
        else
            RestoreMaxHeight(textBlock);

        if (!GetAutoToolTip(textBlock) || string.IsNullOrWhiteSpace(textBlock.Text) || textBlock.ActualWidth <= 0)
        {
            RestoreToolTip(textBlock);
            return;
        }

        var availableWidth = textBlock.ActualWidth - textBlock.Padding.Left - textBlock.Padding.Right;
        if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth))
            availableWidth = 1;
        availableWidth = Math.Max(1, availableWidth);
        var formatted = new FormattedText(
            textBlock.Text,
            CultureInfo.CurrentUICulture,
            textBlock.FlowDirection,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(textBlock).PixelsPerDip)
        {
            MaxTextWidth = availableWidth,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        var lineHeight = Math.Max(1, textBlock.FontSize * 1.35);
        var maxLines = Math.Max(1, GetMaxLines(textBlock));
        var truncated = mode == LocalizedOverflowMode.Ellipsis
            ? formatted.WidthIncludingTrailingWhitespace > availableWidth + 1
            : formatted.Height > lineHeight * maxLines + 1;

        if (truncated)
        {
            if (textBlock.ToolTip is null)
                SetValue(textBlock, OwnsToolTipProperty, true);
            if (GetValue(textBlock, OwnsToolTipProperty))
                textBlock.ToolTip = textBlock.Text;
        }
        else
            RestoreToolTip(textBlock);
    }

    private static bool GetValue(DependencyObject obj, DependencyProperty property) => (bool)obj.GetValue(property);
    private static void SetValue(DependencyObject obj, DependencyProperty property, bool value) => obj.SetValue(property, value);

    private static void RestoreToolTip(TextBlock textBlock)
    {
        if (GetValue(textBlock, OwnsToolTipProperty))
        {
            textBlock.ToolTip = null;
            SetValue(textBlock, OwnsToolTipProperty, false);
        }
    }

    private static void RestoreMaxHeight(TextBlock textBlock)
    {
        if (!GetValue(textBlock, OwnsMaxHeightProperty))
            return;

        textBlock.MaxHeight = (double)textBlock.GetValue(OriginalMaxHeightProperty);
        SetValue(textBlock, OwnsMaxHeightProperty, false);
    }
}
