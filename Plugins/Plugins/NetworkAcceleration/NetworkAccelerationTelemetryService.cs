using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UniversalDeviceToolkit.Plugins.Shared;

namespace UniversalDeviceToolkit.Plugins.NetworkAcceleration;

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
    private readonly object _lock = new();

    public NetworkAccelerationTelemetrySnapshot Capture()
    {
        lock (_lock)
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
            long bytesReceived;
            long bytesSent;

            try
            {
                var statistics = networkInterface.GetIPStatistics();
                bytesReceived = statistics.BytesReceived;
                bytesSent = statistics.BytesSent;
            }
            catch (NetworkInformationException ex)
            {
                // A single failing NIC must not crash the entire telemetry snapshot.
                // NetworkAccelerationRuntime.TryReadTotals already guards per-interface
                // reads the same way — this mirrors that resilience for the UI telemetry path.
                PluginLog.Trace($"NetworkAcceleration: Failed to read stats for interface '{networkInterface.Name}': {ex.Message}", ex);
                continue;
            }

            var currentCounter = (ReceivedBytes: bytesReceived, SentBytes: bytesSent);
            currentCounters[networkInterface.Id] = currentCounter;
            totalReceivedBytes += currentCounter.ReceivedBytes;
            totalSentBytes += currentCounter.SentBytes;

            _lastCounters.TryGetValue(networkInterface.Id, out var previousCounter);
            var receivedDelta = Math.Max(0, currentCounter.ReceivedBytes - previousCounter.ReceivedBytes);
            var sentDelta = Math.Max(0, currentCounter.SentBytes - previousCounter.SentBytes);

            var downloadMbps = _lastTimestamp is null ? 0 : (receivedDelta * 8d) / elapsedSeconds / 1_000_000d;
            var uploadMbps = _lastTimestamp is null ? 0 : (sentDelta * 8d) / elapsedSeconds / 1_000_000d;
            var snapshot = new NetworkAccelerationTelemetrySnapshot(
                now,
                networkInterface.Name,
                downloadMbps,
                uploadMbps,
                currentCounter.ReceivedBytes,
                currentCounter.SentBytes);

            if (bestSnapshot is null ||
                (snapshot.DownloadMbps + snapshot.UploadMbps) > (bestSnapshot.DownloadMbps + bestSnapshot.UploadMbps))
            {
                bestSnapshot = snapshot;
            }
        }

        UpdateLastCounters(_lastCounters, interfaces.Select(i => i.Id).ToArray(), currentCounters);

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
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _lastCounters.Clear();
            _lastTimestamp = null;
        }
    }

    /// <summary>
    /// Incrementally updates the last-counters dictionary: updates entries for
    /// interfaces that were successfully read this cycle, removes entries for
    /// interfaces that are no longer Up/eligible (preventing unbounded growth),
    /// and preserves entries for interfaces that failed to read stats this
    /// cycle so their baseline survives for the next successful read.
    /// </summary>
    internal static void UpdateLastCounters(
        Dictionary<string, (long ReceivedBytes, long SentBytes)> lastCounters,
        string[] activeInterfaceIds,
        Dictionary<string, (long ReceivedBytes, long SentBytes)> currentCounters)
    {
        var activeIds = new HashSet<string>(activeInterfaceIds, StringComparer.OrdinalIgnoreCase);

        var staleIds = lastCounters.Keys
            .Where(id => !activeIds.Contains(id))
            .ToList();
        foreach (var staleId in staleIds)
        {
            lastCounters.Remove(staleId);
        }

        foreach (var entry in currentCounters)
        {
            lastCounters[entry.Key] = entry.Value;
        }
    }
}
