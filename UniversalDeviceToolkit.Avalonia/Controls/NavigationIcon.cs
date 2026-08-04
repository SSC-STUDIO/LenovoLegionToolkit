using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Renders the shared navigation icon identifiers without exposing the identifier
/// itself as user-facing text. The glyphs use symbols available in the standard
/// Windows/Linux UI fonts and remain monochrome so they follow the navigation theme.
/// </summary>
public sealed class NavigationIcon : TextBlock
{
    private static readonly IReadOnlyDictionary<string, string> Glyphs =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PaintBrush24"] = "✎",
            ["Apps24"] = "▦",
            ["Keyboard24"] = "⌨",
            ["Desktop24"] = "▣",
            ["ArrowSync24"] = "↻",
            ["Battery024"] = "▰",
            ["PlugConnected24"] = "⚡",
        };

    public static readonly StyledProperty<string?> IconIdentifierProperty =
        AvaloniaProperty.Register<NavigationIcon, string?>(nameof(IconIdentifier));

    public string? IconIdentifier
    {
        get => GetValue(IconIdentifierProperty);
        set => SetValue(IconIdentifierProperty, value);
    }

    public NavigationIcon()
    {
        FontFamily = new FontFamily("Segoe UI Symbol");
        TextAlignment = TextAlignment.Center;
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
        UpdateGlyph();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconIdentifierProperty)
            UpdateGlyph();
    }

    private void UpdateGlyph()
    {
        Text = IconIdentifier is not null && Glyphs.TryGetValue(IconIdentifier, out var glyph)
            ? glyph
            : "•";
    }
}
