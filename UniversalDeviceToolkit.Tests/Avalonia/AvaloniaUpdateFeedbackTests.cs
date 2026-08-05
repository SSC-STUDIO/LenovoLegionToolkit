using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaUpdateFeedbackTests
{
    [Theory]
    [InlineData(AvaloniaUpdateCheckStatus.Success, null, AvaloniaUpdateFeedbackKind.NoUpdates, "MainWindow_CheckForUpdates_Success_Title")]
    [InlineData(AvaloniaUpdateCheckStatus.Success, "5.1.0", AvaloniaUpdateFeedbackKind.UpdateAvailable, "MainWindow_UpdateAvailable")]
    [InlineData(AvaloniaUpdateCheckStatus.RateLimitReached, null, AvaloniaUpdateFeedbackKind.RateLimitReached, "MainWindow_CheckForUpdates_Error_Title")]
    [InlineData(AvaloniaUpdateCheckStatus.Error, null, AvaloniaUpdateFeedbackKind.Error, "MainWindow_CheckForUpdates_Error_Title")]
    [InlineData(AvaloniaUpdateCheckStatus.Unavailable, null, AvaloniaUpdateFeedbackKind.Error, "MainWindow_CheckForUpdates_Error_Title")]
    public void Resolve_MapsUpdateCheckStatusToVisibleFeedback(
        AvaloniaUpdateCheckStatus status,
        string? latestVersion,
        AvaloniaUpdateFeedbackKind expectedKind,
        string expectedTitleKey)
    {
        var feedback = AvaloniaUpdateFeedback.Resolve(new AvaloniaUpdateCheckResult(status, latestVersion));

        feedback.Kind.Should().Be(expectedKind);
        feedback.TitleKey.Should().Be(expectedTitleKey);
    }

    [Fact]
    public void UpdateAvailableFeedback_UsesVersionMessageResource()
    {
        var feedback = AvaloniaUpdateFeedback.Resolve(
            new AvaloniaUpdateCheckResult(AvaloniaUpdateCheckStatus.Success, "5.1.0"));

        feedback.MessageKey.Should().Be("MainWindow_UpdateAvailableWithVersion");
    }
}
