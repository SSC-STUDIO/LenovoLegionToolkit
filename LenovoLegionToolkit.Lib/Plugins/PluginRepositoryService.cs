using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Service for managing online plugin repository
/// </summary>
public class PluginRepositoryService
{
    private readonly HttpClient _httpClient;
    private readonly IPluginManager _pluginManager;
    private readonly string _pluginsDirectory;
    private readonly string _tempDownloadDirectory;
    private const string GITHUB_API_URL = "https://api.github.com";
    private const string PLUGIN_STORE_URL = "https://raw.githubusercontent.com/BartoszCiczek/LenovoLegionToolkit/main/plugins/store.json";
    private const string PLUGIN_RELEASES_URL = "https://api.github.com/repos/BartoszCiczek/LenovoLegionToolkit/releases";

    public event EventHandler<PluginDownloadProgress>? DownloadProgressChanged;
    public event EventHandler<string>? DownloadCompleted;
    public event EventHandler<string>? DownloadFailed;

    public PluginRepositoryService(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LenovoLegionToolkit-PluginManager");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        _pluginsDirectory = GetPluginsDirectory();
        _tempDownloadDirectory = Path.Combine(Path.GetTempPath(), "LLTPluginDownloads");
        
        if (!Directory.Exists(_tempDownloadDirectory))
        {
            Directory.CreateDirectory(_tempDownloadDirectory);
        }
    }

    /// <summary>
    /// Fetch available plugins from the online repository
    /// </summary>
    public async Task<List<PluginManifest>> FetchAvailablePluginsAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Fetching plugins from online repository...");

            var response = await _httpClient.GetAsync(PLUGIN_STORE_URL, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var storeResponse = JsonSerializer.Deserialize<PluginStoreResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (storeResponse == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to deserialize plugin store response");
                return new List<PluginManifest>();
            }

            // Filter out already installed plugins or show their status
            var plugins = storeResponse.Plugins.Select(manifest =>
            {
                manifest.DownloadUrl = GetPluginDownloadUrl(manifest);
                return manifest;
            }).ToList();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Found {plugins.Count} plugins in store");

            return plugins;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error fetching plugins from store: {ex.Message}", ex);
            return new List<PluginManifest>();
        }
    }

    /// <summary>
    /// Download and install a plugin from the repository
    /// </summary>
    public async Task<bool> DownloadAndInstallPluginAsync(PluginManifest manifest)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting download and install for plugin: {manifest.Id}");

            // Create temporary download path
            var tempFilePath = Path.Combine(_tempDownloadDirectory, $"{manifest.Id}.zip");

            // Download the plugin
            var downloaded = await DownloadPluginAsync(manifest, tempFilePath);
            if (!downloaded)
            {
                DownloadFailed?.Invoke(this, $"Failed to download {manifest.Id}");
                return false;
            }

            // Extract and install
            var extractPath = Path.Combine(_tempDownloadDirectory, manifest.Id);
            var installed = await ExtractAndInstallPluginAsync(tempFilePath, extractPath, manifest);

            // Clean up temp files
            try
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
            }
            catch { }

            if (installed)
            {
                DownloadCompleted?.Invoke(this, manifest.Id);
                
                // Mark as installed in settings
                _pluginManager.InstallPlugin(manifest.Id);
                
                // Trigger plugin scan to reload plugins
                _pluginManager.ScanAndLoadPlugins();
            }

            return installed;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error installing plugin {manifest.Id}: {ex.Message}", ex);
            DownloadFailed?.Invoke(this, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Download plugin package
    /// </summary>
    private async Task<bool> DownloadPluginAsync(PluginManifest manifest, string destinationPath)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Downloading plugin {manifest.Id} from {manifest.DownloadUrl}");

            using var response = await _httpClient.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var bytesDownloaded = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                bytesDownloaded += bytesRead;

                var progress = totalBytes > 0 ? (double)bytesDownloaded / totalBytes * 100 : 0;
                
                DownloadProgressChanged?.Invoke(this, new PluginDownloadProgress
                {
                    PluginId = manifest.Id,
                    BytesDownloaded = bytesDownloaded,
                    TotalBytes = totalBytes > 0 ? totalBytes : 0,
                    ProgressPercentage = progress
                });
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Downloaded plugin {manifest.Id} to {destinationPath}");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error downloading plugin {manifest.Id}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Extract plugin zip and install to plugins directory
    /// </summary>
    private async Task<bool> ExtractAndInstallPluginAsync(string zipPath, string extractPath, PluginManifest manifest)
    {
        try
        {
            // Clean up previous extraction
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            Directory.CreateDirectory(extractPath);

            // Extract zip
            ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

            // Verify hash
            var dllPath = Path.Combine(extractPath, $"{manifest.Id}.dll");
            if (!File.Exists(dllPath))
            {
                // Try to find DLL in subdirectories
                dllPath = Directory.GetFiles(extractPath, "*.dll", SearchOption.AllDirectories)
                    .FirstOrDefault(f => Path.GetFileName(f).Contains(manifest.Id));

                if (dllPath == null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Plugin DLL not found for {manifest.Id}");
                    return false;
                }
            }

            // Calculate hash
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(dllPath);
            var hash = sha256.ComputeHash(stream);
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            if (!string.IsNullOrEmpty(manifest.FileHash) && !hashString.Equals(manifest.FileHash, StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Hash mismatch for {manifest.Id}. Expected: {manifest.FileHash}, Got: {hashString}");
                return false;
            }

            // Copy to plugins directory
            var pluginDir = Path.Combine(_pluginsDirectory, manifest.Id);
            if (Directory.Exists(pluginDir))
            {
                Directory.Delete(pluginDir, true);
            }

            Directory.CreateDirectory(pluginDir);

            // Copy all files from extraction
            foreach (var file in Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(extractPath.Length).TrimStart('\\', '/');
                var destPath = Path.Combine(pluginDir, relativePath);
                
                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(file, destPath, overwrite: true);
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Installed plugin {manifest.Id} to {pluginDir}");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error extracting plugin {manifest.Id}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Get plugin download URL from GitHub releases
    /// </summary>
    private string GetPluginDownloadUrl(PluginManifest manifest)
    {
        return $"https://github.com/BartoszCiczek/LenovoLegionToolkit/releases/download/plugins/{manifest.Id}.zip";
    }

    /// <summary>
    /// Get the plugins directory path
    /// </summary>
    private string GetPluginsDirectory()
    {
        var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        var possiblePaths = new[]
        {
            Path.Combine(appBaseDir, "build", "plugins"),
            Path.Combine(appBaseDir, "..", "..", "..", "build", "plugins"),
            Path.Combine(appBaseDir, "..", "build", "plugins"),
            Path.Combine(appBaseDir, "Plugins"),
            Path.Combine(appBaseDir, "plugins"),
        };

        foreach (var possiblePath in possiblePaths)
        {
            var fullPath = Path.GetFullPath(possiblePath);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        var defaultPath = Path.Combine(appBaseDir, "build", "plugins");
        Directory.CreateDirectory(defaultPath);
        return defaultPath;
    }

    /// <summary>
    /// Check for plugin updates
    /// </summary>
    public async Task<List<PluginManifest>> CheckForUpdatesAsync(List<PluginManifest> installedPlugins)
    {
        var availablePlugins = await FetchAvailablePluginsAsync();
        var updates = new List<PluginManifest>();

        foreach (var installed in installedPlugins)
        {
            var available = availablePlugins.FirstOrDefault(p => p.Id == installed.Id);
            if (available != null)
            {
                if (Version.TryParse(available.Version, out var availableVersion) &&
                    Version.TryParse(installed.Version, out var installedVersion))
                {
                    if (availableVersion > installedVersion)
                    {
                        updates.Add(available);
                    }
                }
            }
        }

        return updates;
    }

    /// <summary>
    /// Cleanup temporary download directory
    /// </summary>
    public void CleanupTempFiles()
    {
        try
        {
            if (Directory.Exists(_tempDownloadDirectory))
            {
                Directory.Delete(_tempDownloadDirectory, true);
                Directory.CreateDirectory(_tempDownloadDirectory);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error cleaning up temp files: {ex.Message}", ex);
        }
    }
}
