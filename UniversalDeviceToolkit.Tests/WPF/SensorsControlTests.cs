using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class SensorsControlTests
{
    [Fact]
    public void HasSummarySensorData_WhenOnlyCpuHasData_ShouldReturnFalse()
    {
        var data = new SensorsData(
            new SensorData(
                utilization: 42,
                maxUtilization: 100,
                coreClock: -1,
                maxCoreClock: -1,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: -1,
                maxTemperature: -1,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            SensorData.Empty);

        SensorsControl.HasSummarySensorData(data).Should().BeFalse();
        SensorsControl.HasAnySummarySensorData(data).Should().BeTrue();
    }

    [Fact]
    public void HasSummarySensorData_WhenNoSummaryMetricsExist_ShouldReturnFalse()
    {
        SensorsControl.HasSummarySensorData(SensorsData.Empty).Should().BeFalse();
        SensorsControl.HasAnySummarySensorData(SensorsData.Empty).Should().BeFalse();
    }

    [Fact]
    public void HasSummarySensorData_WhenCpuAndGpuHaveSummaryMetrics_ShouldReturnTrue()
    {
        var data = new SensorsData(
            new SensorData(
                utilization: 42,
                maxUtilization: 100,
                coreClock: -1,
                maxCoreClock: -1,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: -1,
                maxTemperature: -1,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            new SensorData(
                utilization: -1,
                maxUtilization: -1,
                coreClock: 1350,
                maxCoreClock: 2100,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: -1,
                maxTemperature: -1,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1));

        SensorsControl.HasSummarySensorData(data).Should().BeTrue();
        SensorsControl.HasAnySummarySensorData(data).Should().BeTrue();
    }

    [Fact]
    public void FormatUsageInGigabytes_WhenUsageAndTotalAreAvailable_ShouldIncludePercentage()
    {
        var text = SensorsControl.FormatUsageInGigabytes(6.4f, 8f);

        text.Should().Be("6.4 / 8.0 GB (80%)");
    }

    [Fact]
    public void FormatUsageInGigabytes_WhenTotalIsUnavailableButUsageAndPercentageExist_ShouldStillReturnUsefulText()
    {
        var text = SensorsControl.FormatUsageInGigabytes(3.2f, -1f, 40f);

        text.Should().Be("3.2 GB (40%)");
    }

    [Fact]
    public void FormatUsageInGigabytes_WhenOnlyPercentageExists_ShouldReturnPercentage()
    {
        var text = SensorsControl.FormatUsageInGigabytes(-1f, -1f, 73f);

        text.Should().Be("73%");
    }

    [Fact]
    public void FormatTemperaturePair_WhenOneSsdTemperatureExists_ShouldReturnSingleValue()
    {
        var text = SensorsControl.FormatTemperaturePair((41f, -1f), TemperatureUnit.C);

        text.Should().Be("41 °C");
    }
    [Fact]
    public void FormatThroughputPair_WhenRxAndTxExist_ShouldReturnBothDirections()
    {
        var text = SensorsControl.FormatThroughputPair(1024f * 1024f, 2 * 1024f * 1024f);

        text.Should().Be("Rx 1.00 MB/s / Tx 2.00 MB/s");
    }

    [Fact]
    public void FormatThroughputPair_WhenOnlyRxExists_ShouldReturnSingleDirection()
    {
        var text = SensorsControl.FormatThroughputPair(1024f, -1f);

        text.Should().Be("Rx 1.00 KB/s");
    }

    [Fact]
    public void FormatCpuPowerBreakdown_WhenTotalAndComponentsExist_ShouldIncludeAllSegments()
    {
        var text = SensorsControl.FormatCpuPowerBreakdown(58, (24f, 3.5f, 7f));

        text.Should().Be($"58 W | {T("SensorsControl_CpuCoresPower_Label", "Cores")} 24 W | {T("SensorsControl_CpuMemoryPower_Label", "Memory")} 3.5 W | {T("SensorsControl_CpuPlatformPower_Label", "Platform")} 7 W");
    }

    [Fact]
    public void FormatCpuPowerBreakdown_WhenOnlyComponentsExist_ShouldStillReturnUsefulText()
    {
        var text = SensorsControl.FormatCpuPowerBreakdown(-1, (24f, -1f, 7f));

        text.Should().Be($"{T("SensorsControl_CpuCoresPower_Label", "Cores")} 24 W | {T("SensorsControl_CpuPlatformPower_Label", "Platform")} 7 W");
    }

    [Fact]
    public void NormalizeModelName_WhenValueIsBlank_ShouldReturnNull()
    {
        SensorsControl.NormalizeModelName("   ").Should().BeNull();
    }

    [Fact]
    public void NormalizeModelName_WhenValueHasText_ShouldTrimAndReturnValue()
    {
        SensorsControl.NormalizeModelName("  Legion Y9000P IRX9  ").Should().Be("Legion Y9000P IRX9");
    }

    [Fact]
    public void NormalizeHardwareNameOrFallback_WhenValueIsUnknown_ShouldReturnFallback()
    {
        SensorsControl.NormalizeHardwareNameOrFallback(" UNKNOWN ", "Unknown GPU").Should().Be("Unknown GPU");
    }

    [Fact]
    public void GetGpuMemoryUsageTitle_WhenIntegratedGpu_ShouldReturnSharedMemoryTitle()
    {
        var text = SensorsControl.GetGpuMemoryUsageTitle(true);

        text.Should().Be(T("SensorsControl_SharedMemoryUsage_Title", "Shared Memory Usage"));
    }

    [Fact]
    public void GetGpuMemoryUsageTitle_WhenDiscreteGpu_ShouldReturnVramTitle()
    {
        var text = SensorsControl.GetGpuMemoryUsageTitle(false);

        text.Should().Be(T("SensorsControl_VramUsage_Title", "VRAM Usage"));
    }

    [Fact]
    public void FormatVoltage_WhenPositiveVoltage_ShouldFormatWithThreeDecimals()
    {
        var text = SensorsControl.FormatVoltage(1.127f);

        text.Should().Be("1.127 V");
    }

    [Fact]
    public void FormatVoltage_WhenVoltageUnavailable_ShouldReturnNotAvailable()
    {
        var text = SensorsControl.FormatVoltage(-1f);

        text.Should().Be(NotAvailableText());
    }

    [Fact]
    public void FormatPower_WhenPowerAvailable_ShouldFormatWithCompactPrecision()
    {
        var text = SensorsControl.FormatPower(12.265f);

        text.Should().Be("12.3 W");
    }

    [Fact]
    public void FormatPower_WhenPowerUnavailable_ShouldReturnNotAvailable()
    {
        var text = SensorsControl.FormatPower(-1f);

        text.Should().Be(NotAvailableText());
    }

    [Fact]
    public void FormatFrequency_WhenFrequencyAvailable_ShouldFormatInGigahertz()
    {
        var text = SensorsControl.FormatFrequency(4200f);

        text.Should().Be("4.2 GHz");
    }

    [Fact]
    public void FormatFrequency_WhenFrequencyUnavailable_ShouldReturnNotAvailable()
    {
        var text = SensorsControl.FormatFrequency(-1f);

        text.Should().Be(NotAvailableText());
    }

    [Fact]
    public void FormatFallbackRangeText_WhenRangeUnavailable_ShouldFallbackToPrimaryValue()
    {
        var text = SensorsControl.FormatFallbackRangeText("71 °C", SensorsControl.FormatFrequency(-1f));

        text.Should().Be("71 °C");
    }

    [Fact]
    public void FormatFallbackRangeText_WhenRangeExists_ShouldPreferExistingRange()
    {
        var text = SensorsControl.FormatFallbackRangeText("71 °C", "55 °C ~ 82 °C");

        text.Should().Be("55 °C ~ 82 °C");
    }

    [Fact]
    public void MergeSensorDataForDisplay_WhenCurrentSampleDropsMetric_ShouldKeepPreviousValue()
    {
        var previous = new SensorsData(
            new SensorData(
                utilization: 35,
                maxUtilization: 100,
                coreClock: 4200,
                maxCoreClock: 5200,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 71,
                maxTemperature: 100,
                wattage: 58,
                voltage: 1.127,
                fanSpeed: 2400,
                maxFanSpeed: 5200).WithMinMax(1.05, 1.2, 55, 82),
            SensorData.Empty);
        var current = new SensorsData(
            new SensorData(
                utilization: 40,
                maxUtilization: 100,
                coreClock: -1,
                maxCoreClock: -1,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: -1,
                maxTemperature: -1,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            SensorData.Empty);

        var merged = SensorsControl.MergeSensorDataForDisplay(current, previous);

        merged.CPU.Utilization.Should().Be(40);
        merged.CPU.CoreClock.Should().Be(4200);
        merged.CPU.Temperature.Should().Be(71);
        merged.CPU.Wattage.Should().Be(58);
        merged.CPU.Voltage.Should().Be(1.127);
        merged.CPU.FanSpeed.Should().Be(2400);
        merged.CPU.MaxCoreClock.Should().Be(5200);
        merged.CPU.MinTemperature.Should().Be(55);
        merged.CPU.MaxTemperatureRecord.Should().Be(82);
    }

    [Fact]
    public void MergeSensorDataForDisplay_WhenNoPreviousSampleExists_ShouldReturnCurrentSample()
    {
        var current = new SensorsData(
            new SensorData(
                utilization: 25,
                maxUtilization: 100,
                coreClock: 3500,
                maxCoreClock: -1,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 66,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            SensorData.Empty);

        var merged = SensorsControl.MergeSensorDataForDisplay(current, null);

        merged.Should().Be(current);
    }

    [Fact]
    public void CacheSessionSensorDataForDisplay_ShouldKeepPartialUsefulSnapshotAndIgnoreEmptySnapshot()
    {
        var original = SensorsControl.ReplaceSessionSensorDataForTests(null);
        try
        {
            var partial = new SensorsData(
                new SensorData(
                    utilization: 25,
                    maxUtilization: 100,
                    coreClock: -1,
                    maxCoreClock: -1,
                    memoryClock: -1,
                    maxMemoryClock: -1,
                    temperature: -1,
                    maxTemperature: -1,
                    wattage: -1,
                    voltage: 0,
                    fanSpeed: -1,
                    maxFanSpeed: -1),
                SensorData.Empty);

            SensorsControl.CacheSessionSensorDataForDisplay(partial);
            SensorsControl.TryGetSessionSensorDataForDisplay().Should().Be(partial);

            SensorsControl.CacheSessionSensorDataForDisplay(SensorsData.Empty);
            SensorsControl.TryGetSessionSensorDataForDisplay().Should().Be(partial);
        }
        finally
        {
            SensorsControl.ReplaceSessionSensorDataForTests(original);
        }
    }

    private static string NotAvailableText() => T("SensorsControl_NotAvailable", "N/A");

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);
}
