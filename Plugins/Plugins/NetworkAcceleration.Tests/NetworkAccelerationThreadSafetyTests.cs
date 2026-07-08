using LenovoLegionToolkit.Plugins.NetworkAcceleration;
using LenovoLegionToolkit.Plugins.TestCommon;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Tests;

[Collection("NetworkAccelerationResourceCulture")]
public class NetworkAccelerationThreadSafetyTests
{
    [Fact]
    public void ConcurrentSetPreferredMode_NoException()
    {
        var plugin = new NetworkAccelerationPlugin();
        var exceptions = new List<Exception>();

        Parallel.For(0, 100, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try
            {
                var mode = (NetworkAccelerationMode)(i % 3);
                plugin.SetPreferredMode(mode);
            }
            catch (Exception ex)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        });

        Assert.Empty(exceptions);
        Assert.Contains(plugin.Settings.PreferredMode,
            new[] { NetworkAccelerationMode.Balanced, NetworkAccelerationMode.Gaming, NetworkAccelerationMode.Streaming });
    }

    [Fact]
    public async Task ConcurrentApplyAndRead_NoCorruption()
    {
        var plugin = new NetworkAccelerationPlugin();
        plugin.OnInstalled();
        var exceptions = new List<Exception>();

        var writeTask = Task.Run(async () =>
        {
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    var mode = (NetworkAccelerationMode)(i % 3);
                    var settings = new NetworkAccelerationSettings
                    {
                        PreferredMode = mode,
                        AutoOptimizeOnStartup = i % 2 == 0,
                        ResetWinsockOnOptimize = true,
                        ResetTcpIpOnOptimize = false
                    };
                    await plugin.ApplySettingsAsync(settings);
                }
                catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
            }
        });

        var readTask = Task.Run(() =>
        {
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    var snapshot = plugin.Settings;
                    Assert.NotNull(snapshot);
                }
                catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
            }
        });

        await Task.WhenAll(writeTask, readTask);
        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentBooleanSetters_InterleaveCorrectly()
    {
        var plugin = new NetworkAccelerationPlugin();
        var exceptions = new List<Exception>();

        Parallel.For(0, 100, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try
            {
                if (i % 3 == 0) plugin.SetAutoOptimizeOnStartup(i % 2 == 0);
                else if (i % 3 == 1) plugin.SetResetWinsockOnOptimize(i % 2 == 0);
                else plugin.SetResetTcpIpOnOptimize(i % 2 == 0);
            }
            catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void NetworkAccelerationTelemetryService_ConcurrentCapture_NoException()
    {
        var service = new NetworkAccelerationTelemetryService();
        var exceptions = new List<Exception>();

        Parallel.For(0, 20, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i =>
        {
            try
            {
                var snapshot = service.Capture();
                Assert.NotNull(snapshot);
            }
            catch (Exception ex) { lock (exceptions) { exceptions.Add(ex); } }
        });

        service.Dispose();
        Assert.Empty(exceptions);
    }
}
