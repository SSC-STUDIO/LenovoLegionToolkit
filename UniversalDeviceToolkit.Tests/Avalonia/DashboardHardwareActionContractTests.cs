using FluentAssertions;
using Moq;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class DashboardHardwareActionContractTests
{
    [Fact]
    public async Task KillDiscreteGpuProcesses_SuccessRefreshesTheAuthoritativeGpuState()
    {
        var refreshed = GpuState("Inactive", canKill: false, canRestart: true);
        var services = new Mock<IPlatformServices>(MockBehavior.Strict);
        services.Setup(service => service.KillDiscreteGpuProcessesAsync()).ReturnsAsync(true);
        services.Setup(service => service.GetDiscreteGpuStateAsync()).ReturnsAsync(refreshed);
        var viewModel = new DashboardPageViewModel(services.Object);

        await viewModel.KillDiscreteGpuProcessesCommand.ExecuteAsync(null);

        viewModel.DiscreteGpuState.Should().BeSameAs(refreshed);
        viewModel.GpuActionStatus.Should().NotBeNullOrWhiteSpace();
        services.Verify(service => service.KillDiscreteGpuProcessesAsync(), Times.Once);
        services.Verify(service => service.GetDiscreteGpuStateAsync(), Times.Once);
    }

    [Fact]
    public async Task RestartDiscreteGpu_FailurePreservesTheLastKnownStateWithoutRefreshing()
    {
        var services = new Mock<IPlatformServices>(MockBehavior.Strict);
        services.Setup(service => service.RestartDiscreteGpuAsync()).ReturnsAsync(false);
        var viewModel = new DashboardPageViewModel(services.Object)
        {
            DiscreteGpuState = GpuState("Active", canKill: true, canRestart: true),
        };

        await viewModel.RestartDiscreteGpuCommand.ExecuteAsync(null);

        viewModel.DiscreteGpuState.Status.Should().Be("Active");
        viewModel.GpuActionStatus.Should().NotBeNullOrWhiteSpace();
        services.Verify(service => service.RestartDiscreteGpuAsync(), Times.Once);
        services.Verify(service => service.GetDiscreteGpuStateAsync(), Times.Never);
    }

    [Fact]
    public async Task TurnOffMonitors_ReportsBothAcceptedAndRejectedOperations()
    {
        var success = new Mock<IPlatformServices>(MockBehavior.Strict);
        success.Setup(service => service.TurnOffMonitorsAsync()).ReturnsAsync(true);
        var successViewModel = new DashboardPageViewModel(success.Object);

        await successViewModel.TurnOffMonitorsCommand.ExecuteAsync(null);

        successViewModel.GpuActionStatus.Should().NotBeNullOrWhiteSpace();
        success.Verify(service => service.TurnOffMonitorsAsync(), Times.Once);

        var failure = new Mock<IPlatformServices>(MockBehavior.Strict);
        failure.Setup(service => service.TurnOffMonitorsAsync()).ReturnsAsync(false);
        var failureViewModel = new DashboardPageViewModel(failure.Object);

        await failureViewModel.TurnOffMonitorsCommand.ExecuteAsync(null);

        failureViewModel.GpuActionStatus.Should().NotBeNullOrWhiteSpace();
        failureViewModel.GpuActionStatus.Should().NotBe(successViewModel.GpuActionStatus);
        failure.Verify(service => service.TurnOffMonitorsAsync(), Times.Once);
    }

    [Fact]
    public async Task ApplyGpuOverclock_SuccessUsesTheRefreshedAuthoritativeState()
    {
        var refreshed = new GpuOverclockState(true, true, 135, 270, 200, 400);
        var services = new Mock<IPlatformServices>(MockBehavior.Strict);
        services.Setup(service => service.SetGpuOverclockAsync(true, 120, 240)).ReturnsAsync(true);
        services.Setup(service => service.GetGpuOverclockStateAsync()).ReturnsAsync(refreshed);
        var viewModel = new DashboardPageViewModel(services.Object)
        {
            GpuOverclockEnabled = true,
            GpuCoreDeltaMhz = 120,
            GpuMemoryDeltaMhz = 240,
        };

        await viewModel.ApplyGpuOverclockCommand.ExecuteAsync(null);

        viewModel.GpuOverclockState.Should().BeSameAs(refreshed);
        viewModel.GpuCoreDeltaMhz.Should().Be(135);
        viewModel.GpuMemoryDeltaMhz.Should().Be(270);
        services.Verify(service => service.SetGpuOverclockAsync(true, 120, 240), Times.Once);
        services.Verify(service => service.GetGpuOverclockStateAsync(), Times.Once);
    }

    [Fact]
    public async Task ApplyGpuOverclock_FailureRollsBackTheEditableDraft()
    {
        var persisted = new GpuOverclockState(true, false, 25, 50, 200, 400);
        var services = new Mock<IPlatformServices>(MockBehavior.Strict);
        services.Setup(service => service.SetGpuOverclockAsync(true, 120, 240)).ReturnsAsync(false);
        var viewModel = new DashboardPageViewModel(services.Object)
        {
            GpuOverclockState = persisted,
            GpuOverclockEnabled = true,
            GpuCoreDeltaMhz = 120,
            GpuMemoryDeltaMhz = 240,
        };

        await viewModel.ApplyGpuOverclockCommand.ExecuteAsync(null);

        viewModel.GpuOverclockEnabled.Should().BeFalse();
        viewModel.GpuCoreDeltaMhz.Should().Be(25);
        viewModel.GpuMemoryDeltaMhz.Should().Be(50);
        services.Verify(service => service.SetGpuOverclockAsync(true, 120, 240), Times.Once);
        services.Verify(service => service.GetGpuOverclockStateAsync(), Times.Never);
    }

    private static DiscreteGpuState GpuState(string status, bool canKill, bool canRestart) =>
        new(true, status, "P0", 1, canKill, canRestart);
}
