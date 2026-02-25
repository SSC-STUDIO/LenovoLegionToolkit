using System;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Statistics;

/// <summary>
/// Network statistics service interface
/// </summary>
public interface INetworkStatisticsService : IDisposable
{
    /// <summary>
    /// Get downloaded traffic in bytes
    /// </summary>
    long DownloadedBytes { get; }

    /// <summary>
    /// Get uploaded traffic in bytes
    /// </summary>
    long UploadedBytes { get; }

    /// <summary>
    /// Get formatted downloaded traffic string
    /// </summary>
    string GetFormattedDownloaded();

    /// <summary>
    /// Get formatted uploaded traffic string
    /// </summary>
    string GetFormattedUploaded();

    /// <summary>
    /// Start tracking statistics
    /// </summary>
    void Start();

    /// <summary>
    /// Stop tracking statistics
    /// </summary>
    void Stop();

    /// <summary>
    /// Reset statistics
    /// </summary>
    void Reset();

    /// <summary>
    /// Record downloaded bytes
    /// </summary>
    void RecordDownload(long bytes);

    /// <summary>
    /// Record uploaded bytes
    /// </summary>
    void RecordUpload(long bytes);
    
    /// <summary>
    /// Get current download speed
    /// </summary>
    (long bytesPerSecond, string formatted) GetDownloadSpeed();
    
    /// <summary>
    /// Get current upload speed
    /// </summary>
    (long bytesPerSecond, string formatted) GetUploadSpeed();
    
    /// <summary>
    /// Get total traffic (download + upload) formatted
    /// </summary>
    string GetFormattedTotal();
    
    /// <summary>
    /// Get speed history data points for chart (download and upload speeds in bytes/second)
    /// Returns array of (downloadSpeed, uploadSpeed) tuples for the last N data points
    /// </summary>
    (long downloadSpeed, long uploadSpeed)[] GetSpeedHistory(int maxPoints);
}

