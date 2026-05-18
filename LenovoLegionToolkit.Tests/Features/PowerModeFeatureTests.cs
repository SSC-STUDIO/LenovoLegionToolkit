using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;
using Moq;

namespace LenovoLegionToolkit.Tests.Features;

[Trait("Category", TestCategories.Controller)]
public class PowerModeFeatureTests : UnitTestBase
{
    [Fact]
    public void PowerModeState_ShouldIncludeExtremeAndGodMode()
    {
        var states = Enum.GetValues<PowerModeState>();

        states.Should().HaveCount(5);
        states.Should().Contain(PowerModeState.Quiet);
        states.Should().Contain(PowerModeState.Balance);
        states.Should().Contain(PowerModeState.Performance);
        states.Should().Contain(PowerModeState.Extreme);
        states.Should().Contain(PowerModeState.GodMode);
    }

    [Fact]
    public void PowerModeState_Values_ShouldBeCorrect()
    {
        ((int)PowerModeState.Quiet).Should().Be(0);
        ((int)PowerModeState.Balance).Should().Be(1);
        ((int)PowerModeState.Performance).Should().Be(2);
        ((int)PowerModeState.GodMode).Should().Be(254);
    }

    [Fact]
    public void PowerModeUnavailableWithoutACException_ShouldContainPowerMode()
    {
        var exception = new PowerModeUnavailableWithoutACException(PowerModeState.Performance);

        exception.PowerMode.Should().Be(PowerModeState.Performance);
        exception.Message.Should().Contain("Performance");
    }

    [Fact]
    public async Task IsSupportedAsync_WhenResolvedAsInterface_ShouldUsePowerModeStateAvailability()
    {
        ResetCompatibilityCache();

        try
        {
            Environment.SetEnvironmentVariable(Compatibility.SmokeSimulateLegionEnvironmentVariable, "1");

            var feature = (IFeature<PowerModeState>)RuntimeHelpers.GetUninitializedObject(typeof(PowerModeFeature));

            var result = await feature.IsSupportedAsync();

            result.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Compatibility.SmokeSimulateLegionEnvironmentVariable, null);
            ResetCompatibilityCache();
        }
    }

    [Fact]
    public async Task GetAllStatesAsync_WhenMachineReportsExtreme_ShouldIncludeExtreme()
    {
        ResetCompatibilityCache();

        try
        {
            Environment.SetEnvironmentVariable(Compatibility.SmokeSimulateLegionEnvironmentVariable, "1");

            var feature = new PowerModeFeature(
                null!,
                null!,
                null!,
                null!,
                null!);

            var states = await feature.GetAllStatesAsync();

            states.Should().Contain(PowerModeState.Extreme);
        }
        finally
        {
            Environment.SetEnvironmentVariable(Compatibility.SmokeSimulateLegionEnvironmentVariable, null);
            ResetCompatibilityCache();
        }
    }

    [Fact]
    public async Task GetAllStatesAsync_WhenSupportedPowerModesIsSparse_ShouldStillIncludeStandardModes()
    {
        ResetCompatibilityCache();

        try
        {
            typeof(Compatibility).GetField("_machineInformation", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, new MachineInformation
            {
                Vendor = "LENOVO",
                MachineType = "83DE",
                Model = "Legion Pro 7 16IRX9",
                SerialNumber = "TEST",
                SupportedPowerModes = [PowerModeState.Balance],
                Features = MachineInformation.FeatureData.Unknown,
                Properties = new MachineInformation.PropertyData()
            });

            var feature = new PowerModeFeature(
                null!,
                null!,
                null!,
                null!,
                null!);

            var states = await feature.GetAllStatesAsync();

            states.Should().Contain([PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance]);
        }
        finally
        {
            ResetCompatibilityCache();
        }
    }

    [Fact]
    public async Task GetAllStatesAsync_WhenMachinePropertiesSupportExtraModes_ShouldIncludeExtremeAndGodMode()
    {
        ResetCompatibilityCache();

        try
        {
            typeof(Compatibility).GetField("_machineInformation", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, new MachineInformation
            {
                Vendor = "LENOVO",
                MachineType = "83DE",
                Model = "Legion Pro 7 16IRX9",
                SerialNumber = "TEST",
                SupportedPowerModes = [],
                Features = MachineInformation.FeatureData.Unknown,
                Properties = new MachineInformation.PropertyData
                {
                    SupportsExtremeMode = true,
                    SupportsGodModeV2 = true
                }
            });

            var feature = new PowerModeFeature(
                null!,
                null!,
                null!,
                null!,
                null!);

            var states = await feature.GetAllStatesAsync();

            states.Should().Contain([PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance, PowerModeState.Extreme, PowerModeState.GodMode]);
        }
        finally
        {
            ResetCompatibilityCache();
        }
    }

    [Fact]
    public async Task GetAllStatesAsync_WhenSupportedPowerModesIsNull_ShouldStillReturnStandardModes()
    {
        ResetCompatibilityCache();

        try
        {
            typeof(Compatibility).GetField("_machineInformation", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, new MachineInformation
            {
                Vendor = "LENOVO",
                MachineType = "83DE",
                Model = "Legion Pro 7 16IRX9",
                SerialNumber = "TEST",
                SupportedPowerModes = null!,
                Features = MachineInformation.FeatureData.Unknown,
                Properties = new MachineInformation.PropertyData()
            });

            var feature = new PowerModeFeature(
                null!,
                null!,
                null!,
                null!,
                null!);

            var states = await feature.GetAllStatesAsync();

            states.Should().Contain([PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance]);
        }
        finally
        {
            ResetCompatibilityCache();
        }
    }

    [Fact]
    public async Task GetStateAsync_WhenRuntimeReadFails_ShouldFallBackToBalance()
    {
        ResetCompatibilityCache();

        try
        {
            typeof(Compatibility).GetField("_machineInformation", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, new MachineInformation
            {
                Vendor = "LENOVO",
                MachineType = "83DF",
                Model = "Legion Y9000P IRX9",
                SerialNumber = "TEST",
                SupportedPowerModes = [],
                Features = MachineInformation.FeatureData.Unknown,
                Properties = new MachineInformation.PropertyData()
            });

            var feature = new TestPowerModeFeature(() => throw new InvalidOperationException("WMI access denied"));

            var state = await feature.GetStateAsync();

            state.Should().Be(PowerModeState.Balance);
        }
        finally
        {
            ResetCompatibilityCache();
        }
    }

    [Fact]
    public async Task GetStateAsync_WhenRuntimeReadFailsAfterSuccess_ShouldUseLastKnownState()
    {
        ResetCompatibilityCache();

        try
        {
            typeof(Compatibility).GetField("_machineInformation", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, new MachineInformation
            {
                Vendor = "LENOVO",
                MachineType = "83DF",
                Model = "Legion Y9000P IRX9",
                SerialNumber = "TEST",
                SupportedPowerModes = [],
                Features = MachineInformation.FeatureData.Unknown,
                Properties = new MachineInformation.PropertyData()
            });

            var invocationCount = 0;
            var feature = new TestPowerModeFeature(() =>
            {
                invocationCount++;
                return invocationCount switch
                {
                    1 => Task.FromResult(PowerModeState.Performance),
                    _ => throw new InvalidOperationException("WMI access denied"),
                };
            });

            var firstState = await feature.GetStateAsync();
            var secondState = await feature.GetStateAsync();

            firstState.Should().Be(PowerModeState.Performance);
            secondState.Should().Be(PowerModeState.Performance);
        }
        finally
        {
            ResetCompatibilityCache();
        }
    }

    private static void ResetCompatibilityCache()
    {
        typeof(Compatibility).GetField("_machineInformation", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, null);
        typeof(Compatibility).GetField("_isCompatible", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, null);
    }

    private sealed class TestPowerModeFeature(Func<Task<PowerModeState>> readState) : PowerModeFeature(
        null!,
        null!,
        null!,
        null!,
        null!)
    {
        internal override Task<PowerModeState> ReadStateCoreAsync() => readState();
    }
}


[Trait("Category", TestCategories.Controller)]
public class BatteryFeatureTests : UnitTestBase
{
    [Fact]
    public void BatteryState_ShouldHaveThreeStates()
    {
        var states = Enum.GetValues<BatteryState>();

        states.Should().HaveCount(3);
        states.Should().Contain(BatteryState.Conservation);
        states.Should().Contain(BatteryState.Normal);
        states.Should().Contain(BatteryState.RapidCharge);
    }

    [Fact]
    public void BatteryState_Values_ShouldBeCorrect()
    {
        ((int)BatteryState.Conservation).Should().Be(0);
        ((int)BatteryState.Normal).Should().Be(1);
        ((int)BatteryState.RapidCharge).Should().Be(2);
    }
}


[Trait("Category", TestCategories.Controller)]
public class IFeatureInterfaceTests : UnitTestBase
{
    [Fact]
    public void IFeature_ShouldHaveCorrectMethods()
    {
        var methodNames = new[]
        {
            nameof(IFeature<BatteryState>.IsSupportedAsync),
            nameof(IFeature<BatteryState>.GetAllStatesAsync),
            nameof(IFeature<BatteryState>.GetStateAsync),
            nameof(IFeature<BatteryState>.SetStateAsync)
        };

        foreach (var methodName in methodNames)
        {
            typeof(IFeature<BatteryState>).GetMethod(methodName).Should().NotBeNull();
        }
    }

    [Fact]
    public async Task IFeature_MockImplementation_ShouldWorkCorrectly()
    {
        var mockFeature = new Mock<IFeature<PowerModeState>>();

        mockFeature
            .Setup(f => f.IsSupportedAsync())
            .ReturnsAsync(true);

        mockFeature
            .Setup(f => f.GetAllStatesAsync())
            .ReturnsAsync(new[] { PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance });

        mockFeature
            .Setup(f => f.GetStateAsync())
            .ReturnsAsync(PowerModeState.Balance);

        var isSupported = await mockFeature.Object.IsSupportedAsync();
        var states = await mockFeature.Object.GetAllStatesAsync();
        var currentState = await mockFeature.Object.GetStateAsync();

        isSupported.Should().BeTrue();
        states.Should().HaveCount(3);
        currentState.Should().Be(PowerModeState.Balance);
    }
}


[Trait("Category", TestCategories.Controller)]
public class PowerModeStateExtensionsTests : UnitTestBase
{
    [Fact]
    public void Next_ShouldReturnCorrectNextState()
    {
        PowerModeState.Quiet.Next().Should().Be(PowerModeState.Balance);
        PowerModeState.Balance.Next().Should().Be(PowerModeState.Performance);
        PowerModeState.Performance.Next().Should().Be(PowerModeState.GodMode);
        PowerModeState.GodMode.Next().Should().Be(PowerModeState.Quiet);
    }

    [Fact]
    public void Previous_ShouldReturnCorrectPreviousState()
    {
        PowerModeState.Quiet.Previous().Should().Be(PowerModeState.GodMode);
        PowerModeState.Balance.Previous().Should().Be(PowerModeState.Quiet);
        PowerModeState.Performance.Previous().Should().Be(PowerModeState.Balance);
        PowerModeState.GodMode.Previous().Should().Be(PowerModeState.Performance);
    }
}

public static class PowerModeStateExtensions
{
    public static PowerModeState Next(this PowerModeState state)
    {
        return state switch
        {
            PowerModeState.Quiet => PowerModeState.Balance,
            PowerModeState.Balance => PowerModeState.Performance,
            PowerModeState.Performance => PowerModeState.GodMode,
            PowerModeState.GodMode => PowerModeState.Quiet,
            _ => PowerModeState.Balance
        };
    }

    public static PowerModeState Previous(this PowerModeState state)
    {
        return state switch
        {
            PowerModeState.Quiet => PowerModeState.GodMode,
            PowerModeState.Balance => PowerModeState.Quiet,
            PowerModeState.Performance => PowerModeState.Balance,
            PowerModeState.GodMode => PowerModeState.Performance,
            _ => PowerModeState.Balance
        };
    }
}
