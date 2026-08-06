using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class OfficialPluginAvaloniaMigrationContractTests
{
    [Theory]
    [InlineData("CustomMouse", "AvaloniaCustomMouseSettingsControl")]
    [InlineData("ShellIntegration", "AvaloniaShellIntegrationSettingsControl")]
    public void OfficialSettingsPlugins_ProvideNativeAvaloniaControl(string pluginDirectory, string controlType)
    {
        var root = RepositoryPaths.FindRoot();
        var directory = Path.Combine(root, "Plugins", "Official", pluginDirectory);
        File.Exists(Path.Combine(directory, $"{controlType}.cs")).Should().BeTrue();
        File.ReadAllText(Path.Combine(directory, $"UniversalDeviceToolkit.Plugins.{pluginDirectory}.csproj"))
            .Should().Contain("PackageReference Include=\"Avalonia\"");
    }

    [Fact]
    public void AvaloniaHost_UsesConventionFactoryForOfficialPluginControls()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));

        source.Should().Contain("TryGetConventionAvaloniaPageFactory");
        source.Should().Contain("Avalonia{pluginName}");
        source.Should().Contain("CreateAvaloniaPage");
    }
}
