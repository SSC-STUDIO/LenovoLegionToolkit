using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginRepositoryServiceTests : TemporaryFileTestBase
{
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly string? _originalAppDataOverride;

    private const string StoreResponseJson = """
    {
      "lastUpdated": "2026-04-19T10:00:00Z",
      "plugins": [
        {
          "id": "shell-integration",
          "name": "Shell Integration",
          "description": "Adds shell integration",
          "author": "LLT Team",
          "version": "1.0.9",
          "minimumHostVersion": "3.6.1",
          "downloadUrl": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/download/shell-integration-v1.0.9/shell-integration-v1.0.9.zip"
        }
      ]
    }
    """;

    public PluginRepositoryServiceTests()
    {
        _originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
    }

    public override void Dispose()
    {
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _originalAppDataOverride);
        base.Dispose();
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_ShouldRetryTransientStoreFailureAndSucceed()
    {
        // Arrange
        var attempts = 0;
        var seenVersions = new List<Version>();
        using var service = CreateService(request =>
        {
            request.RequestUri.Should().NotBeNull();
            request.RequestUri!.AbsoluteUri.Should().StartWith("https://cdn.jsdelivr.net/gh/SSC-STUDIO/UniversalDeviceToolkit-Plugins@master/store.json");
            seenVersions.Add(request.Version);

            attempts++;
            if (attempts == 1)
                throw new HttpRequestException("Connection reset by peer.");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(StoreResponseJson)
            };
        });

        // Act
        var plugins = await service.FetchAvailablePluginsAsync();

        // Assert
        plugins.Should().ContainSingle(plugin => plugin.Id == "shell-integration");
        attempts.Should().Be(2);
        seenVersions.Should().OnlyContain(version => version == HttpVersion.Version11);
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_ShouldPreferMirrorBeforeRawGithubSources()
    {
        // Arrange
        var requestedUrls = new List<string>();
        using var service = CreateService(request =>
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
            requestedUrls.Add(url);

            if (url.Contains("cdn.jsdelivr.net", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(StoreResponseJson)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        // Act
        var plugins = await service.FetchAvailablePluginsAsync();

        // Assert
        plugins.Should().ContainSingle();
        requestedUrls.Should().NotBeEmpty();
        requestedUrls[0].Should().Contain("cdn.jsdelivr.net/gh/SSC-STUDIO/UniversalDeviceToolkit-Plugins@master/store.json");
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_ShouldFallbackToMirrorWhenPrimarySourcesFail()
    {
        // Arrange
        var requestedUrls = new List<string>();
        using var service = CreateService(request =>
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
            requestedUrls.Add(url);

            if (url.Contains("cdn.jsdelivr.net", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(StoreResponseJson)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        // Act
        var plugins = await service.FetchAvailablePluginsAsync();

        // Assert
        plugins.Should().ContainSingle(plugin => plugin.Id == "shell-integration");
        requestedUrls.Should().Contain(url => url.Contains("cdn.jsdelivr.net/gh/SSC-STUDIO/UniversalDeviceToolkit-Plugins@master/store.json", StringComparison.OrdinalIgnoreCase));
        requestedUrls.Should().NotContain(url => url.Contains("raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/master/store.json", StringComparison.OrdinalIgnoreCase));
        requestedUrls.Should().NotContain(url => url.Contains("raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/refs/heads/master/store.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_ShouldUseCachedStoreWhenRemoteSourcesFail()
    {
        // Arrange
        using (var seedingService = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
               {
                   Content = new StringContent(StoreResponseJson)
               }))
        {
            var seededPlugins = await seedingService.FetchAvailablePluginsAsync();
            seededPlugins.Should().ContainSingle(plugin => plugin.Id == "shell-integration");
        }

        using var fallbackService = CreateService(_ => throw new HttpRequestException("Network unavailable."));

        // Act
        var plugins = await fallbackService.FetchAvailablePluginsAsync();

        // Assert
        plugins.Should().ContainSingle(plugin => plugin.Id == "shell-integration");
    }

    [Fact]
    public void LocalPackageFallbackVersionGate_ShouldRejectOlderLocalPackage()
    {
        // Arrange
        var method = typeof(PluginRepositoryService).GetMethod(
            "IsLocalPackageVersionUsableForFallback",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, ["1.0.16", "1.0.15"]);

        // Assert
        result.Should().Be(false);
    }

    [Theory]
    [InlineData("1.0.16", "1.0.16")]
    [InlineData("1.0.16", "1.0.17")]
    public void LocalPackageFallbackVersionGate_ShouldAcceptSameOrNewerLocalPackage(string requestedVersion, string localVersion)
    {
        // Arrange
        var method = typeof(PluginRepositoryService).GetMethod(
            "IsLocalPackageVersionUsableForFallback",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, [requestedVersion, localVersion]);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void NativeCurlDownloadArguments_ShouldUseBestEffortRevocationOnWindows()
    {
        // Arrange
        var method = typeof(PluginRepositoryService).GetMethod(
            "AddNativeCurlDownloadArguments",
            BindingFlags.NonPublic | BindingFlags.Static);
        var startInfo = new System.Diagnostics.ProcessStartInfo();

        // Act
        method!.Invoke(null, [startInfo, @"C:\Temp\plugin.zip", "https://github.com/example/repo/releases/download/v1/plugin.zip"]);

        // Assert
        var arguments = startInfo.ArgumentList.ToArray();
        arguments.Should().ContainInOrder("--location", "--fail", "--silent", "--show-error");
        arguments.Should().Contain("--output");
        arguments.Should().Contain(@"C:\Temp\plugin.zip");
        arguments.Should().Contain("https://github.com/example/repo/releases/download/v1/plugin.zip");

        if (OperatingSystem.IsWindows())
            arguments.Should().Contain("--ssl-revoke-best-effort");
        else
            arguments.Should().NotContain("--ssl-revoke-best-effort");
    }

    [Theory]
    [InlineData("https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", true)]
    [InlineData("https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/latest/download/custom-mouse-v1.0.16.zip", true)]
    [InlineData("https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/assets/123456", true)]
    [InlineData("https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", true)]
    [InlineData("file:///C:/Temp/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://example.com/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://github.com/SomeoneElse/UniversalDeviceToolkit-Plugins/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://cdn.jsdelivr.net/gh/SSC-STUDIO/UniversalDeviceToolkit-Plugins@master/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", false)]
    public void ShouldTrustDownloadedPluginPackage_ShouldOnlyTrustOfficialGitHubReleaseAssets(string candidateUrl, bool expected)
    {
        // Arrange
        var method = typeof(PluginRepositoryService).GetMethod(
            "ShouldTrustDownloadedPluginPackage",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, [candidateUrl, "custom-mouse"]);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WhenRuntimeDoesNotLoad_ShouldRollbackInstalledState()
    {
        // Arrange
        const string pluginId = "broken-runtime";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        var scanCalls = new List<bool>();

        _pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Callback<bool>(forceRefresh => scanCalls.Add(forceRefresh))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out It.Ref<IPlugin?>.IsAny))
            .Returns(false);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        // Assert
        installed.Should().BeFalse();
        scanCalls.Should().ContainSingle().Which.Should().BeTrue();
        _pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Never);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WhenRuntimeLoads_ShouldKeepPluginInstalled()
    {
        // Arrange
        const string pluginId = "working-runtime";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        var plugin = MockFactory.CreateMockPlugin(id: pluginId);

        _pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out plugin))
            .Returns(true);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        // Assert
        installed.Should().BeTrue();
        _pluginManager.Verify(manager => manager.ScanAndLoadPluginsAsync(true), Times.Exactly(2));
        _pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WhenRuntimeLoads_ShouldRefreshBeforeMarkingPluginInstalled()
    {
        const string pluginId = "ordered-runtime";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        var plugin = MockFactory.CreateMockPlugin(id: pluginId);
        var sequence = new MockSequence();

        _pluginManager.InSequence(sequence)
            .Setup(manager => manager.ScanAndLoadPluginsAsync(true))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out plugin))
            .Returns(true);
        _pluginManager.InSequence(sequence)
            .Setup(manager => manager.InstallPlugin(pluginId));
        _pluginManager.InSequence(sequence)
            .Setup(manager => manager.ScanAndLoadPluginsAsync(true))
            .Returns(Task.CompletedTask);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        installed.Should().BeTrue();
        _pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
        _pluginManager.Verify(manager => manager.ScanAndLoadPluginsAsync(true), Times.Exactly(2));
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithManifestOptimizationActionsOnly_ShouldKeepManifestPluginInstalled()
    {
        // Arrange
        const string pluginId = "manifest-optimization";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: true);
        var manifest = CreateInstallManifest(pluginId, packagePath, includeOptimizationAction: true);

        _pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out It.Ref<IPlugin?>.IsAny))
            .Returns(false);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        // Assert
        installed.Should().BeTrue();
        _pluginManager.Verify(manager => manager.ScanAndLoadPluginsAsync(true), Times.Exactly(2));
        _pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithManifestSettingsPageOnly_ShouldKeepManifestPluginInstalled()
    {
        // Arrange
        const string pluginId = "user-feedback";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.Contributes = new PluginManifestContributions
        {
            SettingsPage = new PluginManifestPageContribution
            {
                Class = "UserFeedback.Settings",
                Title = "Feedback"
            }
        };

        _pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out It.Ref<IPlugin?>.IsAny))
            .Returns(false);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        // Assert
        installed.Should().BeTrue();
        _pluginManager.Verify(manager => manager.ScanAndLoadPluginsAsync(true), Times.Exactly(2));
        _pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithStoreOptimizationActionKeyOnly_ShouldKeepManifestPluginInstalled()
    {
        // Arrange
        const string pluginId = "store-optimization-key";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath, includeOptimizationAction: true, useOptimizationActionKey: true);

        _pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out It.Ref<IPlugin?>.IsAny))
            .Returns(false);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        // Assert
        installed.Should().BeTrue();
        _pluginManager.Verify(manager => manager.ScanAndLoadPluginsAsync(true), Times.Exactly(2));
        _pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithStoreOptimizationActionsOnly_ShouldKeepManifestPluginInstalled()
    {
        // Arrange
        const string pluginId = "store-optimization";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath, includeOptimizationAction: true);

        _pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out It.Ref<IPlugin?>.IsAny))
            .Returns(false);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        // Assert
        installed.Should().BeTrue();
        _pluginManager.Verify(manager => manager.ScanAndLoadPluginsAsync(true), Times.Exactly(2));
        _pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    private PluginRepositoryService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory));
        var httpClientFactory = new StubHttpClientFactory(httpClient);
        return new PluginRepositoryService(_pluginManager.Object, httpClientFactory);
    }

    private string CreatePluginPackage(string pluginId, bool includeOptimizationAction)
    {
        var packageDirectory = CreateTempDirectory();
        var pluginDirectory = Path.Combine(packageDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        File.WriteAllText(Path.Combine(pluginDirectory, $"{pluginId}.dll"), "fake plugin dll");
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.json"),
            CreateManifestJson(pluginId, includeOptimizationAction));

        var packagePath = Path.Combine(packageDirectory, $"{pluginId}.zip");
        ZipFile.CreateFromDirectory(pluginDirectory, packagePath);
        TempFiles.Add(packagePath);
        return packagePath;
    }

    private static PluginManifest CreateInstallManifest(
        string pluginId,
        string packagePath,
        bool includeOptimizationAction = false,
        bool useOptimizationActionKey = false)
    {
        var manifest = new PluginManifest
        {
            Id = pluginId,
            Name = pluginId,
            Description = "Test plugin",
            Version = "1.0.0",
            MinimumHostVersion = "1.0.0",
            DownloadUrl = new Uri(packagePath).AbsoluteUri
        };

        if (includeOptimizationAction)
        {
            manifest.Contributes = new PluginManifestContributions
            {
                OptimizationActions =
                [
                    new PluginManifestOptimizationContribution
                    {
                        Id = useOptimizationActionKey ? string.Empty : "apply-test",
                        Key = useOptimizationActionKey ? "apply-test" : string.Empty,
                        Title = "Apply test"
                    }
                ]
            };
        }

        return manifest;
    }

    private static string CreateManifestJson(string pluginId, bool includeOptimizationAction)
    {
        if (!includeOptimizationAction)
        {
            return $$"""
            {
              "id": "{{pluginId}}",
              "name": "{{pluginId}}",
              "description": "Test plugin"
            }
            """;
        }

        return $$"""
        {
          "id": "{{pluginId}}",
          "name": "{{pluginId}}",
          "description": "Test plugin",
          "contributes": {
            "optimizationActions": [
              {
                "id": "apply-test",
                "title": "Apply test"
              }
            ]
          }
        }
        """;
    }

    private sealed class StubHttpClientFactory(HttpClient client) : HttpClientFactory
    {
        public override HttpClient Create() => client;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }
}
