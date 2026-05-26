using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using Xunit;
using Moq;

namespace UniversalDeviceToolkit.Tests.Controllers;

[Trait("Category", TestCategories.Controller)]
public class GPUControllerTests : UnitTestBase
{
    private Mock<IGPUProcessManager> _processManagerMock = null!;
    private Mock<IGPUHardwareManager> _hardwareManagerMock = null!;
    private GPUController _controller = null!;

    protected override void Setup()
    {
        _processManagerMock = new Mock<IGPUProcessManager>(MockBehavior.Loose);
        _hardwareManagerMock = new Mock<IGPUHardwareManager>(MockBehavior.Loose);
        _controller = new GPUController(_processManagerMock.Object, _hardwareManagerMock.Object);
    }

    protected override void Cleanup()
    {
        _controller?.Dispose();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        _controller.Should().NotBeNull();
        _controller.IsStarted.Should().BeFalse();
    }

    [Fact]
    public void IsStarted_WhenNotStarted_ShouldReturnFalse()
    {
        _controller.IsStarted.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_WhenCalled_ShouldSetIsStartedToTrue()
    {
        await _controller.StartAsync(delay: 100, interval: 5000);

        _controller.IsStarted.Should().BeTrue();

        await _controller.StopAsync(waitForFinish: false);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_ShouldNotStartAgain()
    {
        await _controller.StartAsync(delay: 1000, interval: 5000);
        var firstStartTask = _controller.StartAsync(delay: 1000, interval: 5000);

        firstStartTask.IsCompleted.Should().BeTrue();
        _controller.IsStarted.Should().BeTrue();

        await _controller.StopAsync(waitForFinish: false);
    }

    [Fact]
    public async Task StopAsync_WhenCalled_ShouldSetIsStartedToFalse()
    {
        await _controller.StartAsync(delay: 100, interval: 5000);
        await _controller.StopAsync(waitForFinish: false);

        _controller.IsStarted.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        var act = async () => await _controller.StopAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetLastKnownStateAsync_WhenNotStarted_ShouldReturnUnknown()
    {
        var state = await _controller.GetLastKnownStateAsync();

        state.Should().Be(GPUState.Unknown);
    }

    [Fact]
    public async Task RestartGPUAsync_WhenStateIsUnknown_ShouldNotCallHardwareManager()
    {
        await _controller.RestartGPUAsync();

        _hardwareManagerMock.Verify(
            m => m.RestartGPUAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task KillGPUProcessesAsync_WhenStateIsUnknown_ShouldNotCallProcessManager()
    {
        await _controller.KillGPUProcessesAsync();

        _processManagerMock.Verify(
            m => m.KillGPUProcessesAsync(It.IsAny<IEnumerable<Process>>()),
            Times.Never);
    }

    [Fact]
    public void Dispose_WhenCalled_ShouldNotThrow()
    {
        var act = () => _controller.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        var act = () =>
        {
            _controller.Dispose();
            _controller.Dispose();
            _controller.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public async Task StartAndStop_MultipleCycles_ShouldWorkCorrectly()
    {
        for (int i = 0; i < 3; i++)
        {
            await _controller.StartAsync(delay: 50, interval: 1000);
            _controller.IsStarted.Should().BeTrue();

            await Task.Delay(100);

            await _controller.StopAsync(waitForFinish: false);
            _controller.IsStarted.Should().BeFalse();
        }
    }

    #region Extended Edge Case Tests

    [Fact(Skip = "Requires NVIDIA GPU/NVAPI (Windows only)")]
    [Trait("Requires", "NVAPI")]
    public async Task StopAsync_WithWaitForFinish_ShouldComplete()
    {
        await _controller.StartAsync(delay: 50, interval: 5000);
        _controller.IsStarted.Should().BeTrue();

        var act = async () => await _controller.StopAsync(waitForFinish: true);

        await act.Should().NotThrowAsync();
        _controller.IsStarted.Should().BeFalse();
    }

    [Fact(Skip = "Requires NVIDIA GPU/NVAPI (Windows only)")]
    [Trait("Requires", "NVAPI")]
    public async Task RefreshNowAsync_WhenNotStarted_ShouldReturnUnknownAndNotCrash()
    {
        GPUStatus? result = null;
        var act = async () => { result = await _controller.RefreshNowAsync(); };

        // Note: NVAPI native libraries must be available for this test to pass.
        // In environments without NVIDIA hardware/NVAPI, this may throw.
        await act.Should().NotThrowAsync();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RestartGPUAsync_WhenStateIsNotActiveOrInactive_ShouldNotCallHardwareManager()
    {
        var stateField = typeof(GPUController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        stateField!.SetValue(_controller, GPUState.PoweredOff);

        await _controller.RestartGPUAsync();

        _hardwareManagerMock.Verify(
            m => m.RestartGPUAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task KillGPUProcessesAsync_WhenStateIsNotActive_ShouldNotCallProcessManager()
    {
        var stateField = typeof(GPUController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        stateField!.SetValue(_controller, GPUState.Inactive);

        await _controller.KillGPUProcessesAsync();

        _processManagerMock.Verify(
            m => m.KillGPUProcessesAsync(It.IsAny<IEnumerable<Process>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLastKnownStateAsync_AfterConstructor_ShouldReturnUnknown()
    {
        var state = await _controller.GetLastKnownStateAsync();

        state.Should().Be(GPUState.Unknown);
    }

    #endregion
}
