using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Startup;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaUpdateStartupCoordinatorTests
{
    [Fact]
    public void App_StartsAutomaticUpdateChecksBeforeLongRunningHostInitialization()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "App.axaml.cs"));

        var coordinatorIndex = source.IndexOf(
            "_updateCheckCoordinator = AvaloniaUpdateCheckCoordinator.Create();",
            StringComparison.Ordinal);
        var startupIndex = source.IndexOf(
            "_ = StartWindowsHostServicesAsync(desktop.MainWindow as MainWindow);",
            StringComparison.Ordinal);
        var requestIndex = source.IndexOf(
            "RequestAutomaticUpdateCheck();",
            coordinatorIndex,
            StringComparison.Ordinal);

        coordinatorIndex.Should().BeGreaterThanOrEqualTo(0);
        requestIndex.Should().BeGreaterThan(coordinatorIndex);
        requestIndex.Should().BeLessThan(startupIndex);
    }

    [Fact]
    public async Task CheckAsync_WhenUpdateIsAvailable_PublishesSharedUpdateNotification()
    {
        NotificationMessage? published = null;
        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => Task.FromResult<Version?>(new Version(5, 0, 3, 0)),
            notification => published = notification,
            _ => throw new Xunit.Sdk.XunitException("Unexpected update-check failure."));

        await coordinator.CheckAsync();

        published.Should().NotBeNull();
        published!.Value.Type.Should().Be(NotificationType.UpdateAvailable);
        published.Value.Args.Should().ContainSingle().Which.Should().Be("5.0.3");
    }

    [Fact]
    public async Task CheckAsync_WhenNoUpdateIsAvailable_DoesNotPublishNotification()
    {
        var publishCount = 0;
        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => Task.FromResult<Version?>(null),
            _ => publishCount++,
            _ => throw new Xunit.Sdk.XunitException("Unexpected update-check failure."));

        await coordinator.CheckAsync();

        publishCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckAsync_WhenTheCheckFails_ReportsAndDoesNotThrow()
    {
        Exception? reported = null;
        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => Task.FromException<Version?>(new InvalidOperationException("network failure")),
            _ => throw new Xunit.Sdk.XunitException("No update notification should be published."),
            exception => reported = exception);

        Func<Task> action = () => coordinator.CheckAsync();

        await action.Should().NotThrowAsync();
        reported.Should().BeOfType<InvalidOperationException>();
    }
}
