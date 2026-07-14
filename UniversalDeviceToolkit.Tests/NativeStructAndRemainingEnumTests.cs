using System;
using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class NativeStructAndRemainingEnumTests
{
    [Theory]
    [InlineData(BootLogoFormat.Jpeg, 0x1)]
    [InlineData(BootLogoFormat.Bmp, 0x10)]
    [InlineData(BootLogoFormat.Png, 0x20)]
    public void BootLogoFormat_HexValues(BootLogoFormat format, byte expected)
    {
        ((byte)format).Should().Be(expected);
    }

    [Fact]
    public void BootLogoFormat_CombinedFlags()
    {
        var combined = BootLogoFormat.Bmp | BootLogoFormat.Png;
        combined.Should().HaveFlag(BootLogoFormat.Bmp);
        combined.Should().HaveFlag(BootLogoFormat.Png);
        combined.Should().NotHaveFlag(BootLogoFormat.Jpeg);
    }

    [Fact]
    public void BootLogoFormat_AllFlags_ShouldBe0x31()
    {
        var all = BootLogoFormat.Jpeg | BootLogoFormat.Bmp | BootLogoFormat.Png;
        ((byte)all).Should().Be(0x31);
    }

    [Fact]
    public void BootLogoChecksum_Crc()
    {
        var cs = new BootLogoChecksum { Crc = 42 };
        cs.Crc.Should().Be(42);
    }

    [Fact]
    public void FanCurveSettingsStore_EmptyByDefault()
    {
        var store = new FanCurveSettings.FanCurveSettingsStore();
        store.Entries.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void FanCurveSettingsStore_CanAddEntries()
    {
        var store = new FanCurveSettings.FanCurveSettingsStore();
        store.Entries.Add(new FanCurveEntry());
        store.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void BalanceModeSettingsStore_Default()
    {
        var store = new BalanceModeSettings.BalanceModeSettingsStore();
        store.AIModeEnabled.Should().BeFalse();
    }

    [Fact]
    public void GodModeSettingsStore_Default()
    {
        var store = new GodModeSettings.GodModeSettingsStore();
        store.Presets.Should().NotBeNull();
    }

    [Fact]
    public void IntegrationsSettingsStore_Default()
    {
        var store = new IntegrationsSettings.IntegrationsSettingsStore();
        store.HWiNFO.Should().BeFalse();
        store.CLI.Should().BeFalse();
    }

    [Fact]
    public void GPUOverclockSettingsStore_Default()
    {
        var store = new GPUOverclockSettings.GPUOverclockSettingsStore();
        store.Info.Should().Be(GPUOverclockInfo.Zero);
        store.Enabled.Should().BeFalse();
    }

    [Fact]
    public void SpectrumKeyboardBacklightDirection_Has5Values()
    {
        Enum.GetValues<SpectrumKeyboardBacklightDirection>().Should().HaveCount(5);
    }

    [Fact]
    public void LampEffectType_Has12Values()
    {
        var values = Enum.GetValues<LampEffectType>();
        values.Should().HaveCount(12);
        ((int)LampEffectType.Static).Should().Be(0);
        ((int)LampEffectType.AuroraSync).Should().Be(11);
    }

    [Theory]
    [InlineData(LegionSeries.IdeaPad, 9)]
    [InlineData(LegionSeries.IdeaPad_Gaming, 10)]
    [InlineData(LegionSeries.ThinkBook, 13)]
    public void LegionSeries_ExtendedValues(LegionSeries series, int expected)
    {
        ((int)series).Should().Be(expected);
    }

    [Theory]
    [InlineData(RGBKeyboardBacklightEffect.Static, 0)]
    [InlineData(RGBKeyboardBacklightEffect.WaveLTR, 4)]
    public void RGBKeyboardBacklightEffect_Values(RGBKeyboardBacklightEffect effect, int expected)
    {
        ((int)effect).Should().Be(expected);
    }

    [Fact]
    public void SpectrumKeyboardBacklightEffectType_Has13Values()
    {
        Enum.GetValues<SpectrumKeyboardBacklightEffectType>().Should().HaveCount(13);
    }

    [Theory]
    [InlineData(FanMaxSpeedState.Off, 0)]
    [InlineData(FanMaxSpeedState.On, 1)]
    [InlineData(FanMaxSpeedState.Toggle, 2)]
    public void FanMaxSpeedState_Values(FanMaxSpeedState state, int expected)
    {
        ((int)state).Should().Be(expected);
    }

    [Theory]
    [InlineData(WhiteKeyboardBacklightState.Off, 0)]
    [InlineData(WhiteKeyboardBacklightState.Low, 1)]
    [InlineData(WhiteKeyboardBacklightState.High, 2)]
    public void WhiteKeyboardBacklightState_Values(WhiteKeyboardBacklightState state, int expected)
    {
        ((int)state).Should().Be(expected);
    }

    [Theory]
    [InlineData(PanelLogoBacklightState.Off, 0)]
    [InlineData(PanelLogoBacklightState.On, 1)]
    public void PanelLogoBacklightState_Values(PanelLogoBacklightState state, int expected)
    {
        ((int)state).Should().Be(expected);
    }

    [Theory]
    [InlineData(PortsBacklightState.Off, 0)]
    [InlineData(PortsBacklightState.On, 1)]
    public void PortsBacklightState_Values(PortsBacklightState state, int expected)
    {
        ((int)state).Should().Be(expected);
    }

    [Fact]
    public void GPUOverclockInfo_Zero()
    {
        GPUOverclockInfo.Zero.CoreDeltaMhz.Should().Be(0);
        GPUOverclockInfo.Zero.MemoryDeltaMhz.Should().Be(0);
    }

    [Fact]
    public void GPUOverclockInfo_Equality()
    {
        new GPUOverclockInfo(100, 200).Should().Be(new GPUOverclockInfo(100, 200));
        new GPUOverclockInfo(100, 200).Should().NotBe(new GPUOverclockInfo(100, 300));
    }

    [Fact]
    public void DpiScale_Equality()
    {
        new DpiScale(150).Should().Be(new DpiScale(150));
        new DpiScale(100).Should().NotBe(new DpiScale(150));
    }

    [Fact]
    public void DpiScale_DisplayName()
    {
        new DpiScale(150).DisplayName.Should().Be("150%");
    }

    [Fact]
    public void RefreshRate_Equality()
    {
        new RefreshRate(144).Should().Be(new RefreshRate(144));
        new RefreshRate(60).Should().NotBe(new RefreshRate(144));
    }

    [Fact]
    public void RefreshRate_DisplayName()
    {
        new RefreshRate(144).DisplayName.Should().Be("144 Hz");
    }

    [Fact]
    public void StepperValue_WithValue()
    {
        var sv = new StepperValue(50, 0, 100, 25, new[] { 0, 25, 50, 75, 100 }, null);
        sv.WithValue(75).Value.Should().Be(75);
    }

    [Fact]
    public void Resolution_DisplayName()
    {
        new Resolution(1920, 1080).DisplayName.Should().Contain("1920");
    }
}
