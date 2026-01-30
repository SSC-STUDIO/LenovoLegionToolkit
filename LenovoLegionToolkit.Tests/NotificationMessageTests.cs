using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Messaging.Messages;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class NotificationMessageTests
{
    [Fact]
    public void NotificationMessage_Constructor_ShouldSetDefaultPriority()
    {
        // Arrange
        var type = NotificationType.ACAdapterConnected;

        // Act
        var message = new NotificationMessage(type, "arg1");

        // Assert
        message.Type.Should().Be(type);
        message.Priority.Should().Be(NotificationPriority.Normal);
        message.Args.Should().HaveCount(1);
        message.Args[0].Should().Be("arg1");
    }

    [Fact]
    public void NotificationMessage_Constructor_ShouldSetExplicitPriority()
    {
        // Arrange
        var type = NotificationType.UpdateAvailable;
        var priority = NotificationPriority.High;

        // Act
        var message = new NotificationMessage(type, priority, "arg1", "arg2");

        // Assert
        message.Type.Should().Be(type);
        message.Priority.Should().Be(priority);
        message.Args.Should().HaveCount(2);
        message.Args[0].Should().Be("arg1");
        message.Args[1].Should().Be("arg2");
    }

    [Fact]
    public void PriorityMapping_ShouldMapCorrectlyForPriorityQueue()
    {
        // High priority should have lowest value (0)
        // Normal priority should have middle value (1)
        // Low priority should have highest value (2)

        (2 - (int)NotificationPriority.High).Should().Be(0);
        (2 - (int)NotificationPriority.Normal).Should().Be(1);
        (2 - (int)NotificationPriority.Low).Should().Be(2);
    }
}
