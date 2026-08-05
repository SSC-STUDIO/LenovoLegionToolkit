using Avalonia.Media;
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

    [Fact]
    public void CpuCard_ShouldExposeWpfTrendSeriesContractAndBoundEachSeries()
    {
        var card = DashboardTelemetryGroups.CreateDefaults()
            .Single(item => item.Key == "cpu");

        Assert.Equal(
            ["utilization", "clock", "temperature"],
            card.TrendSeries.Select(series => series.Key));
        Assert.Equal(100, card.TrendSeries[0].Maximum);
        Assert.Equal(110, card.TrendSeries[2].Maximum);
        Assert.Equal("#FF4F9DF7", ((SolidColorBrush)card.TrendSeries[0].Stroke).Color.ToString(), ignoreCase: true);
        Assert.Equal("#FF6FBF73", ((SolidColorBrush)card.TrendSeries[1].Stroke).Color.ToString(), ignoreCase: true);
        Assert.Equal("#FFD9883B", ((SolidColorBrush)card.TrendSeries[2].Stroke).Color.ToString(), ignoreCase: true);

        for (var value = 0; value < 70; value++)
        {
            card.Update(
            [
                new SensorReadingItem("CPU Usage", $"{value} %", "Usage", value, "%"),
                new SensorReadingItem("CPU Core Clock", "4200 MHz", "Clock", 4200, "MHz"),
                new SensorReadingItem("CPU Temperature", "60 C", "Temperature", 60, "C"),
            ]);
        }

        Assert.All(card.TrendSeries, series => Assert.Equal(60, series.Values.Count));
        Assert.Equal(69, card.TrendSeries[0].Values[^1]);
        Assert.Equal(4.2, card.TrendSeries[1].Values[^1], 3);
        Assert.Equal(60, card.TrendSeries[2].Values[^1]);
    }

    [Fact]
    public void BatteryCard_ShouldExposeRateAndTemperatureSeriesAndClearOnUnavailableState()
    {
        var card = DashboardTelemetryGroups.CreateDefaults()
            .Single(item => item.Key == "battery");

        Assert.Equal(["battery-rate", "battery-temp"], card.TrendSeries.Select(series => series.Key));
        for (var index = 0; index < 65; index++)
        {
            card.UpdateBatteryState(new DashboardBatteryState
            {
                IsAvailable = true,
                Percentage = 80,
                DischargeRateWatts = -12,
                TemperatureCelsius = 35,
            });
        }

        Assert.All(card.TrendSeries, series => Assert.Equal(60, series.Values.Count));
        Assert.Equal(12, card.TrendSeries[0].Values[^1]);
        Assert.Equal(35, card.TrendSeries[1].Values[^1]);

        card.UpdateBatteryState(DashboardBatteryState.Empty);

        Assert.All(card.TrendSeries, series => Assert.Empty(series.Values));
    }

    [Fact]
    public void Card_ShouldClearTrendSeriesWhenSummaryReadingsDisappear()
    {
        var card = DashboardTelemetryGroups.CreateDefaults()
            .Single(item => item.Key == "gpu");

        card.Update([new SensorReadingItem("GPU Usage", "50 %", "Usage", 50, "%")]);
        Assert.NotEmpty(card.TrendSeries[0].Values);

        card.Update([]);

        Assert.All(card.TrendSeries, series => Assert.Empty(series.Values));
    }
}
