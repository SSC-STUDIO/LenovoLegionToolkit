using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public sealed class NetworkAccelerationSample
{
    public DateTime TimestampUtc { get; }
    public double DownloadBytesPerSecond { get; }
    public double UploadBytesPerSecond { get; }
    public long TotalBytesReceived { get; }
    public long TotalBytesSent { get; }

    public NetworkAccelerationSample(
        DateTime timestampUtc,
        double downloadBytesPerSecond,
        double uploadBytesPerSecond,
        long totalBytesReceived,
        long totalBytesSent)
    {
        TimestampUtc = timestampUtc;
        DownloadBytesPerSecond = downloadBytesPerSecond;
        UploadBytesPerSecond = uploadBytesPerSecond;
        TotalBytesReceived = totalBytesReceived;
        TotalBytesSent = totalBytesSent;
    }
}

public sealed class NetworkAccelerationRuntime
{
    private const int DefaultHistorySize = 120;
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);

    private readonly object _gate = new();
    private readonly List<NetworkAccelerationSample> _samples = new(DefaultHistorySize);
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public event EventHandler<NetworkAccelerationSample>? Sampled;

    /// <summary>
    /// Gets whether the runtime is currently running.
    /// </summary>
    public bool IsRunning => _cts != null;

    public IReadOnlyList<NetworkAccelerationSample> GetRecentSamples()
    {
        lock (_gate)
        {
            return _samples.ToList();
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_cts != null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunAsync(_cts.Token));
        }
    }

    public void Stop()
    {
        CancellationTokenSource? capturedCts;
        Task? capturedTask;

        lock (_gate)
        {
            capturedCts = _cts;
            capturedTask = _loopTask;
            _cts = null;
            _loopTask = null;
        }

        if (capturedCts == null)
        {
            return;
        }

        try
        {
            capturedCts.Cancel();

            if (capturedTask != null)
            {
                try
                {
                    capturedTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (TimeoutException)
                {
                    PluginLog.Trace("NetworkAcceleration: Sampling loop did not complete within 2 seconds during shutdown.");
                }
                catch (AggregateException)
                {
                    // Expected when the task is cancelled.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected during shutdown.
        }
        catch
        {
            // Ignore stop exceptions to keep shutdown resilient.
        }
        finally
        {
            capturedCts.Dispose();
        }
    }

    /// <summary>
    /// Stops the runtime asynchronously with graceful shutdown.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? cts;
        Task? loopTask;

        lock (_gate)
        {
            cts = _cts;
            loopTask = _loopTask;
            _cts = null;
            _loopTask = null;
        }

        if (cts == null)
        {
            return;
        }

        try
        {
            cts.Cancel();
            if (loopTask != null)
            {
                try
                {
                    await loopTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    // Loop task didn't complete within timeout - log but don't block shutdown
                    PluginLog.Trace("NetworkAcceleration: Sampling loop did not complete within 2 seconds during shutdown.");
                }
                catch (OperationCanceledException)
                {
                    // Internal CTS cancelled - expected during shutdown
                }
            }
        }
        catch
        {
            // Ignore other stop exceptions to keep shutdown resilient.
        }
        finally
        {
            cts.Dispose();
        }

        // Propagate external cancellation after internal cleanup completes
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Gets the cancellation token for the current runtime operation.
    /// Returns CancellationToken.None if the runtime is not running.
    /// </summary>
    public CancellationToken GetCancellationToken()
    {
        lock (_gate)
        {
            return _cts?.Token ?? CancellationToken.None;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        NetworkTotals? previousTotals = null;
        if (TryReadTotals(out var initialTotals))
        {
            previousTotals = initialTotals;
        }

        var previousTimestamp = DateTime.UtcNow;

        using var timer = new PeriodicTimer(SampleInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            var now = DateTime.UtcNow;
            if (!TryReadTotals(out var currentTotals))
            {
                continue;
            }

            if (previousTotals is null)
            {
                previousTotals = currentTotals;
                previousTimestamp = now;
                continue;
            }

            var elapsedSeconds = Math.Max((now - previousTimestamp).TotalSeconds, 0.001);

            var downloadDelta = Math.Max(0, currentTotals.BytesReceived - previousTotals.Value.BytesReceived);
            var uploadDelta = Math.Max(0, currentTotals.BytesSent - previousTotals.Value.BytesSent);

            var sample = new NetworkAccelerationSample(
                now,
                downloadDelta / elapsedSeconds,
                uploadDelta / elapsedSeconds,
                currentTotals.BytesReceived,
                currentTotals.BytesSent);

            lock (_gate)
            {
                if (_samples.Count >= DefaultHistorySize)
                {
                    _samples.RemoveAt(0);
                }

                _samples.Add(sample);
            }

            RaiseSampled(sample);

            previousTotals = currentTotals;
            previousTimestamp = now;
        }
    }

    private void RaiseSampled(NetworkAccelerationSample sample)
    {
        try
        {
            Sampled?.Invoke(this, sample);
        }
        catch
        {
            // Ignore subscriber errors to keep sampling loop running.
        }
    }

    private static bool TryReadTotals(out NetworkTotals totals)
    {
        try
        {
            var interfaceSnapshots = new List<(string Name, OperationalStatus Status, NetworkInterfaceType InterfaceType, long BytesReceived, long BytesSent, Exception? Error)>();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var (name, status, interfaceType) = GetInterfaceMetadata(networkInterface);

                try
                {
                    var stats = networkInterface.GetIPStatistics();
                    interfaceSnapshots.Add((name, status, interfaceType, stats.BytesReceived, stats.BytesSent, null));
                }
                catch (Exception ex)
                {
                    interfaceSnapshots.Add((name, status, interfaceType, 0, 0, ex));
                }
            }

            if (!TryReadTotals(interfaceSnapshots, out var bytesReceived, out var bytesSent))
            {
                totals = default;
                return false;
            }

            totals = new NetworkTotals(bytesReceived, bytesSent);
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"NetworkAcceleration: Failed to read network totals: {ex.Message}", ex);

            totals = default;
            return false;
        }
    }

    internal static bool TryReadTotals(
        IEnumerable<(string Name, OperationalStatus Status, NetworkInterfaceType InterfaceType, long BytesReceived, long BytesSent, Exception? Error)> interfaceSnapshots,
        out long totalBytesReceived,
        out long totalBytesSent)
    {
        long bytesReceived = 0;
        long bytesSent = 0;
        var sawEligibleInterface = false;
        var sawSuccessfulEligibleInterface = false;

        foreach (var interfaceSnapshot in interfaceSnapshots)
        {
            if (interfaceSnapshot.Status != OperationalStatus.Up)
            {
                continue;
            }

            if (interfaceSnapshot.InterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            sawEligibleInterface = true;

            if (interfaceSnapshot.Error is not null)
            {
                PluginLog.Trace($"NetworkAcceleration: Failed to read stats for interface '{interfaceSnapshot.Name}': {interfaceSnapshot.Error.Message}", interfaceSnapshot.Error);
                continue;
            }

            bytesReceived += interfaceSnapshot.BytesReceived;
            bytesSent += interfaceSnapshot.BytesSent;
            sawSuccessfulEligibleInterface = true;
        }

        if (sawEligibleInterface && !sawSuccessfulEligibleInterface)
        {
            totalBytesReceived = 0;
            totalBytesSent = 0;
            return false;
        }

        totalBytesReceived = bytesReceived;
        totalBytesSent = bytesSent;
        return true;
    }

    private static (string Name, OperationalStatus Status, NetworkInterfaceType InterfaceType) GetInterfaceMetadata(NetworkInterface networkInterface)
    {
        string name;
        try
        {
            name = networkInterface.Name;
        }
        catch
        {
            name = "unknown";
        }

        OperationalStatus status;
        try
        {
            status = networkInterface.OperationalStatus;
        }
        catch
        {
            status = OperationalStatus.Unknown;
        }

        NetworkInterfaceType interfaceType;
        try
        {
            interfaceType = networkInterface.NetworkInterfaceType;
        }
        catch
        {
            interfaceType = NetworkInterfaceType.Unknown;
        }

        return (name, status, interfaceType);
    }

    private readonly struct NetworkTotals
    {
        public long BytesReceived { get; }
        public long BytesSent { get; }

        public NetworkTotals(long bytesReceived, long bytesSent)
        {
            BytesReceived = bytesReceived;
            BytesSent = bytesSent;
        }
    }
}
