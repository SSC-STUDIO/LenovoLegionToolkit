using System.Globalization;
using UniversalDeviceToolkit.Avalonia;
using UniversalDeviceToolkit.ViewModels;
using UniversalDeviceToolkit.Abstractions.Localization;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class AvaloniaMigrationContractTests
{
    [Fact]
    public void MainNavigation_ShouldExposeEveryMigratedWpfRoute()
    {
        var routes = new[]
        {
            MainNavigation.Dashboard,
            MainNavigation.Keyboard,
            MainNavigation.Actions,
            MainNavigation.Macro,
            MainNavigation.WindowsOptimization,
            MainNavigation.PluginExtensions,
            MainNavigation.About,
            MainNavigation.Settings,
        };

        Assert.Equal(8, routes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(routes, route => Assert.True(MainNavigation.IsKnown(route)));
    }

    [Fact]
    public void SettingsNavigation_ShouldKeepAllCapabilityEntriesAndIconMappings()
    {
        var model = new SettingsNavigationViewModel(new TestLocalizer());

        model.InitializeNavigationCommand.Execute(true);

        Assert.Equal(
            ["Appearance", "Application", "SmartKeys", "Display", "Update", "Power", "Integrations"],
            model.NavigationItems.Select(item => item.Key));
        Assert.All(model.NavigationItems, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.IconIdentifier));
            Assert.DoesNotContain("24", item.Title, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SettingsNavigation_ShouldHideHardwareOnlyEntriesWhenUnsupported()
    {
        var model = new SettingsNavigationViewModel(new TestLocalizer());

        model.InitializeNavigationCommand.Execute(false);

        Assert.Equal(
            ["Appearance", "Application", "Update", "Integrations"],
            model.NavigationItems.Select(item => item.Key));
    }

    [Theory]
    [InlineData("fan-curve", "plugin:fan-curve")]
    [InlineData("  RGB-Tools  ", "plugin:RGB-Tools")]
    public void PluginRoutes_ShouldRoundTripIdsWithoutChangingRouteSemantics(
        string pluginId,
        string expectedRoute)
    {
        var route = MainNavigation.CreatePluginRoute(pluginId);

        Assert.Equal(expectedRoute, route);
        Assert.True(MainNavigation.IsKnown(route));
        Assert.True(MainNavigation.TryGetPluginId(route, out var resolvedId));
        Assert.Equal(pluginId.Trim(), resolvedId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plugin:")]
    [InlineData("plugin:   ")]
    public void PluginRoutes_ShouldRejectEmptyOrNonPluginRoutes(string? route)
    {
        Assert.False(MainNavigation.TryGetPluginId(route, out _));
        Assert.False(MainNavigation.IsKnown(route));
    }

    private sealed class TestLocalizer : IStringLocalizer
    {
        public string GetString(string key, string fallback = "") => fallback;

        public CultureInfo CurrentCulture { get; set; } = CultureInfo.InvariantCulture;
    }
}
