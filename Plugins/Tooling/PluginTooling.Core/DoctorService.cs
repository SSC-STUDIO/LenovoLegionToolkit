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

        if (!File.Exists(hostReleasePath))
        {
            Add(result, false, "Host library found: (host release manifest missing)");
            Add(result, repository.Plugins.Count > 0, $"Discovered {repository.Plugins.Count} plugin project(s).");
            return result;
        }

        var hostRelease = TryReadHostRelease(hostReleasePath);
        if (hostRelease is null)
        {
            Add(result, false, $"Host release manifest is invalid JSON: {hostReleasePath}");
            Add(result, false, "Host library found: (host version unknown)");
            Add(result, repository.Plugins.Count > 0, $"Discovered {repository.Plugins.Count} plugin project(s).");
            return result;
        }

        var hostRoot = repository.HostDependenciesRoot;
        if (File.Exists(baselineManifestPath))
        {
            if (string.IsNullOrWhiteSpace(hostRelease.HostVersion))
            {
                Add(result, false, "HostBaseline hostVersion is missing.");
                Add(result, false, "Host library found: (host version unknown)");
                Add(result, repository.Plugins.Count > 0, $"Discovered {repository.Plugins.Count} plugin project(s).");
                return result;
            }

            hostRoot = Path.Combine(hostRoot, hostRelease.HostVersion);
            Add(result, Directory.Exists(hostRoot), $"Host dependency cache found: {hostRoot}");
        }

        var libName = string.IsNullOrWhiteSpace(hostRelease.Artifacts.Lib)
            ? "UniversalDeviceToolkit.Lib.dll"
            : hostRelease.Artifacts.Lib;
        var libPath = Path.Combine(hostRoot, libName);
        Add(result, File.Exists(libPath), $"Host library found: {libPath}");

        if (!string.IsNullOrWhiteSpace(hostRelease.Artifacts.LibPlugins))
        {
            var libPluginsPath = Path.Combine(hostRoot, hostRelease.Artifacts.LibPlugins);
            Add(result, File.Exists(libPluginsPath), $"Host plugins library found: {libPluginsPath}");
        }

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
        catch (InvalidOperationException)
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
