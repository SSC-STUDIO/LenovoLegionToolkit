using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaPluginStartupContractTests
{
    [Fact]
    public void AvaloniaStartup_ShouldLoadPluginsAndRefreshNavigationLikeWpf()
    {
        var root = RepositoryPaths.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var mainWindow = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml.cs"));
        var culture = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Localization",
            "AvaloniaPluginResourceCulture.cs"));

        app.Should().Contain("StartWindowsHostServicesAsync(desktop.MainWindow as MainWindow)");
        app.Should().Contain("pluginManager.PruneRetiredPlugins()");
        app.Should().Contain("pluginManager.ScanAndLoadPluginsAsync()");
        app.Should().Contain("AvaloniaPluginResourceCulture.Apply(LocalizationRuntime.CurrentCulture)");
        app.Should().Contain("AvaloniaPluginResourceCulture.Apply(e.Culture)");
        app.Should().Contain("mainWindow.RefreshPluginNavigationAsync()");
        mainWindow.Should().Contain("PluginStateChanged += PluginManagerOnPluginStateChanged");
        mainWindow.Should().Contain("PluginStateChanged -= PluginManagerOnPluginStateChanged");
        culture.Should().Contain("PluginPaths.GetPluginsDirectory()");
        culture.Should().Contain("ResourceManager");
    }
}
