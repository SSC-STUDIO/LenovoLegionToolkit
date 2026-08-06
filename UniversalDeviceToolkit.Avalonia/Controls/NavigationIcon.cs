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
            ["ArrowReset24"] = "\uE72C",
            ["ArrowClockwise24"] = "\uF13E",
            ["ArrowExportLtr24"] = "\uE0C8",
            ["ArrowImport24"] = "\uF15A",
            ["ArrowRepeatAll24"] = "\uF172",
            ["ChevronDown24"] = "\uE70D",
            ["ChevronUp24"] = "\uE70E",
            ["Add24"] = "\uE710",
            ["Edit24"] = "\uE70F",
            ["Delete24"] = "\uE74D",
            ["Save24"] = "\uE74E",
            ["Battery024"] = "\uE850",
            ["PlugConnected24"] = "\uE839",
            // Dashboard icons use the same identifiers as the WPF host. The
            // Segoe fallback glyphs keep the semantic distinction visible on
            // systems where the Fluent icon font is not installed.
            ["BatteryCharge24"] = "\uE83F",
            ["WeatherMoon24"] = "\uE708",
            ["UsbStick24"] = "\uE839",
            ["PlugDisconnected24"] = "\uE8E6",
            ["LeafOne24"] = "\uE793",
            ["DeveloperBoard24"] = "\uE950",
            ["DeveloperBoardLightning20"] = "\uE8C4",
            ["ScaleFill24"] = "\uE8A7",
            ["DesktopPulse24"] = "\uE7F4",
            ["TextFontSize24"] = "\uE8D2",
            ["Hdr24"] = "\uE7B7",
            ["TopSpeed24"] = "\uE9D9",
            ["LightbulbCircle24"] = "\uE793",
            ["UsbPlug24"] = "\uE839",
            ["Mic24"] = "\uE720",
            ["Tablet24"] = "\uE7F4",
            ["Power24"] = "\uE945",
            ["Checkmark24"] = "\uE73E",
            ["CheckmarkCircle24"] = "\uE73E",
            ["Warning24"] = "\uE7BA",
            ["ErrorCircle24"] = "\uEA39",
            ["Dismiss24"] = "\uE711",
            ["ToggleRight24"] = "\uF82C",
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

    public static bool HasGlyph(string? identifier) =>
        !string.IsNullOrWhiteSpace(identifier) && Glyphs.ContainsKey(identifier);

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
