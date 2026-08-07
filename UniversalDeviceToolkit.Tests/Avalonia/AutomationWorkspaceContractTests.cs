using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Lib.Automation.Serialization;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AutomationWorkspaceContractTests
{
    [Fact]
    public async Task UnavailableHost_ExposesAnExplicitEmptyAutomationWorkspace()
    {
        var service = new UnavailablePlatformServices();

        var state = await service.GetAutomationWorkspaceAsync();

        state.IsEnabled.Should().BeFalse();
        state.Pipelines.Should().BeEmpty();
        (await service.SetAutomationEnabledAsync(true)).Should().BeFalse();
        (await service.SaveAutomationWorkspaceAsync([])).Should().BeFalse();
    }

    [Fact]
    public void AutomationPipelineDraft_NewManualPipelineHasNoPersistedIdentity()
    {
        var draft = new AutomationPipelineDraft(null, "Quick action", "Rocket24", false);

        draft.Id.Should().BeNull();
        draft.IsAutomatic.Should().BeFalse();
        draft.Name.Should().Be("Quick action");
    }

    [Fact]
    public void AutomationPipelineDraft_AutomaticPipelineCarriesStableTriggerKey()
    {
        var draft = new AutomationPipelineDraft(
            null,
            "Start with Windows",
            null,
            true)
        {
            TriggerKey = "on-startup",
        };

        draft.IsAutomatic.Should().BeTrue();
        draft.TriggerKey.Should().Be("on-startup");
    }

    [Fact]
    public void AutomationTriggerOption_UsesStableKeyIndependentOfLocalizedDisplayName()
    {
        var option = new AutomationTriggerOption("on-resume", "Resume");

        option.Key.Should().Be("on-resume");
        option.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AutomationPipelineItem_ReportsTriggerAndStepCountForEditorRows()
    {
        var item = new AutomationPipelineItem(
            Guid.NewGuid(),
            "Night mode",
            "WeatherMoon24",
            "At sunset",
            2,
            true);

        item.IsAutomatic.Should().BeTrue();
        item.Trigger.Should().Be("At sunset");
        item.StepCount.Should().Be(2);
    }

    [Fact]
    public void AutomationPipelineItem_UnknownTriggerKeyCanBeNullForLosslessPreservation()
    {
        var item = new AutomationPipelineItem(
            Guid.NewGuid(),
            "Composite",
            null,
            "Combined trigger",
            1,
            true);

        item.TriggerKey.Should().BeNull();
    }

    [Fact]
    public void AutomationDraft_RoundTripsOrderedStepsAndAdvancedTriggerConfiguration()
    {
        var draft = new AutomationPipelineDraft(Guid.NewGuid(), "Night mode", null, true)
        {
            TriggerKey = "hardware-sensor",
            TriggerConfigurationJson = "{\"$type\":\"hardwareSensor\",\"Threshold\":80}",
            IsExclusive = false,
            Steps =
            [
                new AutomationStepItem("Delay", "Delay", "{\"$type\":\"delay\",\"State\":{\"DelaySeconds\":2}}"),
                new AutomationStepItem("PowerMode", "Power mode", "{\"$type\":\"powerMode\",\"State\":2}"),
            ],
        };

        draft.IsExclusive.Should().BeFalse();
        draft.Steps.Select(step => step.TypeKey).Should().Equal("Delay", "PowerMode");
        draft.TriggerConfigurationJson.Should().Contain("hardwareSensor");
    }

    [Fact]
    public void AutomationStepOption_DefaultConfigurationIsIndependentFromLocalizedDisplayName()
    {
        var option = new AutomationStepOption("Run", "Run script", "{\"$type\":\"run\"}");

        option.TypeKey.Should().Be("Run");
        option.DisplayName.Should().Be("Run script");
        option.DefaultConfigurationJson.Should().Contain("$type");
    }

    [Fact]
    public void ManualActions_DoNotOfferQuickActionSteps_AndAutomaticActionsDo()
    {
        var options = new[]
        {
            new AutomationStepOption("Delay", "Delay", "{}"),
            new AutomationStepOption("QuickAction", "Quick action", "{}"),
        };

        AutomationPage.GetAvailableStepOptions(options, isAutomatic: false)
            .Select(option => option.TypeKey)
            .Should().Equal("Delay");
        AutomationPage.GetAvailableStepOptions(options, isAutomatic: true)
            .Select(option => option.TypeKey)
            .Should().Equal("Delay", "QuickAction");
    }

    [Fact]
    public void QuickActionTargets_ContainOnlyManualPipelines()
    {
        var manual = new AutomationPipelineItem(Guid.NewGuid(), "Desk mode", "Desktop24", "Manual", 1, false);
        var automatic = new AutomationPipelineItem(Guid.NewGuid(), "On resume", null, "Resume", 1, true);

        AutomationPage.FilterManualQuickActionTargets([automatic, manual])
            .Should().ContainSingle()
            .Which.Id.Should().Be(manual.Id);
    }

    [Fact]
    public void NewAutomaticPipeline_ExcludesExistingDisallowDuplicateTriggerTypes()
    {
        var startup = new AutomationTriggerOption(
            "on-startup",
            "Startup",
            AutomationSerialization.SerializeTrigger(new OnStartupAutomationPipelineTrigger()));
        var periodic = new AutomationTriggerOption(
            "periodic",
            "Periodic",
            AutomationSerialization.SerializeTrigger(new PeriodicAutomationPipelineTrigger(TimeSpan.FromMinutes(30))));

        AutomationPage.FilterNewPipelineTriggerOptions(
                [startup, periodic],
                [new OnStartupAutomationPipelineTrigger()])
            .Select(option => option.Key)
            .Should().Equal("periodic");
    }
}
