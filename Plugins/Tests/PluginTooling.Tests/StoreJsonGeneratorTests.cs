using System.Text.Json;
using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public class StorePluginEntryTests
{
    [Fact]
    public void Status_DefaultsToActive()
    {
        var entry = new StorePluginEntry();

        Assert.Equal(PluginLifecycleStatus.Active, entry.Status);
    }

    [Fact]
    public void Status_RoundTripsThroughJson()
    {
        var entry = new StorePluginEntry
        {
            Id = "test-plugin",
            Name = "Test Plugin",
            Status = PluginLifecycleStatus.Migrated,
        };

        var json = JsonSerializer.Serialize(entry);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("status", out var statusProperty));
        Assert.Equal("Migrated", statusProperty.GetString());
    }

    [Theory]
    [InlineData(PluginLifecycleStatus.Active)]
    [InlineData(PluginLifecycleStatus.Deprecated)]
    [InlineData(PluginLifecycleStatus.Migrated)]
    public void PluginLifecycleStatus_ExposesExpectedValues(string value)
    {
        Assert.False(string.IsNullOrWhiteSpace(value));
    }
}

public class StoreJsonGeneratorTests : IDisposable
{
    private readonly string _tempRoot;

    public StoreJsonGeneratorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "udt-store-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        // The PluginRepository.EnsureRepositoryRoot guard requires both
        // LenovoLegionToolkit-Plugins.sln and a Plugins/ directory at the root.
        File.WriteAllText(Path.Combine(_tempRoot, "LenovoLegionToolkit-Plugins.sln"), "Microsoft Visual Studio Solution File, Format Version 12.00\n");
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
    public void Generate_MarksMigratedPlugin_FromExplicitLifecycleField()
    {
        CreatePluginFolder("battery-health", manifestLifecycle: "Migrated", manifestName: "Battery Health (Migrated)",
            description: "Deprecated: battery health monitoring is now built into Universal Device Toolkit.");

        var store = InvokeGenerate();

        var entry = Assert.Single(store.Plugins);
        Assert.Equal("battery-health", entry.Id);
        Assert.Equal(PluginLifecycleStatus.Migrated, entry.Status);
    }

    [Fact]
    public void Generate_InfersMigratedStatus_FromNameSuffix_WhenLifecycleMissing()
    {
        CreatePluginFolder("legacy-network", manifestLifecycle: null, manifestName: "Legacy Network (Migrated)",
            description: "Old proxy feature retained for migration.");

        var store = InvokeGenerate();

        Assert.Equal(PluginLifecycleStatus.Migrated, store.Plugins.Single().Status);
    }

    [Fact]
    public void Generate_InfersMigratedStatus_FromDeprecatedDescriptionPrefix()
    {
        CreatePluginFolder("legacy-thing", manifestLifecycle: null, manifestName: "Legacy Thing",
            description: "Deprecated: this feature moved to the host application.");

        var store = InvokeGenerate();

        Assert.Equal(PluginLifecycleStatus.Migrated, store.Plugins.Single().Status);
    }

    [Fact]
    public void Generate_KeepsActiveStatus_ForNonMigratedPlugins()
    {
        CreatePluginFolder("vive-tool", manifestLifecycle: "Active", manifestName: "ViVeTool",
            description: "Unlock hidden Windows features with ViVeTool.");

        var store = InvokeGenerate();

        Assert.Equal(PluginLifecycleStatus.Active, store.Plugins.Single().Status);
    }

    [Fact]
    public void Generate_FallsBackToActive_WhenNoSignalsPresent()
    {
        CreatePluginFolder("custom-mouse", manifestLifecycle: null, manifestName: "Cursor & Pointer",
            description: "Personalize your mouse cursor experience.");

        var store = InvokeGenerate();

        Assert.Equal(PluginLifecycleStatus.Active, store.Plugins.Single().Status);
    }

    [Fact]
    public void Generate_PopulatesStatusField_FromManifestLifecycle()
    {
        CreatePluginFolder("shell-integration", manifestLifecycle: "Active", manifestName: "Nilesoft Shell Manager",
            description: "Manage Nilesoft Shell registration and its UDT-managed configuration.");

        var store = InvokeGenerate();

        var entry = store.Plugins.Single();
        Assert.Equal(PluginLifecycleStatus.Active, entry.Status);
        Assert.Equal("Nilesoft Shell Manager", entry.Name);
    }

    [Fact]
    public void Generate_PersistsLastUpdatedTimestamp()
    {
        CreatePluginFolder("sample", manifestLifecycle: "Active", manifestName: "Sample Plugin",
            description: "Sample description.");

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var store = InvokeGenerate();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.False(string.IsNullOrWhiteSpace(store.LastUpdated));
        Assert.True(DateTimeOffset.TryParse(store.LastUpdated, out var parsed));
        Assert.InRange(parsed, before, after);
    }

    [Fact]
    public void Generate_FiltersPlugins_WithoutStoreMetadata()
    {
        CreatePluginFolder("no-store-meta", includeStoreBlock: false, manifestLifecycle: null,
            manifestName: "No Store Meta", description: string.Empty);

        var store = InvokeGenerate();

        Assert.Empty(store.Plugins);
    }

    [Fact]
    public void Generate_EmitsAuthorFromManifest_WhenStoreMetadataLacksAuthor()
    {
        CreatePluginFolder("explicit-author", manifestLifecycle: "Active", manifestName: "Explicit Author",
            description: "Verifies the author is propagated from the manifest.", author: "SSC-STUDIO");

        var store = InvokeGenerate();

        Assert.Equal("SSC-STUDIO", store.Plugins.Single().Author);
    }

    private void CreatePluginFolder(
        string folderName,
        string? manifestLifecycle,
        string manifestName,
        string description,
        bool includeStoreBlock = true,
        string author = "SSC-STUDIO")
    {
        var pluginDir = Path.Combine(_tempRoot, "Plugins", folderName);
        Directory.CreateDirectory(pluginDir);

        var legacyManifest = new
        {
            Id = folderName,
            Name = manifestName,
            Version = "1.0.0",
            MinLltVersion = "4.2.1",
            Author = author,
            IsSystemPlugin = false,
            Repository = "https://example.com/repo",
            Issues = "https://example.com/issues",
        };
        File.WriteAllText(Path.Combine(pluginDir, "plugin.json"),
            JsonSerializer.Serialize(legacyManifest));

        var lifecycleFragment = manifestLifecycle is null
            ? string.Empty
            : $",\n    \"lifecycle\": \"{manifestLifecycle}\"";

        var storeBlock = includeStoreBlock
            ? $@",
    ""store"": {{
        ""description"": ""{description}"",
        ""icon"": ""Icon24"",
        ""iconBackground"": ""#FFFFFF"",
        ""tags"": [""sample""],
        ""dependencies"": [],
        ""supportedLanguages"": [""en""],
        ""repositoryUrl"": ""https://example.com/repo""
    }}"
            : string.Empty;

        var unifiedManifest = $@"{{
    ""schemaVersion"": 1,
    ""id"": ""{folderName}"",
    ""name"": ""{manifestName}"",
    ""version"": ""1.0.0"",
    ""minHostVersion"": ""4.2.1"",
    ""author"": ""{author}"",
    ""isSystemPlugin"": false,
    ""repository"": ""https://example.com/repo"",
    ""issues"": ""https://example.com/issues""{lifecycleFragment}{storeBlock}
}}";

        File.WriteAllText(Path.Combine(pluginDir, "plugin.manifest.json"), unifiedManifest);
    }

    private StoreDocument InvokeGenerate()
    {
        var request = new StoreGenerationRequest
        {
            RepositoryRoot = _tempRoot,
            AssetRoot = Path.Combine(_tempRoot, "Build", "release-assets"),
            ReleaseRepositoryUrl = "https://example.com/releases",
            MergeExisting = false,
            RequireAssets = false,
        };

        return new StoreJsonGenerator().Generate(request);
    }
}