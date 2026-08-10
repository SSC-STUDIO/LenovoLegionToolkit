using System;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Behaviors;

/// <summary>
/// Applies the shared localized overflow policy to an existing Avalonia TextBlock without
/// requiring callers to replace the control type in a template.
/// </summary>
public static class LocalizedOverflowBehavior
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("IsEnabled", typeof(LocalizedOverflowBehavior), false);

    public static readonly AttachedProperty<LocalizedOverflowMode> ModeProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, LocalizedOverflowMode>("Mode", typeof(LocalizedOverflowBehavior), LocalizedOverflowMode.Ellipsis);

    public static readonly AttachedProperty<int> MaxLinesProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, int>("MaxLines", typeof(LocalizedOverflowBehavior), 1);

    public static readonly AttachedProperty<bool> AutoToolTipProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("AutoToolTip", typeof(LocalizedOverflowBehavior), true);

    private static readonly AttachedProperty<bool> OwnsToolTipProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("OwnsToolTip", typeof(LocalizedOverflowBehavior), false);

    private static readonly AttachedProperty<bool> OwnsMaxHeightProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("OwnsMaxHeight", typeof(LocalizedOverflowBehavior), false);

    private static readonly AttachedProperty<double> OriginalMaxHeightProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, double>("OriginalMaxHeight", typeof(LocalizedOverflowBehavior), double.PositiveInfinity);

    static LocalizedOverflowBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<TextBlock>(OnIsEnabledChanged);
        ModeProperty.Changed.AddClassHandler<TextBlock>(OnPolicyChanged);
        MaxLinesProperty.Changed.AddClassHandler<TextBlock>(OnPolicyChanged);
        AutoToolTipProperty.Changed.AddClassHandler<TextBlock>(OnPolicyChanged);
    }

    public static bool GetIsEnabled(AvaloniaObject obj) => obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(AvaloniaObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);
    public static LocalizedOverflowMode GetMode(AvaloniaObject obj) => obj.GetValue(ModeProperty);
    public static void SetMode(AvaloniaObject obj, LocalizedOverflowMode value) => obj.SetValue(ModeProperty, value);
    public static int GetMaxLines(AvaloniaObject obj) => obj.GetValue(MaxLinesProperty);
    public static void SetMaxLines(AvaloniaObject obj, int value) => obj.SetValue(MaxLinesProperty, value);
    public static bool GetAutoToolTip(AvaloniaObject obj) => obj.GetValue(AutoToolTipProperty);
    public static void SetAutoToolTip(AvaloniaObject obj, bool value) => obj.SetValue(AutoToolTipProperty, value);

    private static void OnIsEnabledChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
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
            RestoreToolTip(textBlock);
            RestoreMaxHeight(textBlock);
        }
    }

    private static void OnPolicyChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        if (GetIsEnabled(textBlock))
            Update(textBlock);
    }

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

        if (!GetAutoToolTip(textBlock) || string.IsNullOrWhiteSpace(textBlock.Text) || textBlock.Bounds.Width <= 0)
        {
            RestoreToolTip(textBlock);
            return;
        }

        var availableWidth = textBlock.Bounds.Width - textBlock.Padding.Left - textBlock.Padding.Right;
        if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth))
            availableWidth = 1;
        availableWidth = Math.Max(1, availableWidth);

        var lineHeight = Math.Max(1, textBlock.FontSize * 1.35);
        var maxLines = Math.Max(1, GetMaxLines(textBlock));

        var truncated = mode == LocalizedOverflowMode.Ellipsis
            ? MeasureNaturalWidth(textBlock) > availableWidth + 1
            : MeasureHeight(textBlock, availableWidth) > lineHeight * maxLines + 1;

        if (truncated)
        {
            if (ToolTip.GetTip(textBlock) is null)
                SetValue(textBlock, OwnsToolTipProperty, true);
            if (GetValue(textBlock, OwnsToolTipProperty))
                ToolTip.SetTip(textBlock, textBlock.Text);
        }
        else
            RestoreToolTip(textBlock);
    }

    private static bool GetValue(AvaloniaObject obj, AttachedProperty<bool> property) => obj.GetValue(property);
    private static void SetValue(AvaloniaObject obj, AttachedProperty<bool> property, bool value) => obj.SetValue(property, value);

    private static void RestoreToolTip(TextBlock textBlock)
    {
        if (GetValue(textBlock, OwnsToolTipProperty))
        {
            ToolTip.SetTip(textBlock, null);
            SetValue(textBlock, OwnsToolTipProperty, false);
        }
    }

    private static void RestoreMaxHeight(TextBlock textBlock)
    {
        if (!GetValue(textBlock, OwnsMaxHeightProperty))
            return;

        textBlock.MaxHeight = textBlock.GetValue(OriginalMaxHeightProperty);
        SetValue(textBlock, OwnsMaxHeightProperty, false);
    }

    private static double MeasureNaturalWidth(TextBlock textBlock)
    {
        var formatted = new FormattedText(
            textBlock.Text,
            CultureInfo.CurrentUICulture,
            textBlock.FlowDirection,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black);
        return formatted.WidthIncludingTrailingWhitespace;
    }

    private static double MeasureHeight(TextBlock textBlock, double availableWidth)
    {
        var formatted = new FormattedText(
            textBlock.Text,
            CultureInfo.CurrentUICulture,
            textBlock.FlowDirection,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black)
        {
            MaxTextWidth = availableWidth,
        };
        return formatted.Height;
    }
}
