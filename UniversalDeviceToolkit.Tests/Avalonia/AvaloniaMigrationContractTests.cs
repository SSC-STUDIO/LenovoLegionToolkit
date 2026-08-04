using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Lifecycle;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.WPF.CLI;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaMigrationContractTests
{
    [Fact]
    public void WpfIpcServer_ImplementsSharedCliLifecycleContract()
    {
        typeof(IpcServer).GetInterfaces().Should().Contain(typeof(ICliHostLifecycle));
    }

    [Theory]
    [InlineData("CPU Usage", "CPU", "cpu")]
    [InlineData("GPU Temperature", "Temperature", "gpu")]
    [InlineData("Battery Charge", "Battery", "battery")]
    [InlineData("Memory Total", "Memory", "system")]
    public void DashboardTelemetryGroups_ClassifiesAdapterReadings(
        string name,
        string category,
        string expectedGroup)
    {
        var reading = new SensorReadingItem(name, "1", category, 1, "%");

        DashboardTelemetryGroups.Classify(reading).Should().Be(expectedGroup);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void SensorReadingItem_OnlyExposesPercentValuesAsProgress(double value, bool expected)
    {
        var reading = new SensorReadingItem("CPU Usage", "value", "CPU", value, "%");

        reading.HasProgress.Should().Be(expected);
    }

    [Fact]
    public void LanguagePackService_ReportsBuiltInEnglishWithoutNetworkAccess()
    {
        var service = AvaloniaLanguagePackServiceFactory.Create();
        var english = System.Globalization.CultureInfo.GetCultureInfo("en");

        service.IsAvailable.Should().BeTrue();
        service.IsEnglish(english).Should().BeTrue();
        service.IsInstalled(english).Should().BeTrue();
    }

    [Theory]
    [InlineData("macro-record:60", 0x60UL)]
    [InlineData("MACRO-RECORD:69", 0x69UL)]
    public void MacroRecordActionKeys_TargetOnlySupportedKeyboardSlots(string actionKey, ulong expectedKey)
    {
        FeatureActionContract.TryParseMacroRecordKey(actionKey, out var key).Should().BeTrue();
        key.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData("macro-record:00")]
    [InlineData("macro-record:6A")]
    [InlineData("macro-record:not-a-key")]
    [InlineData("macro-key:60")]
    public void MacroRecordActionKeys_RejectInvalidOrWrongActionPrefixes(string actionKey)
    {
        FeatureActionContract.TryParseMacroRecordKey(actionKey, out _).Should().BeFalse();
    }
}
