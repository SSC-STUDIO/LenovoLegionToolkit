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

    [Fact]
    public async Task CheckAsync_WhenUpdateIsAvailable_RaisesUpdateAvailableChangedWithReleaseInfo()
    {
        UpdateReleaseInfo? raised = null;
        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => Task.FromResult<Version?>(new Version(5, 0, 3, 0)),
            _ => { },
            _ => throw new Xunit.Sdk.XunitException("Unexpected update-check failure."),
            () => Task.FromResult<IReadOnlyList<UpdateReleaseInfo>>(
            [
                new UpdateReleaseInfo(new Version(5, 0, 3), "v5.0.3", false, "Release", "Notes", new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)),
            ]));
        coordinator.UpdateAvailableChanged += info => raised = info;

        await coordinator.CheckAsync();

        raised.Should().NotBeNull();
        raised!.Version.Should().Be(new Version(5, 0, 3));
        raised.TagName.Should().Be("v5.0.3");
        coordinator.LatestUpdate.Should().BeSameAs(raised);
    }

    [Fact]
    public async Task CheckAsync_WhenUpdateIsAvailable_DoesNotRaiseEventWithoutReleaseInfo()
    {
        UpdateReleaseInfo? raised = null;
        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => Task.FromResult<Version?>(new Version(5, 0, 3, 0)),
            _ => { },
            _ => throw new Xunit.Sdk.XunitException("Unexpected update-check failure."));
        coordinator.UpdateAvailableChanged += info => raised = info;

        await coordinator.CheckAsync();

        raised.Should().BeNull();
        coordinator.LatestUpdate.Should().BeNull();
    }

    [Fact]
    public async Task CheckAsync_WhenNoUpdateIsAvailable_DoesNotRaiseUpdateAvailableChanged()
    {
        UpdateReleaseInfo? raised = null;
        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => Task.FromResult<Version?>(null),
            _ => { },
            _ => throw new Xunit.Sdk.XunitException("Unexpected update-check failure."),
            () => Task.FromResult<IReadOnlyList<UpdateReleaseInfo>>(
            [
                new UpdateReleaseInfo(new Version(5, 0, 3), "v5.0.3", false, "Release", "Notes", DateTimeOffset.UtcNow),
            ]));
        coordinator.UpdateAvailableChanged += info => raised = info;

        await coordinator.CheckAsync();

        raised.Should().BeNull();
        coordinator.LatestUpdate.Should().BeNull();
    }

    [Fact]
    public async Task CheckAsync_WhenTheCheckFails_DoesNotRaiseUpdateAvailableChanged()
    {
        UpdateReleaseInfo? raised = null;
        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => Task.FromException<Version?>(new InvalidOperationException("network failure")),
            _ => { },
            _ => { });
        coordinator.UpdateAvailableChanged += info => raised = info;

        await coordinator.CheckAsync();

        raised.Should().BeNull();
        coordinator.LatestUpdate.Should().BeNull();
    }

    [Fact]
    public async Task CheckAsync_PicksTheNewestReleaseForTheEvent()
    {
        UpdateReleaseInfo? raised = null;
        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => Task.FromResult<Version?>(new Version(6, 0, 0, 0)),
            _ => { },
            _ => throw new Xunit.Sdk.XunitException("Unexpected update-check failure."),
            () => Task.FromResult<IReadOnlyList<UpdateReleaseInfo>>(
            [
                new UpdateReleaseInfo(new Version(5, 1, 0), "v5.1.0", false, "Old", string.Empty, DateTimeOffset.UtcNow),
                new UpdateReleaseInfo(new Version(6, 0, 0), "v6.0.0", false, "New", string.Empty, DateTimeOffset.UtcNow),
            ]));
        coordinator.UpdateAvailableChanged += info => raised = info;

        await coordinator.CheckAsync();

        raised.Should().NotBeNull();
        raised!.Version.Should().Be(new Version(6, 0, 0));
    }
}
