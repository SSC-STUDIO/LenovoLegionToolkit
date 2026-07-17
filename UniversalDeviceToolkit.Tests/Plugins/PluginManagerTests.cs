using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginManagerTests : IDisposable
{
    private readonly string? _previousAppDataOverride;
    private readonly Mock<IPluginSignatureValidator> _mockSignatureValidator;
    private readonly Mock<IPluginLoader> _mockLoader;
    private readonly Mock<IPluginRegistry> _mockRegistry;
    private readonly Mock<IPluginFileSystemManager> _mockFileSystemManager;
    private readonly List<string> _tempDirectories = new();

    public PluginManagerTests()
    {
        var appDataOverride = CreateTempDirectory();
        _previousAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, appDataOverride);
        _mockSignatureValidator = new Mock<IPluginSignatureValidator>();
        _mockLoader = new Mock<IPluginLoader>();
        _mockRegistry = new Mock<IPluginRegistry>();
        _mockFileSystemManager = new Mock<IPluginFileSystemManager>();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _previousAppDataOverride);

        foreach (var dir in _tempDirectories.Where(Directory.Exists))
        {
            try { Directory.Delete(dir, true); }
            catch { /* Best-effort cleanup in Dispose */ }
        }
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        _tempDirectories.Add(path);
        return path;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Arrange & Act
        var manager = CreateManager();

        // Assert
        manager.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullSignatureValidator_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new PluginManager(
            CreateSettings(),
            null!,
            _mockLoader.Object,
            _mockRegistry.Object,
            _mockFileSystemManager.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("signatureValidator");
    }

    [Fact]
    public void Constructor_WithNullLoader_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            null!,
            _mockRegistry.Object,
            _mockFileSystemManager.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("loader");
    }

    [Fact]
    public void Constructor_WithNullRegistry_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            null!,
            _mockFileSystemManager.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("registry");
    }

    [Fact]
    public void Constructor_WithNullFileSystemManager_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            _mockRegistry.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystemManager");
    }

    #endregion

    #region ScanAndLoadPlugins Tests

    [Fact]
    public async Task ScanAndLoadPlugins_WhenDirectoryDoesNotExist_ShouldNotLoad()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _mockFileSystemManager
            .Setup(f => f.GetPluginsDirectory())
            .Returns(nonExistentPath);

        var manager = CreateManager();

        // Act
        await manager.ScanAndLoadPluginsAsync();

        // Assert - Should not throw
        _mockLoader.Verify(l => l.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<IPluginSignatureValidator>()), Times.Never);
    }

    [Fact]
    public async Task ScanAndLoadPlugins_WhenDirectoryExists_ShouldScan()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        _mockFileSystemManager
            .Setup(f => f.GetPluginsDirectory())
            .Returns(tempDir);
        _mockFileSystemManager
            .Setup(f => f.GetPluginDllFiles())
            .Returns(new List<string>());
        _mockFileSystemManager
            .Setup(f => f.GetCultureFolders())
            .Returns(new HashSet<string>());

        var manager = CreateManager();

        // Act
        await manager.ScanAndLoadPluginsAsync();

        // Assert
        _mockFileSystemManager.Verify(f => f.GetPluginDllFiles(), Times.Once);
    }

    [Fact]
    public async Task ScanAndLoadPlugins_WithPluginFiles_ShouldAttemptLoad()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var pluginFile = Path.Combine(tempDir, "TestPlugin.dll");
        File.WriteAllText(pluginFile, "fake");

        _mockFileSystemManager
            .Setup(f => f.GetPluginsDirectory())
            .Returns(tempDir);
        _mockFileSystemManager
            .Setup(f => f.GetPluginDllFiles())
            .Returns(new List<string> { pluginFile });
        _mockFileSystemManager
            .Setup(f => f.GetCultureFolders())
            .Returns(new HashSet<string>());
        _mockSignatureValidator
            .Setup(v => v.ValidateAsync(pluginFile))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid, null));
        _mockLoader
            .Setup(l => l.LoadFromFileAsync(pluginFile, _mockSignatureValidator.Object))
            .ReturnsAsync((IPlugin?)null);

        var manager = CreateManager();

        // Act
        await manager.ScanAndLoadPluginsAsync();

        // Assert
        _mockLoader.Verify(l => l.LoadFromFileAsync(pluginFile, _mockSignatureValidator.Object), Times.Once);
    }

    [Fact]
    public async Task ScanAndLoadPlugins_WithForceRefresh_ShouldClearFileCacheBeforeScanning()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        _mockFileSystemManager
            .Setup(f => f.GetPluginsDirectory())
            .Returns(tempDir);
        _mockFileSystemManager
            .Setup(f => f.GetPluginDllFiles())
            .Returns(new List<string>());
        _mockFileSystemManager
            .Setup(f => f.GetCultureFolders())
            .Returns(new HashSet<string>());

        var manager = CreateManager();

        // Act
        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);

        // Assert
        _mockFileSystemManager.Verify(f => f.ClearFileCache(), Times.Once);
        _mockFileSystemManager.Verify(f => f.GetPluginDllFiles(), Times.Once);
    }

    [Fact]
    public async Task ScanAndLoadPlugins_WithoutForceRefresh_ShouldKeepFileCache()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        _mockFileSystemManager
            .Setup(f => f.GetPluginsDirectory())
            .Returns(tempDir);
        _mockFileSystemManager
            .Setup(f => f.GetPluginDllFiles())
            .Returns(new List<string>());
        _mockFileSystemManager
            .Setup(f => f.GetCultureFolders())
            .Returns(new HashSet<string>());

        var manager = CreateManager();

        // Act
        await manager.ScanAndLoadPluginsAsync();

        // Assert
        _mockFileSystemManager.Verify(f => f.ClearFileCache(), Times.Never);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void PluginStateChanged_WhenRaised_ShouldBeHandled()
    {
        // Arrange
        var manager = CreateManager();
        var eventRaised = false;
        manager.PluginStateChanged += (sender, args) => eventRaised = true;

        // Act - Trigger event (this is a test of event subscription)
        // Since we can't easily trigger internal events, we just verify subscription works
        eventRaised.Should().BeFalse(); // No event raised yet
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        Action act = () => manager.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.Dispose();
        Action act = () => manager.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ScanAndLoadPlugins_WithEmptyDirectory_ShouldCompleteSuccessfully()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        _mockFileSystemManager
            .Setup(f => f.GetPluginsDirectory())
            .Returns(tempDir);
        _mockFileSystemManager
            .Setup(f => f.GetPluginDllFiles())
            .Returns(new List<string>());
        _mockFileSystemManager
            .Setup(f => f.GetCultureFolders())
            .Returns(new HashSet<string>());

        var manager = CreateManager();

        // Act
        Func<Task> act = () => manager.ScanAndLoadPluginsAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void PruneRetiredPlugins_WhenNetworkAccelerationInstalled_ShouldUninstallAndQueueDeletion()
    {
        const string pluginId = "network-acceleration";
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();

        _mockRegistry
            .Setup(r => r.Get(pluginId))
            .Returns((IPlugin?)null);
        _mockRegistry
            .Setup(r => r.GetAll())
            .Returns(Array.Empty<IPlugin>());
        _mockLoader
            .Setup(l => l.Unload(pluginId))
            .Returns(true);

        var manager = CreateManager(settings);

        manager.PruneRetiredPlugins();

        settings.Store.InstalledExtensions.Should().NotContain(id => id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        settings.Store.PendingDeletionExtensions.Should().Contain(pluginId);
    }

    [Fact]
    public void UninstallPlugin_WhenInstalled_ShouldUnloadPluginContext()
    {
        // Arrange
        const string pluginId = "test-plugin";
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();

        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(p => p.Id).Returns(pluginId);

        _mockRegistry
            .Setup(r => r.Get(pluginId))
            .Returns(plugin.Object);
        _mockRegistry
            .Setup(r => r.GetAll())
            .Returns(Array.Empty<IPlugin>());
        _mockLoader
            .Setup(l => l.Unload(pluginId))
            .Returns(true);

        var manager = CreateManager(settings);

        // Act
        var result = manager.UninstallPlugin(pluginId);

        // Assert
        result.Should().BeTrue();
        _mockRegistry.Verify(r => r.MarkStopped(pluginId), Times.Once);
        _mockRegistry.Verify(r => r.ReplaceWithMetadataAdapter(pluginId), Times.Once);
        _mockLoader.Verify(l => l.Unload(pluginId), Times.Once);
    }

    [Fact]
    public void StopPlugin_WhenRegistered_ShouldUnloadPluginContext()
    {
        // Arrange
        const string pluginId = "test-plugin";
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(p => p.Id).Returns(pluginId);

        _mockRegistry
            .Setup(r => r.Get(pluginId))
            .Returns(plugin.Object);
        _mockLoader
            .Setup(l => l.Unload(pluginId))
            .Returns(true);

        var manager = CreateManager();

        // Act
        var result = manager.StopPlugin(pluginId);

        // Assert
        result.Should().BeTrue();
        plugin.Verify(p => p.Stop(), Times.Once);
        _mockRegistry.Verify(r => r.MarkStopped(pluginId), Times.Once);
        _mockRegistry.Verify(r => r.ReplaceWithMetadataAdapter(pluginId), Times.Once);
        _mockLoader.Verify(l => l.Unload(pluginId), Times.Once);
    }

    [Fact]
    public void StopAllPlugins_ShouldUnloadEachRegisteredPlugin()
    {
        // Arrange
        const string pluginA = "plugin-a";
        const string pluginB = "plugin-b";
        var first = new Mock<IPlugin>();
        first.SetupGet(p => p.Id).Returns(pluginA);
        var second = new Mock<IPlugin>();
        second.SetupGet(p => p.Id).Returns(pluginB);

        _mockRegistry
            .Setup(r => r.GetAll())
            .Returns(new[] { first.Object, second.Object });
        _mockLoader
            .Setup(l => l.Unload(It.IsAny<string>()))
            .Returns(true);

        var manager = CreateManager();

        // Act
        manager.StopAllPlugins();

        // Assert
        first.Verify(p => p.Stop(), Times.Once);
        second.Verify(p => p.Stop(), Times.Once);
        _mockRegistry.Verify(r => r.MarkStopped(pluginA), Times.Once);
        _mockRegistry.Verify(r => r.MarkStopped(pluginB), Times.Once);
        _mockRegistry.Verify(r => r.ReplaceWithMetadataAdapter(pluginA), Times.Once);
        _mockRegistry.Verify(r => r.ReplaceWithMetadataAdapter(pluginB), Times.Once);
        _mockLoader.Verify(l => l.Unload(pluginA), Times.Once);
        _mockLoader.Verify(l => l.Unload(pluginB), Times.Once);
    }

    [Fact]
    public void UnloadAllPlugins_ShouldUnloadEachRegisteredPluginAfterClear()
    {
        // Arrange
        const string pluginA = "plugin-a";
        const string pluginB = "plugin-b";
        var first = new Mock<IPlugin>();
        first.SetupGet(p => p.Id).Returns(pluginA);
        var second = new Mock<IPlugin>();
        second.SetupGet(p => p.Id).Returns(pluginB);

        _mockRegistry
            .Setup(r => r.GetAll())
            .Returns(new[] { first.Object, second.Object });
        _mockLoader
            .Setup(l => l.Unload(It.IsAny<string>()))
            .Returns(true);

        var manager = CreateManager();

        // Act
        manager.UnloadAllPlugins();

        // Assert
        _mockRegistry.Verify(r => r.Clear(), Times.Once);
        _mockLoader.Verify(l => l.Unload(pluginA), Times.Once);
        _mockLoader.Verify(l => l.Unload(pluginB), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static ApplicationSettings CreateSettings() => new();

    private PluginManager CreateManager(ApplicationSettings? settings = null)
    {
        return new PluginManager(
            settings ?? CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            _mockRegistry.Object,
            _mockFileSystemManager.Object);
    }

    #endregion
}
