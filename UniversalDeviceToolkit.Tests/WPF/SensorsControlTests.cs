using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using System.Reflection;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class SensorsControlTests
{
    [Fact]
    public void HasInitialSummarySensorData_WhenOnlyCpuHasData_ShouldReturnFalse()
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

        SensorsControl.HasInitialSummarySensorData(data).Should().BeFalse();
        SensorsControl.HasAnySummarySensorData(data).Should().BeTrue();
    }

    [Fact]
    public void HasInitialSummarySensorData_WhenNoSummaryMetricsExist_ShouldReturnFalse()
    {
        SensorsControl.HasInitialSummarySensorData(SensorsData.Empty).Should().BeFalse();
        SensorsControl.HasAnySummarySensorData(SensorsData.Empty).Should().BeFalse();
    }

    [Fact]
    public void HasInitialSummarySensorData_WhenCpuAndGpuHaveOnlyOneSummaryMetricEach_ShouldReturnFalse()
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

        SensorsControl.HasInitialSummarySensorData(data).Should().BeFalse();
        SensorsControl.HasAnySummarySensorData(data).Should().BeTrue();
    }

    [Fact]
    public void HasInitialSummarySensorData_WhenCpuAndGpuHaveRenderableVisibleMetrics_ShouldReturnTrue()
    {
        var data = new SensorsData(
            new SensorData(
                utilization: 42,
                maxUtilization: 100,
                coreClock: 4200,
                maxCoreClock: 5200,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 71,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            new SensorData(
                utilization: 35,
                maxUtilization: 100,
                coreClock: 1350,
                maxCoreClock: 2100,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 62,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1));

        SensorsControl.HasInitialSummarySensorData(data).Should().BeTrue();
        SensorsControl.HasAnySummarySensorData(data).Should().BeTrue();
    }

    [Fact]
    public void HasInitialSummarySensorData_WhenClockAndTemperatureAreZero_ShouldReturnFalse()
    {
        var data = new SensorsData(
            new SensorData(
                utilization: 0,
                maxUtilization: 100,
                coreClock: 0,
                maxCoreClock: 5200,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 0,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            new SensorData(
                utilization: 0,
                maxUtilization: 100,
                coreClock: 0,
                maxCoreClock: 2100,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 0,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1));

        SensorsControl.HasInitialSummarySensorData(data).Should().BeFalse();
        SensorsControl.CanCompleteInitialLoadFromCachedSensorData(data).Should().BeFalse();
    }

    [Fact]
    public void HasInitialSummarySensorData_WhenCpuAndGpuArriveInSeparateSamples_ShouldReturnTrueAfterDisplayMerge()
    {
        var cpuSample = new SensorsData(
            new SensorData(
                utilization: 42,
                maxUtilization: 100,
                coreClock: 4200,
                maxCoreClock: 5200,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 71,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            SensorData.Empty);
        var gpuSample = new SensorsData(
            SensorData.Empty,
            new SensorData(
                utilization: 35,
                maxUtilization: 100,
                coreClock: 1350,
                maxCoreClock: 2100,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 62,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1));

        var renderedData = SensorsControl.MergeSensorDataForDisplay(gpuSample, cpuSample);

        SensorsControl.HasInitialSummarySensorData(gpuSample).Should().BeFalse();
        SensorsControl.HasInitialSummarySensorData(renderedData).Should().BeTrue();
    }

    [Fact]
    public void CanCompleteInitialLoadFromCachedSensorData_WhenCachedDataIsComplete_ShouldReturnTrue()
    {
        var cached = new SensorsData(
            new SensorData(
                utilization: 42,
                maxUtilization: 100,
                coreClock: 4200,
                maxCoreClock: 5200,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 71,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            new SensorData(
                utilization: 35,
                maxUtilization: 100,
                coreClock: 1350,
                maxCoreClock: 2100,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 62,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1));

        SensorsControl.CanCompleteInitialLoadFromCachedSensorData(cached).Should().BeTrue();
    }

    [Fact]
    public void CanCompleteInitialLoadFromCachedSensorData_WhenCachedDataIsPartial_ShouldReturnFalse()
    {
        var partial = new SensorsData(
            new SensorData(
                utilization: 42,
                maxUtilization: 100,
                coreClock: 4200,
                maxCoreClock: 5200,
                memoryClock: -1,
                maxMemoryClock: -1,
                temperature: 71,
                maxTemperature: 100,
                wattage: -1,
                voltage: 0,
                fanSpeed: -1,
                maxFanSpeed: -1),
            SensorData.Empty);

        SensorsControl.CanCompleteInitialLoadFromCachedSensorData(partial).Should().BeFalse();
    }

    [Fact]
    public void SensorsControlMarkup_ShouldNotShowAverageBatteryTemperatureInDetails()
    {
        var xaml = ReadSensorsControlXaml();
        var batteryDetailsStart = xaml.IndexOf("x:Name=\"_batteryDetailsPanel\"", StringComparison.Ordinal);
        batteryDetailsStart.Should().BeGreaterThanOrEqualTo(0);

        var batteryDetailsEnd = xaml.IndexOf("x:Name=\"_gpuSection\"", batteryDetailsStart, StringComparison.Ordinal);
        batteryDetailsEnd.Should().BeGreaterThan(batteryDetailsStart);

        var batteryDetailsXaml = xaml[batteryDetailsStart..batteryDetailsEnd].ToLowerInvariant();
        batteryDetailsXaml.Should().NotContain("average");
        batteryDetailsXaml.Should().NotContain("平均");
    }

    [Fact]
    public void SensorsControl_ShouldUpdateBatteryStatusTextBlock()
    {
        var xaml = ReadSensorsControlXaml();
        var source = ReadSensorsControlSource();

        xaml.Should().Contain("<TextBlock x:Name=\"_batteryStatusLabel\"");
        source.Should().Contain("FindName(\"_batteryStatusLabel\") is TextBlock statusLabel");
        source.Should().Contain("statusLabel.Text = GetBatteryStatusText(batteryInfo);");
        source.Should().NotContain("FindName(\"_batteryStatusLabel\") is ContentControl statusLabel");
        source.Should().NotContain("statusLabel.Content = GetBatteryStatusText(batteryInfo);");
    }

    [Fact]
    public void SensorsControlMarkup_ShouldExposeMotherboardTemperatureInCpuDetails()
    {
        var xaml = ReadSensorsControlXaml();
        var cpuDetailsStart = xaml.IndexOf("x:Name=\"_cpuDetailsPanel\"", StringComparison.Ordinal);
        cpuDetailsStart.Should().BeGreaterThanOrEqualTo(0);

        var cpuDetailsEnd = xaml.IndexOf("x:Name=\"_batterySectionColumn\"", cpuDetailsStart, StringComparison.Ordinal);
        cpuDetailsEnd.Should().BeGreaterThan(cpuDetailsStart);

        var cpuDetailsXaml = xaml[cpuDetailsStart..cpuDetailsEnd];
        cpuDetailsXaml.Should().Contain("x:Name=\"_cpuMotherboardTemperatureTitle\"");
        cpuDetailsXaml.Should().Contain("x:Name=\"_cpuMotherboardTemperature\"");
    }

    [Fact]
    public void SensorsControl_ShouldHideUnavailableOptionalCpuDetails()
    {
        var xaml = ReadSensorsControlXaml();
        var source = ReadSensorsControlSource();

        xaml.Should().Contain("x:Name=\"_cpuWattageTitle\"");
        xaml.Should().Contain("x:Name=\"_cpuVoltageTitle\"");
        xaml.Should().Contain("x:Name=\"_cpuTempRangeTitle\"");
        xaml.Should().Contain("x:Name=\"_cpuVoltageRangeTitle\"");
        source.Should().Contain("UpdateOptionalDetailText(\"_cpuMemoryTemperatureTitle\", \"_cpuMemoryTemperature\"");
        source.Should().Contain("UpdateOptionalDetailText(\"_cpuMotherboardTemperatureTitle\", \"_cpuMotherboardTemperature\"");
        source.Should().Contain("UpdateOptionalDetailText(\"_cpuSsdTemperatureTitle\", \"_cpuSsdTemperature\"");
        source.Should().Contain("T(\"SensorsControl_Motherboard_Temperature\", \"Board Temperature\")");
        source.Should().NotContain("SensorsControl_MotherboardTemperature_Title");

        SensorsControl.IsUsefulDetailValue("55 °C").Should().BeTrue();
        SensorsControl.IsUsefulDetailValue("不可用").Should().BeFalse();
        SensorsControl.IsUsefulDetailValue("N/A").Should().BeFalse();
        SensorsControl.IsUsefulDetailValue("-").Should().BeFalse();
    }

    [Fact]
    public void SensorsControlMarkup_ShouldExposeExpandedSensorCharts()
    {
        var xaml = ReadSensorsControlXaml();

        xaml.Should().Contain("x:Name=\"_cpuChartPanel\"");
        xaml.Should().Contain("x:Name=\"_gpuChartPanel\"");
        xaml.Should().Contain("x:Name=\"_batteryCapacityPanel\"");
        xaml.Should().Contain("x:Name=\"_cpuUtilizationSparkline\"");
        xaml.Should().Contain("x:Name=\"_gpuTemperatureSparkline\"");
        xaml.Should().Contain("AutomationProperties.AutomationId=\"DashboardSensorsCpuChart\"");
    }

    [Theory]
    [InlineData(1119, false)]
    [InlineData(1120, true)]
    [InlineData(1280, true)]
    public void ShouldAutoExpandDetails_ShouldUseWideDashboardThreshold(double width, bool expected)
    {
        SensorsControl.ShouldAutoExpandDetails(width).Should().Be(expected);
    }

    [Fact]
    public void CreateSensorChartPoints_ShouldMapSamplesIntoChartBounds()
    {
        var samples = new[]
        {
            new SensorsControl.SensorChartSample(0, 50, 100),
            new SensorsControl.SensorChartSample(25, 75, 50),
            new SensorsControl.SensorChartSample(100, 0, 25),
        };

        var points = SensorsControl.CreateSensorChartPoints(samples, width: 120, height: 60);

        points.utilization.Should().HaveCount(3);
        points.clock.Should().HaveCount(3);
        points.temperature.Should().HaveCount(3);
        points.utilization.Select(point => point.X).Should().OnlyContain(x => x >= 0 && x <= 120);
        points.utilization.Select(point => point.Y).Should().OnlyContain(y => y >= 0 && y <= 60);
        points.utilization[0].Should().Be(new System.Windows.Point(0, 60));
        points.utilization[^1].Should().Be(new System.Windows.Point(120, 0));
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
    public void FormatTemperaturePair_WhenBothSsdTemperaturesAreUnavailable_ShouldReturnNotAvailable()
    {
        var text = SensorsControl.FormatTemperaturePair((-1f, -1f), TemperatureUnit.C);

        text.Should().Be(NotAvailableText());
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
    public void FormatThroughputPair_WhenBothDirectionsAreUnavailable_ShouldReturnNotAvailable()
    {
        var text = SensorsControl.FormatThroughputPair(-1f, -1f);

        text.Should().Be(NotAvailableText());
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
    public void FormatPowerKeepingPrevious_WhenPowerUnavailableAndPreviousExists_ShouldKeepPreviousValue()
    {
        var text = SensorsControl.FormatPowerKeepingPrevious(-1f, "12.3 W");

        text.Should().Be("12.3 W");
    }

    [Fact]
    public void FormatPowerKeepingPrevious_WhenPowerUnavailableAndPreviousMissing_ShouldReturnNotAvailable()
    {
        var text = SensorsControl.FormatPowerKeepingPrevious(-1f, NotAvailableText());

        text.Should().Be(NotAvailableText());
    }

    [Fact]
    public void FormatNullableTemperature_WhenTemperatureExists_ShouldUseConfiguredUnit()
    {
        var text = SensorsControl.FormatNullableTemperature(30, TemperatureUnit.F);

        text.Should().Be("86 °F");
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    public void NormalizeDetailValueText_WhenTextIsBlankOrPlaceholder_ShouldReturnNotAvailable(string? text)
    {
        SensorsControl.NormalizeDetailValueText(text).Should().Be(NotAvailableText());
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
        var original = ReplaceSessionSensorData(null);
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
            ReplaceSessionSensorData(original);
        }
    }

    private static SensorsData? ReplaceSessionSensorData(SensorsData? data)
    {
        var lockField = typeof(SensorsControl).GetField("SessionSensorDataLock", BindingFlags.NonPublic | BindingFlags.Static);
        var dataField = typeof(SensorsControl).GetField("_sessionSensorData", BindingFlags.NonPublic | BindingFlags.Static);

        lockField.Should().NotBeNull();
        dataField.Should().NotBeNull();

        var syncRoot = lockField!.GetValue(null);
        syncRoot.Should().NotBeNull();

        lock (syncRoot!)
        {
            var previous = dataField!.GetValue(null) as SensorsData?;
            dataField.SetValue(null, data);
            return previous;
        }
    }

    private static string NotAvailableText() => T("SensorsControl_NotAvailable", "N/A");

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private static string ReadSensorsControlXaml()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Controls", "Dashboard", "SensorsControl.xaml"));
    }

    private static string ReadSensorsControlSource()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Controls", "Dashboard", "SensorsControl.xaml.cs"));
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
