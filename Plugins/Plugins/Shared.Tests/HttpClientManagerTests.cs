using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.Shared.Tests;

public class HttpClientManagerTests
{
    [Fact]
    public void GetSharedClient_ReturnsNonNull()
    {
        var client = HttpClientManager.GetSharedClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void GetSharedClient_ReturnsSingleton()
    {
        var client1 = HttpClientManager.GetSharedClient();
        var client2 = HttpClientManager.GetSharedClient();
        Assert.Same(client1, client2);
    }

    [Fact]
    public void GetSharedClient_HasDefaultTimeout()
    {
        var client = HttpClientManager.GetSharedClient();
        Assert.Equal(TimeSpan.FromSeconds(Constants.DefaultTimeoutSeconds), client.Timeout);
    }

    [Fact]
    public void CreateClientWithTimeout_ReturnsNonNull()
    {
        var client = HttpClientManager.CreateClientWithTimeout(60);
        Assert.NotNull(client);
    }

    [Fact]
    public void CreateClientWithTimeout_UsesCustomTimeout()
    {
        var customTimeout = 45;
        var client = HttpClientManager.CreateClientWithTimeout(customTimeout);
        Assert.Equal(TimeSpan.FromSeconds(customTimeout), client.Timeout);
    }

    [Fact]
    public void CreateClientWithTimeout_ZeroTimeout_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HttpClientManager.CreateClientWithTimeout(0));
    }

    [Fact]
    public void CreateClientWithTimeout_NegativeTimeout_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HttpClientManager.CreateClientWithTimeout(-1));
    }

    [Fact]
    public void GetSharedClient_ConcurrentAccess_ReturnsSameSingleton()
    {
        HttpClientManager.DisposeSharedClient();

        const int parallelism = 32;
        var clients = new HttpClient[parallelism];
        Parallel.For(0, parallelism, i =>
        {
            clients[i] = HttpClientManager.GetSharedClient();
        });

        Assert.All(clients, c => Assert.NotNull(c));
        Assert.True(clients.Distinct().Count() == 1,
            "All parallel callers must receive the same singleton instance");
    }

    [Fact]
    public void GetDownloadClient_ConcurrentAccess_ReturnsSameSingleton()
    {
        HttpClientManager.DisposeSharedClient();

        const int parallelism = 32;
        var clients = new HttpClient[parallelism];
        Parallel.For(0, parallelism, i =>
        {
            clients[i] = HttpClientManager.GetDownloadClient();
        });

        Assert.All(clients, c => Assert.NotNull(c));
        Assert.True(clients.Distinct().Count() == 1,
            "All parallel callers must receive the same singleton instance");
    }

    [Fact]
    public void DisposeSharedClient_AllowsRecreationAfterDispose()
    {
        var client1 = HttpClientManager.GetSharedClient();
        HttpClientManager.DisposeSharedClient();
        var client2 = HttpClientManager.GetSharedClient();

        Assert.NotSame(client1, client2);
        Assert.NotNull(client2);
    }
}