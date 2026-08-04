using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class DashboardTelemetryGroupsTests
{
    [Theory]
    [InlineData("CPU Usage", "Usage", "cpu")]
    [InlineData("GPU Temperature", "Temperature", "gpu")]
    [InlineData("Battery Charge", "Battery", "battery")]
    [InlineData("Memory Total", "Memory", "system")]
    public void Classify_ShouldKeepIndependentTelemetryGroups(string name, string category, string expected)
    {
        var reading = new SensorReadingItem(name, "1 %", category, 1, "%");

        Assert.Equal(expected, DashboardTelemetryGroups.Classify(reading));
    }

    [Fact]
    public void Card_ShouldExposeEmptyStateWithoutInventingAReading()
    {
        var card = DashboardTelemetryGroups.CreateDefaults()
            .Single(item => item.Key == "gpu");

        card.Update([]);

        Assert.False(card.IsAvailable);
        Assert.True(card.IsUnavailable);
        Assert.Empty(card.Metrics);
        Assert.Empty(card.History);
        Assert.NotEmpty(card.PrimaryValue);
        Assert.NotEqual("0", card.PrimaryValue);
    }

    [Fact]
    public void Card_ShouldKeepBoundedHistoryAndUsePercentageAsPrimaryMetric()
    {
        var card = DashboardTelemetryGroups.CreateDefaults()
            .Single(item => item.Key == "cpu");

        for (var value = 0; value < 40; value++)
        {
            card.Update(
            [
                new SensorReadingItem("CPU Usage", $"{value} %", "Usage", value, "%"),
                new SensorReadingItem("CPU Temperature", "60 °C", "Temperature", 60, "°C"),
            ]);
        }

        Assert.True(card.IsAvailable);
        Assert.True(card.HasPrimaryProgress);
        Assert.Equal(39, card.PrimaryProgressPercent);
        Assert.Equal(30, card.History.Count);
        Assert.Equal(39, card.History[^1]);
        Assert.Equal(2, card.Metrics.Count);
    }
}
