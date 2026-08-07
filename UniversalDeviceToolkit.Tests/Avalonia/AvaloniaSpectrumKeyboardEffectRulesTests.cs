using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaSpectrumKeyboardEffectRulesTests
{
    [Theory]
    [InlineData("Static", false, true)]
    [InlineData("Breath", true, true)]
    [InlineData("Smooth", true, false)]
    [InlineData("WaveRTL", true, false)]
    [InlineData("WaveLTR", true, false)]
    public void RgbCapabilities_ShouldMatchWpfEditor(string effectType, bool supportsSpeed, bool supportsZones)
    {
        RgbKeyboardEffectRules.SupportsSpeed(effectType).Should().Be(supportsSpeed);
        RgbKeyboardEffectRules.SupportsZones(effectType).Should().Be(supportsZones);
    }

    [Theory]
    [InlineData("ColorWave", true)]
    [InlineData("RainbowWave", true)]
    [InlineData("Always", false)]
    public void DirectionSupport_ShouldMatchWpfEffectEditor(string effectType, bool expected)
    {
        SpectrumKeyboardEffectRules.SupportsDirection(effectType).Should().Be(expected);
    }

    [Theory]
    [InlineData("RainbowScrew", true)]
    [InlineData("ColorWave", false)]
    public void ClockwiseDirectionSupport_ShouldMatchWpfEffectEditor(string effectType, bool expected)
    {
        SpectrumKeyboardEffectRules.SupportsClockwiseDirection(effectType).Should().Be(expected);
    }

    [Theory]
    [InlineData("Always", false)]
    [InlineData("ColorChange", true)]
    [InlineData("RainbowWave", true)]
    [InlineData("AudioBounce", false)]
    public void SpeedSupport_ShouldMatchWpfEffectEditor(string effectType, bool expected)
    {
        SpectrumKeyboardEffectRules.SupportsSpeed(effectType).Should().Be(expected);
    }

    [Theory]
    [InlineData("Always", true)]
    [InlineData("Type", true)]
    [InlineData("RainbowScrew", false)]
    [InlineData("AudioRipple", false)]
    public void ColorSupport_ShouldMatchWpfEffectEditor(string effectType, bool expected)
    {
        SpectrumKeyboardEffectRules.SupportsColors(effectType).Should().Be(expected);
    }

    [Theory]
    [InlineData("Always", true)]
    [InlineData("ColorChange", false)]
    [InlineData("RainbowWave", false)]
    [InlineData("Type", false)]
    public void SingleColorSupport_ShouldMatchWpfEffectEditor(string effectType, bool expected)
    {
        SpectrumKeyboardEffectRules.UsesSingleColor(effectType).Should().Be(expected);
    }

    [Fact]
    public void AllLightsEffects_ShouldDiscardPerKeySelectionAndColors()
    {
        SpectrumKeyboardEffectRules.HidesKeySelection("AudioBounce").Should().BeTrue();
        SpectrumKeyboardEffectRules.NormalizeKeys("AudioBounce", [1, 2], [10, 20]).Should().BeEmpty();
        SpectrumKeyboardEffectRules.NormalizeColors(
            "AudioBounce",
            [new KeyboardColorState(1, 2, 3)]).Should().BeEmpty();
    }

    [Fact]
    public void WholeKeyboardEffects_ShouldUseEveryDetectedKey()
    {
        SpectrumKeyboardEffectRules.HidesKeySelection("Ripple").Should().BeTrue();
        SpectrumKeyboardEffectRules.NormalizeKeys("Ripple", [1], [20, 10, 20])
            .Should().Equal(10, 20);
    }

    [Fact]
    public void PerKeyEffects_ShouldRetainOnlyUniqueOrderedSelectedKeys()
    {
        SpectrumKeyboardEffectRules.HidesKeySelection("Always").Should().BeFalse();
        SpectrumKeyboardEffectRules.NormalizeKeys("Always", [9, 2, 9], [1, 2, 9])
            .Should().Equal(2, 9);
    }
}
