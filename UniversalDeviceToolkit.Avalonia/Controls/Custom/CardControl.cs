using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

// AVALONIA: WPF-UI CardControl (Button) + AutomationPeer replaced by an Avalonia Button
// subclass with the same public members. Template lives in Styles/CardControl.axaml.
public class CardControl : Avalonia.Controls.Button
{
    public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<CardControl, object?>(
        nameof(Header));

    public static readonly StyledProperty<object?> SubtitleProperty = AvaloniaProperty.Register<CardControl, object?>(
        nameof(Subtitle));

    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<CardControl, object?>(
        nameof(Icon),
        coerce: CardAction.CoerceIcon);

    public static readonly StyledProperty<IBrush?> IconForegroundProperty = AvaloniaProperty.Register<CardControl, IBrush?>(
        nameof(IconForeground));

    public static readonly StyledProperty<Style?> HeaderStyleProperty = AvaloniaProperty.Register<CardControl, Style?>(
        nameof(HeaderStyle));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = AvaloniaProperty.Register<CardControl, CornerRadius>(
        nameof(CornerRadius));

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

    public Style? HeaderStyle
    {
        get => GetValue(HeaderStyleProperty);
        set => SetValue(HeaderStyleProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}
