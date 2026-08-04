using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class PluginCatalogContractTests
{
    [Fact]
    public async Task UnavailableHost_ReportsCatalogUnavailableWithoutEntries()
    {
        var service = new UnavailablePlatformServices();

        var state = await service.GetPluginCatalogAsync();

        state.IsAvailable.Should().BeFalse();
        state.Plugins.Should().BeEmpty();
        (await service.InstallPluginAsync("missing-plugin")).Should().BeFalse();
        (await service.UpdatePluginAsync("missing-plugin")).Should().BeFalse();
    }
}
