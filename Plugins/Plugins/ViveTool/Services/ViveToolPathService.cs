using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.ViveTool.Services;

/// <summary>
/// Manages ViVeTool path resolution and caching.
/// </summary>
public class ViveToolPathService
{
    public const string ViveToolExeName = "ViVeTool.exe";
    private const string BundledViveToolDirectoryName = "Bundled";

    private string? _cachedViveToolPath;
    private readonly Settings.ViveToolSettings _settings;

    public ViveToolPathService()
    {
        _settings = new Settings.ViveToolSettings();
        _ = _settings.LoadAsync();
    }

    public string? CachedPath
    {
        get => _cachedViveToolPath;
        set => _cachedViveToolPath = value;
    }

    public async Task<string?> GetViveToolPathAsync()
    {
        if (!string.IsNullOrEmpty(_cachedViveToolPath) && File.Exists(_cachedViveToolPath))
            return _cachedViveToolPath;

        // First check user-specified path from settings
        await _settings.LoadAsync().ConfigureAwait(false);
        var userSpecifiedPath = _settings.ViveToolPath;
        if (!string.IsNullOrEmpty(userSpecifiedPath) && File.Exists(userSpecifiedPath))
        {
            _cachedViveToolPath = userSpecifiedPath;
            return _cachedViveToolPath;
        }

        // Then check bundled runtime shipped with plugin package.
        var bundledPath = GetBundledViveToolPath();
        if (File.Exists(bundledPath))
        {
            _cachedViveToolPath = bundledPath;
            return _cachedViveToolPath;
        }

        // Try built-in (download to AppData if missing)
        var builtInPath = GetBuiltInViveToolPath();
        var builtInAvailable = await EnsureBuiltInViveToolAsync().ConfigureAwait(false);
        if (builtInAvailable && File.Exists(builtInPath))
        {
            _cachedViveToolPath = builtInPath;
            return _cachedViveToolPath;
        }

        // Check in PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, ViveToolExeName);
                if (File.Exists(fullPath))
                {
                    _cachedViveToolPath = fullPath;
                    return _cachedViveToolPath;
                }
            }
        }

        // Check current directory
        var currentPath = Path.Combine(Directory.GetCurrentDirectory(), ViveToolExeName);
        if (File.Exists(currentPath))
        {
            _cachedViveToolPath = currentPath;
            return _cachedViveToolPath;
        }

        return null;
    }

    public string GetBuiltInViveToolPath()
    {
        return Path.Combine(Folders.AppData, "ViveTool", ViveToolExeName);
    }

    public string GetBundledViveToolPath()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(ViveToolService).Assembly.Location) ?? AppContext.BaseDirectory;
        return Path.Combine(assemblyDirectory, BundledViveToolDirectoryName, ViveToolExeName);
    }

    public async Task<bool> SetViveToolPathAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _settings.ViveToolPath = null;
                _cachedViveToolPath = null;
                return true;
            }

            if (!File.Exists(filePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Specified file does not exist: {filePath}");
                return false;
            }

            // Verify it's actually vivetool.exe
            var fileName = Path.GetFileName(filePath);
            if (!fileName.Equals(ViveToolExeName, StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Specified file is not vivetool.exe: {filePath}");
                return false;
            }

            _settings.ViveToolPath = filePath;
            _cachedViveToolPath = filePath;
            await _settings.SaveAsync().ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Path set to: {filePath}");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error setting path: {ex.Message}", ex);
            return false;
        }
    }

    private async Task<bool> EnsureBuiltInViveToolAsync()
    {
        try
        {
            var bundledPath = GetBundledViveToolPath();
            if (File.Exists(bundledPath))
            {
                _cachedViveToolPath = bundledPath;
                return true;
            }

            var builtInPath = GetBuiltInViveToolPath();
            if (File.Exists(builtInPath))
                return true;

            var builtInDir = Path.GetDirectoryName(builtInPath);
            if (!string.IsNullOrEmpty(builtInDir) && !Directory.Exists(builtInDir))
                Directory.CreateDirectory(builtInDir);

            // Download ZIP file to temporary location
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"ViVeTool_{Guid.NewGuid()}.zip");
            try
            {
                using var httpClient = LenovoLegionToolkit.Plugins.Shared.HttpClientManager.CreateClientWithTimeout(
                    LenovoLegionToolkit.Plugins.Shared.Constants.DownloadTimeoutSeconds);
                var zipBytes = await httpClient.GetByteArrayAsync(ViveToolDownloadService.DefaultViveToolDownloadUrl).ConfigureAwait(false);
                await File.WriteAllBytesAsync(tempZipPath, zipBytes).ConfigureAwait(false);

                // Extract all files from ZIP to the built-in directory
                // ViVeTool.exe needs its dependencies (DLLs) in the same directory
                using var archive = System.IO.Compression.ZipFile.OpenRead(tempZipPath);

                // Verify ViVeTool.exe exists in the archive
                var exeEntry = archive.GetEntry(ViveToolExeName);
                if (exeEntry == null)
                {
                    // Try case-insensitive search
                    exeEntry = archive.Entries.FirstOrDefault(e =>
                        e.Name.Equals(ViveToolExeName, StringComparison.OrdinalIgnoreCase));
                }

                if (exeEntry == null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"ViveTool: {ViveToolExeName} not found in ZIP archive");
                    return false;
                }

                // Extract all entries to the built-in directory
                foreach (var entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    // SECURITY: Validate entry name to prevent path traversal in ZIP
                    if (entry.Name.Contains("..") || entry.Name.Contains('/') || entry.Name.Contains('\\'))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"SECURITY: Skipping suspicious entry name in ZIP: {entry.Name}");
                        continue;
                    }

                    var destinationPath = Path.Combine(builtInDir!, entry.Name);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Downloaded and extracted built-in ViVeTool and dependencies to {builtInDir}");

                return true;
            }
            finally
            {
                // Clean up temporary ZIP file
                try
                {
                    if (File.Exists(tempZipPath))
                        File.Delete(tempZipPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Failed to download built-in ViVeTool: {ex.Message}", ex);
            return false;
        }
    }
}
