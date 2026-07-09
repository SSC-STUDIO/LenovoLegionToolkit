using System.Text.Json;
using System;
using System.IO;
using LenovoLegionToolkit.Plugins.Shared;
using LenovoLegionToolkit.Plugins.BatteryHealth;
using Xunit;

namespace LenovoLegionToolkit.Plugins.BatteryHealth.Tests;

public class BatteryHealthSettingsTests
{
    [Fact]
    public void DefaultSettings_HaveExpectedValues()
    {
        var settings = new BatteryHealthSettings();

        Assert.True(settings.EnableRealTimeMonitoring);
        Assert.Equal(80, settings.LowHealthThreshold);
        Assert.Equal(60, settings.CriticalHealthThreshold);
        Assert.True(settings.EnableNotification);
    }

    [Fact]
    public void Settings_WithCustomValues_RoundTripsViaJson()
    {
        var original = new BatteryHealthSettings
        {
            EnableRealTimeMonitoring = false,
            LowHealthThreshold = 70,
            CriticalHealthThreshold = 50,
            EnableNotification = false
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<BatteryHealthSettings>(json);

        Assert.NotNull(deserialized);
        Assert.False(deserialized!.EnableRealTimeMonitoring);
        Assert.Equal(70, deserialized.LowHealthThreshold);
        Assert.Equal(50, deserialized.CriticalHealthThreshold);
        Assert.False(deserialized.EnableNotification);
    }

    [Theory]
    [InlineData(80, 60)]
    [InlineData(90, 70)]
    [InlineData(95, 10)]
    [InlineData(70, 69)]
    public void Thresholds_ValidCombinations(int low, int critical)
    {
        var settings = new BatteryHealthSettings
        {
            LowHealthThreshold = low,
            CriticalHealthThreshold = critical
        };

        Assert.True(critical < low, $"Expected critical ({critical}) < low ({low})");
    }

    [Theory]
    [InlineData(80, 80)]
    [InlineData(80, 90)]
    [InlineData(70, 70)]
    public void Thresholds_InvalidCombinations(int low, int critical)
    {
        var settings = new BatteryHealthSettings
        {
            LowHealthThreshold = low,
            CriticalHealthThreshold = critical
        };

        Assert.False(critical < low, $"Expected settings to be invalid: critical ({critical}) >= low ({low})");
        Assert.False(settings.AreThresholdsValid);
    }

    [Fact]
    public void AreThresholdsValid_DefaultSettings_IsTrue()
    {
        var settings = new BatteryHealthSettings();
        Assert.True(settings.AreThresholdsValid);
    }

    [Theory]
    [InlineData(80, 60, true)]
    [InlineData(90, 70, true)]
    [InlineData(50, 49, true)]
    [InlineData(80, 80, false)]
    [InlineData(80, 90, false)]
    [InlineData(70, 70, false)]
    [InlineData(-1, 60, false)]
    [InlineData(101, 60, false)]
    [InlineData(80, -1, false)]
    [InlineData(80, 101, false)]
    public void AreThresholdsValid_VariousCombinations(int low, int critical, bool expected)
    {
        var settings = new BatteryHealthSettings
        {
            LowHealthThreshold = low,
            CriticalHealthThreshold = critical
        };

        Assert.Equal(expected, settings.AreThresholdsValid);
    }

    [Theory]
    [InlineData(90, 95, true, 90, 70)]   // Critical > Low -> reposition Critical
    [InlineData(80, 80, true, 80, 60)]    // Critical == Low -> reposition Critical
    [InlineData(-5, 60, true, 80, 60)]   // Low out of range -> reset Low, reposition Critical
    [InlineData(105, 60, true, 80, 60)]  // Low out of range -> reset Low, reposition Critical
    [InlineData(80, -10, true, 80, 60)]  // Critical out of range -> reset Critical
    [InlineData(80, 200, true, 80, 60)]   // Critical out of range -> reset Critical
    [InlineData(80, 60, false, 80, 60)]    // Already valid -> no change, returns false
    [InlineData(70, 50, false, 70, 50)]    // Already valid different values -> no change
    public void EnsureValidThresholds_FixesInvalidConfiguration(
        int initialLow, int initialCritical, bool expectAdjusted,
        int expectedLow, int expectedCritical)
    {
        var settings = new BatteryHealthSettings
        {
            LowHealthThreshold = initialLow,
            CriticalHealthThreshold = initialCritical
        };

        var adjusted = settings.EnsureValidThresholds();
        Assert.Equal(expectAdjusted, adjusted);
        Assert.Equal(expectedLow, settings.LowHealthThreshold);
        Assert.Equal(expectedCritical, settings.CriticalHealthThreshold);
        Assert.True(settings.AreThresholdsValid,
            $"After EnsureValidThresholds, AreThresholdsValid should be true but got Low={settings.LowHealthThreshold}, Critical={settings.CriticalHealthThreshold}");
    }

    [Fact]
    public void EnsureValidThresholds_LowAtZero_DoesNotProduceNegativeCritical()
    {
        var settings = new BatteryHealthSettings
        {
            LowHealthThreshold = 0,
            CriticalHealthThreshold = 50
        };

        var adjusted = settings.EnsureValidThresholds();

        Assert.True(adjusted, "Should have adjusted because Critical(50) >= Low(0)");
        Assert.True(settings.CriticalHealthThreshold >= 0,
            $"Critical threshold should never go negative, got {settings.CriticalHealthThreshold}");
        Assert.True(settings.AreThresholdsValid,
            $"After adjustment, thresholds should be valid: Low={settings.LowHealthThreshold}, Critical={settings.CriticalHealthThreshold}");
    }

    [Fact]
    public void EnsureValidThresholds_LowAtBoundary_CriticalStaysInRange()
    {
        var settings = new BatteryHealthSettings
        {
            LowHealthThreshold = 10,
            CriticalHealthThreshold = 90
        };

        settings.EnsureValidThresholds();

        Assert.True(settings.CriticalHealthThreshold >= 0 && settings.CriticalHealthThreshold < settings.LowHealthThreshold);
        Assert.True(settings.AreThresholdsValid);
    }

    [Fact]
    public void EnsureValidThresholds_ConstantDefaults_MatchDefaultConstructor()
    {
        var settings = new BatteryHealthSettings();
        Assert.Equal(BatteryHealthSettings.DefaultLowHealthThreshold, settings.LowHealthThreshold);
        Assert.Equal(BatteryHealthSettings.DefaultCriticalHealthThreshold, settings.CriticalHealthThreshold);
    }

    [Fact]
    public void SettingsManager_RoundTripsSettings()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "BatteryHealthTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        var pluginName = "BatteryHealth-RoundTrip-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        try
        {
            var manager = new SettingsManager<BatteryHealthSettings>(pluginName, null, testDir);
            var settings = new BatteryHealthSettings
            {
                EnableRealTimeMonitoring = false,
                LowHealthThreshold = 75,
                CriticalHealthThreshold = 55,
                EnableNotification = false
            };

            var saveResult = manager.Save(settings);
            Assert.True(saveResult);

            manager.Clear(false);
            var loaded = manager.Load();

            Assert.False(loaded.EnableRealTimeMonitoring);
            Assert.Equal(75, loaded.LowHealthThreshold);
            Assert.Equal(55, loaded.CriticalHealthThreshold);
            Assert.False(loaded.EnableNotification);
        }
        finally
        {
            try
            {
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
            catch { }
        }
    }
}
