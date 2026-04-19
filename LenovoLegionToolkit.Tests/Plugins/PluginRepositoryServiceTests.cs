using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using Moq;
using Xunit;

namespace LenovoLegionToolkit.Tests.Plugins;

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
          "downloadUrl": "https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases/download/shell-integration-v1.0.9/shell-integration-v1.0.9.zip"
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
            request.RequestUri!.AbsoluteUri.Should().StartWith("https://raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/master/store.json");
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
        requestedUrls.Should().Contain(url => url.Contains("raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/master/store.json", StringComparison.OrdinalIgnoreCase));
        requestedUrls.Should().Contain(url => url.Contains("raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/refs/heads/master/store.json", StringComparison.OrdinalIgnoreCase));
        requestedUrls.Should().Contain(url => url.Contains("cdn.jsdelivr.net/gh/SSC-STUDIO/LenovoLegionToolkit-Plugins@master/store.json", StringComparison.OrdinalIgnoreCase));
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

    private PluginRepositoryService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory));
        var httpClientFactory = new StubHttpClientFactory(httpClient);
        return new PluginRepositoryService(_pluginManager.Object, httpClientFactory);
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
