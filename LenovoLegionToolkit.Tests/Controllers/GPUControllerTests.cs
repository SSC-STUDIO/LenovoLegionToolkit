using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LenovoLegionToolkit.Tests.Controllers;

[TestClass]
[TestCategory(TestCategories.Controller)]
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

    [TestMethod]
    public void Constructor_ShouldInitializeCorrectly()
    {
        _controller.Should().NotBeNull();
        _controller.IsStarted.Should().BeFalse();
    }

    [TestMethod]
    public void IsStarted_WhenNotStarted_ShouldReturnFalse()
    {
        _controller.IsStarted.Should().BeFalse();
    }

    [TestMethod]
    public async Task StartAsync_WhenCalled_ShouldSetIsStartedToTrue()
    {
        await _controller.StartAsync(delay: 100, interval: 5000);

        _controller.IsStarted.Should().BeTrue();

        await _controller.StopAsync(waitForFinish: false);
    }

    [TestMethod]
    public async Task StartAsync_WhenAlreadyStarted_ShouldNotStartAgain()
    {
        await _controller.StartAsync(delay: 1000, interval: 5000);
        var firstStartTask = _controller.StartAsync(delay: 1000, interval: 5000);

        firstStartTask.IsCompleted.Should().BeTrue();
        _controller.IsStarted.Should().BeTrue();

        await _controller.StopAsync(waitForFinish: false);
    }

    [TestMethod]
    public async Task StopAsync_WhenCalled_ShouldSetIsStartedToFalse()
    {
        await _controller.StartAsync(delay: 100, interval: 5000);
        await _controller.StopAsync(waitForFinish: false);

        _controller.IsStarted.Should().BeFalse();
    }

    [TestMethod]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        var act = async () => await _controller.StopAsync();

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task GetLastKnownStateAsync_WhenNotStarted_ShouldReturnUnknown()
    {
        var state = await _controller.GetLastKnownStateAsync();

        state.Should().Be(GPUState.Unknown);
    }

    [TestMethod]
    public async Task RestartGPUAsync_WhenStateIsUnknown_ShouldNotCallHardwareManager()
    {
        await _controller.RestartGPUAsync();

        _hardwareManagerMock.Verify(
            m => m.RestartGPUAsync(It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task KillGPUProcessesAsync_WhenStateIsUnknown_ShouldNotCallProcessManager()
    {
        await _controller.KillGPUProcessesAsync();

        _processManagerMock.Verify(
            m => m.KillGPUProcessesAsync(It.IsAny<IEnumerable<Process>>()),
            Times.Never);
    }

    [TestMethod]
    public void Dispose_WhenCalled_ShouldNotThrow()
    {
        var act = () => _controller.Dispose();

        act.Should().NotThrow();
    }

    [TestMethod]
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

    [TestMethod]
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
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class GPUProcessManagerTests : UnitTestBase
{
    private GPUProcessManager _processManager = null!;

    protected override void Setup()
    {
        _processManager = new GPUProcessManager();
    }

    [TestMethod]
    public async Task KillGPUProcessesAsync_WithEmptyList_ShouldNotThrow()
    {
        var processes = new List<Process>();

        var act = async () => await _processManager.KillGPUProcessesAsync(processes);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public void KillGPUProcessesAsync_WithNull_ShouldThrow()
    {
        var act = async () => await _processManager.KillGPUProcessesAsync(null!);

        act.Should().ThrowAsync<ArgumentNullException>();
    }
}
