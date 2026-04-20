using System;
using System.IO;
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
    private static readonly string[] BuiltInRequiredFileNames =
    [
        ViveToolExeName,
        "Albacore.ViVe.dll",
        "Newtonsoft.Json.dll",
        "FeatureDictionary.pfs"
    ];

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
        if (IsTrustedViveToolPath(_cachedViveToolPath))
            return _cachedViveToolPath;

        // First check user-specified path from settings
        await _settings.LoadAsync().ConfigureAwait(false);
        var userSpecifiedPath = _settings.ViveToolPath;
        if (IsTrustedViveToolPath(userSpecifiedPath))
        {
            _cachedViveToolPath = userSpecifiedPath;
            return _cachedViveToolPath;
        }

        // Then check bundled runtime shipped with plugin package.
        var bundledPath = GetBundledViveToolPath();
        if (IsTrustedViveToolPath(bundledPath))
        {
            _cachedViveToolPath = bundledPath;
            return _cachedViveToolPath;
        }

        // Try built-in (download to AppData if missing)
        var builtInPath = GetBuiltInViveToolPath();
        var builtInDirectory = Path.GetDirectoryName(builtInPath);
        var builtInAvailable = await EnsureBuiltInViveToolAsync().ConfigureAwait(false);
        if (builtInAvailable && IsInstallComplete(builtInDirectory))
        {
            _cachedViveToolPath = builtInPath;
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
                await _settings.SaveAsync().ConfigureAwait(false);
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

            if (!IsInstallComplete(Path.GetDirectoryName(filePath)))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Specified path does not include the full ViVeTool runtime: {filePath}");
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
            var builtInDir = Path.GetDirectoryName(builtInPath);
            if (IsInstallComplete(builtInDir))
            {
                _cachedViveToolPath = builtInPath;
                return true;
            }

            return await new ViveToolDownloadService(this).DownloadViveToolAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Failed to download built-in ViVeTool: {ex.Message}", ex);
            return false;
        }
    }

    internal static bool IsInstallComplete(string? viveToolDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(viveToolDirectoryPath) || !Directory.Exists(viveToolDirectoryPath))
            return false;

        return BuiltInRequiredFileNames.All(fileName =>
            File.Exists(Path.Combine(viveToolDirectoryPath, fileName)));
    }

    private static bool IsTrustedViveToolPath(string? viveToolPath)
    {
        if (string.IsNullOrWhiteSpace(viveToolPath) || !File.Exists(viveToolPath))
            return false;

        if (!Path.GetFileName(viveToolPath).Equals(ViveToolExeName, StringComparison.OrdinalIgnoreCase))
            return false;

        return IsInstallComplete(Path.GetDirectoryName(viveToolPath));
    }
}
