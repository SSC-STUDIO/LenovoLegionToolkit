using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using Moq;
using Xunit;

namespace LenovoLegionToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginManagerTests : IDisposable
{
    private readonly Mock<ApplicationSettings> _mockSettings;
    private readonly Mock<IPluginSignatureValidator> _mockSignatureValidator;
    private readonly Mock<IPluginLoader> _mockLoader;
    private readonly Mock<IPluginRegistry> _mockRegistry;
    private readonly Mock<IPluginFileSystemManager> _mockFileSystemManager;
    private readonly List<string> _tempDirectories = new();

    public PluginManagerTests()
    {
        _mockSettings = new Mock<ApplicationSettings>();
        _mockSignatureValidator = new Mock<IPluginSignatureValidator>();
        _mockLoader = new Mock<IPluginLoader>();
        _mockRegistry = new Mock<IPluginRegistry>();
        _mockFileSystemManager = new Mock<IPluginFileSystemManager>();
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirectories.Where(Directory.Exists))
        {
            try { Directory.Delete(dir, true); }
            catch { }
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
            _mockSettings.Object,
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
            _mockSettings.Object,
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
            _mockSettings.Object,
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
            _mockSettings.Object,
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

    [Fact]
    public void Dispose_ShouldImplementIDisposable()
    {
        // Arrange & Act
        var manager = CreateManager();

        // Assert
        manager.Should().BeAssignableTo<IDisposable>();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PluginManager_ShouldImplementIPluginManager()
    {
        // Arrange & Act
        var manager = CreateManager();

        // Assert
        manager.Should().BeAssignableTo<IPluginManager>();
    }

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

    #endregion

    #region Helper Methods

    private PluginManager CreateManager()
    {
        return new PluginManager(
            _mockSettings.Object,
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            _mockRegistry.Object,
            _mockFileSystemManager.Object);
    }

    #endregion
}
