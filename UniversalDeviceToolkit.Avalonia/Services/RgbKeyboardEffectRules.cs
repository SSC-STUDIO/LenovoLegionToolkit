namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Preserves the RGB keyboard editor capabilities exposed by the WPF host.
/// </summary>
public static class RgbKeyboardEffectRules
{
    public static bool SupportsSpeed(string? effectType) => !Is(effectType, "Static");

    public static bool SupportsZones(string? effectType) =>
        Is(effectType, "Static", "Breath");

    private static bool Is(string? effectType, params string[] values)
    {
        foreach (var value in values)
        {
            if (string.Equals(effectType, value, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
