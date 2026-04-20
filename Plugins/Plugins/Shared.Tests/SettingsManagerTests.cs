using System;
using System.IO;
using System.Text.Json;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace LenovoLegionToolkit.Plugins.Shared.Tests;

// Test settings class for use in tests
public class TestSettings
{
    public string Name { get; set; } = "Default";
    public int Value { get; set; } = 42;
    public bool Enabled { get; set; } = true;
}

public class SettingsManagerTests
{
    private readonly string _testPluginName = "TestPlugin";
    private readonly string _testDirectory;

    public SettingsManagerTests()
    {
        // Create a temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "SharedTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    private SettingsManager<TestSettings> CreateManager(string? pluginName = null, ILogger? logger = null)
    {
        var name = pluginName ?? _testPluginName;
        return new SettingsManager<TestSettings>(name, logger, _testDirectory);
    }

    private string GetSettingsPath(string pluginName)
    {
        return Path.Combine(_testDirectory, pluginName, "settings.json");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidPluginName_CreatesInstance()
    {
        var manager = new SettingsManager<TestSettings>(_testPluginName, null, _testDirectory);
        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_NullPluginName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new SettingsManager<TestSettings>(null!));
    }

    [Fact]
    public void Constructor_EmptyPluginName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new SettingsManager<TestSettings>(""));
    }

    [Fact]
    public void Constructor_WhitespacePluginName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new SettingsManager<TestSettings>("   "));
    }

    [Fact]
    public void Constructor_WithLogger_CreatesInstance()
    {
        var loggerMock = new Mock<ILogger>();
        var manager = new SettingsManager<TestSettings>(_testPluginName, loggerMock.Object, _testDirectory);
        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_CreatesPluginDirectory()
    {
        // Clean up any existing directory first
        var expectedPath = Path.Combine(_testDirectory, _testPluginName);
        if (Directory.Exists(expectedPath))
        {
            try { Directory.Delete(expectedPath, true); } catch { }
        }

        var manager = new SettingsManager<TestSettings>(_testPluginName, null, _testDirectory);

        // Trigger Load to ensure directory is created
        manager.Load();

        Assert.True(Directory.Exists(expectedPath));
    }

    #endregion

    #region Load Tests

    [Fact]
    public void Load_NoExistingFile_ReturnsDefaultSettings()
    {
        var manager = CreateManager("LoadTest_NoFile");
        manager.Clear(true); // Ensure no file exists

        var settings = manager.Load();

        Assert.NotNull(settings);
        Assert.Equal("Default", settings.Name);
        Assert.Equal(42, settings.Value);
        Assert.True(settings.Enabled);
    }

    [Fact]
    public void Load_ReturnsCachedSettingsOnSecondCall()
    {
        var manager = CreateManager("LoadTest_Cache");

        var settings1 = manager.Load();
        var settings2 = manager.Load();

        Assert.Same(settings1, settings2);
    }

    [Fact]
    public void Load_ExistingFile_ReturnsSavedSettings()
    {
        var uniqueName = "LoadTest_Existing_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);

        // First create and save settings
        var originalSettings = new TestSettings
        {
            Name = "SavedName",
            Value = 100,
            Enabled = false
        };
        manager.Save(originalSettings);

        // Clear the manager to reset cache (simulate restart)
        manager.Clear(false);

        // Now load again
        var loadedSettings = manager.Load();

        Assert.Equal("SavedName", loadedSettings.Name);
        Assert.Equal(100, loadedSettings.Value);
        Assert.False(loadedSettings.Enabled);
    }

    [Fact]
    public void Load_LegacySettingsFile_MigratesAndLoadsSettings()
    {
        var uniqueName = "LoadTest_Legacy_" + Guid.NewGuid().ToString();
        var legacySettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", uniqueName, "settings.json");
        var manager = CreateManager(uniqueName);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(legacySettingsPath)!);
            File.WriteAllText(legacySettingsPath, JsonSerializer.Serialize(new TestSettings
            {
                Name = "LegacyName",
                Value = 314,
                Enabled = false
            }));

            var loadedSettings = manager.Load();

            Assert.Equal("LegacyName", loadedSettings.Name);
            Assert.Equal(314, loadedSettings.Value);
            Assert.False(loadedSettings.Enabled);
            Assert.True(File.Exists(GetSettingsPath(uniqueName)));
        }
        finally
        {
            try
            {
                var legacyDirectory = Path.GetDirectoryName(legacySettingsPath);
                if (!string.IsNullOrWhiteSpace(legacyDirectory) && Directory.Exists(legacyDirectory))
                    Directory.Delete(legacyDirectory, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    [Fact]
    public void Load_CorruptedJson_ReturnsDefaultSettings()
    {
        var uniqueName = "LoadTest_Corrupted_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);

        // Write invalid JSON to the settings file
        var settingsPath = GetSettingsPath(uniqueName);
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, "invalid json content {{");

        manager.Clear(false); // Reset cache
        var settings = manager.Load();

        // Should return defaults when JSON is corrupted
        Assert.NotNull(settings);
        Assert.Equal("Default", settings.Name);
    }

    [Fact]
    public void Load_WithLogger_LogsInfoWhenFileNotFound()
    {
        var loggerMock = new Mock<ILogger>();
        var uniqueName = "LoadTest_LogNoFile_" + Guid.NewGuid().ToString();
        var manager = new SettingsManager<TestSettings>(uniqueName, loggerMock.Object, _testDirectory);
        manager.Clear(true);

        manager.Load();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found") || v.ToString()!.Contains("default")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Save Tests

    [Fact]
    public void Save_ValidSettings_ReturnsTrue()
    {
        var manager = CreateManager("SaveTest_Valid");
        var settings = new TestSettings { Name = "Test", Value = 99 };

        var result = manager.Save(settings);

        Assert.True(result);
    }

    [Fact]
    public void Save_NullSettings_ReturnsFalse()
    {
        var manager = CreateManager("SaveTest_Null");

        var result = manager.Save(null!);

        Assert.False(result);
    }

    [Fact]
    public void Save_CreatesSettingsFile()
    {
        var uniqueName = "SaveTest_CreateFile_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);
        var settings = new TestSettings { Name = "FileTest" };

        manager.Save(settings);

        var settingsPath = GetSettingsPath(uniqueName);
        Assert.True(File.Exists(settingsPath));
    }

    [Fact]
    public void Save_UpdatesCachedSettings()
    {
        var manager = CreateManager("SaveTest_Cache");
        var settings = new TestSettings { Name = "Updated" };

        manager.Save(settings);
        var loaded = manager.Load();

        Assert.Same(settings, loaded);
    }

    [Fact]
    public void Save_PersistedJsonCanBeLoaded()
    {
        var uniqueName = "SaveTest_Persist_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);
        var settings = new TestSettings
        {
            Name = "PersistTest",
            Value = 123,
            Enabled = false
        };

        manager.Save(settings);
        manager.Clear(false); // Reset cache

        var loaded = manager.Load();

        Assert.Equal(settings.Name, loaded.Name);
        Assert.Equal(settings.Value, loaded.Value);
        Assert.Equal(settings.Enabled, loaded.Enabled);
    }

    [Fact]
    public void Save_RaisesSettingsChangedEvent()
    {
        var manager = CreateManager("SaveTest_Event");
        var settings = new TestSettings { Name = "EventTest" };
        var eventRaised = false;
        TestSettings? eventSettings = null;

        manager.SettingsChanged += (sender, s) =>
        {
            eventRaised = true;
            eventSettings = s;
        };

        manager.Save(settings);

        Assert.True(eventRaised);
        Assert.Same(settings, eventSettings);
    }

    [Fact]
    public void Save_NoSubscriber_DoesNotThrow()
    {
        var manager = CreateManager("SaveTest_NoSub");
        var settings = new TestSettings { Name = "NoSubscriber" };

        // No event subscriber
        var result = manager.Save(settings);

        Assert.True(result);
    }

    [Fact]
    public void Save_WithLogger_LogsErrorOnNull()
    {
        var loggerMock = new Mock<ILogger>();
        var manager = new SettingsManager<TestSettings>("SaveTest_LogNull", loggerMock.Object, _testDirectory);

        manager.Save(null!);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("null")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Save_FileIsIndentedJson()
    {
        var uniqueName = "SaveTest_Indent_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);
        var settings = new TestSettings { Name = "Indented" };

        manager.Save(settings);

        var settingsPath = GetSettingsPath(uniqueName);
        var json = File.ReadAllText(settingsPath);

        // Indented JSON should contain newlines
        Assert.Contains("\n", json);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ValidAction_ReturnsTrue()
    {
        var manager = CreateManager("UpdateTest_Valid");

        var result = manager.Update(s => s.Value = 77);

        Assert.True(result);
    }

    [Fact]
    public void Update_NullAction_ReturnsFalse()
    {
        var manager = CreateManager("UpdateTest_Null");

        var result = manager.Update(null!);

        Assert.False(result);
    }

    [Fact]
    public void Update_ModifiesSettings()
    {
        var manager = CreateManager("UpdateTest_Modify");

        manager.Update(s =>
        {
            s.Name = "Modified";
            s.Value = 88;
            s.Enabled = false;
        });

        var settings = manager.Load();

        Assert.Equal("Modified", settings.Name);
        Assert.Equal(88, settings.Value);
        Assert.False(settings.Enabled);
    }

    [Fact]
    public void Update_SavesAfterModification()
    {
        var uniqueName = "UpdateTest_Save_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);

        manager.Update(s => s.Name = "SavedAfterUpdate");
        manager.Clear(false); // Reset cache

        var loaded = manager.Load();

        Assert.Equal("SavedAfterUpdate", loaded.Name);
    }

    [Fact]
    public void Update_WithLogger_LogsErrorOnNullAction()
    {
        var loggerMock = new Mock<ILogger>();
        var manager = new SettingsManager<TestSettings>("UpdateTest_LogNull", loggerMock.Object, _testDirectory);

        manager.Update(null!);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("null")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Update_ActionThrowsException_ReturnsFalse()
    {
        var manager = CreateManager("UpdateTest_Exception");

        var result = manager.Update(s => throw new InvalidOperationException("Test exception"));

        Assert.False(result);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ResetsCache()
    {
        var manager = CreateManager("ClearTest_Cache");

        var settings1 = manager.Load();
        manager.Clear(false);
        var settings2 = manager.Load();

        // After clearing cache, new instance should be created
        Assert.NotSame(settings1, settings2);
    }

    [Fact]
    public void Clear_WithoutDeleteFile_FileRemains()
    {
        var uniqueName = "ClearTest_NoDelete_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);

        manager.Save(new TestSettings { Name = "Persisted" });
        manager.Clear(false);

        var settingsPath = GetSettingsPath(uniqueName);
        Assert.True(File.Exists(settingsPath));
    }

    [Fact]
    public void Clear_WithDeleteFile_FileRemoved()
    {
        var uniqueName = "ClearTest_Delete_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);

        manager.Save(new TestSettings { Name = "ToDelete" });
        manager.Clear(true);

        var settingsPath = GetSettingsPath(uniqueName);
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public void Clear_WithDeleteFile_NoFile_DoesNotThrow()
    {
        var uniqueName = "ClearTest_NoFile_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);

        // Ensure no file
        manager.Clear(true);

        // Clear again with deleteFile=true should not throw
        manager.Clear(true);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async System.Threading.Tasks.Task Load_ConcurrentCalls_ReturnsSameCachedInstance()
    {
        var uniqueName = "ThreadTest_Concurrent_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);

        var tasks = new System.Threading.Tasks.Task<TestSettings>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() => manager.Load());
        }

        await System.Threading.Tasks.Task.WhenAll(tasks);

        // All results should be the same cached instance
        var firstResult = await tasks[0];
        for (int i = 1; i < 10; i++)
        {
            var result = await tasks[i];
            Assert.Same(firstResult, result);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task Save_ConcurrentCalls_AllSucceed()
    {
        var uniqueName = "ThreadTest_Save_" + Guid.NewGuid().ToString();
        var manager = CreateManager(uniqueName);
        var results = new bool[10];

        var tasks = new System.Threading.Tasks.Task[10];
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                results[index] = manager.Save(new TestSettings { Value = index });
            });
        }

        await System.Threading.Tasks.Task.WhenAll(tasks);

        // All saves should succeed
        for (int i = 0; i < 10; i++)
        {
            Assert.True(results[i]);
        }
    }

    #endregion
}
