using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// Tests for ViveToolPathService - path resolution and caching.
/// </summary>
public class ViveToolPathServiceTests
{
    private static readonly FieldInfo SettingsField = typeof(ViveToolPathService)
        .GetField("_settings", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [Fact]
    public async Task Constructor_CreatesInstance()
    {
        await using var harness = await CreateHarnessAsync();

        Assert.NotNull(harness.Service);
    }

    [Fact]
    public async Task CachedPath_Getter_WhenNotSet_ReturnsNull()
    {
        await using var harness = await CreateHarnessAsync();

        Assert.Null(harness.Service.CachedPath);
    }

    [Fact]
    public async Task CachedPath_Setter_StoresAndClearsValue()
    {
        await using var harness = await CreateHarnessAsync();

        harness.Service.CachedPath = "C:\\test\\ViVeTool.exe";
        Assert.Equal("C:\\test\\ViVeTool.exe", harness.Service.CachedPath);

        harness.Service.CachedPath = null;
        Assert.Null(harness.Service.CachedPath);
    }

    [Fact]
    public async Task GetViveToolPathAsync_WithTrustedCachedPath_ReturnsCachedPath()
    {
        await using var harness = await CreateHarnessAsync();
        await harness.ClearPersistedOverrideAsync();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        harness.Service.CachedPath = runtime.ExePath;

        var result = await harness.Service.GetViveToolPathAsync();

        Assert.Equal(runtime.ExePath, result);
    }

    [Fact]
    public async Task GetViveToolPathAsync_WithInvalidCachedPath_FallsBackToBundledPath()
    {
        await using var harness = await CreateHarnessAsync();
        await harness.ClearPersistedOverrideAsync();
        harness.Service.CachedPath = "C:\\nonexistent\\ViVeTool.exe";

        var result = await harness.Service.GetViveToolPathAsync();

        Assert.Equal(harness.Service.GetBundledViveToolPath(), result);
        Assert.Equal(result, harness.Service.CachedPath);
    }

    [Fact]
    public async Task GetViveToolPathAsync_WithoutOverride_ResolvesBundledPath()
    {
        await using var harness = await CreateHarnessAsync();
        await harness.ClearPersistedOverrideAsync();
        harness.Service.CachedPath = null;

        var result = await harness.Service.GetViveToolPathAsync();

        Assert.Equal(harness.Service.GetBundledViveToolPath(), result);
        AssertRuntimeFilesExist(result!);
    }

    [Fact]
    public async Task GetViveToolPathAsync_CalledMultipleTimes_ReturnsSameBundledPath()
    {
        await using var harness = await CreateHarnessAsync();
        await harness.ClearPersistedOverrideAsync();

        var result1 = await harness.Service.GetViveToolPathAsync();
        var result2 = await harness.Service.GetViveToolPathAsync();

        Assert.Equal(result1, result2);
        Assert.Equal(harness.Service.GetBundledViveToolPath(), result1);
    }

    [Fact]
    public async Task GetBundledViveToolPath_ReturnsAbsoluteBundledRuntimePath()
    {
        await using var harness = await CreateHarnessAsync();

        var path = harness.Service.GetBundledViveToolPath();

        Assert.True(Path.IsPathRooted(path));
        Assert.EndsWith(ViveToolPathService.ViveToolExeName, path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"{Path.DirectorySeparatorChar}Bundled{Path.DirectorySeparatorChar}", path, StringComparison.OrdinalIgnoreCase);
        AssertRuntimeFilesExist(path);
    }

    [Fact]
    public async Task GetBundledViveToolPath_WithLocalPluginDirectoryOverride_ReturnsLocalPackageRuntime()
    {
        const string overrideEnvironmentVariable = "LLT_PLUGIN_DIRECTORY_OVERRIDE";
        var originalOverride = Environment.GetEnvironmentVariable(overrideEnvironmentVariable);
        var pluginsDirectoryPath = Path.Combine(Path.GetTempPath(), $"llt-vivetool-plugins-{Guid.NewGuid():N}");
        var bundledDirectoryPath = Path.Combine(pluginsDirectoryPath, "local", "vive-tool", "Bundled");

        try
        {
            await ViveToolTestRuntimeHelper.CreateCompleteRuntimeAsync(bundledDirectoryPath);
            Environment.SetEnvironmentVariable(overrideEnvironmentVariable, pluginsDirectoryPath);

            await using var harness = await CreateHarnessAsync();

            var path = harness.Service.GetBundledViveToolPath();

            Assert.Equal(
                Path.GetFullPath(Path.Combine(bundledDirectoryPath, ViveToolPathService.ViveToolExeName)),
                Path.GetFullPath(path));
            AssertRuntimeFilesExist(path);
        }
        finally
        {
            Environment.SetEnvironmentVariable(overrideEnvironmentVariable, originalOverride);
            ViveToolTestRuntimeHelper.DeleteDirectoryBestEffort(pluginsDirectoryPath);
        }
    }

    [Fact]
    public async Task GetBuiltInViveToolPath_ReturnsAppDataViveToolExePath()
    {
        await using var harness = await CreateHarnessAsync();

        var path = harness.Service.GetBuiltInViveToolPath();

        Assert.True(Path.IsPathRooted(path));
        Assert.EndsWith(ViveToolPathService.ViveToolExeName, path, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ViveTool", Path.GetFileName(Path.GetDirectoryName(path)));
        Assert.Contains("AppData", path, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetViveToolPathAsync_WithClearingValue_ClearsPersistedOverrideAndCache(string? filePath)
    {
        await using var harness = await CreateHarnessAsync();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        var setResult = await harness.Service.SetViveToolPathAsync(runtime.ExePath);

        var clearResult = await harness.Service.SetViveToolPathAsync(filePath!);

        Assert.True(setResult);
        Assert.True(clearResult);
        Assert.Null(harness.Service.CachedPath);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithNonexistentFile_ReturnsFalse()
    {
        await using var harness = await CreateHarnessAsync();

        var result = await harness.Service.SetViveToolPathAsync("C:\\nonexistent\\ViVeTool.exe");

        Assert.False(result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithWrongFileName_ReturnsFalse()
    {
        await using var harness = await CreateHarnessAsync();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".exe", "wrong-vivetool-name-");
        await File.WriteAllTextAsync(tempFile.FilePath, "test");

        var result = await harness.Service.SetViveToolPathAsync(tempFile.FilePath);

        Assert.False(result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithIncompleteRuntime_ReturnsFalse()
    {
        await using var harness = await CreateHarnessAsync();
        var directoryPath = Path.Combine(Path.GetTempPath(), $"llt-vivetool-incomplete-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        var exePath = Path.Combine(directoryPath, ViveToolPathService.ViveToolExeName);
        try
        {
            await File.WriteAllTextAsync(exePath, "incomplete runtime");

            var result = await harness.Service.SetViveToolPathAsync(exePath);

            Assert.False(result);
        }
        finally
        {
            ViveToolTestRuntimeHelper.DeleteDirectoryBestEffort(directoryPath);
        }
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithValidRuntime_CachesAndReturnsRuntimePath()
    {
        await using var harness = await CreateHarnessAsync();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();

        var result = await harness.Service.SetViveToolPathAsync(runtime.ExePath);
        var resolvedPath = await harness.Service.GetViveToolPathAsync();

        Assert.True(result);
        Assert.Equal(runtime.ExePath, harness.Service.CachedPath);
        Assert.Equal(runtime.ExePath, resolvedPath);
    }

    [Fact]
    public async Task SetViveToolPathAsync_DoesNotOverwriteExistingCacheOnFailure()
    {
        await using var harness = await CreateHarnessAsync();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        var setResult = await harness.Service.SetViveToolPathAsync(runtime.ExePath);

        var result = await harness.Service.SetViveToolPathAsync("C:\\nonexistent\\ViVeTool.exe");

        Assert.True(setResult);
        Assert.False(result);
        Assert.Equal(runtime.ExePath, harness.Service.CachedPath);
    }

    [Fact]
    public async Task GetViveToolPathAsync_PrefersPersistedUserPathOverBundledPath()
    {
        await using var harness = await CreateHarnessAsync();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        await harness.ClearPersistedOverrideAsync();
        var setResult = await harness.Service.SetViveToolPathAsync(runtime.ExePath);
        await using var reloadedHarness = await CreateHarnessAsync();
        var result = await reloadedHarness.Service.GetViveToolPathAsync();

        Assert.True(setResult);
        Assert.Equal(runtime.ExePath, result);
    }

    [Fact]
    public async Task GetViveToolPathAsync_PrefersTrustedCachedPathOverPersistedUserPath()
    {
        await using var harness = await CreateHarnessAsync();
        await using var storedRuntime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        await using var cachedRuntime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        var setResult = await harness.Service.SetViveToolPathAsync(storedRuntime.ExePath);
        harness.Service.CachedPath = cachedRuntime.ExePath;

        var result = await harness.Service.GetViveToolPathAsync();

        Assert.True(setResult);
        Assert.Equal(cachedRuntime.ExePath, result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_PersistsOverrideForNewServiceInstance()
    {
        await using var harness = await CreateHarnessAsync();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        var setResult = await harness.Service.SetViveToolPathAsync(runtime.ExePath);

        await using var reloadedHarness = await CreateHarnessAsync();
        var result = await reloadedHarness.Service.GetViveToolPathAsync();

        Assert.True(setResult);
        Assert.Equal(runtime.ExePath, result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithInvalidPathCharacters_ReturnsFalse()
    {
        await using var harness = await CreateHarnessAsync();

        var result = await harness.Service.SetViveToolPathAsync("C:\\invalid|path");

        Assert.False(result);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithTraversalPathToValidRuntime_ReturnsFalse()
    {
        await using var harness = await CreateHarnessAsync();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        var parentDirectory = Path.GetDirectoryName(runtime.DirectoryPath);
        Assert.False(string.IsNullOrWhiteSpace(parentDirectory));
        var traversalPath = Path.Combine(
            parentDirectory!,
            "..",
            Path.GetFileName(parentDirectory)!,
            Path.GetFileName(runtime.DirectoryPath),
            ViveToolPathService.ViveToolExeName);

        var result = await harness.Service.SetViveToolPathAsync(traversalPath);

        Assert.True(File.Exists(Path.GetFullPath(traversalPath)));
        Assert.False(result);
        Assert.Null(harness.Service.CachedPath);
    }

    [Fact]
    public async Task GetViveToolPathAsync_WithTraversalCachedPath_FallsBackToBundledPath()
    {
        await using var harness = await CreateHarnessAsync();
        await harness.ClearPersistedOverrideAsync();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();
        var parentDirectory = Path.GetDirectoryName(runtime.DirectoryPath);
        Assert.False(string.IsNullOrWhiteSpace(parentDirectory));
        harness.Service.CachedPath = Path.Combine(
            parentDirectory!,
            "..",
            Path.GetFileName(parentDirectory)!,
            Path.GetFileName(runtime.DirectoryPath),
            ViveToolPathService.ViveToolExeName);

        var result = await harness.Service.GetViveToolPathAsync();

        Assert.Equal(harness.Service.GetBundledViveToolPath(), result);
    }

    [Fact]
    public void ViveToolExeName_IsCorrect()
    {
        Assert.Equal("ViVeTool.exe", ViveToolPathService.ViveToolExeName);
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTempFile_AndProducesValidJson()
    {
        var settings = GetSettings(new ViveToolPathService());
        await settings.LoadAsync();

        // Set a known value and save
        settings.ViveToolPath = "C:\\test\\ViVeTool.exe";
        await settings.SaveAsync();

        // Get the settings file path via reflection (static readonly)
        var settingsFilePath = (string)typeof(UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings.ViveToolSettings)
            .GetField("SettingsFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null)!;

        // Verify no .tmp file left behind
        Assert.False(File.Exists(settingsFilePath + ".tmp"),
            "Temp file should not exist after atomic save completes");

        // Verify settings file is valid JSON
        Assert.True(File.Exists(settingsFilePath));
        var json = await File.ReadAllTextAsync(settingsFilePath);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("ViveToolPath", out var pathProp));
        Assert.Equal("C:\\test\\ViVeTool.exe", pathProp.GetString());

        // Restore original state
        settings.ViveToolPath = null;
        await settings.SaveAsync();
    }

    [Fact]
    public async Task SaveAsync_WhenFileMoveFails_CleansUpTempFile()
    {
        var settings = GetSettings(new ViveToolPathService());
        await settings.LoadAsync();

        // Ensure the settings file exists so we can lock it
        settings.ViveToolPath = "C:\\test\\ViVeTool.exe";
        await settings.SaveAsync();

        var settingsFilePath = (string)typeof(UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings.ViveToolSettings)
            .GetField("SettingsFilePath", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

        // Lock the target file so File.Move(overwrite: true) throws
        using (var lockStream = new FileStream(settingsFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            try
            {
                // This should fail at File.Move because the target is locked
                settings.ViveToolPath = "C:\\test2\\ViVeTool.exe";
                await settings.SaveAsync();
            }
            catch
            {
                // SaveAsync swallows exceptions internally — if it throws, catch it
            }
        }

        // The .tmp file must be cleaned up even though File.Move failed
        Assert.False(File.Exists(settingsFilePath + ".tmp"),
            "Temp file should not exist after SaveAsync with locked target file");

        // Restore original state
        settings.ViveToolPath = null;
        await settings.SaveAsync();
    }

    private static UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings.ViveToolSettings GetSettings(ViveToolPathService service)
    {
        return (UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings.ViveToolSettings)SettingsField.GetValue(service)!;
    }

    private static async Task<ViveToolPathServiceHarness> CreateHarnessAsync()
    {
        var service = new ViveToolPathService();
        var settings = GetSettings(service);
        await settings.LoadAsync().ConfigureAwait(false);
        return new ViveToolPathServiceHarness(service, settings, settings.ViveToolPath);
    }

    private static void AssertRuntimeFilesExist(string viveToolPath)
    {
        var runtimeDirectory = Path.GetDirectoryName(viveToolPath);
        Assert.NotNull(runtimeDirectory);

        foreach (var requiredFileName in RequiredRuntimeFileNames)
        {
            Assert.True(File.Exists(Path.Combine(runtimeDirectory!, requiredFileName)), $"Missing runtime file: {requiredFileName}");
        }
    }

    private static readonly string[] RequiredRuntimeFileNames =
    [
        ViveToolPathService.ViveToolExeName,
        "Albacore.ViVe.dll",
        "Newtonsoft.Json.dll",
        "FeatureDictionary.pfs"
    ];

    private sealed class ViveToolPathServiceHarness(
        ViveToolPathService service,
        UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings.ViveToolSettings settings,
        string? originalStoredPath) : IDisposable, IAsyncDisposable
    {
        public ViveToolPathService Service { get; } = service;

        public async Task ClearPersistedOverrideAsync()
        {
            settings.ViveToolPath = null;
            await settings.SaveAsync().ConfigureAwait(false);
            Service.CachedPath = null;
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            Service.CachedPath = null;
            settings.ViveToolPath = originalStoredPath;
            await settings.SaveAsync().ConfigureAwait(false);
        }
    }
}
