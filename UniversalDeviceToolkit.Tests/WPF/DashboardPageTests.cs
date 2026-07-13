using FluentAssertions;
using UniversalDeviceToolkit.WPF.Pages;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class DashboardPageTests
{
    [Fact]
    public void GetDashboardFallbackLoadingDelay_ShouldRemainShortAndStable()
    {
        DashboardPage.GetDashboardFallbackLoadingDelay().Should().Be(TimeSpan.FromMilliseconds(120));
    }

    [Fact]
    public void GetDashboardSensorDataReadyTimeout_ShouldKeepInitialLoadingBounded()
    {
        DashboardPage.GetDashboardSensorDataReadyTimeout().Should().Be(TimeSpan.FromSeconds(12));
    }

    [Fact]
    public void DashboardPageMarkup_ShouldKeepContentVisibleWhileLoadingSoSensorsCanStart()
    {
        ReadDashboardPageXaml()
            .Should()
            .Contain("ContentVisibilityWhileLoading=\"Visible\"")
            .And.Contain("Opacity=\"0\"")
            .And.Contain("IsHitTestVisible=\"False\"");
    }

    [Fact]
    public void DashboardPageMarkup_LoadingSkeleton_ShouldMirrorSensorsSummaryGeometry()
    {
        var xaml = ReadDashboardPageXaml();

        // Page-level loader must show a 3-column sensors silhouette (CPU / Battery / GPU),
        // not only list-item cards that look nothing like the live dashboard.
        xaml.Should().Contain("AutomationProperties.AutomationId=\"DashboardSensorsLoadingSkeleton\"");
        xaml.Should().Contain("x:Name=\"_skeletonSensorsCard\"");
        xaml.Should().Contain("x:Name=\"_skeletonSensorsGrid\"");
        xaml.Should().Contain("Columns=\"3\"");
        xaml.Should().Contain("DashboardSensorsSkeletonGaugeStyle");
        xaml.Should().Contain("GaugeSizeMD");
        xaml.Should().Contain("DashboardSensorsSkeletonBarStyle");
        xaml.Should().Contain("DashboardSensorsSkeletonTrendPanelStyle");
        // Feature groups remain below sensors.
        xaml.Should().Contain("x:Name=\"_skeletonGroupsGrid\"");
    }

    [Theory]
    [InlineData(500, 1)]
    [InlineData(1000, 1)]
    [InlineData(1001, 2)]
    [InlineData(1300, 2)]
    [InlineData(1500, 2)]
    [InlineData(1501, 3)]
    [InlineData(2400, 3)]
    public void GetColumnCountForWidth_ShouldScaleColumnsWithWidth(double width, int expectedColumns)
    {
        DashboardPage.GetColumnCountForWidth(width).Should().Be(expectedColumns);
    }

    [Fact]
    public void DashboardPage_ShouldRestartSensorInitialLoadForEachRefresh()
    {
        var source = ReadDashboardPageSource();
        var restartTrendIndex = source.IndexOf("_sensors.RestartTrendCharts();", StringComparison.Ordinal);
        var restartIndex = source.IndexOf("sensorsReadyTask = _sensors.RestartInitialSensorDataLoad();", StringComparison.Ordinal);
        var visibleIndex = source.IndexOf("_sensors.Visibility = Visibility.Visible;", StringComparison.Ordinal);

        restartTrendIndex.Should().BeGreaterThanOrEqualTo(0);
        restartIndex.Should().BeGreaterThanOrEqualTo(0);
        restartIndex.Should().BeGreaterThan(restartTrendIndex);
        visibleIndex.Should().BeGreaterThan(restartIndex);
        source.Should().NotContain("_sensors.FirstSensorDataReadyTask");
    }

    [Fact]
    public void DashboardPage_ShouldWaitForSensorReadinessBeforeEndingInitialLoading()
    {
        var source = ReadDashboardPageSource();

        source.Should().Contain("await WaitForDashboardShellAsync(sensorsReadyTask);");
        source.Should().Contain("await WaitForDashboardSensorDataAsync(sensorsReadyTask);");
        var shellMethod = ExtractMethod(source, "private async Task WaitForDashboardShellAsync(Task? sensorsReadyTask)");
        shellMethod
            .Should()
            .Contain("await WaitForDashboardSensorDataAsync(sensorsReadyTask);");
        ExtractMethod(source, "private static async Task WaitForDashboardSensorDataAsync(Task sensorsReadyTask)")
            .Should()
            .Contain("WaitAsync(GetDashboardSensorDataReadyTimeout())")
            .And.Contain("catch (TimeoutException)");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var braceStart = source.IndexOf('{', start);
        braceStart.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[start..(i + 1)];
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static string ReadDashboardPageXaml()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Pages", "DashboardPage.xaml"));
    }

    private static string ReadDashboardPageSource()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Pages", "DashboardPage.xaml.cs"));
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
