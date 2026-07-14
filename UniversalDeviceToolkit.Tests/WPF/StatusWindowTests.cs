using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class StatusWindowTests
{
    [Fact]
    public void FormatSensorSummary_WhenTemperatureAndPowerExist_ShouldCombineBoth()
    {
        var text = StatusWindow.FormatSensorSummary((71f, 58.2f, -1f), TemperatureUnit.C);

        text.Should().Be($"{UniversalDeviceToolkit.WPF.Controls.Dashboard.SensorsControl.FormatTemperature(71f, TemperatureUnit.C)} | 58.2 W");
    }

    [Fact]
    public void FormatSensorSummary_WhenTemperaturePowerAndVoltageExist_ShouldCombineAll()
    {
        var text = StatusWindow.FormatSensorSummary((71f, 58.2f, 1.127f), TemperatureUnit.C);

        text.Should().Be($"{UniversalDeviceToolkit.WPF.Controls.Dashboard.SensorsControl.FormatTemperature(71f, TemperatureUnit.C)} | 58.2 W | 1.127 V");
    }

    [Fact]
    public void FormatSensorSummary_WhenOnlyVoltageExists_ShouldReturnVoltage()
    {
        var text = StatusWindow.FormatSensorSummary((-1f, -1f, 0.981f), TemperatureUnit.C);

        text.Should().Be("0.981 V");
    }

    [Fact]
    public void FormatSensorSummary_WhenOnlyGpuSensorValuesExist_ShouldStillReturnSummary()
    {
        var text = StatusWindow.FormatSensorSummary((64f, 82f, 0.981f), TemperatureUnit.C);

        text.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FormatSensorSummary_WhenNoMetricsExist_ShouldReturnNull()
    {
        var text = StatusWindow.FormatSensorSummary((-1f, -1f, -1f), TemperatureUnit.C);

        text.Should().BeNull();
    }

    [Fact]
    public void FormatMemorySummary_WhenMemoryMetricsExist_ShouldReturnUsageText()
    {
        var text = StatusWindow.FormatMemorySummary((12.5f, 32f, 39f, -1d), TemperatureUnit.C);

        text.Should().Be("12.5 / 32.0 GB (39%)");
    }

    [Fact]
    public void FormatMemorySummary_WhenNoMetricsExist_ShouldReturnNull()
    {
        var text = StatusWindow.FormatMemorySummary((-1f, -1f, -1f, -1d), TemperatureUnit.C);

        text.Should().BeNull();
    }

    [Fact]
    public void FormatMemorySummary_WhenTemperatureExists_ShouldAppendTemperature()
    {
        var text = StatusWindow.FormatMemorySummary((12.5f, 32f, 39f, 54d), TemperatureUnit.C);

        text.Should().Be("12.5 / 32.0 GB (39%) | 54 °C");
    }

    [Fact]
    public void FormatSsdSummary_WhenTemperaturesExist_ShouldReturnPair()
    {
        var text = StatusWindow.FormatSsdSummary((45f, 52f), TemperatureUnit.C);

        text.Should().Be("45 °C / 52 °C");
    }

    [Fact]
    public void FormatSsdSummary_WhenNoTemperaturesExist_ShouldReturnNull()
    {
        var text = StatusWindow.FormatSsdSummary((-1f, -1f), TemperatureUnit.C);

        text.Should().BeNull();
    }
}
