using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Localization;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Localization;

[Trait("Category", TestCategories.Unit)]
public sealed class LocalizedOverflowPolicyTests
{
    [Fact]
    public void SharedPolicy_ShouldExposeTheSameBudgetsUsedByBothHosts()
    {
        Enum.GetValues<LocalizedOverflowMode>().Should().BeEquivalentTo(
            [LocalizedOverflowMode.Wrap, LocalizedOverflowMode.Ellipsis]);
        LocalizedOverflowPolicy.TitleMaxLines.Should().Be(2);
        LocalizedOverflowPolicy.DescriptionMaxLines.Should().Be(3);
        LocalizedOverflowPolicy.MinimumReadableFontSize.Should().Be(11);
        LocalizedOverflowPolicy.TitleMode.Should().Be(LocalizedOverflowMode.Wrap);
        LocalizedOverflowPolicy.DescriptionMode.Should().Be(LocalizedOverflowMode.Wrap);
        LocalizedOverflowPolicy.CompactMode.Should().Be(LocalizedOverflowMode.Ellipsis);
    }

    [Theory]
    [InlineData(LocalizedOverflowMode.Wrap, 3)]
    [InlineData(LocalizedOverflowMode.Ellipsis, 1)]
    public void GetMaxLines_ShouldKeepCompactAndDescriptiveSlotsDistinct(
        LocalizedOverflowMode mode,
        int expectedMaxLines)
    {
        LocalizedOverflowPolicy.GetMaxLines(mode).Should().Be(expectedMaxLines);
    }

    [Fact]
    public void CriticalHostMarkup_ShouldUseSemanticLocalizedOverflowControls()
    {
        var root = RepositoryPaths.FindRoot();
        var wpfSettings = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Pages",
            "SettingsPage.xaml"));
        var wpfNavigation = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Styles",
            "NavigationStore.xaml"));
        var cardHeader = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Controls",
            "CardHeaderControl.cs"));

        wpfSettings.Should().Contain("controls:AdaptiveTextBlock");
        wpfSettings.Should().Contain("overflow:LocalizedOverflowBehavior.IsEnabled=\"True\"");
        wpfNavigation.Should().Contain("overflow:LocalizedOverflowBehavior.Mode=\"Ellipsis\"");
        cardHeader.Should().Contain("MaxLines = 2");
        cardHeader.Should().Contain("MaxLines = 3");
    }
}
