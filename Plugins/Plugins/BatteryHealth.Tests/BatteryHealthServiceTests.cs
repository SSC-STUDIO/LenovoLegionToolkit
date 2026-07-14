using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.BatteryHealth;
using UniversalDeviceToolkit.Plugins.Shared;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.BatteryHealth.Tests;

public class BatteryHealthServiceTests
{
    [Fact]
    public void BatteryHealthReport_DefaultState_IsUnknown()
    {
        var report = new BatteryHealthReport();
        Assert.Equal(BatteryHealthStatus.Unknown, report.Status);
        Assert.Equal(0, report.DesignCapacity);
        Assert.Equal(0, report.FullChargeCapacity);
        Assert.Equal(0, report.CycleCount);
        Assert.Equal(0, report.HealthPercentage);
        Assert.Equal(0, report.WearPercentage);
    }

    [Fact]
    public void BatteryHealthReport_SetProperties_RetainsValues()
    {
        var report = new BatteryHealthReport
        {
            DesignCapacity = 80000,
            FullChargeCapacity = 72000,
            CycleCount = 150,
            EstimatedChargeRemaining = 65000,
            HealthPercentage = 90,
            WearPercentage = 10,
            Status = BatteryHealthStatus.Healthy
        };

        Assert.Equal(80000, report.DesignCapacity);
        Assert.Equal(72000, report.FullChargeCapacity);
        Assert.Equal(150, report.CycleCount);
        Assert.Equal(65000, report.EstimatedChargeRemaining);
        Assert.Equal(90, report.HealthPercentage);
        Assert.Equal(10, report.WearPercentage);
        Assert.Equal(BatteryHealthStatus.Healthy, report.Status);
    }

    [Theory]
    [InlineData(BatteryHealthStatus.NoBattery)]
    [InlineData(BatteryHealthStatus.Healthy)]
    [InlineData(BatteryHealthStatus.Warning)]
    [InlineData(BatteryHealthStatus.Critical)]
    [InlineData(BatteryHealthStatus.Unknown)]
    public void BatteryHealthReport_AllStatuses_AreAssignable(BatteryHealthStatus status)
    {
        var report = new BatteryHealthReport { Status = status };
        Assert.Equal(status, report.Status);
    }

    [Fact]
    public void Settings_CorruptedJson_LoadReturnsDefaults()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "BHCorrupt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        var manager = new SettingsManager<BatteryHealthSettings>("corrupt-test", null, testDir);

        var pluginDir3 = Path.Combine(testDir, "corrupt-test");
        Directory.CreateDirectory(pluginDir3);
        File.WriteAllText(Path.Combine(pluginDir3, "settings.json"), "{ this is not valid json }}}");

        var loaded = manager.Load();

        Assert.NotNull(loaded);
        Assert.True(loaded.EnableRealTimeMonitoring);
        Assert.Equal(80, loaded.LowHealthThreshold);
        Assert.Equal(60, loaded.CriticalHealthThreshold);
    }

    [Fact]
    public void Settings_EmptyFile_LoadReturnsDefaults()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "BHEmpty", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        var manager = new SettingsManager<BatteryHealthSettings>("empty-test", null, testDir);

        var pluginDir2 = Path.Combine(testDir, "empty-test");
        Directory.CreateDirectory(pluginDir2);
        File.WriteAllText(Path.Combine(pluginDir2, "settings.json"), "");

        var loaded = manager.Load();

        Assert.NotNull(loaded);
        Assert.True(loaded.EnableRealTimeMonitoring);
    }

    [Fact]
    public void Settings_PartialJson_LoadReturnsDefaults()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "BHPartial", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        var manager = new SettingsManager<BatteryHealthSettings>("partial-test", null, testDir);

        var pluginDir = Path.Combine(testDir, "partial-test");
        Directory.CreateDirectory(pluginDir);
        File.WriteAllText(Path.Combine(pluginDir, "settings.json"), "{\"LowHealthThreshold\": 75}");

        var loaded = manager.Load();

        Assert.NotNull(loaded);
        Assert.Equal(75, loaded.LowHealthThreshold);
        Assert.Equal(60, loaded.CriticalHealthThreshold);
    }

    [Fact]
    public async Task Settings_ConcurrentReads_ThreadSafe()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "BHConcurrent", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        var manager = new SettingsManager<BatteryHealthSettings>("concurrent-test", null, testDir);

        var settings = new BatteryHealthSettings { LowHealthThreshold = 85, CriticalHealthThreshold = 65 };
        manager.Save(settings);
        manager.Clear(false);

        var tasks = new Task<BatteryHealthSettings>[20];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() => manager.Load());
        }

        await Task.WhenAll(tasks);

        foreach (var task in tasks)
        {
            var r = await task;
            Assert.Equal(85, r.LowHealthThreshold);
        }
    }

    [Fact]
    public async Task Settings_ConcurrentWrites_ThreadSafe()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "BHConcurrentWrite", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        var manager = new SettingsManager<BatteryHealthSettings>("concurrent-write-test", null, testDir);

        var tasks = new Task[20];
        for (int i = 0; i < tasks.Length; i++)
        {
            var threshold = 50 + i;
            tasks[i] = Task.Run(() =>
            {
                var s = new BatteryHealthSettings { LowHealthThreshold = threshold, CriticalHealthThreshold = threshold - 10 };
                manager.Save(s);
            });
        }

        await Task.WhenAll(tasks);

        var loaded = manager.Load();
        Assert.NotNull(loaded);
        Assert.InRange(loaded.LowHealthThreshold, 50, 69);
    }

    [Fact]
    public void Settings_ClearAndReload_ReturnsDefaults()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "BHClear", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        var manager = new SettingsManager<BatteryHealthSettings>("clear-test", null, testDir);

        var original = new BatteryHealthSettings { LowHealthThreshold = 90, CriticalHealthThreshold = 70 };
        manager.Save(original);

        var changed = new BatteryHealthSettings { LowHealthThreshold = 50, CriticalHealthThreshold = 30 };
        manager.Save(changed);

        manager.Clear(false);
        var loaded = manager.Load();

        Assert.Equal(50, loaded.LowHealthThreshold);
    }

    [Fact]
    public async Task Settings_SaveAsync_CorrectlyPersisted()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "BHAsync", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        var manager = new SettingsManager<BatteryHealthSettings>("async-test", null, testDir);

        var settings = new BatteryHealthSettings { EnableRealTimeMonitoring = false, LowHealthThreshold = 77 };
        var result = await manager.SaveAsync(settings);

        Assert.True(result);
        manager.Clear(false);
        var loaded = manager.Load();

        Assert.False(loaded.EnableRealTimeMonitoring);
        Assert.Equal(77, loaded.LowHealthThreshold);
    }
}
