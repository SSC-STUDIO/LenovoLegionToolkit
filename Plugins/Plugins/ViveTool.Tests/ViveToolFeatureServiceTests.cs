using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// Tests for ViveToolFeatureService - feature flag operations.
/// </summary>
public class ViveToolFeatureServiceTests
{
    private static readonly MethodInfo ParseFeatureDictionaryLinesMethod = typeof(ViveToolFeatureService)
        .GetMethod("ParseFeatureDictionaryLines", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ApplyConfiguredStatusesMethod = typeof(ViveToolFeatureService)
        .GetMethod("ApplyConfiguredStatuses", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ParseVersionFromOutputMethod = typeof(ViveToolFeatureService)
        .GetMethod("ParseVersionFromOutput", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo ParseFeatureListMethod = typeof(ViveToolFeatureService)
        .GetMethod("ParseFeatureList", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo ParseStatusFromLineMethod = typeof(ViveToolFeatureService)
        .GetMethod("ParseStatusFromLine", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo ParseStatusFromStringMethod = typeof(ViveToolFeatureService)
        .GetMethod("ParseStatusFromString", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo DefaultCacheDurationField = typeof(ViveToolFeatureService)
        .GetField("DefaultCacheDuration", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly FieldInfo CachedFeaturesField = typeof(ViveToolFeatureService)
        .GetField("_cachedFeatures", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo CachedFeaturesTimestampField = typeof(ViveToolFeatureService)
        .GetField("_cachedFeaturesTimestamp", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private ViveToolFeatureService CreateService(
        ViveToolPathService? pathService = null,
        ViveToolProcessService? processService = null)
    {
        pathService ??= new ViveToolPathService();
        processService ??= new ViveToolProcessService();
        return new ViveToolFeatureService(pathService, processService);
    }

    private static async Task<ViveToolFeatureServiceHarness> CreateDictionaryBackedServiceAsync(params string[] dictionaryLines)
    {
        var runtimeScope = await ViveToolTestRuntimeHelper.CreateCommandBackedRuntimeScopeAsync(dictionaryLines).ConfigureAwait(false);
        return await CreateHarnessAsync(runtimeScope).ConfigureAwait(false);
    }

    private static async Task<ViveToolFeatureServiceHarness> CreateNonExecutableServiceAsync()
    {
        var runtimeScope = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync().ConfigureAwait(false);
        return await CreateHarnessAsync(runtimeScope).ConfigureAwait(false);
    }

    private static async Task<ViveToolFeatureServiceHarness> CreateHarnessAsync(ViveToolTestRuntimeScope runtimeScope)
    {
        var pathService = new ViveToolPathService();
        var setResult = await pathService.SetViveToolPathAsync(runtimeScope.ExePath).ConfigureAwait(false);
        Assert.True(setResult);

        return new ViveToolFeatureServiceHarness(
            runtimeScope,
            pathService,
            new ViveToolFeatureService(pathService, new ViveToolProcessService()));
    }

    private ViveToolFeatureService CreateServiceWithCachedFeatures(params FeatureFlagInfo[] features)
    {
        var service = CreateService();
        SetCachedFeatures(service, features);
        return service;
    }

    private static void SetCachedFeatures(ViveToolFeatureService service, params FeatureFlagInfo[] features)
    {
        CachedFeaturesField.SetValue(service, features.ToList());
        CachedFeaturesTimestampField.SetValue(service, DateTime.UtcNow);
    }

    private static string? InvokeParseVersionFromOutput(string output)
    {
        return (string?)ParseVersionFromOutputMethod.Invoke(
            new ViveToolFeatureService(new ViveToolPathService(), new ViveToolProcessService()),
            new object?[] { output });
    }

    private static List<FeatureFlagInfo> InvokeParseFeatureList(string? output)
    {
        return (List<FeatureFlagInfo>)ParseFeatureListMethod.Invoke(
            new ViveToolFeatureService(new ViveToolPathService(), new ViveToolProcessService()),
            new object?[] { output! })!;
    }

    private static FeatureFlagStatus InvokeParseStatusFromLine(string line)
    {
        return (FeatureFlagStatus)ParseStatusFromLineMethod.Invoke(
            new ViveToolFeatureService(new ViveToolPathService(), new ViveToolProcessService()),
            new object[] { line })!;
    }

    private static FeatureFlagStatus InvokeParseStatusFromString(string status)
    {
        return (FeatureFlagStatus)ParseStatusFromStringMethod.Invoke(
            new ViveToolFeatureService(new ViveToolPathService(), new ViveToolProcessService()),
            new object[] { status })!;
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

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var pathService = new ViveToolPathService();
        var processService = new ViveToolProcessService();

        var service = new ViveToolFeatureService(pathService, processService);

        Assert.NotNull(service);
    }

    #endregion

    #region ClearFeatureCache Tests

    [Fact]
    public void ClearFeatureCache_DoesNotThrow()
    {
        var service = CreateServiceWithCachedFeatures(CreateFeature(100, "Alpha", string.Empty));

        service.ClearFeatureCache();

        Assert.Null(CachedFeaturesField.GetValue(service));
        Assert.Equal(DateTime.MinValue, (DateTime)CachedFeaturesTimestampField.GetValue(service)!);
    }

    [Fact]
    public void ClearFeatureCache_CalledMultipleTimes_DoesNotThrow()
    {
        var service = CreateServiceWithCachedFeatures(CreateFeature(100, "Alpha", string.Empty));

        service.ClearFeatureCache();
        service.ClearFeatureCache();
        service.ClearFeatureCache();

        Assert.Null(CachedFeaturesField.GetValue(service));
        Assert.Equal(DateTime.MinValue, (DateTime)CachedFeaturesTimestampField.GetValue(service)!);
    }

    #endregion

    #region EnableFeatureAsync Tests

    [Fact]
    public async Task EnableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse_WhenNoUsableRuntimeIsAvailable()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.EnableFeatureAsync(12345);

        Assert.False(result);
    }

    [Fact]
    public async Task EnableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse_ForNegativeId()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.EnableFeatureAsync(-1);

        Assert.False(result);
    }

    [Fact]
    public async Task EnableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse_ForZeroId()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.EnableFeatureAsync(0);

        Assert.False(result);
    }

    [Fact]
    public async Task EnableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.EnableFeatureAsync(int.MaxValue);

        Assert.False(result);
    }

    [Fact]
    public async Task EnableFeatureAsync_WithNonExecutableRuntime_PreservesCache()
    {
        await using var harness = await CreateNonExecutableServiceAsync();
        var service = harness.Service;

        SetCachedFeatures(service, CreateFeature(100, "CachedAlpha", string.Empty));

        await service.EnableFeatureAsync(12345);

        Assert.NotNull(CachedFeaturesField.GetValue(service));
    }

    #endregion

    #region DisableFeatureAsync Tests

    [Fact]
    public async Task DisableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse_WhenNoUsableRuntimeIsAvailable()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.DisableFeatureAsync(12345);

        Assert.False(result);
    }

    [Fact]
    public async Task DisableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse_ForNegativeId()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.DisableFeatureAsync(-1);

        Assert.False(result);
    }

    [Fact]
    public async Task DisableFeatureAsync_WithNonExecutableRuntime_ReturnsFalse_ForZeroId()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.DisableFeatureAsync(0);

        Assert.False(result);
    }

    [Fact]
    public async Task DisableFeatureAsync_WithNonExecutableRuntime_PreservesCache()
    {
        await using var harness = await CreateNonExecutableServiceAsync();
        var service = harness.Service;

        SetCachedFeatures(service, CreateFeature(100, "CachedAlpha", string.Empty));

        await service.DisableFeatureAsync(12345);

        Assert.NotNull(CachedFeaturesField.GetValue(service));
    }

    #endregion

    #region GetFeatureStatusAsync Tests

    [Fact]
    public async Task GetFeatureStatusAsync_WithNonExecutableRuntime_ReturnsNull()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.GetFeatureStatusAsync(12345);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFeatureStatusAsync_WithNegativeId_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.GetFeatureStatusAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFeatureStatusAsync_WithZeroId_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.GetFeatureStatusAsync(0);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Feature state is Enabled", FeatureFlagStatus.Enabled)]
    [InlineData("Feature state is Disabled", FeatureFlagStatus.Disabled)]
    [InlineData("Feature state is Default", FeatureFlagStatus.Default)]
    [InlineData("Feature state is Unknown", FeatureFlagStatus.Unknown)]
    public void ParseStatusFromLine_RecognizesSupportedStates(string line, FeatureFlagStatus expectedStatus)
    {
        Assert.Equal(expectedStatus, InvokeParseStatusFromLine(line));
    }

    [Theory]
    [InlineData("Enabled", FeatureFlagStatus.Enabled)]
    [InlineData("Disabled", FeatureFlagStatus.Disabled)]
    [InlineData("Default", FeatureFlagStatus.Default)]
    [InlineData("Other", FeatureFlagStatus.Unknown)]
    public void ParseStatusFromString_RecognizesSupportedStates(string status, FeatureFlagStatus expectedStatus)
    {
        Assert.Equal(expectedStatus, InvokeParseStatusFromString(status));
    }

    #endregion

    #region ListFeaturesAsync Tests

    [Fact]
    public async Task ListFeaturesAsync_WithNonExecutableRuntime_ReturnsEmptyList()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.ListFeaturesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListFeaturesAsync_ReturnsListInstance()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var result = await harness.Service.ListFeaturesAsync();

        Assert.NotNull(result);
        Assert.IsType<List<FeatureFlagInfo>>(result);
        Assert.Collection(
            result.OrderBy(feature => feature.Id),
            feature => Assert.Equal(100, feature.Id),
            feature => Assert.Equal(200, feature.Id));
    }

    [Fact]
    public async Task ListFeaturesAsync_CachesResult()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var result1 = await harness.Service.ListFeaturesAsync();
        await File.WriteAllLinesAsync(harness.DictionaryPath, ["GammaFeature,300"]);
        var result2 = await harness.Service.ListFeaturesAsync();

        Assert.Equal(result1.Select(feature => feature.Id), result2.Select(feature => feature.Id));
        Assert.DoesNotContain(result2, feature => feature.Id == 300);
    }

    [Fact]
    public async Task ListFeaturesAsync_ClearCacheForcesReload()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");
        var service = harness.Service;

        var initial = await service.ListFeaturesAsync();
        await File.WriteAllLinesAsync(harness.DictionaryPath, ["GammaFeature,300"]);
        service.ClearFeatureCache();
        var reloaded = await service.ListFeaturesAsync();

        Assert.Equal(2, initial.Count);
        Assert.Single(reloaded);
        Assert.Equal(300, reloaded[0].Id);
    }

    [Fact]
    public async Task ListFeaturesAsync_CalledMultipleTimes_ReturnsConsistentResults()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var result1 = await harness.Service.ListFeaturesAsync();
        var result2 = await harness.Service.ListFeaturesAsync();
        var result3 = await harness.Service.ListFeaturesAsync();

        Assert.Equal(result1.Select(feature => feature.Id), result2.Select(feature => feature.Id));
        Assert.Equal(result2.Select(feature => feature.Id), result3.Select(feature => feature.Id));
    }

    [Fact]
    public async Task ListFeaturesAsync_ReturnsImmutableCopy()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var result1 = await harness.Service.ListFeaturesAsync();
        var result2 = await harness.Service.ListFeaturesAsync();

        Assert.NotSame(result1, result2);
        Assert.Equal(result1.Select(feature => feature.Id), result2.Select(feature => feature.Id));
    }

    [Fact]
    public void ParseFeatureDictionaryLines_ReturnsFullFeatureDictionary()
    {
        var features = InvokeParseFeatureDictionaryLines(new[]
        {
            "AlphaFeature,100",
            "BetaFeature,200"
        });

        Assert.Collection(
            features,
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
    public void ParseFeatureDictionaryLines_SkipsInvalidAndDuplicateRows()
    {
        var features = InvokeParseFeatureDictionaryLines(new[]
        {
            "AlphaFeature,100",
            "BrokenRow",
            "DuplicateAlpha,100",
            " ,200",
            "ZeroFeature,0"
        });

        Assert.Equal(2, features.Count);
        Assert.Equal("AlphaFeature", features[0].Name);
        Assert.Equal("Feature 200", features[1].Name);
    }

    [Fact]
    public void ApplyConfiguredStatuses_OverlaysConfiguredSubsetOnFullDictionary()
    {
        var dictionaryFeatures = new List<FeatureFlagInfo>
        {
            new() { Id = 100, Name = "AlphaFeature", Status = FeatureFlagStatus.Default },
            new() { Id = 200, Name = "BetaFeature", Status = FeatureFlagStatus.Default }
        };
        var configuredFeatures = new List<FeatureFlagInfo>
        {
            new() { Id = 200, Name = "BetterBetaName", Status = FeatureFlagStatus.Enabled }
        };

        ApplyConfiguredStatusesMethod.Invoke(null, new object?[] { dictionaryFeatures, configuredFeatures });

        Assert.Equal(FeatureFlagStatus.Default, dictionaryFeatures[0].Status);
        Assert.Equal(FeatureFlagStatus.Enabled, dictionaryFeatures[1].Status);
        Assert.Equal("BetterBetaName", dictionaryFeatures[1].Name);
    }

    [Fact]
    public void ApplyConfiguredStatuses_WithDuplicateConfiguredIds_UsesLastStatus()
    {
        var dictionaryFeatures = new List<FeatureFlagInfo>
        {
            new() { Id = 100, Name = "AlphaFeature", Status = FeatureFlagStatus.Default }
        };
        var configuredFeatures = new List<FeatureFlagInfo>
        {
            new() { Id = 100, Name = "AlphaEnabled", Status = FeatureFlagStatus.Enabled },
            new() { Id = 100, Name = "AlphaDisabled", Status = FeatureFlagStatus.Disabled }
        };

        ApplyConfiguredStatusesMethod.Invoke(null, new object?[] { dictionaryFeatures, configuredFeatures });

        Assert.Equal(FeatureFlagStatus.Disabled, dictionaryFeatures[0].Status);
        Assert.Equal("AlphaDisabled", dictionaryFeatures[0].Name);
    }

    #endregion

    #region SearchFeaturesAsync Tests

    [Fact]
    public async Task SearchFeaturesAsync_WithEmptyKeyword_ReturnsAllFeatures()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", "Enables alpha mode"),
            CreateFeature(200, "BetaFeature", "Enables beta mode"));

        var searchResult = await service.SearchFeaturesAsync("");

        Assert.Equal([100, 200], searchResult.Select(feature => feature.Id));
    }

    [Fact]
    public async Task SearchFeaturesAsync_WithWhitespace_ReturnsAllFeatures()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", "Enables alpha mode"),
            CreateFeature(200, "BetaFeature", "Enables beta mode"));

        var searchResult = await service.SearchFeaturesAsync("   ");

        Assert.Equal([100, 200], searchResult.Select(feature => feature.Id));
    }

    [Fact]
    public async Task SearchFeaturesAsync_WithNull_ReturnsAllFeatures()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", "Enables alpha mode"),
            CreateFeature(200, "BetaFeature", "Enables beta mode"));

        var searchResult = await service.SearchFeaturesAsync(null!);

        Assert.Equal([100, 200], searchResult.Select(feature => feature.Id));
    }

    [Fact]
    public async Task SearchFeaturesAsync_SearchesById()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(12345, "AlphaFeature", "Enables alpha mode"),
            CreateFeature(200, "BetaFeature", "Enables beta mode"));

        var result = await service.SearchFeaturesAsync("12345");

        Assert.Single(result);
        Assert.Equal(12345, result[0].Id);
    }

    [Fact]
    public async Task SearchFeaturesAsync_SearchesByName()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", "Enables alpha mode"),
            CreateFeature(200, "GammaToggle", "Enables beta mode"));

        var result = await service.SearchFeaturesAsync("feature");

        Assert.Single(result);
        Assert.Equal(100, result[0].Id);
    }

    [Fact]
    public async Task SearchFeaturesAsync_SearchesByDescription()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", "Enables alpha mode"),
            CreateFeature(200, "GammaToggle", "Disables gamma mode"));

        var result = await service.SearchFeaturesAsync("disables");

        Assert.Single(result);
        Assert.Equal(200, result[0].Id);
    }

    [Fact]
    public async Task SearchFeaturesAsync_IsCaseInsensitive()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", "Enables alpha mode"),
            CreateFeature(200, "GammaToggle", "Disables gamma mode"));

        var result1 = await service.SearchFeaturesAsync("FEATURE");
        var result2 = await service.SearchFeaturesAsync("feature");

        Assert.Equal(result1.Select(feature => feature.Id), result2.Select(feature => feature.Id));
    }

    [Fact]
    public async Task SearchFeaturesAsync_WithNoMatches_ReturnsEmptyList()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", "Enables alpha mode"),
            CreateFeature(200, "GammaToggle", "Disables gamma mode"));

        var result = await service.SearchFeaturesAsync("xyznonexistent123456789");

        Assert.Empty(result);
    }

    #endregion

    #region GetViveToolVersionAsync Tests

    [Fact]
    public async Task GetViveToolVersionAsync_WithNonExecutableRuntime_ReturnsNull()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.GetViveToolVersionAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetViveToolVersionAsync_WithCommandBackedRuntime_ReturnsNullWhenVersionCannotBeParsed()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100");

        var result = await harness.Service.GetViveToolVersionAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetViveToolVersionAsync_CalledMultipleTimes_ReturnsConsistentResults()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100");

        var result1 = await harness.Service.GetViveToolVersionAsync();
        var result2 = await harness.Service.GetViveToolVersionAsync();

        Assert.Equal(result1, result2);
    }

    [Fact]
    public async Task GetViveToolVersionAsync_WithCommandBackedRuntime_RemainsNullAcrossCalls()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100");

        var result = await harness.Service.GetViveToolVersionAsync();

        Assert.Null(result);
    }

    #endregion

    #region Version Parsing Tests

    [Theory]
    [InlineData("ViVeTool v0.3.4", "0.3.4")]
    [InlineData("v0.3.4", "0.3.4")]
    [InlineData("Version: 0.3.4", "0.3.4")]
    [InlineData("0.3.4", "0.3.4")]
    [InlineData("v0.3", "0.3")]
    [InlineData("Version: 0.3", "0.3")]
    [InlineData("0.3", "0.3")]
    public void VersionParsing_HandlesVariousFormats(string input, string expectedVersion)
    {
        var version = InvokeParseVersionFromOutput(input);

        Assert.Equal(expectedVersion, version);
    }

    [Fact]
    public void VersionParsing_IgnoresWindowsCommandProcessorBanner()
    {
        const string input = """
Microsoft Windows [Version 10.0.26100.0]
(c) Microsoft Corporation. All rights reserved.
""";

        var version = InvokeParseVersionFromOutput(input);

        Assert.Null(version);
    }

    #endregion

    #region Feature List Parsing Tests

    [Fact]
    public void ParseFeatureList_WithEmpty_ReturnsEmptyList()
    {
        var result = InvokeParseFeatureList(string.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseFeatureList_WithNullOutput_ReturnsEmptyList()
    {
        var result = InvokeParseFeatureList(null);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseFeatureList_WithWhitespace_ReturnsEmptyList()
    {
        var result = InvokeParseFeatureList("   \r\n   ");

        Assert.Empty(result);
    }

    #endregion

    #region Cache Duration Tests

    [Fact]
    public void DefaultCacheDuration_IsFiveMinutes()
    {
        var cacheDuration = Assert.IsType<TimeSpan>(DefaultCacheDurationField.GetValue(null));

        Assert.Equal(TimeSpan.FromMinutes(5), cacheDuration);
    }

    #endregion

    #region FeatureFlagInfo Tests

    [Fact]
    public void FeatureFlagInfo_DefaultValues_AreCorrect()
    {
        var info = new FeatureFlagInfo();

        Assert.Equal(0, info.Id);
        Assert.Equal(string.Empty, info.Name);
        Assert.Equal(string.Empty, info.Description);
        Assert.Equal(FeatureFlagStatus.Unknown, info.Status);
    }

    [Fact]
    public void FeatureFlagInfo_CanSetProperties()
    {
        var info = new FeatureFlagInfo
        {
            Id = 12345,
            Name = "Test Feature",
            Description = "Test Description",
            Status = FeatureFlagStatus.Enabled
        };

        Assert.Equal(12345, info.Id);
        Assert.Equal("Test Feature", info.Name);
        Assert.Equal("Test Description", info.Description);
        Assert.Equal(FeatureFlagStatus.Enabled, info.Status);
    }

    #endregion

    #region FeatureFlagStatus Tests

    [Theory]
    [InlineData(FeatureFlagStatus.Enabled)]
    [InlineData(FeatureFlagStatus.Disabled)]
    [InlineData(FeatureFlagStatus.Default)]
    [InlineData(FeatureFlagStatus.Unknown)]
    public void FeatureFlagStatus_AllValuesAreDefined(FeatureFlagStatus status)
    {
        Assert.True(Enum.IsDefined(typeof(FeatureFlagStatus), status));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task EnableFeatureAsync_HandlesException()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.EnableFeatureAsync(int.MaxValue);

        Assert.False(result);
    }

    [Fact]
    public async Task DisableFeatureAsync_HandlesException()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.DisableFeatureAsync(int.MaxValue);

        Assert.False(result);
    }

    [Fact]
    public async Task GetFeatureStatusAsync_HandlesException()
    {
        await using var harness = await CreateNonExecutableServiceAsync();

        var result = await harness.Service.GetFeatureStatusAsync(int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListFeaturesAsync_HandlesException()
    {
        await using var harness = await CreateNonExecutableServiceAsync();
        var service = harness.Service;

        for (int i = 0; i < 3; i++)
        {
            var result = await service.ListFeaturesAsync();
            Assert.Empty(result);
        }
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ListFeaturesAsync_ConcurrentCalls_DoesNotCrash()
    {
        await using var harness = await CreateDictionaryBackedServiceAsync("AlphaFeature,100", "BetaFeature,200");

        var tasks = new Task<List<FeatureFlagInfo>>[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = harness.Service.ListFeaturesAsync();
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.Equal([100, 200], result.Select(feature => feature.Id)));
    }

    [Fact]
    public async Task ClearFeatureCache_ConcurrentCalls_DoesNotCrash()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", string.Empty),
            CreateFeature(200, "BetaFeature", string.Empty));

        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(service.ClearFeatureCache);
        }

        await Task.WhenAll(tasks);

        Assert.Null(CachedFeaturesField.GetValue(service));
        Assert.Equal(DateTime.MinValue, (DateTime)CachedFeaturesTimestampField.GetValue(service)!);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ListFeaturesAsync_CacheHit_IsFast()
    {
        var service = CreateServiceWithCachedFeatures(
            CreateFeature(100, "AlphaFeature", string.Empty),
            CreateFeature(200, "BetaFeature", string.Empty));

        var startTime = DateTime.UtcNow;
        var result = await service.ListFeaturesAsync();
        var elapsed = DateTime.UtcNow - startTime;

        Assert.Equal([100, 200], result.Select(feature => feature.Id));
        Assert.True(elapsed.TotalMilliseconds < 1000);
    }

    #endregion

    private static List<FeatureFlagInfo> InvokeParseFeatureDictionaryLines(IEnumerable<string> lines)
    {
        return (List<FeatureFlagInfo>)ParseFeatureDictionaryLinesMethod.Invoke(null, new object[] { lines })!;
    }

    private sealed class ViveToolFeatureServiceHarness(
        ViveToolTestRuntimeScope runtimeScope,
        ViveToolPathService pathService,
        ViveToolFeatureService service) : IAsyncDisposable
    {
        public ViveToolFeatureService Service { get; } = service;

        public string DictionaryPath { get; } = Path.Combine(runtimeScope.DirectoryPath, "FeatureDictionary.pfs");

        public async ValueTask DisposeAsync()
        {
            await pathService.SetViveToolPathAsync(string.Empty).ConfigureAwait(false);
            await runtimeScope.DisposeAsync().ConfigureAwait(false);
        }
    }
}
