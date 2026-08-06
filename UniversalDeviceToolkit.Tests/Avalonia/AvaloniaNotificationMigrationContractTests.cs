using FluentAssertions;
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
}
