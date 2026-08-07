using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaGpuOverclockProfileContractTests
{
    [Fact]
    public void Dashboard_ProvidesTheWpfEquivalentGpuProfileOperations()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "DashboardPage.axaml"));
        var code = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "DashboardPage.axaml.cs"));
        var dialog = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "Windows", "GpuOverclockProfilesWindow.cs"));

        markup.Should().Contain("ConfigureGpuOverclockButton_Click");
        code.Should().Contain("new GpuOverclockProfilesWindow()");
        dialog.Should().Contain("GetProfiles()");
        dialog.Should().Contain("AddProfile(");
        dialog.Should().Contain("RenameProfile(");
        dialog.Should().Contain("DeleteProfile(");
        dialog.Should().Contain("SetActiveProfile(");
        dialog.Should().Contain("SaveProfile(");
        dialog.Should().Contain("ApplyStateAsync()");
    }
}
