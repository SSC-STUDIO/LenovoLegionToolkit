#if WINDOWS

using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class DisplaySettingsServiceTests
{
    private static readonly string[] NotificationOptionKeys =
    [
        "NotificationPosition",
        "NotificationDuration",
        "NotificationAlwaysOnTop",
        "NotificationOnAllScreens",
        "NotificationSound",
        "NotificationSuccess",
        "NotificationUpdateAvailable",
        "NotificationCapsNumLock",
        "NotificationFnLock",
        "NotificationTouchpadLock",
        "NotificationKeyboardBacklight",
        "NotificationCameraLock",
        "NotificationMicrophone",
        "NotificationPowerMode",
        "NotificationRefreshRate",
        "NotificationACAdapter",
        "NotificationSmartKey",
        "NotificationAutomation",
    ];

    [Fact]
    public async Task DisablingNotifications_DisablesEveryNotificationOption()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var before = await service.GetPageAsync("Display");
        var disableOption = before.Options.Single(option => option.Key == "DontShowNotifications");

        try
        {
            await service.SetToggleAsync("Display", "DontShowNotifications", true);

            var after = await service.GetPageAsync("Display");
            after.Options.Single(option => option.Key == "DontShowNotifications")
                .IsEnabled.Should().BeTrue();

            foreach (var key in NotificationOptionKeys)
            {
                after.Options.Single(option => option.Key == key)
                    .IsEnabled.Should().BeFalse($"{key} should be disabled when notifications are disabled");
            }
        }
        finally
        {
            await service.SetToggleAsync("Display", "DontShowNotifications", disableOption.BoolValue);
        }
    }

    [Fact]
    public async Task NotificationSelections_UseLocalizedDisplayValuesAndRoundTrip()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var before = await service.GetPageAsync("Display");
        var position = before.Options.Single(option => option.Key == "NotificationPosition");
        var duration = before.Options.Single(option => option.Key == "NotificationDuration");

        var bottomRight = NotificationPosition.BottomRight.GetDisplayName();
        var topLeft = NotificationPosition.TopLeft.GetDisplayName();
        var longDuration = NotificationDuration.Long.GetDisplayName();

        position.Values.Should().Contain(bottomRight);
        position.Values.Should().NotContain("BottomRight");
        duration.Values.Should().Contain(NotificationDuration.Normal.GetDisplayName());

        try
        {
            await service.SetSelectionAsync("Display", "NotificationPosition", topLeft);
            await service.SetSelectionAsync("Display", "NotificationDuration", longDuration);

            var after = await service.GetPageAsync("Display");
            after.Options.Single(option => option.Key == "NotificationPosition")
                .SelectedValue.Should().Be(topLeft);
            after.Options.Single(option => option.Key == "NotificationDuration")
                .SelectedValue.Should().Be(longDuration);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(position.SelectedValue))
                await service.SetSelectionAsync("Display", "NotificationPosition", position.SelectedValue);
            if (!string.IsNullOrWhiteSpace(duration.SelectedValue))
                await service.SetSelectionAsync("Display", "NotificationDuration", duration.SelectedValue);
        }
    }
}

#endif
