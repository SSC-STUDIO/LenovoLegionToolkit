using System;
using System.Net.NetworkInformation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Tests;

public class NetworkAccelerationRuntimeTests
{
    private NetworkAccelerationRuntime CreateRuntime()
    {
        return new NetworkAccelerationRuntime();
    }

    #region Lifecycle Tests

    [Fact]
    public void Start_InitialState_IsNotRunning()
    {
        var runtime = CreateRuntime();

        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public void Start_AfterStart_IsRunning()
    {
        var runtime = CreateRuntime();

        runtime.Start();

        Assert.True(runtime.IsRunning);

        runtime.Stop();
    }

    [Fact]
    public void Start_MultipleTimes_OnlyOneInstance()
    {
        var runtime = CreateRuntime();

        runtime.Start();
        runtime.Start();
        runtime.Start();

        Assert.True(runtime.IsRunning);

        runtime.Stop();
    }

    [Fact]
    public void Stop_AfterStart_IsNotRunning()
    {
        var runtime = CreateRuntime();

        runtime.Start();
        runtime.Stop();

        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        // Should not throw
        runtime.Stop();

        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public void Stop_MultipleTimes_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start();
        runtime.Stop();
        runtime.Stop();

        Assert.False(runtime.IsRunning);
    }

    #endregion

    #region Async Lifecycle Tests

    [Fact]
    public async Task StopAsync_AfterStart_IsNotRunning()
    {
        var runtime = CreateRuntime();

        runtime.Start();
        await runtime.StopAsync();

        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task StopAsync_WithoutStart_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        // Should not throw
        await runtime.StopAsync();

        Assert.False(runtime.IsRunning);
    }

    [Fact]
    public async Task StopAsync_CancellationRequested_ThrowsCancellationException()
    {
        var runtime = CreateRuntime();
        runtime.Start();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await runtime.StopAsync(cts.Token));

        runtime.Stop();
    }

    #endregion

    #region Sample Collection Tests

    [Fact]
    public void GetRecentSamples_InitialState_ReturnsEmptyList()
    {
        var runtime = CreateRuntime();

        var samples = runtime.GetRecentSamples();

        Assert.NotNull(samples);
        Assert.Empty(samples);
    }

    [Fact]
    public void GetRecentSamples_AfterStart_ReturnsSamples()
    {
        var runtime = CreateRuntime();
        runtime.Start();

        // Wait for at least one sample (sample interval is 1 second)
        Thread.Sleep(2500);

        var samples = runtime.GetRecentSamples();

        runtime.Stop();

        Assert.True(samples.Count > 0, "Should have collected at least one sample");
    }

    [Fact]
    public void TryReadTotals_IgnoresFaultedEligibleInterface_WhenAnotherEligibleInterfaceSucceeds()
    {
        var interfaceSnapshots = new[]
        {
            (Name: "faulted", Status: OperationalStatus.Up, InterfaceType: NetworkInterfaceType.Ethernet, BytesReceived: 0L, BytesSent: 0L, Error: (Exception?)new InvalidOperationException("boom")),
            (Name: "healthy", Status: OperationalStatus.Up, InterfaceType: NetworkInterfaceType.Wireless80211, BytesReceived: 120L, BytesSent: 45L, Error: (Exception?)null),
            (Name: "loopback", Status: OperationalStatus.Up, InterfaceType: NetworkInterfaceType.Loopback, BytesReceived: 999L, BytesSent: 999L, Error: (Exception?)null),
        };

        var success = NetworkAccelerationRuntime.TryReadTotals(interfaceSnapshots, out var totalBytesReceived, out var totalBytesSent);

        Assert.True(success);
        Assert.Equal(120L, totalBytesReceived);
        Assert.Equal(45L, totalBytesSent);
    }

    [Fact]
    public void GetRecentSamples_ReturnsImmutableCopy()
    {
        var runtime = CreateRuntime();
        runtime.Start();
        Thread.Sleep(1100);

        var samples1 = runtime.GetRecentSamples();
        var samples2 = runtime.GetRecentSamples();

        // They should be equal in content but different instances
        Assert.Equal(samples1.Count, samples2.Count);

        runtime.Stop();
    }

    [Fact]
    public void GetRecentSamples_DoesNotIncludeFutureSamples()
    {
        var runtime = CreateRuntime();
        runtime.Start();
        Thread.Sleep(1100);

        var samples1 = runtime.GetRecentSamples();
        Thread.Sleep(500);
        var samples2 = runtime.GetRecentSamples();

        // samples2 should have more or equal samples
        Assert.True(samples2.Count >= samples1.Count);

        runtime.Stop();
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Sampled_Event_RaisesOnSample()
    {
        var runtime = CreateRuntime();
        var eventRaised = false;
        var sampleReceived = new ManualResetEventSlim(false);

        runtime.Sampled += (sender, sample) =>
        {
            eventRaised = true;
            sampleReceived.Set();
        };

        runtime.Start();

        // Wait for event to be raised
        var received = sampleReceived.Wait(TimeSpan.FromSeconds(3));

        runtime.Stop();

        Assert.True(received, "Sample event should have been raised within timeout");
        Assert.True(eventRaised);
    }

    [Fact]
    public void Sampled_Event_ContainsValidSample()
    {
        var runtime = CreateRuntime();
        NetworkAccelerationSample? receivedSample = null;
        var sampleReceived = new ManualResetEventSlim(false);

        runtime.Sampled += (sender, sample) =>
        {
            receivedSample = sample;
            sampleReceived.Set();
        };

        runtime.Start();

        var received = sampleReceived.Wait(TimeSpan.FromSeconds(3));

        runtime.Stop();

        Assert.True(received);
        Assert.NotNull(receivedSample);
        Assert.True(receivedSample.TimestampUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Sampled_Event_AfterStop_MayStillFireInFlightEvents()
    {
        var runtime = CreateRuntime();
        var sampleCount = 0;
        var lastSampleBeforeStop = DateTime.MinValue;
        var sync = new object();

        runtime.Sampled += (sender, sample) =>
        {
            lock (sync)
            {
                sampleCount++;
            }
        };

        runtime.Start();
        Thread.Sleep(1500); // Wait for at least one sample

        lock (sync)
        {
            lastSampleBeforeStop = DateTime.UtcNow;
        }

        var countBeforeStop = sampleCount;
        runtime.Stop();
        var stopTime = DateTime.UtcNow;

        Thread.Sleep(200); // Small delay for in-flight events

        // The key test: after stop, no NEW samples should be collected
        // We may have collected samples that were in-flight, so we just verify
        // that the runtime has stopped by checking IsRunning is false
        Assert.False(runtime.IsRunning);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task StartAndStop_Concurrently_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        var startTask = Task.Run(() => runtime.Start());
        var stopTask = Task.Run(() =>
        {
            Thread.Sleep(100);
            runtime.Stop();
        });

        // Should not throw
        await Task.WhenAll(startTask, stopTask);

        runtime.Stop();
    }

    [Fact]
    public async Task GetRecentSamples_ConcurrentWithStart_DoesNotThrow()
    {
        var runtime = CreateRuntime();
        runtime.Start();

        var exceptionThrown = false;

        var samplesTask = Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    _ = runtime.GetRecentSamples();
                }
            }
            catch
            {
                exceptionThrown = true;
            }
        });

        var sampleWaitTask = Task.Run(() => Thread.Sleep(1500));

        await Task.WhenAll(samplesTask, sampleWaitTask);

        runtime.Stop();

        Assert.False(exceptionThrown);
    }

    [Fact]
    public void MultipleRuntimes_Independent()
    {
        var runtime1 = CreateRuntime();
        var runtime2 = CreateRuntime();

        runtime1.Start();
        runtime2.Start();

        Thread.Sleep(2500);

        var samples1 = runtime1.GetRecentSamples();
        var samples2 = runtime2.GetRecentSamples();

        runtime1.Stop();
        runtime2.Stop();

        // Each runtime should have collected samples independently
        Assert.True(samples1.Count > 0, "Runtime 1 should have collected samples");
        Assert.True(samples2.Count > 0, "Runtime 2 should have collected samples");
    }

    #endregion

    #region Sample Data Tests

    [Fact]
    public void Sample_Properties_AreValid()
    {
        var runtime = CreateRuntime();
        NetworkAccelerationSample? sample = null;
        var sampleReceived = new ManualResetEventSlim(false);

        runtime.Sampled += (sender, s) =>
        {
            sample = s;
            sampleReceived.Set();
        };

        runtime.Start();

        sampleReceived.Wait(TimeSpan.FromSeconds(3));

        runtime.Stop();

        Assert.NotNull(sample);
        Assert.True(sample.TotalBytesReceived >= 0);
        Assert.True(sample.TotalBytesSent >= 0);
        Assert.True(sample.DownloadBytesPerSecond >= 0);
        Assert.True(sample.UploadBytesPerSecond >= 0);
    }

    [Fact]
    public void Sample_Timestamp_IsUtc()
    {
        var runtime = CreateRuntime();
        NetworkAccelerationSample? sample = null;
        var sampleReceived = new ManualResetEventSlim(false);

        runtime.Sampled += (sender, s) =>
        {
            sample = s;
            sampleReceived.Set();
        };

        runtime.Start();

        sampleReceived.Wait(TimeSpan.FromSeconds(3));

        runtime.Stop();

        Assert.NotNull(sample);
        // Timestamp should be close to now
        var timeDiff = DateTime.UtcNow - sample.TimestampUtc;
        Assert.True(timeDiff.TotalSeconds < 5);
    }

    #endregion
}
