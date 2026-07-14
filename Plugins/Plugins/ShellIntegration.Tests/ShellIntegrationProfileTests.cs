using System;
using UniversalDeviceToolkit.Plugins.ShellIntegration;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ShellIntegration.Tests;

public class ShellIntegrationProfileTests
{
    // ── NormalizeHexColor ────────────────────────────────────────────

    [Theory]
    [InlineData("#1A2B3C", "#1A2B3C")]
    [InlineData("1A2B3C", "#1A2B3C")]
    [InlineData("FF0000", "#FF0000")]
    // 3-char hex not supported by NormalizeHexColor - falls through to default
    [InlineData("#AABBCCDD", "#AABBCCDD")]
    public void NormalizeHexColor_ValidHex_ReturnsNormalized(string input, string expected)
    {
        Assert.Equal(expected, ShellIntegrationProfile.NormalizeHexColor(input, "#000000"));
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ZZZZZZ")]
    public void NormalizeHexColor_InvalidHex_ReturnsFallback(string? input)
    {
        Assert.Equal("#FALLBACK", ShellIntegrationProfile.NormalizeHexColor(input, "#FALLBACK"));
    }

    [Theory]
    [InlineData("#GGG")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    public void NormalizeHexColor_MalformedHex_ReturnsFallback(string input)
    {
        Assert.Equal("#DEFAULT", ShellIntegrationProfile.NormalizeHexColor(input, "#DEFAULT"));
    }

    // ── Normalize ────────────────────────────────────────────────────

    [Fact]
    public void Normalize_ClampsBackgroundOpacity()
    {
        var p = new ShellIntegrationProfile { BackgroundOpacity = 200 };
        Assert.Equal(100, p.Normalize().BackgroundOpacity);
    }

    [Fact]
    public void Normalize_ClampsShowDelay()
    {
        var p = new ShellIntegrationProfile { ShowDelay = 5000 };
        Assert.Equal(4000, p.Normalize().ShowDelay);
    }

    [Fact]
    public void Normalize_ClampsShadowValues()
    {
        var p = new ShellIntegrationProfile { ShadowSize = 50, ShadowOpacity = 150, ShadowOffset = 40 };
        var n = p.Normalize();
        Assert.Equal(30, n.ShadowSize);
        Assert.Equal(100, n.ShadowOpacity);
        Assert.Equal(30, n.ShadowOffset);
    }

    [Fact]
    public void Normalize_ClampsRadii()
    {
        var p = new ShellIntegrationProfile { ItemRadius = 10, BorderRadius = 10 };
        var n = p.Normalize();
        Assert.Equal(3, n.ItemRadius);
        Assert.Equal(3, n.BorderRadius);
    }

    [Fact]
    public void Normalize_ClampsTipTime()
    {
        var p = new ShellIntegrationProfile { TipTimeSeconds = 10.0 };
        Assert.Equal(4.0, p.Normalize().TipTimeSeconds);

        var p2 = new ShellIntegrationProfile { TipTimeSeconds = 0.05 };
        Assert.Equal(0.2, p2.Normalize().TipTimeSeconds);
    }

    [Fact]
    public void Normalize_NullThemeName_DefaultsToModern()
    {
        var p = new ShellIntegrationProfile { ThemeName = null! };
        Assert.Equal("modern", p.Normalize().ThemeName);
    }

    [Fact]
    public void Normalize_EmptyThemeName_DefaultsToModern()
    {
        var p = new ShellIntegrationProfile { ThemeName = "   " };
        Assert.Equal("modern", p.Normalize().ThemeName);
    }

    [Fact]
    public void Normalize_NormalizesHexColors()
    {
        var p = new ShellIntegrationProfile
        {
            AccentColor = "ff0000",
            BackgroundColor = "invalid",
            HoverColor = "#aabbcc",
            TextColor = null!,
            MutedTextColor = "",
            SelectedTextColor = "#11223344",
            TintColor = null!,
        };
        var n = p.Normalize();
        Assert.Equal("#FF0000", n.AccentColor);
        Assert.Equal("#F7F8FC", n.BackgroundColor); // fallback
        Assert.Equal("#AABBCC", n.HoverColor);
        Assert.Equal("#111827", n.TextColor); // fallback
        Assert.Equal("#667085", n.MutedTextColor); // fallback
        Assert.Equal("#11223344", n.SelectedTextColor);
        Assert.Equal("#DCE6FF", n.TintColor); // fallback
    }

    // ── GetColorSchemeExpression ─────────────────────────────────────

    [Theory]
    [InlineData(ShellColorScheme.Light, "false")]
    [InlineData(ShellColorScheme.Dark, "true")]
    public void GetColorSchemeExpression_ReturnsCorrect(ShellColorScheme scheme, string expected)
    {
        Assert.Equal(expected, new ShellIntegrationProfile { ColorScheme = scheme }.GetColorSchemeExpression());
    }

    // ── GetViewExpression ────────────────────────────────────────────

    [Fact]
    public void GetViewExpression_Compact_ReturnsCompact()
    {
        Assert.Equal("view.compact", new ShellIntegrationProfile { UseCompactView = true }.GetViewExpression());
    }

    [Fact]
    public void GetViewExpression_Medium_ReturnsMedium()
    {
        Assert.Equal("view.medium", new ShellIntegrationProfile { UseCompactView = false }.GetViewExpression());
    }

    // ── GetEffectExpression ──────────────────────────────────────────

    [Fact]
    public void GetEffectExpression_MotionDisabled_ReturnsZero()
    {
        Assert.Equal("0", new ShellIntegrationProfile { EnableMotionEffects = false }.GetEffectExpression());
    }

    [Theory]
    [InlineData(ShellVisualEffect.Transparent, "1")]
    [InlineData(ShellVisualEffect.Blur, "2")]
    public void GetEffectExpression_NonAcrylic_ReturnsCorrect(ShellVisualEffect effect, string expected)
    {
        Assert.Equal(expected, new ShellIntegrationProfile
        {
            EnableMotionEffects = true, BackgroundEffect = effect
        }.GetEffectExpression());
    }

    [Fact]
    public void GetEffectExpression_Acrylic_ReturnsBracketExpression()
    {
        var result = new ShellIntegrationProfile
        {
            EnableMotionEffects = true,
            BackgroundEffect = ShellVisualEffect.Acrylic,
            TintColor = "#1D4ED8",
            BackgroundOpacity = 84,
        }.GetEffectExpression();
        Assert.StartsWith("[3,", result);
        Assert.Contains("1D4ED8", result);
        Assert.Contains("84", result);
    }

    [Fact]
    public void GetEffectExpression_NoneEffect_ReturnsZero()
    {
        var result = new ShellIntegrationProfile
        {
            EnableMotionEffects = true,
            BackgroundEffect = ShellVisualEffect.None,
        }.GetEffectExpression();
        Assert.Equal("0", result);
    }

    // ── CreateDefault ────────────────────────────────────────────────

    [Fact]
    public void CreateDefault_HasValidDefaults()
    {
        var d = ShellIntegrationProfile.CreateDefault();
        Assert.True(d.EnableShellIntegration);
        Assert.Equal(ShellVisualEffect.Acrylic, d.BackgroundEffect);
        Assert.Equal(ShellColorScheme.Auto, d.ColorScheme);
        Assert.Equal(92, d.BackgroundOpacity);
        Assert.Equal("modern", d.ThemeName);
    }

    // ── CreatePreset ─────────────────────────────────────────────

    [Theory]
    [InlineData(ShellIntegrationPreset.Default)]
    
    
    public void CreatePreset_ReturnsNonNullProfile(ShellIntegrationPreset preset)
    {
        var p = ShellIntegrationProfile.CreatePreset(preset);
        Assert.NotNull(p);
        Assert.True(p.EnableShellIntegration);
        Assert.False(string.IsNullOrEmpty(p.ThemeName));
    }

    // ── SanitizeBackgroundEffect ─────────────────────────────────

    [Theory]
    [InlineData(999)]
    [InlineData(-1)]
    public void SanitizeBackgroundEffect_OutOfRangeValue_ReturnsAcrylic(int raw)
    {
        var result = ShellIntegrationProfile.SanitizeBackgroundEffect((ShellVisualEffect)raw);
        Assert.Equal(ShellVisualEffect.Acrylic, result);
    }

    [Theory]
    [InlineData(ShellVisualEffect.None)]
    [InlineData(ShellVisualEffect.Transparent)]
    [InlineData(ShellVisualEffect.Blur)]
    [InlineData(ShellVisualEffect.Acrylic)]
    public void SanitizeBackgroundEffect_ValidValue_PreservesValue(ShellVisualEffect raw)
    {
        var result = ShellIntegrationProfile.SanitizeBackgroundEffect(raw);
        Assert.Equal(raw, result);
    }

    // ── SanitizeColorScheme ──────────────────────────────────────

    [Theory]
    [InlineData(999)]
    [InlineData(-1)]
    public void SanitizeColorScheme_OutOfRangeValue_ReturnsAuto(int raw)
    {
        var result = ShellIntegrationProfile.SanitizeColorScheme((ShellColorScheme)raw);
        Assert.Equal(ShellColorScheme.Auto, result);
    }

    [Theory]
    [InlineData(ShellColorScheme.Auto)]
    [InlineData(ShellColorScheme.Light)]
    [InlineData(ShellColorScheme.Dark)]
    public void SanitizeColorScheme_ValidValue_PreservesValue(ShellColorScheme raw)
    {
        var result = ShellIntegrationProfile.SanitizeColorScheme(raw);
        Assert.Equal(raw, result);
    }
}