using System;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class AdditionalEnumAndModelTests
{
    #region HybridModeState Enum Tests

    [Theory]
    [InlineData(HybridModeState.On)]
    [InlineData(HybridModeState.OnIGPUOnly)]
    [InlineData(HybridModeState.OnAuto)]
    [InlineData(HybridModeState.Off)]
    public void HybridModeState_ShouldBeDefined(HybridModeState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void HybridModeState_AllValues_ShouldHaveFourMembers()
    {
        Enum.GetValues<HybridModeState>().Should().HaveCount(4);
    }

    #endregion

    #region IGPUModeState Enum Tests

    [Theory]
    [InlineData(IGPUModeState.Default)]
    [InlineData(IGPUModeState.IGPUOnly)]
    [InlineData(IGPUModeState.Auto)]
    public void IGPUModeState_ShouldBeDefined(IGPUModeState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void IGPUModeState_Default_ShouldBeDefault()
    {
        default(IGPUModeState).Should().Be(IGPUModeState.Default);
    }

    #endregion

    #region HDRState Enum Tests

    [Theory]
    [InlineData(HDRState.Off)]
    [InlineData(HDRState.On)]
    public void HDRState_ShouldBeDefined(HDRState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region GSyncState Enum Tests

    [Theory]
    [InlineData(GSyncState.Off)]
    [InlineData(GSyncState.On)]
    public void GSyncState_ShouldBeDefined(GSyncState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region NotificationPriority Enum Tests

    [Theory]
    [InlineData(NotificationPriority.Low)]
    [InlineData(NotificationPriority.Normal)]
    [InlineData(NotificationPriority.High)]
    public void NotificationPriority_ShouldBeDefined(NotificationPriority value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void NotificationPriority_AllValues_ShouldHaveThreeMembers()
    {
        Enum.GetValues<NotificationPriority>().Should().HaveCount(3);
    }

    #endregion

    #region Theme Enum Tests

    [Theory]
    [InlineData(Theme.System)]
    [InlineData(Theme.Light)]
    [InlineData(Theme.Dark)]
    public void Theme_ShouldBeDefined(Theme value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region ThemeStylePreset Enum Tests

    [Theory]
    [InlineData(ThemeStylePreset.Default)]
    [InlineData(ThemeStylePreset.Official)]
    [InlineData(ThemeStylePreset.Midnight)]
    [InlineData(ThemeStylePreset.Forest)]
    public void ThemeStylePreset_ShouldBeDefined(ThemeStylePreset value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region AccentColorSource Enum Tests

    [Theory]
    [InlineData(AccentColorSource.System)]
    [InlineData(AccentColorSource.Custom)]
    public void AccentColorSource_ShouldBeDefined(AccentColorSource value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region WindowBackdropStyle Enum Tests

    [Theory]
    [InlineData(WindowBackdropStyle.Windows)]
    [InlineData(WindowBackdropStyle.macOS)]
    public void WindowBackdropStyle_ShouldBeDefined(WindowBackdropStyle value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region NotificationPosition Enum Tests

    [Fact]
    public void NotificationPosition_AllValues_ShouldBeDefined()
    {
        foreach (var value in Enum.GetValues<NotificationPosition>())
            Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void NotificationPosition_ShouldHaveMultipleMembers()
    {
        Enum.GetValues<NotificationPosition>().Length.Should().BeGreaterThanOrEqualTo(6);
    }

    #endregion

    #region KnownFolder Enum Tests

    [Theory]
    [InlineData(KnownFolder.Contacts)]
    [InlineData(KnownFolder.Downloads)]
    [InlineData(KnownFolder.Favorites)]
    [InlineData(KnownFolder.Links)]
    [InlineData(KnownFolder.SavedGames)]
    [InlineData(KnownFolder.SavedSearches)]
    public void KnownFolder_ShouldBeDefined(KnownFolder value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void KnownFolder_ShouldHaveSixMembers()
    {
        Enum.GetValues<KnownFolder>().Should().HaveCount(6);
    }

    #endregion

    #region LampEffectType Enum Tests

    [Fact]
    public void LampEffectType_AllValues_ShouldBeDefined()
    {
        foreach (var value in Enum.GetValues<LampEffectType>())
            Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void LampEffectType_ShouldHaveMultipleMembers()
    {
        Enum.GetValues<LampEffectType>().Length.Should().BeGreaterThanOrEqualTo(6);
    }

    #endregion

    #region Delay Struct Tests

    [Fact]
    public void Delay_ShouldReturnConstructorValue()
    {
        var delay = new Delay(5);
        delay.DelaySeconds.Should().Be(5);
    }

    [Fact]
    public void Delay_Zero_ShouldWork()
    {
        var delay = new Delay(0);
        delay.DelaySeconds.Should().Be(0);
    }

    [Fact]
    public void Delay_LargeValue_ShouldWork()
    {
        var delay = new Delay(3600);
        delay.DelaySeconds.Should().Be(3600);
    }

    #endregion

    #region ITSMode Enum Tests

    [Theory]
    [InlineData(ITSMode.None)]
    public void ITSMode_ShouldBeDefined(ITSMode value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void ITSMode_AllValues_ShouldBeDefined()
    {
        foreach (var value in Enum.GetValues<ITSMode>())
            Enum.IsDefined(value).Should().BeTrue();
    }

    #endregion

    #region Additional ProcessInfo Edge Cases

    [Fact]
    public void ProcessInfo_Constructor_NullPath_ShouldWork()
    {
        var info = new ProcessInfo("test", null);
        info.Name.Should().Be("test");
        info.ExecutablePath.Should().BeNull();
    }

    [Fact]
    public void ProcessInfo_FromPath_EmptyString_ShouldWork()
    {
        var info = ProcessInfo.FromPath("");
        info.Name.Should().Be("");
        info.ExecutablePath.Should().Be("");
    }

    [Fact]
    public void ProcessInfo_ToString_ShouldContainNameAndPath()
    {
        var info = new ProcessInfo("MyApp", @"C:\MyApp.exe");
        info.ToString().Should().Contain("MyApp").And.Contain(@"C:\MyApp.exe");
    }

    #endregion

    #region Device Struct Additional Edge Cases

    [Fact]
    public void Device_Constructor_ShouldSetAllProperties()
    {
        var guid = Guid.NewGuid();
        var device = new Device(
            "TestDevice", "A test device", "Bus Description",
            "PCI\\VEN_10DE", guid, "Display", true, true);

        device.Name.Should().Be("TestDevice");
        device.Description.Should().Be("A test device");
        device.BusReportedDeviceDescription.Should().Be("Bus Description");
        device.DeviceInstanceId.Should().Be("PCI\\VEN_10DE");
        device.ClassGuid.Should().Be(guid);
        device.ClassName.Should().Be("Display");
        device.IsRemovable.Should().BeTrue();
        device.IsDisconnected.Should().BeTrue();
    }

    [Fact]
    public void Device_IsDisconnected_ShouldReflectConstructor()
    {
        var device = new Device("N", "D", "B", "I", Guid.NewGuid(), "C", false, true);
        device.IsDisconnected.Should().BeTrue();
    }

    [Fact]
    public void Device_IsRemovable_ShouldReflectConstructor()
    {
        var device = new Device("N", "D", "B", "I", Guid.NewGuid(), "C", true, false);
        device.IsRemovable.Should().BeTrue();
    }

    #endregion

    #region Additional FanTableData Edge Cases

    [Fact]
    public void FanTableData_WithLargeArrays_ShouldWork()
    {
        ushort[] speeds = new ushort[100];
        ushort[] temps = new ushort[100];
        for (int i = 0; i < 100; i++)
        {
            speeds[i] = (ushort)(i * 100);
            temps[i] = (ushort)(i);
        }
        var data = new FanTableData(FanTableType.CPU, 0, 0, speeds, temps);
        data.FanSpeeds.Should().HaveCount(100);
        data.Temps.Should().HaveCount(100);
        data.FanSpeeds[99].Should().Be(9900);
        data.Temps[99].Should().Be(99);
    }

    [Fact]
    public void FanTableData_MaxByteValues_ShouldWork()
    {
        var data = new FanTableData(FanTableType.GPU, byte.MaxValue, byte.MaxValue, [ushort.MaxValue], [ushort.MaxValue]);
        data.FanId.Should().Be(byte.MaxValue);
        data.SensorId.Should().Be(byte.MaxValue);
        data.FanSpeeds[0].Should().Be(ushort.MaxValue);
        data.Temps[0].Should().Be(ushort.MaxValue);
    }

    #endregion

    #region Additional DriverInfo Edge Cases

    [Fact]
    public void DriverInfo_EmptyStrings_ShouldWork()
    {
        var info = new DriverInfo("", "", null, null);
        info.DeviceId.Should().Be("");
        info.HardwareId.Should().Be("");
        info.Version.Should().BeNull();
        info.Date.Should().BeNull();
    }

    [Fact]
    public void DriverInfo_DifferentDates_ShouldNotBeEqual()
    {
        var a = new DriverInfo("DEV1", "HW1", null, new DateTime(2020, 1, 1));
        var b = new DriverInfo("DEV1", "HW1", null, new DateTime(2025, 1, 1));
        a.Equals(b).Should().BeFalse();
    }

    #endregion

    #region Additional WindowsPowerPlan Edge Cases

    [Fact]
    public void WindowsPowerPlan_DifferentNamesSameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        var a = new WindowsPowerPlan(guid, "Balanced", true);
        var b = new WindowsPowerPlan(guid, "High Performance", false);
        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void WindowsPowerPlan_DifferentGuidsSameName_ShouldNotBeEqual()
    {
        var a = new WindowsPowerPlan(Guid.NewGuid(), "Balanced", true);
        var b = new WindowsPowerPlan(Guid.NewGuid(), "Balanced", true);
        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    #endregion

    #region Additional RGBKeyboardBacklightState Edge Cases

    [Fact]
    public void RGBKeyboardBacklightState_AllPresets_ShouldWork()
    {
        var presets = new Dictionary<RGBKeyboardBacklightPreset, RGBKeyboardBacklightBacklightPresetDescription>
        {
            { RGBKeyboardBacklightPreset.Off, RGBKeyboardBacklightBacklightPresetDescription.Default },
            { RGBKeyboardBacklightPreset.One, RGBKeyboardBacklightBacklightPresetDescription.Default },
            { RGBKeyboardBacklightPreset.Two, RGBKeyboardBacklightBacklightPresetDescription.Default },
            { RGBKeyboardBacklightPreset.Three, RGBKeyboardBacklightBacklightPresetDescription.Default }
        };
        var state = new RGBKeyboardBacklightState(RGBKeyboardBacklightPreset.Off, presets);
        state.SelectedPreset.Should().Be(RGBKeyboardBacklightPreset.Off);
        state.Presets.Should().HaveCount(4);
    }

    #endregion

    #region Additional StepperValue Edge Cases

    [Fact]
    public void StepperValue_WithNullSteps_ShouldWork()
    {
        var sv = new StepperValue(10, 0, 100, 5, null!, 50);
        sv.Steps.Should().BeNull();
    }

    [Fact]
    public void StepperValue_WithNegativeValues_ShouldWork()
    {
        var sv = new StepperValue(-10, -50, 0, 5, [-50, -25, 0], -25);
        sv.Value.Should().Be(-10);
        sv.Min.Should().Be(-50);
        sv.Max.Should().Be(0);
        sv.DefaultValue.Should().Be(-25);
    }

    #endregion

    #region Additional BiosVersion Edge Cases

    [Fact]
    public void BiosVersion_DifferentPrefixes_ShouldNotBeEqual()
    {
        var a = new BiosVersion("J", 100);
        var b = new BiosVersion("K", 100);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void BiosVersion_NullVersion_ShouldBeLowerThanAny()
    {
        var a = new BiosVersion("J", null);
        var b = new BiosVersion("J", 0);
        a.IsLowerThan(b).Should().BeTrue();
    }

    [Fact]
    public void BiosVersion_BothNull_ShouldBeEqual()
    {
        var a = new BiosVersion("J", null);
        var b = new BiosVersion("J", null);
        a.Equals(b).Should().BeTrue();
    }

    #endregion

    #region Additional Resolution Edge Cases

    [Fact]
    public void Resolution_DifferentWidths_ShouldNotBeEqual()
    {
        var a = new Resolution(1920, 1080);
        var b = new Resolution(2560, 1080);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Resolution_DifferentHeights_ShouldNotBeEqual()
    {
        var a = new Resolution(1920, 1080);
        var b = new Resolution(1920, 1440);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Resolution_SameValues_ShouldBeEqual()
    {
        var a = new Resolution(3840, 2160);
        var b = new Resolution(3840, 2160);
        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Resolution_CompareTo_ShouldWork()
    {
        var a = new Resolution(1920, 1080);
        var b = new Resolution(3840, 2160);
        a.CompareTo(b).Should().BeNegative();
    }

    [Fact]
    public void Resolution_DisplayName_ShouldContainResolution()
    {
        var res = new Resolution(1920, 1080);
        res.DisplayName.Should().Contain("1920");
        res.DisplayName.Should().Contain("1080");
    }

    #endregion

    #region Additional DpiScale Edge Cases

    [Fact]
    public void DpiScale_SameValues_ShouldBeEqual()
    {
        var a = new DpiScale(100);
        var b = new DpiScale(100);
        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void DpiScale_DifferentValues_ShouldNotBeEqual()
    {
        var a = new DpiScale(100);
        var b = new DpiScale(150);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void DpiScale_GetHashCode_SameValues_ShouldMatch()
    {
        var a = new DpiScale(125);
        var b = new DpiScale(125);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void DpiScale_DisplayName_ShouldContainScale()
    {
        var scale = new DpiScale(150);
        scale.DisplayName.Should().Contain("150");
    }

    #endregion

    #region Additional RefreshRate Edge Cases

    [Fact]
    public void RefreshRate_SameValues_ShouldBeEqual()
    {
        var a = new RefreshRate(144);
        var b = new RefreshRate(144);
        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void RefreshRate_DifferentValues_ShouldNotBeEqual()
    {
        var a = new RefreshRate(60);
        var b = new RefreshRate(144);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void RefreshRate_GetHashCode_SameValues_ShouldMatch()
    {
        var a = new RefreshRate(240);
        var b = new RefreshRate(240);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void RefreshRate_DisplayName_ShouldContainFrequency()
    {
        var rate = new RefreshRate(60);
        rate.DisplayName.Should().Contain("60");
    }

    #endregion
}

