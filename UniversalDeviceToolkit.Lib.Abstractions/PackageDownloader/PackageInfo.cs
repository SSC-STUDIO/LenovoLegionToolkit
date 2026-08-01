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

    /// <summary>
    /// Validates that required fields are present and non-empty.
    /// Call this after deserialization or construction to catch incomplete data.
    /// </summary>
    public bool IsValid(out string? validationError)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            validationError = "PackageInfo.Id is required";
            return false;
        }
        if (string.IsNullOrWhiteSpace(FileName))
        {
            validationError = "PackageInfo.FileName is required";
            return false;
        }
        if (string.IsNullOrWhiteSpace(FileLocation))
        {
            validationError = "PackageInfo.FileLocation is required";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Version))
        {
            validationError = "PackageInfo.Version is required";
            return false;
        }

        validationError = null;
        return true;
    }
}
