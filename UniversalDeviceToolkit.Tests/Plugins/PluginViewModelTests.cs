using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
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
}
