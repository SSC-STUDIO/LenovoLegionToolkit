using Avalonia;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// WPF-UI compatible toggle switch: adds <see cref="Icon"/> and <see cref="Appearance"/>
/// on top of <see cref="Avalonia.Controls.ToggleSwitch"/>.
/// </summary>
public class ToggleSwitch : global::Avalonia.Controls.ToggleSwitch
{
    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<ToggleSwitch, object?>(nameof(Icon));

    /// <summary>Defines the <see cref="Appearance"/> property.</summary>
    public static readonly StyledProperty<ControlAppearance> AppearanceProperty =
        AvaloniaProperty.Register<ToggleSwitch, ControlAppearance>(nameof(Appearance), ControlAppearance.Secondary);

    /// <summary>
    /// Gets or sets the icon shown in the toggle switch. A <see cref="SymbolIcon"/> (or any
    /// control) is the common value; strings such as <c>"Play24"</c> are converted to a
    /// <see cref="SymbolIcon"/> automatically.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the visual appearance of the toggle switch.
    /// </summary>
    public ControlAppearance Appearance
    {
        get => GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
            IconHelper.TryConvertStringIcon(this, IconProperty, change.NewValue);
    }
}
