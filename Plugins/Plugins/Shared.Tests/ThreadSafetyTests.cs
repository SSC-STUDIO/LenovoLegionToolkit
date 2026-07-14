using UniversalDeviceToolkit.Plugins.Shared;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.Shared.Tests;

public class ThreadSafetyTests
{
    [Fact]
    public void HttpClientManager_GetSharedClient_ReturnsNonNull()
    {
        var client = HttpClientManager.GetSharedClient();
        Assert.NotNull(client);
        Assert.Equal(TimeSpan.FromSeconds(Constants.DefaultTimeoutSeconds), client.Timeout);
    }

    [Fact]
    public void HttpClientManager_GetDownloadClient_ReturnsNonNull()
    {
        var client = HttpClientManager.GetDownloadClient();
        Assert.NotNull(client);
        Assert.Equal(TimeSpan.FromSeconds(Constants.DownloadTimeoutSeconds), client.Timeout);
    }

    [Fact]
    public void HttpClientManager_CreateClientWithTimeout_PositiveValue_ReturnsClient()
    {
        var client = HttpClientManager.CreateClientWithTimeout(60);
        Assert.NotNull(client);
        Assert.Equal(TimeSpan.FromSeconds(60), client.Timeout);
        client.Dispose();
    }

    [Fact]
    public void HttpClientManager_CreateClientWithTimeout_ZeroOrNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HttpClientManager.CreateClientWithTimeout(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => HttpClientManager.CreateClientWithTimeout(-1));
    }

    [Fact]
    public void HttpClientManager_DisposeAndRecreate_Safe()
    {
        HttpClientManager.DisposeSharedClient();
        var client = HttpClientManager.GetSharedClient();
        Assert.NotNull(client);
        HttpClientManager.DisposeSharedClient();
    }

    [Fact]
    public void HttpClientManager_ConcurrentCreateClientWithTimeout_NoException()
    {
        var exceptions = new List<Exception>();

        Parallel.For(0, 20, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try
            {
                using var client = HttpClientManager.CreateClientWithTimeout(i + 1);
                Assert.NotNull(client);
            }
            catch (Exception ex)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        });

        Assert.Empty(exceptions);
    }

    #region ProcessRunner Concurrent Safety

    [Fact]
    public async Task ProcessRunner_ConcurrentExecutions_DoesNotCrash()
    {
        var runner = new ProcessRunner();
        var results = new ProcessResult[10];
        var exceptions = new List<Exception>();

        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks[index] = Task.Run(async () =>
            {
                try
                {
                    var path = System.IO.Path.Combine(Environment.SystemDirectory, "cmd.exe");
                    results[index] = await runner.RunProcessAsync(path, "/c echo test" + index);
                }
                catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
            });
        }

        await Task.WhenAll(tasks);
        Assert.Empty(exceptions);
        Assert.All(results, r => Assert.NotNull(r));
    }

    #endregion
}
