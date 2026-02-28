using System.Linq;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.ShellIntegration;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Tests;

public class ShellIntegrationPluginTests
{
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
    public void GetSettingsPage_ReturnsPluginPage()
    {
        var plugin = new ShellIntegrationPlugin();

        var settingsPage = Assert.IsAssignableFrom<IPluginPage>(plugin.GetSettingsPage());

        Assert.Equal(ShellIntegrationText.SettingsPageTitle, settingsPage.PageTitle);
        Assert.Equal("Settings24", settingsPage.PageIcon);
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
    public void OpenStyleSettingsWindow_WithoutApplication_DoesNotThrow()
    {
        var plugin = new ShellIntegrationPlugin();

        plugin.OpenStyleSettingsWindow();
    }
}
