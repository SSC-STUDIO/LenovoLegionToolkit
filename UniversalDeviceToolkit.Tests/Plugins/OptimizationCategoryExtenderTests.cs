using System;
using System.Collections.Generic;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Plugins;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class OptimizationCategoryExtenderTests
{
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
