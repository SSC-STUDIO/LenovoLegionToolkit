using System.Reflection;
using Avalonia.Automation;
using Avalonia.Controls;
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
        Assert.Equal("1.2.4", attribute.Version);
        Assert.Equal("5.0.0", attribute.MinimumHostVersion);
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

    [Fact]
    public void FeaturePage_ProvidesAvaloniaFactoryWithoutChangingWpfFactory()
    {
        var plugin = new ViveToolPlugin();
        var page = Assert.IsType<ViveToolPluginPage>(plugin.GetFeatureExtension());

        Assert.IsType<AvaloniaViveToolPage>(page.CreateAvaloniaPage());
    }

    [Fact]
    public void SettingsPage_ProvidesAvaloniaFactoryWithoutChangingWpfFactory()
    {
        var plugin = new ViveToolPlugin();
        var page = Assert.IsType<ViveToolSettingsPluginPage>(plugin.GetSettingsPage());

        var avaloniaPage = Assert.IsType<AvaloniaViveToolSettingsPage>(page.CreateAvaloniaPage());
        Assert.Equal("AvaloniaViveToolSettingsRoot", AutomationProperties.GetAutomationId(avaloniaPage));
        Assert.IsAssignableFrom<Control>(avaloniaPage);
    }
}
