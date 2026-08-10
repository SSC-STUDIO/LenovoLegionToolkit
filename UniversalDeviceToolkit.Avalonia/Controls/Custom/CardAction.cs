using System;
using Avalonia;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

// AVALONIA: WPF-UI CardAction (Button) + AutomationPeer replaced by an Avalonia Button
// subclass with the same public members. Template lives in Styles/CardAction.axaml.
public class CardAction : Avalonia.Controls.Button
{
    public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<CardAction, object?>(
        nameof(Header));

    public static readonly StyledProperty<object?> SubtitleProperty = AvaloniaProperty.Register<CardAction, object?>(
        nameof(Subtitle));

    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<CardAction, object?>(
        nameof(Icon),
        coerce: CoerceIcon);

    public static readonly StyledProperty<IBrush?> IconForegroundProperty = AvaloniaProperty.Register<CardAction, IBrush?>(
        nameof(IconForeground));

    public static readonly StyledProperty<bool> IsChevronVisibleProperty = AvaloniaProperty.Register<CardAction, bool>(
        nameof(IsChevronVisible),
        true);

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IBrush? IconForeground
    {
        get => GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    public bool IsChevronVisible
    {
        get => GetValue(IsChevronVisibleProperty);
        set => SetValue(IsChevronVisibleProperty, value);
    }

    /// <summary>
    /// XAML often assigns glyph names (Icon="Home24") to the object-typed Icon slot;
    /// turn those into a rendered <see cref="SymbolIcon" /> so templates can display them.
    /// </summary>
    internal static object? CoerceIcon(AvaloniaObject element, object? value)
    {
        if (value is string glyph && Enum.TryParse<SymbolRegular>(glyph, out var symbol))
            return new SymbolIcon { Symbol = symbol };

        return value;
    }
}
