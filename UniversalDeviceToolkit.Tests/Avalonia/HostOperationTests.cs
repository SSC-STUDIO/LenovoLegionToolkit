using FluentAssertions;
using Moq;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class HostOperationTests
{
    [Fact]
    public async Task TryExecuteAsync_ReturnsTheHostAcceptanceResult()
    {
        var services = new Mock<IPlatformServices>(MockBehavior.Strict);
        services.Setup(service => service.SetMacroEnabledAsync(true)).ReturnsAsync(true);

        var accepted = await HostOperation.TryExecuteAsync(() => services.Object.SetMacroEnabledAsync(true));

        accepted.Should().BeTrue();
        services.Verify(service => service.SetMacroEnabledAsync(true), Times.Once);
    }

    [Fact]
    public async Task TryExecuteAsync_PreservesAnExplicitHostRejection()
    {
        var services = new Mock<IPlatformServices>(MockBehavior.Strict);
        services.Setup(service => service.SaveAutomationWorkspaceAsync(It.IsAny<IReadOnlyList<AutomationPipelineDraft>>()))
            .ReturnsAsync(false);

        var accepted = await HostOperation.TryExecuteAsync(
            () => services.Object.SaveAutomationWorkspaceAsync([]));

        accepted.Should().BeFalse();
        services.Verify(
            service => service.SaveAutomationWorkspaceAsync(It.Is<IReadOnlyList<AutomationPipelineDraft>>(drafts => drafts.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task TryExecuteAsync_ConvertsAHostExceptionIntoARecoverableFailure()
    {
        var services = new Mock<IPlatformServices>(MockBehavior.Strict);
        services.Setup(service => service.StartMacroRecordingAsync(0x60, MacroRecordingMode.Keyboard))
            .ThrowsAsync(new InvalidOperationException("Controller unavailable"));

        var accepted = await HostOperation.TryExecuteAsync(
            () => services.Object.StartMacroRecordingAsync(0x60, MacroRecordingMode.Keyboard));

        accepted.Should().BeFalse();
        services.Verify(service => service.StartMacroRecordingAsync(0x60, MacroRecordingMode.Keyboard), Times.Once);
    }
}
