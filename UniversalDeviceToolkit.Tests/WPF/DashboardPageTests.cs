using FluentAssertions;
using System.Diagnostics;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
using UniversalDeviceToolkit.WPF.Pages;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class DashboardPageTests
{
    [Fact]
    public void TryGetProcessName_ExitedProcess_ShouldReturnNullWithoutThrowing()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c exit 0",
            CreateNoWindow = true,
            UseShellExecute = false,
        });

        process.Should().NotBeNull();
        process!.WaitForExit();

        var action = () => DiscreteGPUControl.TryGetProcessName(process);

        action.Should().NotThrow();
        action().Should().BeNull();
    }

    [Fact]
    public void DashboardPage_ShouldUseCancelableLatestWinsLoading()
    {
        ReadDashboardPageSource()
            .Should()
            .Contain("CancellationTokenSource")
            .And.Contain("refreshVersion")
            .And.Contain("_hasLoadedContent");
    }

    [Fact]
    public void DashboardPage_ShouldNotUseFixedLoadingDelays()
    {
        ReadDashboardPageSource()
            .Should()
            .NotContain("GetDashboardFallbackLoadingDelay")
            .And.NotContain("Task.Delay(");
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
    public void DashboardPageMarkup_LoadingSkeleton_ShouldIncludeSensorsCardSilhouette()
    {
        var xaml = ReadDashboardPageXaml();
        var source = ReadDashboardPageSource();

        // Content is Opacity 0 while loading, so SensorsControl overlay cannot paint.
        // Page skeleton owns a detailed sensor-card silhouette (matches SensorsControl overlay).
        xaml.Should().Contain("x:Name=\"_skeletonSensorsCard\"");
        xaml.Should().Contain("AutomationProperties.AutomationId=\"DashboardSensorsLoadingSkeleton\"");
        xaml.Should().Contain("x:Name=\"_skeletonGroupsGrid\"");
        xaml.Should().Contain("DashboardPage owns loading chrome");
        // Detail parity with SensorsControl: title+subtitle, GaugeSizeMD, trend well, legend.
        xaml.Should().Contain("DashboardSensorsSkeletonSubtitleStyle");
        xaml.Should().Contain("GaugeSizeMD");
        xaml.Should().Contain("DashboardSensorsSkeletonTrendPanelStyle");
        xaml.Should().Contain("DashboardSensorsSkeletonLegendPanelStyle");
        source.Should().Contain("LoadingChromeOwnership.Page");
        source.Should().Contain("ILoadingChromeOwner");
        source.Should().Contain("_skeletonSensorsCard.Visibility");
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

        source.Should().Contain("await WaitForDashboardShellAsync(sensorsReadyTask, cancellationToken);");
        source.Should().Contain("await WaitForDashboardSensorDataAsync(sensorsReadyTask, cancellationToken);");
        var shellMethod = ExtractMethod(source, "private async Task WaitForDashboardShellAsync(Task? sensorsReadyTask, CancellationToken cancellationToken)");
        shellMethod
            .Should()
            .Contain("await WaitForDashboardSensorDataAsync(sensorsReadyTask, cancellationToken);");
        ExtractMethod(source, "private static async Task WaitForDashboardSensorDataAsync(Task sensorsReadyTask, CancellationToken cancellationToken)")
            .Should()
            .Contain("WaitAsync(GetDashboardSensorDataReadyTimeout(), cancellationToken)")
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
