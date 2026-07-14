using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class EnumStructTests
{
    #region RebootType Tests

    [Theory]
    [InlineData(RebootType.NotRequired, 0)]
    [InlineData(RebootType.Forced, 1)]
    [InlineData(RebootType.Requested, 3)]
    [InlineData(RebootType.ForcedPowerOff, 4)]
    [InlineData(RebootType.Delayed, 5)]
    public void RebootType_ShouldHaveExpectedValues(RebootType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    [Fact]
    public void RebootType_Default_ShouldBeNotRequired()
    {
        var reboot = default(RebootType);
        reboot.Should().Be(RebootType.NotRequired);
    }

    #endregion

    #region FanType Tests

    [Fact]
    public void FanType_Cpu_ShouldBeZero()
    {
        ((int)FanType.Cpu).Should().Be(0);
    }

    [Fact]
    public void FanType_Gpu_ShouldBeOne()
    {
        ((int)FanType.Gpu).Should().Be(1);
    }

    #endregion

    #region FanTableType Tests

    [Theory]
    [InlineData(FanTableType.Unknown)]
    [InlineData(FanTableType.CPU)]
    [InlineData(FanTableType.CPUSensor)]
    [InlineData(FanTableType.GPU)]
    public void FanTableType_ShouldContainKnownValues(FanTableType type)
    {
        Enum.IsDefined(type).Should().BeTrue();
    }

    #endregion

    #region DriverKey Tests

    [Fact]
    public void DriverKey_FnF10_ShouldBe32()
    {
        ((int)DriverKey.FnF10).Should().Be(32);
    }

    [Fact]
    public void DriverKey_FnSpace_ShouldBe4096()
    {
        ((int)DriverKey.FnSpace).Should().Be(4096);
    }

    #endregion

    #region GPUState Tests

    [Theory]
    [InlineData(GPUState.Unknown)]
    [InlineData(GPUState.Active)]
    [InlineData(GPUState.MonitorConnected)]
    public void GPUState_ShouldContainKnownValues(GPUState state)
    {
        Enum.IsDefined(state).Should().BeTrue();
    }

    #endregion

    #region CapabilityID Tests

    [Fact]
    public void CapabilityID_IGPUMode_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.IGPUMode).Should().Be(0x00010000);
    }

    [Fact]
    public void CapabilityID_FlipToStart_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.FlipToStart).Should().Be(0x00030000);
    }

    [Fact]
    public void CapabilityID_OverDrive_ShouldBeExpectedHex()
    {
        ((int)CapabilityID.OverDrive).Should().Be(0x001A0000);
    }

    #endregion

    #region CpuProfileMode Tests

    [Fact]
    public void CpuProfileMode_Productivity_ShouldBeDefined()
    {
        Enum.IsDefined(CpuProfileMode.Productivity).Should().BeTrue();
    }

    [Fact]
    public void CpuProfileMode_X3DGaming_ShouldBeDefined()
    {
        Enum.IsDefined(CpuProfileMode.X3DGaming).Should().BeTrue();
    }

    #endregion

    #region AlwaysOnUSBState Tests

    [Fact]
    public void AlwaysOnUSBState_Off_ShouldBeDefault()
    {
        var state = default(AlwaysOnUSBState);
        state.Should().Be(AlwaysOnUSBState.Off);
    }

    #endregion
}
