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
