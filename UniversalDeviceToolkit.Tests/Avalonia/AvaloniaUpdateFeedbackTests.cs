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

    [Theory]
    [InlineData("**Bold** text", "Bold text")]
    [InlineData("__Bold__ text", "Bold text")]
    [InlineData("*Italic* text", "Italic text")]
    [InlineData("_Italic_ text", "Italic text")]
    [InlineData("`inline code`", "inline code")]
    [InlineData("### Heading", "Heading")]
    [InlineData("# Heading", "Heading")]
    [InlineData("> quoted line", "quoted line")]
    [InlineData("[Release notes](https://example.com)", "Release notes")]
    [InlineData("![Logo](https://example.com/logo.png)", "Logo")]
    [InlineData("- list item", "\u2022 list item")]
    [InlineData("* list item", "\u2022 list item")]
    [InlineData("normal line", "normal line")]
    public void StripMarkdown_ReducesLightMarkdownToPlainText(string markdown, string expected)
    {
        UniversalDeviceToolkit.Avalonia.Windows.AvaloniaUpdateWindow.StripMarkdown(markdown).Should().Be(expected);
    }

    [Fact]
    public void StripMarkdown_RemovesRulesAndKeepsMultiLineBody()
    {
        var markdown = "### Release\r\n\r\n- Added **feature A**\r\n- Fixed [bug](https://example.com)\r\n\r\n---\r\n";
        var expected = $"Release{Environment.NewLine}{Environment.NewLine}\u2022 Added feature A{Environment.NewLine}\u2022 Fixed bug";

        UniversalDeviceToolkit.Avalonia.Windows.AvaloniaUpdateWindow.StripMarkdown(markdown).Should().Be(expected);
    }

    [Fact]
    public void StripMarkdown_ReturnsEmptyForBlankInput()
    {
        UniversalDeviceToolkit.Avalonia.Windows.AvaloniaUpdateWindow.StripMarkdown(null).Should().BeEmpty();
        UniversalDeviceToolkit.Avalonia.Windows.AvaloniaUpdateWindow.StripMarkdown("  ").Should().BeEmpty();
    }
}
