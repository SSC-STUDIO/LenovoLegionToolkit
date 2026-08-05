using FluentAssertions;
using UniversalDeviceToolkit.Avalonia;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class PluginSettingsRouteContractTests
{
    [Fact]
    public void PluginSettingsRoute_RoundTripsPluginId()
    {
        var route = MainNavigation.CreatePluginSettingsRoute("demo.plugin");

        MainNavigation.TryGetPluginSettingsId(route, out var pluginId).Should().BeTrue();
        pluginId.Should().Be("demo.plugin");
        MainNavigation.IsKnown(route).Should().BeTrue();
        MainNavigation.TryGetPluginId(route, out _).Should().BeFalse();
    }

    [Fact]
    public async Task UnavailableHost_ReportsPluginSettingsCompatibilityState()
    {
        var state = await new UnavailablePlatformServices()
            .GetPluginSettingsPageStateAsync("demo.plugin");

        state.PluginId.Should().Be("demo.plugin");
        state.IsInstalled.Should().BeFalse();
        state.HasFeaturePage.Should().BeFalse();
        state.IsAvaloniaPage.Should().BeFalse();
        state.StatusMessage.Should().NotBeNullOrWhiteSpace();
    }
}
