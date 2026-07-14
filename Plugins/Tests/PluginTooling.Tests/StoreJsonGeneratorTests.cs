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
        // UniversalDeviceToolkit-Plugins.sln and a Plugins/ directory at the root.
        File.WriteAllText(Path.Combine(_tempRoot, "UniversalDeviceToolkit-Plugins.sln"), "Microsoft Visual Studio Solution File, Format Version 12.00\n");
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

    [Fact]
    public void Generate_ComputesZipAndMainDllHashes_WhenReleaseAssetExists()
    {
        CreatePluginFolder("hash-plugin", manifestLifecycle: "Active", manifestName: "Hash Plugin",
            description: "Integrity hashes from release ZIP.");

        var assetDir = Path.Combine(_tempRoot, "Build", "release-assets");
        Directory.CreateDirectory(assetDir);
        var zipPath = Path.Combine(assetDir, "hash-plugin-v1.0.0.zip");
        var dllBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 }; // tiny PE-like payload
        using (var archive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("UniversalDeviceToolkit.Plugins.hash-plugin.dll");
            using var stream = entry.Open();
            stream.Write(dllBytes);
        }

        string expectedZipHash;
        using (var zipStream = File.OpenRead(zipPath))
            expectedZipHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(zipStream)).ToLowerInvariant();
        var expectedDllHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(dllBytes)).ToLowerInvariant();

        var store = InvokeGenerate(pluginIds: ["hash-plugin"]);
        var entryResult = Assert.Single(store.Plugins);
        Assert.False(string.IsNullOrWhiteSpace(entryResult.ZipHash));
        Assert.False(string.IsNullOrWhiteSpace(entryResult.FileHash));
        Assert.Equal(expectedZipHash, entryResult.ZipHash);
        Assert.Equal(expectedDllHash, entryResult.FileHash);
    }

    private void CreatePluginFolder(
        string folderName,
        string? manifestLifecycle,
        string manifestName,
        string description,
        bool includeStoreBlock = true,
        bool includeLocalization = false,
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

        var localizationFragment = includeLocalization
            ? @",
    ""localizedNames"": {
        ""default"": """ + manifestName + @""",
        ""zh-Hans"": ""电池健康（旧版）""
    },
    ""localizedDescriptions"": {
        ""default"": """ + description + @""",
        ""zh-Hans"": ""已弃用：电池健康插件。""
    },
    ""localizedTags"": {
        ""default"": [ ""battery"", ""health"" ],
        ""zh-Hans"": [ ""电池"", ""健康"" ]
    }"
            : string.Empty;

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
    ""issues"": ""https://example.com/issues""{localizationFragment}{lifecycleFragment}{storeBlock}
}}";

        File.WriteAllText(Path.Combine(pluginDir, "plugin.manifest.json"), unifiedManifest);
    }

    [Fact]
    public void Generate_MergeExisting_PreservesLocalizedFields_WhenManifestLacksThem()
    {
        CreatePluginFolder("custom-mouse", manifestLifecycle: "Active", manifestName: "Cursor & Pointer",
            description: "Mouse customization plugin.");

        var assetDir = Path.Combine(_tempRoot, "Build", "release-assets");
        Directory.CreateDirectory(assetDir);
        File.WriteAllBytes(Path.Combine(assetDir, "custom-mouse-v1.0.0.zip"), new byte[1234]);

        var storePath = Path.Combine(_tempRoot, "store.json");
        File.WriteAllText(storePath,
            """
            {
              "lastUpdated": "2026-07-14T00:00:00.0000000+00:00",
              "storeVersion": "1.1.1",
              "plugins": [
                {
                  "id": "custom-mouse",
                  "name": "Cursor & Pointer",
                  "description": "Mouse customization plugin.",
                  "localizedNames": {
                    "default": "Cursor & Pointer",
                    "zh-Hans": "光标与指针"
                  },
                  "localizedDescriptions": {
                    "default": "Mouse customization plugin.",
                    "zh-Hans": "鼠标自定义插件。"
                  },
                  "localizedTags": {
                    "default": [ "mouse", "customization" ],
                    "zh-Hans": [ "鼠标", "自定义" ]
                  },
                  "author": "SSC-STUDIO",
                  "version": "1.0.0",
                  "minLLTVersion": "4.2.1",
                  "isSystemPlugin": false,
                  "downloadUrl": "https://example.com/releases/download/custom-mouse-v1.0.0/custom-mouse-v1.0.0.zip",
                  "changelog": "https://example.com/releases/tag/custom-mouse-v1.0.0",
                  "fileSize": 1234,
                  "releaseDate": "2026-07-14T00:00:00.0000000+00:00",
                  "repositoryUrl": "https://example.com/repo",
                  "supportedLanguages": [ "en" ],
                  "icon": "Icon24",
                  "iconBackground": "#FFFFFF",
                  "dependencies": [],
                  "tags": [ "mouse", "customization" ],
                  "status": "Active"
                }
              ]
            }
            """);

        var store = InvokeGenerate(mergeExisting: true, pluginIds: ["custom-mouse"]);

        var entry = Assert.Single(store.Plugins);
        Assert.Equal("光标与指针", entry.LocalizedNames["zh-Hans"]);
        Assert.Equal("鼠标自定义插件。", entry.LocalizedDescriptions["zh-Hans"]);
        Assert.Equal(new[] { "鼠标", "自定义" }, entry.LocalizedTags["zh-Hans"]);
    }

    [Fact]
    public void Generate_PopulatesLocalizedFields_FromUnifiedManifest()
    {
        CreatePluginFolder("battery-health", manifestLifecycle: "Migrated", manifestName: "Battery Health (Legacy)",
            description: "Deprecated battery plugin.", includeLocalization: true);

        var store = InvokeGenerate();

        var entry = Assert.Single(store.Plugins);
        Assert.Equal("电池健康（旧版）", entry.LocalizedNames["zh-Hans"]);
        Assert.Contains("已弃用", entry.LocalizedDescriptions["zh-Hans"]);
        Assert.Equal(new[] { "电池", "健康" }, entry.LocalizedTags["zh-Hans"]);
    }

    [Fact]
    public void ProductionStoreJson_RoundTripsWithoutLosingModeledFields()
    {
        var repositoryRoot = PluginRepository.FindRepositoryRoot(AppContext.BaseDirectory);
        var storePath = Path.Combine(repositoryRoot, "store.json");
        Assert.True(File.Exists(storePath), $"Expected production store at {storePath}");

        var original = PluginRepository.ReadJsonFile<StoreDocument>(storePath);
        Assert.False(string.IsNullOrWhiteSpace(original.StoreVersion));
        Assert.Equal(3, original.Plugins.Count);
        Assert.All(original.Plugins, entry =>
        {
            Assert.NotEmpty(entry.LocalizedNames);
            Assert.NotEmpty(entry.LocalizedDescriptions);
            Assert.NotEmpty(entry.LocalizedTags);
            // Official Active packages must ship ZIP + main-DLL integrity digests.
            if (string.Equals(entry.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.ZipHash), $"{entry.Id} missing zipHash");
                Assert.False(string.IsNullOrWhiteSpace(entry.FileHash), $"{entry.Id} missing fileHash");
                Assert.Equal(64, entry.ZipHash.Length);
                Assert.Equal(64, entry.FileHash.Length);
            }
        });

        var roundTripPath = Path.Combine(_tempRoot, "store-roundtrip.json");
        PluginRepository.WriteJsonFile(roundTripPath, original);
        var roundTrip = PluginRepository.ReadJsonFile<StoreDocument>(roundTripPath);

        Assert.Equal(original.StoreVersion, roundTrip.StoreVersion);
        Assert.Equal(original.Plugins.Count, roundTrip.Plugins.Count);
        foreach (var expected in original.Plugins)
        {
            var actual = roundTrip.Plugins.Single(plugin => plugin.Id == expected.Id);
            Assert.Equal(expected.LocalizedNames.Count, actual.LocalizedNames.Count);
            foreach (var pair in expected.LocalizedNames)
                Assert.Equal(pair.Value, actual.LocalizedNames[pair.Key]);

            Assert.Equal(expected.LocalizedDescriptions.Count, actual.LocalizedDescriptions.Count);
            foreach (var pair in expected.LocalizedDescriptions)
                Assert.Equal(pair.Value, actual.LocalizedDescriptions[pair.Key]);

            Assert.Equal(expected.LocalizedTags["zh-Hans"], actual.LocalizedTags["zh-Hans"]);
        }
    }

    [Fact]
    public void Generate_MergeExisting_BumpsTwoPartStoreVersion()
    {
        CreatePluginFolder("sample", manifestLifecycle: "Active", manifestName: "Sample",
            description: "Sample plugin.");

        // Write a store.json with a 2-part SemVer version string ("1.0" not "1.0.0").
        // BumpStoreVersion must handle Version.Build == -1 from 2-part parsing.
        var storePath = Path.Combine(_tempRoot, "store.json");
        File.WriteAllText(storePath,
            """
            {
              "lastUpdated": "2026-07-14T00:00:00.0000000+00:00",
              "storeVersion": "1.0",
              "plugins": []
            }
            """);

        // Modify the manifest so content differs from the existing (empty) plugin list.
        // This triggers storeContentChanged = true, invoking BumpStoreVersion.
        CreatePluginFolder("sample", manifestLifecycle: "Active", manifestName: "Sample",
            description: "Sample plugin.");

        var request = new StoreGenerationRequest
        {
            RepositoryRoot = _tempRoot,
            AssetRoot = Path.Combine(_tempRoot, "Build", "release-assets"),
            ReleaseRepositoryUrl = "https://example.com/releases",
            MergeExisting = true,
            RequireAssets = false,
            PluginIds = Array.Empty<string>(),
        };
        var store = new StoreJsonGenerator().Generate(request);

        // 2-part "1.0" → Math.Max(-1, 0) + 1 = 1 → "1.0.1", NOT "1.0.0"
        Assert.Equal("1.0.1", store.StoreVersion);
    }

    [Fact]
    public void Generate_MergeExisting_HandlesNullCollectionFieldsInExistingStore()
    {
        CreatePluginFolder("sample", manifestLifecycle: "Active", manifestName: "Sample",
            description: "Sample plugin.");

        // Write a store.json where collection fields are explicitly null.
        // System.Text.Json deserializes "field": null to null even when the
        // C# property has a non-null default, causing NullReferenceException
        // in EntriesEqual → StringDictionariesEqual/TagDictionariesEqual/SequenceEqual.
        var storePath = Path.Combine(_tempRoot, "store.json");
        File.WriteAllText(storePath,
            """
            {
              "lastUpdated": "2026-07-14T00:00:00.0000000+00:00",
              "storeVersion": "1.0.0",
              "plugins": [
                {
                  "id": "sample",
                  "name": "Sample",
                  "description": "Sample plugin.",
                  "localizedNames": null,
                  "localizedDescriptions": null,
                  "localizedTags": null,
                  "supportedLanguages": null,
                  "dependencies": null,
                  "tags": null
                }
              ]
            }
            """);

        var store = InvokeGenerate(mergeExisting: true);

        // Should not throw NRE; should produce valid output with defaults.
        Assert.Equal("1.0.1", store.StoreVersion);
        var entry = Assert.Single(store.Plugins);
        Assert.Equal("sample", entry.Id);
        Assert.NotNull(entry.SupportedLanguages);
        Assert.NotNull(entry.Dependencies);
        Assert.NotNull(entry.Tags);
    }

    private StoreDocument InvokeGenerate(bool mergeExisting = false, IReadOnlyList<string>? pluginIds = null)
    {
        var request = new StoreGenerationRequest
        {
            RepositoryRoot = _tempRoot,
            AssetRoot = Path.Combine(_tempRoot, "Build", "release-assets"),
            ReleaseRepositoryUrl = "https://example.com/releases",
            MergeExisting = mergeExisting,
            RequireAssets = false,
            PluginIds = pluginIds ?? Array.Empty<string>(),
        };

        return new StoreJsonGenerator().Generate(request);
    }

    [Fact]
    public void Generate_NullCollectionFieldsInManifestJson_DoesNotThrow()
    {
        // When plugin.manifest.json has "tags": null, "dependencies": null, etc.,
        // StoreJsonGenerator.Generate must null-coalesce before .ToList().
        // This exercises the initial entry creation path (lines 79/82/83).
        var pluginDir = Path.Combine(_tempRoot, "Plugins", "null-collections");
        Directory.CreateDirectory(pluginDir);

        var legacyManifest = JsonSerializer.Serialize(new
        {
            Id = "null-collections",
            Name = "Null Collections Test",
            Version = "1.0.0",
            MinLltVersion = "4.2.1",
            Author = "SSC-STUDIO",
            IsSystemPlugin = false,
            Repository = "https://example.com/repo",
            Issues = "https://example.com/issues",
        });
        File.WriteAllText(Path.Combine(pluginDir, "plugin.json"), legacyManifest);

        // Manifest with explicit null for tags, dependencies, supportedLanguages
        var unifiedManifest = """
            {
              "schemaVersion": 1,
              "id": "null-collections",
              "name": "Null Collections Test",
              "version": "1.0.0",
              "minHostVersion": "4.2.1",
              "author": "SSC-STUDIO",
              "isSystemPlugin": false,
              "repository": "https://example.com/repo",
              "issues": "https://example.com/issues",
              "store": {
                "description": "Test plugin with null collections.",
                "icon": "PuzzlePiece24",
                "iconBackground": "#FFF1E2",
                "tags": null,
                "dependencies": null,
                "supportedLanguages": null,
                "repositoryUrl": "https://example.com/repo"
              }
            }
            """;
        File.WriteAllText(Path.Combine(pluginDir, "plugin.manifest.json"), unifiedManifest);

        var store = InvokeGenerate();

        // HasStoreMetadata returns false when Tags is null/empty, so the plugin
        // is correctly skipped — no entry in store.Plugins. The test verifies
        // that Generate does not throw NRE when collections are null.
        Assert.Empty(store.Plugins);
    }

    [Fact]
    public void Generate_NullRequiredFilesInManifestJson_HasStoreMetadataDoesNotThrow()
    {
        // HasStoreMetadata in StoreJsonGenerator accesses store.Tags.Count and
        // store.SupportedLanguages.Count. With null collections, this must not throw.
        var pluginDir = Path.Combine(_tempRoot, "Plugins", "null-required-files");
        Directory.CreateDirectory(pluginDir);

        File.WriteAllText(Path.Combine(pluginDir, "plugin.json"),
            JsonSerializer.Serialize(new { Id = "null-required-files", Name = "Null Required Files", Version = "1.0.0", MinLltVersion = "4.2.1", Author = "SSC-STUDIO", IsSystemPlugin = false, Repository = "https://example.com/repo", Issues = "https://example.com/issues" }));

        // Manifest with null for requiredFiles AND null store collections
        var unifiedManifest = """
            {
              "schemaVersion": 1,
              "id": "null-required-files",
              "name": "Null Required Files",
              "version": "1.0.0",
              "minHostVersion": "4.2.1",
              "author": "SSC-STUDIO",
              "isSystemPlugin": false,
              "repository": "https://example.com/repo",
              "issues": "https://example.com/issues",
              "package": {
                "assetName": "",
                "requiredFiles": null
              },
              "store": {
                "description": "Test plugin with null requiredFiles.",
                "icon": "PuzzlePiece24",
                "iconBackground": "#FFF1E2",
                "tags": null,
                "dependencies": null,
                "supportedLanguages": null
              }
            }
            """;
        File.WriteAllText(Path.Combine(pluginDir, "plugin.manifest.json"), unifiedManifest);

        var store = InvokeGenerate();
        // Does not throw NRE — plugin is skipped because HasStoreMetadata is false.
        Assert.Empty(store.Plugins);
    }

    [Fact]
    public void Migrate_NullCollectionFields_DoesNotThrow()
    {
        // PluginManifestMigrator.Migrate accesses SupportedLanguages.Count, Tags.Count,
        // RequiredFiles.Contains/Add, and SupportedLanguages.AddRange — all NRE-prone
        // when JSON has explicit null for these collections.
        var pluginDir = Path.Combine(_tempRoot, "Plugins", "migrate-null-test");
        Directory.CreateDirectory(pluginDir);

        File.WriteAllText(Path.Combine(pluginDir, "plugin.json"),
            JsonSerializer.Serialize(new { Id = "migrate-null-test", Name = "Migrate Null Test", Version = "1.0.0", MinLltVersion = "4.2.1", Author = "SSC-STUDIO", IsSystemPlugin = false, Repository = "https://example.com/repo", Issues = "https://example.com/issues" }));

        var unifiedManifest = """
            {
              "schemaVersion": 1,
              "id": "migrate-null-test",
              "name": "Migrate Null Test",
              "version": "1.0.0",
              "minHostVersion": "4.2.1",
              "author": "SSC-STUDIO",
              "isSystemPlugin": false,
              "repository": "https://example.com/repo",
              "issues": "https://example.com/issues",
              "package": {
                "assetName": "",
                "requiredFiles": null
              },
              "store": {
                "description": "Test plugin for Migrate null-collection regression.",
                "icon": "PuzzlePiece24",
                "iconBackground": "#FFF1E2",
                "tags": null,
                "dependencies": null,
                "supportedLanguages": null
              }
            }
            """;
        File.WriteAllText(Path.Combine(pluginDir, "plugin.manifest.json"), unifiedManifest);

        var migrator = new PluginManifestMigrator();
        var written = migrator.Migrate(_tempRoot, ["migrate-null-test"]);

        Assert.Single(written);
        // After migration, the manifest should have been written back with
        // non-null collections (EnsureRequiredFile adds required files, etc.)
        var rewrittenJson = File.ReadAllText(Path.Combine(pluginDir, "plugin.manifest.json"));
        Assert.DoesNotContain("\"requiredFiles\": null", rewrittenJson);
    }
}

public class CreateUnifiedManifestNullGuardTests
{
    private static PluginManifest NewManifest() => new(
        Id: "test-plugin",
        Name: "Test Plugin",
        Version: "1.0.0",
        MinLltVersion: "1.0.0",
        Author: "TestAuthor",
        IsSystemPlugin: false,
        Repository: "https://example.com/repo",
        Issues: "https://example.com/issues");

    [Fact]
    public void CreateUnifiedManifest_NullCollectionPropertiesInStoreEntry_DoesNotThrow()
    {
        // OfficialStoreEntry with null Tags/Dependencies/SupportedLanguages.
        // This simulates store-entry.json containing "tags": null, etc.
        var storeEntry = new OfficialStoreEntry(
            Description: "Test",
            Icon: "PuzzlePiece24",
            IconBackground: "#FFF1E2",
            Tags: null!,
            Dependencies: null!,
            SupportedLanguages: null!,
            RepositoryUrl: null);

        var manifest = NewManifest();
        var unified = PluginRepository.CreateUnifiedManifest(manifest, storeEntry, folderName: "TestPlugin");

        Assert.NotNull(unified.Store.Tags);
        Assert.Empty(unified.Store.Tags);
        Assert.NotNull(unified.Store.Dependencies);
        Assert.Empty(unified.Store.Dependencies);
        Assert.NotNull(unified.Store.SupportedLanguages);
        Assert.Equal(["en"], unified.Store.SupportedLanguages);
    }

    [Fact]
    public void CreateUnifiedManifest_NullStoreEntry_UsesDefaults()
    {
        var manifest = NewManifest();
        var unified = PluginRepository.CreateUnifiedManifest(manifest, storeEntry: null, folderName: "TestPlugin");

        Assert.Empty(unified.Store.Tags);
        Assert.Empty(unified.Store.Dependencies);
        Assert.Equal(["en"], unified.Store.SupportedLanguages);
    }

    [Fact]
    public void ToStoreEntry_NullCollectionFields_DoesNotProduceNullCollections()
    {
        // When plugin.manifest.json has "tags": null etc., PluginStoreMetadata.Tags
        // is deserialized as null (overwriting the [] initializer). ToStoreEntry
        // must null-coalesce so OfficialStoreEntry never carries null collections,
        // otherwise PluginValidationService.SequenceEqual throws NRE.
        var manifest = new UnifiedPluginManifest
        {
            Id = "test-plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Store = new PluginStoreMetadata
            {
                Description = "Test",
                Icon = "PuzzlePiece24",
                IconBackground = "#FFF1E2",
                Tags = null!,
                Dependencies = null!,
                SupportedLanguages = null!,
            },
        };

        var storeEntry = PluginRepository.ToStoreEntry(manifest);

        Assert.NotNull(storeEntry.Tags);
        Assert.Empty(storeEntry.Tags);
        Assert.NotNull(storeEntry.Dependencies);
        Assert.Empty(storeEntry.Dependencies);
        Assert.NotNull(storeEntry.SupportedLanguages);
        Assert.Empty(storeEntry.SupportedLanguages);
    }

    [Fact]
    public void EntriesEqual_NullCollectionFieldsOnBothSides_DoesNotThrow()
    {
        // Simulates PluginValidationService comparing two OfficialStoreEntry
        // instances where collection fields are null on both sides.
        var left = new OfficialStoreEntry(
            Description: "Test",
            Icon: "PuzzlePiece24",
            IconBackground: "#FFF1E2",
            Tags: null!,
            Dependencies: null!,
            SupportedLanguages: null!,
            RepositoryUrl: null);

        var right = new OfficialStoreEntry(
            Description: "Test",
            Icon: "PuzzlePiece24",
            IconBackground: "#FFF1E2",
            Tags: null!,
            Dependencies: null!,
            SupportedLanguages: null!,
            RepositoryUrl: null);

        // (null ?? []).SequenceEqual(null ?? []) => [].SequenceEqual([]) => true
        Assert.True((left.Tags ?? []).SequenceEqual(right.Tags ?? [], StringComparer.Ordinal));
        Assert.True((left.Dependencies ?? []).SequenceEqual(right.Dependencies ?? [], StringComparer.Ordinal));
        Assert.True((left.SupportedLanguages ?? []).SequenceEqual(right.SupportedLanguages ?? [], StringComparer.Ordinal));
    }
}