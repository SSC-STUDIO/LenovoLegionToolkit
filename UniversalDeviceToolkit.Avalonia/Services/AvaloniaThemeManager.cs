using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using UniversalDeviceToolkit.Shared.Logging;
using UniversalDeviceToolkit.Shared.Settings;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Applies the persisted appearance preferences to the Avalonia shell:
/// theme variant (Light/Dark/System), accent color, font family and the UI
/// scale factor. Mirrors the WPF ThemeManager surface without depending on
/// WPF/Wpf.Ui. Windows builds additionally honor the shared
/// ApplicationSettings store (ThemeStylePreset / AccentColor / AppFontStyle /
/// AppTextSize / AppScale); portable builds read the cross-platform
/// avalonia-theme.json store.
/// </summary>
public sealed class AvaloniaThemeManager
{
    // Accent resource keys owned by the Fluent theme (SystemAccentColor family)
    // plus the host DesignTokens accent brushes. Overriding them at the
    // application level retints the whole shell like WPF's accent application.
    private static readonly string[] FluentAccentResourceKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1",
        "SystemAccentColorLight2",
        "SystemAccentColorLight3",
        "SystemAccentColorDark1",
        "SystemAccentColorDark2",
        "SystemAccentColorDark3",
    ];

    private const string DefaultFontFamilyName = "Default";

    private readonly AvaloniaThemePreferences _themePreferences = new();
    private double _uiScale = 1d;

    private AvaloniaThemeManager()
    {
    }

    /// <summary>Process-wide singleton consumed by the App shell and windows.</summary>
    public static AvaloniaThemeManager Instance { get; } = new();

    /// <summary>Raised after every successful apply (theme, accent, font, scale).</summary>
    public event EventHandler? ThemeApplied;

    /// <summary>Raised when only the UI scale factor changed.</summary>
    public event EventHandler<double>? UiScaleChanged;

    /// <summary>Current UI scale factor (1.0 = 100%).</summary>
    public double UiScaleFactor => _uiScale;

    /// <summary>
    /// Re-applies every persisted appearance preference to the running shell.
    /// </summary>
    public void Apply()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(Apply);
            return;
        }

        try
        {
            ApplyThemeVariant();
            ApplyAccentColor();
            ApplyFontFamily();
            ApplyUiScale();
        }
        catch (Exception ex)
        {
            SharedLog.Warning("Failed to apply persisted Avalonia appearance preferences.", ex);
        }

        ThemeApplied?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Re-applies preferences; safe to call whenever settings may have changed.</summary>
    public void Reapply() => Apply();

    /// <summary>
    /// Maps a persisted theme name to an Avalonia theme variant.
    /// Accepts "Light"/"Dark"/"System" (portable store) and the Lib Theme enum
    /// names (System/Light/Dark).
    /// </summary>
    public static ThemeVariant MapThemeVariant(string? theme)
    {
        if (string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase))
            return ThemeVariant.Light;

        if (string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase))
            return ThemeVariant.Dark;

        return ThemeVariant.Default;
    }

    /// <summary>
    /// Maps a persisted UI scale name to a scale factor. Mirrors the WPF
    /// AppScale percentages: Compact=90%, Standard=100%, Large=110%,
    /// ExtraLarge=125%.
    /// </summary>
    public static double ResolveUiScaleFactor(string? uiScale)
    {
        return uiScale?.Trim() switch
        {
            "Compact" => 0.90d,
            "Large" => 1.10d,
            "ExtraLarge" => 1.25d,
            _ => 1.0d,
        };
    }

    /// <summary>
    /// Parses a "#RRGGBB" (or "RRGGBB" / "#AARRGGBB") hex color into an
    /// Avalonia color. Returns null for null, empty or malformed input.
    /// </summary>
    public static Color? ParseAccentColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        var normalized = hex.Trim().TrimStart('#');
        if (normalized.Length == 8)
            normalized = normalized[2..];

        if (normalized.Length != 6)
            return null;

        if (!byte.TryParse(normalized.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var r)
            || !byte.TryParse(normalized.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var g)
            || !byte.TryParse(normalized.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var b))
            return null;

        return Color.FromRgb(r, g, b);
    }

    /// <summary>
    /// Resolves a persisted font family name to a renderable family string.
    /// "Default" (or null/empty) returns null so the host keeps the framework
    /// default font.
    /// </summary>
    public static string? ResolveFontFamily(string? fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily)
            || string.Equals(fontFamily, DefaultFontFamilyName, StringComparison.OrdinalIgnoreCase))
            return null;

        return fontFamily.Trim();
    }

    /// <summary>
    /// Maps the Windows-only AppFontStyle enum name (as persisted by the shared
    /// settings service) to a renderable font family string, matching the WPF
    /// option labels. Unknown or default styles resolve to null.
    /// </summary>
    public static string? ResolveWindowsFontStyleName(string? appFontStyle)
    {
        return appFontStyle?.Trim() switch
        {
            "FluentVariable" => "Segoe UI Variable",
            "YaHeiUI" => "Microsoft YaHei UI",
            "DengXian" => "DengXian",
            "NotoSans" => "Noto Sans CJK SC",
            "SimHei" => "SimHei",
            "SimSun" => "SimSun",
            "KaiTi" => "KaiTi",
            _ => null,
        };
    }

    /// <summary>
    /// Maps the persisted UI scale name to the same factor as
    /// <see cref="ResolveUiScaleFactor"/>, accepting the Windows AppScale enum
    /// name too ("Small" → 90%, "Standard" → 100%, ...).
    /// </summary>
    public static double ResolveAppScaleFactor(string? appScale)
    {
        return appScale?.Trim() switch
        {
            "Compact" => 0.80d,
            "Small" => 0.90d,
            "Large" => 1.10d,
            "ExtraLarge" => 1.25d,
            _ => 1.0d,
        };
    }

    private void ApplyThemeVariant()
    {
        if (Application.Current is not { } app)
            return;

        var themeName = GetPersistedThemeName();
        app.RequestedThemeVariant = MapThemeVariant(themeName);
    }

    private string GetPersistedThemeName()
    {
#if WINDOWS
        try
        {
            var store = WindowsAvaloniaSettingsService.SharedApplicationSettings.Store;
            return store.Theme.ToString();
        }
        catch
        {
            // Fall through to the portable theme store.
        }
#endif
        return _themePreferences.Store.Theme;
    }

    private void ApplyAccentColor()
    {
        if (Application.Current is not { } app)
            return;

        var (accent, apply) = ResolveAccentColor();
        if (!apply || accent is not { } color)
        {
            ClearAccentOverride();
            return;
        }

        var resources = app.Resources;
        resources["SystemAccentColor"] = color;
        resources["SystemAccentColorLight1"] = Lighten(color, 0.15);
        resources["SystemAccentColorLight2"] = Lighten(color, 0.30);
        resources["SystemAccentColorLight3"] = Lighten(color, 0.45);
        resources["SystemAccentColorDark1"] = Darken(color, 0.15);
        resources["SystemAccentColorDark2"] = Darken(color, 0.30);
        resources["SystemAccentColorDark3"] = Darken(color, 0.45);
        resources["AccentBackgroundBrush"] = new SolidColorBrush(color);
        resources["AccentBackgroundBrushDark"] = new SolidColorBrush(Darken(color, 0.12));
        resources["AccentBackgroundBrushLight"] = new SolidColorBrush(Lighten(color, 0.18));
    }

    private void ClearAccentOverride()
    {
        if (Application.Current is not { } app)
            return;

        foreach (var key in FluentAccentResourceKeys)
            app.Resources.Remove(key);
        app.Resources.Remove("AccentBackgroundBrush");
        app.Resources.Remove("AccentBackgroundBrushDark");
        app.Resources.Remove("AccentBackgroundBrushLight");
    }

    private (Color? Color, bool Apply) ResolveAccentColor()
    {
#if WINDOWS
        try
        {
            var store = WindowsAvaloniaSettingsService.SharedApplicationSettings.Store;
            if (store.ApplyAccentColorToTheme && store.AccentColor is { } rgb)
                return (Color.FromRgb(rgb.R, rgb.G, rgb.B), true);
            if (store.ApplyAccentColorToTheme)
                return (null, true);
            return (null, false);
        }
        catch
        {
            // Fall through to the portable theme store.
        }
#endif
        var preferences = _themePreferences.Store;
        if (!preferences.ApplyAccentColorToTheme)
            return (null, false);

        if (preferences.UseSystemAccent)
            return (null, true);

        return (ParseAccentColor(preferences.AccentColorHex), true);
    }

    private void ApplyFontFamily()
    {
        if (Application.Current is not { } app)
            return;

        var fontFamilyName = GetPersistedFontFamilyName();
        var family = ResolveFontFamily(fontFamilyName);
        if (family is null)
            app.Resources.Remove("FontFamily");
        else
            app.Resources["FontFamily"] = new FontFamily(family);
    }

    private string? GetPersistedFontFamilyName()
    {
#if WINDOWS
        try
        {
            var store = WindowsAvaloniaSettingsService.SharedApplicationSettings.Store;
            return ResolveWindowsFontStyleName(store.AppFontStyle.ToString());
        }
        catch
        {
            // Fall through to the portable theme store.
        }
#endif
        return _themePreferences.Store.FontFamily;
    }

    private void ApplyUiScale()
    {
        var scale = GetPersistedUiScaleFactor();
        if (Math.Abs(scale - _uiScale) < 0.0001)
            return;

        _uiScale = scale;
        UiScaleChanged?.Invoke(this, scale);
    }

    private double GetPersistedUiScaleFactor()
    {
#if WINDOWS
        try
        {
            var store = WindowsAvaloniaSettingsService.SharedApplicationSettings.Store;
            return ResolveAppScaleFactor(store.AppScale.ToString());
        }
        catch
        {
            // Fall through to the portable theme store.
        }
#endif
        return ResolveUiScaleFactor(_themePreferences.Store.UiScale);
    }

    private static Color Lighten(Color color, double factor) => Color.FromArgb(
        color.A,
        (byte)(color.R + (255 - color.R) * factor),
        (byte)(color.G + (255 - color.G) * factor),
        (byte)(color.B + (255 - color.B) * factor));

    private static Color Darken(Color color, double factor) => Color.FromArgb(
        color.A,
        (byte)(color.R * (1 - factor)),
        (byte)(color.G * (1 - factor)),
        (byte)(color.B * (1 - factor)));
}
