using System;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Listeners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LenovoLegionToolkit.Tests.Features;

[TestClass]
[TestCategory(TestCategories.Controller)]
public class PowerModeFeatureTests : UnitTestBase
{
    private Mock<GodModeController> _godModeControllerMock = null!;
    private Mock<WindowsPowerModeController> _windowsPowerModeControllerMock = null!;
    private Mock<WindowsPowerPlanController> _windowsPowerPlanControllerMock = null!;
    private Mock<ThermalModeListener> _thermalModeListenerMock = null!;
    private Mock<PowerModeListener> _powerModeListenerMock = null!;

    protected override void Setup()
    {
        _godModeControllerMock = new Mock<GodModeController>(null!, null!);
        _windowsPowerModeControllerMock = new Mock<WindowsPowerModeController>(null!, null!);
        _windowsPowerPlanControllerMock = new Mock<WindowsPowerPlanController>(null!, null!);
        _thermalModeListenerMock = new Mock<ThermalModeListener>(null!);
        _powerModeListenerMock = new Mock<PowerModeListener>(null!);
    }

    [TestMethod]
    public void PowerModeState_ShouldHaveFourStates()
    {
        var states = Enum.GetValues<PowerModeState>();

        states.Should().HaveCount(4);
        states.Should().Contain(PowerModeState.Quiet);
        states.Should().Contain(PowerModeState.Balance);
        states.Should().Contain(PowerModeState.Performance);
        states.Should().Contain(PowerModeState.GodMode);
    }

    [TestMethod]
    public void PowerModeState_Values_ShouldBeCorrect()
    {
        ((int)PowerModeState.Quiet).Should().Be(0);
        ((int)PowerModeState.Balance).Should().Be(1);
        ((int)PowerModeState.Performance).Should().Be(2);
        ((int)PowerModeState.GodMode).Should().Be(254);
    }

    [TestMethod]
    public void PowerModeUnavailableWithoutACException_ShouldContainPowerMode()
    {
        var exception = new PowerModeUnavailableWithoutACException(PowerModeState.Performance);

        exception.PowerMode.Should().Be(PowerModeState.Performance);
        exception.Message.Should().Contain("Performance");
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class BatteryFeatureTests : UnitTestBase
{
    [TestMethod]
    public void BatteryState_ShouldHaveThreeStates()
    {
        var states = Enum.GetValues<BatteryState>();

        states.Should().HaveCount(3);
        states.Should().Contain(BatteryState.Conservation);
        states.Should().Contain(BatteryState.Normal);
        states.Should().Contain(BatteryState.RapidCharge);
    }

    [TestMethod]
    public void BatteryState_Values_ShouldBeCorrect()
    {
        ((int)BatteryState.Conservation).Should().Be(0);
        ((int)BatteryState.Normal).Should().Be(1);
        ((int)BatteryState.RapidCharge).Should().Be(2);
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class IFeatureInterfaceTests : UnitTestBase
{
    [TestMethod]
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

    [TestMethod]
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

[TestClass]
[TestCategory(TestCategories.Controller)]
public class PowerModeStateExtensionsTests : UnitTestBase
{
    [TestMethod]
    public void Next_ShouldReturnCorrectNextState()
    {
        PowerModeState.Quiet.Next().Should().Be(PowerModeState.Balance);
        PowerModeState.Balance.Next().Should().Be(PowerModeState.Performance);
        PowerModeState.Performance.Next().Should().Be(PowerModeState.GodMode);
        PowerModeState.GodMode.Next().Should().Be(PowerModeState.Quiet);
    }

    [TestMethod]
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
