using System.IO.Compression;

namespace PluginTooling.Core;

public sealed class PluginPackager
{
    private readonly PluginRepository _repository = new();
    private readonly ProcessRunner _processRunner = new();

    public async Task<PackResult> PackAsync(PackRequest request, Action<string>? log = null, CancellationToken cancellationToken = default)
    {
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
                throw new InvalidOperationException($"dotnet build failed for '{pluginId}'.");
        }

        if (!Directory.Exists(plugin.OutputDirectory))
            throw new DirectoryNotFoundException($"Plugin build output not found: {plugin.OutputDirectory}");

        var outputDirectory = request.OutputDirectory is null
            ? Path.Combine(repository.RootPath, "Build", "release-assets")
            : Path.GetFullPath(request.OutputDirectory);

        Directory.CreateDirectory(outputDirectory);

        var assetName = $"{plugin.Manifest.Id}-v{plugin.Manifest.Version}.zip";
        var zipPath = Path.Combine(outputDirectory, assetName);
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(plugin.OutputDirectory, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        var fileSize = new FileInfo(zipPath).Length;

        log?.Invoke($"Created {zipPath}");
        return new PackResult(zipPath, assetName, fileSize);
    }
}
