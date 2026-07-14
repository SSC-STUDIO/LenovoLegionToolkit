using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginHotReloadTests : TemporaryFileTestBase
{
    [Fact]
    public void EnableAndDisableHotReload_ShouldToggleState()
    {
        var sandbox = new Mock<IPluginSandbox>();
        var storage = CreateTempDirectory();
        using var hotReload = new PluginHotReload(sandbox.Object, storage);

        hotReload.EnableHotReload("plugin-a").Should().BeTrue();
        hotReload.IsHotReloadEnabled("plugin-a").Should().BeTrue();
        hotReload.EnableHotReload("plugin-a").Should().BeFalse();

        hotReload.DisableHotReload("plugin-a").Should().BeTrue();
        hotReload.IsHotReloadEnabled("plugin-a").Should().BeFalse();
        hotReload.DisableHotReload("plugin-a").Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldLoadSavedStatesWhenPresent()
    {
        var sandbox = new Mock<IPluginSandbox>();
        var storage = CreateTempDirectory();
        var stateDir = Path.Combine(storage, "PluginStates");
        Directory.CreateDirectory(stateDir);
        var statePath = Path.Combine(stateDir, "plugin-a.json");
        var state = new PluginStateData
        {
            PluginId = "plugin-a",
            PluginVersion = "1.0.0",
            SavedAt = DateTime.UtcNow
        };
        File.WriteAllText(statePath, JsonSerializer.Serialize(state));

        using var hotReload = new PluginHotReload(sandbox.Object, storage);

        hotReload.GetSavedState("plugin-a").Should().NotBeNull();
        hotReload.GetSavedState("plugin-a")!.PluginId.Should().Be("plugin-a");
    }

    [Fact]
    public async Task SaveAndClearSavedState_ShouldRoundTrip()
    {
        var sandbox = new Mock<IPluginSandbox>();
        var storage = CreateTempDirectory();
        using var hotReload = new PluginHotReload(sandbox.Object, storage);

        (await hotReload.SavePluginStateAsync("plugin-b")).Should().BeFalse();

        hotReload.ClearSavedState("plugin-b").Should().BeTrue();
        hotReload.GetSavedState("plugin-b").Should().BeNull();
    }
}
