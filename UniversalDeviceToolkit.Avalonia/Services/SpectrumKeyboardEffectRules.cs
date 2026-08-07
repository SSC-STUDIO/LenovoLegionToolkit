namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Mirrors the WPF Spectrum effect editor's type-specific parameter rules.
/// </summary>
public static class SpectrumKeyboardEffectRules
{
    public static bool SupportsDirection(string? effectType) => Is(effectType,
        "ColorWave", "RainbowWave");

    public static bool SupportsClockwiseDirection(string? effectType) => Is(effectType, "RainbowScrew");

    public static bool SupportsSpeed(string? effectType) => Is(effectType,
        "ColorChange", "ColorPulse", "ColorWave", "Rain", "RainbowScrew", "RainbowWave", "Ripple", "Smooth", "Type");

    public static bool SupportsColors(string? effectType) => Is(effectType,
        "Always", "ColorChange", "ColorPulse", "ColorWave", "Rain", "Ripple", "Smooth", "Type");

    public static bool UsesSingleColor(string? effectType) => Is(effectType, "Always");

    public static bool HidesKeySelection(string? effectType)
    {
        return IsAllLightsEffect(effectType) || IsWholeKeyboardEffect(effectType);
    }

    public static IReadOnlyList<ushort> NormalizeKeys(
        string? effectType,
        IEnumerable<ushort> selectedKeys,
        IEnumerable<ushort> allKeyboardKeys)
    {
        if (IsAllLightsEffect(effectType))
            return [];
        if (IsWholeKeyboardEffect(effectType))
            return allKeyboardKeys.Distinct().OrderBy(key => key).ToArray();

        return selectedKeys.Distinct().OrderBy(key => key).ToArray();
    }

    public static IReadOnlyList<KeyboardColorState> NormalizeColors(
        string? effectType,
        IEnumerable<KeyboardColorState> colors)
    {
        return SupportsColors(effectType) ? colors.ToArray() : [];
    }

    private static bool IsAllLightsEffect(string? effectType) => Is(effectType,
        "AudioBounce", "AudioRipple", "AuroraSync");

    private static bool IsWholeKeyboardEffect(string? effectType) => Is(effectType, "Ripple", "Type");

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
