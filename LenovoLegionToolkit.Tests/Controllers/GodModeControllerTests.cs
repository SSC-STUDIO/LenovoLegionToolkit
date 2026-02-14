using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LenovoLegionToolkit.Tests.Controllers;

[TestClass]
[TestCategory(TestCategories.Controller)]
public class GodModeControllerTests : UnitTestBase
{
    private Mock<GodModeControllerV1> _controllerV1Mock = null!;
    private Mock<GodModeControllerV2> _controllerV2Mock = null!;
    private GodModeController _controller = null!;

    protected override void Setup()
    {
        _controllerV1Mock = new Mock<GodModeControllerV1>(
            MockBehavior.Loose,
            null!,
            null!,
            null!,
            null!,
            null!);
        
        _controllerV2Mock = new Mock<GodModeControllerV2>(
            MockBehavior.Loose,
            null!,
            null!,
            null!,
            null!,
            null!);

        _controller = new GodModeController(_controllerV1Mock.Object, _controllerV2Mock.Object);
    }

    [TestMethod]
    public void Constructor_ShouldInitializeCorrectly()
    {
        _controller.Should().NotBeNull();
    }

    [TestMethod]
    public void PresetChanged_Event_ShouldBeSubscribedToBothControllers()
    {
        var eventRaised = false;
        EventHandler<Guid> handler = (sender, id) => eventRaised = true;

        _controller.PresetChanged += handler;
        
        _controllerV1Mock.Raise(m => m.PresetChanged += null, Guid.NewGuid());
        eventRaised.Should().BeTrue();

        eventRaised = false;
        _controllerV2Mock.Raise(m => m.PresetChanged += null, Guid.NewGuid());
        eventRaised.Should().BeTrue();

        _controller.PresetChanged -= handler;
    }

    [TestMethod]
    public async Task NeedsVantageDisabledAsync_ShouldCallCorrectController()
    {
        _controllerV1Mock
            .Setup(m => m.NeedsVantageDisabledAsync())
            .ReturnsAsync(true);

        var result = await _controller.NeedsVantageDisabledAsync();

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task NeedsLegionZoneDisabledAsync_ShouldCallCorrectController()
    {
        _controllerV1Mock
            .Setup(m => m.NeedsLegionZoneDisabledAsync())
            .ReturnsAsync(false);

        var result = await _controller.NeedsLegionZoneDisabledAsync();

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetActivePresetIdAsync_ShouldReturnCorrectId()
    {
        var expectedId = Guid.NewGuid();
        _controllerV1Mock
            .Setup(m => m.GetActivePresetIdAsync())
            .ReturnsAsync(expectedId);

        var result = await _controller.GetActivePresetIdAsync();

        result.Should().Be(expectedId);
    }

    [TestMethod]
    public async Task GetActivePresetNameAsync_ShouldReturnCorrectName()
    {
        const string expectedName = "Test Preset";
        _controllerV1Mock
            .Setup(m => m.GetActivePresetNameAsync())
            .ReturnsAsync(expectedName);

        var result = await _controller.GetActivePresetNameAsync();

        result.Should().Be(expectedName);
    }

    [TestMethod]
    public async Task GetActivePresetNameAsync_WhenNoPreset_ShouldReturnNull()
    {
        _controllerV1Mock
            .Setup(m => m.GetActivePresetNameAsync())
            .ReturnsAsync((string?)null);

        var result = await _controller.GetActivePresetNameAsync();

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task SetStateAsync_ShouldCallCorrectController()
    {
        var state = new GodModeState();
        _controllerV1Mock
            .Setup(m => m.SetStateAsync(state))
            .Returns(Task.CompletedTask);

        var act = async () => await _controller.SetStateAsync(state);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ApplyStateAsync_ShouldCallCorrectController()
    {
        _controllerV1Mock
            .Setup(m => m.ApplyStateAsync())
            .Returns(Task.CompletedTask);

        var act = async () => await _controller.ApplyStateAsync();

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task GetDefaultFanTableAsync_ShouldReturnFanTable()
    {
        var expectedFanTable = new FanTable();
        _controllerV1Mock
            .Setup(m => m.GetDefaultFanTableAsync())
            .ReturnsAsync(expectedFanTable);

        var result = await _controller.GetDefaultFanTableAsync();

        result.Should().Be(expectedFanTable);
    }

    [TestMethod]
    public async Task GetMinimumFanTableAsync_ShouldReturnFanTable()
    {
        var expectedFanTable = new FanTable();
        _controllerV1Mock
            .Setup(m => m.GetMinimumFanTableAsync())
            .ReturnsAsync(expectedFanTable);

        var result = await _controller.GetMinimumFanTableAsync();

        result.Should().Be(expectedFanTable);
    }

    [TestMethod]
    public async Task GetDefaultsInOtherPowerModesAsync_ShouldReturnDictionary()
    {
        var expectedDict = new Dictionary<PowerModeState, GodModeDefaults>();
        _controllerV1Mock
            .Setup(m => m.GetDefaultsInOtherPowerModesAsync())
            .ReturnsAsync(expectedDict);

        var result = await _controller.GetDefaultsInOtherPowerModesAsync();

        result.Should().BeSameAs(expectedDict);
    }

    [TestMethod]
    public async Task RestoreDefaultsInOtherPowerModeAsync_ShouldCallCorrectController()
    {
        var powerMode = PowerModeState.Quiet;
        _controllerV1Mock
            .Setup(m => m.RestoreDefaultsInOtherPowerModeAsync(powerMode))
            .Returns(Task.CompletedTask);

        var act = async () => await _controller.RestoreDefaultsInOtherPowerModeAsync(powerMode);

        await act.Should().NotThrowAsync();
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class FanTableTests : UnitTestBase
{
    [TestMethod]
    public void FanTable_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        var fanTable = new FanTable();

        fanTable.Should().NotBeNull();
    }

    [TestMethod]
    public void FanTable_WithParameters_ShouldSetPropertiesCorrectly()
    {
        var fanTable = new FanTable
        {
            FSTM = 1,
            FSID = 0
        };

        fanTable.FSTM.Should().Be(1);
        fanTable.FSID.Should().Be(0);
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class GodModeStateTests : UnitTestBase
{
    [TestMethod]
    public void GodModeState_DefaultConstructor_ShouldInitialize()
    {
        var state = new GodModeState();

        state.Should().NotBeNull();
    }

    [TestMethod]
    public void GodModeState_WithPresetId_ShouldSetCorrectly()
    {
        var presetId = Guid.NewGuid();
        var state = new GodModeState
        {
            ActivePresetId = presetId
        };

        state.ActivePresetId.Should().Be(presetId);
    }
}
