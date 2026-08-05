using System.Text.Json;
using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public class PluginValidationServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public PluginValidationServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "udt-validation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(
            Path.Combine(_tempRoot, "UniversalDeviceToolkit.Plugins.sln"),
            "Microsoft Visual Studio Solution File, Format Version 12.00\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RunAsync_ToleratesNullOptimizationActions()
    {
        CreatePluginFolder("test-plugin", optimizationActions: "null");

        var service = new PluginValidationService(_ => { });
        var request = new ValidationRequest
        {
            RepositoryRoot = _tempRoot,
            SkipBuild = true,
            SkipTests = true,
            Profile = PluginValidationProfile.Contributor,
            PluginIds = ["test-plugin"],
        };

        var report = await service.RunAsync(request);

        // Should not throw NullReferenceException. Validation may report
        // failures for missing files but must not crash.
        Assert.NotNull(report);
        Assert.Single(report.Plugins);
    }

    [Fact]
    public async Task RunAsync_ToleratesNullRequiredFilesWithOutputDir()
    {
        CreatePluginFolder("test-plugin", requiredFiles: "null");

        // Create a fake build output directory so ValidatePackageContents
        // is reached (it early-returns when the directory does not exist).
        var outputDir = Path.Combine(
            _tempRoot, ".build", "plugins", "UniversalDeviceToolkit.Plugins.test-plugin");
        Directory.CreateDirectory(outputDir);
        File.WriteAllText(Path.Combine(outputDir, "dummy.txt"), "placeholder");

        var service = new PluginValidationService(_ => { });
        var request = new ValidationRequest
        {
            RepositoryRoot = _tempRoot,
            SkipBuild = true,
            SkipTests = true,
            Profile = PluginValidationProfile.Contributor,
            PluginIds = ["test-plugin"],
        };

        var report = await service.RunAsync(request);

        // Should not throw NullReferenceException from iterating null RequiredFiles.
        Assert.NotNull(report);
        Assert.Single(report.Plugins);
    }

    private void CreatePluginFolder(
        string folderName,
        string? optimizationActions = null,
        string? requiredFiles = null)
    {
        var pluginDir = Path.Combine(_tempRoot, "Official", folderName);
        Directory.CreateDirectory(pluginDir);

        // Legacy plugin.json
        var legacyManifest = new
        {
            Id = folderName,
            Name = "Test Plugin",
            Version = "1.0.0",
            MinLltVersion = "4.2.1",
            Author = "SSC-STUDIO",
            IsSystemPlugin = false,
            Repository = "https://example.com/repo",
            Issues = "https://example.com/issues",
        };
        File.WriteAllText(
            Path.Combine(pluginDir, "plugin.json"),
            JsonSerializer.Serialize(legacyManifest));

        // Unified plugin.manifest.json with optional null tokens
        var optimizationActionsFragment = optimizationActions is null
            ? string.Empty
            : $@",\n    ""optimizationActions"": {optimizationActions}";

        // When requiredFiles is explicitly null, it overwrites the C# [] initializer
        // after deserialization, causing NRE without the ?? [] guard.
        var requiredFilesValue = requiredFiles ?? @"[""plugin.json""]";

        var unifiedManifest = $@"{{
    ""schemaVersion"": 1,
    ""id"": ""{folderName}"",
    ""name"": ""Test Plugin"",
    ""version"": ""1.0.0"",
    ""minHostVersion"": ""4.2.1"",
    ""author"": ""SSC-STUDIO"",
    ""isSystemPlugin"": false,
    ""repository"": ""https://example.com/repo"",
    ""issues"": ""https://example.com/issues"",
    ""store"": {{
        ""description"": ""A test plugin."",
        ""icon"": ""Icon24"",
        ""iconBackground"": ""#FFFFFF"",
        ""tags"": [""test""],
        ""dependencies"": [],
        ""supportedLanguages"": [""en""],
        ""repositoryUrl"": ""https://example.com/repo""
    }},
    ""package"": {{
        ""assetName"": ""{folderName}-v1.0.0.zip"",
        ""requiredFiles"": {requiredFilesValue}
    }}{optimizationActionsFragment}
}}";

        File.WriteAllText(
            Path.Combine(pluginDir, "plugin.manifest.json"),
            unifiedManifest.Replace("\\n", "\n"));
    }
}
