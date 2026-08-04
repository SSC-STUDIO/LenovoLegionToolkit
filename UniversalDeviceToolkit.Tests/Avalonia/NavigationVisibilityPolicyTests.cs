using FluentAssertions;
using UniversalDeviceToolkit.Avalonia;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class NavigationVisibilityPolicyTests
{
    [Fact]
    public void Entries_RepresentEveryOptionalShellRoute()
    {
        NavigationVisibilityPolicy.Entries.Select(entry => entry.Route)
            .Should().BeEquivalentTo(
                [
                    MainNavigation.Keyboard,
                    MainNavigation.Actions,
                    MainNavigation.Macro,
                    MainNavigation.WindowsOptimization,
                    MainNavigation.PluginExtensions,
                    MainNavigation.About,
                ],
                options => options.WithStrictOrdering());
    }

    [Fact]
    public void IsVisible_UsesPersistedKeyAndDefaultsMissingEntriesToVisible()
    {
        var settings = new Dictionary<string, bool>
        {
            ["automation"] = false,
            ["pluginExtensions"] = true,
        };

        NavigationVisibilityPolicy.IsVisible(MainNavigation.Actions, settings).Should().BeFalse();
        NavigationVisibilityPolicy.IsVisible(MainNavigation.PluginExtensions, settings).Should().BeTrue();
        NavigationVisibilityPolicy.IsVisible(MainNavigation.Macro, settings).Should().BeTrue();
        NavigationVisibilityPolicy.IsVisible(MainNavigation.Dashboard, settings).Should().BeTrue();
    }
}
