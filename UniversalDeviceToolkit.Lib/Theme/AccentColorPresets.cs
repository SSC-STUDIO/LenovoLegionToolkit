namespace UniversalDeviceToolkit.Lib;

/// <summary>
/// Predefined accent color swatches shared by the WPF and Avalonia UI layers.
/// The system-accent dot is handled separately by the UI layer (clicking it sets
/// <see cref="AccentColorSource.System"/>) and is intentionally not part of this array.
/// </summary>
public static class AccentColorPresets
{
    /// <summary>Solid accent color presets, each paired with a short English key.</summary>
    public static readonly (RGBColor Color, string Key)[] Swatches =
    [
        (new RGBColor(0, 120, 212), "Blue"),    // #0078D4
        (new RGBColor(177, 70, 194), "Purple"), // #B146C2
        (new RGBColor(227, 0, 140), "Pink"),    // #E3008C
        (new RGBColor(232, 17, 35), "Red"),     // #E81123
        (new RGBColor(247, 99, 12), "Orange"),  // #F7630C
        (new RGBColor(255, 185, 0), "Amber"),   // #FFB900
        (new RGBColor(16, 124, 16), "Green"),   // #107C10
        (new RGBColor(128, 128, 128), "Gray"),  // #808080
    ];
}
