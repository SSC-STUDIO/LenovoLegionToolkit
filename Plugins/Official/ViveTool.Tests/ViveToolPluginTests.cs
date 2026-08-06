using System;
using System.Reflection;
using System.IO;
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

    [Fact]
    public void AvaloniaFeaturePage_PreservesWpfWarningImportProgressAndLoadingContracts()
    {
        var source = ReadAvaloniaPagesSource();

        Assert.Contains("Resource.ViveTool_WarningMessage", source);
        Assert.Contains("ViveToolFeatureGoToSettingsButton", source);
        Assert.Contains("ViveToolMissingRefreshStatusButton", source);
        Assert.Contains("ImportFeaturesFromUrlAsync", source);
        Assert.Contains("PickImportModeAsync", source);
        Assert.Contains("EstimatedViveToolDownloadBytes", source);
        Assert.Contains("AvaloniaViveToolDownloadProgressBar", source);
        Assert.Contains("_featureList.IsVisible = !busy", source);
        Assert.Contains("ViveToolEmptyStatePanel", source);
    }

    private static string ReadAvaloniaPagesSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
            {
                return File.ReadAllText(Path.Combine(
                    directory.FullName,
                    "Plugins",
                    "Official",
                    "ViveTool",
                    "AvaloniaViveToolPages.cs"));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("UniversalDeviceToolkit repository root was not found.");
    }
}
