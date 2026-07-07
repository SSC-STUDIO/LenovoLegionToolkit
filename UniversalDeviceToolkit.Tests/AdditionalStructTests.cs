using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class AdditionalStructTests
{
    #region Brightness Struct Tests

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)50)]
    [InlineData((byte)100)]
    [InlineData((byte)255)]
    public void Brightness_Constructor_ShouldSetValues(byte value)
    {
        var b = new Brightness(value);
        b.Value.Should().Be(value);
    }

    [Fact]
    public void Brightness_Equality_SameValues_ShouldBeEqual()
    {
        var a = new Brightness(128);
        var b = new Brightness(128);
        a.Should().Be(b);
    }

    [Fact]
    public void Brightness_Equality_DifferentValues_ShouldNotBeEqual()
    {
        var a = new Brightness(0);
        var b = new Brightness(255);
        a.Should().NotBe(b);
    }

    #endregion

    #region DisplayAdvancedColorInfo Tests

    [Fact]
    public void DisplayAdvancedColorInfo_Constructor_ShouldSetAllProperties()
    {
        var info = new DisplayAdvancedColorInfo(true, true, false, false);
        info.AdvancedColorSupported.Should().BeTrue();
        info.AdvancedColorEnabled.Should().BeTrue();
        info.WideColorEnforced.Should().BeFalse();
        info.AdvancedColorForceDisabled.Should().BeFalse();
    }

    [Fact]
    public void DisplayAdvancedColorInfo_AllFalse_ShouldWork()
    {
        var info = new DisplayAdvancedColorInfo(false, false, false, false);
        info.AdvancedColorSupported.Should().BeFalse();
        info.AdvancedColorEnabled.Should().BeFalse();
    }

    [Fact]
    public void DisplayAdvancedColorInfo_AllTrue_ShouldWork()
    {
        var info = new DisplayAdvancedColorInfo(true, true, true, true);
        info.WideColorEnforced.Should().BeTrue();
        info.AdvancedColorForceDisabled.Should().BeTrue();
    }

    [Fact]
    public void DisplayAdvancedColorInfo_Equality_SameValues_ShouldBeEqual()
    {
        var a = new DisplayAdvancedColorInfo(true, false, true, false);
        var b = new DisplayAdvancedColorInfo(true, false, true, false);
        a.Should().Be(b);
    }

    #endregion

    #region GodModeDefaults Tests

    [Fact]
    public void GodModeDefaults_Defaults_ShouldHaveNullValues()
    {
        var d = new GodModeDefaults();
        d.CPULongTermPowerLimit.Should().BeNull();
        d.CPUShortTermPowerLimit.Should().BeNull();
        d.CPUPeakPowerLimit.Should().BeNull();
        d.CPUCrossLoadingPowerLimit.Should().BeNull();
        d.CPUPL1Tau.Should().BeNull();
        d.APUsPPTPowerLimit.Should().BeNull();
        d.CPUTemperatureLimit.Should().BeNull();
        d.GPUPowerBoost.Should().BeNull();
        d.GPUConfigurableTGP.Should().BeNull();
        d.GPUTemperatureLimit.Should().BeNull();
        d.FanTable.Should().BeNull();
        d.FanFullSpeed.Should().BeNull();
        d.EnableOverclocking.Should().BeNull();
    }

    [Fact]
    public void GodModeDefaults_SetProperties_ShouldRetainValues()
    {
        var d = new GodModeDefaults
        {
            CPULongTermPowerLimit = 45,
            CPUShortTermPowerLimit = 65,
            GPUPowerBoost = 15,
            FanFullSpeed = true,
            EnableOverclocking = true
        };
        d.CPULongTermPowerLimit.Should().Be(45);
        d.CPUShortTermPowerLimit.Should().Be(65);
        d.GPUPowerBoost.Should().Be(15);
        d.FanFullSpeed.Should().BeTrue();
        d.EnableOverclocking.Should().BeTrue();
    }

    #endregion

    #region GodModePreset Tests

    [Fact]
    public void GodModePreset_Defaults_ShouldHaveNullValues()
    {
        var p = new GodModePreset();
        p.Name.Should().BeNull();
        p.PowerPlanGuid.Should().BeNull();
        p.PowerMode.Should().BeNull();
        p.SourcePowerMode.Should().BeNull();
        p.CPULongTermPowerLimit.Should().BeNull();
        p.EnableOverclocking.Should().BeNull();
    }

    [Fact]
    public void GodModePreset_SetProperties_ShouldRetainValues()
    {
        var p = new GodModePreset
        {
            Name = "Custom",
            PowerMode = WindowsPowerMode.BestPerformance,
            SourcePowerMode = PowerModeState.Performance,
            EnableOverclocking = true
        };
        p.Name.Should().Be("Custom");
        p.PowerMode.Should().Be(WindowsPowerMode.BestPerformance);
        p.SourcePowerMode.Should().Be(PowerModeState.Performance);
        p.EnableOverclocking.Should().BeTrue();
    }

    #endregion

    #region GPUStatus Tests

    [Fact]
    public void GPUStatus_Constructor_ShouldSetProperties()
    {
        var processes = new List<Process> { Process.GetCurrentProcess() };
        var status = new GPUStatus(GPUState.Active, "P0", processes);
        status.State.Should().Be(GPUState.Active);
        status.PerformanceState.Should().Be("P0");
        status.Processes.Should().HaveCount(1);
        status.ProcessCount.Should().Be(1);
    }

    [Fact]
    public void GPUStatus_EmptyProcesses_ShouldWork()
    {
        var status = new GPUStatus(GPUState.Inactive, null, new List<Process>());
        status.State.Should().Be(GPUState.Inactive);
        status.PerformanceState.Should().BeNull();
        status.Processes.Should().BeEmpty();
        status.ProcessCount.Should().Be(0);
    }

    #endregion

    #region SensorsData Tests

    [Fact]
    public void SensorsData_Empty_ShouldHaveDefaultValues()
    {
        var data = SensorsData.Empty;
        data.CPU.Should().Be(SensorData.Empty);
        data.GPU.Should().Be(SensorData.Empty);
    }

    [Fact]
    public void SensorsData_Constructor_ShouldSetProperties()
    {
        var cpu = new SensorData(50, 100, 3000, 5000, 800, 1200, 65, 100, 45, 0.8, 1200, 2000);
        var gpu = new SensorData(30, 100, 1500, 2000, 700, 1000, 55, 95, 40, 0.7, 1500, 2500);
        var data = new SensorsData(cpu, gpu);
        data.CPU.Utilization.Should().Be(50);
        data.GPU.Utilization.Should().Be(30);
    }

    [Fact]
    public void SensorsData_ToString_ShouldContainCPUAndGPU()
    {
        var data = SensorsData.Empty;
        var str = data.ToString();
        str.Should().Contain("CPU");
        str.Should().Contain("GPU");
    }

    #endregion

    #region WarrantyInfo Tests

    [Fact]
    public void WarrantyInfo_Constructor_ShouldSetProperties()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2027, 1, 1);
        var link = new Uri("https://example.com/warranty");
        var info = new WarrantyInfo(start, end, link);
        info.Start.Should().Be(start);
        info.End.Should().Be(end);
        info.Link.Should().Be(link);
    }

    [Fact]
    public void WarrantyInfo_NullValues_ShouldWork()
    {
        var info = new WarrantyInfo(null, null, null);
        info.Start.Should().BeNull();
        info.End.Should().BeNull();
        info.Link.Should().BeNull();
    }

    [Fact]
    public void WarrantyInfo_PartialNull_ShouldWork()
    {
        var start = new DateTime(2024, 1, 1);
        var info = new WarrantyInfo(start, null, null);
        info.Start.Should().Be(start);
        info.End.Should().BeNull();
    }

    [Fact]
    public void WarrantyInfo_Equality_SameValues_ShouldBeEqual()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2027, 1, 1);
        var link = new Uri("https://example.com");
        var a = new WarrantyInfo(start, end, link);
        var b = new WarrantyInfo(start, end, link);
        a.Should().Be(b);
    }

    [Fact]
    public void WarrantyInfo_Equality_DifferentValues_ShouldNotBeEqual()
    {
        var a = new WarrantyInfo(new DateTime(2024, 1, 1), null, null);
        var b = new WarrantyInfo(new DateTime(2025, 1, 1), null, null);
        a.Should().NotBe(b);
    }

    #endregion

    #region SpectrumKeyboardBacklightEffect Tests

    [Fact]
    public void SpectrumKeyboardBacklightEffect_Constructor_ShouldSetProperties()
    {
        var colors = new RGBColor[] { RGBColor.Red, RGBColor.Green };
        var keys = new ushort[] { 1, 2, 3 };
        var effect = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.Always,
            SpectrumKeyboardBacklightSpeed.Speed2,
            SpectrumKeyboardBacklightDirection.LeftToRight,
            SpectrumKeyboardBacklightClockwiseDirection.None,
            colors, keys);
        effect.Type.Should().Be(SpectrumKeyboardBacklightEffectType.Always);
        effect.Speed.Should().Be(SpectrumKeyboardBacklightSpeed.Speed2);
        effect.Direction.Should().Be(SpectrumKeyboardBacklightDirection.LeftToRight);
        effect.Colors.Should().HaveCount(2);
    }

    [Fact]
    public void SpectrumKeyboardBacklightEffect_Equality_SameValues_ShouldBeEqual()
    {
        var colors = new RGBColor[] { RGBColor.Red };
        var keys = new ushort[] { 1 };
        var a = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.Always,
            SpectrumKeyboardBacklightSpeed.Speed1,
            SpectrumKeyboardBacklightDirection.None,
            SpectrumKeyboardBacklightClockwiseDirection.None,
            colors, keys);
        var b = new SpectrumKeyboardBacklightEffect(
            SpectrumKeyboardBacklightEffectType.Always,
            SpectrumKeyboardBacklightSpeed.Speed1,
            SpectrumKeyboardBacklightDirection.None,
            SpectrumKeyboardBacklightClockwiseDirection.None,
            colors, keys);
        a.Should().Be(b);
    }

    #endregion

    #region WindowsPowerPlan Struct Tests

    [Fact]
    public void WindowsPowerPlan_Constructor_ShouldSetProperties()
    {
        var guid = Guid.NewGuid();
        var plan = new WindowsPowerPlan(guid, "High Performance", true);
        plan.Guid.Should().Be(guid);
        plan.Name.Should().Be("High Performance");
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WindowsPowerPlan_Inactive_ShouldWork()
    {
        var plan = new WindowsPowerPlan(Guid.NewGuid(), "Balanced", false);
        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WindowsPowerPlan_Equality_SameValues_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        var a = new WindowsPowerPlan(guid, "Plan", true);
        var b = new WindowsPowerPlan(guid, "Plan", true);
        a.Should().Be(b);
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public void Device_Index_ShouldReturnExpectedProperties()
    {
        var device = new Device(
            "TestDevice", "Description", "BusDesc",
            "PCI\\VEN_1234", Guid.NewGuid(),
            "Display", true, false);
        device.Name.Should().Be("TestDevice");
        device.Description.Should().Be("Description");
        device.IsRemovable.Should().BeTrue();
        device.IsDisconnected.Should().BeFalse();
    }

    [Fact]
    public void Device_DefaultIndex_ShouldBeZero()
    {
        var device = new Device(
            "Test", "Desc", "Bus",
            "ID", Guid.NewGuid(),
            "Class", false, true);
        device.Index.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DriverInfo_Constructor_ShouldSetProperties()
    {
        var version = new Version(1, 2, 3, 4);
        var date = new DateTime(2024, 6, 15);
        var info = new DriverInfo("PCI\\VEN_1234", "HID\\VID_1234", version, date);
        info.DeviceId.Should().Be("PCI\\VEN_1234");
        info.HardwareId.Should().Be("HID\\VID_1234");
        info.Version.Should().Be(version);
        info.Date.Should().Be(date);
    }

    [Fact]
    public void DriverInfo_NullVersionAndDate_ShouldWork()
    {
        var info = new DriverInfo("dev", "hw", null, null);
        info.Version.Should().BeNull();
        info.Date.Should().BeNull();
    }

    #endregion
}

