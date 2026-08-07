using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaNotificationMigrationContractTests
{
    [Fact]
    public void AvaloniaHost_ShouldBridgeBothSharedNotificationBuses()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "AvaloniaNotificationManager.cs"));
        var app = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "App.axaml.cs"));

        source.Should().Contain("MessagingCenter.Subscribe<NotificationMessage>");
        source.Should().Contain("IAppNotificationService");
        source.Should().Contain("NotificationTypePolicyStore.GetOrDefault");
        source.Should().Contain("NotificationOnAllScreens");
        source.Should().Contain("NotificationPosition");
        source.Should().Contain("NotificationDuration");
        source.Should().Contain("NotificationAlwaysOnTop");
        source.Should().Contain("NotificationSound");
        source.Should().Contain("DontShowNotifications");
        source.Should().Contain("AvaloniaToastWindow");
        app.Should().Contain("new AvaloniaNotificationManager(");
        app.Should().Contain("_notificationManager?.Dispose();");
    }

    [Fact]
    public void AvaloniaNotificationHost_ShouldPreserveWpfNotificationCategories()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "AvaloniaNotificationManager.cs"));

        foreach (var category in new[]
                 {
                     "ACAdapter",
                     "AutomationNotification",
                     "CapsNumLock",
                     "CameraLock",
                     "FnLock",
                     "KeyboardBacklight",
                     "PowerMode",
                     "RefreshRate",
                     "SmartKey",
                     "TouchpadLock",
                     "UpdateAvailable",
                 })
        {
            source.Should().Contain($"\"{category}\"");
        }
    }

    [Fact]
    public void UpdateNotificationClick_PrefersUpdateWindowOverAboutFallback()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "AvaloniaNotificationManager.cs"));

        source.Should().Contain("ShowUpdateAsync");
        source.Should().Contain("AvaloniaUpdateCheckCoordinator.Current");
        source.Should().Contain("MainNavigation.About");

        var coordinatorIndex = source.IndexOf("AvaloniaUpdateCheckCoordinator.Current", StringComparison.Ordinal);
        var fallbackIndex = source.IndexOf("MainNavigation.About", StringComparison.Ordinal);
        coordinatorIndex.Should().BeGreaterThanOrEqualTo(0);
        fallbackIndex.Should().BeGreaterThan(coordinatorIndex);
    }

    [Fact]
    public void AvaloniaNotificationHost_ShouldHonorWpfFullscreenSuppression()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "AvaloniaNotificationManager.cs"));

        source.Should().Contain("ShouldSuppressForFullscreen");
        source.Should().Contain("GetForegroundWindow");
        source.Should().Contain("GetWindowRect");
        source.Should().Contain("NotificationAlwaysOnTop");
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public void FullscreenSuppression_AppliesOnlyWhenNotificationsAreNotAlwaysOnTop(
        bool notificationAlwaysOnTop,
        bool isAnyApplicationFullscreen,
        bool expected)
    {
        AvaloniaNotificationManager.ShouldSuppressForFullscreen(notificationAlwaysOnTop, isAnyApplicationFullscreen)
            .Should().Be(expected);
    }
}
