using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Tests.Infrastructure;
using TestMockFactory = UniversalDeviceToolkit.Tests.Infrastructure.MockFactory;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
[Collection(TestCollections.ProcessState)]
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
          "author": "UDT Team",
          "version": "1.0.9",
          "minimumHostVersion": "3.6.1",
          "downloadUrl": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/shell-integration-v1.0.9.zip"
        }
      ]
    }
    """;

    public PluginRepositoryServiceTests()
    {
        _originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        _pluginManager
            .Setup(manager => manager.AcquirePluginMutation(It.IsAny<string>()))
            .Returns(Mock.Of<IDisposable>());
        _pluginManager
            .Setup(manager => manager.CapturePluginRuntimeSnapshot())
            .Returns(new PluginRuntimeSnapshot(
                new Dictionary<string, PluginRuntimeIdentity>(StringComparer.OrdinalIgnoreCase)));
        _pluginManager
            .Setup(manager => manager.ForgetPluginRuntime(
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(true);
        _pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.LoadPluginRuntimeStrictAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.CommitPluginInstallation(
                It.IsAny<string>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()))
            .Returns((string id, IDisposable? _, Action? coordinatedCommit) =>
            {
                coordinatedCommit?.Invoke();
                return new PluginInstallationStateSnapshot(id, false, false);
            });
    }

    public override void Dispose()
    {
        // PluginRepositoryService logs to the process singleton. Release its async
        // file sink while the temporary AppData directory is still in scope.
        Log.ResetForTests();
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _originalAppDataOverride);
        base.Dispose();
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_ShouldRetryTransientCatalogFailureAndSucceed()
    {
        // Arrange
        var attempts = 0;
        var seenVersions = new List<Version>();
        using var service = CreateService(request =>
        {
            request.RequestUri.Should().NotBeNull();
            request.RequestUri!.AbsoluteUri.Should().Be("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/store.json");
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
        var plugins = await service.FetchAvailablePluginsAsync(forceRefresh: true);

        // Assert
        plugins.Should().ContainSingle(plugin => plugin.Id == "shell-integration");
        attempts.Should().Be(2);
        seenVersions.Should().OnlyContain(version => version == HttpVersion.Version11);
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_ShouldUseMainRepositoryCatalog()
    {
        // Arrange
        var requestedUrls = new List<string>();
        using var service = CreateService(request =>
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
            requestedUrls.Add(url);

            if (url.Equals("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/store.json", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(StoreResponseJson)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        // Act
        var plugins = await service.FetchAvailablePluginsAsync(forceRefresh: true);

        // Assert
        plugins.Should().ContainSingle();
        requestedUrls.Should().NotBeEmpty();
        requestedUrls.Should().ContainSingle("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/store.json");
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_PreviewHost_ShouldReadPreviewCatalog()
    {
        var requestedUrls = new List<string>();
        using var service = CreateService(
            request =>
            {
                var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
                requestedUrls.Add(url);

                if (url.Equals(
                    "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog-preview/store.json",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(StoreResponseJson)
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            },
            informationalVersion: "6.0.0-preview.1");

        var plugins = await service.FetchAvailablePluginsAsync(forceRefresh: true);

        plugins.Should().ContainSingle();
        requestedUrls.Should().ContainSingle(
            "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog-preview/store.json");
        requestedUrls.Should().NotContain(
            "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/store.json");
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_ShouldPreserveWebPageContributionThroughCacheClone()
    {
        const string storeJson = """
        {
          "lastUpdated": "2026-08-16T00:00:00Z",
          "plugins": [
            {
              "id": "custom-mouse",
              "name": "Custom Mouse",
              "version": "1.0.0",
              "minimumHostVersion": "1.0.0",
              "status": "Active",
              "contributes": {
                "webPage": { "entry": "web/index.html" },
                "settingsPage": { "class": "CustomMouse.Settings", "title": "Mouse" }
              }
            }
          ]
        }
        """;
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(storeJson)
        });

        var plugins = await service.FetchAvailablePluginsAsync(forceRefresh: true);

        plugins.Should().ContainSingle();
        plugins[0].Status.Should().Be("Active");
        plugins[0].Contributes.Should().NotBeNull();
        plugins[0].Contributes!.WebPage.Should().NotBeNull();
        plugins[0].Contributes!.WebPage!.Entry.Should().Be("web/index.html");
        plugins[0].Contributes!.SettingsPage!.Class.Should().Be("CustomMouse.Settings");

        service.TryGetCachedAvailablePlugins(out var cached).Should().BeTrue();
        cached.Should().ContainSingle();
        cached![0].Contributes!.WebPage!.Entry.Should().Be("web/index.html");

        plugins[0].Contributes!.WebPage!.Entry = "mutated.html";
        service.TryGetCachedAvailablePlugins(out var cachedAgain).Should().BeTrue();
        cachedAgain![0].Contributes!.WebPage!.Entry.Should().Be("web/index.html");
    }

    [Theory]
    [InlineData("")]
    [InlineData("{not-json")]
    public async Task FetchAvailablePluginsAsync_WithEmptyOrMalformedPayload_ShouldRejectCatalog(string payload)
    {
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload)
        });

        Func<Task> act = async () => await service.FetchAvailablePluginsAsync(forceRefresh: true);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_WithNoPluginEntries_ShouldReturnAndCacheEmptyCatalog()
    {
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"plugins":[]}""")
        });

        var plugins = await service.FetchAvailablePluginsAsync(forceRefresh: true);

        plugins.Should().BeEmpty();
        service.TryGetCachedAvailablePlugins(out var cachedPlugins).Should().BeTrue();
        cachedPlugins.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchAvailablePluginsAsync_ShouldNotUseLegacyPluginRepository()
    {
        // Arrange
        var requestedUrls = new List<string>();
        using var service = CreateService(request =>
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
            requestedUrls.Add(url);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        // Act
        var act = async () => await service.FetchAvailablePluginsAsync(forceRefresh: true);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        requestedUrls.Should().ContainSingle("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/store.json");
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
    public async Task FetchAvailablePluginsAsync_ShouldRejectV1StoreCacheAfterCacheSeedRotation()
    {
        // Arrange: v1 used a different HMAC seed. It must not become a valid v2 cache
        // merely because the cache file is still present on disk.
        using var service = CreateService(_ => throw new HttpRequestException("Network unavailable."));
        var cachePath = GetPrivateField<string>(service, "_storeCachePath");
        var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(StoreResponseJson));

        using var keyDerivation = new HMACSHA256(Encoding.UTF8.GetBytes("UDT_PluginStoreCache_v1"));
        var legacyKey = keyDerivation.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
        string legacyHmac;
        using (var hmac = new HMACSHA256(legacyKey))
        {
            legacyHmac = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
        }

        File.WriteAllText(
            cachePath,
            JsonSerializer.Serialize(new { Data = data, Hmac = legacyHmac }),
            Encoding.UTF8);

        // Act
        var act = async () => await service.FetchAvailablePluginsAsync();

        // Assert: the stale v1 cache is ignored and the failed remote fetch is surfaced.
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task TryResolvePublishedAssetAsync_ShouldUseExactVersionFromFixedCatalogRelease()
    {
        // Arrange
        var requestedUrls = new List<string>();
        const string releaseJson = """
        {
          "tag_name": "plugin-catalog",
          "draft": false,
          "prerelease": false,
          "assets": [
            {
              "name": "custom-mouse-v9.9.9.zip",
              "browser_download_url": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/custom-mouse-v9.9.9.zip",
              "url": "https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit/releases/assets/999"
            },
            {
              "name": "custom-mouse-v1.0.16.zip",
              "browser_download_url": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/custom-mouse-v1.0.16.zip",
              "url": "https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit/releases/assets/1016"
            }
          ]
        }
        """;

        using var service = CreateService(request =>
        {
            requestedUrls.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(releaseJson)
            };
        });
        var manifest = new PluginManifest
        {
            Id = "custom-mouse",
            Version = "1.0.16"
        };
        var method = typeof(PluginRepositoryService).GetMethod(
            "TryResolvePublishedAssetAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act
        var task = (Task)method!.Invoke(service, [manifest])!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().GetProperty("AssetName")!.GetValue(result)
            .Should().Be("custom-mouse-v1.0.16.zip");
        manifest.Version.Should().Be("1.0.16");
        requestedUrls.Should().ContainSingle(
            "https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit/releases/tags/plugin-catalog");
    }

    [Fact]
    public async Task TryResolvePublishedAssetAsync_StableHost_ShouldIgnorePrereleaseCatalogRelease()
    {
        const string releaseJson = """
        {
          "tag_name": "plugin-catalog",
          "draft": false,
          "prerelease": true,
          "assets": [
            {
              "name": "custom-mouse-v1.0.16.zip",
              "browser_download_url": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/custom-mouse-v1.0.16.zip"
            }
          ]
        }
        """;
        var requestedUrls = new List<string>();
        using var service = CreateService(
            request =>
            {
                requestedUrls.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(releaseJson)
                };
            },
            informationalVersion: "5.0.2");
        var manifest = new PluginManifest
        {
            Id = "custom-mouse",
            Version = "1.0.16"
        };

        var result = await InvokeTryResolvePublishedAssetAsync(service, manifest);

        result.Should().BeNull();
        requestedUrls.Should().NotBeEmpty();
        requestedUrls.Should().OnlyContain(url =>
            url == "https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit/releases/tags/plugin-catalog");
    }

    [Fact]
    public async Task TryResolvePublishedAssetAsync_PreviewHost_ShouldTrustPrereleasePreviewCatalogAsset()
    {
        const string browserDownloadUrl =
            "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog-preview/custom-mouse-v2.0.0-preview.2.zip";
        const string releaseJson = """
        {
          "tag_name": "plugin-catalog-preview",
          "draft": false,
          "prerelease": true,
          "assets": [
            {
              "name": "custom-mouse-v2.0.0-preview.2.zip",
              "browser_download_url": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog-preview/custom-mouse-v2.0.0-preview.2.zip"
            }
          ]
        }
        """;
        var requestedUrls = new List<string>();
        using var service = CreateService(
            request =>
            {
                requestedUrls.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(releaseJson)
                };
            },
            informationalVersion: "6.0.0-preview.1");
        var manifest = new PluginManifest
        {
            Id = "custom-mouse",
            Version = "2.0.0-preview.2"
        };

        var result = await InvokeTryResolvePublishedAssetAsync(service, manifest);

        result.Should().NotBeNull();
        var resultType = result!.GetType();
        resultType.GetProperty("DownloadUrl")!.GetValue(result)
            .Should().Be(browserDownloadUrl);
        resultType.GetProperty("AssetName")!.GetValue(result)
            .Should().Be("custom-mouse-v2.0.0-preview.2.zip");
        requestedUrls.Should().ContainSingle(
            "https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit/releases/tags/plugin-catalog-preview");
    }

    [Theory]
    [InlineData("5.0.2", "1.0.9", "1.1.0", "5.0.2", true)]
    [InlineData("6.0.0-preview.1", "2.0.0-preview.1", "2.0.0-preview.2", "6.0.0", true)]
    [InlineData("6.0.0-preview.1", "2.0.0-preview.2", "2.0.0", "6.0.0", true)]
    [InlineData("6.0.0-preview.1", "2.0.0", "2.0.0-preview.2", "6.0.0", false)]
    public async Task CheckForUpdatesAsync_ShouldRespectStableAndPrereleaseVersionOrdering(
        string informationalVersion,
        string installedVersion,
        string availableVersion,
        string minimumHostVersion,
        bool shouldUpdate)
    {
        var storeJson = CreateStoreResponseJson(availableVersion, minimumHostVersion);
        using var service = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(storeJson)
            },
            informationalVersion);
        var installed = new List<PluginManifest>
        {
            new()
            {
                Id = "channel-plugin",
                Version = installedVersion
            }
        };

        var updates = await service.CheckForUpdatesAsync(installed, forceRefresh: true);

        if (!shouldUpdate)
        {
            updates.Should().BeEmpty();
            return;
        }

        var update = updates.Should().ContainSingle().Which;
        update.Version.Should().Be(availableVersion);
        update.MinimumHostVersion.Should().Be(minimumHostVersion);
        new VersionChecker(informationalVersion)
            .IsCompatible(update.MinimumHostVersion)
            .Should().BeTrue();
        update.DownloadUrl.Should().Be(
            PluginCatalogTags.PackageDownloadUrl(
                PluginCatalogTags.ResolveTag(informationalVersion),
                update.Id,
                update.Version));
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
    [InlineData("v1.0.16", "1.0.16")]
    [InlineData("1.0.16", "v1.0.17")]
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

    [Theory]
    [InlineData("v1.0.16", "1.0.15")]
    [InlineData("1.0.16", "v1.0.15")]
    public void LocalPackageFallbackVersionGate_ShouldRejectOlderLocalPackage_WithVPrefix(string requestedVersion, string localVersion)
    {
        var method = typeof(PluginRepositoryService).GetMethod(
            "IsLocalPackageVersionUsableForFallback",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = method!.Invoke(null, [requestedVersion, localVersion]);

        result.Should().Be(false);
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
    [InlineData("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/custom-mouse-v1.0.16.zip", true)]
    [InlineData("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog-preview/custom-mouse-v1.0.16.zip", true)]
    [InlineData("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest/download/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/custom-mouse-v9.9.9.zip", false)]
    [InlineData("https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit/releases/assets/123456", false)]
    [InlineData("https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit/releases/assets/not-a-number", false)]
    [InlineData("https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://gh-proxy.com/https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/custom-mouse-v1.0.16.zip", true)]
    [InlineData("https://ghfast.top/https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/custom-mouse-v1.0.16.zip", true)]
    [InlineData("https://gh-proxy.com/https://example.com/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://gh-proxy.com/https://github.com/SomeoneElse/UniversalDeviceToolkit/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", false)]
    [InlineData("file:///C:/Temp/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://example.com/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://github.com/SomeoneElse/UniversalDeviceToolkit/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", false)]
    [InlineData("https://cdn.jsdelivr.net/gh/SSC-STUDIO/UniversalDeviceToolkit@master/releases/download/custom-mouse-v1.0.16/custom-mouse-v1.0.16.zip", false)]
    public void ShouldTrustDownloadedPluginPackage_ShouldOnlyTrustOfficialGitHubReleaseAssets(string candidateUrl, bool expected)
    {
        // Arrange
        var method = typeof(PluginRepositoryService).GetMethod(
            "ShouldTrustDownloadedPluginPackage",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, [candidateUrl, "custom-mouse", "1.0.16"]);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsTrustedGitHubReleaseAssetApiPath_ShouldRequireTheExactMetadataAssetName()
    {
        var method = typeof(PluginRepositoryService).GetMethod(
            "IsTrustedGitHubReleaseAssetApiPath",
            BindingFlags.NonPublic | BindingFlags.Static);
        var segments = new[]
        {
            "repos",
            "SSC-STUDIO",
            "UniversalDeviceToolkit",
            "releases",
            "assets",
            "123456"
        };

        var exact = method!.Invoke(
            null,
            [segments, "custom-mouse-v1.0.16.zip", "custom-mouse", "1.0.16"]);
        var wrongName = method.Invoke(
            null,
            [segments, "custom-mouse-v9.9.9.zip", "custom-mouse", "1.0.16"]);
        var invalidAssetId = method.Invoke(
            null,
            [new[] { "repos", "SSC-STUDIO", "UniversalDeviceToolkit", "releases", "assets", "-1" }, "custom-mouse-v1.0.16.zip", "custom-mouse", "1.0.16"]);

        exact.Should().Be(true);
        wrongName.Should().Be(false);
        invalidAssetId.Should().Be(false);
    }

    [Fact]
    public async Task VerifyDownloadedPackageIntegrityAsync_WithMatchingZipHash_ShouldSucceed()
    {
        const string pluginId = "zip-integrity";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);

        var method = typeof(PluginRepositoryService).GetMethod(
            "VerifyDownloadedPackageIntegrityAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        // trustAsOfficialOnlinePackage: false — DEBUG may skip missing hashes; matching hash still passes.
        var result = await (Task<bool>)method!.Invoke(null, [packagePath, manifest, false])!;

        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyDownloadedPackageIntegrityAsync_WithMismatchedZipHash_ShouldFail()
    {
        const string pluginId = "zip-integrity-fail";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = new string('a', 64);

        var method = typeof(PluginRepositoryService).GetMethod(
            "VerifyDownloadedPackageIntegrityAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = await (Task<bool>)method!.Invoke(null, [packagePath, manifest, false])!;

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyDownloadedPackageIntegrityAsync_OfficialPackageMissingZipHash_ShouldFail()
    {
        const string pluginId = "zip-integrity-official-missing";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = string.Empty;

        var method = typeof(PluginRepositoryService).GetMethod(
            "VerifyDownloadedPackageIntegrityAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Official online packages must fail closed without zipHash even in DEBUG.
        var result = await (Task<bool>)method!.Invoke(null, [packagePath, manifest, true])!;

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithMismatchedFileHash_ShouldFail()
    {
        const string pluginId = "dll-integrity-fail";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = new string('b', 64);

        _pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var outcome = await service.DownloadAndInstallPluginWithOutcomeAsync(manifest);

        outcome.Success.Should().BeFalse();
        outcome.Degraded.Should().BeFalse();
        outcome.Error.Should().NotBeNullOrWhiteSpace();
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithMismatchedZipHash_ShouldCleanTemporaryArtifacts()
    {
        const string pluginId = "zip-cleanup-fail";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = new string('c', 64);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var tempDirectory = GetPrivateField<string>(service, "_tempDownloadDirectory");

        var outcome = await service.DownloadAndInstallPluginWithOutcomeAsync(manifest);

        outcome.Success.Should().BeFalse();
        outcome.Degraded.Should().BeFalse();
        outcome.Error.Should().NotBeNullOrWhiteSpace();
        File.Exists(Path.Combine(tempDirectory, $"{pluginId}.zip")).Should().BeFalse();
        Directory.Exists(Path.Combine(tempDirectory, pluginId)).Should().BeFalse();
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithInvalidPluginId_ShouldRejectBeforeDownloading()
    {
        var requestCount = 0;
        var manifest = new PluginManifest
        {
            Id = "../escape-plugin",
            Name = "Invalid plugin",
            Version = "1.0.0",
            MinimumHostVersion = "1.0.0",
            DownloadUrl = "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/escape.zip"
        };

        using var service = CreateService(_ =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        installed.Should().BeFalse();
        requestCount.Should().Be(0);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithMatchingIntegrityHashes_ShouldKeepPluginInstalled()
    {
        const string pluginId = "integrity-pass";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var plugin = TestMockFactory.CreateMockPlugin(id: pluginId);

        _pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out plugin))
            .Returns(true);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        installed.Should().BeTrue();
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Once);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WhenRuntimeDoesNotLoad_ShouldRollbackInstalledState()
    {
        // Arrange
        const string pluginId = "broken-runtime";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        _pluginManager
            .Setup(manager => manager.LoadPluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .ThrowsAsync(new InvalidOperationException("Runtime activation failed."));
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out It.Ref<IPlugin?>.IsAny))
            .Returns(false);

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        // Assert
        installed.Should().BeFalse();
        _pluginManager.Verify(
            manager => manager.LoadPluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()),
            Times.Once);
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Never);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WhenRuntimeLoads_ShouldKeepPluginInstalled()
    {
        // Arrange
        const string pluginId = "working-runtime";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var plugin = TestMockFactory.CreateMockPlugin(id: pluginId);

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
        _pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()),
            Times.Once);
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task RepositoryCompletion_WhenFirstSubscriberThrows_ShouldNotifyLaterSubscribersOnce()
    {
        const string pluginId = "completion-subscriber-isolation";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var plugin = TestMockFactory.CreateMockPlugin(id: pluginId);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out plugin))
            .Returns(true);
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var completions = new List<string>();
        service.DownloadCompleted += (_, _) =>
            throw new InvalidOperationException("first completion subscriber failed");
        service.DownloadCompleted += (_, id) => completions.Add($"second:{id}");
        service.DownloadCompleted += (_, id) => completions.Add($"third:{id}");

        var outcome = await service.DownloadAndInstallPluginWithOutcomeAsync(manifest);

        outcome.Success.Should().BeTrue();
        completions.Should().Equal(
            $"second:{pluginId}",
            $"third:{pluginId}");
        _pluginManager.Verify(
            manager => manager.RestorePluginRuntimeSnapshot(
                It.IsAny<PluginRuntimeSnapshot>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<PluginRuntimeReconciliation?>()),
            Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WhenRuntimeLoads_ShouldRefreshBeforeMarkingPluginInstalled()
    {
        const string pluginId = "ordered-runtime";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var plugin = TestMockFactory.CreateMockPlugin(id: pluginId);
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
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Once);
        _pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()),
            Times.Once);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithManifestOptimizationActionsOnly_ShouldKeepManifestPluginInstalled()
    {
        // Arrange
        const string pluginId = "manifest-optimization";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: true);
        var manifest = CreateInstallManifest(pluginId, packagePath, includeOptimizationAction: true);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);

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
        _pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()),
            Times.Once);
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithManifestSettingsPageOnly_ShouldKeepManifestPluginInstalled()
    {
        // Arrange
        const string pluginId = "user-feedback";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
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
        _pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()),
            Times.Once);
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithStoreOptimizationActionKeyOnly_ShouldKeepManifestPluginInstalled()
    {
        // Arrange
        const string pluginId = "store-optimization-key";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath, includeOptimizationAction: true, useOptimizationActionKey: true);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);

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
        _pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()),
            Times.Once);
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_WithStoreOptimizationActionsOnly_ShouldKeepManifestPluginInstalled()
    {
        // Arrange
        const string pluginId = "store-optimization";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath, includeOptimizationAction: true);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);

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
        _pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()),
            Times.Once);
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Once);
        _pluginManager.Verify(manager => manager.UninstallPlugin(pluginId), Times.Never);
    }

    [Fact]
    public async Task RepositoryUpdate_WhenBackupRenameFails_ShouldLeaveOriginalUntouched()
    {
        const string pluginId = "backup-rename-failure";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        using var service = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            moveDirectory: (source, destination) =>
            {
                if (Path.GetFullPath(source).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                    throw new IOException("deterministic backup rename failure");
                Directory.Move(source, destination);
            },
            atomicMoveSupported: (_, _) => true);

        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        installed.Should().BeFalse();
        File.ReadAllText(Path.Combine(target, "original.txt")).Should().Be("original");
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Never);
    }

    [Fact]
    public async Task RepositoryUpdate_WhenStrictActivationFails_ShouldRestoreOriginal()
    {
        const string pluginId = "activation-rollback";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        _pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()))
            .ThrowsAsync(new InvalidOperationException("replacement activation failed"));
        var activatedPlugin = TestMockFactory.CreateMockPlugin(id: pluginId);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out activatedPlugin))
            .Returns(true);
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var outcome = await service.DownloadAndInstallPluginWithOutcomeAsync(manifest);

        outcome.Success.Should().BeFalse();
        outcome.Degraded.Should().BeFalse();
        outcome.Error.Should().Contain("replacement activation failed");
        File.ReadAllText(Path.Combine(target, "original.txt")).Should().Be("original");
        _pluginManager.Verify(
            manager => manager.RestorePluginRuntimeSnapshot(
                It.IsAny<PluginRuntimeSnapshot>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<PluginRuntimeReconciliation?>()),
            Times.Once);
    }

    [Fact]
    public async Task RepositoryUpdate_WhenStrictLoadFails_ShouldRestoreOriginal()
    {
        const string pluginId = "load-rollback";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        _pluginManager
            .Setup(manager => manager.LoadPluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<PluginPackageAuthorization?>()))
            .ThrowsAsync(new InvalidOperationException("replacement load failed"));
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var outcome = await service.DownloadAndInstallPluginWithOutcomeAsync(manifest);

        outcome.Success.Should().BeFalse();
        outcome.Degraded.Should().BeFalse();
        outcome.Error.Should().Contain("replacement load failed");
        File.ReadAllText(Path.Combine(target, "original.txt")).Should().Be("original");
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Never);
    }

    [Fact]
    public async Task RepositoryUpdate_ShouldKeepBackupUntilRuntimeValidationCompletes()
    {
        const string pluginId = "upgrade-backup-lifetime";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var activatedPlugin = TestMockFactory.CreateMockPlugin(id: pluginId);
        _pluginManager.Setup(manager => manager.TryGetPlugin(pluginId, out activatedPlugin)).Returns(true);

        var backupPresentDuringLoad = false;
        var backupPresentDuringActivation = false;
        var backupDeletedBeforeCommitInstallation = false;
        var commitInstallationStarted = false;
        var deletedPaths = new List<string>();

        _pluginManager
            .Setup(manager => manager.LoadPluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<PluginPackageAuthorization?>()))
            .Callback(() => backupPresentDuringLoad = BackupContainsOriginal())
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<PluginPackageAuthorization?>()))
            .Callback(() => backupPresentDuringActivation = BackupContainsOriginal())
            .Returns(Task.CompletedTask);
        _pluginManager
            .Setup(manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()))
            .Returns((string id, IDisposable? _, Action? coordinatedCommit) =>
            {
                commitInstallationStarted = true;
                backupDeletedBeforeCommitInstallation = !BackupContainsOriginal();
                coordinatedCommit?.Invoke();
                return new PluginInstallationStateSnapshot(id, false, false);
            });

        using var service = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            deleteDirectory: path =>
            {
                deletedPaths.Add(path);
                Directory.Delete(path, recursive: true);
            });

        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        installed.Should().BeTrue();
        backupPresentDuringLoad.Should().BeTrue();
        backupPresentDuringActivation.Should().BeTrue();
        commitInstallationStarted.Should().BeTrue();
        backupDeletedBeforeCommitInstallation.Should().BeFalse();
        deletedPaths.Should().Contain(path =>
            Path.GetFileName(path).Equals("backup", StringComparison.OrdinalIgnoreCase));
        Directory.Exists(target).Should().BeTrue();
        Directory.GetFiles(target, "*.dll").Should().NotBeEmpty();
    }

    [Fact]
    public async Task RepositoryUpdate_WhenReplacementIsNotUsableAfterActivation_ShouldRestoreOriginal()
    {
        const string pluginId = "unusable-after-activation";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        _pluginManager
            .Setup(manager => manager.TryGetPlugin(pluginId, out It.Ref<IPlugin?>.IsAny))
            .Returns(false);
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var outcome = await service.DownloadAndInstallPluginWithOutcomeAsync(manifest);

        outcome.Success.Should().BeFalse();
        File.ReadAllText(Path.Combine(target, "original.txt")).Should().Be("original");
        _pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<PluginPackageAuthorization?>()),
            Times.Once);
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()),
            Times.Never);
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_ShouldPersistWebPageFromPackageWhenStoreOmitsIt()
    {
        const string pluginId = "webpage-from-package";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false, includeWebPage: true);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var plugin = TestMockFactory.CreateMockPlugin(id: pluginId);
        _pluginManager.Setup(manager => manager.TryGetPlugin(pluginId, out plugin)).Returns(true);
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        installed.Should().BeTrue();
        ReadInstalledWebPageEntry(pluginId).Should().Be("web/index.html");
    }

    [Fact]
    public async Task DownloadAndInstallPluginAsync_ShouldPersistWebPageFromStoreWhenPackageOmitsIt()
    {
        const string pluginId = "webpage-from-store";
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.Contributes = new PluginManifestContributions
        {
            WebPage = new PluginManifestWebContribution { Entry = "web/index.html" }
        };
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var plugin = TestMockFactory.CreateMockPlugin(id: pluginId);
        _pluginManager.Setup(manager => manager.TryGetPlugin(pluginId, out plugin)).Returns(true);
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        installed.Should().BeTrue();
        ReadInstalledWebPageEntry(pluginId).Should().Be("web/index.html");
    }

    [Fact]
    public async Task RepositoryUpdate_WhenBackupFingerprintChanges_ShouldNotMutateRollbackPayloads()
    {
        const string pluginId = "tampered-repository-backup";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        string? backupDirectory = null;
        _pluginManager
            .Setup(manager => manager.LoadPluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<PluginPackageAuthorization?>()))
            .Callback((
                string _,
                string _,
                IDisposable? _,
                PluginPackageAuthorization? _) =>
            {
                var transactionRoot = Path.Combine(
                    Path.GetDirectoryName(PluginPaths.GetPluginsDirectory())!,
                    ".udt-plugin-transactions");
                backupDirectory = Directory.GetDirectories(
                        transactionRoot,
                        "backup",
                        SearchOption.AllDirectories)
                    .Single();
                File.WriteAllText(
                    Path.Combine(backupDirectory, "original.txt"),
                    "tampered-repository-backup");
            })
            .ThrowsAsync(new InvalidOperationException("activation failed after backup tamper"));
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        (await service.DownloadAndInstallPluginAsync(manifest)).Should().BeFalse();

        backupDirectory.Should().NotBeNull();
        Directory.Exists(backupDirectory!).Should().BeTrue();
        File.ReadAllText(Path.Combine(backupDirectory!, "original.txt"))
            .Should().Be("tampered-repository-backup");
        Directory.Exists(target).Should().BeTrue(
            "the replacement must remain untouched after backup tamper is detected");
        Directory.GetFiles(target, "*.dll").Should().NotBeEmpty();
    }

    [Fact]
    public async Task RepositoryUpdate_WhenRollbackRestoreMoveFails_ShouldRetainRecoveryMaterial()
    {
        const string pluginId = "restore-move-failure";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        _pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()))
            .ThrowsAsync(new InvalidOperationException("replacement activation failed"));
        string? failureMessage = null;
        using var service = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            moveDirectory: (source, destination) =>
            {
                if (Path.GetFileName(source).Equals("backup", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFullPath(destination).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("deterministic restore failure");
                }
                Directory.Move(source, destination);
            });
        service.DownloadFailed += (_, message) => failureMessage = message;

        var installed = await service.DownloadAndInstallPluginAsync(manifest);

        installed.Should().BeFalse();
        failureMessage.Should().Contain("rollback").And.Contain("Recovery material");
        var transactionRoot = Path.Combine(
            Path.GetDirectoryName(PluginPaths.GetPluginsDirectory())!,
            ".udt-plugin-transactions");
        Directory.GetDirectories(transactionRoot, "*", SearchOption.TopDirectoryOnly)
            .Should().ContainSingle(directory => Directory.Exists(Path.Combine(directory, "backup")));
    }

    [Fact]
    public async Task RepositoryUpdate_WhenRuntimeReconciliationFails_ShouldNotMutateEitherPayload()
    {
        const string pluginId = "live-replacement";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var originalBytes = File.ReadAllBytes(Path.Combine(target, "original.txt"));
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var activatedPlugin = TestMockFactory.CreateMockPlugin(id: pluginId);
        _pluginManager.Setup(manager => manager.TryGetPlugin(pluginId, out activatedPlugin)).Returns(true);
        byte[]? replacementBytesBeforeReconcile = null;
        _pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Callback((string _, string path, IDisposable? _, PluginPackageAuthorization? _) =>
                replacementBytesBeforeReconcile = File.ReadAllBytes(path))
            .ThrowsAsync(new InvalidOperationException("startup failed"));
        _pluginManager
            .Setup(manager => manager.ReconcilePluginRuntimes(
                It.IsAny<PluginRuntimeSnapshot>(),
                It.IsAny<string>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<string?>()))
            .Throws(new InvalidOperationException("unload unconfirmed"));
        var moves = new List<(string Source, string Destination)>();
        var deletes = new List<string>();
        string? failure = null;
        using var service = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            moveDirectory: (source, destination) =>
            {
                moves.Add((source, destination));
                Directory.Move(source, destination);
            },
            deleteDirectory: path =>
            {
                deletes.Add(path);
                Directory.Delete(path, recursive: true);
            });
        service.DownloadFailed += (_, message) => failure = message;

        var outcome = await service.DownloadAndInstallPluginWithOutcomeAsync(manifest);

        outcome.Success.Should().BeFalse();
        outcome.Degraded.Should().BeTrue();
        outcome.RecoveryId.Should().Be(pluginId);
        outcome.RecoveryPath.Should().NotBeNullOrWhiteSpace();
        moves.Should().HaveCount(2, "no rollback move is safe after an unconfirmed unload");
        deletes.Should().BeEmpty();
        Directory.Exists(target).Should().BeTrue("the live replacement must remain in place");
        var replacementMainDll = Directory.GetFiles(target, "*.dll", SearchOption.TopDirectoryOnly)
            .Single(path => !PluginAssemblyNaming.IsSdkOrSharedDllFileName(Path.GetFileName(path)));
        File.ReadAllBytes(replacementMainDll).Should().Equal(replacementBytesBeforeReconcile!);
        var transactionRoot = Path.Combine(
            Path.GetDirectoryName(PluginPaths.GetPluginsDirectory())!,
            ".udt-plugin-transactions");
        var retained = Directory.GetDirectories(transactionRoot).Should().ContainSingle().Subject;
        File.ReadAllBytes(Path.Combine(retained, "backup", "original.txt"))
            .Should().Equal(originalBytes);
        failure.Should().Contain("unconfirmed").And.Contain("backup:");
    }

    [Fact]
    public async Task RepositoryUpdate_Rollback_ShouldRestoreExactTrustRecord()
    {
        const string pluginId = "exact-trust";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var mainDll = Path.Combine(target, $"{pluginId}.dll");
        TrustedPluginPackageStore.TrustPluginDirectory(pluginId, target);
        var previouslyUntrusted = Path.Combine(target, "late-added.dll");
        File.WriteAllBytes(previouslyUntrusted, [9, 8, 7]);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var activatedPlugin = TestMockFactory.CreateMockPlugin(id: pluginId);
        _pluginManager.Setup(manager => manager.TryGetPlugin(pluginId, out activatedPlugin)).Returns(true);
        _pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .ThrowsAsync(new InvalidOperationException("startup failed"));
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        (await service.DownloadAndInstallPluginAsync(manifest)).Should().BeFalse();

        TrustedPluginPackageStore.IsTrustedFile(mainDll).Should().BeTrue();
        TrustedPluginPackageStore.IsTrustedFile(previouslyUntrusted).Should().BeFalse(
            "rollback must restore the captured record rather than re-trust the directory");
    }

    [Fact]
    public async Task RepositoryUpdate_WhenMarkerCommitFailsAfterTrustFinalization_ShouldRestoreExactPriorTrust()
    {
        const string pluginId = "post-trust-marker-failure";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var originalMainDll = Path.Combine(target, $"{pluginId}.dll");
        TrustedPluginPackageStore.TrustPluginDirectory(pluginId, target);
        var previouslyUntrusted = Path.Combine(target, "late-added.dll");
        File.WriteAllBytes(previouslyUntrusted, [4, 5, 6]);
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        var activatedPlugin = TestMockFactory.CreateMockPlugin(id: pluginId);
        _pluginManager.Setup(manager => manager.TryGetPlugin(pluginId, out activatedPlugin))
            .Returns(true);
        _pluginManager
            .Setup(manager => manager.CommitPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()))
            .Throws(new IOException("marker commit failed"));
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        (await service.DownloadAndInstallPluginAsync(manifest)).Should().BeFalse();

        File.Exists(originalMainDll).Should().BeTrue();
        TrustedPluginPackageStore.IsTrustedFile(originalMainDll).Should().BeTrue();
        TrustedPluginPackageStore.IsTrustedFile(previouslyUntrusted).Should().BeFalse();
    }

    [Fact]
    public async Task RepositoryTransaction_ShouldKeepExactTrustScopedUntilFinalCommit()
    {
        const string pluginId = "scoped-repository-trust";
        var extractPath = CreateTempDirectory();
        File.WriteAllText(
            Path.Combine(extractPath, $"{pluginId}.dll"),
            "verified repository plugin bytes");
        var target = PluginPaths.GetPluginDirectory(pluginId);
        var manifest = new PluginManifest
        {
            Id = pluginId,
            Name = pluginId,
            Description = "Scoped trust transaction",
            Version = "1.0.0",
            MinimumHostVersion = "1.0.0",
        };
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var installMethod = typeof(PluginRepositoryService).GetMethod(
            "InstallExtractedPluginPayload",
            BindingFlags.Instance | BindingFlags.NonPublic);
        installMethod.Should().NotBeNull();
        var transaction = installMethod!.Invoke(
            service,
            [extractPath, target, manifest, true]);
        transaction.Should().NotBeNull();
        var transactionType = transaction!.GetType();
        var installedMainDll = transactionType.GetProperty("InstalledMainDll")!
            .GetValue(transaction)
            .Should().BeOfType<string>().Which;
        var authorization = transactionType.GetProperty("PackageAuthorization")!
            .GetValue(transaction)
            .Should().BeOfType<PluginPackageAuthorization>().Which;
        var productionValidator = new PluginSignatureValidator(
            PluginSignatureSettings.Production);

        var concurrentReader = Task.Run(() =>
            TrustedPluginPackageStore.IsTrustedFile(installedMainDll));
        var globalBeforeCommit = await productionValidator.ValidateAsync(installedMainDll);
        var scopedBeforeCommit = await authorization
            .Scope(productionValidator)
            .ValidateAsync(installedMainDll);

        (await concurrentReader).Should().BeFalse();
        globalBeforeCommit.IsValid.Should().BeFalse();
        scopedBeforeCommit.IsValid.Should().BeTrue();

        transactionType.GetMethod("CommitTrust")!.Invoke(transaction, null);
        (await productionValidator.ValidateAsync(installedMainDll))
            .IsValid.Should().BeTrue();
        transactionType.GetMethod("Commit")!.Invoke(transaction, null);
    }

    [Fact]
    public async Task RepositoryUpdate_WhenTransactionRootIsLink_ShouldRejectBeforeTargetMutation()
    {
        const string pluginId = "linked-transaction-root";
        var target = CreateExistingRepositoryPlugin(pluginId);
        var transactionRoot = Path.Combine(
            Path.GetDirectoryName(PluginPaths.GetPluginsDirectory())!,
            ".udt-plugin-transactions");
        var redirectedRoot = CreateTempDirectory();
        try
        {
            Directory.CreateSymbolicLink(transactionRoot, redirectedRoot);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                $"Symbolic-link creation is unavailable in this environment: {ex.Message}");
        }
        var packagePath = CreatePluginPackage(pluginId, includeOptimizationAction: false);
        var manifest = CreateInstallManifest(pluginId, packagePath);
        manifest.ZipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(packagePath);
        manifest.FileHash = await ComputePackageDllHashAsync(packagePath, pluginId);
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        (await service.DownloadAndInstallPluginAsync(manifest)).Should().BeFalse();

        File.ReadAllText(Path.Combine(target, "original.txt")).Should().Be("original");
        Directory.GetFileSystemEntries(redirectedRoot).Should().BeEmpty();
        _pluginManager.Verify(
            manager => manager.CommitPluginInstallation(
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()),
            Times.Never);
    }

    private static string CreateExistingRepositoryPlugin(string pluginId)
    {
        var target = PluginPaths.GetPluginDirectory(pluginId);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "original.txt"), "original");
        File.WriteAllBytes(Path.Combine(target, $"{pluginId}.dll"), [1, 2, 3]);
        return target;
    }

    private static bool BackupContainsOriginal()
    {
        var pluginsParent = Path.GetDirectoryName(PluginPaths.GetPluginsDirectory());
        if (string.IsNullOrWhiteSpace(pluginsParent))
            return false;

        var transactionRoot = Path.Combine(pluginsParent, ".udt-plugin-transactions");
        if (!Directory.Exists(transactionRoot))
            return false;

        return Directory.GetDirectories(transactionRoot, "backup", SearchOption.AllDirectories)
            .Any(backupDirectory =>
            {
                var sentinel = Path.Combine(backupDirectory, "original.txt");
                return File.Exists(sentinel) && File.ReadAllText(sentinel) == "original";
            });
    }

    private static string? ReadInstalledWebPageEntry(string pluginId)
    {
        var manifestPath = Path.Combine(PluginPaths.GetPluginDirectory(pluginId), "plugin.manifest.json");
        if (!File.Exists(manifestPath))
            return null;

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (!document.RootElement.TryGetProperty("contributes", out var contributes) ||
            !contributes.TryGetProperty("webPage", out var webPage) ||
            !webPage.TryGetProperty("entry", out var entry))
        {
            return null;
        }

        return entry.GetString();
    }

    [Fact]
    public void RepositoryMainDllMatcher_ShouldAcceptOfficialShellIntegrationName()
    {
        var root = CreateTempDirectory();
        var expected = Path.Combine(root, "UniversalDeviceToolkit.Plugins.ShellIntegration.dll");
        File.WriteAllBytes(expected, [1]);

        InvokeRepositoryMainDllMatcher(root, "shell-integration").Should().Be(expected);
    }

    [Fact]
    public void RepositoryMainDllMatcher_ShouldIgnoreDependencyBeforeCanonicalMainDll()
    {
        var root = CreateTempDirectory();
        File.WriteAllBytes(Path.Combine(root, "Dependency.dll"), [1]);
        var expected = Path.Combine(root, "ShellIntegration.dll");
        File.WriteAllBytes(expected, [2]);

        InvokeRepositoryMainDllMatcher(root, "shell-integration").Should().Be(expected);
    }

    [Fact]
    public void RepositoryMainDllMatcher_ShouldRejectAmbiguousCanonicalAssemblies()
    {
        var root = CreateTempDirectory();
        File.WriteAllBytes(Path.Combine(root, "ShellIntegration.dll"), [1]);
        File.WriteAllBytes(Path.Combine(root, "UniversalDeviceToolkit.Plugins.ShellIntegration.dll"), [2]);

        InvokeRepositoryMainDllMatcher(root, "shell-integration").Should().BeNull();
    }

    [Fact]
    public void RepositoryMainDllMatcher_ShouldRejectArbitrarySingleDll()
    {
        var root = CreateTempDirectory();
        File.WriteAllBytes(Path.Combine(root, "Unrelated.dll"), [1]);

        InvokeRepositoryMainDllMatcher(root, "shell-integration").Should().BeNull();
    }

    private static string? InvokeRepositoryMainDllMatcher(string root, string pluginId)
    {
        var method = typeof(PluginRepositoryService).GetMethod(
            "FindPluginMainDll",
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return (string?)method!.Invoke(null, [root, pluginId]);
    }

    private PluginRepositoryService CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        string? informationalVersion = "5.0.2",
        Action<string, string>? moveDirectory = null,
        Action<string>? deleteDirectory = null,
        Func<string, string, bool>? atomicMoveSupported = null)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory));
        var httpClientFactory = new StubHttpClientFactory(httpClient);
        // Tests stage plugin packages as file:// URIs to exercise the install/scan contract
        // without a network download. forceAllowFileUrls is the explicit dev/test opt-in that
        // bypasses the production-mode security gate in PluginRepositoryService which otherwise
        // blocks file:// downloads in Release builds (CI runs --configuration Release).
        return new PluginRepositoryService(
            _pluginManager.Object,
            httpClientFactory,
            forceAllowFileUrls: true,
            informationalVersion,
            moveDirectory,
            deleteDirectory,
            atomicMoveSupported);
    }

    private static T GetPrivateField<T>(PluginRepositoryService service, string fieldName)
    {
        var field = typeof(PluginRepositoryService).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull();
        return (T)field!.GetValue(service)!;
    }

    private static async Task<object?> InvokeTryResolvePublishedAssetAsync(
        PluginRepositoryService service,
        PluginManifest manifest)
    {
        var method = typeof(PluginRepositoryService).GetMethod(
            "TryResolvePublishedAssetAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var task = (Task)method!.Invoke(service, [manifest])!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    private static string CreateStoreResponseJson(string version, string minimumHostVersion) =>
        $$"""
        {
          "plugins": [
            {
              "id": "channel-plugin",
              "name": "Channel Plugin",
              "version": "{{version}}",
              "minimumHostVersion": "{{minimumHostVersion}}"
            }
          ]
        }
        """;

    private static async Task<string> ComputePackageDllHashAsync(string packagePath, string pluginId)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"udt-plugin-hash-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            ZipFile.ExtractToDirectory(packagePath, tempDirectory);
            var dllPath = Directory
                .EnumerateFiles(tempDirectory, $"{pluginId}.dll", SearchOption.AllDirectories)
                .Single();
            return await PluginPackageIntegrity.ComputeSha256HexAsync(dllPath);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private string CreatePluginPackage(
        string pluginId,
        bool includeOptimizationAction,
        bool includeWebPage = false)
    {
        var packageDirectory = CreateTempDirectory();
        var pluginDirectory = Path.Combine(packageDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        File.WriteAllText(Path.Combine(pluginDirectory, $"{pluginId}.dll"), "fake plugin dll");
        File.WriteAllText(
            Path.Combine(pluginDirectory, "plugin.json"),
            CreateManifestJson(pluginId, includeOptimizationAction, includeWebPage));

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

    private static string CreateManifestJson(
        string pluginId,
        bool includeOptimizationAction,
        bool includeWebPage = false)
    {
        if (!includeOptimizationAction && !includeWebPage)
        {
            return $$"""
            {
              "id": "{{pluginId}}",
              "name": "{{pluginId}}",
              "description": "Test plugin"
            }
            """;
        }

        var contributions = new List<string>();
        if (includeWebPage)
        {
            contributions.Add("""
                "webPage": {
                  "entry": "web/index.html"
                }
            """);
        }

        if (includeOptimizationAction)
        {
            contributions.Add("""
                "optimizationActions": [
                  {
                    "id": "apply-test",
                    "title": "Apply test"
                  }
                ]
            """);
        }

        return $$"""
        {
          "id": "{{pluginId}}",
          "name": "{{pluginId}}",
          "description": "Test plugin",
          "contributes": {
            {{string.Join(",\n            ", contributions)}}
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
