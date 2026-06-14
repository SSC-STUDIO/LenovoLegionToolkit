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
    public void HasInitialSummarySensorData_WhenCpuAndGpuHaveOnlyOneSummaryMetricEach_ShouldReturnTrue()
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

        SensorsControl.HasInitialSummarySensorData(data).Should().BeTrue();
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
    public void SensorsControlMarkup_ShouldNotShowAverageBatteryTemperatureInDetails()
    {
        var xaml = ReadSensorsControlXaml();
        var batteryDetailsStart = xaml.IndexOf("x:Name=\"_batteryDetailsPanel\"", StringComparison.Ordinal);
        batteryDetailsStart.Should().BeGreaterThanOrEqualTo(0);

        var batteryDetailsEnd = xaml.IndexOf("x:Name=\"_gpuSection\"", batteryDetailsStart, StringComparison.Ordinal);
        batteryDetailsEnd.Should().BeGreaterThan(batteryDetailsStart);

        var batteryDetailsXaml = xaml[batteryDetailsStart..batteryDetailsEnd].ToLowerInvariant();
        batteryDetailsXaml.Should().NotContain("_batteryaveragetemperaturetitle");
        batteryDetailsXaml.Should().NotContain("_batteryaveragetemperature");
        batteryDetailsXaml.Should().NotContain("averagetemperature");
    }

    [Theory]
    [InlineData(759, 3)]
    [InlineData(1049, 3)]
    [InlineData(1050, 3)]
    [InlineData(1499, 3)]
    [InlineData(1500, 3)]
    [InlineData(1800, 3)]
    public void GetSensorColumnCountForWidth_ShouldKeepAllHardwareSectionsOnOneRow(double width, int expectedColumns)
    {
        SensorsControl.GetSensorColumnCountForWidth(width).Should().Be(expectedColumns);
    }

    [Theory]
    [InlineData(1049, "Compact")]
    [InlineData(1050, "Standard")]
    [InlineData(1499, "Standard")]
    [InlineData(1500, "Wide")]
    public void GetSensorSummaryLayoutMode_ShouldAdaptCardDensityWithoutChangingColumns(
        double width,
        string expectedMode)
    {
        SensorsControl.GetSensorSummaryLayoutMode(width).ToString().Should().Be(expectedMode);
    }

    [Theory]
    [InlineData(1049, false)]
    [InlineData(1050, false)]
    [InlineData(1499, false)]
    [InlineData(1500, true)]
    public void CanShowSensorDetailsForWidth_ShouldOnlyAllowDetailsOnWideLayouts(double width, bool expected)
    {
        SensorsControl.CanShowSensorDetailsForWidth(width).Should().Be(expected);
    }

    [Fact]
    public void SensorsControlMarkup_ShouldAvoidHardSectionMinimumWidthForSmallWindows()
    {
        ReadSensorsControlXaml()
            .Should()
            .Contain("<Setter Property=\"MinWidth\" Value=\"0\" />")
            .And.Contain("<Setter Property=\"MinWidth\" Value=\"24\" />");
    }

    [Fact]
    public void SensorsControlMarkup_ShouldIncludeBatteryTrendChart()
    {
        var xaml = ReadSensorsControlXaml();
        var batterySection = ExtractXamlRange(xaml, "x:Name=\"_batterySectionColumn\"", "x:Name=\"_gpuSection\"");

        batterySection.Should().Contain("x:Name=\"_batteryTrendChart\"");
        batterySection.Should().Contain("Resource.SensorsControl_Charge");
        batterySection.Should().Contain("Resource.SensorsControl_Health");
        batterySection.Should().Contain("Resource.SensorsControl_Temperature_Title");
    }

    [Fact]
    public void SensorsControlMarkup_ShouldKeepSummaryGaugeCaptionsAndAvoidDuplicateProgressRows()
    {
        var xaml = ReadSensorsControlXaml();

        ExtractSelfClosingElement(xaml, "x:Name=\"_cpuGauge\"")
            .Should().Contain("Caption=\"{x:Static resources:Resource.SensorsControl_Utilization_Title}\"");
        ExtractSelfClosingElement(xaml, "x:Name=\"_batteryGauge\"")
            .Should().Contain("Caption=\"{x:Static resources:Resource.SensorsControl_Charge}\"");
        ExtractSelfClosingElement(xaml, "x:Name=\"_gpuGauge\"")
            .Should().Contain("Caption=\"{x:Static resources:Resource.SensorsControl_Utilization_Title}\"");

        var cpuSummary = ExtractXamlRange(xaml, "x:Name=\"_cpuGauge\"", "x:Name=\"_cpuTrendChart\"");
        var batterySummary = ExtractXamlRange(xaml, "x:Name=\"_batteryGauge\"", "x:Name=\"_batteryTrendChart\"");
        var gpuSummary = ExtractXamlRange(xaml, "x:Name=\"_gpuGauge\"", "x:Name=\"_gpuTrendChart\"");

        cpuSummary.Should().NotContain("_cpuUtilizationBar").And.NotContain("_cpuUtilizationLabel");
        batterySummary.Should().NotContain("_batteryPercentageBar").And.NotContain("_batteryPercentageLabel");
        gpuSummary.Should().NotContain("_gpuUtilizationBar").And.NotContain("_gpuUtilizationLabel");

        CountOccurrences(cpuSummary, "<ProgressBar ").Should().Be(3);
        CountOccurrences(batterySummary, "<ProgressBar ").Should().Be(3);
        CountOccurrences(gpuSummary, "<ProgressBar ").Should().Be(3);
    }

    [Fact]
    public void SensorsControlMarkup_ShouldWrapDetailRowsSoUnavailableValuesCanBeHidden()
    {
        var xaml = ReadSensorsControlXaml();

        foreach (var detailName in new[]
        {
            "_cpuWattageDetail",
            "_cpuVoltageDetail",
            "_cpuMemoryUsageDetail",
            "_batteryRateRangeDetail",
            "_batteryTemperatureDetail",
            "_gpuWattageDetail",
            "_gpuVoltageDetail",
            "_gpuVramTemperatureDetail",
            "_gpuTempRangeDetail"
        })
        {
            xaml.Should().Contain($"x:Name=\"{detailName}\"");
        }
    }

    [Fact]
    public void SensorsControlCode_ShouldNotAttachDetailsTooltipToWholeCard()
    {
        ReadSensorsControlCode()
            .Should()
            .NotContain("ToolTip = T(\"SensorsControl_DetailsToggleToolTip\"");
    }

    [Fact]
    public void SensorsControlCode_ShouldRestartTrendChartsWhenDashboardReopens()
    {
        var source = ReadSensorsControlCode();
        var restartMethod = ExtractMethod(source, "public void RestartTrendCharts()");
        var cacheWarmupMethod = ExtractMethod(source, "private void InitializeTrendChartsFromSessionCache()");

        restartMethod.Should().Contain("ClearTrendCharts();");
        restartMethod.Should().Contain("InitializeTrendChartsFromSessionCache();");
        cacheWarmupMethod.Should().Contain("TryGetSessionSensorDataForDisplay()");
        cacheWarmupMethod.Should().Contain("PushTrendSamples(_cpuTrendChart, data.CPU);");
        cacheWarmupMethod.Should().Contain("PushTrendSamples(_gpuTrendChart, data.GPU);");
    }

    [Fact]
    public void SensorsControlCode_ShouldOpenDetailsWindowWhenInlineDetailsAreUnavailable()
    {
        var source = ReadSensorsControlCode();
        var toggleMethod = ExtractMethod(source, "private void ToggleDetails()");
        var detailsWindowMethod = ExtractMethod(source, "private void ShowDetailsWindow()");

        toggleMethod.Should().Contain("ShowDetailsWindow();");
        detailsWindowMethod.Should().Contain("new SensorDetailsWindow");
        detailsWindowMethod.Should().Contain("Owner = Window.GetWindow(this)");
    }

    [Fact]
    public void SensorDetailsWindowMarkup_ShouldHostSensorsControl()
    {
        var xaml = ReadSensorDetailsWindowXaml();

        xaml.Should().Contain("AutomationProperties.AutomationId>SensorDetailsWindow");
        xaml.Should().Contain("<dashboard:SensorsControl x:Name=\"_sensors\" />");
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

    private static string ExtractXamlRange(string xaml, string startMarker, string endMarker)
    {
        var start = xaml.IndexOf(startMarker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var end = xaml.IndexOf(endMarker, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);

        return xaml[start..end];
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

    private static string ExtractSelfClosingElement(string xaml, string marker)
    {
        var markerIndex = xaml.IndexOf(marker, StringComparison.Ordinal);
        markerIndex.Should().BeGreaterThanOrEqualTo(0);

        var start = xaml.LastIndexOf('<', markerIndex);
        start.Should().BeGreaterThanOrEqualTo(0);

        var end = xaml.IndexOf("/>", markerIndex, StringComparison.Ordinal);
        end.Should().BeGreaterThan(markerIndex);

        return xaml[start..(end + 2)];
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string ReadSensorsControlXaml()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Controls", "Dashboard", "SensorsControl.xaml"));
    }

    private static string ReadSensorsControlCode()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Controls", "Dashboard", "SensorsControl.xaml.cs"));
    }

    private static string ReadSensorDetailsWindowXaml()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Windows", "Dashboard", "SensorDetailsWindow.xaml"));
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
