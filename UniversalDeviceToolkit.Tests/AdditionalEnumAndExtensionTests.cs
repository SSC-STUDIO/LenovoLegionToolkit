using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class AdditionalEnumAndExtensionTests
{
    #region NativeWindowsMessage Enum Tests

    [Fact]
    public void NativeWindowsMessage_ShouldHaveTwelveValues()
    {
        Enum.GetValues<NativeWindowsMessage>().Should().HaveCount(12);
    }

    [Fact]
    public void NativeWindowsMessage_LidOpened_ShouldBeZero()
    {
        ((int)NativeWindowsMessage.LidOpened).Should().Be(0);
    }

    [Fact]
    public void NativeWindowsMessage_BatterySaverEnabled_ShouldBeLast()
    {
        var values = Enum.GetValues<NativeWindowsMessage>();
        values.Last().Should().Be(NativeWindowsMessage.BatterySaverEnabled);
    }

    #endregion

    #region FlipToStartState Enum Tests

    [Theory]
    [InlineData(FlipToStartState.Off, 0)]
    [InlineData(FlipToStartState.On, 1)]
    public void FlipToStartState_ShouldHaveExpectedValues(FlipToStartState state, int expectedValue)
    {
        ((int)state).Should().Be(expectedValue);
    }

    #endregion

    #region FnLockState Enum Tests

    [Theory]
    [InlineData(FnLockState.Off, 0)]
    [InlineData(FnLockState.On, 1)]
    public void FnLockState_ShouldHaveExpectedValues(FnLockState state, int expectedValue)
    {
        ((int)state).Should().Be(expectedValue);
    }

    #endregion

    #region MicrophoneState Enum Tests

    [Theory]
    [InlineData(MicrophoneState.Off, 0)]
    [InlineData(MicrophoneState.On, 1)]
    public void MicrophoneState_ShouldHaveExpectedValues(MicrophoneState state, int expectedValue)
    {
        ((int)state).Should().Be(expectedValue);
    }

    #endregion

    #region SpeakerState Enum Tests

    [Theory]
    [InlineData(SpeakerState.Mute, 0)]
    [InlineData(SpeakerState.Unmute, 1)]
    public void SpeakerState_ShouldHaveExpectedValues(SpeakerState state, int expectedValue)
    {
        ((int)state).Should().Be(expectedValue);
    }

    #endregion

    #region TouchpadLockState Enum Tests

    [Theory]
    [InlineData(TouchpadLockState.Off, 0)]
    [InlineData(TouchpadLockState.On, 1)]
    public void TouchpadLockState_ShouldHaveExpectedValues(TouchpadLockState state, int expectedValue)
    {
        ((int)state).Should().Be(expectedValue);
    }

    #endregion

    #region KeyboardLayout Enum Tests

    [Fact]
    public void KeyboardLayout_ShouldHaveFourValues()
    {
        Enum.GetValues<KeyboardLayout>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(KeyboardLayout.Ansi, 0)]
    [InlineData(KeyboardLayout.Iso, 1)]
    [InlineData(KeyboardLayout.Jis, 2)]
    [InlineData(KeyboardLayout.Keyboard24Zone, 3)]
    public void KeyboardLayout_ShouldHaveExpectedValues(KeyboardLayout layout, int expectedValue)
    {
        ((int)layout).Should().Be(expectedValue);
    }

    #endregion

    #region WindowsPowerMode Enum Tests

    [Fact]
    public void WindowsPowerMode_ShouldHaveThreeValues()
    {
        Enum.GetValues<WindowsPowerMode>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(WindowsPowerMode.BestPowerEfficiency, 0)]
    [InlineData(WindowsPowerMode.Balanced, 1)]
    [InlineData(WindowsPowerMode.BestPerformance, 2)]
    public void WindowsPowerMode_ShouldHaveExpectedValues(WindowsPowerMode mode, int expectedValue)
    {
        ((int)mode).Should().Be(expectedValue);
    }

    #endregion

    #region OS Enum Tests

    [Fact]
    public void OS_ShouldHaveFourValues()
    {
        Enum.GetValues<OS>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(OS.Windows11, 0)]
    [InlineData(OS.Windows10, 1)]
    [InlineData(OS.Windows8, 2)]
    [InlineData(OS.Windows7, 3)]
    public void OS_ShouldHaveExpectedValues(OS os, int expectedValue)
    {
        ((int)os).Should().Be(expectedValue);
    }

    #endregion

    #region KnownFolder Enum Tests

    [Fact]
    public void KnownFolder_ShouldHaveSixValues()
    {
        Enum.GetValues<KnownFolder>().Should().HaveCount(6);
    }

    [Fact]
    public void KnownFolder_Contacts_ShouldBeZero()
    {
        ((int)KnownFolder.Contacts).Should().Be(0);
    }

    [Fact]
    public void KnownFolder_SavedSearches_ShouldBeLast()
    {
        var values = Enum.GetValues<KnownFolder>();
        values.Last().Should().Be(KnownFolder.SavedSearches);
    }

    #endregion

    #region UpdateCheckFrequency Enum Tests

    [Fact]
    public void UpdateCheckFrequency_ShouldHaveFourValues()
    {
        Enum.GetValues<UpdateCheckFrequency>().Should().HaveCount(6);
    }

    [Fact]
    public void UpdateCheckFrequency_PerHour_ShouldBeZero()
    {
        ((int)UpdateCheckFrequency.PerHour).Should().Be(0);
    }

    #endregion

    #region UpdateCheckStatus Enum Tests

    [Fact]
    public void UpdateCheckStatus_ShouldHaveThreeValues()
    {
        Enum.GetValues<UpdateCheckStatus>().Should().HaveCount(3);
    }

    [Fact]
    public void UpdateCheckStatus_Success_ShouldBeFirstValue()
    {
        ((int)UpdateCheckStatus.Success).Should().Be(0);
    }

    #endregion

    #region PowerModeState Enum Tests

    [Theory]
    [InlineData(PowerModeState.Quiet, 0)]
    [InlineData(PowerModeState.Balance, 1)]
    [InlineData(PowerModeState.Performance, 2)]
    [InlineData(PowerModeState.Extreme, 223)]
    [InlineData(PowerModeState.GodMode, 254)]
    public void PowerModeState_ShouldHaveExpectedValues(PowerModeState state, int expectedValue)
    {
        ((int)state).Should().Be(expectedValue);
    }

    #endregion

    #region ThermalModeState Enum Tests

    [Theory]
    [InlineData(ThermalModeState.Unknown, 0)]
    [InlineData(ThermalModeState.Quiet, 1)]
    [InlineData(ThermalModeState.Balance, 2)]
    [InlineData(ThermalModeState.Performance, 3)]
    [InlineData(ThermalModeState.Extreme, 224)]
    [InlineData(ThermalModeState.GodMode, 255)]
    public void ThermalModeState_ShouldHaveExpectedValues(ThermalModeState state, int expectedValue)
    {
        ((int)state).Should().Be(expectedValue);
    }

    #endregion

    #region FanCurveEntry ImportFromJson Partial Data Tests

    [Fact]
    public void FanCurveEntry_ImportFromJson_WithOnlyType_ShouldPreserveDefaults()
    {
        var json = "{\"Type\":1}";
        var imported = FanCurveEntry.ImportFromJson(json);
        imported.Type.Should().Be(FanType.Gpu);
        imported.CriticalTemp.Should().Be(90);
        imported.MaxPwm.Should().Be(255.0);
    }

    [Fact]
    public void FanCurveEntry_ImportFromJson_WithOnlyCriticalTemp_ShouldPreserveOtherDefaults()
    {
        var json = "{\"CriticalTemp\":75}";
        var imported = FanCurveEntry.ImportFromJson(json);
        imported.CriticalTemp.Should().Be(75);
        imported.Type.Should().Be(FanType.Cpu);
        imported.MaxPwm.Should().Be(255.0);
    }

    [Fact]
    public void FanCurveEntry_ImportFromJson_WithOnlyMaxPwm_ShouldPreserveOtherDefaults()
    {
        var json = "{\"MaxPwm\":200.0}";
        var imported = FanCurveEntry.ImportFromJson(json);
        imported.MaxPwm.Should().Be(200.0);
        imported.Type.Should().Be(FanType.Cpu);
        imported.CriticalTemp.Should().Be(90);
    }

    [Fact]
    public void FanCurveEntry_ImportFromJson_WithCurveNodesOnly_ShouldReplaceDefaults()
    {
        var json = "{\"CurveNodes\":[{\"Temperature\":35.0,\"TargetPercent\":10},{\"Temperature\":70.0,\"TargetPercent\":60}]}";
        var imported = FanCurveEntry.ImportFromJson(json);
        imported.CurveNodes.Should().HaveCount(2);
        imported.CurveNodes[0].Temperature.Should().Be(35.0f);
        imported.CurveNodes[0].TargetPercent.Should().Be(10);
        imported.CurveNodes[1].Temperature.Should().Be(70.0f);
        imported.CurveNodes[1].TargetPercent.Should().Be(60);
    }

    #endregion

    #region CurveNode Additional Edge Cases

    [Fact]
    public void CurveNode_SetTemperatureAndTargetPercent_ShouldUpdateBoth()
    {
        var node = new CurveNode();
        node.Temperature = 50.0f;
        node.TargetPercent = 30;
        node.Temperature.Should().Be(50.0f);
        node.TargetPercent.Should().Be(30);
    }

    [Fact]
    public void CurveNode_TargetPercent_SameValue_ShouldNotFirePropertyChanged()
    {
        var node = new CurveNode { TargetPercent = 42 };
        var fired = false;
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CurveNode.TargetPercent))
                fired = true;
        };
        node.TargetPercent = 42;
        fired.Should().BeFalse();
    }

    [Fact]
    public void CurveNode_Temperature_NegativeValue_ShouldBeAccepted()
    {
        var node = new CurveNode();
        node.Temperature = -10.5f;
        node.Temperature.Should().Be(-10.5f);
    }

    [Fact]
    public void CurveNode_TargetPercent_NegativeValue_ShouldBeAccepted()
    {
        var node = new CurveNode();
        node.TargetPercent = -5;
        node.TargetPercent.Should().Be(-5);
    }

    [Fact]
    public void CurveNode_TargetPercent_LargeValue_ShouldBeAccepted()
    {
        var node = new CurveNode();
        node.TargetPercent = int.MaxValue;
        node.TargetPercent.Should().Be(int.MaxValue);
    }

    [Fact]
    public void CurveNode_Temperature_LargeValue_ShouldBeAccepted()
    {
        var node = new CurveNode();
        node.Temperature = float.MaxValue;
        node.Temperature.Should().Be(float.MaxValue);
    }

    #endregion

    #region EnumerableExtensions ForEach Additional Edge Cases

    [Fact]
    public void ForEach_ShouldExecuteInOrder()
    {
        var list = new List<int> { 1, 2, 3 };
        var results = new List<int>();
        list.ForEach(item => results.Add(item));
        results.Should().BeEquivalentTo(new[] { 1, 2, 3 }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void ForEach_SingleElement_ShouldExecuteOnce()
    {
        var list = new List<int> { 42 };
        var count = 0;
        list.ForEach(_ => count++);
        count.Should().Be(1);
    }

    [Fact]
    public void ForEach_WithEmptyList_ShouldNotExecute()
    {
        var list = new List<int>();
        var count = 0;
        list.ForEach(_ => count++);
        count.Should().Be(0);
    }

    #endregion

    #region DictionaryExtensions Additional Edge Cases

    [Fact]
    public void AddRange_MultipleItems_ShouldAddAll()
    {
        var dict = new Dictionary<int, string>();
        var items = new Dictionary<int, string> { [1] = "a", [2] = "b", [3] = "c" };
        dict.AddRange(items);
        dict.Should().HaveCount(3);
    }

    [Fact]
    public void AsReadOnlyDictionary_WrappedDictionary_ShouldBeReadOnly()
    {
        var dict = new Dictionary<string, int> { ["x"] = 1 };
        var ro = dict.AsReadOnlyDictionary();
        var act = () => ((ICollection<KeyValuePair<string, int>>)ro).Add(new KeyValuePair<string, int>("y", 2));
        act.Should().Throw<NotSupportedException>();
    }

    #endregion

    #region ListExtensions Additional Edge Cases

    [Fact]
    public void ToArray_WithSingleElement_ShouldReturnSingleElementArray()
    {
        IList list = new ArrayList { 99 };
        var result = list.ToArray();
        result.Should().HaveCount(1);
        result[0].Should().Be(99);
    }

    [Fact]
    public void ToArray_WithNullElements_ShouldPreserveNulls()
    {
        IList list = new ArrayList { null, "test", null };
        var result = list.ToArray();
        result.Should().HaveCount(3);
        result[0].Should().BeNull();
        result[1].Should().Be("test");
        result[2].Should().BeNull();
    }

    #endregion

    #region PInvokeExtensions ThrowIfWin32Error Zero Code

    [Fact]
    public void ThrowIfWin32Error_ZeroCode_ShouldThrowGenericException()
    {
        var act = () => PInvokeExtensions.ThrowIfWin32Error(0, "test operation");
        act.Should().Throw<Exception>()
            .WithMessage("*failed but Win32 didn't catch an error*");
    }

    #endregion

    #region CapabilityID Hex Values

    [Theory]
    [InlineData(CapabilityID.IGPUMode, 0x00010000)]
    [InlineData(CapabilityID.FlipToStart, 0x00030000)]
    [InlineData(CapabilityID.NvidiaGPUDynamicDisplaySwitching, 0x00040000)]
    [InlineData(CapabilityID.AMDSmartShiftMode, 0x00050001)]
    [InlineData(CapabilityID.OverDrive, 0x001A0000)]
    [InlineData(CapabilityID.AIChip, 0x000E0000)]
    [InlineData(CapabilityID.CPUShortTermPowerLimit, 0x0101FF00)]
    [InlineData(CapabilityID.CPULongTermPowerLimit, 0x0102FF00)]
    public void CapabilityID_ShouldHaveExpectedHexValues(CapabilityID id, uint expectedValue)
    {
        ((uint)id).Should().Be(expectedValue);
    }

    #endregion

    #region LogoInfoFormat Extension Methods

    [Fact]
    public void ExtensionFilters_WithNoFlags_ShouldReturnEmpty()
    {
        BootLogoFormat format = 0;
        format.ExtensionFilters().Should().BeEmpty();
    }

    [Fact]
    public void ImageFormats_WithNoFlags_ShouldReturnEmpty()
    {
        BootLogoFormat format = 0;
        format.ImageFormats().Should().BeEmpty();
    }

    #endregion
}


