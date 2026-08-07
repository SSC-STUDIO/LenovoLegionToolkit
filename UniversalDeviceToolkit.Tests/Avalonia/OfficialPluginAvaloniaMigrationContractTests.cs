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

    [Fact]
    public void AvaloniaHost_PrefersIAvaloniaPluginPageOverReflection()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));

        source.Should().Contain("is IAvaloniaPluginPage");
        source.Should().Contain("avaloniaPage.CreateAvaloniaPage()");
        source.Should().Contain("GetMethod");
    }

    [Theory]
    [InlineData("UniversalDeviceToolkit.Lib/Plugins/LegacyPluginContracts.cs")]
    [InlineData("Plugins/SDK/Abstractions/IAvaloniaPluginPage.cs")]
    public void IAvaloniaPluginPage_IsDeclaredInBothAbiContracts(string relativePath)
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, relativePath));

        source.Should().Contain("interface IAvaloniaPluginPage");
        source.Should().Contain("object CreateAvaloniaPage()");
    }

    [Theory]
    [InlineData("CustomMouse", "new AvaloniaCustomMouseSettingsControl")]
    [InlineData("ShellIntegration", "new AvaloniaShellIntegrationSettingsControl")]
    [InlineData("ViveTool", "new AvaloniaViveToolSettingsPage")]
    public void OfficialSettingsPlugins_ExposeExplicitAvaloniaFactory(string pluginDirectory, string factoryExpression)
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "Plugins",
            "Official",
            pluginDirectory,
            $"{pluginDirectory}Plugin.cs"));

        source.Should().Contain("public object CreateAvaloniaPage()");
        source.Should().Contain(factoryExpression);
    }

    [Fact]
    public void CustomMouse_AvaloniaSeparatesModePersistenceFromApplyingCurrentTheme()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "Plugins",
            "Official",
            "CustomMouse",
            "AvaloniaCustomMouseSettingsControl.cs"));

        source.Should().Contain("ApplyCurrentCursorThemeAsync");
        source.Should().Contain("ActionButton(CustomMouseText.ApplyCursorThemeNowButton, ApplyCurrentCursorThemeAsync");
        source.Should().Contain("_plugin.ApplyCursorStyleForCurrentThemeAsync()");
        source.Should().Contain("_plugin.SetCursorThemeModeAsync(mode)");
        source.Should().Contain("await _plugin.SaveSettingsAsync()");
        source.Should().Contain("Hydrate(setReadyStatus: false)");
    }

    [Fact]
    public void ShellIntegration_AvaloniaProvidesTheWpfManagementAndProfileOperations()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "Plugins",
            "Official",
            "ShellIntegration",
            "AvaloniaShellIntegrationSettingsControl.cs"));

        foreach (var operation in new[]
                 {
                     "EnableShellAsync",
                     "DisableShellAsync",
                     "SyncManagedConfigurationAsync",
                     "ResetManagedConfigurationAsync",
                     "ApplyPresetAsync",
                     "ExportProfile",
                     "ImportProfileAsync",
                     "OpenShellFolder",
                     "OpenShellConfigFile",
                     "OpenManagedConfigFolder",
                 })
        {
            source.Should().Contain(operation);
        }

        source.Should().Contain("SaveFilePickerAsync");
        source.Should().Contain("OpenFilePickerAsync");
        source.Should().Contain("StatusPresetApplyFailed");
        source.Should().Contain("StatusProfileImportFailed");
    }

    [Fact]
    public void ViveTool_AvaloniaProvidesFeatureAndSettingsOperationsWithFailureStates()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "Plugins",
            "Official",
            "ViveTool",
            "AvaloniaViveToolPages.cs"));

        foreach (var operation in new[]
                 {
                     "ListFeaturesAsync",
                     "EnableFeatureAsync",
                     "DisableFeatureAsync",
                     "ImportFeaturesFromFileAsync",
                     "ImportFeaturesFromUrlAsync",
                     "ExportFeaturesToFileAsync",
                     "DownloadViveToolAsync",
                     "SetViveToolPathAsync",
                 })
        {
            source.Should().Contain(operation);
        }

        source.Should().Contain("ViveTool_ImportFailed");
        source.Should().Contain("ViveTool_ExportFailed");
        source.Should().Contain("ViveTool_DownloadFailed");
        source.Should().Contain("ViveTool_EnableFeatureFailed");
        source.Should().Contain("ViveTool_DisableFeatureFailed");
    }

    [Fact]
    public void PluginHostedPage_EmbedsNativeContentAndRendersCompatibilityStateForOtherPlugins()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "PluginHostedPage.cs"));

        source.Should().Contain("state.Content is Control control && state.IsAvaloniaPage");
        source.Should().Contain("BuildCompatibilityState(state)");
        source.Should().Contain("PluginPage_WpfOnlyTitle");
        source.Should().Contain("PluginPage_NoFeatureTitle");
    }
}
