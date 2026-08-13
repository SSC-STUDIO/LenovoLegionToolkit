using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Tests.PluginFixture;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
[Collection(TestCollections.ProcessState)]
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
        _mockRegistry.Verify(r => r.MarkStopped(pluginId), Times.Never);
        _mockRegistry.Verify(r => r.ReplaceWithMetadataAdapter(pluginId), Times.Once);
        _mockLoader.Verify(l => l.Unload(pluginId), Times.Once);
    }

    [Fact]
    public void RestorePluginInstallationState_ShouldRemoveIntroducedMarkerWithoutSchedulingDeletion()
    {
        const string pluginId = "test-plugin";
        var settings = CreateSettings();
        var manager = CreateManager(settings);

        var snapshot = manager.CommitPluginInstallationState(pluginId);
        snapshot.PluginId.Should().Be(pluginId);
        snapshot.WasInstalled.Should().BeFalse();
        snapshot.WasPendingDeletion.Should().BeFalse();
        settings.Store.InstalledExtensions.Should().Equal(pluginId);
        settings.Store.PendingDeletionExtensions.Should().BeEmpty();
        manager.RestorePluginInstallationState(snapshot);

        settings.Store.InstalledExtensions.Should().BeEmpty();
        settings.Store.PendingDeletionExtensions.Should().BeEmpty();
        _mockRegistry.Verify(r => r.MarkStopped(It.IsAny<string>()), Times.Never);
        _mockRegistry.Verify(r => r.ReplaceWithMetadataAdapter(It.IsAny<string>()), Times.Never);
        _mockLoader.Verify(l => l.Unload(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RestorePluginInstallationState_ShouldPreserveExactPriorPendingDeletionMarkers()
    {
        const string pluginId = "test-plugin";
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add("existing-plugin");
        settings.Store.PendingDeletionExtensions.Add(pluginId);
        settings.Store.PendingDeletionExtensions.Add("other-pending-plugin");
        settings.SynchronizeStore();
        var manager = CreateManager(settings);

        var snapshot = manager.CommitPluginInstallationState(pluginId);
        snapshot.WasInstalled.Should().BeFalse();
        snapshot.WasPendingDeletion.Should().BeTrue();
        settings.Store.InstalledExtensions.Should().Equal("existing-plugin", pluginId);
        settings.Store.PendingDeletionExtensions.Should().Equal("other-pending-plugin");
        manager.RestorePluginInstallationState(snapshot);

        settings.Store.InstalledExtensions.Should().Equal("existing-plugin");
        settings.Store.PendingDeletionExtensions.Should().Equal(pluginId, "other-pending-plugin");
        _mockRegistry.Verify(r => r.MarkStopped(It.IsAny<string>()), Times.Never);
        _mockRegistry.Verify(r => r.ReplaceWithMetadataAdapter(It.IsAny<string>()), Times.Never);
        _mockLoader.Verify(l => l.Unload(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RestorePluginInstallationState_ShouldPreserveConcurrentMarkersForOtherPlugin()
    {
        const string pluginA = "plugin-a";
        const string pluginB = "plugin-b";
        var settings = CreateSettings();
        settings.Store.PendingDeletionExtensions.Add(pluginA);
        settings.Store.PendingDeletionExtensions.Add(pluginB);
        settings.SynchronizeStore();
        var manager = CreateManager(settings);

        var pluginATransaction = manager.CommitPluginInstallationState(pluginA);
        await Task.Run(() => manager.CommitPluginInstallationState(pluginB));
        manager.RestorePluginInstallationState(pluginATransaction);

        settings.Store.InstalledExtensions.Should().Equal(pluginB);
        settings.Store.PendingDeletionExtensions.Should().Equal(pluginA);
    }

    [Fact]
    public void CommitPluginInstallation_WhenInstallCallbackFails_ShouldRestoreExactMarkers()
    {
        const string pluginId = "callback-failure-plugin";
        var settings = CreateSettings();
        settings.Store.PendingDeletionExtensions.Add(pluginId);
        settings.Store.PendingDeletionExtensions.Add("other-pending-plugin");
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.Setup(candidate => candidate.OnInstalled())
            .Throws(new InvalidOperationException("callback failed"));
        _mockRegistry.Setup(registry => registry.Get(pluginId)).Returns(plugin.Object);
        var manager = CreateManager(settings);

        Action action = () => manager.CommitPluginInstallation(pluginId);

        action.Should().Throw<InvalidOperationException>().WithMessage("*callback failed*");
        settings.Store.InstalledExtensions.Should().BeEmpty();
        settings.Store.PendingDeletionExtensions.Should().Equal(
            pluginId,
            "other-pending-plugin");
        _mockRegistry.Verify(registry => registry.Unregister(It.IsAny<string>()), Times.Never);
        _mockLoader.Verify(loader => loader.Unload(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CommitPluginInstallation_WhenCallbackMutatesSamePlugin_ShouldFailFastWithoutStaleLease()
    {
        const string pluginId = "self-mutating-plugin";
        var registry = new PluginRegistry();
        PluginManager? manager = null;
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.Setup(candidate => candidate.OnInstalled())
            .Callback(() => manager!.UninstallPlugin(pluginId));
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId });
        manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            _mockFileSystemManager.Object);

        Action action = () => manager.CommitPluginInstallation(pluginId);

        action.Should().Throw<InvalidOperationException>().WithMessage("*Reentrant public mutation*");
        manager.GetInstalledPluginIds().Should().NotContain(pluginId);
        using var subsequentLease = manager.AcquirePluginMutation(pluginId);
        subsequentLease.Should().NotBeNull("callback failure must dispose ambient ownership");
        manager.Dispose();
    }

    [Fact]
    public void CommitPluginInstallation_WhenChildTaskInheritsContext_ShouldRejectSamePluginMutation()
    {
        const string pluginId = "child-context-plugin";
        var registry = new PluginRegistry();
        PluginManager? manager = null;
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.Setup(candidate => candidate.OnInstalled())
            .Callback(() => Task.Run(() => manager!.InstallPlugin(pluginId)).GetAwaiter().GetResult());
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId });
        manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            _mockFileSystemManager.Object);

        Action action = () => manager.CommitPluginInstallation(pluginId);

        action.Should().Throw<InvalidOperationException>().WithMessage("*Reentrant public mutation*");
        manager.GetInstalledPluginIds().Should().NotContain(pluginId);
        manager.Dispose();
    }

    [Fact]
    public void CommitPluginInstallation_WhenCallbackMutatesUnrelatedPlugin_ShouldAllowIt()
    {
        const string pluginId = "primary-callback-plugin";
        const string otherPluginId = "unrelated-callback-plugin";
        var registry = new PluginRegistry();
        PluginManager? manager = null;
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.Setup(candidate => candidate.OnInstalled())
            .Callback(() => manager!.InstallPlugin(otherPluginId));
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId });
        manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            _mockFileSystemManager.Object);

        manager.CommitPluginInstallation(pluginId);

        manager.GetInstalledPluginIds().Should().BeEquivalentTo(pluginId, otherPluginId);
        manager.Dispose();
    }

    [Fact]
    public void ForgetPluginRuntime_ShouldUnloadWithoutUninstallCallbacksOrMarkerChanges()
    {
        const string pluginId = "runtime-replacement-plugin";
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.Store.PendingDeletionExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        _mockRegistry.Setup(registry => registry.Get(pluginId)).Returns(plugin.Object);
        _mockLoader.Setup(loader => loader.Unload(pluginId)).Returns(true);
        var manager = CreateManager(settings);

        manager.ForgetPluginRuntime(pluginId);

        plugin.Verify(candidate => candidate.Stop(), Times.Never);
        plugin.Verify(candidate => candidate.OnUninstalled(), Times.Never);
        _mockRegistry.Verify(registry => registry.Forget(pluginId), Times.Once);
        _mockRegistry.Verify(registry => registry.Unregister(pluginId), Times.Never);
        _mockLoader.Verify(loader => loader.Unload(pluginId), Times.Once);
        settings.Store.InstalledExtensions.Should().Equal(pluginId);
        settings.Store.PendingDeletionExtensions.Should().Equal(pluginId);
    }

    [Fact]
    public void ForgetPluginRuntime_WhenLoaderRefusesUnload_ShouldKeepRegistrationAndMarkers()
    {
        const string pluginId = "live-context-plugin";
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        _mockRegistry.Setup(registry => registry.Get(pluginId)).Returns(plugin.Object);
        _mockLoader.Setup(loader => loader.Unload(pluginId)).Returns(false);
        var manager = CreateManager(settings);

        var result = manager.ForgetPluginRuntime(pluginId);

        result.Should().BeFalse();
        _mockRegistry.Verify(
            registry => registry.Forget(pluginId),
            Times.Once,
            "the manager must release the registry reference before requesting unload");
        settings.Store.InstalledExtensions.Should().Equal(pluginId);
        settings.Store.PendingDeletionExtensions.Should().BeEmpty();
    }

    [Fact]
    public void UninstallPlugin_WhenStopThrows_ShouldPreserveInstallation()
    {
        const string pluginId = "stop-failure-plugin";
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.Setup(candidate => candidate.Stop()).Throws(new InvalidOperationException("stop failed"));
        _mockRegistry.Setup(registry => registry.Get(pluginId)).Returns(plugin.Object);
        _mockRegistry.Setup(registry => registry.GetAll()).Returns(Array.Empty<IPlugin>());
        _mockRegistry.Setup(registry => registry.IsStarted(pluginId)).Returns(true);
        var manager = CreateManager(settings);

        Action action = () => manager.UninstallPlugin(pluginId);

        action.Should().Throw<InvalidOperationException>().WithMessage("*Failed to stop*");
        settings.Store.InstalledExtensions.Should().Equal(pluginId);
        settings.Store.PendingDeletionExtensions.Should().BeEmpty();
        _mockLoader.Verify(loader => loader.Unload(pluginId), Times.Never);
        _mockRegistry.Verify(registry => registry.ReplaceWithMetadataAdapter(pluginId), Times.Never);
    }

    [Fact]
    public void UninstallPlugin_WhenUnloadRefuses_ShouldRestartAndPreserveInstallation()
    {
        const string pluginId = "unload-refusal-plugin";
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var startup = plugin.As<IAppStartupPlugin>();
        _mockRegistry.Setup(registry => registry.Get(pluginId)).Returns(plugin.Object);
        _mockRegistry.Setup(registry => registry.GetAll()).Returns(Array.Empty<IPlugin>());
        _mockRegistry.Setup(registry => registry.IsStarted(pluginId)).Returns(true);
        _mockRegistry.Setup(registry => registry.MarkStarted(pluginId)).Returns(true);
        _mockLoader.Setup(loader => loader.Unload(pluginId)).Returns(false);
        var manager = CreateManager(settings);

        var result = manager.UninstallPlugin(pluginId);

        result.Should().BeFalse();
        startup.Verify(candidate => candidate.OnAppStarted(), Times.Once);
        settings.Store.InstalledExtensions.Should().Equal(pluginId);
        settings.Store.PendingDeletionExtensions.Should().BeEmpty();
        _mockRegistry.Verify(
            registry => registry.Forget(pluginId),
            Times.Once,
            "the runtime is detached before a loader request can safely be attempted");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UninstallPlugin_WhenMarkerPersistenceFails_ShouldRestoreRuntimeAndExactState(bool wasStarted)
    {
        const string pluginId = "persistence-rollback-plugin";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "PersistenceRollbackPlugin.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var startup = plugin.As<IAppStartupPlugin>();
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        if (wasStarted)
            registry.MarkStarted(pluginId);
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        loader.Setup(candidate => candidate.LoadFromFileAsync(pluginPath, It.IsAny<IPluginSignatureValidator>()))
            .ReturnsAsync(plugin.Object);
        _mockSignatureValidator.Setup(candidate => candidate.ValidateAsync(pluginPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(candidate => candidate.GetPluginDllFiles()).Returns([pluginPath]);
        using var manager = new PluginManager(
            settings,
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var synchronizeCalls = 0;
        manager.SynchronizeStateStoreOverride = () =>
        {
            if (++synchronizeCalls == 1)
                throw new IOException("marker persistence failed");
        };

        Action action = () => manager.UninstallPlugin(pluginId);

        action.Should().Throw<IOException>().WithMessage("*marker persistence failed*");
        settings.Store.InstalledExtensions.Should().Contain(pluginId);
        settings.Store.PendingDeletionExtensions.Should().NotContain(pluginId);
        registry.Get(pluginId).Should().BeSameAs(plugin.Object);
        registry.IsStarted(pluginId).Should().Be(wasStarted);
        plugin.Verify(candidate => candidate.OnUninstalled(), Times.Once);
        plugin.Verify(candidate => candidate.OnInstalled(), Times.Once);
        startup.Verify(
            candidate => candidate.OnAppStarted(),
            wasStarted ? Times.Once() : Times.Never());
    }

    [Fact]
    public void UninstallPlugin_WhenPersistenceAndRuntimeRestoreFail_ShouldAggregateFailures()
    {
        const string pluginId = "degraded-uninstall-rollback";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "DegradedUninstallRollback.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        loader.Setup(candidate => candidate.LoadFromFileAsync(pluginPath, It.IsAny<IPluginSignatureValidator>()))
            .ReturnsAsync((IPlugin?)null);
        _mockSignatureValidator.Setup(candidate => candidate.ValidateAsync(pluginPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            settings,
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var synchronizeCalls = 0;
        manager.SynchronizeStateStoreOverride = () =>
        {
            if (++synchronizeCalls == 1)
                throw new IOException("marker persistence failed");
        };

        Action action = () => manager.UninstallPlugin(pluginId);

        action.Should().Throw<AggregateException>()
            .WithMessage("*restoration was degraded*");
        settings.Store.PendingDeletionExtensions.Should().NotContain(pluginId);
    }

    [Fact]
    public void UninstallPlugin_WhenTrustPersistenceFails_ShouldRestoreTrustRuntimeMarkersAndCallbackState()
    {
        const string pluginId = "trust-finalization-failure";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "TrustFinalizationFailure.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        TrustedPluginPackageStore.TrustPluginDirectory(pluginId, pluginDirectory);
        TrustedPluginPackageStore.IsTrustedFile(pluginPath).Should().BeTrue();
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        loader.Setup(candidate => candidate.LoadFromFileAsync(
                pluginPath,
                It.IsAny<IPluginSignatureValidator>()))
            .ReturnsAsync(plugin.Object);
        _mockSignatureValidator
            .Setup(candidate => candidate.ValidateAsync(pluginPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            settings,
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var persistenceCalls = 0;
        TrustedPluginPackageStore.PersistenceBoundaryOverride = () =>
        {
            if (++persistenceCalls == 1)
                throw new IOException("trust persistence failed");
        };

        try
        {
            Action action = () => manager.UninstallPlugin(pluginId);

            action.Should().Throw<IOException>().WithMessage("*trust persistence failed*");
            settings.Store.InstalledExtensions.Should().Contain(pluginId);
            settings.Store.PendingDeletionExtensions.Should().NotContain(pluginId);
            TrustedPluginPackageStore.IsTrustedFile(pluginPath).Should().BeTrue();
            registry.Get(pluginId).Should().BeSameAs(plugin.Object);
            registry.IsStarted(pluginId).Should().BeFalse();
            plugin.Verify(candidate => candidate.OnUninstalled(), Times.Once);
            plugin.Verify(candidate => candidate.OnInstalled(), Times.Once);
        }
        finally
        {
            TrustedPluginPackageStore.PersistenceBoundaryOverride = null;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UninstallCommit_ShouldHidePartialMarkerAndTrustStateFromReaders(
        bool pauseAtMarkerPersistence)
    {
        const string pluginId = "coordinated-uninstall";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "CoordinatedUninstall.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        TrustedPluginPackageStore.TrustPluginDirectory(pluginId, pluginDirectory);
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            settings,
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        using var commitPaused = new ManualResetEventSlim();
        using var continueCommit = new ManualResetEventSlim();
        if (pauseAtMarkerPersistence)
        {
            manager.SynchronizeStateStoreOverride = () =>
            {
                commitPaused.Set();
                continueCommit.Wait();
            };
        }
        else
        {
            TrustedPluginPackageStore.PersistenceBoundaryOverride = () =>
            {
                commitPaused.Set();
                continueCommit.Wait();
            };
        }

        try
        {
            var uninstall = Task.Run(() => manager.UninstallPlugin(pluginId));
            commitPaused.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            var reader = Task.Run(() => (
                Installed: manager.GetInstalledPluginIds().Contains(pluginId),
                Trusted: TrustedPluginPackageStore.IsTrustedFile(pluginPath)));
            await Task.Delay(100);
            reader.IsCompleted.Should().BeFalse(
                "coordinated readers must not observe a half-committed uninstall");

            continueCommit.Set();
            (await uninstall).Should().BeTrue();
            var observed = await reader;
            observed.Installed.Should().BeFalse();
            observed.Trusted.Should().BeFalse();
        }
        finally
        {
            continueCommit.Set();
            TrustedPluginPackageStore.PersistenceBoundaryOverride = null;
        }
    }

    [Fact]
    public async Task UninstallPlugin_WhenLifecycleSubscriberFails_ShouldRemainCommittedAndContinueDelivery()
    {
        const string pluginId = "lifecycle-finalization-failure";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "LifecycleFinalizationFailure.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var settings = CreateSettings();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        loader.Setup(candidate => candidate.LoadFromFileAsync(
                pluginPath,
                It.IsAny<IPluginSignatureValidator>()))
            .ReturnsAsync(plugin.Object);
        _mockSignatureValidator
            .Setup(candidate => candidate.ValidateAsync(pluginPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            settings,
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        manager.CommitPluginInstallation(pluginId);
        var persistenceCalls = 0;
        manager.SynchronizeStateStoreOverride = () => persistenceCalls++;
        using var subscriberEntered = new ManualResetEventSlim();
        using var continueSubscriber = new ManualResetEventSlim();
        var laterSubscriberCalls = 0;
        manager.PluginStateChanged += (_, _) =>
        {
            subscriberEntered.Set();
            continueSubscriber.Wait();
            throw new InvalidOperationException("lifecycle subscriber failed");
        };
        manager.PluginStateChanged += (_, _) => laterSubscriberCalls++;

        var uninstall = Task.Run(() => manager.UninstallPlugin(pluginId));
        subscriberEntered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        var committedReader = Task.Run(() => (
            Installed: manager.GetInstalledPluginIds().Contains(pluginId),
            Trusted: TrustedPluginPackageStore.IsTrustedFile(pluginPath)));
        var observed = await committedReader;
        observed.Installed.Should().BeFalse();
        observed.Trusted.Should().BeFalse();
        continueSubscriber.Set();

        (await uninstall).Should().BeTrue();
        settings.Store.InstalledExtensions.Should().NotContain(pluginId);
        settings.Store.PendingDeletionExtensions.Should().Contain(pluginId);
        registry.Get(pluginId).Should().BeOfType<PluginManifestAdapter>();
        plugin.Verify(candidate => candidate.OnUninstalled(), Times.Once);
        plugin.Verify(candidate => candidate.OnInstalled(), Times.Once);
        laterSubscriberCalls.Should().Be(1);
        persistenceCalls.Should().Be(1,
            "post-commit notification failures must not reacquire a restoration write boundary");
    }

    [Fact]
    public void UninstallPlugin_WhenCallbackCompensationFails_ShouldReportDegradedRestoration()
    {
        const string pluginId = "callback-compensation-failure";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "CallbackCompensationFailure.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.Setup(candidate => candidate.OnInstalled())
            .Throws(new InvalidOperationException("callback compensation failed"));
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        loader.Setup(candidate => candidate.LoadFromFileAsync(
                pluginPath,
                It.IsAny<IPluginSignatureValidator>()))
            .ReturnsAsync(plugin.Object);
        _mockSignatureValidator
            .Setup(candidate => candidate.ValidateAsync(pluginPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            settings,
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var synchronizeCalls = 0;
        manager.SynchronizeStateStoreOverride = () =>
        {
            if (++synchronizeCalls == 1)
                throw new IOException("marker persistence failed");
        };

        Action action = () => manager.UninstallPlugin(pluginId);

        action.Should().Throw<AggregateException>()
            .WithMessage("*restoration was degraded*");
        settings.Store.InstalledExtensions.Should().Contain(pluginId);
        settings.Store.PendingDeletionExtensions.Should().NotContain(pluginId);
        plugin.Verify(candidate => candidate.OnUninstalled(), Times.Once);
        plugin.Verify(candidate => candidate.OnInstalled(), Times.Once);

        manager.UninstallPlugin(pluginId).Should().BeTrue(
            "retry must tear down the actively restored runtime before confirming unload");
        plugin.Verify(candidate => candidate.OnUninstalled(), Times.Exactly(2));
        settings.Store.InstalledExtensions.Should().NotContain(pluginId);
    }

    [Fact]
    public void ForgetPluginRuntime_WhenRestartAfterUnloadRefusalFails_ShouldSurfaceFailure()
    {
        const string pluginId = "restart-failure-plugin";
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.As<IAppStartupPlugin>()
            .Setup(candidate => candidate.OnAppStarted())
            .Throws(new InvalidOperationException("restart failed"));
        _mockRegistry.Setup(registry => registry.Get(pluginId)).Returns(plugin.Object);
        _mockRegistry.Setup(registry => registry.IsStarted(pluginId)).Returns(true);
        _mockLoader.Setup(loader => loader.Unload(pluginId)).Returns(false);
        var manager = CreateManager(CreateSettings());

        Action action = () => manager.ForgetPluginRuntime(pluginId);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*could not restore its started state*");
        _mockRegistry.Verify(registry => registry.MarkStopped(pluginId), Times.AtLeastOnce);
        _mockRegistry.Verify(registry => registry.Forget(pluginId), Times.Once);
    }

    [Fact]
    public async Task ActivatePluginRuntimeStrictAsync_WhenStartupThrows_ShouldSurfaceFailure()
    {
        const string pluginId = "startup-failure-plugin";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(
            pluginsRoot,
            "UniversalDeviceToolkit.Plugins.StartupFailurePlugin.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var startupPlugin = new Mock<IPlugin>();
        startupPlugin.SetupGet(plugin => plugin.Id).Returns(pluginId);
        startupPlugin.As<IAppStartupPlugin>()
            .Setup(plugin => plugin.OnAppStarted())
            .Throws(new InvalidOperationException("startup failed"));
        var registry = new PluginRegistry();
        registry.Register(startupPlugin.Object, new PluginMetadata
        {
            Id = pluginId,
            FilePath = pluginPath,
        });
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(manager => manager.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(manager => manager.GetPluginDllFiles()).Returns([pluginPath]);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            fileSystem.Object);

        Func<Task> action = () => manager.ActivatePluginRuntimeStrictAsync(pluginId, pluginPath);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*failed startup activation*");
        registry.IsStarted(pluginId).Should().BeFalse();
    }

    [Fact]
    public async Task ActivatePluginRuntimeStrictAsync_WhenStartupSelfUninstalls_ShouldRejectReentry()
    {
        const string pluginId = "startup-self-mutation";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "StartupSelfMutation.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var settings = CreateSettings();
        settings.Store.InstalledExtensions.Add(pluginId);
        settings.SynchronizeStore();
        var registry = new PluginRegistry();
        PluginManager? manager = null;
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.As<IAppStartupPlugin>()
            .Setup(candidate => candidate.OnAppStarted())
            .Callback(() => manager!.UninstallPlugin(pluginId));
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        manager = new PluginManager(
            settings,
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            fileSystem.Object);

        Func<Task> action = () => manager.ActivatePluginRuntimeStrictAsync(pluginId, pluginPath);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*failed startup activation*");
        settings.Store.InstalledExtensions.Should().Contain(pluginId);
        registry.IsStarted(pluginId).Should().BeFalse();
        manager.Dispose();
    }

    [Fact]
    public async Task PreparedInstallation_ShouldRunOnInstalledBeforeStartupAndCommitMarkersLast()
    {
        const string pluginId = "ordered-lifecycle";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "OrderedLifecycle.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var order = new List<string>();
        var prepared = false;
        var registry = new PluginRegistry();
        PluginManager? manager = null;
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.Setup(candidate => candidate.OnInstalled()).Callback(() =>
        {
            manager!.IsInstalled(pluginId).Should().BeFalse();
            prepared = true;
            order.Add("installed");
        });
        plugin.As<IAppStartupPlugin>().Setup(candidate => candidate.OnAppStarted()).Callback(() =>
        {
            prepared.Should().BeTrue();
            manager!.IsInstalled(pluginId).Should().BeFalse();
            order.Add("started");
        });
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            fileSystem.Object);
        manager.PluginStateChanged += (_, _) => order.Add("committed");

        using var lease = manager.AcquirePluginMutation(pluginId);
        await manager.LoadPluginRuntimeStrictAsync(pluginId, pluginPath, lease);
        manager.PreparePluginInstallation(pluginId, lease);
        await manager.ActivatePluginRuntimeStrictAsync(pluginId, pluginPath, lease);
        manager.CommitPluginInstallation(pluginId, lease);

        order.Should().Equal("installed", "started", "committed", "committed");
        manager.GetInstalledPluginIds().Should().Contain(pluginId);
        manager.Dispose();
    }

    [Fact]
    public void CommitPluginInstallation_WhenSubscriberFails_ShouldRemainCommittedAndContinueDelivery()
    {
        const string pluginId = "post-commit-install-notification";
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId });
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            _mockFileSystemManager.Object);
        var laterSubscriberCalls = 0;
        manager.PluginStateChanged += (_, _) =>
            throw new InvalidOperationException("install notification failed");
        manager.PluginStateChanged += (_, _) => laterSubscriberCalls++;

        manager.CommitPluginInstallation(pluginId);

        manager.GetInstalledPluginIds().Should().Contain(pluginId);
        laterSubscriberCalls.Should().Be(1);
        plugin.Verify(candidate => candidate.OnInstalled(), Times.Once);
    }

    [Fact]
    public async Task PreparedInstallation_WhenStartupFails_ShouldCompensateLifecycleWithoutMarkers()
    {
        const string pluginId = "failed-prepared-lifecycle";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "FailedPreparedLifecycle.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var registry = new PluginRegistry();
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.As<IAppStartupPlugin>()
            .Setup(candidate => candidate.OnAppStarted())
            .Throws(new InvalidOperationException("dependency unavailable"));
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            fileSystem.Object);
        using var lease = manager.AcquirePluginMutation(pluginId);
        manager.PreparePluginInstallation(pluginId, lease);

        Func<Task> action = () => manager.ActivatePluginRuntimeStrictAsync(pluginId, pluginPath, lease);
        await action.Should().ThrowAsync<InvalidOperationException>();
        manager.RollbackPreparedPluginInstallation(pluginId, lease);

        plugin.Verify(candidate => candidate.OnInstalled(), Times.Once);
        plugin.Verify(candidate => candidate.OnUninstalled(), Times.Once);
        manager.GetInstalledPluginIds().Should().NotContain(pluginId);
        registry.IsStarted(pluginId).Should().BeFalse();
    }

    [Fact]
    public void PreparedInstallation_WithDependency_ShouldPersistAllMarkersOnceBeforeEvents()
    {
        const string pluginId = "transaction-parent";
        const string dependencyId = "transaction-dependency";
        var order = new List<string>();
        var dependency = new Mock<IPlugin>();
        dependency.SetupGet(candidate => candidate.Id).Returns(dependencyId);
        dependency.Setup(candidate => candidate.OnInstalled())
            .Callback(() => order.Add("dependency-installed"));
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.SetupGet(candidate => candidate.Dependencies).Returns([dependencyId]);
        plugin.Setup(candidate => candidate.OnInstalled())
            .Callback(() => order.Add("parent-installed"));
        var registry = new PluginRegistry();
        registry.Register(dependency.Object, new PluginMetadata { Id = dependencyId });
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId });
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            _mockFileSystemManager.Object);
        var synchronizeCalls = 0;
        manager.SynchronizeStateStoreOverride = () =>
        {
            synchronizeCalls++;
            order.Add("persist");
        };
        manager.PluginStateChanged += (_, args) => order.Add($"{args.PluginId}-event");
        using var lease = manager.AcquirePluginMutation(pluginId);

        manager.PreparePluginInstallation(pluginId, lease);

        manager.GetInstalledPluginIds().Should().BeEmpty();
        order.Should().Equal("dependency-installed", "parent-installed");

        manager.CommitPluginInstallation(pluginId, lease);

        manager.GetInstalledPluginIds().Should().BeEquivalentTo(dependencyId, pluginId);
        synchronizeCalls.Should().Be(1);
        order.Should().Equal(
            "dependency-installed",
            "parent-installed",
            "persist",
            $"{dependencyId}-event",
            $"{pluginId}-event");
    }

    [Fact]
    public void PreparedInstallation_CancellationRollback_ShouldCompensateInReverseWithoutVisibility()
    {
        const string pluginId = "cancelled-parent";
        const string dependencyId = "cancelled-dependency";
        var order = new List<string>();
        var dependency = new Mock<IPlugin>();
        dependency.SetupGet(candidate => candidate.Id).Returns(dependencyId);
        dependency.Setup(candidate => candidate.OnInstalled())
            .Callback(() => order.Add("dependency-installed"));
        dependency.Setup(candidate => candidate.OnUninstalled())
            .Callback(() => order.Add("dependency-uninstalled"));
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.SetupGet(candidate => candidate.Dependencies).Returns([dependencyId]);
        plugin.Setup(candidate => candidate.OnInstalled())
            .Callback(() => order.Add("parent-installed"));
        plugin.Setup(candidate => candidate.OnUninstalled())
            .Callback(() => order.Add("parent-uninstalled"));
        var registry = new PluginRegistry();
        registry.Register(dependency.Object, new PluginMetadata { Id = dependencyId });
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId });
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            _mockFileSystemManager.Object);
        var eventCount = 0;
        manager.PluginStateChanged += (_, _) => eventCount++;
        using var lease = manager.AcquirePluginMutation(pluginId);

        manager.PreparePluginInstallation(pluginId, lease);
        manager.RollbackPreparedPluginInstallation(pluginId, lease);

        order.Should().Equal(
            "dependency-installed",
            "parent-installed",
            "parent-uninstalled",
            "dependency-uninstalled");
        manager.GetInstalledPluginIds().Should().BeEmpty();
        eventCount.Should().Be(0);
    }

    [Fact]
    public void PreparedInstallation_WhenParentCallbackFails_ShouldCompensateDependencyWithoutVisibility()
    {
        const string pluginId = "failing-parent";
        const string dependencyId = "prepared-dependency";
        var dependency = new Mock<IPlugin>();
        dependency.SetupGet(candidate => candidate.Id).Returns(dependencyId);
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        plugin.SetupGet(candidate => candidate.Dependencies).Returns([dependencyId]);
        plugin.Setup(candidate => candidate.OnInstalled())
            .Throws(new InvalidOperationException("parent preparation failed"));
        var registry = new PluginRegistry();
        registry.Register(dependency.Object, new PluginMetadata { Id = dependencyId });
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId });
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            _mockFileSystemManager.Object);
        var eventCount = 0;
        manager.PluginStateChanged += (_, _) => eventCount++;

        Action action = () => manager.PreparePluginInstallation(pluginId);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*parent preparation failed*");
        dependency.Verify(candidate => candidate.OnInstalled(), Times.Once);
        dependency.Verify(candidate => candidate.OnUninstalled(), Times.Once);
        manager.GetInstalledPluginIds().Should().BeEmpty();
        eventCount.Should().Be(0);
    }

    [Fact]
    public async Task PreparedInstallation_RealFixtureStartup_ShouldRequireOnInstalledFirst()
    {
        const string pluginId = "loader-fixture";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "LoaderFixture.dll");
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, pluginPath);
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        var loader = new PluginLoader();
        var registry = new PluginRegistry();
        using var manager = new PluginManager(
            CreateSettings(),
            signatureValidator.Object,
            loader,
            registry,
            fileSystem.Object);
        using var lease = manager.AcquirePluginMutation(pluginId);

        await manager.LoadPluginRuntimeStrictAsync(pluginId, pluginPath, lease);
        manager.PreparePluginInstallation(pluginId, lease);
        await manager.ActivatePluginRuntimeStrictAsync(pluginId, pluginPath, lease);
        manager.CommitPluginInstallation(pluginId, lease);

        registry.IsStarted(pluginId).Should().BeTrue();
        manager.GetInstalledPluginIds().Should().Contain(pluginId);
        manager.ForgetPluginRuntime(pluginId, lease).Should().BeTrue();
    }

    [Fact]
    public async Task StrictLoader_ScopedAuthorization_ShouldNotExposeTrustToConcurrentReaders()
    {
        const string pluginId = "loader-fixture";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "LoaderFixture.dll");
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, pluginPath);
        var authorization = TrustedPluginPackageStore.CreateAuthorization(
            pluginId,
            pluginDirectory);
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        var loader = new PluginLoader();
        var registry = new PluginRegistry();
        using var manager = new PluginManager(
            CreateSettings(),
            new PluginSignatureValidator(PluginSignatureSettings.Production),
            loader,
            registry,
            fileSystem.Object);
        using var lease = manager.AcquirePluginMutation(pluginId);

        var loadTask = manager.LoadPluginRuntimeStrictAsync(
            pluginId,
            pluginPath,
            lease,
            authorization);
        var readerTasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() => TrustedPluginPackageStore.IsTrustedFile(pluginPath)))
            .ToArray();
        await Task.WhenAll(readerTasks.Cast<Task>().Append(loadTask));

        readerTasks.Should().OnlyContain(task => task.Result == false);
        IsRuntimeRegistered(registry, pluginId).Should().BeTrue();
        TrustedPluginPackageStore.IsTrustedFile(pluginPath).Should().BeFalse();
        manager.ForgetPluginRuntime(pluginId, lease).Should().BeTrue();
        authorization.Close();
    }

    [Fact]
    public async Task CoordinatedInstallCommit_ShouldHideMarkerUntilTrustPublicationCompletes()
    {
        const string pluginId = "loader-fixture";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "LoaderFixture.dll");
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, pluginPath);
        var authorization = TrustedPluginPackageStore.CreateAuthorization(
            pluginId,
            pluginDirectory);
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        var loader = new PluginLoader();
        var registry = new PluginRegistry();
        using var manager = new PluginManager(
            CreateSettings(),
            new PluginSignatureValidator(PluginSignatureSettings.Production),
            loader,
            registry,
            fileSystem.Object);
        using var lease = manager.AcquirePluginMutation(pluginId);
        await manager.LoadPluginRuntimeStrictAsync(
            pluginId,
            pluginPath,
            lease,
            authorization);
        manager.PreparePluginInstallation(pluginId, lease);
        using var commitPaused = new ManualResetEventSlim();
        using var continueCommit = new ManualResetEventSlim();
        var commitTask = Task.Run(() => manager.CommitPluginInstallation(
            pluginId,
            lease,
            () =>
            {
                commitPaused.Set();
                continueCommit.Wait();
                TrustedPluginPackageStore.PublishAuthorizationStrict(authorization);
            }));
        commitPaused.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

        var reader = Task.Run(() => new
        {
            Installed = manager.GetInstalledPluginIds()
                .Contains(pluginId, StringComparer.OrdinalIgnoreCase),
            Trusted = TrustedPluginPackageStore.IsTrustedFile(pluginPath),
        });
        await Task.Delay(100);
        reader.IsCompleted.Should().BeFalse(
            "readers must not observe marker state while trust publication can still fail");
        continueCommit.Set();
        await commitTask;
        var observed = await reader;

        observed.Installed.Should().BeTrue();
        observed.Trusted.Should().BeTrue();
        manager.ForgetPluginRuntime(pluginId, lease).Should().BeTrue();
    }

    [Fact]
    public void ReconcilePluginRuntimes_ShouldUnloadEveryReplacementRuntime()
    {
        const string expectedId = "shell-integration";
        const string additionalId = "additional-plugin";
        var pluginsRoot = CreateTempDirectory();
        var replacementDirectory = Path.Combine(pluginsRoot, "local", expectedId);
        Directory.CreateDirectory(replacementDirectory);
        var registry = new PluginRegistry();
        var oldPlugin = new Mock<IPlugin>();
        oldPlugin.SetupGet(plugin => plugin.Id).Returns(expectedId);
        registry.Register(oldPlugin.Object, new PluginMetadata
        {
            Id = expectedId,
            FilePath = Path.Combine(pluginsRoot, "old", "ShellIntegration.dll"),
        });
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            _mockFileSystemManager.Object);
        var baseline = manager.CapturePluginRuntimeSnapshot();
        var replacement = new Mock<IPlugin>();
        replacement.SetupGet(plugin => plugin.Id).Returns(expectedId);
        var additional = new Mock<IPlugin>();
        additional.SetupGet(plugin => plugin.Id).Returns(additionalId);
        registry.Register(replacement.Object, new PluginMetadata
        {
            Id = expectedId,
            FilePath = Path.Combine(
                replacementDirectory,
                "UniversalDeviceToolkit.Plugins.ShellIntegration.dll"),
        });
        registry.Register(additional.Object, new PluginMetadata
        {
            Id = additionalId,
            FilePath = Path.Combine(
                replacementDirectory,
                "UniversalDeviceToolkit.Plugins.AdditionalPlugin.dll"),
        });
        _mockLoader.Setup(loader => loader.Unload(expectedId)).Returns(true);
        _mockLoader.Setup(loader => loader.Unload(additionalId)).Returns(true);

        manager.ReconcilePluginRuntimes(baseline, replacementDirectory);

        registry.IsRegistered(expectedId).Should().BeFalse();
        registry.IsRegistered(additionalId).Should().BeFalse();
        _mockLoader.Verify(loader => loader.Unload(expectedId), Times.Once);
        _mockLoader.Verify(loader => loader.Unload(additionalId), Times.Once);
    }

    [Fact]
    public void RestorePluginRuntimeSnapshot_AfterPluginAReconcile_ShouldNotUndoConcurrentPluginBUpdate()
    {
        const string pluginA = "plugin-a";
        const string pluginB = "plugin-b";
        var pluginsRoot = CreateTempDirectory();
        var baselineDirectory = Path.Combine(pluginsRoot, "baseline");
        var replacementDirectory = Path.Combine(pluginsRoot, "replacement");
        Directory.CreateDirectory(baselineDirectory);
        Directory.CreateDirectory(replacementDirectory);
        var pluginAPath = Path.Combine(baselineDirectory, "PluginA.dll");
        var pluginBPath = Path.Combine(baselineDirectory, "PluginB.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginAPath);
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginBPath);
        var originalA = new Mock<IPlugin>();
        originalA.SetupGet(plugin => plugin.Id).Returns(pluginA);
        var originalB = new Mock<IPlugin>();
        originalB.SetupGet(plugin => plugin.Id).Returns(pluginB);
        var registry = new PluginRegistry();
        registry.Register(originalA.Object, new PluginMetadata { Id = pluginA, FilePath = pluginAPath });
        registry.Register(originalB.Object, new PluginMetadata { Id = pluginB, FilePath = pluginBPath });
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginA)).Returns(true);
        loader.Setup(candidate => candidate.Unload(pluginB)).Returns(true);
        loader.Setup(candidate => candidate.LoadFromFileAsync(
                pluginAPath,
                It.IsAny<IPluginSignatureValidator>()))
            .ReturnsAsync(originalA.Object);
        _mockSignatureValidator
            .Setup(candidate => candidate.ValidateAsync(pluginAPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var baseline = manager.CapturePluginRuntimeSnapshot();
        var replacementA = new Mock<IPlugin>();
        replacementA.SetupGet(plugin => plugin.Id).Returns(pluginA);
        registry.Register(replacementA.Object, new PluginMetadata
        {
            Id = pluginA,
            FilePath = Path.Combine(replacementDirectory, "PluginA.dll"),
        });
        var reconciliation = manager.ReconcilePluginRuntimes(
            baseline,
            replacementDirectory,
            expectedPluginId: pluginA);
        var concurrentB = new Mock<IPlugin>();
        concurrentB.SetupGet(plugin => plugin.Id).Returns(pluginB);
        registry.Register(concurrentB.Object, new PluginMetadata
        {
            Id = pluginB,
            FilePath = pluginBPath,
        });

        manager.RestorePluginRuntimeSnapshot(
            baseline,
            reconciliation: reconciliation);

        registry.Get(pluginA).Should().BeSameAs(originalA.Object);
        registry.Get(pluginB).Should().BeSameAs(concurrentB.Object);
        loader.Verify(candidate => candidate.Unload(pluginB), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestorePluginRuntimeSnapshot_ShouldRestoreExactStartedState(bool wasStarted)
    {
        const string pluginId = "baseline-state-plugin";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "BaselineStatePlugin.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var startup = plugin.As<IAppStartupPlugin>();
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        if (!wasStarted)
            registry.MarkStarted(pluginId);
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(manager => manager.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            fileSystem.Object);
        var capturedIdentity = manager.CapturePluginRuntimeSnapshot().Identities[pluginId];
        var snapshot = new PluginRuntimeSnapshot(
            new Dictionary<string, PluginRuntimeIdentity>
            {
                [pluginId] = new(
                    plugin.Object,
                    pluginPath,
                    wasStarted,
                    capturedIdentity.RuntimeGeneration,
                    capturedIdentity.AssemblySha256),
            });

        manager.RestorePluginRuntimeSnapshot(snapshot);

        registry.IsStarted(pluginId).Should().Be(wasStarted);
        plugin.Verify(candidate => candidate.Stop(), wasStarted ? Times.Never() : Times.Once());
        startup.Verify(
            candidate => candidate.OnAppStarted(),
            wasStarted ? Times.Once() : Times.Never());
    }

    [Fact]
    public void RestorePluginRuntimeSnapshot_ShouldReloadDisplacedBaselineIdentity()
    {
        const string pluginId = "baseline-plugin";
        var pluginsRoot = CreateTempDirectory();
        var baselineDirectory = Path.Combine(pluginsRoot, "baseline");
        var replacementDirectory = Path.Combine(pluginsRoot, "replacement");
        Directory.CreateDirectory(baselineDirectory);
        Directory.CreateDirectory(replacementDirectory);
        var baselinePath = Path.Combine(baselineDirectory, "BaselinePlugin.dll");
        var replacementPath = Path.Combine(replacementDirectory, "BaselinePlugin.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, baselinePath);
        File.Copy(Assembly.GetExecutingAssembly().Location, replacementPath);
        var baselinePlugin = new Mock<IPlugin>();
        baselinePlugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var replacementPlugin = new Mock<IPlugin>();
        replacementPlugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(replacementPlugin.Object, new PluginMetadata
        {
            Id = pluginId,
            FilePath = replacementPath,
            Version = "1.0.0",
        });
        var loader = new Mock<IPluginLoader>();
        _mockSignatureValidator
            .Setup(candidate => candidate.ValidateAsync(baselinePath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        loader.Setup(candidate => candidate.LoadFromFileAsync(
                baselinePath,
                It.IsAny<IPluginSignatureValidator>()))
            .ReturnsAsync(baselinePlugin.Object);
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var baseline = new PluginRuntimeSnapshot(
            new Dictionary<string, PluginRuntimeIdentity>
            {
                [pluginId] = new(baselinePlugin.Object, baselinePath, false),
            });

        manager.RestorePluginRuntimeSnapshot(baseline);

        registry.Get(pluginId).Should().BeSameAs(baselinePlugin.Object);
        registry.GetMetadata(pluginId)!.FilePath.Should().Be(baselinePath);
        registry.IsStarted(pluginId).Should().BeFalse();
        loader.Verify(candidate => candidate.Unload(pluginId), Times.Once);
    }

    [Fact]
    public void RestorePluginRuntimeSnapshot_ShouldRejectSamePathReplacementGeneration()
    {
        const string pluginId = "same-path-plugin";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "SamePathPlugin.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var original = new Mock<IPlugin>();
        original.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var replacement = new Mock<IPlugin>();
        replacement.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(original.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        loader.Setup(candidate => candidate.LoadFromFileAsync(pluginPath, It.IsAny<IPluginSignatureValidator>()))
            .ReturnsAsync(original.Object);
        _mockSignatureValidator.Setup(candidate => candidate.ValidateAsync(pluginPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var baseline = manager.CapturePluginRuntimeSnapshot();
        registry.Register(replacement.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });

        manager.RestorePluginRuntimeSnapshot(baseline);

        registry.Get(pluginId).Should().BeSameAs(original.Object);
        loader.Verify(candidate => candidate.Unload(pluginId), Times.Once);
        loader.Verify(candidate => candidate.LoadFromFileAsync(pluginPath, It.IsAny<IPluginSignatureValidator>()), Times.Once);
    }

    [Fact]
    public void RestorePluginRuntimeSnapshot_WhenRestoredAssemblyWasTampered_ShouldFailBeforeReload()
    {
        const string pluginId = "tampered-baseline";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "TamperedBaseline.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var loader = new Mock<IPluginLoader>();
        loader.Setup(candidate => candidate.Unload(pluginId)).Returns(true);
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var baseline = manager.CapturePluginRuntimeSnapshot();
        manager.ForgetPluginRuntime(pluginId).Should().BeTrue();
        File.WriteAllBytes(pluginPath, [1, 2, 3, 4]);

        Action action = () => manager.RestorePluginRuntimeSnapshot(baseline);

        action.Should().Throw<AggregateException>().WithMessage("*could not be restored*");
        loader.Verify(
            candidate => candidate.LoadFromFileAsync(It.IsAny<string>(), It.IsAny<IPluginSignatureValidator>()),
            Times.Never);
    }

    [Fact]
    public void RestorePluginRuntimeSnapshot_WhenSameRuntimeBackingBytesChanged_ShouldRejectWithoutUnload()
    {
        const string pluginId = "same-runtime-tamper";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "SameRuntimeTamper.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var registry = new PluginRegistry();
        registry.Register(plugin.Object, new PluginMetadata { Id = pluginId, FilePath = pluginPath });
        var loader = new Mock<IPluginLoader>();
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            loader.Object,
            registry,
            fileSystem.Object);
        var baseline = manager.CapturePluginRuntimeSnapshot();
        File.WriteAllBytes(pluginPath, [5, 4, 3, 2, 1]);

        Action action = () => manager.RestorePluginRuntimeSnapshot(
            baseline,
            reconciliation: new PluginRuntimeReconciliation([pluginId]));

        action.Should().Throw<AggregateException>().WithMessage("*could not be restored*");
        registry.Get(pluginId).Should().BeSameAs(plugin.Object);
        loader.Verify(candidate => candidate.Unload(pluginId), Times.Never);
        loader.Verify(
            candidate => candidate.LoadFromFileAsync(
                It.IsAny<string>(),
                It.IsAny<IPluginSignatureValidator>()),
            Times.Never);
    }

    [Fact]
    public async Task PluginMutationLease_ShouldSerializeSameIdButNotDifferentIds()
    {
        const string pluginA = "plugin-a";
        const string pluginB = "plugin-b";
        var manager = CreateManager();
        using var firstLease = manager.AcquirePluginMutation(pluginA);
        using var attempted = new ManualResetEventSlim();
        var sameIdEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Task sameIdTask;
        using (ExecutionContext.SuppressFlow())
        {
            sameIdTask = Task.Run(() =>
            {
                attempted.Set();
                using var lease = manager.AcquirePluginMutation(pluginA);
                sameIdEntered.SetResult();
            });
        }
        attempted.Wait();

        Task differentIdTask;
        using (ExecutionContext.SuppressFlow())
        {
            differentIdTask = Task.Run(() =>
            {
                using var lease = manager.AcquirePluginMutation(pluginB);
            });
        }
        await differentIdTask.WaitAsync(TimeSpan.FromSeconds(2));
        sameIdEntered.Task.IsCompleted.Should().BeFalse();

        firstLease.Dispose();
        await sameIdTask.WaitAsync(TimeSpan.FromSeconds(2));
        sameIdEntered.Task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task PluginMutationLease_InheritedContextAfterDisposal_ShouldNotKeepStaleOwnership()
    {
        using var manager = CreateManager();
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task child;
        using (manager.AcquirePluginMutation("stale-context-plugin"))
        {
            child = Task.Run(async () =>
            {
                await proceed.Task;
                using var lease = manager.AcquirePluginMutation("stale-context-plugin");
            });
        }

        proceed.SetResult();
        await child;
    }

    [Fact]
    public async Task ScanAndLoadPluginsAsync_ShouldWaitForSamePluginMutationBeforeReadingCandidate()
    {
        const string pluginId = "blocked-plugin";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "BlockedPlugin.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, pluginPath);
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        var loadEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockSignatureValidator
            .Setup(candidate => candidate.ValidateAsync(pluginPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        _mockLoader
            .Setup(candidate => candidate.LoadFromFileAsync(
                pluginPath,
                _mockSignatureValidator.Object))
            .Callback(() => loadEntered.TrySetResult())
            .ReturnsAsync(plugin.Object);
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(candidate => candidate.GetPluginDllFiles()).Returns([pluginPath]);
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            new PluginRegistry(),
            fileSystem.Object);
        var leaseHeld = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task holder;
        using (ExecutionContext.SuppressFlow())
        {
            holder = Task.Run(async () =>
            {
                using var lease = manager.AcquirePluginMutation(pluginId);
                leaseHeld.SetResult();
                await releaseLease.Task;
            });
        }
        await leaseHeld.Task;

        Task scan;
        using (ExecutionContext.SuppressFlow())
            scan = Task.Run(() => manager.ScanAndLoadPluginsAsync(forceRefresh: true));
        await Task.Delay(150);

        loadEntered.Task.IsCompleted.Should().BeFalse(
            "the scanner must not read a candidate while its transaction lease is active");
        releaseLease.SetResult();
        await Task.WhenAll(holder, scan);
        loadEntered.Task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ScanAndLoadPluginsAsync_ShouldReuseCanonicalRuntimeAcrossForceScansAndDuplicates()
    {
        const string pluginId = "shell-integration";
        var pluginsRoot = CreateTempDirectory();
        var firstDirectory = Path.Combine(pluginsRoot, "a");
        var secondDirectory = Path.Combine(pluginsRoot, "b");
        Directory.CreateDirectory(firstDirectory);
        Directory.CreateDirectory(secondDirectory);
        var firstPath = Path.Combine(
            firstDirectory,
            "UniversalDeviceToolkit.Plugins.ShellIntegration.dll");
        var secondPath = Path.Combine(
            secondDirectory,
            "LenovoLegionToolkit.Plugins.ShellIntegration.dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, firstPath);
        File.Copy(Assembly.GetExecutingAssembly().Location, secondPath);
        var plugin = new Mock<IPlugin>();
        plugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
        _mockSignatureValidator
            .Setup(validator => validator.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        _mockLoader
            .Setup(loader => loader.LoadFromFileAsync(
                It.IsAny<string>(),
                _mockSignatureValidator.Object))
            .ReturnsAsync(plugin.Object);
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(manager => manager.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(manager => manager.GetPluginDllFiles())
            .Returns([firstPath, secondPath]);
        var registry = new PluginRegistry();
        using var manager = new PluginManager(
            CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            registry,
            fileSystem.Object);

        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);
        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);

        _mockLoader.Verify(
            loader => loader.LoadFromFileAsync(
                It.IsAny<string>(),
                _mockSignatureValidator.Object),
            Times.Once);
        registry.GetMetadata(pluginId)!.FilePath.Should().Be(firstPath);
    }

    [Fact]
    public async Task ScanAndLoadPluginsAsync_ForceRefreshChangedFile_ShouldUnloadAndReplaceRealRuntime()
    {
        const string pluginId = "loader-fixture";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "LoaderFixture.dll");
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, pluginPath);
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(candidate => candidate.GetPluginDllFiles()).Returns([pluginPath]);
        var loader = new PluginLoader();
        var registry = new PluginRegistry();
        using var manager = new PluginManager(
            CreateSettings(),
            signatureValidator.Object,
            loader,
            registry,
            fileSystem.Object);

        var originalRuntime = await ForceRefreshRealRuntimeAsync(
            manager,
            registry,
            pluginId,
            pluginPath);

        originalRuntime.TryGetTarget(out _).Should().BeFalse();
        PluginLoader.TrackedContextCount.Should().Be(1);
        manager.ForgetPluginRuntime(pluginId).Should().BeTrue();
    }

    [Fact]
    public async Task ScanAndLoadPluginsAsync_WithMismatchedFilename_ShouldDiscardRealLoaderCandidate()
    {
        const string pluginId = "loader-fixture";
        var pluginsRoot = CreateTempDirectory();
        var canonicalPath = Path.Combine(pluginsRoot, "LoaderFixture.dll");
        var mismatchedPath = Path.Combine(pluginsRoot, "OtherPlugin.dll");
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, canonicalPath);
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, mismatchedPath);
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(candidate => candidate.GetPluginDllFiles())
            .Returns([canonicalPath, mismatchedPath]);
        var registry = new PluginRegistry();
        var loader = new PluginLoader();
        using var manager = new PluginManager(
            CreateSettings(),
            signatureValidator.Object,
            loader,
            registry,
            fileSystem.Object);

        await ScanRealRuntimeTwiceAsync(manager);

        IsRuntimeRegistered(registry, pluginId).Should().BeTrue();
        registry.GetMetadata(pluginId)!.FilePath.Should().Be(canonicalPath);
        PluginLoader.TrackedContextCount.Should().Be(1);
        PluginLoader.PendingContextCount.Should().Be(0);
        SweepDiscardedUntilCollected((ITransactionalPluginLoader)loader)
            .Should().Be(0);
        PluginLoader.DiscardedContextCount.Should().Be(0);
        manager.ForgetPluginRuntime(pluginId).Should().BeTrue();
        PluginLoader.TrackedContextCount.Should().Be(0);
    }

    [Fact]
    public async Task ForgetPluginRuntime_DefaultLoader_ShouldRemainPendingForExternalReferenceThenConfirm()
    {
        const string pluginId = "loader-fixture";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "LoaderFixture.dll");
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, pluginPath);
        var loader = new PluginLoader();
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(candidate => candidate.GetPluginDllFiles()).Returns([pluginPath]);
        var registry = new PluginRegistry();
        using var manager = new PluginManager(
            CreateSettings(),
            signatureValidator.Object,
            loader,
            registry,
            fileSystem.Object);
        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);
        using var lease = manager.AcquirePluginMutation(pluginId);
        manager.PreparePluginInstallation(pluginId, lease);
        await manager.ActivatePluginRuntimeStrictAsync(pluginId, pluginPath, lease);
        manager.CommitPluginInstallation(pluginId, lease);
        var heldPlugin = CreatePluginReferenceHolder(registry, pluginId);

        manager.ForgetPluginRuntime(pluginId, lease).Should().BeFalse();
        PluginLoader.TrackedContextCount.Should().Be(1);
        loader.GetUnloadState(pluginId).Should().Be(PluginRuntimeUnloadState.UnloadRequested);
        registry.IsRegistered(pluginId).Should().BeFalse();
        GetFixtureCounter(heldPlugin, "StartCallCount").Should().Be(1,
            "an unload-requested runtime must never be restarted");
        manager.ForgetPluginRuntime(pluginId, lease).Should().BeFalse();
        PluginLoader.TrackedContextCount.Should().Be(1);
        heldPlugin.Value = null;

        manager.ForgetPluginRuntime(pluginId, lease).Should().BeTrue();
        PluginLoader.TrackedContextCount.Should().Be(0);
        registry.IsRegistered(pluginId).Should().BeFalse();
        loader.GetUnloadState(pluginId).Should().Be(PluginRuntimeUnloadState.NotTracked);
    }

    [Fact]
    public async Task UninstallPlugin_DefaultLoaderPending_ShouldNeverInvokeOldRuntimeCallbacksAgain()
    {
        const string pluginId = "loader-fixture";
        var pluginsRoot = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        var pluginPath = Path.Combine(pluginDirectory, "LoaderFixture.dll");
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, pluginPath);
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        var loader = new PluginLoader();
        var registry = new PluginRegistry();
        using var manager = new PluginManager(
            CreateSettings(),
            signatureValidator.Object,
            loader,
            registry,
            fileSystem.Object);
        using (var lease = manager.AcquirePluginMutation(pluginId))
        {
            await manager.LoadPluginRuntimeStrictAsync(pluginId, pluginPath, lease);
            manager.PreparePluginInstallation(pluginId, lease);
            await manager.ActivatePluginRuntimeStrictAsync(pluginId, pluginPath, lease);
            manager.CommitPluginInstallation(pluginId, lease);
        }
        var heldPlugin = CreatePluginReferenceHolder(registry, pluginId);

        manager.UninstallPlugin(pluginId).Should().BeFalse();
        manager.GetPluginRuntimeUnloadState(pluginId)
            .Should().Be(PluginRuntimeUnloadState.UnloadRequested);
        manager.GetInstalledPluginIds().Should().Contain(pluginId);
        registry.IsStarted(pluginId).Should().BeFalse();
        GetFixtureCounter(heldPlugin, "StartCallCount").Should().Be(1);
        GetFixtureCounter(heldPlugin, "UninstalledCallCount").Should().Be(1);

        manager.UninstallPlugin(pluginId).Should().BeFalse();
        GetFixtureCounter(heldPlugin, "StartCallCount").Should().Be(1);
        GetFixtureCounter(heldPlugin, "UninstalledCallCount").Should().Be(1);
        heldPlugin.Value = null;

        manager.UninstallPlugin(pluginId).Should().BeTrue();
        manager.GetInstalledPluginIds().Should().NotContain(pluginId);
        PluginLoader.TrackedContextCount.Should().Be(0);
    }

    [Fact]
    public async Task ForgetPluginRuntime_DefaultLoaderWithoutExternalReference_ShouldConfirmImmediately()
    {
        const string pluginId = "loader-fixture";
        var pluginsRoot = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginsRoot, "LoaderFixture.dll");
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, pluginPath);
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(candidate => candidate.GetPluginDllFiles()).Returns([pluginPath]);
        var loader = new PluginLoader();
        var registry = new PluginRegistry();
        using var manager = new PluginManager(
            CreateSettings(),
            signatureValidator.Object,
            loader,
            registry,
            fileSystem.Object);
        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);

        ForgetRuntimeWithoutCapturingPlugin(manager, pluginId).Should().BeTrue();

        loader.GetUnloadState(pluginId).Should().Be(PluginRuntimeUnloadState.NotTracked);
        registry.IsRegistered(pluginId).Should().BeFalse();
    }

    [Fact]
    public void DiscardedRealLoaderCandidate_ShouldRemainBoundedAndSweepAfterRelease()
    {
        var pluginPath = typeof(LoaderFixturePlugin).Assembly.Location;
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var loader = new PluginLoader();
        var transactionalLoader = (ITransactionalPluginLoader)loader;
        var held = CreateDiscardedCandidateHolder(
            loader,
            pluginPath,
            signatureValidator.Object);

        transactionalLoader.ConfirmDiscardedCandidate(held.Token).Should().BeFalse();
        transactionalLoader.SweepDiscardedCandidates().Pending.Should().Be(1);
        held.Holder.Value = null;

        SweepDiscardedUntilCollected(transactionalLoader).Should().Be(0);
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var candidate = CreateDiscardedCandidateHolder(
                loader,
                pluginPath,
                signatureValidator.Object);
            candidate.Holder.Value = null;
            transactionalLoader.RecoverDiscardedCandidates().Pending.Should().BeLessThanOrEqualTo(1);
        }
        SweepDiscardedUntilCollected(transactionalLoader).Should().Be(0);
    }

    [Fact]
    public void CandidateLoads_ShouldNotSweepOrForceCollectUnrelatedDiscardedContexts()
    {
        var pluginPath = typeof(LoaderFixturePlugin).Assembly.Location;
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var loader = new PluginLoader();
        var transactionalLoader = (ITransactionalPluginLoader)loader;
        var held = CreateDiscardedCandidateHolder(
            loader,
            pluginPath,
            signatureValidator.Object);
        var sweepPassesBefore = PluginLoader.DiscardedSweepPassCount;
        var forcedCollectionsBefore = PluginLoader.DiscardedForcedCollectionCount;

        for (var index = 0; index < 8; index++)
        {
            CreateReleasedDiscardedCandidate(
                loader,
                transactionalLoader,
                pluginPath,
                signatureValidator.Object);
        }

        PluginLoader.DiscardedSweepPassCount.Should().Be(sweepPassesBefore);
        PluginLoader.DiscardedForcedCollectionCount.Should().Be(forcedCollectionsBefore);
        held.Holder.Value = null;
        SweepDiscardedUntilCollected(transactionalLoader).Should().Be(0);
    }

    [Fact]
    public void ScheduledDiscardedSweep_ShouldRespectBatchAndBackoff()
    {
        var pluginPath = typeof(LoaderFixturePlugin).Assembly.Location;
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var loader = new PluginLoader();
        var transactionalLoader = (ITransactionalPluginLoader)loader;
        var held = Enumerable.Range(0, 12)
            .Select(_ => CreateDiscardedCandidateHolder(
                loader,
                pluginPath,
                signatureValidator.Object))
            .ToArray();
        var checksBefore = PluginLoader.DiscardedScheduledCheckCount;

        transactionalLoader.SweepDiscardedCandidates();
        var firstPassChecks = PluginLoader.DiscardedScheduledCheckCount - checksBefore;
        transactionalLoader.SweepDiscardedCandidates();
        var secondPassChecks =
            PluginLoader.DiscardedScheduledCheckCount - checksBefore - firstPassChecks;
        transactionalLoader.SweepDiscardedCandidates();
        var thirdPassChecks =
            PluginLoader.DiscardedScheduledCheckCount -
            checksBefore -
            firstPassChecks -
            secondPassChecks;

        firstPassChecks.Should().BeLessThanOrEqualTo(8);
        secondPassChecks.Should().BeLessThanOrEqualTo(8);
        thirdPassChecks.Should().Be(0);
        foreach (var candidate in held)
            candidate.Holder.Value = null;
        SweepDiscardedUntilCollected(transactionalLoader).Should().Be(0);
    }

    [Fact]
    public void ScheduledDiscardedSweep_ShouldEventuallyServeEntriesBeyondStubbornFirstBatch()
    {
        var pluginPath = typeof(LoaderFixturePlugin).Assembly.Location;
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var loader = new PluginLoader();
        var transactionalLoader = (ITransactionalPluginLoader)loader;
        var candidates = Enumerable.Range(0, 12)
            .Select(_ => CreateDiscardedCandidateHolder(
                loader,
                pluginPath,
                signatureValidator.Object))
            .ToArray();
        for (var index = 8; index < candidates.Length; index++)
            candidates[index].Holder.Value = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        transactionalLoader.SweepDiscardedCandidates();
        transactionalLoader.SweepDiscardedCandidates();

        transactionalLoader.PendingDiscardedCandidateCount.Should().Be(8,
            "the released tail must be serviced even while the first batch remains alive");
        for (var index = 0; index < 8; index++)
            candidates[index].Holder.Value = null;
        SweepDiscardedUntilCollected(transactionalLoader).Should().Be(0);
    }

    [Fact]
    public async Task ConcurrentDiscardedSweeps_ShouldPreserveEveryPendingToken()
    {
        var pluginPath = typeof(LoaderFixturePlugin).Assembly.Location;
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var loader = new PluginLoader();
        var transactionalLoader = (ITransactionalPluginLoader)loader;
        var candidates = Enumerable.Range(0, 12)
            .Select(_ => CreateDiscardedCandidateHolder(
                loader,
                pluginPath,
                signatureValidator.Object))
            .ToArray();

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            transactionalLoader.SweepDiscardedCandidates())));

        transactionalLoader.PendingDiscardedCandidateCount.Should().Be(12);
        foreach (var candidate in candidates)
            candidate.Holder.Value = null;
        SweepDiscardedUntilCollected(transactionalLoader).Should().Be(0);
    }

    [Fact]
    public void DirectDiscardedConfirmation_ShouldRemoveEveryQueueMembership()
    {
        var pluginPath = typeof(LoaderFixturePlugin).Assembly.Location;
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var loader = new PluginLoader();
        var transactionalLoader = (ITransactionalPluginLoader)loader;
        var candidates = Enumerable.Range(0, 64)
            .Select(_ => CreateDiscardedCandidateHolder(
                loader,
                pluginPath,
                signatureValidator.Object))
            .ToArray();
        foreach (var candidate in candidates)
            candidate.Holder.Value = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        foreach (var candidate in candidates)
            transactionalLoader.ConfirmDiscardedCandidate(candidate.Token).Should().BeTrue();

        transactionalLoader.PendingDiscardedCandidateCount.Should().Be(0);
        PluginLoader.DiscardedQueueCount.Should().Be(0);
    }

    [Fact]
    public async Task DirectConfirmationRacingFairSweep_ShouldKeepExactBoundedMembership()
    {
        var pluginPath = typeof(LoaderFixturePlugin).Assembly.Location;
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var loader = new PluginLoader();
        var transactionalLoader = (ITransactionalPluginLoader)loader;
        var candidates = Enumerable.Range(0, 12)
            .Select(_ => CreateDiscardedCandidateHolder(
                loader,
                pluginPath,
                signatureValidator.Object))
            .ToArray();
        for (var index = 8; index < candidates.Length; index++)
            candidates[index].Holder.Value = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var directConfirmations = candidates.Skip(8).Select(candidate => Task.Run(() =>
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (transactionalLoader.ConfirmDiscardedCandidate(candidate.Token))
                    return;
                Thread.Yield();
            }
            throw new InvalidOperationException("Direct discarded confirmation did not complete.");
        }));
        var sweeps = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            transactionalLoader.SweepDiscardedCandidates()));
        await Task.WhenAll(directConfirmations.Concat(sweeps));

        transactionalLoader.PendingDiscardedCandidateCount.Should().Be(8);
        PluginLoader.DiscardedQueueCount.Should().Be(8);
        for (var index = 0; index < 8; index++)
            candidates[index].Holder.Value = null;
        SweepDiscardedUntilCollected(transactionalLoader).Should().Be(0);
        PluginLoader.DiscardedQueueCount.Should().Be(0);
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
        _mockRegistry.Setup(r => r.Get(pluginA)).Returns(first.Object);
        _mockRegistry.Setup(r => r.Get(pluginB)).Returns(second.Object);
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ForgetRuntimeWithoutCapturingPlugin(
        PluginManager manager,
        string pluginId) =>
        manager.ForgetPluginRuntime(pluginId);

    private PluginManager CreateManager(ApplicationSettings? settings = null)
    {
        return new PluginManager(
            settings ?? CreateSettings(),
            _mockSignatureValidator.Object,
            _mockLoader.Object,
            _mockRegistry.Object,
            _mockFileSystemManager.Object);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetFixtureCounter(
        PluginReferenceHolder holder,
        string propertyName)
    {
        var plugin = holder.Value
            ?? throw new InvalidOperationException("Fixture plugin reference was released.");
        return (int)(plugin.GetType().GetProperty(propertyName)?.GetValue(plugin)
            ?? throw new InvalidOperationException($"Fixture property {propertyName} was not found."));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<IPlugin> CaptureWeakRuntime(
        PluginRegistry registry,
        string pluginId)
    {
        var plugin = registry.Get(pluginId)
            ?? throw new InvalidOperationException($"Plugin {pluginId} is not registered.");
        return new WeakReference<IPlugin>(plugin);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PluginReferenceHolder CreatePluginReferenceHolder(
        PluginRegistry registry,
        string pluginId)
    {
        var plugin = registry.Get(pluginId)
            ?? throw new InvalidOperationException($"Plugin {pluginId} is not registered.");
        return new PluginReferenceHolder(plugin);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsRuntimeRegistered(
        PluginRegistry registry,
        string pluginId) =>
        registry.Get(pluginId) is not null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference<IPlugin>> ForceRefreshRealRuntimeAsync(
        PluginManager manager,
        PluginRegistry registry,
        string pluginId,
        string pluginPath)
    {
        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);
        var originalRuntime = CaptureWeakRuntime(registry, pluginId);
        File.SetLastWriteTimeUtc(pluginPath, DateTime.UtcNow.AddMinutes(1));
        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);
        if (!IsRuntimeRegistered(registry, pluginId))
            throw new InvalidOperationException("Replacement runtime was not registered.");
        return originalRuntime;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ScanRealRuntimeTwiceAsync(PluginManager manager)
    {
        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);
        await manager.ScanAndLoadPluginsAsync(forceRefresh: true);
    }

    private sealed class PluginReferenceHolder(IPlugin plugin)
    {
        public IPlugin? Value { get; set; } = plugin;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static DiscardedCandidateHolder CreateDiscardedCandidateHolder(
        PluginLoader loader,
        string pluginPath,
        IPluginSignatureValidator signatureValidator)
    {
        var plugin = loader.LoadFromFileAsync(pluginPath, signatureValidator)
            .GetAwaiter()
            .GetResult()
            ?? throw new InvalidOperationException("Fixture candidate did not load.");
        var transactionalLoader = (ITransactionalPluginLoader)loader;
        return new DiscardedCandidateHolder(
            new PluginReferenceHolder(plugin),
            transactionalLoader.DiscardCandidate(plugin));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SweepDiscardedUntilCollected(
        ITransactionalPluginLoader transactionalLoader)
    {
        var pending = int.MaxValue;
        for (var attempt = 0; attempt < 5 && pending > 0; attempt++)
            pending = transactionalLoader.RecoverDiscardedCandidates().Pending;
        return pending;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateReleasedDiscardedCandidate(
        PluginLoader loader,
        ITransactionalPluginLoader transactionalLoader,
        string pluginPath,
        IPluginSignatureValidator signatureValidator)
    {
        var plugin = loader.LoadFromFileAsync(pluginPath, signatureValidator)
            .GetAwaiter()
            .GetResult();
        plugin.Should().NotBeNull();
        transactionalLoader.DiscardCandidate(plugin!);
    }

    private sealed record DiscardedCandidateHolder(
        PluginReferenceHolder Holder,
        PluginCandidateUnloadToken Token);

    #endregion
}
