using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class OsdSettingsServiceTests
{
    [Fact]
    public async Task WindowsApplicationPage_ExposesMigratedOsdOptions()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var service = AvaloniaSettingsServiceFactory.Create();
        var page = await service.GetPageAsync("Application");
        var keys = page.Options.Select(option => option.Key).ToHashSet(StringComparer.Ordinal);

        keys.Should().Contain([
            "ShowOsd",
            "OsdStyle",
            "OsdRefreshInterval",
            "OsdSnapThreshold",
            "OsdLockPosition",
            "OsdResetPosition",
            "OsdOpacity",
            "OsdCornerRadiusTop",
            "OsdCornerRadiusBottom",
            "OsdFontSize",
            "OsdBackgroundColor",
            "OsdCategoryColor",
            "OsdLabelColor",
            "OsdValueColor",
            "OsdWarningColor",
            "OsdCriticalColor",
            "OsdSeparatorColor",
            "OsdItems",
            "OsdTempWarning",
            "OsdTempCritical",
            "OsdUsageWarning",
            "OsdUsageCritical",
            "OsdFpsCritical",
            "OsdLowFpsDelta",
            "HardwareSectionsVisible",
            "HardwareSectionsOrder",
            "HardwareSelectedGpuIsIgpu",
            "HardwareCpuAverageFrequency",
            "HardwareMemoryInGigabytes",
        ]);

        page.Options.Single(option => option.Key == "OsdStyle").Values
            .Should().BeEquivalentTo(["Panel", "Bar"]);
        page.Options.Single(option => option.Key == "HardwareSectionsOrder").Values
            .Should().Contain("GPU, Battery, CPU");
    }

    [Theory]
    [InlineData("OsdRefreshInterval", "0")]
    [InlineData("OsdRefreshInterval", "11")]
    [InlineData("OsdOpacity", "1.1")]
    [InlineData("OsdFontSize", "7")]
    [InlineData("OsdTempWarning", "111")]
    [InlineData("OsdUsageCritical", "101")]
    [InlineData("OsdFpsCritical", "1001")]
    [InlineData("OsdBackgroundColor", "not-a-color")]
    public async Task WindowsOsdTextOptions_RejectValuesOutsideWpfRanges(string optionKey, string value)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var service = AvaloniaSettingsServiceFactory.Create();

        var action = () => service.SetTextAsync("Application", optionKey, value);
        await action.Should().ThrowAsync<ArgumentException>();
    }
}
