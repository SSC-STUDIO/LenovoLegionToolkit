using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

// AVALONIA: WPF-UI CardExpander + AutomationPeer replaced by an Avalonia Expander
// subclass with the same public members. Template lives in Styles/CardExpander.axaml.
public class CardExpander : Expander
{
    public static readonly StyledProperty<object?> SubtitleProperty = AvaloniaProperty.Register<CardExpander, object?>(
        nameof(Subtitle));

    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<CardExpander, object?>(
        nameof(Icon),
        coerce: CardAction.CoerceIcon);

    public static readonly StyledProperty<IBrush?> IconForegroundProperty = AvaloniaProperty.Register<CardExpander, IBrush?>(
        nameof(IconForeground));

    public static readonly StyledProperty<bool> IsChevronVisibleProperty = AvaloniaProperty.Register<CardExpander, bool>(
        nameof(IsChevronVisible),
        true);

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = AvaloniaProperty.Register<CardExpander, CornerRadius>(
        nameof(CornerRadius));

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

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}
