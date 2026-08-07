using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using UniversalDeviceToolkit.Shared.Settings;
#if WINDOWS
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;
#endif

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Applies the shared appearance contract to Avalonia at startup and when the
/// Appearance settings page changes. Windows hosts read <c>settings.json</c>,
/// while portable hosts retain the primitive Avalonia preference file.
/// </summary>
public static class AvaloniaAppearanceManager
{
    private static readonly List<WeakReference<Window>> RegisteredWindows = [];
    private static AvaloniaAppearanceState _current = AvaloniaAppearanceState.Default;

    public static AvaloniaAppearanceState GetCurrentState(AvaloniaThemePreferenceStore portableFallback)
    {
#if WINDOWS
        return FromApplicationSettings(WindowsAvaloniaSettingsService.SharedApplicationSettings);
#else
        return FromPortablePreferences(portableFallback);
#endif
    }

    public static void Apply(AvaloniaThemePreferenceStore portablePreferences) =>
        Apply(FromPortablePreferences(portablePreferences));

    public static void Apply(AvaloniaAppearanceState state)
    {
        _current = state;

        if (Application.Current is { } application)
        {
            application.RequestedThemeVariant = state.Theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };

            ApplyAccent(application, state);
        }

        ApplyToRegisteredWindows();
    }

#if WINDOWS
    public static void Apply(ApplicationSettings settings) => Apply(FromApplicationSettings(settings));

    public static AvaloniaAppearanceState FromApplicationSettings(ApplicationSettings settings)
    {
        var store = settings.Store;
        var accent = store.AccentColor;
        return new AvaloniaAppearanceState(
            store.Theme.ToString(),
            store.ApplyAccentColorToTheme,
            store.ApplyAccentColorToSystem,
            store.AccentColorSource != AccentColorSource.Custom,
            accent is { } color ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : null,
            store.TemperatureUnit == TemperatureUnit.F ? "Fahrenheit" : "Celsius",
            FormatFontStyle(store.AppFontStyle),
            FormatUiScale(store.AppTextSize, store.AppScale));
    }
#endif

    public static AvaloniaAppearanceState FromPortablePreferences(AvaloniaThemePreferenceStore store) =>
        new(
            store.Theme,
            store.ApplyAccentColorToTheme,
            store.ApplyAccentColorToSystem,
            store.UseSystemAccent,
            store.AccentColorHex,
            store.TemperatureUnit,
            store.FontFamily,
            store.UiScale);

    /// <summary>
    /// Registers a top-level window for live typography and layout-density updates.
    /// The host is inserted once, leaving the existing XAML tree and named controls intact.
    /// </summary>
    public static void Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window.Content is Control content)
        {
            if (content is not AvaloniaAppScaleHost)
                window.Content = new AvaloniaAppScaleHost { Child = content };
        }

        RegisteredWindows.Add(new WeakReference<Window>(window));
        ApplyToWindow(window);
    }

    private static void ApplyToRegisteredWindows()
    {
        for (var index = RegisteredWindows.Count - 1; index >= 0; index--)
        {
            if (!RegisteredWindows[index].TryGetTarget(out var window))
            {
                RegisteredWindows.RemoveAt(index);
                continue;
            }

            ApplyToWindow(window);
        }
    }

    private static void ApplyToWindow(Window window)
    {
        window.FontFamily = new FontFamily(GetFontFamilyChain(_current.FontFamily));
        window.FontSize = 15d * GetTextScale(_current.UiScale);

        if (window.Content is AvaloniaAppScaleHost scaleHost)
            scaleHost.Scale = GetLayoutScale(_current.UiScale);

        window.InvalidateMeasure();
        window.InvalidateArrange();
        window.InvalidateVisual();
    }

    private static void ApplyAccent(Application application, AvaloniaAppearanceState state)
    {
        if (!state.ApplyAccentColorToTheme
            || state.UseSystemAccent
            || !Color.TryParse(state.AccentColorHex, out var accent))
        {
            ClearAccentOverride(application);
            return;
        }

        application.Resources["SystemAccentColor"] = accent;
        application.Resources["SystemAccentColorLight1"] = Lighten(accent, 0.15);
        application.Resources["SystemAccentColorLight2"] = Lighten(accent, 0.30);
        application.Resources["SystemAccentColorLight3"] = Lighten(accent, 0.45);
        application.Resources["SystemAccentColorDark1"] = Darken(accent, 0.15);
        application.Resources["SystemAccentColorDark2"] = Darken(accent, 0.30);
        application.Resources["SystemAccentColorDark3"] = Darken(accent, 0.45);
    }

    private static void ClearAccentOverride(Application application)
    {
        foreach (var key in AccentResourceKeys)
            application.Resources.Remove(key);
    }

    private static readonly string[] AccentResourceKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
    ];

    private static string GetFontFamilyChain(string fontFamily) => fontFamily switch
    {
        "Segoe UI Variable" => "Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI, Microsoft YaHei",
        "Microsoft YaHei UI" => "Microsoft YaHei UI, Segoe UI, Microsoft YaHei",
        "DengXian" => "DengXian, Segoe UI, Microsoft YaHei UI",
        "Noto Sans CJK SC" => "Noto Sans CJK SC, Source Han Sans SC, Segoe UI, Microsoft YaHei UI",
        "SimHei" => "SimHei, Microsoft YaHei UI, Segoe UI",
        "SimSun" => "SimSun, NSimSun, Microsoft YaHei UI, Segoe UI",
        "KaiTi" => "KaiTi, Microsoft YaHei UI, Segoe UI",
        _ => "Segoe UI, Microsoft YaHei UI, Microsoft YaHei, Noto Sans CJK SC, SimSun",
    };

    private static double GetTextScale(string uiScale) => uiScale switch
    {
        "Compact" => 0.90d,
        "Large" => 1.10d,
        "ExtraLarge" => 1.25d,
        _ => 1d,
    };

    private static double GetLayoutScale(string uiScale) => uiScale switch
    {
        "Compact" => 0.90d,
        "Large" => 1.10d,
        "ExtraLarge" => 1.25d,
        _ => 1d,
    };

#if WINDOWS
    private static string FormatFontStyle(AppFontStyle value) => value switch
    {
        AppFontStyle.FluentVariable => "Segoe UI Variable",
        AppFontStyle.YaHeiUI => "Microsoft YaHei UI",
        AppFontStyle.DengXian => "DengXian",
        AppFontStyle.NotoSans => "Noto Sans CJK SC",
        AppFontStyle.SimHei => "SimHei",
        AppFontStyle.SimSun => "SimSun",
        AppFontStyle.KaiTi => "KaiTi",
        _ => "Default",
    };

    private static string FormatUiScale(AppTextSize textSize, AppScale layoutScale) => (textSize, layoutScale) switch
    {
        (AppTextSize.Compact, AppScale.Small) => "Compact",
        (AppTextSize.Large, AppScale.Large) => "Large",
        (AppTextSize.ExtraLarge, AppScale.ExtraLarge) => "ExtraLarge",
        (AppTextSize.Standard, AppScale.Standard) => "Standard",
        _ => "Standard",
    };
#endif

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

public sealed record AvaloniaAppearanceState(
    string Theme,
    bool ApplyAccentColorToTheme,
    bool ApplyAccentColorToSystem,
    bool UseSystemAccent,
    string? AccentColorHex,
    string TemperatureUnit,
    string FontFamily,
    string UiScale)
{
    public static AvaloniaAppearanceState Default { get; } = new(
        "System", true, true, true, null, "Celsius", "Default", "Standard");
}

/// <summary>
/// Arranges its child in unscaled logical coordinates, then scales the visual.
/// This is Avalonia's layout equivalent of the WPF host's LayoutTransform path.
/// </summary>
internal sealed class AvaloniaAppScaleHost : Decorator
{
    private double _scale = 1d;
    private readonly ScaleTransform _transform = new();

    public double Scale
    {
        get => _scale;
        set
        {
            var normalized = value > 0 ? value : 1d;
            if (_scale.Equals(normalized))
                return;

            _scale = normalized;
            _transform.ScaleX = normalized;
            _transform.ScaleY = normalized;
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }
    }

    public AvaloniaAppScaleHost()
    {
        RenderTransform = _transform;
        RenderTransformOrigin = RelativePoint.TopLeft;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is null)
            return default;

        Child.Measure(Divide(availableSize));
        return Multiply(Child.DesiredSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(Divide(finalSize)));
        return finalSize;
    }

    private Size Divide(Size size) => new(size.Width / _scale, size.Height / _scale);

    private Size Multiply(Size size) => new(size.Width * _scale, size.Height * _scale);
}
