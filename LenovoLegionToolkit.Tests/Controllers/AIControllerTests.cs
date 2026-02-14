using System;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.AutoListeners;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LenovoLegionToolkit.Tests.Controllers;

[TestClass]
[TestCategory(TestCategories.Controller)]
public class AIControllerTests : UnitTestBase
{
    private Mock<PowerModeListener> _powerModeListenerMock = null!;
    private Mock<PowerStateListener> _powerStateListenerMock = null!;
    private Mock<GameAutoListener> _gameAutoListenerMock = null!;
    private Mock<PowerModeFeature> _powerModeFeatureMock = null!;
    private Mock<BalanceModeSettings> _settingsMock = null!;

    protected override void Setup()
    {
        _powerModeListenerMock = new Mock<PowerModeListener>(null!);
        _powerStateListenerMock = new Mock<PowerStateListener>(null!);
        _gameAutoListenerMock = new Mock<GameAutoListener>(null!, null!);
        _powerModeFeatureMock = new Mock<PowerModeFeature>(null!, null!, null!, null!, null!);
        _settingsMock = new Mock<BalanceModeSettings>();
    }

    [TestMethod]
    public void Constructor_ShouldInitializeCorrectly()
    {
        var controller = CreateController();

        controller.Should().NotBeNull();
    }

    [TestMethod]
    public void IsAIModeEnabled_GetSet_ShouldWorkCorrectly()
    {
        var settings = new BalanceModeSettings();
        var controller = new AIController(
            _powerModeListenerMock.Object,
            _powerStateListenerMock.Object,
            _gameAutoListenerMock.Object,
            _powerModeFeatureMock.Object,
            settings);

        controller.IsAIModeEnabled = true;
        controller.IsAIModeEnabled.Should().BeTrue();

        controller.IsAIModeEnabled = false;
        controller.IsAIModeEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void Dispose_ShouldNotThrow()
    {
        var controller = CreateController();

        var act = () => controller.Dispose();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        var controller = CreateController();

        var act = () =>
        {
            controller.Dispose();
            controller.Dispose();
            controller.Dispose();
        };

        act.Should().NotThrow();
    }

    private AIController CreateController()
    {
        return new AIController(
            _powerModeListenerMock.Object,
            _powerStateListenerMock.Object,
            _gameAutoListenerMock.Object,
            _powerModeFeatureMock.Object,
            _settingsMock.Object);
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class BalanceModeSettingsTests : UnitTestBase
{
    [TestMethod]
    public void Store_DefaultValues_ShouldBeCorrect()
    {
        var settings = new BalanceModeSettings();

        settings.Store.AIModeEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void Store_CanBeModified()
    {
        var settings = new BalanceModeSettings();

        settings.Store.AIModeEnabled = true;

        settings.Store.AIModeEnabled.Should().BeTrue();
    }
}
