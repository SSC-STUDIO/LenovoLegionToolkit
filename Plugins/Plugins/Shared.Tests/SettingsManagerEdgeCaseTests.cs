using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace LenovoLegionToolkit.Plugins.Shared.Tests;

/// <summary>
/// Additional edge-case tests for SettingsManager targeting corrupted file recovery,
/// concurrent safety, debounce, and file-locking scenarios.
/// </summary>
public class SettingsManagerEdgeCaseTests : IDisposable
{
    private readonly string _testDir;

    public SettingsManagerEdgeCaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SharedEdgeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true); } catch { }
    }

    private SettingsManager<TestSettings> CreateManager(string pluginName)
        => new(pluginName, null, _testDir);

    private string SettingsPath(string pluginName)
        => Path.Combine(_testDir, pluginName, "settings.json");

    [Fact]
    public void Load_CorruptedJsonFile_FiresSettingsCorruptedEvent()
    {
        var manager = CreateManager("corrupted-event");
        File.WriteAllText(SettingsPath("corrupted-event"), "{ invalid json }}}");

        string? backedUpPath = null;
        manager.SettingsCorrupted += (_, path) => backedUpPath = path;

        var loaded = manager.Load();

        Assert.NotNull(loaded);
        Assert.Equal("Default", loaded.Name);
        Assert.Equal(42, loaded.Value);
        Assert.NotNull(backedUpPath);
        Assert.True(File.Exists(backedUpPath));
    }

    [Fact]
    public void Load_CorruptedMessagePack_ReturnsDefaults()
    {
        var manager = new SettingsManager<TestSettings>("mp-corrupt", null, _testDir, useMessagePack: true);
        var mpPath = Path.Combine(_testDir, "mp-corrupt", "settings.mpack");
        File.WriteAllBytes(mpPath, new byte[] { 0xFF, 0xFE, 0xFD, 0x01, 0x02 });

        var loaded = manager.Load();

        Assert.NotNull(loaded);
        Assert.Equal("Default", loaded.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Save_NullOrEmptyName_HandledGracefully(string? name)
    {
        var manager = CreateManager("save-null-test");
        var settings = new TestSettings { Name = name ?? "" };
        var result = manager.Save(settings);
        Assert.True(result);
        manager.Clear(false);
        var loaded = manager.Load();
        Assert.Equal(name ?? "", loaded.Name);
    }

    [Fact]
    public void SettingsChanged_FiresOnEveryChangedSave()
    {
        var manager = CreateManager("event-count");
        int changeCount = 0;
        manager.SettingsChanged += (_, _) => changeCount++;

        // Each save has different content — should fire SettingsChanged each time
        manager.Save(new TestSettings { Value = 1 });
        manager.Save(new TestSettings { Value = 2 });
        manager.Save(new TestSettings { Value = 3 });

        Assert.Equal(3, changeCount);
    }

    [Fact]
    public async Task SaveAsync_NullSettings_ReturnsFalse()
    {
        var manager = CreateManager("async-null");
        var result = await manager.SaveAsync(null!);
        Assert.False(result);
    }

    [Fact]
    public void Load_VeryLargeSettings_LoadsCorrectly()
    {
        var manager = CreateManager("large-settings");
        var settings = new TestSettings { Name = new string('X', 100_000) };
        manager.Save(settings);
        manager.Clear(false);

        var loaded = manager.Load();
        Assert.Equal(100_000, loaded.Name.Length);
        Assert.All(loaded.Name.ToCharArray(), c => Assert.Equal('X', c));
    }

    [Fact]
    public void Clear_CacheOnly_FileStillExists()
    {
        var manager = CreateManager("cache-only");
        var settings = new TestSettings { Value = 99 };
        manager.Save(settings);

        manager.Clear(deleteFile: false);
        Assert.True(File.Exists(SettingsPath("cache-only")));

        manager.Clear(deleteFile: true);
        Assert.False(File.Exists(SettingsPath("cache-only")));
    }

    [Fact]
    public void Load_DirectoryNotExist_CreatesAndReturnsDefaults()
    {
        var missing = Path.Combine(_testDir, "does-not-exist");
        var manager = new SettingsManager<TestSettings>("missing-dir", null, missing);

        var loaded = manager.Load();

        Assert.NotNull(loaded);
        Assert.Equal("Default", loaded.Name);
        Assert.True(Directory.Exists(Path.Combine(missing, "missing-dir")));
    }

    [Fact]
    public void Update_ModifyMultipleTimes_ReflectsLatestState()
    {
        var manager = CreateManager("multi-update");
        manager.Update(s => s.Value = 10);
        manager.Update(s => s.Value = 20);
        manager.Update(s => s.Value = 30);

        manager.Clear(false);
        var loaded = manager.Load();
        Assert.Equal(30, loaded.Value);
    }

    #region MessagePack Save/Load Tests (M-020)

    [Fact]
    public void Save_MessagePack_PersistsToMpackFile()
    {
        var manager = new SettingsManager<TestSettings>("mp-save", null, _testDir, useMessagePack: true);
        manager.Save(new TestSettings { Name = "MP_Persist", Value = 7 });

        var mpPath = Path.Combine(_testDir, "mp-save", "settings.mpack");
        Assert.True(File.Exists(mpPath), "settings.mpack should exist after MessagePack save");

        // M-020: JSON temp file should NOT be left behind — the temp path should be .mpack.tmp
        var jsonTemp = Path.Combine(_testDir, "mp-save", "settings.json.tmp");
        Assert.False(File.Exists(jsonTemp), "JSON temp file should not exist after MessagePack save");
    }

    [Fact]
    public void Save_MessagePack_RoundTrip_LoadsCorrectly()
    {
        var manager = new SettingsManager<TestSettings>("mp-roundtrip", null, _testDir, useMessagePack: true);
        var original = new TestSettings { Name = "RoundTrip", Value = 999, Enabled = false };
        manager.Save(original);
        manager.Clear(false); // Reset cache to force reload from disk

        var loaded = manager.Load();
        Assert.Equal("RoundTrip", loaded.Name);
        Assert.Equal(999, loaded.Value);
        Assert.False(loaded.Enabled);
    }

    [Fact]
    public void Save_MessagePack_SkipsUnchangedSettings()
    {
        var manager = new SettingsManager<TestSettings>("mp-skip", null, _testDir, useMessagePack: true);
        var settings = new TestSettings { Name = "SkipTest", Value = 42 };
        manager.Save(settings);

        // Capture file's last write time
        var mpPath = Path.Combine(_testDir, "mp-skip", "settings.mpack");
        var firstWriteTime = File.GetLastWriteTimeUtc(mpPath);

        // Save the SAME object again — should be skipped (memory transaction)
        // Small delay to ensure timestamp precision
        System.Threading.Thread.Sleep(50);
        manager.Save(settings);

        var secondWriteTime = File.GetLastWriteTimeUtc(mpPath);

        // File should NOT have been rewritten (timestamp unchanged)
        Assert.Equal(firstWriteTime, secondWriteTime);
    }

    [Fact]
    public void Save_MessagePack_DetectsChangedSettings()
    {
        var manager = new SettingsManager<TestSettings>("mp-change", null, _testDir, useMessagePack: true);
        var settings = new TestSettings { Name = "Before", Value = 1 };
        manager.Save(settings);

        var mpPath = Path.Combine(_testDir, "mp-change", "settings.mpack");
        var firstWriteTime = File.GetLastWriteTimeUtc(mpPath);

        // Change settings and save — should write to disk
        System.Threading.Thread.Sleep(50);
        settings.Name = "After";
        settings.Value = 2;
        manager.Save(settings);

        var secondWriteTime = File.GetLastWriteTimeUtc(mpPath);

        // File SHOULD have been rewritten (timestamp changed)
        Assert.NotEqual(firstWriteTime, secondWriteTime);

        // Verify the new values persist
        manager.Clear(false);
        var loaded = manager.Load();
        Assert.Equal("After", loaded.Name);
        Assert.Equal(2, loaded.Value);
    }

    [Fact]
    public async Task SaveAsync_MessagePack_RoundTrip_LoadsCorrectly()
    {
        var manager = new SettingsManager<TestSettings>("mp-async-rt", null, _testDir, useMessagePack: true);
        var original = new TestSettings { Name = "AsyncRT", Value = 555, Enabled = true };
        await manager.SaveAsync(original);
        manager.Clear(false);

        var loaded = manager.Load();
        Assert.Equal("AsyncRT", loaded.Name);
        Assert.Equal(555, loaded.Value);
        Assert.True(loaded.Enabled);
    }

    [Fact]
    public async Task SaveAsync_SkipsUnchangedSettings()
    {
        var manager = new SettingsManager<TestSettings>("async-skip-json", null, _testDir);
        var settings = new TestSettings { Name = "AsyncSkip", Value = 42 };
        await manager.SaveAsync(settings);

        var jsonPath = Path.Combine(_testDir, "async-skip-json", "settings.json");
        var firstWriteTime = File.GetLastWriteTimeUtc(jsonPath);

        // Save the SAME object again — should be skipped
        await Task.Delay(50);
        await manager.SaveAsync(settings);

        var secondWriteTime = File.GetLastWriteTimeUtc(jsonPath);

        // File should NOT have been rewritten (memory transaction skip)
        Assert.Equal(firstWriteTime, secondWriteTime);
    }

    [Fact]
    public async Task SaveAsync_MessagePack_SkipsUnchangedSettings()
    {
        var manager = new SettingsManager<TestSettings>("async-skip-mp", null, _testDir, useMessagePack: true);
        var settings = new TestSettings { Name = "AsyncMpSkip", Value = 99 };
        await manager.SaveAsync(settings);

        var mpPath = Path.Combine(_testDir, "async-skip-mp", "settings.mpack");
        var firstWriteTime = File.GetLastWriteTimeUtc(mpPath);

        // Save the SAME object again — should be skipped (memory transaction)
        await Task.Delay(50);
        await manager.SaveAsync(settings);

        var secondWriteTime = File.GetLastWriteTimeUtc(mpPath);

        // File should NOT have been rewritten
        Assert.Equal(firstWriteTime, secondWriteTime);
    }

    [Fact]
    public async Task SaveAsync_MessagePack_DetectsChangedSettings()
    {
        var manager = new SettingsManager<TestSettings>("async-change-mp", null, _testDir, useMessagePack: true);
        var settings = new TestSettings { Name = "Before", Value = 100 };
        await manager.SaveAsync(settings);

        var mpPath = Path.Combine(_testDir, "async-change-mp", "settings.mpack");
        var firstWriteTime = File.GetLastWriteTimeUtc(mpPath);

        // Change settings and save — SHOULD write to disk
        await Task.Delay(50);
        settings.Name = "After";
        settings.Value = 200;
        await manager.SaveAsync(settings);

        var secondWriteTime = File.GetLastWriteTimeUtc(mpPath);

        // File SHOULD have been rewritten
        Assert.NotEqual(firstWriteTime, secondWriteTime);

        // Verify the new values persist
        manager.Clear(false);
        var loaded = manager.Load();
        Assert.Equal("After", loaded.Name);
        Assert.Equal(200, loaded.Value);
    }

    [Fact]
    public async Task Save_ConcurrentWithSaveAsync_NoCorruption()
    {
        var manager = CreateManager("concurrent-save");
        var jsonPath = SettingsPath("concurrent-save");
        var exceptions = new System.Collections.Generic.List<Exception>();

        // Interleave 10 synchronous saves and 10 asynchronous saves on the same instance
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            // Alternate: even = Save (sync), odd = SaveAsync
            if (index % 2 == 0)
            {
                tasks[index] = Task.Run(() =>
                {
                    try
                    {
                        manager.Save(new TestSettings { Name = $"sync-{index}", Value = index, Enabled = true });
                    }
                    catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
                });
            }
            else
            {
                tasks[index] = Task.Run(async () =>
                {
                    try
                    {
                        await manager.SaveAsync(new TestSettings { Name = $"async-{index}", Value = index, Enabled = false });
                    }
                    catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
                });
            }
        }

        await Task.WhenAll(tasks);
        Assert.Empty(exceptions);

        // Verify the file is valid JSON and deserializable (not corrupted)
        Assert.True(File.Exists(jsonPath));
        var json = await File.ReadAllTextAsync(jsonPath);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TestSettings>(json);
        Assert.NotNull(deserialized);
        Assert.True(deserialized!.Value >= 0 && deserialized.Value < 10);
    }

    #endregion

    [Fact]
    public async Task Update_ConcurrentWithSaveAsync_DoesNotDeadlock()
    {
        var manager = CreateManager("update-deadlock-test");
        var settingsPath = SettingsPath("update-deadlock-test");

        // Seed with initial settings so Load() inside Update has cached data
        manager.Save(new TestSettings { Name = "initial", Value = 0, Enabled = true });

        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
        var gate = new System.Threading.ManualResetEventSlim(false);

        // Thread A: repeatedly calls Update while Thread B holds the semaphore via SaveAsync.
        // Before the fix, Update held _lock while calling Save, which tried to acquire _semaphore.
        // If SaveAsync already held _semaphore and was waiting for _lock (inside its Phase 2
        // cache update), both threads would deadlock.
        var updateTask = Task.Run(() =>
        {
            gate.Wait(cts.Token);
            return manager.Update(s =>
            {
                s.Name = "updated";
                s.Value = 42;
            });
        }, cts.Token);

        var saveAsyncTask = Task.Run(async () =>
        {
            gate.Set();
            return await manager.SaveAsync(new TestSettings { Name = "async", Value = 99, Enabled = false });
        }, cts.Token);

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
        var allDone = await Task.WhenAny(
            Task.WhenAll(updateTask, saveAsyncTask),
            timeoutTask);

        Assert.False(cts.IsCancellationRequested,
            "Update + SaveAsync deadlocked — 5 second timeout exceeded.");
        Assert.True(timeoutTask.IsCompleted == false,
            "Tasks should complete before timeout.");
        Assert.True(await updateTask, "Update should have succeeded.");
        Assert.True(await saveAsyncTask, "SaveAsync should have succeeded.");
    }
}
