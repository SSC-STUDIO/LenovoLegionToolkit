using System.Reflection;
using UniversalDeviceToolkit.Plugins.SDK;
using UniversalDeviceToolkit.Plugins.TestCommon;
using UniversalDeviceToolkit.Plugins.ViveTool;
using UniversalDeviceToolkit.Plugins.ViveTool.Resources;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

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
        Assert.Equal("1.2.3", attribute.Version);
        Assert.Equal("3.6.1", attribute.MinimumHostVersion);
    }

    [Fact]
    public void GetFeatureExtension_ReturnsPluginPage()
    {
        var plugin = new ViveToolPlugin();

        PluginPageAssertions.AssertPluginPage<ViveToolPluginPage>(
            plugin.GetFeatureExtension(),
            Resource.ViveTool_PageTitle,
            "Code24");
    }

    [Fact]
    public void GetSettingsPage_ReturnsPluginSettingsPage()
    {
        var plugin = new ViveToolPlugin();

        PluginPageAssertions.AssertPluginPage<ViveToolSettingsPluginPage>(
            plugin.GetSettingsPage(),
            Resource.ViveTool_BinaryPathTitle,
            "Settings24");
    }
}
