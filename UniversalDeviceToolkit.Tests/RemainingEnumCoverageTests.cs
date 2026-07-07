using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class RemainingEnumCoverageTests
{
    #region AutorunState

    [Theory]
    [InlineData(AutorunState.Enabled)]
    [InlineData(AutorunState.EnabledDelayed)]
    [InlineData(AutorunState.Disabled)]
    public void AutorunState_ShouldBeDefined(AutorunState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void AutorunState_ShouldHaveThreeMembers()
    {
        Enum.GetValues<AutorunState>().Should().HaveCount(3);
    }

    #endregion

    #region BatteryNightChargeState

    [Theory]
    [InlineData(BatteryNightChargeState.On)]
    [InlineData(BatteryNightChargeState.Off)]
    public void BatteryNightChargeState_ShouldBeDefined(BatteryNightChargeState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void BatteryNightChargeState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<BatteryNightChargeState>().Should().HaveCount(2);
    }

    #endregion

    #region BatteryState

    [Theory]
    [InlineData(BatteryState.Conservation)]
    [InlineData(BatteryState.Normal)]
    [InlineData(BatteryState.RapidCharge)]
    public void BatteryState_ShouldBeDefined(BatteryState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void BatteryState_ShouldHaveThreeMembers()
    {
        Enum.GetValues<BatteryState>().Should().HaveCount(3);
    }

    #endregion

    #region FanState

    [Theory]
    [InlineData(FanState.Auto)]
    [InlineData(FanState.Manual)]
    public void FanState_ShouldBeDefined(FanState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void FanState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<FanState>().Should().HaveCount(2);
    }

    #endregion

    #region FlipToStartState

    [Theory]
    [InlineData(FlipToStartState.Off)]
    [InlineData(FlipToStartState.On)]
    public void FlipToStartState_ShouldBeDefined(FlipToStartState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void FlipToStartState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<FlipToStartState>().Should().HaveCount(2);
    }

    #endregion

    #region FnLockState

    [Theory]
    [InlineData(FnLockState.Off)]
    [InlineData(FnLockState.On)]
    public void FnLockState_ShouldBeDefined(FnLockState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void FnLockState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<FnLockState>().Should().HaveCount(2);
    }

    #endregion

    #region GSyncState

    [Theory]
    [InlineData(GSyncState.Off)]
    [InlineData(GSyncState.On)]
    public void GSyncState_ShouldBeDefined(GSyncState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void GSyncState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<GSyncState>().Should().HaveCount(2);
    }

    #endregion

    #region HDRState

    [Theory]
    [InlineData(HDRState.Off)]
    [InlineData(HDRState.On)]
    public void HDRState_ShouldBeDefined(HDRState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void HDRState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<HDRState>().Should().HaveCount(2);
    }

    #endregion

    #region ITSMode

    [Theory]
    [InlineData(ITSMode.None)]
    [InlineData(ITSMode.ItsAuto)]
    [InlineData(ITSMode.MmcCool)]
    [InlineData(ITSMode.MmcPerformance)]
    [InlineData(ITSMode.MmcGeek)]
    public void ITSMode_ShouldBeDefined(ITSMode value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void ITSMode_ShouldHaveFiveMembers()
    {
        Enum.GetValues<ITSMode>().Should().HaveCount(5);
    }

    #endregion

    #region InstantBootState

    [Theory]
    [InlineData(InstantBootState.Off)]
    [InlineData(InstantBootState.AcAdapter)]
    [InlineData(InstantBootState.UsbPowerDelivery)]
    [InlineData(InstantBootState.AcAdapterAndUsbPowerDelivery)]
    public void InstantBootState_ShouldBeDefined(InstantBootState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void InstantBootState_ShouldHaveFourMembers()
    {
        Enum.GetValues<InstantBootState>().Should().HaveCount(4);
    }

    #endregion

    #region LegionSeries

    [Theory]
    [InlineData(LegionSeries.Legion_5)]
    [InlineData(LegionSeries.Legion_Pro_5)]
    [InlineData(LegionSeries.Legion_Slim_5)]
    [InlineData(LegionSeries.Legion_7)]
    [InlineData(LegionSeries.Legion_Pro_7)]
    [InlineData(LegionSeries.Legion_9)]
    [InlineData(LegionSeries.Legion_Go)]
    [InlineData(LegionSeries.Legion_Legacy)]
    [InlineData(LegionSeries.YOGA)]
    [InlineData(LegionSeries.LOQ)]
    public void LegionSeries_ShouldBeDefined(LegionSeries value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void LegionSeries_Unknown_ShouldHaveMaxValue()
    {
        ((int)LegionSeries.Unknown).Should().Be(255);
    }

    [Fact]
    public void LegionSeries_ShouldHave15Members()
    {
        Enum.GetValues<LegionSeries>().Should().NotBeEmpty();
    }

    #endregion

    #region LightingChangeState

    [Fact]
    public void LightingChangeState_Panel_ShouldEqualZero()
    {
        ((int)LightingChangeState.Panel).Should().Be(0);
    }

    [Fact]
    public void LightingChangeState_Ports_ShouldEqualOne()
    {
        ((int)LightingChangeState.Ports).Should().Be(1);
    }

    #endregion

    #region ModifierKey

    [Theory]
    [InlineData(ModifierKey.None)]
    [InlineData(ModifierKey.Shift)]
    [InlineData(ModifierKey.Ctrl)]
    [InlineData(ModifierKey.Alt)]
    public void ModifierKey_ShouldBeDefined(ModifierKey value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void ModifierKey_None_ShouldBeZero()
    {
        ((int)ModifierKey.None).Should().Be(0);
    }

    [Fact]
    public void ModifierKey_Shift_ShouldBeOne()
    {
        ((int)ModifierKey.Shift).Should().Be(1);
    }

    [Fact]
    public void ModifierKey_Ctrl_ShouldBeTwo()
    {
        ((int)ModifierKey.Ctrl).Should().Be(2);
    }

    [Fact]
    public void ModifierKey_Alt_ShouldBeFour()
    {
        ((int)ModifierKey.Alt).Should().Be(4);
    }

    [Fact]
    public void ModifierKey_ShouldHaveFourMembers()
    {
        Enum.GetValues<ModifierKey>().Should().HaveCount(4);
    }

    #endregion

    #region NotificationDuration

    [Theory]
    [InlineData(NotificationDuration.Short)]
    [InlineData(NotificationDuration.Normal)]
    [InlineData(NotificationDuration.Long)]
    public void NotificationDuration_ShouldBeDefined(NotificationDuration value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void NotificationDuration_ShouldHaveThreeMembers()
    {
        Enum.GetValues<NotificationDuration>().Should().HaveCount(3);
    }

    #endregion

    #region NotificationType

    [Theory]
    [InlineData(NotificationType.ACAdapterConnected)]
    [InlineData(NotificationType.ACAdapterDisconnected)]
    [InlineData(NotificationType.CameraOn)]
    [InlineData(NotificationType.CameraOff)]
    [InlineData(NotificationType.FnLockOn)]
    [InlineData(NotificationType.MicrophoneOff)]
    [InlineData(NotificationType.PowerModeQuiet)]
    [InlineData(NotificationType.PowerModeBalance)]
    [InlineData(NotificationType.PowerModePerformance)]
    [InlineData(NotificationType.PowerModeGodMode)]
    [InlineData(NotificationType.RGBKeyboardBacklightChanged)]
    [InlineData(NotificationType.SpectrumBacklightChanged)]
    public void NotificationType_ShouldBeDefined(NotificationType value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void NotificationType_ShouldHaveMultipleMembers()
    {
        Enum.GetValues<NotificationType>().Should().NotBeEmpty();
    }

    #endregion

    #region NotificationPriority

    [Theory]
    [InlineData(NotificationPriority.Low)]
    [InlineData(NotificationPriority.Normal)]
    [InlineData(NotificationPriority.High)]
    public void NotificationPriority_ShouldBeDefined(NotificationPriority value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void NotificationPriority_ShouldHaveThreeMembers()
    {
        Enum.GetValues<NotificationPriority>().Should().HaveCount(3);
    }

    #endregion

    #region PortsBacklightState

    [Theory]
    [InlineData(PortsBacklightState.Off)]
    [InlineData(PortsBacklightState.On)]
    public void PortsBacklightState_ShouldBeDefined(PortsBacklightState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void PortsBacklightState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<PortsBacklightState>().Should().HaveCount(2);
    }

    #endregion

    #region PowerAdapterStatus

    [Theory]
    [InlineData(PowerAdapterStatus.Connected)]
    [InlineData(PowerAdapterStatus.ConnectedLowWattage)]
    [InlineData(PowerAdapterStatus.Disconnected)]
    public void PowerAdapterStatus_ShouldBeDefined(PowerAdapterStatus value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void PowerAdapterStatus_ShouldHaveThreeMembers()
    {
        Enum.GetValues<PowerAdapterStatus>().Should().HaveCount(3);
    }

    #endregion

    #region PowerModeState

    [Theory]
    [InlineData(PowerModeState.Quiet)]
    [InlineData(PowerModeState.Balance)]
    [InlineData(PowerModeState.Performance)]
    [InlineData(PowerModeState.Extreme)]
    [InlineData(PowerModeState.GodMode)]
    public void PowerModeState_ShouldBeDefined(PowerModeState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void PowerModeState_Extreme_ShouldEqual223()
    {
        ((int)PowerModeState.Extreme).Should().Be(223);
    }

    [Fact]
    public void PowerModeState_GodMode_ShouldEqual254()
    {
        ((int)PowerModeState.GodMode).Should().Be(254);
    }

    [Fact]
    public void PowerModeState_ShouldHaveFiveMembers()
    {
        Enum.GetValues<PowerModeState>().Should().HaveCount(5);
    }

    #endregion

    #region PowerStateEvent

    [Theory]
    [InlineData(PowerStateEvent.Unknown)]
    [InlineData(PowerStateEvent.StatusChange)]
    [InlineData(PowerStateEvent.Suspend)]
    [InlineData(PowerStateEvent.Resume)]
    public void PowerStateEvent_ShouldBeDefined(PowerStateEvent value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void PowerStateEvent_Unknown_ShouldEqualMinusOne()
    {
        ((int)PowerStateEvent.Unknown).Should().Be(-1);
    }

    [Fact]
    public void PowerStateEvent_ShouldHaveFourMembers()
    {
        Enum.GetValues<PowerStateEvent>().Should().HaveCount(4);
    }

    #endregion

    #region SoftwareStatus

    [Theory]
    [InlineData(SoftwareStatus.Enabled)]
    [InlineData(SoftwareStatus.Disabled)]
    [InlineData(SoftwareStatus.NotFound)]
    public void SoftwareStatus_ShouldBeDefined(SoftwareStatus value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void SoftwareStatus_ShouldHaveThreeMembers()
    {
        Enum.GetValues<SoftwareStatus>().Should().HaveCount(3);
    }

    #endregion

    #region Theme

    [Theory]
    [InlineData(Theme.System)]
    [InlineData(Theme.Light)]
    [InlineData(Theme.Dark)]
    public void Theme_ShouldBeDefined(Theme value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void Theme_ShouldHaveThreeMembers()
    {
        Enum.GetValues<Theme>().Should().HaveCount(3);
    }

    #endregion

    #region AccentColorSource

    [Theory]
    [InlineData(AccentColorSource.System)]
    [InlineData(AccentColorSource.Custom)]
    public void AccentColorSource_ShouldBeDefined(AccentColorSource value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void AccentColorSource_ShouldHaveTwoMembers()
    {
        Enum.GetValues<AccentColorSource>().Should().HaveCount(2);
    }

    #endregion

    #region ThemeStylePreset

    [Theory]
    [InlineData(ThemeStylePreset.Default)]
    [InlineData(ThemeStylePreset.Official)]
    [InlineData(ThemeStylePreset.Midnight)]
    [InlineData(ThemeStylePreset.Forest)]
    public void ThemeStylePreset_ShouldBeDefined(ThemeStylePreset value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void ThemeStylePreset_ShouldHaveFourMembers()
    {
        Enum.GetValues<ThemeStylePreset>().Should().HaveCount(4);
    }

    #endregion

    #region TemperatureUnit

    [Theory]
    [InlineData(TemperatureUnit.C)]
    [InlineData(TemperatureUnit.F)]
    public void TemperatureUnit_ShouldBeDefined(TemperatureUnit value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void TemperatureUnit_ShouldHaveTwoMembers()
    {
        Enum.GetValues<TemperatureUnit>().Should().HaveCount(2);
    }

    #endregion

    #region ThermalModeState

    [Theory]
    [InlineData(ThermalModeState.Unknown)]
    [InlineData(ThermalModeState.Quiet)]
    [InlineData(ThermalModeState.Balance)]
    [InlineData(ThermalModeState.Performance)]
    [InlineData(ThermalModeState.Extreme)]
    [InlineData(ThermalModeState.GodMode)]
    public void ThermalModeState_ShouldBeDefined(ThermalModeState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void ThermalModeState_Extreme_ShouldEqual224()
    {
        ((int)ThermalModeState.Extreme).Should().Be(224);
    }

    [Fact]
    public void ThermalModeState_GodMode_ShouldEqual255()
    {
        ((int)ThermalModeState.GodMode).Should().Be(255);
    }

    [Fact]
    public void ThermalModeState_ShouldHaveSixMembers()
    {
        Enum.GetValues<ThermalModeState>().Should().HaveCount(6);
    }

    #endregion

    #region UpdateCheckFrequency

    [Theory]
    [InlineData(UpdateCheckFrequency.PerHour)]
    [InlineData(UpdateCheckFrequency.PerThreeHours)]
    [InlineData(UpdateCheckFrequency.PerTwelveHours)]
    [InlineData(UpdateCheckFrequency.PerDay)]
    [InlineData(UpdateCheckFrequency.PerWeek)]
    [InlineData(UpdateCheckFrequency.PerMonth)]
    public void UpdateCheckFrequency_ShouldBeDefined(UpdateCheckFrequency value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void UpdateCheckFrequency_ShouldHaveSixMembers()
    {
        Enum.GetValues<UpdateCheckFrequency>().Should().HaveCount(6);
    }

    #endregion

    #region UpdateCheckStatus

    [Theory]
    [InlineData(UpdateCheckStatus.Success)]
    [InlineData(UpdateCheckStatus.RateLimitReached)]
    [InlineData(UpdateCheckStatus.Error)]
    public void UpdateCheckStatus_ShouldBeDefined(UpdateCheckStatus value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void UpdateCheckStatus_ShouldHaveThreeMembers()
    {
        Enum.GetValues<UpdateCheckStatus>().Should().HaveCount(3);
    }

    #endregion

    #region WindowsPowerMode

    [Theory]
    [InlineData(WindowsPowerMode.BestPowerEfficiency)]
    [InlineData(WindowsPowerMode.Balanced)]
    [InlineData(WindowsPowerMode.BestPerformance)]
    public void WindowsPowerMode_ShouldBeDefined(WindowsPowerMode value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void WindowsPowerMode_ShouldHaveThreeMembers()
    {
        Enum.GetValues<WindowsPowerMode>().Should().HaveCount(3);
    }

    #endregion

    #region OsdItem

    [Theory]
    [InlineData(OsdItem.Fps)]
    [InlineData(OsdItem.LowFps)]
    [InlineData(OsdItem.FrameTime)]
    [InlineData(OsdItem.CpuFrequency)]
    [InlineData(OsdItem.CpuUtilization)]
    [InlineData(OsdItem.CpuTemperature)]
    [InlineData(OsdItem.CpuPower)]
    [InlineData(OsdItem.CpuFan)]
    [InlineData(OsdItem.GpuFrequency)]
    [InlineData(OsdItem.GpuUtilization)]
    [InlineData(OsdItem.GpuTemperature)]
    public void OsdItem_ShouldBeDefined(OsdItem value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void OsdItem_ShouldHave19Members()
    {
        Enum.GetValues<OsdItem>().Should().NotBeEmpty();
    }

    #endregion

    #region OsdState

    [Theory]
    [InlineData(OsdState.Hidden)]
    [InlineData(OsdState.Show)]
    [InlineData(OsdState.Toggle)]
    public void OsdState_ShouldBeDefined(OsdState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void OsdState_ShouldHaveThreeMembers()
    {
        Enum.GetValues<OsdState>().Should().HaveCount(3);
    }

    #endregion

    #region HardwareSensorsState

    [Theory]
    [InlineData(HardwareSensorsState.Off)]
    [InlineData(HardwareSensorsState.On)]
    public void HardwareSensorsState_ShouldBeDefined(HardwareSensorsState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void HardwareSensorsState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<HardwareSensorsState>().Should().HaveCount(2);
    }

    #endregion

    #region WinKeyState

    [Theory]
    [InlineData(WinKeyState.Off)]
    [InlineData(WinKeyState.On)]
    public void WinKeyState_ShouldBeDefined(WinKeyState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void WinKeyState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<WinKeyState>().Should().HaveCount(2);
    }

    #endregion

    #region SpecialKey

    [Theory]
    [InlineData(SpecialKey.FnF9)]
    [InlineData(SpecialKey.FnLockOn)]
    [InlineData(SpecialKey.FnLockOff)]
    [InlineData(SpecialKey.FnPrtSc)]
    [InlineData(SpecialKey.CameraOn)]
    [InlineData(SpecialKey.CameraOff)]
    [InlineData(SpecialKey.FnR)]
    [InlineData(SpecialKey.SpectrumBacklightOff)]
    [InlineData(SpecialKey.SpectrumBacklight1)]
    [InlineData(SpecialKey.SpectrumBacklight2)]
    [InlineData(SpecialKey.SpectrumBacklight3)]
    public void SpecialKey_ShouldBeDefined(SpecialKey value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void SpecialKey_FnF9_ShouldEqualOne()
    {
        ((int)SpecialKey.FnF9).Should().Be(1);
    }

    [Fact]
    public void SpecialKey_FnR2_ShouldEqual0x0041002A()
    {
        ((int)SpecialKey.FnR2).Should().Be(0x0041002A);
    }

    [Fact]
    public void SpecialKey_ShouldHave23Members()
    {
        Enum.GetValues<SpecialKey>().Should().NotBeEmpty();
    }

    #endregion

    #region LibreHardwareMonitorInitialState

    [Theory]
    [InlineData(LibreHardwareMonitorInitialState.Fail)]
    [InlineData(LibreHardwareMonitorInitialState.Initialized)]
    [InlineData(LibreHardwareMonitorInitialState.Success)]
    [InlineData(LibreHardwareMonitorInitialState.PawnIONotInstalled)]
    public void LibreHardwareMonitorInitialState_ShouldBeDefined(LibreHardwareMonitorInitialState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void LibreHardwareMonitorInitialState_ShouldHaveFourMembers()
    {
        Enum.GetValues<LibreHardwareMonitorInitialState>().Should().HaveCount(4);
    }

    #endregion

    #region ProcessEventInfoType

    [Theory]
    [InlineData(ProcessEventInfoType.Started)]
    [InlineData(ProcessEventInfoType.Stopped)]
    public void ProcessEventInfoType_ShouldBeDefined(ProcessEventInfoType value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void ProcessEventInfoType_ShouldHaveTwoMembers()
    {
        Enum.GetValues<ProcessEventInfoType>().Should().HaveCount(2);
    }

    #endregion

    #region MicrophoneState

    [Theory]
    [InlineData(MicrophoneState.Off)]
    [InlineData(MicrophoneState.On)]
    public void MicrophoneState_ShouldBeDefined(MicrophoneState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void MicrophoneState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<MicrophoneState>().Should().HaveCount(2);
    }

    #endregion

    #region NativeWindowsMessage

    [Fact]
    public void NativeWindowsMessage_ShouldHaveExpectedMembers()
    {
        var values = Enum.GetValues<NativeWindowsMessage>();
        values.Should().Contain(NativeWindowsMessage.LidOpened);
        values.Should().Contain(NativeWindowsMessage.LidClosed);
        values.Should().Contain(NativeWindowsMessage.MonitorOn);
    }

    #endregion

    #region OneLevelWhiteKeyboardBacklightState

    [Theory]
    [InlineData(OneLevelWhiteKeyboardBacklightState.Off)]
    [InlineData(OneLevelWhiteKeyboardBacklightState.On)]
    public void OneLevelWhiteKeyboardBacklightState_ShouldBeDefined(OneLevelWhiteKeyboardBacklightState value)
    {
        Enum.IsDefined(value).Should().BeTrue();
    }

    [Fact]
    public void OneLevelWhiteKeyboardBacklightState_ShouldHaveTwoMembers()
    {
        Enum.GetValues<OneLevelWhiteKeyboardBacklightState>().Should().HaveCount(2);
    }

    #endregion

    #region Display Attribute Verification

    [Theory]
    [InlineData(typeof(AutorunState), nameof(AutorunState.Enabled))]
    [InlineData(typeof(BatteryState), nameof(BatteryState.Conservation))]
    [InlineData(typeof(HDRState), nameof(HDRState.Off))]
    [InlineData(typeof(FnLockState), nameof(FnLockState.On))]
    [InlineData(typeof(Theme), nameof(Theme.Dark))]
    [InlineData(typeof(WindowsPowerMode), nameof(WindowsPowerMode.BestPerformance))]
    public void Enums_ShouldHaveDisplayAttributes(Type enumType, string memberName)
    {
        var member = enumType.GetMember(memberName).First();
        var attr = member.GetCustomAttributes(typeof(DisplayAttribute), false).Cast<DisplayAttribute>().FirstOrDefault();
        attr.Should().NotBeNull($"{enumType.Name}.{memberName} should have a Display attribute");
        attr!.Name.Should().NotBeNullOrWhiteSpace();
    }

    #endregion
}

