using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Windows;
using Wpf.Ui.Appearance;

namespace UniversalDeviceToolkit.WPF.Utils;

public class ThemeManager
{
    private static readonly RGBColor DefaultAccentColor = new(255, 33, 33);
    private static readonly string[] StylePresetBrushKeys =
    [
        "ApplicationBackgroundBrush",
        "ControlFillColorDefaultBrush",
        "ControlFillColorSecondaryBrush",
        "ControlFillColorTertiaryBrush",
        "ControlStrokeColorDefaultBrush",
        "ControlStrokeColorSecondaryBrush",
        "ControlElevationBorderBrush",
        "CardStrokeColorDefaultBrush",
        "TextFillColorSecondaryBrush",
        "AppSurfaceBackgroundBrush",
        "AppSurfaceCardBrush",
        "AppNavigationBackgroundBrush",
        "ChartSurfaceBrush",
        "ChartSurfaceBorderBrush",
        "ChartGridlineBrush",
        "ChartBaselineBrush",
        "NotificationGlassSurfaceBrush",
        "NotificationGlassBorderBrush"
    ];

    private readonly ApplicationSettings _settings;
    private readonly SystemThemeListener _listener;

    public event EventHandler? ThemeApplied;

    public ThemeManager(SystemThemeListener systemThemeListener, ApplicationSettings settings)
    {
        _listener = systemThemeListener;
        _settings = settings;

        _listener.Changed += (_, _) => Application.Current.Dispatcher.BeginInvoke(Apply);

        // IoC AutoActivate resolves this before any window is shown; apply saved theme immediately
        // so App.xaml's hard-coded Dark defaults do not flash on light/system mode.
        Apply();
    }

    public void Apply()
    {
        ClearStylePresetBrushes();
        SetTheme();
        SetColor();
        ApplyStylePreset();
        ApplySurfaceResources();
        ApplyStatusTextBrushes();

        ThemeApplied?.Invoke(this, EventArgs.Empty);
    }

    public RGBColor GetAccentColor()
    {
        switch (_settings.Store.AccentColorSource)
        {
            case AccentColorSource.Custom:
                return _settings.Store.AccentColor ?? DefaultAccentColor;
            case AccentColorSource.System:
                try
                {
                    return SystemTheme.GetAccentColor();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Couldn't check system accent color; using default.", ex);

                    return DefaultAccentColor;
                }
            default:
                return DefaultAccentColor;
        }
    }

    private bool IsDarkMode()
    {
        var theme = _settings.Store.Theme;

        switch (theme)
        {
            case Theme.Dark:
                return true;
            case Theme.Light:
                return false;
            case Theme.System:
                try
                {
                    return SystemTheme.IsDarkMode();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Couldn't check system theme; assuming Dark Mode.", ex);

                    return true;
                }
            default:
                return true;
        }
    }

    private void SetTheme()
    {
        var isDark = IsDarkMode();
        var theme = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        var backgroundType = RenderingCompatibilityHelper.GetPreferredBackgroundType(_settings);
        ApplicationThemeManager.Apply(theme, backgroundType, false);

        Application.Current.Resources["SnackbarShadowColor"] = isDark ? System.Windows.Media.Colors.Black : System.Windows.Media.Color.FromArgb(64, 0, 0, 0);
        
        // Update all BaseWindow instances
        UpdateWindowBackdrops();
    }

    private void UpdateWindowBackdrops()
    {
        var backgroundType = RenderingCompatibilityHelper.GetPreferredBackgroundType(_settings);
        
        foreach (Window window in Application.Current.Windows)
        {
            if (window is BaseWindow baseWindow)
            {
                baseWindow.WindowBackdropType = backgroundType;
                // Acrylic background type provides dynamic blur effect
                // that adapts to background content and color changes
            }
        }
    }

    private void SetColor()
    {
        var accentColor = GetAccentColor().ToColor();
        
        // Apply accent color with improved color contrast
        ApplicationAccentColorManager.Apply(systemAccent: accentColor,
            primaryAccent: accentColor,
            secondaryAccent: accentColor,
            tertiaryAccent: accentColor);
    }

    private void ClearStylePresetBrushes()
    {
        foreach (var key in StylePresetBrushKeys)
            Application.Current.Resources.Remove(key);
    }

    private void ApplyStylePreset()
    {
        var palette = GetPresetPalette(_settings.Store.ThemeStylePreset, IsDarkMode());
        if (palette is null)
            return;

        SetBrush("ApplicationBackgroundBrush", palette.ApplicationBackground);
        SetBrush("ControlFillColorDefaultBrush", palette.ControlFillDefault);
        SetBrush("ControlFillColorSecondaryBrush", palette.ControlFillSecondary);
        SetBrush("ControlFillColorTertiaryBrush", palette.ControlFillTertiary);
        SetBrush("ControlStrokeColorDefaultBrush", palette.ControlStrokeDefault);
        SetBrush("ControlStrokeColorSecondaryBrush", palette.ControlStrokeSecondary);
        SetBrush("ControlElevationBorderBrush", palette.ControlElevationBorder);
        SetBrush("CardStrokeColorDefaultBrush", palette.CardStroke);
        SetBrush("TextFillColorSecondaryBrush", palette.TextSecondary);
        Application.Current.Resources["SnackbarShadowColor"] = palette.SnackbarShadow;
    }

    private void ApplySurfaceResources()
    {
        var isDark = IsDarkMode();
        var defaultSurface = isDark ? Color.FromRgb(32, 32, 32) : Color.FromRgb(246, 246, 246);
        var palette = GetPresetPalette(_settings.Store.ThemeStylePreset, isDark);

        // Style presets (Official Cool / Midnight / Forest) must also retint cards, charts,
        // and notification glass — otherwise sensors + Hotkeys toast stay neutral grey on a
        // colored shell (user report: 官方炫酷模式适配).
        if (palette is not null)
        {
            ApplyPresetSurfaceResources(palette, isDark);
            return;
        }

        // Navigation and content surface must share one background or the shell seam shows in light mode
        // (WPF-UI ApplicationBackgroundBrush is pure white while the content surface uses #F6F6F6).
        var surfaceBackground = isDark
            ? (TryGetBrushColor("ApplicationBackgroundBrush") ?? defaultSurface)
            : defaultSurface;

        SetBrush("AppSurfaceBackgroundBrush", surfaceBackground);
        SetBrush("AppSurfaceCardBrush", isDark ? Color.FromRgb(48, 48, 48) : Color.FromRgb(255, 255, 255));
        SetBrush("AppNavigationBackgroundBrush", surfaceBackground);
        // Chart surface fill + soft guides. Baseline is a warm accent under the filled band
        // (reference area chart), not a neutral grey stroke.
        SetBrush("ChartSurfaceBrush", isDark ? Color.FromRgb(255, 255, 255) : Color.FromRgb(26, 26, 26), isDark ? 0.045 : 0.035);
        SetBrush("ChartSurfaceBorderBrush", isDark ? Color.FromRgb(255, 255, 255) : Color.FromRgb(0, 0, 0), isDark ? 0.10 : 0.08);
        SetBrush("ChartGridlineBrush", isDark ? Color.FromRgb(255, 255, 255) : Color.FromRgb(0, 0, 0), isDark ? 0.10 : 0.08);
        SetBrush("ChartBaselineBrush", Color.FromRgb(210, 160, 90), isDark ? 0.75 : 0.85);
        SetBrush("NotificationGlassSurfaceBrush", isDark ? Color.FromRgb(28, 28, 28) : Color.FromRgb(252, 252, 252), isDark ? 0.62 : 0.72);
        SetBrush("NotificationGlassBorderBrush", isDark ? Color.FromRgb(255, 255, 255) : Color.FromRgb(0, 0, 0), isDark ? 0.22 : 0.14);
    }

    /// <summary>
    /// Surface tokens for Official Cool / Midnight Neon / Forest Tech so dashboard sensors
    /// cards and in-app toasts match the tinted shell (not neutral Fluent grey).
    /// </summary>
    private static void ApplyPresetSurfaceResources(ThemeStylePalette palette, bool isDark)
    {
        SetBrush("AppSurfaceBackgroundBrush", palette.ApplicationBackground);
        // Card one step above page bg (ControlFillDefault) — matches sensors / list surfaces.
        SetBrush("AppSurfaceCardBrush", palette.ControlFillDefault);
        SetBrush("AppNavigationBackgroundBrush", palette.ApplicationBackground);

        // Soft chart wells: light wash over the tinted card, not pure white-on-grey.
        var chartWash = isDark ? Color.FromRgb(255, 255, 255) : BlendToward(palette.ApplicationBackground, Colors.Black, 0.55);
        SetBrush("ChartSurfaceBrush", chartWash, isDark ? 0.06 : 0.045);
        SetBrush("ChartSurfaceBorderBrush", palette.ControlStrokeDefault, isDark ? 0.35 : 0.28);
        SetBrush("ChartGridlineBrush", palette.ControlStrokeSecondary, isDark ? 0.28 : 0.22);
        // Baseline keeps a warm copper note so series remain readable on blue/purple/green shells.
        SetBrush("ChartBaselineBrush", Color.FromRgb(210, 160, 90), isDark ? 0.72 : 0.82);

        // Toast / status banner glass: elevated fill from the same family + stroke tint.
        SetBrush("NotificationGlassSurfaceBrush", palette.ControlFillSecondary, isDark ? 0.82 : 0.88);
        SetBrush("NotificationGlassBorderBrush", palette.ControlStrokeDefault, isDark ? 0.55 : 0.42);
    }

    private static Color BlendToward(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)Math.Clamp((int)Math.Round(from.R + (to.R - from.R) * amount), 0, 255),
            (byte)Math.Clamp((int)Math.Round(from.G + (to.G - from.G) * amount), 0, 255),
            (byte)Math.Clamp((int)Math.Round(from.B + (to.B - from.B) * amount), 0, 255));
    }

    private static void SetBrush(string key, Color color, double opacity = 1.0)
    {
        Application.Current.Resources[key] = CreateBrush(color, opacity);
    }

    private static Color? TryGetBrushColor(string key)
    {
        return Application.Current.Resources[key] is SolidColorBrush brush ? brush.Color : null;
    }

    private static SolidColorBrush CreateBrush(Color color, double opacity = 1.0)
    {
        var brush = new SolidColorBrush(color) { Opacity = opacity };
        if (brush.CanFreeze)
            brush.Freeze();

        return brush;
    }

    private static ThemeStylePalette? GetPresetPalette(ThemeStylePreset preset, bool isDark)
    {
        return preset switch
        {
            ThemeStylePreset.Default => null,
            ThemeStylePreset.Official => isDark
                ? new ThemeStylePalette(
                    Color.FromRgb(12, 18, 30),
                    Color.FromRgb(20, 30, 48),
                    Color.FromRgb(26, 40, 62),
                    Color.FromRgb(34, 52, 78),
                    Color.FromRgb(54, 92, 150),
                    Color.FromRgb(78, 128, 196),
                    Color.FromRgb(62, 104, 168),
                    Color.FromRgb(70, 118, 188),
                    Color.FromRgb(170, 196, 232),
                    Color.FromArgb(160, 7, 16, 30))
                : new ThemeStylePalette(
                    Color.FromRgb(243, 248, 255),
                    Color.FromRgb(232, 240, 252),
                    Color.FromRgb(221, 232, 248),
                    Color.FromRgb(212, 225, 244),
                    Color.FromRgb(127, 165, 219),
                    Color.FromRgb(102, 146, 212),
                    Color.FromRgb(117, 156, 220),
                    Color.FromRgb(88, 132, 201),
                    Color.FromRgb(70, 91, 124),
                    Color.FromArgb(72, 25, 52, 94)),
            ThemeStylePreset.Midnight => isDark
                ? new ThemeStylePalette(
                    Color.FromRgb(9, 10, 20),
                    Color.FromRgb(18, 20, 37),
                    Color.FromRgb(25, 28, 49),
                    Color.FromRgb(33, 36, 61),
                    Color.FromRgb(104, 77, 196),
                    Color.FromRgb(139, 92, 246),
                    Color.FromRgb(110, 86, 210),
                    Color.FromRgb(147, 112, 255),
                    Color.FromRgb(196, 181, 253),
                    Color.FromArgb(176, 6, 6, 18))
                : new ThemeStylePalette(
                    Color.FromRgb(244, 241, 255),
                    Color.FromRgb(235, 230, 255),
                    Color.FromRgb(228, 221, 252),
                    Color.FromRgb(220, 213, 248),
                    Color.FromRgb(154, 132, 232),
                    Color.FromRgb(139, 92, 246),
                    Color.FromRgb(156, 137, 233),
                    Color.FromRgb(132, 104, 223),
                    Color.FromRgb(93, 80, 143),
                    Color.FromArgb(88, 31, 19, 61)),
            ThemeStylePreset.Forest => isDark
                ? new ThemeStylePalette(
                    Color.FromRgb(12, 23, 18),
                    Color.FromRgb(19, 33, 26),
                    Color.FromRgb(26, 44, 34),
                    Color.FromRgb(33, 55, 43),
                    Color.FromRgb(69, 128, 95),
                    Color.FromRgb(78, 154, 109),
                    Color.FromRgb(73, 138, 101),
                    Color.FromRgb(89, 166, 118),
                    Color.FromRgb(177, 220, 191),
                    Color.FromArgb(168, 6, 17, 10))
                : new ThemeStylePalette(
                    Color.FromRgb(242, 249, 244),
                    Color.FromRgb(232, 243, 236),
                    Color.FromRgb(222, 236, 228),
                    Color.FromRgb(214, 230, 220),
                    Color.FromRgb(119, 170, 134),
                    Color.FromRgb(101, 155, 116),
                    Color.FromRgb(112, 163, 126),
                    Color.FromRgb(89, 145, 105),
                    Color.FromRgb(73, 104, 82),
                    Color.FromArgb(72, 20, 50, 31)),
            _ => null
        };
    }

    private void ApplyStatusTextBrushes()
    {
        // StatusCriticalTextBrush is keyed as a static resource with the dark-mode color
        // (#FFEB6B6B). In light mode that washed-out pink fails contrast against a near-
        // white surface, so swap it for a darker critical tone whenever the app is in
        // light theme. Keep the static token intact so XAML references continue to work.
        var criticalColor = IsDarkMode()
            ? Color.FromRgb(0xEB, 0x6B, 0x6B)
            : Color.FromRgb(0xC6, 0x28, 0x28);
        SetBrush("StatusCriticalTextBrush", criticalColor);
    }

    private sealed record ThemeStylePalette(
        Color ApplicationBackground,
        Color ControlFillDefault,
        Color ControlFillSecondary,
        Color ControlFillTertiary,
        Color ControlStrokeDefault,
        Color ControlStrokeSecondary,
        Color ControlElevationBorder,
        Color CardStroke,
        Color TextSecondary,
        Color SnackbarShadow);
}
