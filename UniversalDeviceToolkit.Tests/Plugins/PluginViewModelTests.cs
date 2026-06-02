using FluentAssertions;
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
}
