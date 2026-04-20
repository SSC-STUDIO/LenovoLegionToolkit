using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// Tests for ViveToolDownloadService - download and import operations.
/// </summary>
public class ViveToolDownloadServiceTests
{
    [Fact]
    public void Constructor_WithValidPathService_CreatesInstance()
    {
        var (_, service) = CreateService();

        Assert.NotNull(service);
    }

    [Fact]
    public async Task DownloadViveToolAsync_WithBundledRuntimeAvailable_ReturnsTrueAndCachesBundledPath()
    {
        var (pathService, service) = CreateService();

        var result = await service.DownloadViveToolAsync();

        Assert.True(result);
        Assert.Equal(pathService.GetBundledViveToolPath(), pathService.CachedPath);
        AssertBundledRuntimeFilesExist(pathService.CachedPath!);
    }

    [Fact]
    public async Task DownloadViveToolAsync_WithProgressReporter_DoesNotReportWhenBundledRuntimeIsUsed()
    {
        var (_, service) = CreateService();
        long? lastProgress = null;
        var progress = new Progress<long>(bytes => lastProgress = bytes);

        var result = await service.DownloadViveToolAsync(progress);

        Assert.True(result);
        Assert.Null(lastProgress);
    }

    [Fact]
    public async Task DownloadViveToolAsync_CalledMultipleTimes_KeepsBundledPathAndCreatesNoTempArtifacts()
    {
        var (pathService, service) = CreateService();
        var beforeZipFiles = Directory.GetFiles(Path.GetTempPath(), "ViVeTool_*.zip").OrderBy(path => path).ToArray();
        var beforeExtractDirectories = Directory.GetDirectories(Path.GetTempPath(), "ViVeTool_extract_*").OrderBy(path => path).ToArray();

        var result1 = await service.DownloadViveToolAsync();
        var result2 = await service.DownloadViveToolAsync();

        var afterZipFiles = Directory.GetFiles(Path.GetTempPath(), "ViVeTool_*.zip").OrderBy(path => path).ToArray();
        var afterExtractDirectories = Directory.GetDirectories(Path.GetTempPath(), "ViVeTool_extract_*").OrderBy(path => path).ToArray();

        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal(pathService.GetBundledViveToolPath(), pathService.CachedPath);
        Assert.Equal(beforeZipFiles, afterZipFiles);
        Assert.Equal(beforeExtractDirectories, afterExtractDirectories);
    }

    [Fact]
    public void GetBuiltInViveToolPath_TargetsAppDataRuntimeLocation()
    {
        var (pathService, _) = CreateService();

        var builtInPath = pathService.GetBuiltInViveToolPath();

        Assert.EndsWith(ViveToolPathService.ViveToolExeName, builtInPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ViveTool", Path.GetFileName(Path.GetDirectoryName(builtInPath)));
        Assert.Contains("AppData", builtInPath, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:\\nonexistent\\file.json")]
    [InlineData("..\\..\\..\\sensitive.json")]
    [InlineData("C:\\system\\protected.json")]
    public async Task ImportFeaturesFromFileAsync_WithRejectedOrMissingPath_ReturnsEmptyList(string? filePath)
    {
        var (_, service) = CreateService();

        var result = await service.ImportFeaturesFromFileAsync(filePath!);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithOversizedFile_ReturnsEmptyList()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json");

        await using (var stream = File.Create(tempFile.FilePath))
            stream.SetLength((1024 * 1024) + 1);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithJsonArray_ReturnsParsedFeatures()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json");
        const string jsonContent = """
            [
              { "id": 12345, "name": "Feature 1", "description": "Test feature" },
              { "id": 67890, "name": "Feature 2", "description": "Another test" }
            ]
            """;
        await File.WriteAllTextAsync(tempFile.FilePath, jsonContent);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Collection(
            result,
            feature => AssertFeature(feature, 12345, "Feature 1", "Test feature"),
            feature => AssertFeature(feature, 67890, "Feature 2", "Another test"));
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithSingleJsonObject_ReturnsSingleFeature()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json");
        const string jsonContent = """{ "id": 12345, "name": "Single Feature", "description": "Test" }""";
        await File.WriteAllTextAsync(tempFile.FilePath, jsonContent);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        var feature = Assert.Single(result);
        AssertFeature(feature, 12345, "Single Feature", "Test");
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithFeaturesPropertyAndAlternateNames_ReturnsParsedFeatures()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json");
        const string jsonContent = """
            {
              "features": [
                { "Id": 12345, "Name": "Feature A" },
                { "FeatureId": 67890, "Description": "Imported from dictionary" }
              ]
            }
            """;
        await File.WriteAllTextAsync(tempFile.FilePath, jsonContent);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Collection(
            result,
            feature => AssertFeature(feature, 12345, "Feature A", string.Empty),
            feature => AssertFeature(feature, 67890, "Feature 67890", "Imported from dictionary"));
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithTextIdsAndComments_ReturnsOnlyFeatureIds()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".txt");
        const string textContent = """
            # This is a comment
            12345
            # Another comment
            67890
            11111
            """;
        await File.WriteAllTextAsync(tempFile.FilePath, textContent);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Collection(
            result,
            feature => AssertFeature(feature, 12345, "Feature 12345", string.Empty),
            feature => AssertFeature(feature, 67890, "Feature 67890", string.Empty),
            feature => AssertFeature(feature, 11111, "Feature 11111", string.Empty));
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithCsvRows_ReturnsParsedFeatures()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".csv");
        const string csvContent = """
            12345,Feature 1,Description 1
            67890,Feature 2,Description 2
            """;
        await File.WriteAllTextAsync(tempFile.FilePath, csvContent);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Collection(
            result,
            feature => AssertFeature(feature, 12345, "Feature 1", "Description 1"),
            feature => AssertFeature(feature, 67890, "Feature 2", "Description 2"));
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithJsonMissingIds_ReturnsEmptyList()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json");
        const string jsonContent = """[{ "name": "Feature", "description": "Missing id" }]""";
        await File.WriteAllTextAsync(tempFile.FilePath, jsonContent);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithInvalidJsonFallsBackToTextAndReturnsEmptyForNonNumericLines()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json");
        const string jsonContent = """
            {invalid json content}
            not-a-number
            also-not-a-number
            """;
        await File.WriteAllTextAsync(tempFile.FilePath, jsonContent);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ImportFeaturesFromFileAsync_WithEmptyFile_ReturnsEmptyList()
    {
        var (_, service) = CreateService();
        using var tempFile = ViveToolTestFileHelper.CreateScope(".json");
        await File.WriteAllTextAsync(tempFile.FilePath, string.Empty);

        var result = await service.ImportFeaturesFromFileAsync(tempFile.FilePath);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-url")]
    [InlineData("http://example.com/features.json")]
    [InlineData("https://localhost/features.json")]
    [InlineData("https://127.0.0.1/features.json")]
    [InlineData("https://10.0.0.1/features.json")]
    [InlineData("https://192.168.1.10/features.json")]
    [InlineData("https://169.254.1.10/features.json")]
    [InlineData("https://0.0.0.0/features.json")]
    public async Task ImportFeaturesFromUrlAsync_WithRejectedUri_ReturnsEmptyList(string? url)
    {
        var (_, service) = CreateService();

        var result = await service.ImportFeaturesFromUrlAsync(url!);

        Assert.Empty(result);
    }

    private static (ViveToolPathService PathService, ViveToolDownloadService Service) CreateService()
    {
        var pathService = new ViveToolPathService();
        return (pathService, new ViveToolDownloadService(pathService));
    }

    private static void AssertFeature(
        FeatureFlagInfo feature,
        int expectedId,
        string expectedName,
        string expectedDescription)
    {
        Assert.Equal(expectedId, feature.Id);
        Assert.Equal(expectedName, feature.Name);
        Assert.Equal(expectedDescription, feature.Description);
        Assert.Equal(FeatureFlagStatus.Unknown, feature.Status);
    }

    private static void AssertBundledRuntimeFilesExist(string bundledPath)
    {
        var runtimeDirectory = Path.GetDirectoryName(bundledPath);
        Assert.NotNull(runtimeDirectory);

        foreach (var requiredFileName in RequiredRuntimeFileNames)
            Assert.True(File.Exists(Path.Combine(runtimeDirectory!, requiredFileName)), $"Missing bundled runtime file: {requiredFileName}");
    }

    private static readonly string[] RequiredRuntimeFileNames =
    [
        ViveToolPathService.ViveToolExeName,
        "Albacore.ViVe.dll",
        "Newtonsoft.Json.dll",
        "FeatureDictionary.pfs"
    ];
}
