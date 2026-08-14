using FluentAssertions;
using Moq;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Controllers;

[Trait("Category", TestCategories.Unit)]
public class SensorsGroupControllerSubscriberTests : UnitTestBase
{
    [Fact]
    public void Stop_WhenLastSubscriberRemoved_ShouldLeaveSubscriberCountZero()
    {
        var processManager = new Mock<IGPUProcessManager>(MockBehavior.Loose);
        var hardwareManager = new Mock<IGPUHardwareManager>(MockBehavior.Loose);
        var delay = new DefaultDelayProvider();
        using var gpu = new GPUController(processManager.Object, hardwareManager.Object, delay);
        using var controller = new SensorsGroupController(delay, gpu);
        var subscriber = new object();

        controller.Start(subscriber, TimeSpan.FromHours(1));
        controller.SubscriberCount.Should().Be(1);

        controller.Stop(subscriber);
        controller.SubscriberCount.Should().Be(0);
    }

    [Fact]
    public void Stop_WhenOtherSubscribersRemain_ShouldKeepProducerCount()
    {
        var processManager = new Mock<IGPUProcessManager>(MockBehavior.Loose);
        var hardwareManager = new Mock<IGPUHardwareManager>(MockBehavior.Loose);
        var delay = new DefaultDelayProvider();
        using var gpu = new GPUController(processManager.Object, hardwareManager.Object, delay);
        using var controller = new SensorsGroupController(delay, gpu);
        var dashboard = new object();
        var osd = new object();

        controller.Start(dashboard, TimeSpan.FromSeconds(1));
        controller.Start(osd, TimeSpan.FromSeconds(1));
        controller.SubscriberCount.Should().Be(2);

        controller.Stop(dashboard);
        controller.SubscriberCount.Should().Be(1);

        controller.Stop(osd);
        controller.SubscriberCount.Should().Be(0);
    }
}
