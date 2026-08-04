namespace UniversalDeviceToolkit.Shared.Settings;

/// <summary>
/// Cross-platform DTO for the Avalonia settings page theme/accent preferences.
/// Uses only primitive, portable types so the Avalonia (net10.0) project can persist theme
/// choices without referencing the Windows-only Lib enums (Theme / AccentColorSource / RGBColor).
/// </summary>
public sealed class AvaloniaThemePreferenceStore
{
    /// <summary>Selected theme: "System", "Light" or "Dark".</summary>
    public string Theme { get; set; } = "System";

    /// <summary>Whether the accent color should be applied to the theme.</summary>
    public bool ApplyAccentColorToTheme { get; set; } = true;

    /// <summary>Whether a selected custom accent should also be written to the Windows system.</summary>
    public bool ApplyAccentColorToSystem { get; set; } = true;

    /// <summary>True maps to AccentColorSource.System; false means a custom accent color is used.</summary>
    public bool UseSystemAccent { get; set; } = true;

    /// <summary>Custom accent color in "#RRGGBB" form; ignored when <see cref="UseSystemAccent"/> is true.</summary>
    public string? AccentColorHex { get; set; }

    /// <summary>Temperature display unit: "Celsius" or "Fahrenheit".</summary>
    public string TemperatureUnit { get; set; } = "Celsius";

    /// <summary>Portable font family choice used by the Avalonia host.</summary>
    public string FontFamily { get; set; } = "Default";

    /// <summary>Portable UI density choice: Compact, Standard, Large or ExtraLarge.</summary>
    public string UiScale { get; set; } = "Standard";
}

/// <summary>
/// Persists <see cref="AvaloniaThemePreferenceStore"/> to a dedicated cross-platform settings file.
/// A separate file (not ApplicationSettings' settings.json) is used because AbstractSettings performs
/// full-document serialization and would otherwise clobber unrelated fields owned by other layers.
/// </summary>
public sealed class AvaloniaThemePreferences : AbstractSettings<AvaloniaThemePreferenceStore>
{
    public AvaloniaThemePreferences() : base("avalonia-theme.json")
    {
    }
}
