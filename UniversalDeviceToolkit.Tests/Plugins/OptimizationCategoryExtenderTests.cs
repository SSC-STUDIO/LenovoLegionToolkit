using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class OptimizationCategoryExtenderTests : TemporaryFileTestBase
{
    private readonly string? _originalAppDataOverride;

    public OptimizationCategoryExtenderTests()
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
    public void GetPluginCategories_WhenNoInstalledPlugins_ShouldReturnEmpty()
    {
        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin>());
        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        extender.GetPluginCategories().Should().BeEmpty();
    }

    [Fact]
    public void GetPluginCategories_WhenProviderReturnsCategory_ShouldIncludeCategory()
    {
        var category = new WindowsOptimizationCategoryDefinition(
            "plugin-category",
            "title",
            "description",
            Array.Empty<WindowsOptimizationActionDefinition>());
        var provider = new TestOptimizationProvider("provider-plugin", category);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin> { provider });
        pluginManager.Setup(m => m.IsInstalled("provider-plugin")).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        categories.Should().ContainSingle(c => c.Key == "plugin-category" && c.PluginId == "provider-plugin");
    }

    [Fact]
    public void GetPluginCategories_WhenProviderThrows_ShouldSkipPlugin()
    {
        var failing = new ThrowingOptimizationProvider("failing-plugin");

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin> { failing });
        pluginManager.Setup(m => m.IsInstalled("failing-plugin")).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        extender.GetPluginCategories().Should().BeEmpty();
    }

    [Fact]
    public void GetPluginCategories_WhenInstalledManifestHasOptimizationActionsOnly_ShouldSkipNoOpManifestCategory()
    {
        var pluginId = "manifest-plugin";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "manifest-plugin",
              "name": "Manifest Plugin",
              "description": "Manifest description",
              "contributes": {
                "settingsPage": {
                  "class": "Plugin.Settings",
                  "title": "Settings"
                },
                "optimizationActions": [
                  {
                    "id": "manifest.action",
                    "title": "Manifest action"
                  }
                ]
              }
            }
            """);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin>());
        pluginManager.Setup(m => m.IsInstalled(pluginId)).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        categories.Should().BeEmpty();
    }

    [Fact]
    public void GetPluginCategories_WhenImportedPluginHasManifestActionsOnly_ShouldSkipNoOpManifestCategory()
    {
        var pluginId = "local-plugin";
        var pluginDirectory = Path.Combine(PluginPaths.GetPluginsDirectory(), "local", pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "local-plugin",
              "name": "Local Plugin",
              "description": "Local manifest description",
              "contributes": {
                "optimizationActions": [
                  {
                    "id": "local.action",
                    "title": "Local action"
                  }
                ]
              }
            }
            """);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin>());
        pluginManager.Setup(m => m.IsInstalled(pluginId)).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        categories.Should().BeEmpty();
    }

    [Fact]
    public void GetPluginCategories_WhenManifestOnlyPluginIsInInstalledList_ShouldSkipNoOpManifestCategory()
    {
        var pluginId = "manifest-only-plugin";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "manifest-only-plugin",
              "name": "Manifest Only Plugin",
              "description": "Manifest-only optimization contribution",
              "contributes": {
                "optimizationActions": [
                  {
                    "id": "manifest-only.action",
                    "title": "Manifest-only action"
                  }
                ]
              }
            }
            """);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin>());
        pluginManager.Setup(m => m.GetInstalledPluginIds()).Returns(new[] { pluginId });
        pluginManager.Setup(m => m.IsInstalled(pluginId)).Returns(false);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        categories.Should().BeEmpty();
    }

    [Fact]
    public void GetPluginCategories_WhenInstalledManifestUsesActionKeyOnly_ShouldSkipNoOpManifestCategory()
    {
        var pluginId = "manifest-key-plugin";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "manifest-key-plugin",
              "name": "Manifest Key Plugin",
              "description": "Manifest-level description",
              "contributes": {
                "optimizationActions": [
                  {
                    "key": "manifest-key.action",
                    "title": "Manifest key action",
                    "description": "Action-level description",
                    "recommended": true
                  }
                ]
              }
            }
            """);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin>());
        pluginManager.Setup(m => m.GetInstalledPluginIds()).Returns(new[] { pluginId });

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        categories.Should().BeEmpty();
    }

    [Fact]
    public void GetPluginCategories_WhenInstalledIdUsesLegacyAssemblyName_ShouldSkipNoOpManifestCategory()
    {
        var pluginDirectory = Path.Combine(PluginPaths.GetPluginsDirectory(), "LenovoLegionToolkit.Plugins.CustomMouse");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "custom-mouse",
              "name": "Custom Mouse",
              "description": "Legacy assembly directory manifest",
              "contributes": {
                "optimizationActions": [
                  {
                    "id": "custom-mouse.action",
                    "title": "Custom Mouse action"
                  }
                ]
              }
            }
            """);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin>());
        pluginManager.Setup(m => m.GetInstalledPluginIds()).Returns(new[] { "CustomMouse" });

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        categories.Should().BeEmpty();
    }

    [Fact]
    public void GetPluginCategories_WhenInstalledManifestHasSettingsPageOnly_ShouldSkipEmptyCategory()
    {
        var pluginId = "settings-only-plugin";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "settings-only-plugin",
              "name": "Settings Only Plugin",
              "description": "Settings-only description",
              "contributes": {
                "settingsPage": {
                  "class": "Plugin.Settings",
                  "title": "Settings"
                },
                "optimizationActions": []
              }
            }
            """);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin>());
        pluginManager.Setup(m => m.IsInstalled(pluginId)).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        categories.Should().BeEmpty();
    }

    [Fact]
    public void WindowsOptimizationCategoryDefinition_ShouldPreserveLegacyFiveArgumentConstructor()
    {
        var actions = Array.Empty<WindowsOptimizationActionDefinition>();

        var category = new WindowsOptimizationCategoryDefinition(
            "legacy-category",
            "title",
            "description",
            actions,
            "legacy-plugin");

        category.Key.Should().Be("legacy-category");
        category.PluginId.Should().Be("legacy-plugin");
        category.ResourceAnchorType.Should().BeNull();
        category.Actions.Should().BeSameAs(actions);
    }

    private sealed class TestOptimizationProvider(string id, WindowsOptimizationCategoryDefinition category) : IPlugin, IOptimizationCategoryProvider
    {
        public string Id => id;
        public string Name => id;
        public string Description => "test";
        public string Icon => "PlugConnected24";
        public bool IsSystemPlugin => false;
        public string[]? Dependencies => null;

        public WindowsOptimizationCategoryDefinition? GetOptimizationCategory() => category;

        public void OnInstalled() { }
        public void OnUninstalled() { }
        public void OnShutdown() { }
        public void Stop() { }
    }

    private sealed class ThrowingOptimizationProvider(string id) : IPlugin, IOptimizationCategoryProvider
    {
        public string Id => id;
        public string Name => id;
        public string Description => "test";
        public string Icon => "PlugConnected24";
        public bool IsSystemPlugin => false;
        public string[]? Dependencies => null;

        public WindowsOptimizationCategoryDefinition? GetOptimizationCategory() => throw new InvalidOperationException("boom");

        public void OnInstalled() { }
        public void OnUninstalled() { }
        public void OnShutdown() { }
        public void Stop() { }
    }
}
