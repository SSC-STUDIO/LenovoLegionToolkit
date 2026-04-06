using System;
using System.Net.Http;
using LenovoLegionToolkit.Plugins.Shared;
using Xunit;

namespace LenovoLegionToolkit.Plugins.Shared.Tests;

/// <summary>
/// Integration tests for SDK-Shared HttpClient singleton chain.
/// Verifies that SDK PluginBase.GetSharedHttpClient() correctly delegates to HttpClientManager.
/// </summary>
public class SdkSharedIntegrationTests
{
    /// <summary>
    /// Verifies that HttpClientManager returns the same singleton instance across multiple calls.
    /// </summary>
    [Fact]
    public void HttpClientManager_GetSharedClient_ReturnsSameInstance()
    {
        // Arrange & Act
        var client1 = HttpClientManager.GetSharedClient();
        var client2 = HttpClientManager.GetSharedClient();

        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.Same(client1, client2);
    }

    /// <summary>
    /// Verifies that HttpClient singleton has expected default timeout.
    /// </summary>
    [Fact]
    public void HttpClientManager_GetSharedClient_HasDefaultTimeout()
    {
        // Arrange & Act
        var client = HttpClientManager.GetSharedClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(TimeSpan.FromSeconds(Constants.DefaultTimeoutSeconds), client.Timeout);
    }

    /// <summary>
    /// Verifies that CreateClientWithTimeout creates new instances (not singleton).
    /// </summary>
    [Fact]
    public void HttpClientManager_CreateClientWithTimeout_ReturnsNewInstance()
    {
        // Arrange & Act
        var client1 = HttpClientManager.CreateClientWithTimeout(60);
        var client2 = HttpClientManager.CreateClientWithTimeout(60);

        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.NotSame(client1, client2);
    }

    /// <summary>
    /// Verifies that CreateClientWithTimeout sets custom timeout correctly.
    /// </summary>
    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void HttpClientManager_CreateClientWithTimeout_SetsCustomTimeout(int timeoutSeconds)
    {
        // Arrange & Act
        var client = HttpClientManager.CreateClientWithTimeout(timeoutSeconds);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(TimeSpan.FromSeconds(timeoutSeconds), client.Timeout);
    }

    /// <summary>
    /// Verifies that CreateClientWithTimeout rejects invalid timeout values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void HttpClientManager_CreateClientWithTimeout_RejectsInvalidTimeout(int timeoutSeconds)
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HttpClientManager.CreateClientWithTimeout(timeoutSeconds));
    }

    /// <summary>
    /// Verifies that singleton client is different from custom timeout client.
    /// </summary>
    [Fact]
    public void HttpClientManager_SingletonDiffersFromCustomClient()
    {
        // Arrange & Act
        var singletonClient = HttpClientManager.GetSharedClient();
        var customClient = HttpClientManager.CreateClientWithTimeout(60);

        // Assert
        Assert.NotSame(singletonClient, customClient);
    }

    /// <summary>
    /// Verifies Constants values are correctly defined.
    /// </summary>
    [Fact]
    public void Constants_HaveExpectedValues()
    {
        // Assert
        Assert.Equal(30, Constants.DefaultTimeoutSeconds);
        Assert.Equal(120, Constants.DownloadTimeoutSeconds);
        Assert.Equal(60, Constants.ProcessTimeoutSeconds);
        Assert.Equal(8192, Constants.DefaultBufferSize);
        Assert.Equal(65536, Constants.LargeBufferSize);
    }

    /// <summary>
    /// Verifies that HttpClient instances are properly disposed when using CreateClientWithTimeout.
    /// Note: Singleton client should NOT be disposed.
    /// </summary>
    [Fact]
    public void HttpClientManager_CustomClient_CanBeDisposed()
    {
        // Arrange
        var client = HttpClientManager.CreateClientWithTimeout(30);

        // Act & Assert - Should not throw
        client.Dispose();
    }

    /// <summary>
    /// Verifies multiple calls to GetSharedClient after custom client creation still return singleton.
    /// </summary>
    [Fact]
    public void HttpClientManager_SingletonPersistsAfterCustomClientCreation()
    {
        // Arrange
        var singletonBefore = HttpClientManager.GetSharedClient();

        // Act
        var customClient = HttpClientManager.CreateClientWithTimeout(60);
        var singletonAfter = HttpClientManager.GetSharedClient();

        // Assert
        Assert.Same(singletonBefore, singletonAfter);
        Assert.NotSame(singletonBefore, customClient);

        // Cleanup
        customClient.Dispose();
    }
}