using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

internal sealed record NetworkAccelerationTelemetrySnapshot(
    DateTimeOffset Timestamp,
    string InterfaceName,
    double DownloadMbps,
    double UploadMbps,
    long TotalReceivedBytes,
    long TotalSentBytes);

internal sealed class NetworkAccelerationTelemetryService : IDisposable
{
    private readonly Dictionary<string, (long ReceivedBytes, long SentBytes)> _lastCounters = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset? _lastTimestamp;

    public NetworkAccelerationTelemetrySnapshot Capture()
    {
        var now = DateTimeOffset.UtcNow;
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface =>
                networkInterface.OperationalStatus == OperationalStatus.Up &&
                networkInterface.NetworkInterfaceType is not NetworkInterfaceType.Loopback &&
                networkInterface.NetworkInterfaceType is not NetworkInterfaceType.Tunnel)
            .ToArray();

        if (interfaces.Length == 0)
        {
            _lastCounters.Clear();
            _lastTimestamp = now;
            return new NetworkAccelerationTelemetrySnapshot(now, NetworkAccelerationText.NoActiveAdapter, 0, 0, 0, 0);
        }

        var elapsedSeconds = Math.Max(0.1, _lastTimestamp is null ? 0 : (now - _lastTimestamp.Value).TotalSeconds);
        NetworkAccelerationTelemetrySnapshot? bestSnapshot = null;
        var currentCounters = new Dictionary<string, (long ReceivedBytes, long SentBytes)>(StringComparer.OrdinalIgnoreCase);
        long totalReceivedBytes = 0;
        long totalSentBytes = 0;

        foreach (var networkInterface in interfaces)
        {
            var statistics = networkInterface.GetIPStatistics();
            var currentCounter = (statistics.BytesReceived, statistics.BytesSent);
            currentCounters[networkInterface.Id] = currentCounter;
            totalReceivedBytes += currentCounter.BytesReceived;
            totalSentBytes += currentCounter.BytesSent;

            _lastCounters.TryGetValue(networkInterface.Id, out var previousCounter);
            var receivedDelta = Math.Max(0, currentCounter.BytesReceived - previousCounter.ReceivedBytes);
            var sentDelta = Math.Max(0, currentCounter.BytesSent - previousCounter.SentBytes);

            var downloadMbps = _lastTimestamp is null ? 0 : (receivedDelta * 8d) / elapsedSeconds / 1_000_000d;
            var uploadMbps = _lastTimestamp is null ? 0 : (sentDelta * 8d) / elapsedSeconds / 1_000_000d;
            var snapshot = new NetworkAccelerationTelemetrySnapshot(
                now,
                networkInterface.Name,
                downloadMbps,
                uploadMbps,
                currentCounter.BytesReceived,
                currentCounter.BytesSent);

            if (bestSnapshot is null ||
                (snapshot.DownloadMbps + snapshot.UploadMbps) > (bestSnapshot.DownloadMbps + bestSnapshot.UploadMbps))
            {
                bestSnapshot = snapshot;
            }
        }

        _lastCounters.Clear();
        foreach (var entry in currentCounters)
        {
            _lastCounters[entry.Key] = entry.Value;
        }

        _lastTimestamp = now;
        if (bestSnapshot is null)
        {
            return new NetworkAccelerationTelemetrySnapshot(now, NetworkAccelerationText.NoActiveAdapter, 0, 0, 0, 0);
        }

        return bestSnapshot with
        {
            TotalReceivedBytes = totalReceivedBytes,
            TotalSentBytes = totalSentBytes
        };
    }

    public void Dispose()
    {
        _lastCounters.Clear();
        _lastTimestamp = null;
    }
}
