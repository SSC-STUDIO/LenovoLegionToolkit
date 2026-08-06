using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class HardwareSensorSectionsWindowContractTests
{
    [Fact]
    public void HardwareSensorSectionsWindow_ProvidesVisibilityOrderAndDialogActions()
    {
        var root = RepositoryPaths.FindRoot();
        var path = Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "Windows",
            "HardwareSensorSectionsWindow.cs");
        var source = File.ReadAllText(path);

        source.Should().Contain("public sealed class HardwareSensorSectionsWindow : Window");
        source.Should().Contain("HardwareSectionsVisible");
        source.Should().Contain("HardwareSectionsOrder");
        source.Should().Contain("SetMultiSelectionAsync");
        source.Should().Contain("SetSelectionAsync");
        source.Should().Contain("MoveUp");
        source.Should().Contain("MoveDown");
        source.Should().Contain("Close(true)");
        source.Should().Contain("Close(false)");
    }

    [Fact]
    public void ApplicationSettings_OffersHardwareSectionConfigurationAction()
    {
        var root = RepositoryPaths.FindRoot();
        var path = Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsCapabilityView.axaml.cs");
        var source = File.ReadAllText(path);

        source.Should().Contain("HardwareSectionsConfigure");
        source.Should().Contain("new HardwareSensorSectionsWindow(_settingsService)");
        source.Should().Contain("await dialog.ShowDialog(owner)");
        source.Should().Contain("await RefreshPageAsync()");
        source.Should().Contain("OsdConfigure");
        source.Should().Contain("new OsdSettingsWindow(_settingsService)");
    }
}
