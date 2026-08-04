using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

public class ViveToolServiceTests
{
    private static readonly FieldInfo PathServiceField = typeof(ViveToolService)
        .GetField("_pathService", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo FeatureServiceField = typeof(ViveToolService)
        .GetField("_featureService", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo SettingsField = typeof(ViveToolPathService)
        .GetField("_settings", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo CachedFeaturesField = typeof(ViveToolFeatureService)
        .GetField("_cachedFeatures", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo CachedFeaturesTimestampField = typeof(ViveToolFeatureService)
        .GetField("_cachedFeaturesTimestamp", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [Fact]
    public async Task IsViveToolAvailableAsync_WithConfiguredRuntime_ReturnsTrue()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100");

        var result = await harness.Service.IsViveToolAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task GetViveToolPathAsync_WithConfiguredRuntime_ReturnsConfiguredPath()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100");

        var result = await harness.Service.GetViveToolPathAsync();

        Assert.Equal(harness.RuntimePath, result);
    }

    [Fact]
    public async Task GetViveToolPathAsync_CalledMultipleTimes_ReturnsSameConfiguredPath()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100");

        var result1 = await harness.Service.GetViveToolPathAsync();
        var result2 = await harness.Service.GetViveToolPathAsync();

        Assert.Equal(harness.RuntimePath, result1);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithValidRuntime_ReturnsTrueAndOverridesResolvedPath()
    {
        await using var harness = await CreateServiceAsync();
        await using var runtimeScope = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();

        var setResult = await harness.Service.SetViveToolPathAsync(runtimeScope.ExePath);
        var resolvedPath = await harness.Service.GetViveToolPathAsync();

        Assert.True(setResult);
        Assert.Equal(runtimeScope.ExePath, resolvedPath);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SetViveToolPathAsync_WithClearingValue_RemovesConfiguredOverride(string? clearedValue)
    {
        await using var harness = await CreateServiceAsync();
        await using var runtimeScope = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();

        var setResult = await harness.Service.SetViveToolPathAsync(runtimeScope.ExePath);
        var clearedResult = await harness.Service.SetViveToolPathAsync(clearedValue!);

        Assert.True(setResult);
        Assert.True(clearedResult);
        Assert.Null(GetPathService(harness.Service).CachedPath);
    }

    [Fact]
    public async Task SetViveToolPathAsync_WithNonexistentPath_ReturnsFalse()
    {
        await using var harness = await CreateServiceAsync();

        var result = await harness.Service.SetViveToolPathAsync("C:\\nonexistent\\vivetool.exe");

        Assert.False(result);
    }

    [Fact]
    public async Task EnableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.EnableFeatureAsync(12345);

        Assert.False(result);
    }

    [Fact]
    public async Task DisableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.DisableFeatureAsync(12345);

        Assert.False(result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetFeatureStatusAsync_WithNonPositiveId_ReturnsNull(int featureId)
    {
        await using var harness = await CreateServiceAsync();

        var result = await harness.Service.GetFeatureStatusAsync(featureId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFeatureStatusAsync_WithNonExecutableRuntime_ReturnsNull()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.GetFeatureStatusAsync(12345);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListFeaturesAsync_WithConfiguredDictionary_ReturnsDictionaryFeatures()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var result = await harness.Service.ListFeaturesAsync();

        Assert.Collection(
            result.OrderBy(feature => feature.Id),
            feature =>
            {
                Assert.Equal(100, feature.Id);
                Assert.Equal("AlphaFeature", feature.Name);
                Assert.Equal(FeatureFlagStatus.Default, feature.Status);
            },
            feature =>
            {
                Assert.Equal(200, feature.Id);
                Assert.Equal("BetaFeature", feature.Name);
                Assert.Equal(FeatureFlagStatus.Default, feature.Status);
            });
    }

    [Fact]
    public async Task SearchFeaturesAsync_WithEmptyKeyword_ReturnsAllConfiguredFeatures()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var result = await harness.Service.SearchFeaturesAsync(string.Empty);

        Assert.Equal([100, 200], result.OrderBy(feature => feature.Id).Select(feature => feature.Id));
    }

    [Fact]
    public async Task SearchFeaturesAsync_WithMatchingKeyword_FiltersConfiguredFeatures()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var result = await harness.Service.SearchFeaturesAsync("beta");

        var feature = Assert.Single(result);
        Assert.Equal(200, feature.Id);
        Assert.Equal("BetaFeature", feature.Name);
    }

    [Fact]
    public async Task SearchFeaturesAsync_WithUnknownKeyword_ReturnsEmptyList()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var result = await harness.Service.SearchFeaturesAsync("missing-feature");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithJsonFile_ReturnsParsedFeatures()
    {
        await using var harness = await CreateServiceAsync();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json", "vivetool-import-");
        const string jsonContent = """
            [
              { "id": 12345, "name": "Windows Feature", "description": "Enable new Windows feature" },
              { "id": 67890, "name": "Test Feature", "description": "Another test feature" }
            ]
            """;
        await File.WriteAllTextAsync(tempFile.FilePath, jsonContent);

        var result = await harness.Service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Collection(
            result,
            feature =>
            {
                Assert.Equal(12345, feature.Id);
                Assert.Equal("Windows Feature", feature.Name);
                Assert.Equal("Enable new Windows feature", feature.Description);
                Assert.Equal(FeatureFlagStatus.Unknown, feature.Status);
            },
            feature =>
            {
                Assert.Equal(67890, feature.Id);
                Assert.Equal("Test Feature", feature.Name);
                Assert.Equal("Another test feature", feature.Description);
                Assert.Equal(FeatureFlagStatus.Unknown, feature.Status);
            });
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithMissingFile_ReturnsEmptyList()
    {
        await using var harness = await CreateServiceAsync();

        var result = await harness.Service.ImportFeaturesFromFileAsync("C:\\nonexistent\\file.txt");

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("http://localhost/features.json")]
    public async Task ImportFeaturesFromUrlAsync_WithRejectedUrl_ReturnsEmptyList(string url)
    {
        await using var harness = await CreateServiceAsync();

        var result = await harness.Service.ImportFeaturesFromUrlAsync(url);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DownloadViveToolAsync_WithBundledRuntimeAvailable_ReturnsTrueWithoutReportingProgress()
    {
        await using var harness = await CreateServiceAsync();
        long? lastProgress = null;
        var progress = new Progress<long>(bytes => lastProgress = bytes);

        var result = await harness.Service.DownloadViveToolAsync(progress);

        Assert.True(result);
        Assert.Null(lastProgress);
    }

    [Fact]
    public async Task ClearFeatureCache_ClearsUnderlyingFeatureServiceCache()
    {
        await using var harness = await CreateServiceAsync();
        SeedFeatureCache(
            harness.Service,
            CreateFeature(100, "AlphaFeature", string.Empty));

        harness.Service.ClearFeatureCache();

        var featureService = GetFeatureService(harness.Service);
        Assert.Null(CachedFeaturesField.GetValue(featureService));
        Assert.Equal(DateTime.MinValue, (DateTime)CachedFeaturesTimestampField.GetValue(featureService)!);
    }

    [Fact]
    public async Task ClearFeatureCache_CalledMultipleTimes_KeepsUnderlyingCacheCleared()
    {
        await using var harness = await CreateServiceAsync();
        SeedFeatureCache(
            harness.Service,
            CreateFeature(100, "AlphaFeature", string.Empty),
            CreateFeature(200, "BetaFeature", string.Empty));

        harness.Service.ClearFeatureCache();
        harness.Service.ClearFeatureCache();
        harness.Service.ClearFeatureCache();

        var featureService = GetFeatureService(harness.Service);
        Assert.Null(CachedFeaturesField.GetValue(featureService));
        Assert.Equal(DateTime.MinValue, (DateTime)CachedFeaturesTimestampField.GetValue(featureService)!);
    }

    [Fact]
    public async Task GetViveToolVersionAsync_WithCommandBackedRuntime_ReturnsNullWhenNoVersionCanBeParsed()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100");

        var result = await harness.Service.GetViveToolVersionAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ExportFeaturesToFileAsync_WithValidPath_DelegatesToDownloadService()
    {
        await using var harness = await CreateServiceAsync();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json", "vivetool-service-export-");
        FeatureFlagInfo[] features =
        [
            CreateFeature(100, "AlphaFeature", "First", FeatureFlagStatus.Enabled),
            CreateFeature(200, "BetaFeature", "Second", FeatureFlagStatus.Default)
        ];

        var result = await harness.Service.ExportFeaturesToFileAsync(tempFile.FilePath, features);
        var content = await File.ReadAllTextAsync(tempFile.FilePath);

        Assert.True(result);
        Assert.Contains(@"""name"": ""AlphaFeature""", content);
        Assert.Contains(@"""status"": ""Enabled""", content);
    }

    [Fact]
    public async Task MultipleOperations_ConcurrentCalls_ReturnConsistentConfiguredResults()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(async () => new
            {
                Available = await harness.Service.IsViveToolAvailableAsync(),
                Path = await harness.Service.GetViveToolPathAsync(),
                Features = await harness.Service.ListFeaturesAsync()
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result =>
        {
            Assert.True(result.Available);
            Assert.Equal(harness.RuntimePath, result.Path);
            Assert.Equal([100, 200], result.Features.OrderBy(feature => feature.Id).Select(feature => feature.Id));
        });
    }

    [Fact]
    public async Task FullWorkflow_ClearCacheThenListFeatures_ReloadsUpdatedDictionary()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var initial = await harness.Service.ListFeaturesAsync();
        await File.WriteAllLinesAsync(harness.DictionaryPath!, ["GammaFeature,300"]);
        harness.Service.ClearFeatureCache();
        var reloaded = await harness.Service.ListFeaturesAsync();

        Assert.Equal([100, 200], initial.OrderBy(feature => feature.Id).Select(feature => feature.Id));
        var feature = Assert.Single(reloaded);
        Assert.Equal(300, feature.Id);
        Assert.Equal("GammaFeature", feature.Name);
    }

    [Fact]
    public async Task FullWorkflow_CheckAvailabilityThenGetPath_ReturnsConsistentConfiguredRuntime()
    {
        await using var harness = await CreateCommandBackedServiceAsync("AlphaFeature,100");

        var available = await harness.Service.IsViveToolAvailableAsync();
        var path = await harness.Service.GetViveToolPathAsync();

        Assert.True(available);
        Assert.Equal(harness.RuntimePath, path);
    }

    private static FeatureFlagInfo CreateFeature(
        int id,
        string name,
        string description,
        FeatureFlagStatus status = FeatureFlagStatus.Unknown)
    {
        return new FeatureFlagInfo
        {
            Id = id,
            Name = name,
            Description = description,
            Status = status
        };
    }

    private static ViveToolPathService GetPathService(ViveToolService service)
    {
        return (ViveToolPathService)PathServiceField.GetValue(service)!;
    }

    private static ViveToolFeatureService GetFeatureService(ViveToolService service)
    {
        return (ViveToolFeatureService)FeatureServiceField.GetValue(service)!;
    }

    private static UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings.ViveToolSettings GetSettings(ViveToolPathService pathService)
    {
        return (UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings.ViveToolSettings)SettingsField.GetValue(pathService)!;
    }

    private static void SeedFeatureCache(ViveToolService service, params FeatureFlagInfo[] features)
    {
        var featureService = GetFeatureService(service);
        CachedFeaturesField.SetValue(featureService, features.ToList());
        CachedFeaturesTimestampField.SetValue(featureService, DateTime.UtcNow);
    }

    private static async Task<ViveToolServiceHarness> CreateServiceAsync()
    {
        var service = new ViveToolService();
        var pathService = GetPathService(service);
        var settings = GetSettings(pathService);
        await settings.LoadAsync().ConfigureAwait(false);
        return new ViveToolServiceHarness(service, pathService, settings, settings.ViveToolPath, null);
    }

    private static async Task<ViveToolServiceHarness> CreateCommandBackedServiceAsync(params string[] dictionaryLines)
    {
        var runtimeScope = await ViveToolTestRuntimeHelper.CreateCommandBackedRuntimeScopeAsync(dictionaryLines).ConfigureAwait(false);
        return await CreateServiceWithRuntimeAsync(runtimeScope).ConfigureAwait(false);
    }

    private static async Task<ViveToolServiceHarness> CreateNonExecutableServiceAsync()
    {
        var runtimeScope = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync().ConfigureAwait(false);
        return await CreateServiceWithRuntimeAsync(runtimeScope).ConfigureAwait(false);
    }

    private static async Task<ViveToolServiceHarness> CreateServiceWithRuntimeAsync(ViveToolTestRuntimeScope runtimeScope)
    {
        var service = new ViveToolService();
        var pathService = GetPathService(service);
        var settings = GetSettings(pathService);
        await settings.LoadAsync().ConfigureAwait(false);
        var originalStoredPath = settings.ViveToolPath;

        try
        {
            var setResult = await service.SetViveToolPathAsync(runtimeScope.ExePath).ConfigureAwait(false);
            Assert.True(setResult);
            return new ViveToolServiceHarness(service, pathService, settings, originalStoredPath, runtimeScope);
        }
        catch
        {
            await runtimeScope.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class ViveToolServiceHarness(
        ViveToolService service,
        ViveToolPathService pathService,
        UniversalDeviceToolkit.Plugins.ViveTool.Services.Settings.ViveToolSettings settings,
        string? originalStoredPath,
        ViveToolTestRuntimeScope? runtimeScope) : IDisposable, IAsyncDisposable
    {
        public ViveToolService Service { get; } = service;

        public string? RuntimePath { get; } = runtimeScope?.ExePath;

        public string? DictionaryPath { get; } = runtimeScope is null
            ? null
            : Path.Combine(runtimeScope.DirectoryPath, "FeatureDictionary.pfs");

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            pathService.CachedPath = null;
            settings.ViveToolPath = originalStoredPath;
            await settings.SaveAsync().ConfigureAwait(false);

            if (runtimeScope is not null)
            {
                await runtimeScope.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
