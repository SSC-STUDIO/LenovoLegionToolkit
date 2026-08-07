using FluentAssertions;
using UniversalDeviceToolkit.Avalonia;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class MainNavigationRouteTests
{
    [Theory]
    [InlineData(MainNavigation.Dashboard)]
    [InlineData(MainNavigation.Keyboard)]
    [InlineData(MainNavigation.Actions)]
    [InlineData(MainNavigation.Macro)]
    [InlineData(MainNavigation.WindowsOptimization)]
    [InlineData(MainNavigation.PluginExtensions)]
    [InlineData(MainNavigation.Settings)]
    [InlineData(MainNavigation.About)]
    public void StaticRoutes_AreKnownCaseInsensitively(string route)
    {
        MainNavigation.IsKnown(route.ToUpperInvariant()).Should().BeTrue();
    }

    [Fact]
    public void PluginFeatureAndSettingsRoutes_RoundTripTrimmedIdsWithoutCrossMatching()
    {
        var featureRoute = MainNavigation.CreatePluginRoute("  sample.plugin  ");
        var settingsRoute = MainNavigation.CreatePluginSettingsRoute("  sample.plugin  ");

        featureRoute.Should().Be("plugin:sample.plugin");
        settingsRoute.Should().Be("plugin-settings:sample.plugin");
        MainNavigation.TryGetPluginId(featureRoute, out var featureId).Should().BeTrue();
        MainNavigation.TryGetPluginSettingsId(settingsRoute, out var settingsId).Should().BeTrue();
        MainNavigation.TryGetPluginId(settingsRoute, out _).Should().BeFalse();
        MainNavigation.TryGetPluginSettingsId(featureRoute, out _).Should().BeFalse();
        featureId.Should().Be("sample.plugin");
        settingsId.Should().Be("sample.plugin");
        MainNavigation.IsKnown(featureRoute).Should().BeTrue();
        MainNavigation.IsKnown(settingsRoute).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    [InlineData("plugin:")]
    [InlineData("plugin-settings:   ")]
    public void UnknownAndIncompleteRoutes_AreNotKnown(string? route)
    {
        MainNavigation.IsKnown(route).Should().BeFalse();
        MainNavigation.TryGetPluginId(route, out _).Should().BeFalse();
        MainNavigation.TryGetPluginSettingsId(route, out _).Should().BeFalse();
    }
}
