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
        DashboardPage.GetDashboardFallbackLoadingDelay().Should().Be(TimeSpan.FromMilliseconds(350));
    }

    [Fact]
    public void GetDashboardSensorDataReadyTimeout_ShouldGiveSensorsLongerThanRegularCards()
    {
        DashboardPage.GetDashboardSensorDataReadyTimeout().Should().BeGreaterThan(DashboardPage.GetDashboardGroupContentReadyTimeout());
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
    public void DashboardPage_ShouldRestartSensorInitialLoadForEachRefresh()
    {
        var source = ReadDashboardPageSource();
        var restartIndex = source.IndexOf("sensorsReadyTask = _sensors.RestartInitialSensorDataLoad();", StringComparison.Ordinal);
        var visibleIndex = source.IndexOf("_sensors.Visibility = Visibility.Visible;", StringComparison.Ordinal);

        restartIndex.Should().BeGreaterThanOrEqualTo(0);
        visibleIndex.Should().BeGreaterThan(restartIndex);
        source.Should().NotContain("_sensors.FirstSensorDataReadyTask");
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
