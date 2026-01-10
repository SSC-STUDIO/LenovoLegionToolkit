using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class ApplicationSettingsTests
{
    [Fact]
    public void ShowDonateButton_ShouldDefaultToTrue()
    {
        // Arrange & Act
        // Create a new store instance to test default value
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Assert
        store.ShowDonateButton.Should().BeTrue();
    }

    [Fact]
    public void ShowDonateButton_ShouldPersistValue()
    {
        // Arrange
        var settings = new ApplicationSettings();
        settings.Store.ShowDonateButton = false;

        // Act
        settings.SynchronizeStore();
        var value = settings.Store.ShowDonateButton;

        // Assert
        value.Should().BeFalse();
    }

    [Fact]
    public void Notifications_ShouldHaveDefaultValues()
    {
        // Arrange
        var settings = new ApplicationSettings();

        // Act
        var notifications = settings.Store.Notifications;

        // Assert
        notifications.Should().NotBeNull();
        notifications.UpdateAvailable.Should().BeTrue();
        notifications.TouchpadLock.Should().BeTrue();
        notifications.KeyboardBacklight.Should().BeTrue();
        notifications.CameraLock.Should().BeTrue();
        notifications.Microphone.Should().BeTrue();
        notifications.RefreshRate.Should().BeTrue();
        notifications.AutomationNotification.Should().BeTrue();
    }

    [Fact]
    public void Notifications_ShouldHaveDefaultFalseValues()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act
        var notifications = store.Notifications;

        // Assert
        notifications.CapsNumLock.Should().BeFalse();
        notifications.FnLock.Should().BeFalse();
        notifications.PowerMode.Should().BeFalse();
        notifications.ACAdapter.Should().BeFalse();
        notifications.SmartKey.Should().BeFalse();
    }

    [Fact]
    public void CustomCleanupRules_ShouldDefaultToEmptyList()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act
        var rules = store.CustomCleanupRules;

        // Assert
        rules.Should().NotBeNull();
        rules.Should().BeEmpty();
    }

    [Fact]
    public void CustomCleanupRule_ShouldHaveDefaultValues()
    {
        // Arrange
        var rule = new CustomCleanupRule();

        // Act & Assert
        rule.DirectoryPath.Should().BeEmpty();
        rule.Extensions.Should().NotBeNull();
        rule.Extensions.Should().BeEmpty();
        rule.Recursive.Should().BeTrue();
    }

    [Fact]
    public void PowerModeMappingMode_ShouldDefaultToWindowsPowerMode()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act & Assert
        store.PowerModeMappingMode.Should().Be(PowerModeMappingMode.WindowsPowerMode);
    }

    [Fact]
    public void NotificationPosition_ShouldDefaultToBottomCenter()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act & Assert
        store.NotificationPosition.Should().Be(NotificationPosition.BottomCenter);
    }

    [Fact]
    public void NotificationDuration_ShouldDefaultToNormal()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act & Assert
        store.NotificationDuration.Should().Be(NotificationDuration.Normal);
    }

    [Fact]
    public void MinimizeToTray_ShouldDefaultToTrue()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act & Assert
        store.MinimizeToTray.Should().BeTrue();
    }

    [Fact]
    public void MinimizeOnClose_ShouldDefaultToFalse()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act & Assert
        store.MinimizeOnClose.Should().BeFalse();
    }

    [Fact]
    public void ExcludedRefreshRates_ShouldDefaultToEmptyList()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act & Assert
        store.ExcludedRefreshRates.Should().NotBeNull();
        store.ExcludedRefreshRates.Should().BeEmpty();
    }

    [Fact]
    public void SmartKeyActionLists_ShouldDefaultToEmptyLists()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act & Assert
        store.SmartKeySinglePressActionList.Should().NotBeNull();
        store.SmartKeySinglePressActionList.Should().BeEmpty();
        store.SmartKeyDoublePressActionList.Should().NotBeNull();
        store.SmartKeyDoublePressActionList.Should().BeEmpty();
    }

    [Fact]
    public void PowerPlansAndModes_ShouldDefaultToEmptyDictionaries()
    {
        // Arrange
        var store = new ApplicationSettings.ApplicationSettingsStore();

        // Act & Assert
        store.PowerPlans.Should().NotBeNull();
        store.PowerPlans.Should().BeEmpty();
        store.PowerModes.Should().NotBeNull();
        store.PowerModes.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationSettings_ShouldBeSingleton()
    {
        // Arrange
        var settings1 = IoCContainer.Resolve<ApplicationSettings>();
        var settings2 = IoCContainer.Resolve<ApplicationSettings>();

        // Act & Assert
        settings1.Should().BeSameAs(settings2);
    }

    [Fact]
    public void LoadStore_ShouldReturnDefault_WhenFileNotFound()
    {
        // Arrange
        var settings = new ApplicationSettings();

        // Act
        var store = settings.LoadStore();

        // Assert
        store.Should().NotBeNull();
    }

    [Fact]
    public void SynchronizeStore_ShouldNotThrow()
    {
        // Arrange
        var settings = new ApplicationSettings();

        // Act
        Action act = () => settings.SynchronizeStore();

        // Assert
        act.Should().NotThrow();
    }
}

