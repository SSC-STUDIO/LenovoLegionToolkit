using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
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

        text.Should().Be("58 W | 核心 24 W | 内存 3.5 W | 平台 7 W");
    }

    [Fact]
    public void FormatCpuPowerBreakdown_WhenOnlyComponentsExist_ShouldStillReturnUsefulText()
    {
        var text = SensorsControl.FormatCpuPowerBreakdown(-1, (24f, -1f, 7f));

        text.Should().Be("核心 24 W | 平台 7 W");
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

        text.Should().Be("共享内存占用");
    }

    [Fact]
    public void GetGpuMemoryUsageTitle_WhenDiscreteGpu_ShouldReturnVramTitle()
    {
        var text = SensorsControl.GetGpuMemoryUsageTitle(false);

        text.Should().Be("显存占用");
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

        text.Should().Be("不可用");
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

        text.Should().Be("不可用");
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

        text.Should().Be("不可用");
    }

    [Fact]
    public void FormatFallbackRangeText_WhenRangeUnavailable_ShouldFallbackToPrimaryValue()
    {
        var text = SensorsControl.FormatFallbackRangeText("71 °C", "不可用");

        text.Should().Be("71 °C");
    }

    [Fact]
    public void FormatFallbackRangeText_WhenRangeExists_ShouldPreferExistingRange()
    {
        var text = SensorsControl.FormatFallbackRangeText("71 °C", "55 °C ~ 82 °C");

        text.Should().Be("55 °C ~ 82 °C");
    }
}
