namespace UniversalDeviceToolkit.Abstractions.PackageDownloader;

/// <summary>
/// Cross-platform representation of a downloadable driver/software package.
/// </summary>
public readonly struct PackageInfo
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public string Version { get; init; }
    public string Category { get; init; }
    public string FileName { get; init; }
    public string FileSize { get; init; }
    public string? FileCrc { get; init; }
    public DateTime ReleaseDate { get; init; }
    public string? Readme { get; init; }
    public string FileLocation { get; init; }
    public bool IsUpdate { get; init; }
    public string Index { get; init; }
}
