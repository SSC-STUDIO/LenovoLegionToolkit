using FluentAssertions;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.WPF.Pages;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginViewModelTests
{
    [Fact]
    public void SupportsOpenAction_WhenOptimizationCategoryIsAvailable_ShouldBeTrueWithoutFeaturePage()
    {
        var plugin = MockFactory.CreateMockPlugin(id: "optimization-only-plugin");
        var viewModel = new PluginViewModel(plugin, isInstalled: true)
        {
            SupportsFeaturePage = false,
            SupportsOptimizationCategory = true
        };

        viewModel.SupportsOpenAction.Should().BeTrue();
    }

    [Fact]
    public void SupportsOpenAction_WhenExecutableEntryPointIsAvailable_ShouldBeTrueWithoutHostedPages()
    {
        var plugin = MockFactory.CreateMockPlugin(id: "user-feedback");
        var viewModel = new PluginViewModel(plugin, isInstalled: true)
        {
            SupportsFeaturePage = false,
            SupportsOptimizationCategory = false,
            SupportsConfiguration = false,
            SupportsExecutableEntryPoint = true
        };

        viewModel.SupportsOpenAction.Should().BeTrue();
        viewModel.ShouldShowInstalledActions.Should().BeTrue();
    }

    [Fact]
    public void SupportsOpenAction_WhenConfigurationIsAvailable_ShouldBeTrueWithoutHostedPages()
    {
        var plugin = MockFactory.CreateMockPlugin(id: "user-feedback");
        var viewModel = new PluginViewModel(plugin, isInstalled: true)
        {
            SupportsFeaturePage = false,
            SupportsOptimizationCategory = false,
            SupportsExecutableEntryPoint = false,
            SupportsConfiguration = true
        };

        viewModel.SupportsOpenAction.Should().BeTrue();
        viewModel.ShouldShowInstalledActions.Should().BeTrue();
    }

    [Fact]
    public void ShouldNavigateToOptimizationAfterInstall_WhenOnlyOptimizationCategoryIsAvailable_ShouldReturnTrue()
    {
        var capabilities = new PluginUiCapabilities
        {
            SupportsOptimizationCategory = true
        };

        PluginExtensionsPage.ShouldNavigateToOptimizationAfterInstall(capabilities, hasExecutable: false)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void ResolveRuntimePluginCapabilities_WhenRuntimePluginProvidesOptimizationCategory_ShouldExposeOptimizationEntryPoint()
    {
        var plugin = new OptimizationOnlyRuntimePlugin();

        var capabilities = PluginExtensionsPage.ResolveRuntimePluginCapabilities(plugin);

        capabilities.SupportsSettingsPage.Should().BeFalse();
        capabilities.SupportsFeaturePage.Should().BeFalse();
        capabilities.SupportsOptimizationCategory.Should().BeTrue();
        PluginExtensionsPage.ShouldNavigateToOptimizationAfterInstall(capabilities, hasExecutable: false)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void ResolveRuntimePluginCapabilities_WhenPluginIsManifestAdapter_ShouldExposeOptimizationEntryPoint()
    {
        var manifest = new PluginManifest
        {
            Id = "manifest-only-plugin",
            Name = "Manifest Only Plugin",
            Contributes = new PluginManifestContributions
            {
                OptimizationActions =
                [
                    new PluginManifestOptimizationContribution
                    {
                        Id = "manifest-only.action",
                        Title = "Manifest-only action"
                    }
                ]
            }
        };

        var capabilities = PluginExtensionsPage.ResolveRuntimePluginCapabilities(new PluginManifestAdapter(manifest));

        capabilities.SupportsSettingsPage.Should().BeFalse();
        capabilities.SupportsFeaturePage.Should().BeFalse();
        capabilities.SupportsOptimizationCategory.Should().BeTrue();
    }

    [Fact]
    public void ResolveInstalledPluginCapabilities_WhenInstalledManifestHasSettingsPage_ShouldExposeConfiguration()
    {
        var installedManifestCapabilities = new PluginUiCapabilities
        {
            SupportsSettingsPage = true,
            SupportsOptimizationCategory = true
        };

        var capabilities = PluginExtensionsPage.ResolveInstalledPluginCapabilities(
            plugin: null,
            manifestCapabilities: default,
            installedManifestCapabilities);

        capabilities.SupportsSettingsPage.Should().BeTrue();
        capabilities.SupportsFeaturePage.Should().BeFalse();
        capabilities.SupportsOptimizationCategory.Should().BeTrue();
        PluginExtensionsPage.ShouldNavigateToOptimizationAfterInstall(capabilities, hasExecutable: false)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ResolveInstalledPluginCapabilities_WhenRuntimePluginProvidesSettingsPage_ShouldExposeConfiguration()
    {
        var plugin = new RuntimeSettingsPlugin();

        var capabilities = PluginExtensionsPage.ResolveInstalledPluginCapabilities(
            plugin,
            manifestCapabilities: default,
            installedManifestCapabilities: default);

        capabilities.SupportsSettingsPage.Should().BeTrue();
        capabilities.SupportsFeaturePage.Should().BeFalse();
        capabilities.SupportsOptimizationCategory.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void ShouldNavigateToOptimizationAfterInstall_WhenPluginHasPrimaryEntryPoint_ShouldReturnFalse(
        bool supportsFeaturePage,
        bool supportsSettingsPage,
        bool hasExecutable)
    {
        var capabilities = new PluginUiCapabilities
        {
            SupportsOptimizationCategory = true,
            SupportsFeaturePage = supportsFeaturePage,
            SupportsSettingsPage = supportsSettingsPage
        };

        PluginExtensionsPage.ShouldNavigateToOptimizationAfterInstall(capabilities, hasExecutable)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ShouldNavigateToOptimizationAfterInstall_WhenOptimizationCategoryIsMissing_ShouldReturnFalse()
    {
        var capabilities = new PluginUiCapabilities();

        PluginExtensionsPage.ShouldNavigateToOptimizationAfterInstall(capabilities, hasExecutable: false)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ResolveInstalledPluginFeedback_WhenRuntimeEntryPointExists_ShouldReportEntryAvailable()
    {
        var runtimeCapabilities = new PluginUiCapabilities
        {
            SupportsOptimizationCategory = true
        };

        PluginExtensionsPage.ResolveInstalledPluginFeedback(runtimeCapabilities, default, hasExecutable: false, runtimeMissing: false)
            .Should()
            .Be(PluginExtensionsPage.InstalledPluginFeedback.EntryAvailable);
    }

    [Fact]
    public void ResolveInstalledPluginFeedback_WhenExecutableEntryPointExists_ShouldReportEntryAvailable()
    {
        PluginExtensionsPage.ResolveInstalledPluginFeedback(default, default, hasExecutable: true, runtimeMissing: true)
            .Should()
            .Be(PluginExtensionsPage.InstalledPluginFeedback.EntryAvailable);
    }

    [Fact]
    public void ResolveInstalledPluginFeedback_WhenRuntimeMissingButManifestOnlyHasOptimizationCategory_ShouldReportEntryAvailable()
    {
        var manifestCapabilities = new PluginUiCapabilities
        {
            SupportsOptimizationCategory = true
        };

        PluginExtensionsPage.ResolveInstalledPluginFeedback(default, manifestCapabilities, hasExecutable: false, runtimeMissing: true)
            .Should()
            .Be(PluginExtensionsPage.InstalledPluginFeedback.EntryAvailable);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ResolveInstalledPluginFeedback_WhenRuntimeMissingAndManifestNeedsRuntimeUi_ShouldReportRuntimeNotLoaded(
        bool supportsFeaturePage,
        bool supportsSettingsPage)
    {
        var manifestCapabilities = new PluginUiCapabilities
        {
            SupportsOptimizationCategory = true,
            SupportsFeaturePage = supportsFeaturePage,
            SupportsSettingsPage = supportsSettingsPage
        };

        PluginExtensionsPage.ResolveInstalledPluginFeedback(default, manifestCapabilities, hasExecutable: false, runtimeMissing: true)
            .Should()
            .Be(PluginExtensionsPage.InstalledPluginFeedback.RuntimeNotLoaded);
    }

    [Fact]
    public void ResolveInstalledPluginFeedback_WhenRuntimeLoadedAndManifestHasEntryPoint_ShouldReportEntryAvailable()
    {
        var manifestCapabilities = new PluginUiCapabilities
        {
            SupportsOptimizationCategory = true
        };

        PluginExtensionsPage.ResolveInstalledPluginFeedback(default, manifestCapabilities, hasExecutable: false, runtimeMissing: false)
            .Should()
            .Be(PluginExtensionsPage.InstalledPluginFeedback.EntryAvailable);
    }

    [Fact]
    public void ResolveInstalledPluginFeedback_WhenRuntimeMissingAndNoEntryPoint_ShouldReportRuntimeNotLoaded()
    {
        PluginExtensionsPage.ResolveInstalledPluginFeedback(default, default, hasExecutable: false, runtimeMissing: true)
            .Should()
            .Be(PluginExtensionsPage.InstalledPluginFeedback.RuntimeNotLoaded);
    }

    [Fact]
    public void ResolveInstalledPluginFeedback_WhenRuntimeLoadedButNoEntryPoint_ShouldReportNoUserFacingEntry()
    {
        PluginExtensionsPage.ResolveInstalledPluginFeedback(default, default, hasExecutable: false, runtimeMissing: false)
            .Should()
            .Be(PluginExtensionsPage.InstalledPluginFeedback.NoUserFacingEntry);
    }

    private sealed class OptimizationOnlyRuntimePlugin : IPlugin, IOptimizationCategoryProvider
    {
        public string Id => "runtime-optimization-plugin";
        public string Name => "Runtime Optimization Plugin";
        public string Description => "Provides a runtime optimization category.";
        public string Icon => "PlugConnected24";
        public bool IsSystemPlugin => false;
        public string[]? Dependencies => null;

        public WindowsOptimizationCategoryDefinition? GetOptimizationCategory() =>
            new(
                "runtime-optimization",
                "RuntimeOptimization_Title",
                "RuntimeOptimization_Description",
                [
                    new WindowsOptimizationActionDefinition(
                        "runtime-optimization.action",
                        "RuntimeOptimizationAction_Title",
                        "RuntimeOptimizationAction_Description",
                        _ => Task.CompletedTask)
                ],
                Id);

        public void OnInstalled() { }
        public void OnUninstalled() { }
        public void OnShutdown() { }
        public void Stop() { }
    }

    private sealed class RuntimeSettingsPlugin : PluginBase
    {
        public override string Id => "runtime-settings-plugin";
        public override string Name => "Runtime Settings Plugin";
        public override string Description => "Provides a runtime settings page.";
        public override string Icon => "PlugConnected24";
        public override bool IsSystemPlugin => false;

        public override object? GetSettingsPage() => new object();
    }
}
