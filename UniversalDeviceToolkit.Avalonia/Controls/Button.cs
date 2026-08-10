using System;
using Avalonia;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// WPF-UI compatible button: adds <see cref="Appearance"/>, <see cref="Icon"/> and
/// <see cref="AppearanceMode"/> on top of <see cref="Avalonia.Controls.Button"/>.
/// Styles may target the appearance via property selectors, e.g.
/// <c>Selector="Button[Appearance=Caution]"</c>.
/// </summary>
public class Button : global::Avalonia.Controls.Button
{
    protected override Type StyleKeyOverride => typeof(global::Avalonia.Controls.Button);

    /// <summary>Defines the <see cref="Appearance"/> property.</summary>
    public static readonly StyledProperty<ControlAppearance> AppearanceProperty =
        AvaloniaProperty.Register<Button, ControlAppearance>(nameof(Appearance), ControlAppearance.Secondary);

    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<Button, object?>(nameof(Icon));

    /// <summary>Defines the <see cref="AppearanceMode"/> property.</summary>
    public static readonly StyledProperty<AppearanceMode> AppearanceModeProperty =
        AvaloniaProperty.Register<Button, AppearanceMode>(nameof(AppearanceMode), AppearanceMode.None);

    /// <summary>Defines the <see cref="PressedForeground"/> property.</summary>
    public static readonly StyledProperty<IBrush?> PressedForegroundProperty =
        AvaloniaProperty.Register<Button, IBrush?>(nameof(PressedForeground));

    /// <summary>Defines the <see cref="MouseOverBackground"/> property.</summary>
    public static readonly StyledProperty<IBrush?> MouseOverBackgroundProperty =
        AvaloniaProperty.Register<Button, IBrush?>(nameof(MouseOverBackground));

    /// <summary>Defines the <see cref="MouseOverBorderBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> MouseOverBorderBrushProperty =
        AvaloniaProperty.Register<Button, IBrush?>(nameof(MouseOverBorderBrush));

    /// <summary>
    /// Gets or sets the visual appearance of the button.
    /// </summary>
    public ControlAppearance Appearance
    {
        get => GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon shown in the button. A <see cref="SymbolIcon"/> (or any
    /// control) is the common value; strings such as <c>"Home24"</c> are converted to a
    /// <see cref="SymbolIcon"/> automatically.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the appearance mode of the button.
    /// </summary>
    public AppearanceMode AppearanceMode
    {
        get => GetValue(AppearanceModeProperty);
        set => SetValue(AppearanceModeProperty, value);
    }

    /// <summary>
    /// Foreground used while the button is pressed (WPF-UI compatibility; consumed by styles).
    /// </summary>
    public IBrush? PressedForeground
    {
        get => GetValue(PressedForegroundProperty);
        set => SetValue(PressedForegroundProperty, value);
    }

    /// <summary>
    /// Background used while the pointer is over the button (WPF-UI compatibility; consumed by styles).
    /// </summary>
    public IBrush? MouseOverBackground
    {
        get => GetValue(MouseOverBackgroundProperty);
        set => SetValue(MouseOverBackgroundProperty, value);
    }

    /// <summary>
    /// Border brush used while the pointer is over the button (WPF-UI compatibility; consumed by styles).
    /// </summary>
    public IBrush? MouseOverBorderBrush
    {
        get => GetValue(MouseOverBorderBrushProperty);
        set => SetValue(MouseOverBorderBrushProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
            IconHelper.TryConvertStringIcon(this, IconProperty, change.NewValue);
    }
}

/// <summary>
/// Shared conversion helper: turns <c>Icon="SymbolName"</c> attribute strings into
/// <see cref="SymbolIcon"/> instances (same behavior as WPF-UI's IconElement.GetIcon).
/// </summary>
internal static class IconHelper
{
    public static void TryConvertStringIcon(AvaloniaObject target, StyledProperty<object?> iconProperty, object? value)
    {
        if (value is not string iconName)
            return;

        if (!Enum.TryParse<SymbolRegular>(iconName, out var symbol))
            return;

        target.SetCurrentValue(iconProperty, new SymbolIcon { Symbol = symbol });
    }
}
