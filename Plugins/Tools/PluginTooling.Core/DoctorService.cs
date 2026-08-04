using System.Text.Json;

namespace PluginTooling.Core;

public sealed class DoctorService
{
    private readonly PluginRepository _repository = new();

    public DoctorResult Run(string repositoryRoot)
    {
        var repository = _repository.Load(repositoryRoot);
        var result = new DoctorResult
        {
            RepositoryRoot = repository.RootPath,
            GeneratedAt = DateTimeOffset.UtcNow.ToString("O"),
        };

        Add(result, File.Exists(repository.SolutionPath), $"Solution found: {repository.SolutionPath}");

        var baselineManifestPath = Path.Combine(repository.RootPath, "HostBaseline", "host-release.json");
        var hostReleasePath = File.Exists(baselineManifestPath)
            ? baselineManifestPath
            : Path.Combine(repository.HostDependenciesRoot, "host-release.json");
        Add(result, File.Exists(hostReleasePath), $"Host release manifest found: {hostReleasePath}");

        var hostRelease = TryReadHostRelease(hostReleasePath);
        var hostRoot = repository.HostDependenciesRoot;
        if (hostRelease is not null && File.Exists(baselineManifestPath))
        {
            hostRoot = Path.Combine(hostRoot, hostRelease.HostVersion);
        }

        var libName = hostRelease?.Artifacts.Lib ?? "UniversalDeviceToolkit.Lib.dll";
        var libPath = Path.Combine(hostRoot, libName);
        Add(result, File.Exists(libPath), $"Host library found: {libPath}");

        var wpfName = hostRelease?.Artifacts.Wpf ?? "Universal Device Toolkit.dll";
        var wpfPath = Path.Combine(hostRoot, wpfName);
        Add(result, File.Exists(wpfPath), $"Host WPF assembly found: {wpfPath}");

        Add(result, repository.Plugins.Count > 0, $"Discovered {repository.Plugins.Count} plugin project(s).");

        return result;
    }

    private static HostReleaseManifest? TryReadHostRelease(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return PluginRepository.ReadJsonFile<HostReleaseManifest>(path);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Add(DoctorResult result, bool condition, string message)
    {
        result.Checks.Add(new DoctorCheck
        {
            Status = condition ? "PASS" : "FAIL",
            Message = message,
        });
    }
}
