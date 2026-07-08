using LenovoLegionToolkit.Plugins.BatteryHealth;
using LenovoLegionToolkit.Plugins.Shared;
using LenovoLegionToolkit.Plugins.TestCommon;
using System.Text.Json;
using Xunit;

namespace LenovoLegionToolkit.Plugins.BatteryHealth.Tests;

[Collection("BatteryHealthResourceCulture")]
public class BatteryHealthSettingsEdgeCaseTests
{
    [Fact]
    public void Settings_DefaultValues_AreReasonable()
    {
        var settings = new BatteryHealthSettings();

        Assert.True(settings.EnableRealTimeMonitoring);
        Assert.Equal(80, settings.LowHealthThreshold);
        Assert.Equal(60, settings.CriticalHealthThreshold);
        Assert.True(settings.EnableNotification);
    }

    [Fact]
    public void Settings_ThresholdBoundary_LowEqualsCritical_IsValid()
    {
        var settings = new BatteryHealthSettings
        {
            LowHealthThreshold = 60,
            CriticalHealthThreshold = 60
        };

        Assert.Equal(settings.LowHealthThreshold, settings.CriticalHealthThreshold);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Settings_LowThreshold_AtBoundaryValues(int threshold)
    {
        var settings = new BatteryHealthSettings { LowHealthThreshold = threshold };
        Assert.Equal(threshold, settings.LowHealthThreshold);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Settings_CriticalThreshold_AtBoundaryValues(int threshold)
    {
        var settings = new BatteryHealthSettings { CriticalHealthThreshold = threshold };
        Assert.Equal(threshold, settings.CriticalHealthThreshold);
    }

    [Fact]
    public void Settings_ToggleMonitoring_ChangesState()
    {
        var settings = new BatteryHealthSettings { EnableRealTimeMonitoring = false };
        Assert.False(settings.EnableRealTimeMonitoring);

        settings.EnableRealTimeMonitoring = true;
        Assert.True(settings.EnableRealTimeMonitoring);
    }

    [Fact]
    public void Settings_ToggleNotification_ChangesState()
    {
        var settings = new BatteryHealthSettings { EnableNotification = false };
        Assert.False(settings.EnableNotification);

        settings.EnableNotification = true;
        Assert.True(settings.EnableNotification);
    }

    [Fact]
    public void Settings_JsonRoundTrip_PreservesAllValues()
    {
        var original = new BatteryHealthSettings
        {
            EnableRealTimeMonitoring = false,
            LowHealthThreshold = 75,
            CriticalHealthThreshold = 45,
            EnableNotification = false
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<BatteryHealthSettings>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.EnableRealTimeMonitoring, deserialized.EnableRealTimeMonitoring);
        Assert.Equal(original.LowHealthThreshold, deserialized.LowHealthThreshold);
        Assert.Equal(original.CriticalHealthThreshold, deserialized.CriticalHealthThreshold);
        Assert.Equal(original.EnableNotification, deserialized.EnableNotification);
    }

    [Fact]
    public void Settings_DeserializeFromPartialJson_UsesDefaults()
    {
        var json = "{}";
        var deserialized = JsonSerializer.Deserialize<BatteryHealthSettings>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.EnableRealTimeMonitoring);
        Assert.Equal(80, deserialized.LowHealthThreshold);
    }
}

[Collection("BatteryHealthResourceCulture")]
public class BatteryHealthReportEdgeCaseTests
{
    [Fact]
    public void Report_ZeroDesignCapacity_StatusIsUnknown()
    {
        var report = new BatteryHealthReport
        {
            DesignCapacity = 0,
            FullChargeCapacity = 0,
            HealthPercentage = 0,
            Status = BatteryHealthStatus.Unknown
        };

        Assert.Equal(0, report.DesignCapacity);
        Assert.Equal(BatteryHealthStatus.Unknown, report.Status);
    }

    [Fact]
    public void Report_ZeroWearPercentage_WhenFullCapacity()
    {
        var report = new BatteryHealthReport
        {
            DesignCapacity = 80000,
            FullChargeCapacity = 80000,
            HealthPercentage = 100,
            WearPercentage = 0,
            Status = BatteryHealthStatus.Healthy
        };

        Assert.Equal(100, report.HealthPercentage);
        Assert.Equal(0, report.WearPercentage);
    }

    [Fact]
    public void Report_HundredPercentWear_WhenEmpty()
    {
        var report = new BatteryHealthReport
        {
            DesignCapacity = 80000,
            FullChargeCapacity = 0,
            HealthPercentage = 0,
            WearPercentage = 100,
            Status = BatteryHealthStatus.Critical
        };

        Assert.Equal(0, report.HealthPercentage);
        Assert.Equal(100, report.WearPercentage);
    }

    [Fact]
    public void Report_LargeCycleCount_HandlesCorrectly()
    {
        var report = new BatteryHealthReport
        {
            DesignCapacity = 80000,
            FullChargeCapacity = 64000,
            CycleCount = 10000,
            HealthPercentage = 80,
            WearPercentage = 20,
            Status = BatteryHealthStatus.Healthy
        };

        Assert.Equal(10000, report.CycleCount);
    }

    [Fact]
    public void BatteryHealthStatus_AllValues_AreDefined()
    {
        Assert.Equal(5, System.Enum.GetValues<BatteryHealthStatus>().Length);
        Assert.Contains(BatteryHealthStatus.Healthy, System.Enum.GetValues<BatteryHealthStatus>());
        Assert.Contains(BatteryHealthStatus.Warning, System.Enum.GetValues<BatteryHealthStatus>());
        Assert.Contains(BatteryHealthStatus.Critical, System.Enum.GetValues<BatteryHealthStatus>());
        Assert.Contains(BatteryHealthStatus.NoBattery, System.Enum.GetValues<BatteryHealthStatus>());
        Assert.Contains(BatteryHealthStatus.Unknown, System.Enum.GetValues<BatteryHealthStatus>());
    }
}
