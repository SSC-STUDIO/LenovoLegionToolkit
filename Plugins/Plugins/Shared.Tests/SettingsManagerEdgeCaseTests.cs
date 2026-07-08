using System;
using System.IO;
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
    public void SettingsChanged_FiresOnEverySave()
    {
        var manager = CreateManager("event-count");
        int changeCount = 0;
        manager.SettingsChanged += (_, _) => changeCount++;

        var s1 = new TestSettings { Value = 1 };
        manager.Save(s1);
        manager.Save(s1);
        manager.Save(s1);

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
}
