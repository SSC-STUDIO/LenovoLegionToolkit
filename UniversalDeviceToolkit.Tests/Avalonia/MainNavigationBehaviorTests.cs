using FluentAssertions;
using UniversalDeviceToolkit.Avalonia;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class MainNavigationBehaviorTests
{
    [Theory]
    [InlineData(MainNavigation.Dashboard)]
    [InlineData(MainNavigation.Keyboard)]
    [InlineData(MainNavigation.Actions)]
    [InlineData(MainNavigation.Macro)]
    [InlineData(MainNavigation.WindowsOptimization)]
    [InlineData(MainNavigation.PluginExtensions)]
    [InlineData(MainNavigation.About)]
    [InlineData(MainNavigation.Settings)]
    public void IsKnown_AcceptsEveryDesktopRouteRegardlessOfCasing(string route)
    {
        MainNavigation.IsKnown(route.ToUpperInvariant()).Should().BeTrue();
    }

    [Fact]
    public void IsKnown_AcceptsPluginRoutesAndRejectsBlankOrUnknownRoutes()
    {
        MainNavigation.IsKnown(MainNavigation.CreatePluginRoute("demo.plugin")).Should().BeTrue();
        MainNavigation.IsKnown(MainNavigation.CreatePluginSettingsRoute("demo.plugin")).Should().BeTrue();
        MainNavigation.IsKnown(" ").Should().BeFalse();
        MainNavigation.IsKnown("unsupported-route").Should().BeFalse();
    }
}
