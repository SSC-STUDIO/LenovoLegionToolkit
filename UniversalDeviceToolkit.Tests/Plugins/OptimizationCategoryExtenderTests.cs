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
    public void GetPluginCategories_WhenRuntimePluginIdDiffersFromInstalledManifestId_ShouldIncludeProviderCategory()
    {
        var installedPluginId = "shell-integration";
        var runtimePluginId = "LenovoLegionToolkit.Plugins.ShellIntegration";
        var pluginDirectory = PluginPaths.GetPluginDirectory(installedPluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginFilePath = Path.Combine(pluginDirectory, "LenovoLegionToolkit.Plugins.ShellIntegration.dll");
        File.WriteAllText(pluginFilePath, string.Empty);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "shell-integration",
              "name": "Shell Integration",
              "contributes": {
                "optimizationActions": []
              }
            }
            """);

        var category = new WindowsOptimizationCategoryDefinition(
            "shell.category",
            "Shell category",
            "Shell category description",
            Array.Empty<WindowsOptimizationActionDefinition>(),
            runtimePluginId);
        var provider = new TestOptimizationProvider(runtimePluginId, category);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin> { provider });
        pluginManager.Setup(m => m.GetPluginMetadata(runtimePluginId)).Returns(new PluginMetadata
        {
            Id = runtimePluginId,
            FilePath = pluginFilePath
        });
        pluginManager.Setup(m => m.GetInstalledPluginIds()).Returns(new[] { installedPluginId });
        pluginManager.Setup(m => m.IsInstalled(runtimePluginId)).Returns(false);
        pluginManager.Setup(m => m.IsInstalled(installedPluginId)).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        var result = categories.Should().ContainSingle().Subject;
        result.Key.Should().Be("shell.category");
        result.PluginId.Should().Be(installedPluginId);
    }

    [Fact]
    public void GetPluginCategories_WhenPluginUsesPublicConventionMethod_ShouldIncludeCategory()
    {
        var plugin = new ConventionOptimizationPlugin("convention-plugin");

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin> { plugin });
        pluginManager.Setup(m => m.IsInstalled("convention-plugin")).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        var category = categories.Should().ContainSingle().Subject;
        category.Key.Should().Be("convention.category");
        category.PluginId.Should().Be("convention-plugin");
        category.Actions.Should().ContainSingle();
        category.Actions[0].Key.Should().Be("convention.action");
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
    public void GetPluginCategories_WhenInstalledManifestHasOptimizationActionsOnly_ShouldIncludeManifestCategory()
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
        pluginManager.Setup(m => m.GetInstalledPluginIds()).Returns(new[] { pluginId });
        pluginManager.Setup(m => m.IsInstalled(pluginId)).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        AssertManifestCategory(categories, pluginId, "Manifest Plugin", "Manifest description", "manifest.action", "Manifest action");
    }

    [Fact]
    public void GetPluginCategories_WhenRuntimePluginIdDiffersAndManifestHasActions_ShouldIncludeManifestCategory()
    {
        var installedPluginId = "optimization-pack";
        var runtimePluginId = "Vendor.OptimizationPack.Runtime";
        var pluginDirectory = PluginPaths.GetPluginDirectory(installedPluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginFilePath = Path.Combine(pluginDirectory, "Vendor.OptimizationPack.Runtime.dll");
        File.WriteAllText(pluginFilePath, string.Empty);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "optimization-pack",
              "name": "Optimization Pack",
              "description": "Runtime id mismatch manifest",
              "contributes": {
                "optimizationActions": [
                  {
                    "id": "optimization-pack.action",
                    "title": "Optimization action"
                  }
                ]
              }
            }
            """);

        var plugin = new BasicPlugin(runtimePluginId);
        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin> { plugin });
        pluginManager.Setup(m => m.GetPluginMetadata(runtimePluginId)).Returns(new PluginMetadata
        {
            Id = runtimePluginId,
            FilePath = pluginFilePath
        });
        pluginManager.Setup(m => m.GetInstalledPluginIds()).Returns(new[] { installedPluginId });
        pluginManager.Setup(m => m.IsInstalled(runtimePluginId)).Returns(false);
        pluginManager.Setup(m => m.IsInstalled(installedPluginId)).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        AssertManifestCategory(
            categories,
            installedPluginId,
            "Optimization Pack",
            "Runtime id mismatch manifest",
            "optimization-pack.action",
            "Optimization action");
    }

    [Fact]
    public void GetPluginCategories_WhenImportedPluginHasManifestActionsOnly_ShouldIncludeManifestCategory()
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
        pluginManager.Setup(m => m.GetInstalledPluginIds()).Returns(new[] { pluginId });
        pluginManager.Setup(m => m.IsInstalled(pluginId)).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        AssertManifestCategory(categories, pluginId, "Local Plugin", "Local manifest description", "local.action", "Local action");
    }

    [Fact]
    public void GetPluginCategories_WhenManifestOnlyPluginIsInInstalledList_ShouldIncludeManifestCategory()
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

        AssertManifestCategory(categories, pluginId, "Manifest Only Plugin", "Manifest-only optimization contribution", "manifest-only.action", "Manifest-only action");
    }

    [Fact]
    public void GetPluginCategories_WhenInstalledIdMatchesManifestButDirectoryNameDiffers_ShouldIncludeManifestCategory()
    {
        var pluginId = "shell-integration";
        var pluginDirectory = Path.Combine(PluginPaths.GetPluginsDirectory(), "local", "LenovoLegionToolkit.Plugins.ShellIntegration");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "shell-integration",
              "name": "Shell Integration",
              "description": "Shell manifest optimization contribution",
              "contributes": {
                "optimizationActions": [
                  {
                    "id": "shell-integration.action",
                    "title": "Shell integration action"
                  }
                ]
              }
            }
            """);

        var pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(m => m.GetRegisteredPlugins()).Returns(new List<IPlugin>());
        pluginManager.Setup(m => m.GetInstalledPluginIds()).Returns(new[] { pluginId });
        pluginManager.Setup(m => m.IsInstalled(pluginId)).Returns(true);

        var extender = new OptimizationCategoryExtender(pluginManager.Object);

        var categories = extender.GetPluginCategories();

        AssertManifestCategory(categories, pluginId, "Shell Integration", "Shell manifest optimization contribution", "shell-integration.action", "Shell integration action");
    }

    [Fact]
    public void GetPluginCategories_WhenInstalledManifestUsesActionKeyOnly_ShouldIncludeManifestCategory()
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

        var category = AssertManifestCategory(
            categories,
            pluginId,
            "Manifest Key Plugin",
            "Manifest-level description",
            "manifest-key.action",
            "Manifest key action",
            recommended: true);
        category.Actions[0].DescriptionResourceKey.Should().Be("Action-level description");
        category.Actions[0].Recommended.Should().BeTrue();
    }

    [Fact]
    public void GetPluginCategories_WhenInstalledManifestOmitsRecommended_ShouldDefaultManifestActionToNotRecommended()
    {
        var pluginId = "manifest-default-recommended-plugin";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            """
            {
              "id": "manifest-default-recommended-plugin",
              "name": "Manifest Default Recommended Plugin",
              "description": "Manifest-level description",
              "contributes": {
                "optimizationActions": [
                  {
                    "id": "manifest-default.action",
                    "title": "Manifest default action"
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

        var category = AssertManifestCategory(categories, pluginId, "Manifest Default Recommended Plugin", "Manifest-level description", "manifest-default.action", "Manifest default action");
        category.Actions[0].Recommended.Should().BeFalse();
    }

    [Fact]
    public void GetPluginCategories_WhenInstalledIdUsesLegacyAssemblyName_ShouldIncludeManifestCategory()
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

        AssertManifestCategory(categories, "custom-mouse", "Custom Mouse", "Legacy assembly directory manifest", "custom-mouse.action", "Custom Mouse action");
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

    private static WindowsOptimizationCategoryDefinition AssertManifestCategory(
        IReadOnlyList<WindowsOptimizationCategoryDefinition> categories,
        string pluginId,
        string title,
        string description,
        string actionKey,
        string actionTitle,
        bool recommended = false)
    {
        var category = categories.Should().ContainSingle().Subject;
        category.Key.Should().Be($"plugin.{pluginId}");
        category.PluginId.Should().Be(pluginId);
        category.TitleResourceKey.Should().Be(title);
        category.DescriptionResourceKey.Should().Be(description);
        category.Actions.Should().ContainSingle();
        category.Actions[0].Key.Should().Be(actionKey);
        category.Actions[0].TitleResourceKey.Should().Be(actionTitle);
        category.Actions[0].Recommended.Should().Be(recommended);
        return category;
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

    private sealed class BasicPlugin(string id) : IPlugin
    {
        public string Id => id;
        public string Name => id;
        public string Description => "test";
        public string Icon => "PlugConnected24";
        public bool IsSystemPlugin => false;
        public string[]? Dependencies => null;

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

    private sealed class ConventionOptimizationPlugin(string id) : IPlugin
    {
        public string Id => id;
        public string Name => id;
        public string Description => "test";
        public string Icon => "PlugConnected24";
        public bool IsSystemPlugin => false;
        public string[]? Dependencies => null;

        public WindowsOptimizationCategoryDefinition GetOptimizationCategory() =>
            new(
                "convention.category",
                "Convention category",
                "Convention description",
                [
                    new WindowsOptimizationActionDefinition(
                        "convention.action",
                        "Convention action",
                        "Convention action description",
                        _ => System.Threading.Tasks.Task.CompletedTask)
                ]);

        public void OnInstalled() { }
        public void OnUninstalled() { }
        public void OnShutdown() { }
        public void Stop() { }
    }
}
