using Avalonia;
using System;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// WPF-UI compatible text box: adds <see cref="ClearButtonEnabled"/>, <see cref="Icon"/>
/// and <see cref="Appearance"/> on top of <see cref="Avalonia.Controls.TextBox"/>.
/// </summary>
public class TextBox : global::Avalonia.Controls.TextBox
{
    protected override Type StyleKeyOverride => typeof(global::Avalonia.Controls.TextBox);

    /// <summary>Defines the <see cref="ClearButtonEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> ClearButtonEnabledProperty =
        AvaloniaProperty.Register<TextBox, bool>(nameof(ClearButtonEnabled), false);

    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<TextBox, object?>(nameof(Icon));

    /// <summary>Defines the <see cref="Appearance"/> property.</summary>
    public static readonly StyledProperty<ControlAppearance> AppearanceProperty =
        AvaloniaProperty.Register<TextBox, ControlAppearance>(nameof(Appearance), ControlAppearance.Secondary);

    /// <summary>
    /// Gets or sets a value indicating whether the clear (X) button is shown.
    /// </summary>
    public bool ClearButtonEnabled
    {
        get => GetValue(ClearButtonEnabledProperty);
        set => SetValue(ClearButtonEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon shown in the text box. A <see cref="SymbolIcon"/> (or any
    /// control) is the common value; strings such as <c>"Search24"</c> are converted to a
    /// <see cref="SymbolIcon"/> automatically.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the visual appearance of the text box.
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
