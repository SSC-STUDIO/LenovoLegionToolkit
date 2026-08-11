using System.Linq;
using System.Reflection;
using UniversalDeviceToolkit.Plugins.ShellIntegration;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ShellIntegration.Tests;

[Collection("ShellIntegrationResourceCulture")]
public class ShellIntegrationPluginTests
{
    private static bool? ParseShellRegistrationStatus(string commandOutput)
    {
        var method = typeof(ShellIntegrationPlugin).GetMethod("ParseShellRegistrationStatus", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool?)method!.Invoke(null, [commandOutput]);
    }

    [Fact]
    public void Plugin_HasExpectedMetadata()
    {
        var plugin = new ShellIntegrationPlugin();

        Assert.Equal("shell-integration", plugin.Id);
        Assert.Equal(ShellIntegrationText.PluginName, plugin.Name);
        Assert.True(plugin.IsSystemPlugin);
        Assert.Equal("Folder24", plugin.Icon);
        Assert.Equal(ShellIntegrationText.PluginDescription, plugin.Description);
    }

    [Fact]
    public void GetSettingsPage_ReturnsNull_WhenNoUiIsAvailable()
    {
        var plugin = new ShellIntegrationPlugin();

        Assert.Null(plugin.GetSettingsPage());
        Assert.Null(plugin.GetFeatureExtension());
    }

    [Fact]
    public void GetOptimizationCategory_ReturnsExpectedActions()
    {
        var plugin = new ShellIntegrationPlugin();

        var category = plugin.GetOptimizationCategory();

        Assert.NotNull(category);
        Assert.Equal("shell.integration", category!.Key);
        Assert.Equal(plugin.Id, category.PluginId);
        Assert.Equal(2, category.Actions.Count);

        var enableAction = category.Actions.Single(a => a.Key == "shell.integration.enable");
        var disableAction = category.Actions.Single(a => a.Key == "shell.integration.disable");

        Assert.True(enableAction.Recommended);
        Assert.False(disableAction.Recommended);
        Assert.NotNull(enableAction.IsAppliedAsync);
        Assert.NotNull(disableAction.IsAppliedAsync);
    }

    [Fact]
    public void ShellDetection_IsConsistentWithResolvedPath()
    {
        var plugin = new ShellIntegrationPlugin();
        var path = plugin.GetShellInstallPath();

        Assert.Equal(path is not null, plugin.IsShellInstalled());
    }

    [Fact]
    public void ConfigService_RenderTheme_ContainsManagedAccentAndEffect()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        profile.AccentColor = "#3366FF";
        profile.BackgroundEffect = ShellVisualEffect.Acrylic;

        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("color = #3366FF", rendered);
        Assert.Contains("effect = [3, #DCE6FF, 92]", rendered);
        Assert.Contains("name = \"modern\"", rendered);
    }

    [Fact]
    public void ConfigService_UpsertManagedImportBlock_IsIdempotent()
    {
        var content = "theme { }";
        var once = ShellIntegrationConfigService.UpsertManagedImportBlock(content);
        var twice = ShellIntegrationConfigService.UpsertManagedImportBlock(once);

        Assert.Equal(once, twice);
        Assert.Contains("imports/lenovo-legion-toolkit/settings.nss", twice);
        Assert.Contains("imports/lenovo-legion-toolkit/theme.nss", twice);
    }

    [Theory]
    [InlineData("Shell integration is not registered.")]
    [InlineData("Registered: false")]
    [InlineData("Enabled: false")]
    [InlineData("State: inactive")]
    public void ParseShellRegistrationStatus_WithNegativeSignals_ReturnsFalse(string output)
    {
        Assert.False(ParseShellRegistrationStatus(output));
    }

    [Theory]
    [InlineData("Shell integration is registered.")]
    [InlineData("Registered: true")]
    [InlineData("Enabled: true")]
    [InlineData("Status: active")]
    public void ParseShellRegistrationStatus_WithPositiveSignals_ReturnsTrue(string output)
    {
        Assert.True(ParseShellRegistrationStatus(output));
    }

    [Fact]
    public void ParseShellRegistrationStatus_PrefersExplicitNegativeSignals()
    {
        var output = """
                     Status: active
                     Registered: false
                     """;

        Assert.False(ParseShellRegistrationStatus(output));
    }

    [Fact]
    public void ParseShellRegistrationStatus_WithUnrelatedOutput_ReturnsNull()
    {
        Assert.Null(ParseShellRegistrationStatus("Shell command completed successfully."));
    }
}
