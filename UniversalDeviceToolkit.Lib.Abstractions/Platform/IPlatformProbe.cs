using System.IO;

namespace UniversalDeviceToolkit.Abstractions.Platform;

/// <summary>
/// Read-only platform probe used by capability detection. Keeping filesystem
/// access behind this contract makes non-host platforms testable on Windows.
/// </summary>
public interface IPlatformProbe
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    IReadOnlyList<string> EnumerateFiles(string path, string searchPattern, bool recursive = false);
    IReadOnlyList<string> EnumerateDirectories(string path);
}

public sealed class PhysicalPlatformProbe : IPlatformProbe
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IReadOnlyList<string> EnumerateFiles(string path, string searchPattern, bool recursive = false) =>
        Directory.Exists(path)
            ? Directory.EnumerateFiles(
                    path,
                    searchPattern,
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .ToArray()
            : [];

    public IReadOnlyList<string> EnumerateDirectories(string path) =>
        Directory.Exists(path)
            ? Directory.EnumerateDirectories(path).ToArray()
            : [];
}
