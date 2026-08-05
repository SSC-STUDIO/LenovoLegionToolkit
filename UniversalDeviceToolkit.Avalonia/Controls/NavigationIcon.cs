using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Renders shared navigation icon identifiers without exposing the identifier
/// itself as user-facing text. Segoe Fluent Icons codepoints are used when the
/// font is available; the symbol fallback remains legible on other platforms.
/// </summary>
public sealed class NavigationIcon : TextBlock
{
    private static readonly IReadOnlyDictionary<string, string> Glyphs =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Home24"] = "\uE80F",
            ["Info24"] = "\uE946",
            ["Settings24"] = "\uE713",
            ["PaintBrush24"] = "\uE790",
            ["Apps24"] = "\uE71D",
            ["Keyboard24"] = "\uE765",
            ["Rocket24"] = "\uE99A",
            ["ReceiptPlay24"] = "\uE90B",
            ["Gauge24"] = "\uE9D9",
            ["Desktop24"] = "\uE7F4",
            ["ArrowSync24"] = "\uE895",
            ["ArrowLeft24"] = "\uE72B",
            ["ArrowRight24"] = "\uE72A",
            ["ArrowUp24"] = "\uE74A",
            ["ArrowDown24"] = "\uE74B",
            ["Battery024"] = "\uE850",
            ["PlugConnected24"] = "\uE839",
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
        FontFamily = new FontFamily("Segoe MDL2 Assets, Segoe Fluent Icons, Segoe UI Symbol");
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
            : "\uE7B7";
    }
}
