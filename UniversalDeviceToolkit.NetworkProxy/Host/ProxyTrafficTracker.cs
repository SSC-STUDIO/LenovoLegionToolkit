using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace UniversalDeviceToolkit.NetworkProxy.Host;

/// <summary>One ring-buffer log line emitted by the proxy worker.</summary>
public sealed record ProxyLogEntry(DateTime TimestampUtc, string Level, string Message);

/// <summary>Point-in-time traffic view used by the GUI traffic tab.</summary>
public sealed record ProxyTrafficSnapshot(
    long TotalUploadBytes,
    long TotalDownloadBytes,
    double UploadBytesPerSecond,
    double DownloadBytesPerSecond,
    int ActiveSessions,
    long TotalConnections,
    DateTime StartedAtUtc);

/// <summary>
/// Thread-safe traffic + connection-log tracker for the loopback proxy.
/// Rates are computed per snapshot: each call returns the throughput since the previous
/// snapshot and re-bases the window (the GUI polls about once per second).
/// </summary>
internal sealed class ProxyTrafficTracker
{
    private const int MaxLogEntries = 500;

    private readonly object _gate = new();
    private readonly Queue<ProxyLogEntry> _logs = new();
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;

    private long _totalUpload;
    private long _totalDownload;
    private long _totalConnections;

    // Rate calculation uses cumulative totals — the previous snapshot's totals
    // are stored so we can compute the delta. This avoids the race condition in
    // the old window-counter design where Interlocked.Read + Interlocked.Exchange
    // could lose bytes written between the two operations.
    private long _prevUpload;
    private long _prevDownload;
    private DateTime _prevSnapshotUtc = DateTime.UtcNow;

    public void AddUpload(long bytes)
    {
        Interlocked.Add(ref _totalUpload, bytes);
    }

    public void AddDownload(long bytes)
    {
        Interlocked.Add(ref _totalDownload, bytes);
    }

    public void AddConnection()
    {
        Interlocked.Increment(ref _totalConnections);
    }

    public void Info(string message) => Add("info", message);

    public void Warning(string message) => Add("warning", message);

    public void Error(string message) => Add("error", message);

    private void Add(string level, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        lock (_gate)
        {
            if (_logs.Count >= MaxLogEntries)
                _logs.Dequeue();

            _logs.Enqueue(new ProxyLogEntry(DateTime.UtcNow, level, message));
        }
    }

    public ProxyTrafficSnapshot Snapshot(int activeSessions)
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            var elapsed = Math.Max(0.001, (now - _prevSnapshotUtc).TotalSeconds);

            var curUpload = Interlocked.Read(ref _totalUpload);
            var curDownload = Interlocked.Read(ref _totalDownload);

            var uploadDelta = curUpload - _prevUpload;
            var downloadDelta = curDownload - _prevDownload;

            var snapshot = new ProxyTrafficSnapshot(
                curUpload,
                curDownload,
                uploadDelta / elapsed,
                downloadDelta / elapsed,
                activeSessions,
                Interlocked.Read(ref _totalConnections),
                _startedAtUtc);

            _prevUpload = curUpload;
            _prevDownload = curDownload;
            _prevSnapshotUtc = now;
            return snapshot;
        }
    }

    public IReadOnlyList<ProxyLogEntry> Recent(int max = 200)
    {
        lock (_gate)
            return _logs.Reverse().Take(Math.Max(1, max)).ToArray();
    }
}
