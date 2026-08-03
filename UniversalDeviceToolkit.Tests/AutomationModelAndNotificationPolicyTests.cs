using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Lib.System;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public sealed class AutomationModelAndNotificationPolicyTests
{
    [Fact]
    public void BatteryPercentageTrigger_SerializesRoundTrip()
    {
        var trigger = new BatteryPercentageAutomationPipelineTrigger(
            BatteryPercentageComparison.BelowOrEqual,
            25,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromMinutes(2),
            BatteryChargeFilter.Discharging);

        var copy = (BatteryPercentageAutomationPipelineTrigger)trigger.DeepCopy();
        copy.Comparison.Should().Be(BatteryPercentageComparison.BelowOrEqual);
        copy.Threshold.Should().Be(25);
        copy.Duration.Should().Be(TimeSpan.FromSeconds(3));
        copy.Cooldown.Should().Be(TimeSpan.FromMinutes(2));
        copy.ChargeFilter.Should().Be(BatteryChargeFilter.Discharging);
        copy.Should().NotBeSameAs(trigger);
    }

    [SkippableFact]
    public async Task BatteryPercentageTrigger_NoData_ReturnsFalseSafely()
    {
        // DeepCopy path does not throw; IsMatchingState must tolerate missing battery by returning false.
        Skip.If(Battery.IsBatteryMonitoringSupported(), "battery monitoring is available on this machine; the no-data path cannot be exercised");

        var trigger = new BatteryPercentageAutomationPipelineTrigger(
            BatteryPercentageComparison.AboveOrEqual,
            50,
            TimeSpan.Zero,
            TimeSpan.Zero,
            BatteryChargeFilter.Any);

        var result = await trigger.IsMatchingState();
        result.Should().BeFalse();
    }

    [Fact]
    public void NotificationTypePolicyStore_Defaults_AreStable()
    {
        var defaults = NotificationTypePolicyStore.CreateDefaults();
        defaults.Should().ContainKey("UpdateAvailable");
        defaults["UpdateAvailable"].Enabled.Should().BeTrue();
        defaults["CapsNumLock"].Enabled.Should().BeFalse();
        defaults["AutomationNotification"].Severity.Should().Be(NotificationPriority.Normal);
        defaults["AutomationNotification"].Persist.Should().BeFalse();

        var missing = NotificationTypePolicyStore.GetOrDefault(null, "Missing", legacyEnabled: false);
        missing.Enabled.Should().BeFalse();
        missing.Persist.Should().BeFalse();
    }

    [Fact]
    public void ApplicationSettings_NotificationPosition_DefaultsBottomRight()
    {
        var store = new ApplicationSettings.ApplicationSettingsStore();
        store.NotificationPosition.Should().Be(NotificationPosition.BottomRight);
        store.Notifications.TypePolicies.Should().NotBeEmpty();
    }

    [Fact]
    public void ShowHideMainWindowSteps_DeepCopy()
    {
        IAutomationStep show = new ShowMainWindowAutomationStep();
        IAutomationStep hide = new HideMainWindowAutomationStep();
        show.DeepCopy().Should().BeOfType<ShowMainWindowAutomationStep>();
        hide.DeepCopy().Should().BeOfType<HideMainWindowAutomationStep>();
    }
}
