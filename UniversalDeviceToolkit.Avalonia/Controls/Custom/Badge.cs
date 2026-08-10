using Avalonia;
using Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

public enum BadgeAppearance
{
    Primary,
    Secondary,
    Success,
    Caution,
    Danger,
    Info,
    Transparent,
    Dark,
    Light
}

// AVALONIA: WPF-UI Badge + AutomationPeer replaced by a ContentControl with the same public members.
public class Badge : ContentControl
{
    public static readonly StyledProperty<BadgeAppearance> AppearanceProperty = AvaloniaProperty.Register<Badge, BadgeAppearance>(
        nameof(Appearance),
        BadgeAppearance.Primary);

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = AvaloniaProperty.Register<Badge, CornerRadius>(
        nameof(CornerRadius));

    public BadgeAppearance Appearance
    {
        get => GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}
