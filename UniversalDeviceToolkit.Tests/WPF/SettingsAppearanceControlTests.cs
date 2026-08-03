using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Guard)]
public sealed class SettingsAppearanceControlTests
{
    [Fact]
    public void ThemePreviewCards_ShouldRoundTheTitleAndDockAtTheirOuterEdges()
    {
        var root = RepositoryPaths.FindRoot();
        var xaml = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Controls",
            "Settings",
            "SettingsAppearanceControl.xaml"));

        foreach (var cardName in new[] { "_themeLightCard", "_themeDarkCard", "_themeSystemCard" })
        {
            var start = xaml.IndexOf($"x:Name=\"{cardName}\"", StringComparison.Ordinal);
            start.Should().BeGreaterThanOrEqualTo(0, $"{cardName} must remain a theme preview");

            var end = xaml.IndexOf("</Button>", start, StringComparison.Ordinal);
            end.Should().BeGreaterThan(start);

            var card = xaml[start..end];
            card.Should().Contain("Style=\"{StaticResource ThemePreviewTitleBarStyle}\"");
            card.Should().Contain("Style=\"{StaticResource ThemePreviewDockStyle}\"");
        }
    }
}
