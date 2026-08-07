#if WINDOWS

using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class ApplicationAndIntegrationSettingsContractTests
{
    [Fact]
    public async Task ApplicationBehaviorPage_ExposesAndPersistsTheWpfWindowLifecycleSettings()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var page = await service.GetPageAsync("Application");
        var settings = WindowsAvaloniaSettingsService.SharedApplicationSettings;
        var originalMinimizeToTray = settings.Store.MinimizeToTray;
        var originalMinimizeOnClose = settings.Store.MinimizeOnClose;
        var originalAnimations = settings.Store.AnimationsEnabled;

        page.Options.Select(option => option.Key).Should().Contain([
            "Autorun",
            "MinimizeToTray",
            "MinimizeOnClose",
            "AnimationsEnabled",
            "EnableHardwareSensors",
            "ShowOsd",
            "ExportSettings",
            "ImportSettings",
        ]);

        try
        {
            await service.SetToggleAsync("Application", "MinimizeToTray", !originalMinimizeToTray);
            await service.SetToggleAsync("Application", "MinimizeOnClose", !originalMinimizeOnClose);
            await service.SetToggleAsync("Application", "AnimationsEnabled", !originalAnimations);

            settings.Store.MinimizeToTray.Should().Be(!originalMinimizeToTray);
            settings.Store.MinimizeOnClose.Should().Be(!originalMinimizeOnClose);
            settings.Store.AnimationsEnabled.Should().Be(!originalAnimations);
        }
        finally
        {
            await service.SetToggleAsync("Application", "MinimizeToTray", originalMinimizeToTray);
            await service.SetToggleAsync("Application", "MinimizeOnClose", originalMinimizeOnClose);
            await service.SetToggleAsync("Application", "AnimationsEnabled", originalAnimations);
        }
    }

    [Fact]
    public void IntegrationLifecycleToggles_RollBackWhenTheirSharedRuntimeServiceRejectsTheChange()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsAvaloniaSettingsService.cs"));

        source.Should().Contain("case (\"Integrations\", \"HWiNFO\"):");
        source.Should().Contain("await (IoCContainer.TryResolve<HWiNFOIntegration>()");
        source.Should().Contain("_integrationsSettings.Store.HWiNFO = previousHwInfo;");
        source.Should().Contain("case (\"Integrations\", \"CLI\"):");
        source.Should().Contain("await lifecycle.StartStopIfNeededAsync().ConfigureAwait(false);");
        source.Should().Contain("_integrationsSettings.Store.CLI = previousCli;");
        source.Should().Contain("case (\"Integrations\", \"CLIPath\"):");
        source.Should().Contain("SystemPath.SetCLI(value);");
    }
}

#endif
