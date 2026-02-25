using System;
using System.Collections.Concurrent;
using System.Linq;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Statistics;

/// <summary>
/// Network statistics service implementation
/// </summary>
public class NetworkStatisticsService : INetworkStatisticsService
{
    private long _downloadedBytes;
    private long _uploadedBytes;
    private bool _isTracking;
    
    // For calculating real-time speed using sliding window (similar to SteamTools)
    private const int SpeedCalculationIntervalSeconds = 5;
    private readonly FlowQueue _downloadQueue = new(SpeedCalculationIntervalSeconds);
    private readonly FlowQueue _uploadQueue = new(SpeedCalculationIntervalSeconds);
    
    // For chart history (store last 60 data points, updated every second)
    private const int MaxHistoryPoints = 60;
    private readonly ConcurrentQueue<(long downloadSpeed, long uploadSpeed, long timestamp)> _speedHistory = new();
    private long _lastHistoryUpdate = 0;
    private const long HistoryUpdateIntervalMs = 1000; // Update every second

    public long DownloadedBytes => _downloadedBytes;
    public long UploadedBytes => _uploadedBytes;

    public string GetFormattedDownloaded() => FormatBytes(_downloadedBytes);
    public string GetFormattedUploaded() => FormatBytes(_uploadedBytes);
    
    /// <summary>
    /// Get current download speed in bytes per second (using sliding window)
    /// </summary>
    public (long bytesPerSecond, string formatted) GetDownloadSpeed()
    {
        var rate = _downloadQueue.GetRate();
        var speed = (long)rate;
        
        // Update history periodically
        UpdateSpeedHistory(speed, (long)_uploadQueue.GetRate());
        
        return (speed, FormatBytes(speed) + "/s");
    }
    
    /// <summary>
    /// Get current upload speed in bytes per second (using sliding window)
    /// </summary>
    public (long bytesPerSecond, string formatted) GetUploadSpeed()
    {
        var rate = _uploadQueue.GetRate();
        return ((long)rate, FormatBytes((long)rate) + "/s");
    }
    
    /// <summary>
    /// Update speed history for chart display
    /// </summary>
    private void UpdateSpeedHistory(long downloadSpeed, long uploadSpeed)
    {
        var now = Environment.TickCount64;
        if (now - _lastHistoryUpdate < HistoryUpdateIntervalMs)
            return;
        
        _lastHistoryUpdate = now;
        
        _speedHistory.Enqueue((downloadSpeed, uploadSpeed, now));
        
        // Keep only last MaxHistoryPoints
        while (_speedHistory.Count > MaxHistoryPoints)
        {
            _speedHistory.TryDequeue(out _);
        }
    }
    
    /// <summary>
    /// Get speed history data points for chart
    /// </summary>
    public (long downloadSpeed, long uploadSpeed)[] GetSpeedHistory(int maxPoints)
    {
        var points = _speedHistory.ToArray();
        if (points.Length == 0)
            return Array.Empty<(long, long)>();
        
        // Return the last maxPoints items
        var startIndex = Math.Max(0, points.Length - maxPoints);
        var result = new (long, long)[points.Length - startIndex];
        for (int i = startIndex; i < points.Length; i++)
        {
            result[i - startIndex] = (points[i].downloadSpeed, points[i].uploadSpeed);
        }
        return result;
    }
    
    /// <summary>
    /// Get total traffic (download + upload)
    /// </summary>
    public string GetFormattedTotal() => FormatBytes(_downloadedBytes + _uploadedBytes);

    public void Start()
    {
        if (_isTracking)
        {
            return;
        }

        _isTracking = true;
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Network statistics tracking started.");
    }

    public void Stop()
    {
        if (!_isTracking)
        {
            return;
        }

        _isTracking = false;
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Network statistics tracking stopped.");
    }

    public void Reset()
    {
        _downloadedBytes = 0;
        _uploadedBytes = 0;
        _downloadQueue.Reset();
        _uploadQueue.Reset();
        while (_speedHistory.TryDequeue(out _)) { }
        _lastHistoryUpdate = 0;
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Network statistics reset.");
    }

    /// <summary>
    /// Record downloaded bytes
    /// </summary>
    public void RecordDownload(long bytes)
    {
        if (_isTracking)
        {
            _downloadedBytes += bytes;
            _downloadQueue.OnFlow(bytes);
        }
    }

    /// <summary>
    /// Record uploaded bytes
    /// </summary>
    public void RecordUpload(long bytes)
    {
        if (_isTracking)
        {
            _uploadedBytes += bytes;
            _uploadQueue.OnFlow(bytes);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public void Dispose()
    {
        Stop();
    }
    
    /// <summary>
    /// Flow queue for calculating speed using sliding window (similar to SteamTools FlowQueues)
    /// </summary>
    private sealed class FlowQueue
    {
        private readonly ConcurrentQueue<QueueItem> _queue = new();
        private readonly int _intervalSeconds;
        private long _totalBytes;
        private int _cleaning;
        
        private record QueueItem(long Ticks, long Length);
        
        public FlowQueue(int intervalSeconds)
        {
            _intervalSeconds = intervalSeconds;
        }
        
        public long TotalBytes => _totalBytes;
        
        public void OnFlow(long length)
        {
            System.Threading.Interlocked.Add(ref _totalBytes, length);
            CleanInvalidRecords();
            _queue.Enqueue(new QueueItem(Environment.TickCount64, length));
        }
        
        public double GetRate()
        {
            CleanInvalidRecords();
            var intervalSecondsDouble = (double)_intervalSeconds;
            // Use ToArray() to create a snapshot for thread-safe enumeration
            var sum = _queue.ToArray().Sum(item => item.Length);
            return sum / intervalSecondsDouble;
        }
        
        public void Reset()
        {
            _totalBytes = 0;
            while (_queue.TryDequeue(out _)) { }
        }
        
        private bool CleanInvalidRecords()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _cleaning, 1, 0) != 0)
                return false;
            
            var ticks = Environment.TickCount64;
            var threshold = _intervalSeconds * 1000L;
            
            while (_queue.TryPeek(out var item))
            {
                if (ticks - item.Ticks < threshold)
                    break;
                _queue.TryDequeue(out _);
            }
            
            System.Threading.Interlocked.Exchange(ref _cleaning, 0);
            return true;
        }
    }
}

