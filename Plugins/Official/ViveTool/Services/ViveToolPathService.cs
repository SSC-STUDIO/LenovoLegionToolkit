using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#if UDT_PLUGIN_AVALONIA_ONLY
using UniversalDeviceToolkit.Plugins.Core;
#else
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Plugins.Core;
#endif

namespace UniversalDeviceToolkit.Plugins.ViveTool.Services;

/// <summary>
/// Manages ViVeTool path resolution and caching.
/// </summary>
public class ViveToolPathService
{
    public const string ViveToolExeName = "ViVeTool.exe";
    private const string BundledViveToolDirectoryName = "Bundled";
    private const string PluginsDirectoryOverrideEnvironmentVariable = "LLT_PLUGIN_DIRECTORY_OVERRIDE";
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
        {
            return _cachedViveToolPath;
        }

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
#if UDT_PLUGIN_AVALONIA_ONLY
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UniversalDeviceToolkit");
#else
        var appData = Folders.AppData;
#endif
        return Path.Combine(appData, "ViveTool", ViveToolExeName);
    }

    public string GetBundledViveToolPath()
    {
        var bundledPath = EnumerateBundledViveToolPathCandidates().FirstOrDefault(path =>
            IsInstallComplete(Path.GetDirectoryName(path)));

        if (!string.IsNullOrWhiteSpace(bundledPath))
        {
            return bundledPath;
        }

        return EnumerateBundledViveToolPathCandidates().First();
    }

    private static IEnumerable<string> EnumerateBundledViveToolPathCandidates()
    {
        var overridePluginsDirectory = GetPluginsDirectoryOverride();
        var directories = new List<string?>();

        if (!string.IsNullOrWhiteSpace(overridePluginsDirectory))
        {
            directories.Add(Path.Combine(overridePluginsDirectory, "local", "vive-tool"));
            directories.Add(Path.Combine(overridePluginsDirectory, "vive-tool"));
            directories.Add(Path.Combine(overridePluginsDirectory, "UniversalDeviceToolkit.Plugins.ViveTool"));
        }

        directories.Add(Path.GetDirectoryName(typeof(ViveToolService).Assembly.Location));
        directories.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        if (string.IsNullOrWhiteSpace(overridePluginsDirectory))
        {
            var pluginsDirectory = GetDefaultPluginsDirectory();
            directories.Add(Path.Combine(pluginsDirectory, "local", "vive-tool"));
            directories.Add(Path.Combine(pluginsDirectory, "vive-tool"));
            directories.Add(Path.Combine(pluginsDirectory, "UniversalDeviceToolkit.Plugins.ViveTool"));
        }

        directories.Add(AppContext.BaseDirectory);

        var distinctDirectories = directories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in distinctDirectories)
        {
            yield return Path.Combine(directory, BundledViveToolDirectoryName, ViveToolExeName);
        }
    }

    private static string? GetPluginsDirectoryOverride()
    {
        var overridePath = Environment.GetEnvironmentVariable(PluginsDirectoryOverrideEnvironmentVariable);
        return string.IsNullOrWhiteSpace(overridePath)
            ? null
            : Path.GetFullPath(overridePath);
    }

    private static string GetDefaultPluginsDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "UniversalDeviceToolkit", "plugins");
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
                PluginLog.Trace($"ViveTool: Specified file does not exist: {filePath}");
                return false;
            }

            // Verify it's actually vivetool.exe
            var fileName = Path.GetFileName(filePath);
            if (!fileName.Equals(ViveToolExeName, StringComparison.OrdinalIgnoreCase))
            {
                PluginLog.Trace($"ViveTool: Specified file is not vivetool.exe: {filePath}");
                return false;
            }

            if (!IsInstallComplete(Path.GetDirectoryName(filePath)))
            {
                PluginLog.Trace($"ViveTool: Specified path does not include the full ViVeTool runtime: {filePath}");
                return false;
            }

            _settings.ViveToolPath = filePath;
            _cachedViveToolPath = filePath;
            await _settings.SaveAsync().ConfigureAwait(false);

            PluginLog.Trace($"ViveTool: Path set to: {filePath}");

            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error setting path: {ex.Message}", ex);
            return false;
        }
    }

    private async Task<bool> EnsureBuiltInViveToolAsync()
    {
        try
        {
            var bundledPath = GetBundledViveToolPath();
            if (IsInstallComplete(Path.GetDirectoryName(bundledPath)))
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
            PluginLog.Trace($"ViveTool: Failed to download built-in ViVeTool: {ex.Message}", ex);
            return false;
        }
    }

    internal static bool IsInstallComplete(string? viveToolDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(viveToolDirectoryPath) || !Directory.Exists(viveToolDirectoryPath))
        {
            return false;
        }

        return BuiltInRequiredFileNames.All(fileName =>
            File.Exists(Path.Combine(viveToolDirectoryPath, fileName)));
    }

    private static bool IsTrustedViveToolPath(string? viveToolPath)
    {
        if (string.IsNullOrWhiteSpace(viveToolPath) || !File.Exists(viveToolPath))
        {
            return false;
        }

        if (!Path.GetFileName(viveToolPath).Equals(ViveToolExeName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsInstallComplete(Path.GetDirectoryName(viveToolPath));
    }
}
