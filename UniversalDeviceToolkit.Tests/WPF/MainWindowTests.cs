using FluentAssertions;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class MainWindowTests
{
    [Fact]
    public void MainWindowMarkup_ShouldKeepMinimumWidthWideEnoughForDashboardSensors()
    {
        ReadMainWindowXaml()
            .Should()
            .Contain("MinWidth=\"1200\"");
    }

    [Fact]
    public void MainWindowMinimumWidth_ShouldStayAbovePreviousCrampedMinimum()
    {
        var minWidth = ExtractDoubleAttribute(ReadMainWindowXaml(), "MinWidth");

        minWidth.Should().BeGreaterThanOrEqualTo(1200);
        minWidth.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void DashboardSensors_ShouldKeepAllHardwareSectionsOnOneRowAtMainWindowMinimumWidth()
    {
        var minWidth = ExtractDoubleAttribute(ReadMainWindowXaml(), "MinWidth");

        const double shellChromeWidth = 12 + 220 + 12 + 24 + 16;
        var sensorAvailableWidth = minWidth - shellChromeWidth;

        SensorsControl.GetSensorColumnCountForWidth(sensorAvailableWidth).Should().Be(3);
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(1.5, 0.92)]
    [InlineData(2.0, 0.92)]
    public void DpiAwareTypography_ShouldReduceLogicalFontSizeAsDpiIncreases(
        double dpiScale,
        double expectedScale)
    {
        DpiAwareTypography.GetFontScaleForDpi(dpiScale).Should().BeApproximately(expectedScale, 0.01);
    }

    [Fact]
    public void TypographyMarkup_ShouldUseDynamicFontSizeTokens()
    {
        var typography = ReadTypographyXaml();

        typography.Should().Contain("FontSize\" Value=\"{DynamicResource FontSizeBody}\"");
        typography.Should().Contain("FontSize\" Value=\"{DynamicResource FontSizePageTitle}\"");
        typography.Should().Contain("FontSize\" Value=\"{DynamicResource FontSizeSmallBody}\"");
    }

    private static double ExtractDoubleAttribute(string xaml, string attributeName)
    {
        var marker = $"{attributeName}=\"";
        var start = xaml.IndexOf(marker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        start += marker.Length;

        var end = xaml.IndexOf('"', start);
        end.Should().BeGreaterThan(start);

        return double.Parse(xaml[start..end], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ReadMainWindowXaml()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Windows", "MainWindow.xaml"));
    }

    private static string ReadTypographyXaml()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Styles", "Typography.xaml"));
    }

    private static string FindRepositoryRoot()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates.Where(static candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            var current = Path.GetFullPath(candidate!);
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "UniversalDeviceToolkit.sln")))
                    return current;

                current = Directory.GetParent(current)?.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate UniversalDeviceToolkit.sln.");
    }
}
