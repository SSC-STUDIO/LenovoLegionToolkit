using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

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
                return;

            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunAsync(_cts.Token));
        }
    }

    public void Stop()
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
            return;

        try
        {
            cts.Cancel();
            loopTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Ignore stop exceptions to keep shutdown resilient.
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var previousTotals = ReadTotals();
        var previousTimestamp = DateTime.UtcNow;

        using var timer = new PeriodicTimer(SampleInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            var currentTotals = ReadTotals();
            var now = DateTime.UtcNow;
            var elapsedSeconds = Math.Max((now - previousTimestamp).TotalSeconds, 0.001);

            var downloadDelta = Math.Max(0, currentTotals.BytesReceived - previousTotals.BytesReceived);
            var uploadDelta = Math.Max(0, currentTotals.BytesSent - previousTotals.BytesSent);

            var sample = new NetworkAccelerationSample(
                now,
                downloadDelta / elapsedSeconds,
                uploadDelta / elapsedSeconds,
                currentTotals.BytesReceived,
                currentTotals.BytesSent);

            lock (_gate)
            {
                if (_samples.Count >= DefaultHistorySize)
                    _samples.RemoveAt(0);
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

    private static NetworkTotals ReadTotals()
    {
        try
        {
            long bytesReceived = 0;
            long bytesSent = 0;

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                var interfaceType = networkInterface.NetworkInterfaceType;
                if (interfaceType == NetworkInterfaceType.Loopback || interfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                var stats = networkInterface.GetIPStatistics();
                bytesReceived += stats.BytesReceived;
                bytesSent += stats.BytesSent;
            }

            return new NetworkTotals(bytesReceived, bytesSent);
        }
        catch
        {
            return new NetworkTotals(0, 0);
        }
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
