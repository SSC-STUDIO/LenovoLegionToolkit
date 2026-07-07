using System;
using System.Linq;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class BootStructsAndSpectrumEffectTests
{
    #region BootLogoFormat Enum Tests

    [Theory]
    [InlineData(BootLogoFormat.Jpeg)]
    [InlineData(BootLogoFormat.Bmp)]
    [InlineData(BootLogoFormat.Png)]
    public void BootLogoFormat_SingleValues_ShouldBeDefined(BootLogoFormat format)
    {
        Enum.IsDefined(format).Should().BeTrue();
    }

    [Fact]
    public void BootLogoFormat_CombinedFlags_ShouldHaveExpectedByteValues()
    {
        ((byte)BootLogoFormat.Jpeg).Should().Be(0x01);
        ((byte)BootLogoFormat.Bmp).Should().Be(0x10);
        ((byte)BootLogoFormat.Png).Should().Be(0x20);
    }

    [Fact]
    public void BootLogoFormat_AllCombined_ShouldSetAllBits()
    {
        var combined = BootLogoFormat.Jpeg | BootLogoFormat.Bmp | BootLogoFormat.Png;
        combined.HasFlag(BootLogoFormat.Jpeg).Should().BeTrue();
        combined.HasFlag(BootLogoFormat.Bmp).Should().BeTrue();
        combined.HasFlag(BootLogoFormat.Png).Should().BeTrue();
    }

    [Fact]
    public void BootLogoFormat_Zero_ShouldNotHaveAnyFlags()
    {
        BootLogoFormat format = 0;
        format.HasFlag(BootLogoFormat.Jpeg).Should().BeFalse();
        format.HasFlag(BootLogoFormat.Bmp).Should().BeFalse();
        format.HasFlag(BootLogoFormat.Png).Should().BeFalse();
    }

    #endregion

    #region BootLogoInfo Tests

    [Fact]
    public void BootLogoInfo_Default_ShouldHaveZeroValues()
    {
        var info = default(BootLogoInfo);
        info.Enabled.Should().Be(0);
        info.SupportedWidth.Should().Be(0);
        info.SupportedHeight.Should().Be(0);
        info.SupportedFormat.Should().Be(default(BootLogoFormat));
    }

    [Fact]
    public void BootLogoInfo_Enabled_IsMutable()
    {
        var info = default(BootLogoInfo);
        info.Enabled = 1;
        info.Enabled.Should().Be(1);
    }

    [Fact]
    public void BootLogoInfo_Enabled_CanBeSetToMaxByte()
    {
        var info = default(BootLogoInfo);
        info.Enabled = byte.MaxValue;
        info.Enabled.Should().Be(byte.MaxValue);
    }

    #endregion

    #region BootLogoChecksum Tests

    [Fact]
    public void BootLogoChecksum_Default_Crc_ShouldBeZero()
    {
        var checksum = default(BootLogoChecksum);
        checksum.Crc.Should().Be(0u);
    }

    [Fact]
    public void BootLogoChecksum_Crc_IsMutable()
    {
        var checksum = default(BootLogoChecksum);
        checksum.Crc = 0xDEADBEEF;
        checksum.Crc.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void BootLogoChecksum_Crc_MaxUInt32()
    {
        var checksum = default(BootLogoChecksum);
        checksum.Crc = uint.MaxValue;
        checksum.Crc.Should().Be(uint.MaxValue);
    }

    #endregion

    #region SpectrumKeyboardBacklightEffect Tests

    [Fact]
    public void SpectrumEffect_AudioBounce_ShouldClearKeysArray()
    {
        ushort[] keys = [1, 2, 3, 4, 5];
        var effect = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.AudioBounce,
            SpectrumKeyboardBacklightSpeed.Speed2,
            SpectrumKeyboardBacklightDirection.None,
            SpectrumKeyboardBacklightClockwiseDirection.None,
            [],
            keys);

        effect.Keys.Should().BeEmpty();
        effect.Type.Should().Be(SpectrumKeyboardBacklightEffectType.AudioBounce);
    }

    [Fact]
    public void SpectrumEffect_AudioRipple_ShouldClearKeysArray()
    {
        ushort[] keys = [10, 20, 30];
        var effect = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.AudioRipple,
            SpectrumKeyboardBacklightSpeed.Speed1,
            SpectrumKeyboardBacklightDirection.None,
            SpectrumKeyboardBacklightClockwiseDirection.None,
            [],
            keys);

        effect.Keys.Should().BeEmpty();
    }

    [Fact]
    public void SpectrumEffect_AuroraSync_ShouldClearKeysArray()
    {
        ushort[] keys = [100, 200];
        var effect = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.AuroraSync,
            SpectrumKeyboardBacklightSpeed.Speed3,
            SpectrumKeyboardBacklightDirection.None,
            SpectrumKeyboardBacklightClockwiseDirection.None,
            [],
            keys);

        effect.Keys.Should().BeEmpty();
    }

    [Fact]
    public void SpectrumEffect_NonAllLights_ShouldRetainKeys()
    {
        ushort[] keys = [1, 2, 3];
        var effect = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.Always,
            SpectrumKeyboardBacklightSpeed.None,
            SpectrumKeyboardBacklightDirection.BottomToTop,
            SpectrumKeyboardBacklightClockwiseDirection.None,
            [],
            keys);

        effect.Keys.Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void SpectrumEffect_ColorWave_ShouldRetainKeys()
    {
        ushort[] keys = [42, 84];
        var effect = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.ColorWave,
            SpectrumKeyboardBacklightSpeed.Speed1,
            SpectrumKeyboardBacklightDirection.LeftToRight,
            SpectrumKeyboardBacklightClockwiseDirection.Clockwise,
            [],
            keys);

        effect.Keys.Should().ContainInOrder(42, 84);
    }

    [Fact]
    public void SpectrumEffect_Properties_ShouldReflectConstructor()
    {
        var effect = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.RainbowScrew,
            SpectrumKeyboardBacklightSpeed.Speed3,
            SpectrumKeyboardBacklightDirection.TopToBottom,
            SpectrumKeyboardBacklightClockwiseDirection.CounterClockwise,
            [],
            [1]);

        effect.Type.Should().Be(SpectrumKeyboardBacklightEffectType.RainbowScrew);
        effect.Speed.Should().Be(SpectrumKeyboardBacklightSpeed.Speed3);
        effect.Direction.Should().Be(SpectrumKeyboardBacklightDirection.TopToBottom);
        effect.ClockwiseDirection.Should().Be(SpectrumKeyboardBacklightClockwiseDirection.CounterClockwise);
    }

    [Fact]
    public void SpectrumEffect_EmptyKeys_NonAllLights_ShouldBeEmpty()
    {
        var effect = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.Smooth,
            SpectrumKeyboardBacklightSpeed.Speed3,
            SpectrumKeyboardBacklightDirection.RightToLeft,
            SpectrumKeyboardBacklightClockwiseDirection.None,
            [],
            []);

        effect.Keys.Should().BeEmpty();
        effect.Type.Should().Be(SpectrumKeyboardBacklightEffectType.Smooth);
    }

    #endregion

    #region SpectrumKeyboardBacklightDirection Enum Tests

    [Fact]
    public void SpectrumKeyboardBacklightDirection_AllValues_ShouldBeDefined()
    {
        foreach (var value in Enum.GetValues<SpectrumKeyboardBacklightDirection>())
            Enum.IsDefined(value).Should().BeTrue();
    }

    [Theory]
    [InlineData(SpectrumKeyboardBacklightDirection.None)]
    [InlineData(SpectrumKeyboardBacklightDirection.BottomToTop)]
    [InlineData(SpectrumKeyboardBacklightDirection.TopToBottom)]
    [InlineData(SpectrumKeyboardBacklightDirection.LeftToRight)]
    [InlineData(SpectrumKeyboardBacklightDirection.RightToLeft)]
    public void SpectrumKeyboardBacklightDirection_ShouldHaveExpectedMinimumCount(SpectrumKeyboardBacklightDirection value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region SpectrumKeyboardBacklightClockwiseDirection Enum Tests

    [Fact]
    public void SpectrumKeyboardBacklightClockwiseDirection_AllValues_ShouldBeDefined()
    {
        foreach (var value in Enum.GetValues<SpectrumKeyboardBacklightClockwiseDirection>())
            Enum.IsDefined(value).Should().BeTrue();
    }

    [Theory]
    [InlineData(SpectrumKeyboardBacklightClockwiseDirection.None)]
    [InlineData(SpectrumKeyboardBacklightClockwiseDirection.Clockwise)]
    [InlineData(SpectrumKeyboardBacklightClockwiseDirection.CounterClockwise)]
    public void SpectrumKeyboardBacklightClockwiseDirection_ShouldHaveExpectedCount(SpectrumKeyboardBacklightClockwiseDirection value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region SpectrumKeyboardBacklightBrightness Enum Tests

    [Fact]
    public void SpectrumKeyboardBacklightBrightness_AllValues_ShouldBeDefined()
    {
        foreach (var value in Enum.GetValues<SpectrumKeyboardBacklightBrightness>())
            Enum.IsDefined(value).Should().BeTrue();
    }

    [Theory]
    [InlineData(SpectrumKeyboardBacklightBrightness.Off)]
    [InlineData(SpectrumKeyboardBacklightBrightness.Low)]
    [InlineData(SpectrumKeyboardBacklightBrightness.Medium)]
    [InlineData(SpectrumKeyboardBacklightBrightness.High)]
    public void SpectrumKeyboardBacklightBrightness_ShouldHaveExpectedValues(SpectrumKeyboardBacklightBrightness value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region SpectrumKeyboardBacklightSpeed Enum Tests

    [Fact]
    public void SpectrumKeyboardBacklightSpeed_AllValues_ShouldBeDefined()
    {
        foreach (var value in Enum.GetValues<SpectrumKeyboardBacklightSpeed>())
            Enum.IsDefined(value).Should().BeTrue();
    }

    [Theory]
    [InlineData(SpectrumKeyboardBacklightSpeed.None)]
    [InlineData(SpectrumKeyboardBacklightSpeed.Speed1)]
    [InlineData(SpectrumKeyboardBacklightSpeed.Speed2)]
    [InlineData(SpectrumKeyboardBacklightSpeed.Speed3)]
    public void SpectrumKeyboardBacklightSpeed_ShouldHaveExpectedValues(SpectrumKeyboardBacklightSpeed value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region MachineInformation.PropertyData GodModeV3/V4 Tests

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(true, false, false, false, true)]
    [InlineData(false, true, false, false, true)]
    [InlineData(false, false, true, false, true)]
    [InlineData(false, false, false, true, true)]
    [InlineData(true, true, true, true, true)]
    public void PropertyData_SupportsGodMode_WithV3AndV4_ShouldBeCorrect(bool v1, bool v2, bool v3, bool v4, bool expected)
    {
        var pd = new MachineInformation.PropertyData
        {
            SupportsGodModeV1 = v1,
            SupportsGodModeV2 = v2,
            SupportsGodModeV3 = v3,
            SupportsGodModeV4 = v4
        };
        pd.SupportsGodMode.Should().Be(expected);
    }

    [Fact]
    public void PropertyData_SupportsExtremeMode_ShouldDefaultToFalse()
    {
        var pd = new MachineInformation.PropertyData();
        pd.SupportsExtremeMode.Should().BeFalse();
    }

    [Fact]
    public void PropertyData_SupportsITSMode_ShouldDefaultToFalse()
    {
        var pd = new MachineInformation.PropertyData();
        pd.SupportsITSMode.Should().BeFalse();
    }

    [Fact]
    public void PropertyData_HasGodModeToOtherModeSwitchingBug_ShouldDefaultToFalse()
    {
        var pd = new MachineInformation.PropertyData();
        pd.HasGodModeToOtherModeSwitchingBug.Should().BeFalse();
    }

    [Fact]
    public void PropertyData_SupportsBootLogoChange_ShouldMatchSupportBootLogoChange()
    {
        var pd = new MachineInformation.PropertyData { SupportBootLogoChange = true };
        pd.SupportsBootLogoChange.Should().BeTrue();
    }

    [Fact]
    public void PropertyData_IsExcludedFromLenovoLighting_ShouldDefaultToFalse()
    {
        var pd = new MachineInformation.PropertyData();
        pd.IsExcludedFromLenovoLighting.Should().BeFalse();
    }

    [Fact]
    public void PropertyData_IsExcludedFromPanelLogoLenovoLighting_ShouldDefaultToFalse()
    {
        var pd = new MachineInformation.PropertyData();
        pd.IsExcludedFromPanelLogoLenovoLighting.Should().BeFalse();
    }

    [Fact]
    public void PropertyData_HasAlternativeFullSpectrumLayout_ShouldDefaultToFalse()
    {
        var pd = new MachineInformation.PropertyData();
        pd.HasAlternativeFullSpectrumLayout.Should().BeFalse();
    }

    #endregion

    #region CapabilityID Additional Values

    [Fact]
    public void CapabilityID_SupportedPowerModes_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.SupportedPowerModes).Should().Be(0x00070000);
    }

    [Fact]
    public void CapabilityID_LegionZoneSupportVersion_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.LegionZoneSupportVersion).Should().Be(0x00090000);
    }

    [Fact]
    public void CapabilityID_GodModeFnQSwitchable_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.GodModeFnQSwitchable).Should().Be(0x00100000);
    }

    [Fact]
    public void CapabilityID_AIChip_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.AIChip).Should().Be(0x000E0000);
    }

    [Fact]
    public void CapabilityID_FanFullSpeed_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.FanFullSpeed).Should().Be(0x04020000);
    }

    [Fact]
    public void CapabilityID_InstantBootAc_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.InstantBootAc).Should().Be(0x03010001);
    }

    [Fact]
    public void CapabilityID_InstantBootUsbPowerDelivery_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.InstantBootUsbPowerDelivery).Should().Be(0x03010002);
    }

    [Fact]
    public void CapabilityID_AllValues_ShouldBeDefined()
    {
        foreach (var value in Enum.GetValues<CapabilityID>())
            Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region MathExtensions Additional Edge Cases

    [Fact]
    public void RoundNearest_Factor1_ShouldReturnSameValue()
    {
        MathExtensions.RoundNearest(42, 1).Should().Be(42);
        MathExtensions.RoundNearest(0, 1).Should().Be(0);
        MathExtensions.RoundNearest(-7, 1).Should().Be(-7);
    }

    [Fact]
    public void RoundNearest_LargeValues_ShouldWork()
    {
        MathExtensions.RoundNearest(999, 100).Should().Be(1000);
        MathExtensions.RoundNearest(950, 100).Should().Be(1000);
        MathExtensions.RoundNearest(949, 100).Should().Be(900);
    }

    [Fact]
    public void RoundNearest_ValueEqualToFactor_ShouldReturnFactor()
    {
        MathExtensions.RoundNearest(10, 10).Should().Be(10);
        MathExtensions.RoundNearest(25, 25).Should().Be(25);
    }

    [Fact]
    public void RoundNearest_ZeroValue_ShouldReturnZero()
    {
        MathExtensions.RoundNearest(0, 5).Should().Be(0);
        MathExtensions.RoundNearest(0, 100).Should().Be(0);
    }

    #endregion
}

