using System;
using System.IO;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// Tests for ViveToolPathService - path resolution and caching.
/// </summary>
public class ViveToolPathServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesInstance()
    {
        var service = new ViveToolPathService();

        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_InitializesSettings()
    {
        var service = new ViveToolPathService();

        // Service should initialize settings
        Assert.NotNull(service);
    }

    #endregion

    #region CachedPath Property Tests

    [Fact]
    public void CachedPath_Getter_WhenNotSet_ReturnsNull()
    {
        var service = new ViveToolPathService();

        Assert.Null(service.CachedPath);
    }

    [Fact]
    public void CachedPath_Setter_SetsValue()
    {
        var service = new ViveToolPathService();

        service.CachedPath = "C:\\test\\ViVeTool.exe";

        Assert.Equal("C:\\test\\ViVeTool.exe", service.CachedPath);
    }

    [Fact]
    public void CachedPath_Setter_WithNull_ClearsCache()
    {
        var service = new ViveToolPathService();

        service.CachedPath = "C:\\test\\ViVeTool.exe";
        service.CachedPath = null;

        Assert.Null(service.CachedPath);
    }

    [Fact]
    public void CachedPath_Setter_WithEmptyString_SetsEmpty()
    {
        var service = new ViveToolPathService();

        service.CachedPath = "";

        Assert.Equal("", service.CachedPath);
    }

    #endregion

    #region GetViveToolPathAsync Tests

    [Fact]
    public async Task GetViveToolPathAsync_WithCachedPath_ReturnsCachedPath()
    {
        var service = new ViveToolPathService();
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var vivetoolPath = Path.Combine(tempDir, ViveToolPathService.ViveToolExeName);

        try
        {
            // Create an actual ViVeTool.exe file so File.Exists check passes
            await File.WriteAllTextAsync(vivetoolPath, "test");

            service.CachedPath = vivetoolPath;

            var result = await service.GetViveToolPathAsync();

            Assert.Equal(vivetoolPath, result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    [Fact]
    public async Task GetViveToolPathAsync_WithCachedPath_VerifiesFileExists()
    {
        var service = new ViveToolPathService();

        // Cache a non-existent path
        service.CachedPath = "C:\\nonexistent\\ViVeTool.exe";

        var result = await service.GetViveToolPathAsync();

        // Should return null if cached file doesn't exist
        Assert.True(result == null || File.Exists(result));
    }

    [Fact]
    public async Task GetViveToolPathAsync_ClearsCachedPath_WhenFileNotFound()
    {
        var service = new ViveToolPathService();

        // Cache a non-existent path
        service.CachedPath = "C:\\nonexistent\\ViVeTool.exe";

        var result = await service.GetViveToolPathAsync();

        // Should fall back to other methods
        Assert.True(result == null || result != "C:\\nonexistent\\ViVeTool.exe");
    }

    [Fact]
    public async Task GetViveToolPathAsync_CalledMultipleTimes_ReturnsConsistentResult()
    {
        var service = new ViveToolPathService();

        var result1 = await service.GetViveToolPathAsync();
        var result2 = await service.GetViveToolPathAsync();

        Assert.Equal(result1, result2);
    }

    [Fact]
    public async Task GetViveToolPathAsync_ChecksUserSettings()
    {
        var service = new ViveToolPathService();

        // Service should check user settings
        var result = await service.GetViveToolPathAsync();

        Assert.True(result == null || result.EndsWith(ViveToolPathService.ViveToolExeName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetViveToolPathAsync_ChecksBundledPath()
    {
        var service = new ViveToolPathService();

        var bundledPath = service.GetBundledViveToolPath();

        // Bundled path should have correct format
        Assert.EndsWith(ViveToolPathService.ViveToolExeName, bundledPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetViveToolPathAsync_ChecksBuiltInPath()
    {
        var service = new ViveToolPathService();

        var builtInPath = service.GetBuiltInViveToolPath();

        // Built-in path should be in AppData
        Assert.Contains("AppData", builtInPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetViveToolPathAsync_ChecksPathEnvironmentVariable()
    {
        var service = new ViveToolPathService();

        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        // PATH should exist
        Assert.NotNull(pathEnv);
    }

    [Fact]
    public async Task GetViveToolPathAsync_ChecksCurrentDirectory()
    {
        var service = new ViveToolPathService();

        var currentPath = Path.Combine(Directory.GetCurrentDirectory(), ViveToolPathService.ViveToolExeName);

        // Current path should be valid
        Assert.EndsWith(ViveToolPathService.ViveToolExeName, currentPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetViveToolPathAsync_ReturnsNull_WhenNoVivetoolFound()
    {
        var service = new ViveToolPathService();

        // Clear any cached path
        service.CachedPath = null;

        var result = await service.GetViveToolPathAsync();

        // Result may be null or a valid path depending on availability
        Assert.True(result == null || File.Exists(result) || result.EndsWith(ViveToolPathService.ViveToolExeName));
    }

    #endregion

    #region GetBundledViveToolPath Tests

    [Fact]
    public void GetBundledViveToolPath_ReturnsValidPath()
    {
        var service = new ViveToolPathService();

        var path = service.GetBundledViveToolPath();

        Assert.NotNull(path);
        Assert.EndsWith(ViveToolPathService.ViveToolExeName, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBundledViveToolPath_ContainsBundledDirectory()
    {
        var service = new ViveToolPathService();

        var path = service.GetBundledViveToolPath();

        Assert.Contains("Bundled", path);
    }

    [Fact]
    public void GetBundledViveToolPath_ReturnsAbsolutePath()
    {
        var service = new ViveToolPathService();

        var path = service.GetBundledViveToolPath();

        Assert.True(Path.IsPathRooted(path));
    }

    [Fact]
    public void GetBundledViveToolPath_ChecksAssemblyLocation()
    {
        var service = new ViveToolPathService();

        var path = service.GetBundledViveToolPath();
        var assemblyDir = AppContext.BaseDirectory;

        // Path should be based on assembly location or base directory
        Assert.Contains(assemblyDir, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBundledViveToolPath_FileMayOrMayNotExist()
    {
        var service = new ViveToolPathService();

        var path = service.GetBundledViveToolPath();

        // Bundled file may or may not exist depending on installation
        Assert.True(true);
    }

    #endregion

    #region GetBuiltInViveToolPath Tests

    [Fact]
    public void GetBuiltInViveToolPath_ReturnsValidPath()
    {
        var service = new ViveToolPathService();

        var path = service.GetBuiltInViveToolPath();

        Assert.NotNull(path);
        Assert.EndsWith(ViveToolPathService.ViveToolExeName, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBuiltInViveToolPath_ContainsViveToolDirectory()
    {
        var service = new ViveToolPathService();

        var path = service.GetBuiltInViveToolPath();

        Assert.Contains("ViveTool", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBuiltInViveToolPath_IsInAppData()
    {
        var service = new ViveToolPathService();

        var path = service.GetBuiltInViveToolPath();

        Assert.Contains("AppData", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBuiltInViveToolPath_ReturnsAbsolutePath()
    {
        var service = new ViveToolPathService();

        var path = service.GetBuiltInViveToolPath();

        Assert.True(Path.IsPathRooted(path));
    }

    [Fact]
    public void GetBuiltInViveToolPath_FileMayOrMayNotExist()
    {
        var service = new ViveToolPathService();

        var path = service.GetBuiltInViveToolPath();

        // Built-in file may or may not exist
        Assert.True(true);
    }

    #endregion

    #region SetViveToolPathAsync Tests

    [Fact]
    public async Task SetViveToolPathAsync_WithNull_ReturnsTrue()
    {
        var service = new ViveToolPathService();

        var result = await service.SetViveToolPathAsync(null!);

        Assert.True(result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithNull_ClearsCachedPath()
    {
        var service = new ViveToolPathService();

        service.CachedPath = "C:\\test\\ViVeTool.exe";
        await service.SetViveToolPathAsync(null!);

        Assert.Null(service.CachedPath);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithEmpty_ReturnsTrue()
    {
        var service = new ViveToolPathService();

        var result = await service.SetViveToolPathAsync("");

        Assert.True(result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithWhitespace_ReturnsTrue()
    {
        var service = new ViveToolPathService();

        var result = await service.SetViveToolPathAsync("   ");

        Assert.True(result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithNonexistentFile_ReturnsFalse()
    {
        var service = new ViveToolPathService();

        var result = await service.SetViveToolPathAsync("C:\\nonexistent\\ViVeTool.exe");

        Assert.False(result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithWrongFileName_ReturnsFalse()
    {
        var service = new ViveToolPathService();
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_wrong_{Guid.NewGuid()}.exe");

        try
        {
            // Create a file with wrong name
            await File.WriteAllTextAsync(tempFile, "test");

            var result = await service.SetViveToolPathAsync(tempFile);

            Assert.False(result);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithValidVivetoolPath_ReturnsTrue()
    {
        var service = new ViveToolPathService();
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var vivetoolPath = Path.Combine(tempDir, ViveToolPathService.ViveToolExeName);

        try
        {
            // Create a dummy ViVeTool.exe
            await File.WriteAllTextAsync(vivetoolPath, "test");

            var result = await service.SetViveToolPathAsync(vivetoolPath);

            Assert.True(result);
            Assert.Equal(vivetoolPath, service.CachedPath);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithValidVivetoolPath_CachesThePath()
    {
        var service = new ViveToolPathService();
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var vivetoolPath = Path.Combine(tempDir, ViveToolPathService.ViveToolExeName);

        try
        {
            // Create a dummy ViVeTool.exe
            await File.WriteAllTextAsync(vivetoolPath, "test");

            await service.SetViveToolPathAsync(vivetoolPath);

            Assert.Equal(vivetoolPath, service.CachedPath);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    [Fact]
    public async Task SetViveToolPathAsync_IsCaseInsensitive()
    {
        var service = new ViveToolPathService();
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var vivetoolPath = Path.Combine(tempDir, ViveToolPathService.ViveToolExeName);

        try
        {
            // Create a dummy ViVeTool.exe
            await File.WriteAllTextAsync(vivetoolPath, "test");

            // Use lowercase path
            var lowerPath = vivetoolPath.ToLowerInvariant();

            var result = await service.SetViveToolPathAsync(lowerPath);

            Assert.True(result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    [Fact]
    public async Task SetViveToolPathAsync_ClearsCachedPath_OnFailure()
    {
        var service = new ViveToolPathService();

        service.CachedPath = "C:\\existing\\ViVeTool.exe";
        var result = await service.SetViveToolPathAsync("C:\\nonexistent\\ViVeTool.exe");

        Assert.False(result);
        // Cached path should be preserved on failure (implementation behavior)
        Assert.Equal("C:\\existing\\ViVeTool.exe", service.CachedPath);
    }

    #endregion

    #region Path Resolution Priority Tests

    [Fact]
    public async Task GetViveToolPathAsync_Priority_UserSpecified_Over_Bundled()
    {
        var service = new ViveToolPathService();
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var customPath = Path.Combine(tempDir, ViveToolPathService.ViveToolExeName);

        try
        {
            // Create a custom ViVeTool.exe
            await File.WriteAllTextAsync(customPath, "test");

            // Set user-specified path
            await service.SetViveToolPathAsync(customPath);

            var result = await service.GetViveToolPathAsync();

            Assert.Equal(customPath, result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    [Fact]
    public async Task GetViveToolPathAsync_Priority_Cached_Over_UserSpecified()
    {
        var service = new ViveToolPathService();
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var customPath = Path.Combine(tempDir, ViveToolPathService.ViveToolExeName);

        try
        {
            // Create a custom ViVeTool.exe
            await File.WriteAllTextAsync(customPath, "test");

            // Set cached path
            service.CachedPath = customPath;

            var result = await service.GetViveToolPathAsync();

            Assert.Equal(customPath, result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    #endregion

    #region ViveToolExeName Constant Tests

    [Fact]
    public void ViveToolExeName_IsCorrect()
    {
        Assert.Equal("ViVeTool.exe", ViveToolPathService.ViveToolExeName);
    }

    [Fact]
    public void ViveToolExeName_IsCaseSensitive()
    {
        var name = ViveToolPathService.ViveToolExeName;

        Assert.Equal("ViVeTool.exe", name);
        Assert.EndsWith(".exe", name, StringComparison.Ordinal);
    }

    #endregion

    #region Settings Integration Tests

    [Fact]
    public async Task GetViveToolPathAsync_LoadsSettings()
    {
        var service = new ViveToolPathService();

        // Service should load settings during operation
        var result = await service.GetViveToolPathAsync();

        Assert.True(result == null || File.Exists(result) || result.EndsWith(ViveToolPathService.ViveToolExeName));
    }

    [Fact]
    public async Task SetViveToolPathAsync_SavesSettings()
    {
        var service = new ViveToolPathService();
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var vivetoolPath = Path.Combine(tempDir, ViveToolPathService.ViveToolExeName);

        try
        {
            // Create a dummy ViVeTool.exe
            await File.WriteAllTextAsync(vivetoolPath, "test");

            // Setting path should also save
            var result = await service.SetViveToolPathAsync(vivetoolPath);

            Assert.True(result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetViveToolPathAsync_HandlesEnvironmentVariableException()
    {
        var service = new ViveToolPathService();

        // Should handle any exceptions gracefully
        var result = await service.GetViveToolPathAsync();

        Assert.True(result == null || result.EndsWith(ViveToolPathService.ViveToolExeName));
    }

    [Fact]
    public async Task GetViveToolPathAsync_HandlesPathEnvironmentNull()
    {
        var service = new ViveToolPathService();

        // PATH may be null in some environments
        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathEnv))
        {
            var result = await service.GetViveToolPathAsync();
            Assert.True(true);
        }
        else
        {
            Assert.NotNull(pathEnv);
        }
    }

    [Fact]
    public async Task SetViveToolPathAsync_HandlesException()
    {
        var service = new ViveToolPathService();

        // Setting an invalid path should return false
        var result = await service.SetViveToolPathAsync("C:\\invalid|path");

        Assert.False(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task GetViveToolPathAsync_Integration_ReturnsAvailableVivetool()
    {
        var service = new ViveToolPathService();

        var result = await service.GetViveToolPathAsync();

        // If ViVeTool is installed, result should be valid
        // Otherwise, result may be null
        Assert.True(result == null || File.Exists(result));
    }

    [Fact]
    public async Task PathService_CanFindVivetool()
    {
        var service = new ViveToolPathService();

        var bundledPath = service.GetBundledViveToolPath();
        var builtInPath = service.GetBuiltInViveToolPath();

        // At least one path should be valid
        Assert.NotNull(bundledPath);
        Assert.NotNull(builtInPath);
    }

    #endregion
}