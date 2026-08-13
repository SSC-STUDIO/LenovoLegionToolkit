using System;
using System.IO;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
[Collection(TestCollections.ProcessState)]
public class PluginUiCapabilityResolverTests : TemporaryFileTestBase
{
    private readonly string? _originalAppDataOverride;

    public PluginUiCapabilityResolverTests()
    {
        _originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        PluginUiCapabilityResolver.InvalidateCache();
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
        capabilities.SupportsWebPage.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromInstalledManifest_WhenManifestHasWebPage_ShouldReportWebPage()
    {
        var pluginId = "web-plugin";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "web-plugin",
              "contributes": {
                "webPage": {
                  "entry": "web/index.html"
                }
              }
            }
            """);

        var capabilities = PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId);

        capabilities.SupportsWebPage.Should().BeTrue();
        capabilities.SupportsSettingsPage.Should().BeFalse();
        capabilities.HasAny.Should().BeTrue();
    }

    [Fact]
    public void ResolveFromInstalledManifest_WhenManifestHasOptimizationActionsOnly_ShouldReportOptimizationCategory()
    {
        var pluginId = "optimization-only-plugin";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "optimization-only-plugin",
              "contributes": {
                "optimizationActions": [
                  {
                    "id": "optimization-only.action",
                    "title": "Optimization-only action"
                  }
                ]
              }
            }
            """);

        var capabilities = PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId);

        capabilities.SupportsSettingsPage.Should().BeFalse();
        capabilities.SupportsFeaturePage.Should().BeFalse();
        capabilities.SupportsOptimizationCategory.Should().BeTrue();
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

    [Fact]
    public void ResolveFromInstalledManifest_WhenLocalDirectoryNameDiffersFromManifestId_ShouldReadCapabilities()
    {
        var pluginId = "shell-integration";
        var pluginDirectory = Path.Combine(PluginPaths.GetPluginsDirectory(), "local", "UniversalDeviceToolkit.Plugins.ShellIntegration");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "shell-integration",
              "contributes": {
                "optimizationActions": [
                  {
                    "id": "shell.action",
                    "title": "Shell action"
                  }
                ]
              }
            }
            """);

        var capabilities = PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId);

        capabilities.SupportsOptimizationCategory.Should().BeTrue();
    }
}
