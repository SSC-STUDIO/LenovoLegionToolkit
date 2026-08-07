using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaPowerMappingDialogContractTests
{
    [Fact]
    public void PowerSettings_AreConfiguredInNativeAvaloniaDialogs()
    {
        var root = RepositoryPaths.FindRoot();
        var capabilitySource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsCapabilityView.axaml.cs"));
        var dialogSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "Windows",
            "PowerMappingSettingsWindow.cs"));

        capabilitySource.Should().Contain("ConfigurePowerMappingAsync(PowerMappingKind.WindowsPowerMode, button)");
        capabilitySource.Should().Contain("ConfigurePowerMappingAsync(PowerMappingKind.WindowsPowerPlan, button)");
        dialogSource.Should().Contain("WindowsPowerPlanController");
        dialogSource.Should().Contain("PowerModeFeature");
        dialogSource.Should().Contain("EnsureCorrectWindowsPowerSettingsAreSetAsync");
        dialogSource.Should().Contain("_settings.Store.PowerModes[state]");
        dialogSource.Should().Contain("_settings.Store.PowerPlans[state]");
        dialogSource.Should().Contain("AvaloniaPowerPlansAlwaysOnAcWarning");
        dialogSource.Should().Contain("SupportsAlwaysOnAc.status");
    }
}
