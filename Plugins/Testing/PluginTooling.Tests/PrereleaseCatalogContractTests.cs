using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public sealed class PluginSemVerContractTests
{
    private const string PreviewVersion = "2.0.0-preview.1";
    private const string StableVersion = "2.0.0";

    [Fact]
    public void PreviewVersion_ParsesAndOrdersBeforeStableWithSameNumericCore()
    {
        Assert.True(PluginVersionSynchronizer.IsPluginVersion(PreviewVersion));
        Assert.True(PluginVersionSynchronizer.IsPrereleasePluginVersion(PreviewVersion));
        Assert.False(PluginVersionSynchronizer.IsPrereleasePluginVersion(StableVersion));

        var previewNumeric = Version.Parse(PluginVersionSynchronizer.ToNumericVersion(PreviewVersion));
        var stableNumeric = Version.Parse(PluginVersionSynchronizer.ToNumericVersion(StableVersion));
        Assert.Equal(0, previewNumeric.CompareTo(stableNumeric));

        var ordered = new[] { StableVersion, PreviewVersion }
            .OrderBy(version => Version.Parse(PluginVersionSynchronizer.ToNumericVersion(version)))
            .ThenBy(version => PluginVersionSynchronizer.IsPrereleasePluginVersion(version) ? 0 : 1)
            .ToArray();

        Assert.Equal([PreviewVersion, StableVersion], ordered);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2.0")]
    [InlineData("2.0.0.0")]
    [InlineData("2.0.0-")]
    [InlineData("2.0.0-preview..1")]
    [InlineData("2.0.0-preview_1")]
    [InlineData("v2.0.0")]
    [InlineData("2.0.0 preview.1")]
    public void PluginVersionParser_RejectsMalformedVersionForms(string? version)
    {
        Assert.False(PluginVersionSynchronizer.IsPluginVersion(version));
        Assert.False(PluginVersionSynchronizer.IsPrereleasePluginVersion(version));
    }
}

public sealed class PrereleaseCatalogContractTests : IDisposable
{
    private const string PreviewVersion = "2.0.0-preview.1";
    private const string StableVersion = "2.0.0";
    private const string MinimumHostVersion = "6.0.0";
    private const string StableCatalogUrl =
        "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog";
    private const string PreviewCatalogUrl =
        "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog-preview";

    private readonly string _tempRoot;

    public PrereleaseCatalogContractTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "udt-prerelease-catalog-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, "Official"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, ".build", "catalog"));
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
    public void StoreGenerationRequest_DefaultsToStableCatalogTag()
    {
        var request = new StoreGenerationRequest();

        Assert.Equal(PluginCatalogChannel.Stable, request.CatalogChannel);
        Assert.Equal(StableCatalogUrl, request.ReleaseRepositoryUrl);
        Assert.DoesNotContain("plugin-catalog-preview", request.ReleaseRepositoryUrl, StringComparison.Ordinal);
    }

    [Fact]
    public void Synchronizer_PreservesPreviewVersionAcrossFilesAndChangelog()
    {
        const string changelog = """
            # Sample Plugin

            ## [2.0.0-preview.1] - 2026-08-13

            - Preview release.
            """;
        var pluginDirectory = CreatePluginFolder(
            folderName: "Sample",
            pluginId: "sample-plugin",
            version: PreviewVersion,
            legacyVersion: "1.0.0",
            projectVersion: "1.0.0",
            attributeVersion: "1.0.0",
            changelog: changelog);
        var changelogPath = Path.Combine(pluginDirectory, "CHANGELOG.md");
        var originalChangelog = File.ReadAllText(changelogPath);

        var context = new PluginRepository().Load(_tempRoot).Plugins["sample-plugin"];
        var report = new PluginVersionSynchronizer().Sync(context, writeChanges: true);

        Assert.True(report.Changed);
        Assert.True(report.IsAligned);
        Assert.Equal(PreviewVersion, report.ManifestVersion);
        Assert.Equal(PreviewVersion, report.ProjectVersion);
        Assert.Equal(PreviewVersion, report.PluginAttributeVersion);

        var unifiedManifest = PluginRepository.ReadJsonFile<UnifiedPluginManifest>(
            Path.Combine(pluginDirectory, "plugin.manifest.json"));
        var compatibilityManifest = PluginRepository.ReadJsonFile<PluginManifest>(
            Path.Combine(pluginDirectory, "plugin.json"));
        Assert.Equal(PreviewVersion, unifiedManifest.Version);
        Assert.Equal($"sample-plugin-v{PreviewVersion}.zip", unifiedManifest.Package.AssetName);
        Assert.Equal(PreviewVersion, compatibilityManifest.Version);
        Assert.Equal(MinimumHostVersion, compatibilityManifest.MinLltVersion);

        var projectPath = Path.Combine(
            pluginDirectory,
            "UniversalDeviceToolkit.Plugins.Sample.csproj");
        Assert.Equal(PreviewVersion, PluginRepository.ReadProjectProperty(projectPath, "Version"));
        Assert.Equal(StableVersion, PluginRepository.ReadProjectProperty(projectPath, "FileVersion"));
        Assert.Equal(StableVersion, PluginRepository.ReadProjectProperty(projectPath, "AssemblyVersion"));
        Assert.Equal(
            PreviewVersion,
            PluginVersionSynchronizer.ReadPluginAttributeVersion(pluginDirectory));
        Assert.Equal(originalChangelog, File.ReadAllText(changelogPath));
        Assert.Contains($"[{PreviewVersion}]", originalChangelog, StringComparison.Ordinal);

        var reloaded = new PluginRepository().Load(_tempRoot).Plugins["sample-plugin"];
        Assert.Equal(PreviewVersion, reloaded.UnifiedManifest.Version);
    }

    [Fact]
    public void Generate_PreviewCatalog_UsesExactTagAndRoundTripsPrerelease()
    {
        CreatePluginFolder(
            folderName: "PreviewSample",
            pluginId: "preview-sample",
            version: PreviewVersion);

        var store = Generate(
            PluginCatalogChannel.Preview,
            PreviewCatalogUrl,
            pluginIds: ["preview-sample"]);

        var entry = Assert.Single(store.Plugins);
        Assert.Equal(PreviewVersion, entry.Version);
        Assert.Equal(MinimumHostVersion, entry.MinLltVersion);
        Assert.Equal(
            $"{PreviewCatalogUrl}/preview-sample-v{PreviewVersion}.zip",
            entry.DownloadUrl);
        Assert.Equal(
            "https://example.com/repo/blob/master/Plugins/Official/PreviewSample/CHANGELOG.md",
            entry.Changelog);
        Assert.DoesNotContain(
            "/download/plugin-catalog/",
            entry.DownloadUrl,
            StringComparison.Ordinal);

        var roundTripPath = Path.Combine(_tempRoot, "preview-store-roundtrip.json");
        PluginRepository.WriteJsonFile(roundTripPath, store);
        var roundTrip = PluginRepository.ReadJsonFile<StoreDocument>(roundTripPath);
        var roundTripEntry = Assert.Single(roundTrip.Plugins);
        Assert.Equal(PreviewVersion, roundTripEntry.Version);
        Assert.Equal(entry.DownloadUrl, roundTripEntry.DownloadUrl);
        Assert.Equal(MinimumHostVersion, roundTripEntry.MinLltVersion);
    }

    [Theory]
    [InlineData(
        PluginCatalogChannel.Stable,
        PreviewCatalogUrl,
        "Stable catalog channel cannot use")]
    [InlineData(
        PluginCatalogChannel.Preview,
        StableCatalogUrl,
        "Preview catalog channel requires")]
    public void Generate_RejectsCatalogChannelTagMismatch(
        PluginCatalogChannel channel,
        string releaseUrl,
        string expectedMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Generate(channel, releaseUrl));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PluginCatalogChannel.Stable, "stable-only")]
    [InlineData(PluginCatalogChannel.Preview, "preview-only")]
    public void Generate_MixedExistingCatalog_KeepsOnlyRequestedChannel(
        PluginCatalogChannel channel,
        string expectedPluginId)
    {
        WriteExistingStore(
            channel,
            new StorePluginEntry
            {
                Id = "stable-only",
                Name = "Stable Only",
                Version = "1.5.0",
                DownloadUrl = $"{StableCatalogUrl}/stable-only-v1.5.0.zip",
            },
            new StorePluginEntry
            {
                Id = "preview-only",
                Name = "Preview Only",
                Version = PreviewVersion,
                DownloadUrl = $"{PreviewCatalogUrl}/preview-only-v{PreviewVersion}.zip",
            });

        var releaseUrl = channel == PluginCatalogChannel.Preview
            ? PreviewCatalogUrl
            : StableCatalogUrl;
        var store = Generate(channel, releaseUrl, mergeExisting: true);

        var entry = Assert.Single(store.Plugins);
        Assert.Equal(expectedPluginId, entry.Id);
        Assert.Equal(
            channel == PluginCatalogChannel.Preview,
            entry.DownloadUrl.Contains(
                "plugin-catalog-preview",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Write_PreviewOutput_DoesNotOverwriteStableCatalogFile()
    {
        WriteExistingStore(
            PluginCatalogChannel.Stable,
            new StorePluginEntry
            {
                Id = "stable-only",
                Name = "Stable Only",
                Version = "1.5.0",
                DownloadUrl = $"{StableCatalogUrl}/stable-only-v1.5.0.zip",
            });
        var stablePath = Path.Combine(_tempRoot, ".build", "catalog", "store.json");
        var originalStableCatalog = File.ReadAllText(stablePath);

        CreatePluginFolder(
            folderName: "PreviewSample",
            pluginId: "preview-sample",
            version: PreviewVersion);
        var previewPath = Path.Combine(
            _tempRoot,
            ".build",
            "catalog-preview",
            "store.json");
        var request = CreateStoreRequest(
            PluginCatalogChannel.Preview,
            PreviewCatalogUrl,
            pluginIds: ["preview-sample"],
            mergeExisting: true,
            outputPath: previewPath);

        var writtenPath = new StoreJsonGenerator().Write(request);

        Assert.Equal(previewPath, writtenPath);
        Assert.Equal(originalStableCatalog, File.ReadAllText(stablePath));
        var previewStore = PluginRepository.ReadJsonFile<StoreDocument>(previewPath);
        var previewEntry = Assert.Single(previewStore.Plugins);
        Assert.Equal("preview-sample", previewEntry.Id);
        Assert.Equal(PreviewVersion, previewEntry.Version);
        Assert.Contains(
            "plugin-catalog-preview",
            previewEntry.DownloadUrl,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_RejectsDuplicatePluginManifestIds()
    {
        CreatePluginFolder(
            folderName: "FirstFolder",
            pluginId: "duplicate-plugin",
            version: StableVersion);
        CreatePluginFolder(
            folderName: "SecondFolder",
            pluginId: "duplicate-plugin",
            version: StableVersion);

        var exception = Assert.Throws<ArgumentException>(
            () => Generate(PluginCatalogChannel.Stable, StableCatalogUrl));

        Assert.Contains("duplicate-plugin", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_OrdersPluginIdsDeterministically()
    {
        CreatePluginFolder("Zeta", "zeta-plugin", StableVersion);
        CreatePluginFolder("Alpha", "alpha-plugin", StableVersion);
        CreatePluginFolder("Beta", "beta-plugin", StableVersion);
        var releaseDate = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var request = CreateStoreRequest(
            PluginCatalogChannel.Stable,
            StableCatalogUrl,
            pluginIds: ["zeta-plugin", "alpha-plugin", "beta-plugin", "alpha-plugin"],
            releaseDate: releaseDate);
        var generator = new StoreJsonGenerator();

        var first = generator.Generate(request);
        var second = generator.Generate(request);

        Assert.Equal(
            ["alpha-plugin", "beta-plugin", "zeta-plugin"],
            first.Plugins.Select(entry => entry.Id));
        Assert.Equal(
            PluginRepository.ToJson(first),
            PluginRepository.ToJson(second));
    }

    [Fact]
    public async Task Validation_AcceptsMatchedMinHostVersion600CompatibilityFields()
    {
        CreatePluginFolder(
            folderName: "Compatible",
            pluginId: "compatible-plugin",
            version: PreviewVersion);

        var report = await ValidateAsync("compatible-plugin");

        Assert.Contains(
            report.Steps,
            step =>
                string.Equals(step.Status, "PASS", StringComparison.Ordinal) &&
                step.Message.Contains("plugin.json minLLTVersion", StringComparison.Ordinal));
        Assert.DoesNotContain(
            report.Steps,
            step =>
                string.Equals(step.Status, "FAIL", StringComparison.Ordinal) &&
                (step.Message.Contains("minHostVersion does not match", StringComparison.Ordinal) ||
                 step.Message.Contains("minLLTVersion does not match", StringComparison.Ordinal) ||
                 step.Message.Contains("FileVersion", StringComparison.Ordinal) ||
                 step.Message.Contains("AssemblyVersion", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Validation_AcceptsNumericFileAndAssemblyVersionForPrerelease()
    {
        CreatePluginFolder(
            folderName: "NumericAssembly",
            pluginId: "numeric-assembly",
            version: PreviewVersion);

        var report = await ValidateAsync("numeric-assembly");

        Assert.Contains(
            report.Steps,
            step =>
                string.Equals(step.Status, "PASS", StringComparison.Ordinal) &&
                step.Message.Contains("FileVersion", StringComparison.Ordinal));
        Assert.Contains(
            report.Steps,
            step =>
                string.Equals(step.Status, "PASS", StringComparison.Ordinal) &&
                step.Message.Contains("AssemblyVersion", StringComparison.Ordinal));
        Assert.DoesNotContain(
            report.Steps,
            step =>
                string.Equals(step.Status, "FAIL", StringComparison.Ordinal) &&
                (step.Message.Contains("FileVersion", StringComparison.Ordinal) ||
                 step.Message.Contains("AssemblyVersion", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Validation_OfficialReleaseStable_RejectsPrereleaseVersion()
    {
        CreatePluginFolder(
            folderName: "PreviewOnly",
            pluginId: "preview-only",
            version: PreviewVersion);

        var report = await ValidateAsync(
            "preview-only",
            PluginValidationProfile.OfficialRelease,
            PluginCatalogChannel.Stable);
        var failureMessages = report.Steps
            .Where(step => string.Equals(step.Status, "FAIL", StringComparison.Ordinal))
            .Select(step => step.Message)
            .ToArray();

        Assert.Contains(
            failureMessages,
            message => message.Contains(
                "Stable official-release validation cannot accept a prerelease plugin version",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task Validation_OfficialReleasePreview_AlignsWithPreviewCatalog()
    {
        CreatePluginFolder(
            folderName: "PreviewSample",
            pluginId: "preview-sample",
            version: PreviewVersion);
        WriteExistingStore(
            PluginCatalogChannel.Preview,
            new StorePluginEntry
            {
                Id = "preview-sample",
                Name = "PreviewSample Plugin",
                Version = PreviewVersion,
                MinLltVersion = MinimumHostVersion,
                DownloadUrl = $"{PreviewCatalogUrl}/preview-sample-v{PreviewVersion}.zip",
            });

        var report = await ValidateAsync(
            "preview-sample",
            PluginValidationProfile.OfficialRelease,
            PluginCatalogChannel.Preview);

        Assert.Contains(
            report.Steps,
            step =>
                string.Equals(step.Status, "PASS", StringComparison.Ordinal) &&
                step.Message.Contains("store.json version matches", StringComparison.Ordinal));
        Assert.Contains(
            report.Steps,
            step =>
                string.Equals(step.Status, "PASS", StringComparison.Ordinal) &&
                step.Message.Contains("store.json minLLTVersion matches", StringComparison.Ordinal));
        Assert.DoesNotContain(
            report.Steps,
            step =>
                string.Equals(step.Status, "FAIL", StringComparison.Ordinal) &&
                (step.Message.Contains("store.json is missing", StringComparison.Ordinal) ||
                 step.Message.Contains("catalog-preview/store.json is missing", StringComparison.Ordinal) ||
                 step.Message.Contains("prerelease plugin version", StringComparison.Ordinal)));
    }

    [Fact]
    public void Write_PreviewChannel_DefaultsToCatalogPreviewPath()
    {
        CreatePluginFolder(
            folderName: "PreviewSample",
            pluginId: "preview-sample",
            version: PreviewVersion);
        var stablePath = PluginRepository.GetCatalogStorePath(_tempRoot, PluginCatalogChannel.Stable);
        const string stableSentinel = """
            {
              "lastUpdated": "2026-08-13T00:00:00.0000000+00:00",
              "storeVersion": "9.9.9",
              "plugins": []
            }
            """;
        Directory.CreateDirectory(Path.GetDirectoryName(stablePath)!);
        File.WriteAllText(stablePath, stableSentinel);

        var writtenPath = new StoreJsonGenerator().Write(
            CreateStoreRequest(
                PluginCatalogChannel.Preview,
                PreviewCatalogUrl,
                pluginIds: ["preview-sample"]));

        Assert.Equal(
            PluginRepository.GetCatalogStorePath(_tempRoot, PluginCatalogChannel.Preview),
            writtenPath);
        Assert.Equal(stableSentinel, File.ReadAllText(stablePath));
        var previewStore = PluginRepository.ReadJsonFile<StoreDocument>(writtenPath);
        var previewEntry = Assert.Single(previewStore.Plugins);
        Assert.Equal("preview-sample", previewEntry.Id);
        Assert.Equal(PreviewVersion, previewEntry.Version);
    }

    [Fact]
    public void Generate_StableChannel_SkipsPrereleaseWhenSelectingAll()
    {
        CreatePluginFolder(
            folderName: "PreviewSample",
            pluginId: "preview-sample",
            version: PreviewVersion);

        var store = Generate(PluginCatalogChannel.Stable, StableCatalogUrl);

        Assert.Empty(store.Plugins);
    }

    [Fact]
    public async Task Validation_ReportsInconsistentPreviewVersionsAcrossFiles()
    {
        CreatePluginFolder(
            folderName: "Inconsistent",
            pluginId: "inconsistent-plugin",
            version: PreviewVersion,
            legacyVersion: StableVersion,
            projectVersion: StableVersion,
            attributeVersion: StableVersion);

        var report = await ValidateAsync("inconsistent-plugin");
        var failureMessages = report.Steps
            .Where(step => string.Equals(step.Status, "FAIL", StringComparison.Ordinal))
            .Select(step => step.Message)
            .ToArray();

        Assert.Contains(
            failureMessages,
            message => message.Contains(
                "plugin.json version does not match plugin.manifest.json version",
                StringComparison.Ordinal));
        Assert.Contains(
            failureMessages,
            message => message.Contains(
                "Project Version does not match plugin.manifest.json version",
                StringComparison.Ordinal));
        Assert.Contains(
            failureMessages,
            message => message.Contains(
                "[Plugin] attribute version does not match plugin.manifest.json version",
                StringComparison.Ordinal));
    }

    private string CreatePluginFolder(
        string folderName,
        string pluginId,
        string version,
        string minHostVersion = MinimumHostVersion,
        string? legacyVersion = null,
        string? projectVersion = null,
        string? attributeVersion = null,
        string? changelog = null)
    {
        legacyVersion ??= version;
        projectVersion ??= version;
        attributeVersion ??= version;

        var pluginDirectory = Path.Combine(_tempRoot, "Official", folderName);
        Directory.CreateDirectory(pluginDirectory);

        var unifiedManifest = new UnifiedPluginManifest
        {
            Id = pluginId,
            Name = $"{folderName} Plugin",
            Version = version,
            MinHostVersion = minHostVersion,
            Author = "SSC-STUDIO",
            Repository = "https://example.com/repo",
            Issues = "https://example.com/issues",
            Lifecycle = PluginLifecycleStatus.Active,
            Package = new PluginPackageMetadata
            {
                AssetName = $"{pluginId}-v{version}.zip",
                RequiredFiles =
                [
                    $"UniversalDeviceToolkit.Plugins.{folderName}.dll",
                    "plugin.json",
                    "plugin.manifest.json",
                ],
            },
            Store = new PluginStoreMetadata
            {
                Description = $"{folderName} plugin description.",
                Icon = "PuzzlePiece24",
                IconBackground = "#2563EB",
                Tags = ["sample"],
                Dependencies = [],
                SupportedLanguages = ["en"],
                RepositoryUrl = "https://example.com/repo",
            },
        };
        PluginRepository.WriteJsonFile(
            Path.Combine(pluginDirectory, "plugin.manifest.json"),
            unifiedManifest);
        PluginRepository.WriteJsonFile(
            Path.Combine(pluginDirectory, "plugin.json"),
            new PluginManifest(
                pluginId,
                unifiedManifest.Name,
                legacyVersion,
                minHostVersion,
                unifiedManifest.Author,
                false,
                unifiedManifest.Repository,
                unifiedManifest.Issues));

        var numericProjectVersion = PluginVersionSynchronizer.IsPluginVersion(projectVersion)
            ? PluginVersionSynchronizer.ToNumericVersion(projectVersion)
            : projectVersion;
        File.WriteAllText(
            Path.Combine(
                pluginDirectory,
                $"UniversalDeviceToolkit.Plugins.{folderName}.csproj"),
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <Version>{{projectVersion}}</Version>
                <FileVersion>{{numericProjectVersion}}</FileVersion>
                <AssemblyVersion>{{numericProjectVersion}}</AssemblyVersion>
                <AssemblyName>UniversalDeviceToolkit.Plugins.{{folderName}}</AssemblyName>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(pluginDirectory, $"{folderName}Plugin.cs"),
            $$"""
            namespace UniversalDeviceToolkit.Plugins.{{folderName}};

            [Plugin(
                id: "{{pluginId}}",
                name: "{{unifiedManifest.Name}}",
                version: "{{attributeVersion}}",
                description: "Test plugin",
                author: "SSC-STUDIO",
                MinimumHostVersion = "{{minHostVersion}}",
                Icon = "PuzzlePiece24"
            )]
            public sealed class {{folderName}}Plugin
            {
            }
            """);
        File.WriteAllText(
            Path.Combine(pluginDirectory, "CHANGELOG.md"),
            changelog ?? $"# {unifiedManifest.Name}\n\n## [{version}]\n");

        return pluginDirectory;
    }

    private StoreDocument Generate(
        PluginCatalogChannel channel,
        string releaseUrl,
        IReadOnlyList<string>? pluginIds = null,
        bool mergeExisting = false)
    {
        return new StoreJsonGenerator().Generate(
            CreateStoreRequest(
                channel,
                releaseUrl,
                pluginIds,
                mergeExisting));
    }

    private StoreGenerationRequest CreateStoreRequest(
        PluginCatalogChannel channel,
        string releaseUrl,
        IReadOnlyList<string>? pluginIds = null,
        bool mergeExisting = false,
        string? outputPath = null,
        DateTimeOffset? releaseDate = null)
    {
        return new StoreGenerationRequest
        {
            RepositoryRoot = _tempRoot,
            OutputPath = outputPath,
            AssetRoot = Path.Combine(_tempRoot, ".build", "release-assets"),
            ReleaseRepositoryUrl = releaseUrl,
            PluginIds = pluginIds ?? Array.Empty<string>(),
            ReleaseDate = releaseDate,
            MergeExisting = mergeExisting,
            RequireAssets = false,
            CatalogChannel = channel,
        };
    }

    private void WriteExistingStore(PluginCatalogChannel channel, params StorePluginEntry[] entries)
    {
        PluginRepository.WriteJsonFile(
            PluginRepository.GetCatalogStorePath(_tempRoot, channel),
            new StoreDocument
            {
                LastUpdated = "2026-08-13T00:00:00.0000000+00:00",
                StoreVersion = "1.0.0",
                Plugins = entries.ToList(),
            });
    }

    private async Task<ValidationReport> ValidateAsync(
        string pluginId,
        PluginValidationProfile profile = PluginValidationProfile.Contributor,
        PluginCatalogChannel catalogChannel = PluginCatalogChannel.Stable)
    {
        return await new PluginValidationService(_ => { }).RunAsync(
            new ValidationRequest
            {
                RepositoryRoot = _tempRoot,
                SkipBuild = true,
                SkipTests = true,
                Profile = profile,
                CatalogChannel = catalogChannel,
                PluginIds = [pluginId],
            });
    }
}
