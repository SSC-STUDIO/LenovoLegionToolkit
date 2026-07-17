using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Guard)]
public sealed class PackagesPageGuardTests
{
    [Fact]
    public void FilterForm_ShouldExposeStableAutomationIds()
    {
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PackagesPage.xaml");
        xaml.Should().Contain("PackagesMachineTypeTextBox");
        xaml.Should().Contain("PackagesOsComboBox");
        xaml.Should().Contain("PackagesDownloadToTextBox");
        xaml.Should().Contain("PackagesBrowseButton");
        xaml.Should().Contain("PackagesOpenFolderButton");
        xaml.Should().Contain("PackagesRefreshButton");
        xaml.Should().Contain("PackagesCancelButton");
    }

    [Fact]
    public void FilterForm_ShouldNotBeStuffedIntoCardControlHeader()
    {
        // Multi-field filter forms belong in card body/surface chrome, not CardControl.Header.
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PackagesPage.xaml");
        AssertAutomationIdNotInsideCardControlHeader(xaml, "PackagesMachineTypeTextBox");
        AssertAutomationIdNotInsideCardControlHeader(xaml, "PackagesRefreshButton");
    }

    private static void AssertAutomationIdNotInsideCardControlHeader(string xaml, string automationId)
    {
        var idIndex = xaml.IndexOf(automationId, System.StringComparison.Ordinal);
        idIndex.Should().BeGreaterThanOrEqualTo(0, $"expected AutomationId {automationId}");

        var searchFrom = 0;
        while (true)
        {
            var headerOpen = xaml.IndexOf("CardControl.Header", searchFrom, System.StringComparison.Ordinal);
            if (headerOpen < 0 || headerOpen > idIndex)
                break;

            var headerClose = xaml.IndexOf("/custom:CardControl.Header", headerOpen, System.StringComparison.Ordinal);
            if (headerClose < 0)
                headerClose = xaml.IndexOf("/wpfui:CardControl.Header", headerOpen, System.StringComparison.Ordinal);
            if (headerClose < 0)
                break;

            (idIndex > headerOpen && idIndex < headerClose)
                .Should()
                .BeFalse($"{automationId} must not live inside CardControl.Header");

            searchFrom = headerClose + 1;
        }
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the test must run from inside the repository tree");
        return File.ReadAllText(Path.Combine([directory!.FullName, .. pathParts]));
    }
}
