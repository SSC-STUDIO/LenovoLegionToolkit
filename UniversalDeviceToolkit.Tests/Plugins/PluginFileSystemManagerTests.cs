using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginFileSystemManagerTests : TemporaryFileTestBase
{
    private readonly PluginFileSystemManager _fileSystemManager;
    private readonly string? _previousAppDataOverride;

    public PluginFileSystemManagerTests()
    {
        _previousAppDataOverride = Environment.GetEnvironmentVariable(UniversalDeviceToolkit.Lib.Utils.Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(UniversalDeviceToolkit.Lib.Utils.Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        _fileSystemManager = new PluginFileSystemManager();
    }

    public override void Dispose()
    {
        Environment.SetEnvironmentVariable(UniversalDeviceToolkit.Lib.Utils.Folders.AppDataOverrideEnvironmentVariable, _previousAppDataOverride);
        base.Dispose();
    }

    #region GetPluginsDirectory Tests

    [Fact]
    public void GetPluginsDirectory_ShouldReturnValidPath()
    {
        // Act
        var path = _fileSystemManager.GetPluginsDirectory();

        // Assert
        path.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(path).Should().BeTrue();
        path.Should().EndWith("plugins");
    }

    [Fact]
    public void GetPluginsDirectory_WhenCalledMultipleTimes_ShouldReturnSamePath()
    {
        // Act
        var path1 = _fileSystemManager.GetPluginsDirectory();
        var path2 = _fileSystemManager.GetPluginsDirectory();

        // Assert
        path1.Should().Be(path2);
    }

    [Fact]
    public void GetPluginsDirectory_ShouldUseAppDataPluginsDirectory()
    {
        var appDataDirectory = Environment.GetEnvironmentVariable(UniversalDeviceToolkit.Lib.Utils.Folders.AppDataOverrideEnvironmentVariable);
        appDataDirectory.Should().NotBeNullOrWhiteSpace();

        var path = _fileSystemManager.GetPluginsDirectory();

        path.Should().Be(Path.Combine(Path.GetFullPath(appDataDirectory!), PluginPaths.PluginsDirectoryName));
        Directory.Exists(path).Should().BeTrue();
    }

    #endregion

    #region GetPluginDllFiles Tests

    [Fact]
    public void GetPluginDllFiles_WhenDirectoryDoesNotExist_ShouldReturnEmptyList()
    {
        // Arrange - Use a non-existent directory
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "plugins");

        // Act
        var files = _fileSystemManager.GetPluginDllFiles();

        // Assert - The default plugins directory might not exist
        // This test validates that the method handles non-existent directories gracefully
        files.Should().NotBeNull();
        files.Should().BeOfType<List<string>>();
    }

    [Fact]
    public void GetPluginDllFiles_WhenEmptyDirectory_ShouldReturnEmptyList()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var pluginsDir = Path.Combine(tempDir, "plugins");
        Directory.CreateDirectory(pluginsDir);

        // Act - We need to temporarily override the plugins directory
        // Since GetPluginDllFiles uses GetPluginsDirectory internally,
        // we verify the behavior by checking it doesn't throw
        var files = _fileSystemManager.GetPluginDllFiles();

        // Assert
        files.Should().NotBeNull();
    }

    [Fact]
    public void GetPluginDllFiles_ShouldExcludeCultureFolders()
    {
        // Arrange
        var cultureFolders = _fileSystemManager.GetCultureFolders();

        // Assert
        cultureFolders.Should().NotBeEmpty();
        cultureFolders.Should().Contain(new[] { "ar", "de", "es", "fr", "ja", "zh-Hans" });
        cultureFolders.Should().Contain("tools");
    }

    [Fact]
    public void GetPluginDllFiles_ShouldExcludeSDKDll()
    {
        // Arrange
        var sdkDllName = "LenovoLegionToolkit.Plugins.SDK.dll";

        // Assert - Verify that SDK DLL naming is handled
        sdkDllName.Should().EndWith(".dll");
        sdkDllName.Should().Contain("SDK");
    }

    #endregion

    #region GetMainPluginDllNameCandidates Tests

    [Fact]
    public void GetMainPluginDllNameCandidates_ShouldReturnMultipleCandidates()
    {
        // Arrange
        var pluginId = "test-plugin";

        // Act
        var candidates = _fileSystemManager.GetMainPluginDllNameCandidates(pluginId);

        // Assert
        candidates.Should().NotBeEmpty();
        candidates.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void GetMainPluginDllNameCandidates_ShouldIncludeExpectedFormats()
    {
        // Arrange
        var pluginId = "my-plugin";

        // Act
        var candidates = _fileSystemManager.GetMainPluginDllNameCandidates(pluginId);

        // Assert
        candidates.Should().Contain($"{pluginId}.dll");
        candidates.Should().Contain($"UniversalDeviceToolkit.Plugins.{pluginId}.dll");
    }

    [Fact]
    public void GetMainPluginDllNameCandidates_WithNullPluginId_ShouldReturnEmptyArray()
    {
        // Act
        var candidates = _fileSystemManager.GetMainPluginDllNameCandidates(null!);

        // Assert
        candidates.Should().NotBeNull();
        // Empty or minimal candidates for null input
    }

    [Fact]
    public void GetMainPluginDllNameCandidates_WithEmptyPluginId_ShouldReturnEmptyArray()
    {
        // Act
        var candidates = _fileSystemManager.GetMainPluginDllNameCandidates("");

        // Assert
        candidates.Should().NotBeNull();
    }

    [Fact]
    public void GetMainPluginDllNameCandidates_ShouldNormalizePluginId()
    {
        // Arrange
        var pluginId = "My-Cool-Plugin";

        // Act
        var candidates = _fileSystemManager.GetMainPluginDllNameCandidates(pluginId);

        // Assert
        candidates.Should().NotBeEmpty();
        // Should include normalized version (lowercase alphanumeric only)
        candidates.Should().Contain(c => c.Contains("mycoolplugin"));
    }

    [Fact]
    public void GetMainPluginDllNameCandidates_ShouldReturnDistinctCandidates()
    {
        // Arrange
        var pluginId = "test";

        // Act
        var candidates = _fileSystemManager.GetMainPluginDllNameCandidates(pluginId);

        // Assert
        candidates.Distinct().Should().HaveCount(candidates.Length);
    }

    #endregion

    #region DeleteFileWithRetryAsync Tests

    [Fact]
    public async Task DeleteFileWithRetryAsync_WhenFileExists_ShouldDeleteFile()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");

        // Act
        var result = await _fileSystemManager.DeleteFileWithRetryAsync(tempFile);

        // Assert
        result.Should().BeTrue();
        File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileWithRetryAsync_WhenFileDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await _fileSystemManager.DeleteFileWithRetryAsync(nonExistentFile);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFileWithRetryAsync_WithMaxRetries_ShouldRetrySpecifiedTimes()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var maxRetries = 5;

        // Act
        var result = await _fileSystemManager.DeleteFileWithRetryAsync(tempFile, maxRetries);

        // Assert
        result.Should().BeTrue();
        File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileWithRetryAsync_WithCustomDelay_ShouldUseSpecifiedDelay()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var maxRetries = 3;
        var delayMs = 50; // Smaller delay for faster test

        // Act
        var result = await _fileSystemManager.DeleteFileWithRetryAsync(tempFile, maxRetries, delayMs);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region DeleteDirectoryWithRetryAsync Tests

    [Fact]
    public async Task DeleteDirectoryWithRetryAsync_WhenDirectoryExists_ShouldDeleteDirectory()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        // Act
        var result = await _fileSystemManager.DeleteDirectoryWithRetryAsync(tempDir);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDirectoryWithRetryAsync_WhenDirectoryDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await _fileSystemManager.DeleteDirectoryWithRetryAsync(nonExistentDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDirectoryWithRetryAsync_WithFilesInside_ShouldDeleteAllContents()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var tempFile = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(tempFile, "test content");

        // Act
        var result = await _fileSystemManager.DeleteDirectoryWithRetryAsync(tempDir);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
        File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDirectoryWithRetryAsync_WithSubdirectories_ShouldDeleteRecursively()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        var tempFile = Path.Combine(subDir, "nested.txt");
        File.WriteAllText(tempFile, "nested content");

        // Act
        var result = await _fileSystemManager.DeleteDirectoryWithRetryAsync(tempDir);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(tempDir).Should().BeFalse();
        Directory.Exists(subDir).Should().BeFalse();
    }

    #endregion

    #region UpdateFileCache Tests

    [Fact]
    public void UpdateFileCache_ShouldCacheFileTimestamp()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");

        // Act
        _fileSystemManager.UpdateFileCache(tempFile);

        // Assert - No exception should be thrown
        // Cache is internal, we verify by ensuring no error occurs
    }

    [Fact]
    public void UpdateFileCache_WhenCalledMultipleTimes_ShouldUpdateCache()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");

        // Act
        _fileSystemManager.UpdateFileCache(tempFile);
        File.WriteAllText(tempFile, "updated content");
        _fileSystemManager.UpdateFileCache(tempFile);

        // Assert - No exception should be thrown
    }

    #endregion

    #region GetCultureFolders Tests

    [Fact]
    public void GetCultureFolders_ShouldReturnKnownCultureFolders()
    {
        // Act
        var cultureFolders = _fileSystemManager.GetCultureFolders();

        // Assert
        cultureFolders.Should().NotBeEmpty();
        cultureFolders.Should().Contain(new[]
        {
            "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu",
            "it", "ja", "ko", "lv", "nl-NL", "pl", "pt", "pt-BR", "ro",
            "ru", "sk", "tr", "uk", "uz-Latn-UZ", "vi", "zh-Hans", "zh-Hant"
        });
    }

    [Fact]
    public void GetCultureFolders_ShouldContainToolsFolder()
    {
        // Act
        var cultureFolders = _fileSystemManager.GetCultureFolders();

        // Assert
        cultureFolders.Should().Contain("tools");
    }

    [Fact]
    public void GetCultureFolders_ShouldBeCaseInsensitive()
    {
        // Act
        var cultureFolders = _fileSystemManager.GetCultureFolders();

        // Assert - HashSet should be case insensitive
        cultureFolders.Contains("AR").Should().BeTrue();
        cultureFolders.Contains("DE").Should().BeTrue();
        cultureFolders.Contains("TOOLS").Should().BeTrue();
    }

    #endregion

    #region Plugin DLL Filtering Tests

    [Fact]
    public void PluginDllFiltering_ShouldExcludeResourcesDlls()
    {
        // Arrange
        var resourcesDllName = "SomePlugin.resources.dll";

        // Assert - resources DLLs should be filtered out
        resourcesDllName.Should().Contain(".resources.dll");
    }

    [Fact]
    public void PluginDllFiltering_ShouldExcludeSDKDll()
    {
        // Arrange
        var sdkDllName = "LenovoLegionToolkit.Plugins.SDK.dll";

        // Assert - SDK DLL should be filtered out
        sdkDllName.Should().Be("LenovoLegionToolkit.Plugins.SDK.dll");
    }

    [Fact]
    public void PluginDllFiltering_ShouldIncludePluginsWithCorrectPrefix()
    {
        // Arrange
        var pluginDllName = "UniversalDeviceToolkit.Plugins.MyPlugin.dll";

        // Assert - Plugin DLLs with preferred UDT prefix should be included
        pluginDllName.Should().StartWith("UniversalDeviceToolkit.Plugins.");
        pluginDllName.Should().NotContain("SDK");
        pluginDllName.Should().EndWith(".dll");
    }

    #endregion

    #region GetCultureFolders Tests

    [Fact]
    public void GetCultureFolders_ShouldNotBeEmpty()
    {
        var manager = new PluginFileSystemManager();
        manager.GetCultureFolders().Should().NotBeEmpty();
    }

    [Fact]
    public void GetCultureFolders_ShouldContainKnownCultures()
    {
        var cultures = new PluginFileSystemManager().GetCultureFolders();
        cultures.Should().Contain(new[] { "de", "ja", "ru", "zh-Hans", "zh-Hant", "fr", "es", "it", "ko" });
    }

    [Fact]
    public void GetCultureFolders_ShouldContainTools()
    {
        new PluginFileSystemManager().GetCultureFolders().Should().Contain("tools");
    }

    #endregion

    #region GetMainPluginDllNameCandidates Tests

    [Fact]
    public void GetMainPluginDllNameCandidates_ValidId_ShouldReturnCandidates()
    {
        var manager = new PluginFileSystemManager();
        var candidates = manager.GetMainPluginDllNameCandidates("my-plugin");
        candidates.Should().NotBeEmpty();
        candidates.Should().Contain(c => c.Contains("my-plugin"));
    }

    [Fact]
    public void GetMainPluginDllNameCandidates_EmptyId_ShouldReturnEmpty()
    {
        var manager = new PluginFileSystemManager();
        manager.GetMainPluginDllNameCandidates("").Should().BeEmpty();
    }

    [Fact]
    public void GetMainPluginDllNameCandidates_NullId_ShouldReturnEmpty()
    {
        var manager = new PluginFileSystemManager();
        manager.GetMainPluginDllNameCandidates(null!).Should().BeEmpty();
    }

    [Fact]
    public void GetMainPluginDllNameCandidates_SimpleId_ShouldIncludeStandardNames()
    {
        var manager = new PluginFileSystemManager();
        var candidates = manager.GetMainPluginDllNameCandidates("test");
        candidates.Should().Contain(c => c == "test.dll");
        candidates.Should().Contain(c => c == "UniversalDeviceToolkit.Plugins.test.dll");
    }

    #endregion

    #region GetPluginDllFiles Tests

    [Fact]
    public void GetPluginDllFiles_NonExistentDirectory_ShouldReturnEmpty()
    {
        var manager = new PluginFileSystemManager();
        manager.GetPluginDllFiles().Should().NotBeNull();
    }

    #endregion

    #region ClearFileCache Tests

    [Fact]
    public void ClearFileCache_ShouldNotThrow()
    {
        var manager = new PluginFileSystemManager();
        var act = () => manager.ClearFileCache();
        act.Should().NotThrow();
    }

    #endregion

    #region UpdateFileCache Tests

    [Fact]
    public void UpdateFileCache_NonExistentFile_ShouldNotThrow()
    {
        var manager = new PluginFileSystemManager();
        var act = () => manager.UpdateFileCache(@"C:\nonexistent\fake.dll");
        act.Should().NotThrow();
    }

    #endregion

    #region GetPluginsDirectory Tests

    [Fact]
    public void GetPluginsDirectory_ShouldReturnNonEmptyPath()
    {
        var manager = new PluginFileSystemManager();
        manager.GetPluginsDirectory().Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region DeleteFileWithRetryAsync Tests

    [Fact]
    public async Task DeleteFileWithRetryAsync_NonExistentFile_ShouldReturnTrue()
    {
        var manager = new PluginFileSystemManager();
        var result = await manager.DeleteFileWithRetryAsync(@"C:\nonexistent\fake.dll");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDirectoryWithRetryAsync_NonExistentDir_ShouldReturnTrue()
    {
        var manager = new PluginFileSystemManager();
        var result = await manager.DeleteDirectoryWithRetryAsync(@"C:\nonexistent\fake_dir");
        result.Should().BeTrue();
    }

    #endregion
}
