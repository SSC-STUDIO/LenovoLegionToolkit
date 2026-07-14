using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class PluginFileSystemManagerTests
{
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
        cultures.Should().Contain(new[] { "de", "ja", "ru", "zh-hans", "zh-hant", "fr", "es", "it", "ko" });
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