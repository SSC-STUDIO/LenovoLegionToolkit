using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Controllers;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Controllers;

[Trait("Category", TestCategories.Controller)]
public class GPUHardwareManagerEdgeCaseTests : UnitTestBase
{
    private GPUHardwareManager _manager = null!;

    protected override void Setup()
    {
        _manager = new GPUHardwareManager();
    }

    [Fact]
    public async Task RestartGPUAsync_WithNullInstanceId_ShouldNotThrow()
    {
        var act = async () => await _manager.RestartGPUAsync(null!);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RestartGPUAsync_WithEmptyInstanceId_ShouldNotThrow()
    {
        var act = async () => await _manager.RestartGPUAsync(string.Empty);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RestartGPUAsync_WithWhitespaceInstanceId_ShouldNotThrow()
    {
        var act = async () => await _manager.RestartGPUAsync("   ");

        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires pnputil (Windows only)")]
    public async Task RestartGPUAsync_WithValidInstanceIdFormat_ShouldHandleGracefully()
    {
        // pnputil will fail in test environment, but the method should complete
        // via the exception path rather than throwing up
        var act = async () => await _manager.RestartGPUAsync("PCI\\VEN_10DE&DEV_1F95&SUBSYS_17AA38A9&REV_A1\\4&2B6F1C0&0&00E6");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RestartGPUAsync_WithMinimalInstanceId_ShouldNotThrow()
    {
        var act = async () => await _manager.RestartGPUAsync("A");

        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires pnputil (Windows only)")]
    public async Task RestartGPUAsync_WithSpecialCharsInstanceId_ShouldNotThrow()
    {
        var act = async () => await _manager.RestartGPUAsync("TEST&ID_123&REV_A1");

        await act.Should().NotThrowAsync();
    }
}
