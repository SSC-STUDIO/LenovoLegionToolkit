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
}
