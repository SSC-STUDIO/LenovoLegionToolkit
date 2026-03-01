using System;
using System.Runtime.CompilerServices;
using FluentAssertions;
using LenovoLegionToolkit.Lib.AutoListeners;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LenovoLegionToolkit.Tests.Controllers;

[TestClass]
[TestCategory(TestCategories.Controller)]
public class AIControllerTests : UnitTestBase
{
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
        var controller = CreateController(settings);

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
        return CreateController(new BalanceModeSettings());
    }

    private static AIController CreateController(BalanceModeSettings settings)
    {
        return new AIController(
            CreateUninitialized<PowerModeListener>(),
            CreateUninitialized<PowerStateListener>(),
            CreateUninitialized<GameAutoListener>(),
            CreateUninitialized<PowerModeFeature>(),
            settings);
    }

    private static T CreateUninitialized<T>() where T : class =>
        (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
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
