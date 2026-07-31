using System;

namespace UniversalDeviceToolkit.Lib.Network;

/// <summary>GUI-side traffic view mirrored from the proxy worker over IPC.</summary>
public sealed record NetworkTrafficSnapshot
{
    public long TotalUploadBytes { get; init; }

    public long TotalDownloadBytes { get; init; }

    public double UploadBytesPerSecond { get; init; }

    public double DownloadBytesPerSecond { get; init; }

    public int ActiveSessions { get; init; }

    public long TotalConnections { get; init; }

    public DateTime StartedAtUtc { get; init; }
}

/// <summary>One connection-log line mirrored from the proxy worker over IPC.</summary>
public sealed record NetworkProxyLogEntry
{
    public DateTime TimestampUtc { get; init; }

    public string Level { get; init; } = "info";

    public string Message { get; init; } = string.Empty;
}
