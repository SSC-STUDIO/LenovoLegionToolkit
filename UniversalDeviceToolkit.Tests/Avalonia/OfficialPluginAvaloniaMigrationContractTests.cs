using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class OfficialPluginAvaloniaMigrationContractTests
{
    [Theory]
    [InlineData("CustomMouse", "AvaloniaCustomMouseSettingsControl")]
    [InlineData("ShellIntegration", "AvaloniaShellIntegrationSettingsControl")]
    [InlineData("ViveTool", "AvaloniaViveToolSettingsPage")]
    public void OfficialSettingsPlugins_ProvideNativeAvaloniaControl(string pluginDirectory, string controlType)
    {
        var root = RepositoryPaths.FindRoot();
        var directory = Path.Combine(root, "Plugins", "Official", pluginDirectory);
        var controlSource = Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
            .SingleOrDefault(path => File.ReadAllText(path).Contains($"class {controlType}", StringComparison.Ordinal));
        controlSource.Should().NotBeNull($"{pluginDirectory} should define {controlType}");
        File.ReadAllText(Path.Combine(directory, $"UniversalDeviceToolkit.Plugins.{pluginDirectory}.csproj"))
            .Should().Contain("PackageReference Include=\"Avalonia\"");
    }

    [Fact]
    public void ViveToolFeaturePlugin_ProvidesNativeAvaloniaPageAndFactory()
    {
        var root = RepositoryPaths.FindRoot();
        var directory = Path.Combine(root, "Plugins", "Official", "ViveTool");
        var pageSource = File.ReadAllText(Path.Combine(directory, "AvaloniaViveToolPages.cs"));
        var pluginSource = File.ReadAllText(Path.Combine(directory, "ViveToolPlugin.cs"));

        pageSource.Should().Contain("class AvaloniaViveToolPage");
        pageSource.Should().Contain("class AvaloniaViveToolSettingsPage");
        pluginSource.Should().Contain("public object CreateAvaloniaPage()");
        pluginSource.Should().Contain("new AvaloniaViveToolPage()");
        pluginSource.Should().Contain("new AvaloniaViveToolSettingsPage()");
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
