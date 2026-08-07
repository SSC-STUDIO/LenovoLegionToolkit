using FluentAssertions;
using Moq;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class SettingsNavigationViewModelTests
{
    [Fact]
    public async Task Initialization_OnSupportedMachine_MapsEverySettingsViewAndTracksSelection()
    {
        var platform = new Mock<IPlatformServices>(MockBehavior.Strict);
        platform.Setup(service => service.IsSupportedLegionMachineAsync()).ReturnsAsync(true);
        var requestedKeys = new List<string>();
        var viewModel = new SettingsPageViewModel(
            platform.Object,
            key =>
            {
                requestedKeys.Add(key);
                return $"view:{key}";
            });

        await viewModel.Initialization;

        viewModel.NavigationItems.Select(item => item.Key).Should().Equal(
            "Appearance",
            "Application",
            "SmartKeys",
            "Display",
            "Update",
            "Power",
            "Integrations");
        viewModel.SelectedNavigationIndex.Should().Be(0);
        viewModel.SelectedContent.Should().Be("view:Appearance");

        viewModel.SelectedNavigationIndex = 5;

        viewModel.SelectedContent.Should().Be("view:Power");
        requestedKeys.Should().Equal("Appearance", "Power");
    }

    [Fact]
    public async Task Initialization_OnUnsupportedMachine_KeepsPortableSettingsReachable()
    {
        var platform = new Mock<IPlatformServices>(MockBehavior.Strict);
        platform.Setup(service => service.IsSupportedLegionMachineAsync()).ReturnsAsync(false);
        var viewModel = new SettingsPageViewModel(platform.Object, key => key);

        await viewModel.Initialization;

        viewModel.NavigationItems.Select(item => item.Key).Should().Equal(
            "Appearance",
            "Application",
            "Update",
            "Integrations");
        viewModel.SelectedContent.Should().Be("Appearance");
    }

    [Fact]
    public async Task Initialization_WhenCapabilityDetectionFails_UsesPortableNavigationAndCanRecoverSelection()
    {
        var platform = new Mock<IPlatformServices>(MockBehavior.Strict);
        platform.Setup(service => service.IsSupportedLegionMachineAsync())
            .Returns(Task.FromException<bool>(new InvalidOperationException("host unavailable")));
        var viewModel = new SettingsPageViewModel(platform.Object, key => key);

        await viewModel.Initialization;

        viewModel.NavigationItems.Should().NotBeEmpty();
        viewModel.SelectedContent.Should().Be("Appearance");
        viewModel.SelectedNavigationIndex = 99;
        viewModel.SelectedContent.Should().BeNull();
        viewModel.SelectedNavigationIndex = 3;
        viewModel.SelectedContent.Should().Be("Integrations");
    }
}
