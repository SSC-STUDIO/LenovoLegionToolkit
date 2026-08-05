using System.IO.Compression;

namespace PluginTooling.Core;

public sealed class PluginPackager
{
    private static readonly HashSet<string> HostRuntimeFileStems = new(StringComparer.OrdinalIgnoreCase)
    {
        "Universal Device Toolkit",
        "UniversalDeviceToolkit.WPF",
        "UniversalDeviceToolkit.Lib",
        "UniversalDeviceToolkit.Lib.Plugins",
        "UniversalDeviceToolkit.Lib.Abstractions",
        "UniversalDeviceToolkit.Lib.Shared",
        "UniversalDeviceToolkit.Lib.Macro",
        "UniversalDeviceToolkit.Lib.Automation",
        "UniversalDeviceToolkit.CLI.Lib",
        "UniversalDeviceToolkit.Plugins.Abstractions",
    };

    private readonly PluginRepository _repository = new();
    private readonly ProcessRunner _processRunner = new();

    public async Task<PackResult> PackAsync(PackRequest request, Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var repository = _repository.Load(request.RepositoryRoot);
        var pluginId = _repository.ResolveTargetPluginIds(repository, [request.PluginId]).Single();
        var plugin = repository.Plugins[pluginId];

        if (request.BuildFirst && !string.IsNullOrWhiteSpace(plugin.ProjectPath))
        {
            var exitCode = await _processRunner.RunDotnetAsync(
                ["build", plugin.ProjectPath!, "-c", request.Configuration, "--nologo"],
                repository.RootPath,
                log,
                cancellationToken);

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"dotnet build failed for '{pluginId}'.");
            }
        }

        if (!Directory.Exists(plugin.OutputDirectory))
        {
            throw new DirectoryNotFoundException($"Plugin build output not found: {plugin.OutputDirectory}");
        }

        EnsurePackageRequiredFiles(plugin);

        var outputDirectory = request.OutputDirectory is null
            ? Path.Combine(repository.RootPath, ".build", "release-assets")
            : Path.GetFullPath(request.OutputDirectory);

        Directory.CreateDirectory(outputDirectory);

        var assetName = string.IsNullOrWhiteSpace(plugin.UnifiedManifest.Package.AssetName)
            ? $"{plugin.Manifest.Id}-v{plugin.Manifest.Version}.zip"
            : plugin.UnifiedManifest.Package.AssetName;
        var zipPath = Path.Combine(outputDirectory, assetName);
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        CreatePackageArchive(plugin, zipPath);
        var fileSize = new FileInfo(zipPath).Length;

        log?.Invoke($"Created {zipPath}");
        return new PackResult(zipPath, assetName, fileSize);
    }

    private static void EnsurePackageRequiredFiles(PluginContext plugin)
    {
        foreach (var requiredFile in plugin.UnifiedManifest.Package.RequiredFiles ?? [])
        {
            var path = Path.Combine(plugin.OutputDirectory, requiredFile);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Package required file is missing from build output: {requiredFile}", path);
            }
        }
    }

    private static void CreatePackageArchive(PluginContext plugin, string zipPath)
    {
        var sourceRoot = Path.GetFullPath(plugin.OutputDirectory);
        var targetPath = Path.GetFullPath(zipPath);
        var sourceFiles = Directory
            .EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFullPath(path), targetPath, StringComparison.OrdinalIgnoreCase))
            .Where(path => !IsHostRuntimeFile(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var includedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = NormalizePackagePath(Path.GetRelativePath(sourceRoot, sourceFile));
                archive.CreateEntryFromFile(sourceFile, relativePath, CompressionLevel.Optimal);
                includedFiles.Add(relativePath);
            }
        }

        var missingRequiredFiles = (plugin.UnifiedManifest.Package.RequiredFiles ?? [])
            .Select(NormalizePackagePath)
            .Where(requiredFile => !includedFiles.Contains(requiredFile))
            .ToArray();
        if (missingRequiredFiles.Length != 0)
        {
            throw new InvalidDataException(
                $"Package archive is missing required files after host-runtime filtering: {string.Join(", ", missingRequiredFiles)}");
        }
    }

    private static bool IsHostRuntimeFile(string path)
    {
        var fileStem = Path.GetFileNameWithoutExtension(path);
        return fileStem is not null && HostRuntimeFileStems.Contains(fileStem);
    }

    private static string NormalizePackagePath(string path) =>
        path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}
