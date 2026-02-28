using System.Reflection;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.ViveTool;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

public class ViveToolPluginTests
{
    [Fact]
    public void Plugin_HasExpectedMetadata()
    {
        var plugin = new ViveToolPlugin();

        Assert.Equal("vive-tool", plugin.Id);
        Assert.False(plugin.IsSystemPlugin);
        Assert.Equal("Code24", plugin.Icon);
        Assert.False(string.IsNullOrWhiteSpace(plugin.Name));
        Assert.False(string.IsNullOrWhiteSpace(plugin.Description));
    }

    [Fact]
    public void Plugin_HasExpectedAttribute()
    {
        var attribute = typeof(ViveToolPlugin).GetCustomAttribute<PluginAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("vive-tool", attribute!.Id);
        Assert.Equal("ViVeTool", attribute.Name);
        Assert.Equal("1.1.4", attribute.Version);
        Assert.Equal("3.6.1", attribute.MinimumHostVersion);
    }

    [Fact]
    public void GetFeatureExtension_ReturnsPluginPage()
    {
        var plugin = new ViveToolPlugin();

        var featurePage = Assert.IsType<ViveToolPluginPage>(plugin.GetFeatureExtension());

        Assert.IsAssignableFrom<IPluginPage>(featurePage);
        Assert.Equal(string.Empty, featurePage.PageTitle);
        Assert.Equal(string.Empty, featurePage.PageIcon);
    }

    [Fact]
    public void GetSettingsPage_ReturnsPluginSettingsPage()
    {
        var plugin = new ViveToolPlugin();

        var settingsPage = Assert.IsType<ViveToolSettingsPluginPage>(plugin.GetSettingsPage());

        Assert.IsAssignableFrom<IPluginPage>(settingsPage);
        Assert.Equal(string.Empty, settingsPage.PageTitle);
        Assert.Equal(string.Empty, settingsPage.PageIcon);
    }
}
