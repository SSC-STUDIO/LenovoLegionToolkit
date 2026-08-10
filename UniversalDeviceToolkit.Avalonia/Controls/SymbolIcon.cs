using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Icon glyph rendered with the bundled Fluent System Icons (regular) font.
/// Mirrors the WPF-UI SymbolIcon control: set <see cref="Symbol"/> to a
/// <see cref="SymbolRegular"/> glyph code (enum values are font codepoints).
/// </summary>
public class SymbolIcon : TextBlock
{
    public const string SymbolFontFamilyName = "FluentSystemIcons-Regular";
    public const string SymbolFontUri =
        "avares://UniversalDeviceToolkit.Avalonia/Assets/Fonts/fluentsystemicons-regular.ttf#FluentSystemIcons-Regular";

    public static readonly StyledProperty<SymbolRegular> SymbolProperty =
        AvaloniaProperty.Register<SymbolIcon, SymbolRegular>(nameof(Symbol), SymbolRegular.Empty);

    /// <summary>
    /// Gets or sets the glyph to render.
    /// </summary>
    public SymbolRegular Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    static SymbolIcon()
    {
        AffectsRender<SymbolIcon>(SymbolProperty);
    }

    public SymbolIcon()
    {
        FontFamily = new FontFamily(SymbolFontUri);
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
        UpdateGlyph();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SymbolProperty)
            UpdateGlyph();
    }

    private void UpdateGlyph()
    {
        var value = (uint)Symbol;
        Text = value == 0 ? string.Empty : ((char)value).ToString();
    }
}
