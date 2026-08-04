using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
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
}
