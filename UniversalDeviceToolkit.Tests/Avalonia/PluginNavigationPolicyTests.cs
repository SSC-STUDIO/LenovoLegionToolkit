using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class PluginNavigationPolicyTests
{
    [Fact]
    public void VisiblePlugins_OnlyIncludesInstalledFeaturePagesAndDeduplicatesIds()
    {
        var state = new PluginCatalogState(
            true,
            string.Empty,
            [
                new PluginCatalogItem("demo", "Demo", "", null, "1", "", true, false, null, false, true, false, []),
                new PluginCatalogItem("DEMO", "Duplicate", "", null, "1", "", true, false, null, false, true, false, []),
                new PluginCatalogItem("settings-only", "Settings", "", null, "1", "", true, false, null, true, false, false, []),
                new PluginCatalogItem("not-installed", "Not installed", "", null, "1", "", false, false, null, false, true, false, []),
            ]);

        var visible = PluginNavigationPolicy.GetVisiblePlugins(state);

        visible.Select(plugin => plugin.Id).Should().Equal("demo");
    }

    [Fact]
    public void VisiblePlugins_IsEmptyWhenCatalogIsUnavailable()
    {
        var state = new PluginCatalogState(
            false,
            "unavailable",
            [new PluginCatalogItem("demo", "Demo", "", null, "1", "", true, false, null, false, true, false, [])]);

        PluginNavigationPolicy.GetVisiblePlugins(state).Should().BeEmpty();
    }
}
