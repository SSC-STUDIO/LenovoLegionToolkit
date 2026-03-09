using System;
using System.Globalization;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

public enum ShellVisualEffect
{
    None,
    Transparent,
    Blur,
    Acrylic
}

public enum ShellColorScheme
{
    Auto,
    Light,
    Dark
}

public sealed class ShellIntegrationProfile
{
    public bool EnableShellIntegration { get; set; } = true;
    public bool EnableMotionEffects { get; set; } = true;
    public bool EnableShadow { get; set; } = true;
    public bool UseCompactView { get; set; }
    public ShellVisualEffect BackgroundEffect { get; set; } = ShellVisualEffect.Acrylic;
    public ShellColorScheme ColorScheme { get; set; } = ShellColorScheme.Auto;
    public int BackgroundOpacity { get; set; } = 92;
    public int ShowDelay { get; set; } = 120;
    public int ShadowSize { get; set; } = 10;
    public int ShadowOpacity { get; set; } = 18;
    public int ShadowOffset { get; set; } = 4;
    public int ItemRadius { get; set; } = 2;
    public int BorderRadius { get; set; } = 2;
    public double TipTimeSeconds { get; set; } = 1.2;
    public string ThemeName { get; set; } = "modern";
    public string AccentColor { get; set; } = "#4F7CFF";
    public string BackgroundColor { get; set; } = "#F7F8FC";
    public string HoverColor { get; set; } = "#E8EEFF";
    public string TextColor { get; set; } = "#111827";
    public string MutedTextColor { get; set; } = "#667085";
    public string SelectedTextColor { get; set; } = "#FFFFFF";
    public string TintColor { get; set; } = "#DCE6FF";

    public static ShellIntegrationProfile CreateDefault() => new();

    public ShellIntegrationProfile Normalize()
    {
        return new ShellIntegrationProfile
        {
            EnableShellIntegration = EnableShellIntegration,
            EnableMotionEffects = EnableMotionEffects,
            EnableShadow = EnableShadow,
            UseCompactView = UseCompactView,
            BackgroundEffect = BackgroundEffect,
            ColorScheme = ColorScheme,
            BackgroundOpacity = Clamp(BackgroundOpacity, 0, 100),
            ShowDelay = Clamp(ShowDelay, 0, 4000),
            ShadowSize = Clamp(ShadowSize, 0, 30),
            ShadowOpacity = Clamp(ShadowOpacity, 0, 100),
            ShadowOffset = Clamp(ShadowOffset, 0, 30),
            ItemRadius = Clamp(ItemRadius, 0, 3),
            BorderRadius = Clamp(BorderRadius, 0, 3),
            TipTimeSeconds = Math.Clamp(TipTimeSeconds, 0.2, 4.0),
            ThemeName = string.IsNullOrWhiteSpace(ThemeName) ? "modern" : ThemeName.Trim(),
            AccentColor = NormalizeHexColor(AccentColor, "#4F7CFF"),
            BackgroundColor = NormalizeHexColor(BackgroundColor, "#F7F8FC"),
            HoverColor = NormalizeHexColor(HoverColor, "#E8EEFF"),
            TextColor = NormalizeHexColor(TextColor, "#111827"),
            MutedTextColor = NormalizeHexColor(MutedTextColor, "#667085"),
            SelectedTextColor = NormalizeHexColor(SelectedTextColor, "#FFFFFF"),
            TintColor = NormalizeHexColor(TintColor, "#DCE6FF")
        };
    }

    public static string NormalizeHexColor(string? raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        var value = raw.Trim();
        if (!value.StartsWith("#", StringComparison.Ordinal))
            value = $"#{value}";

        if (value.Length == 7 || value.Length == 9)
        {
            var valid = true;
            foreach (var ch in value.AsSpan(1))
            {
                if (!Uri.IsHexDigit(ch))
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
                return value.ToUpperInvariant();
        }

        return fallback;
    }

    public string GetColorSchemeExpression() => ColorScheme switch
    {
        ShellColorScheme.Light => "false",
        ShellColorScheme.Dark => "true",
        _ => "default"
    };

    public string GetViewExpression() => UseCompactView ? "view.compact" : "view.medium";

    public string GetEffectExpression()
    {
        if (!EnableMotionEffects)
            return "0";

        return BackgroundEffect switch
        {
            ShellVisualEffect.Transparent => "1",
            ShellVisualEffect.Blur => "2",
            ShellVisualEffect.Acrylic => $"[3, {TintColor}, {BackgroundOpacity.ToString(CultureInfo.InvariantCulture)}]",
            _ => "0"
        };
    }

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
}
