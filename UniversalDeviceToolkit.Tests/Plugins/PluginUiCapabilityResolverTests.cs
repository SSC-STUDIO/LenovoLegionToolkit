using System;
using System.IO;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginUiCapabilityResolverTests : TemporaryFileTestBase
{
    private readonly string? _originalAppDataOverride;

    public PluginUiCapabilityResolverTests()
    {
        _originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
    }

    public override void Dispose()
    {
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _originalAppDataOverride);
        base.Dispose();
    }

    [Fact]
    public void ResolveFromInstalledManifest_WhenPluginIsInstalledUnderLocalDirectory_ShouldReadCapabilities()
    {
        var pluginId = "local-plugin";
        var pluginDirectory = Path.Combine(PluginPaths.GetPluginsDirectory(), "local", pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "local-plugin",
              "contributes": {
                "settingsPage": {
                  "class": "Plugin.Settings",
                  "title": "Settings"
                },
                "optimizationActions": [
                  {
                    "id": "local.action",
                    "title": "Local action"
                  }
                ]
              }
            }
            """);

        var capabilities = PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId);

        capabilities.SupportsSettingsPage.Should().BeTrue();
        capabilities.SupportsOptimizationCategory.Should().BeTrue();
        capabilities.SupportsFeaturePage.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromInstalledManifest_WhenOptimizationFlagHasNoActions_ShouldNotReportOptimizationCategory()
    {
        var pluginId = "flag-only-plugin";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "flag-only-plugin",
              "hasOptimizationCategory": true,
              "contributes": {
                "settingsPage": {
                  "class": "Plugin.Settings",
                  "title": "Settings"
                }
              }
            }
            """);

        var capabilities = PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId);

        capabilities.SupportsSettingsPage.Should().BeTrue();
        capabilities.SupportsOptimizationCategory.Should().BeFalse();
    }
}
