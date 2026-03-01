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
public class PluginRepositoryService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IPluginManager _pluginManager;
    private readonly string _pluginsDirectory;
    private readonly string _tempDownloadDirectory;
    // Try main first, then master for backward compatibility.
    private static readonly string[] PluginStoreUrls =
    {
        "https://raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/main/store.json",
        "https://raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/master/store.json"
    };

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

            string storeJson;
            
            // Try local file first for development
            var localStorePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "store.json");
            if (File.Exists(localStorePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Using local store.json file at {localStorePath}");
                
                storeJson = await File.ReadAllTextAsync(localStorePath).ConfigureAwait(false);
            }
            else
            {
                storeJson = await FetchStoreJsonFromRemoteAsync().ConfigureAwait(false);
            }

            var storeResponse = JsonSerializer.Deserialize<PluginStoreResponse>(storeJson, new JsonSerializerOptions
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
                // Use the download URL from store.json if available, otherwise generate one
                if (string.IsNullOrEmpty(manifest.DownloadUrl))
                {
                    manifest.DownloadUrl = GetPluginDownloadUrl(manifest);
                }
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
            var downloaded = await DownloadPluginAsync(manifest, tempFilePath).ConfigureAwait(false);
            if (!downloaded)
            {
                DownloadFailed?.Invoke(this, $"Failed to download {manifest.Id}");
                return false;
            }

            // Extract and install
            var extractPath = Path.Combine(_tempDownloadDirectory, manifest.Id);
            var installed = await ExtractAndInstallPluginAsync(tempFilePath, extractPath, manifest).ConfigureAwait(false);

            // Clean up temp files
            try
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Cleanup of temp download directory failed: {ex.Message}", ex);
            }

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
        var candidateUrls = GetDownloadUrlCandidates(manifest);
        Exception? lastException = null;

        foreach (var candidateUrl in candidateUrls)
        {
            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Downloading plugin {manifest.Id} from {candidateUrl}");

                // Handle file:// URLs for local development
                if (candidateUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    var filePath = new Uri(candidateUrl).LocalPath;
                    if (File.Exists(filePath))
                    {
                        File.Copy(filePath, destinationPath, overwrite: true);
                        
                        // Report progress for local file copy
                        var fileInfo = new FileInfo(filePath);
                        DownloadProgressChanged?.Invoke(this, new PluginDownloadProgress
                        {
                            PluginId = manifest.Id,
                            BytesDownloaded = fileInfo.Length,
                            TotalBytes = fileInfo.Length,
                            ProgressPercentage = 100
                        });
                        
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Copied local plugin {manifest.Id} from {filePath} to {destinationPath}");
                        
                        manifest.DownloadUrl = candidateUrl;
                        return true;
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Local plugin file not found: {filePath}");
                    
                    continue;
                }

                // Handle HTTP/HTTPS URLs
                using var response = await _httpClient.GetAsync(candidateUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Download URL failed for plugin {manifest.Id}: {candidateUrl} returned {(int)response.StatusCode} {response.StatusCode}");
                    continue;
                }

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var bytesDownloaded = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
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

                manifest.DownloadUrl = candidateUrl;
                return true;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error downloading plugin {manifest.Id} from candidate URL: {candidateUrl}, error: {ex.Message}", ex);
            }
        }

        // Development fallback: if online assets are unavailable (for example HTTP 404),
        // package the local compiled plugin directory and continue installation.
        if (TryCreateLocalPackageFromInstalledFiles(manifest, destinationPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Fell back to local package for plugin {manifest.Id} at {destinationPath}");
            return true;
        }

        if (Log.Instance.IsTraceEnabled)
        {
            var urlsText = string.Join(", ", candidateUrls);
            Log.Instance.Trace($"Error downloading plugin {manifest.Id}: all candidates failed. Tried URLs: [{urlsText}]");
            if (lastException != null)
                Log.Instance.Trace($"Last download exception for plugin {manifest.Id}: {lastException.Message}", lastException);
        }

        return false;
    }

    private List<string> GetDownloadUrlCandidates(PluginManifest manifest)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(manifest.DownloadUrl))
        {
            candidates.Add(manifest.DownloadUrl);

            if (Uri.TryCreate(manifest.DownloadUrl, UriKind.Absolute, out var manifestUri) &&
                manifestUri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                var basePart = manifest.DownloadUrl;

                if (basePart.Contains("/releases/latest/download/", StringComparison.OrdinalIgnoreCase))
                {
                    basePart = manifest.DownloadUrl.Substring(0, manifest.DownloadUrl.IndexOf("/releases/latest/download/", StringComparison.OrdinalIgnoreCase));
                }
                else if (basePart.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase))
                {
                    basePart = manifest.DownloadUrl.Substring(0, manifest.DownloadUrl.IndexOf("/releases/download/", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    basePart = $"{manifestUri.Scheme}://{manifestUri.Host}{string.Join("", manifestUri.Segments.Take(3)).TrimEnd('/')}";
                }

                var versionedAssetName = $"{manifest.Id}-v{manifest.Version}.zip";
                var plainAssetName = $"{manifest.Id}.zip";
                var versionedTag = $"{manifest.Id}-v{manifest.Version}";

                candidates.Add($"{basePart}/releases/latest/download/{versionedAssetName}");
                candidates.Add($"{basePart}/releases/latest/download/{plainAssetName}");
                candidates.Add($"{basePart}/releases/download/{versionedTag}/{versionedAssetName}");
                candidates.Add($"{basePart}/releases/download/{versionedTag}/{plainAssetName}");
            }
        }

        // Always include generated fallback URL as the last remote candidate.
        candidates.Add(GetPluginDownloadUrl(manifest));

        return candidates
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool TryCreateLocalPackageFromInstalledFiles(PluginManifest manifest, string destinationPath)
    {
        try
        {
            var localPluginDirectory = FindLocalPluginDirectory(manifest.Id);
            if (localPluginDirectory == null)
                return false;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Attempting local package fallback for {manifest.Id} from {localPluginDirectory}");

            // Basic sanity check: ensure the directory contains at least one plugin DLL.
            var mainDll = FindPluginMainDll(localPluginDirectory, manifest.Id);
            if (string.IsNullOrWhiteSpace(mainDll))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Local package fallback aborted for {manifest.Id}: no plugin DLL in {localPluginDirectory}");
                return false;
            }

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            ZipFile.CreateFromDirectory(localPluginDirectory, destinationPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            var zipFileInfo = new FileInfo(destinationPath);
            DownloadProgressChanged?.Invoke(this, new PluginDownloadProgress
            {
                PluginId = manifest.Id,
                BytesDownloaded = zipFileInfo.Length,
                TotalBytes = zipFileInfo.Length,
                ProgressPercentage = 100
            });

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Local package fallback failed for {manifest.Id}: {ex.Message}", ex);
            return false;
        }
    }

    private string? FindLocalPluginDirectory(string pluginId)
    {
        try
        {
            if (!Directory.Exists(_pluginsDirectory))
                return null;

            var directCandidate = Path.Combine(_pluginsDirectory, pluginId);
            if (Directory.Exists(directCandidate))
                return directCandidate;

            var localCandidate = Path.Combine(_pluginsDirectory, "local", pluginId);
            if (Directory.Exists(localCandidate))
                return localCandidate;

            var normalizedPluginId = NormalizePluginToken(pluginId);
            var directories = Directory.GetDirectories(_pluginsDirectory, "*", SearchOption.TopDirectoryOnly)
                .Concat(Directory.Exists(Path.Combine(_pluginsDirectory, "local"))
                    ? Directory.GetDirectories(Path.Combine(_pluginsDirectory, "local"), "*", SearchOption.TopDirectoryOnly)
                    : Array.Empty<string>());

            foreach (var directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                var normalizedDirectoryName = NormalizePluginToken(directoryName);
                var normalizedDirectoryShortName = NormalizePluginToken(directoryName.Replace("LenovoLegionToolkit.Plugins.", string.Empty, StringComparison.OrdinalIgnoreCase));

                if (normalizedDirectoryName.Equals(normalizedPluginId, StringComparison.OrdinalIgnoreCase) ||
                    normalizedDirectoryShortName.Equals(normalizedPluginId, StringComparison.OrdinalIgnoreCase))
                {
                    return directory;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error locating local plugin directory for {pluginId}: {ex.Message}", ex);
            return null;
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
            {
                try
                {
                    Directory.Delete(extractPath, true);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to clean up extraction path {extractPath}: {ex.Message}");
                }
            }
            Directory.CreateDirectory(extractPath);

            // Extract zip
            ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);
            
            // Verify hash
            var dllPath = FindPluginMainDll(extractPath, manifest.Id);
            if (string.IsNullOrEmpty(dllPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin DLL not found for {manifest.Id}");
                return false;
            }
            
            // Calculate hash
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(dllPath);
            var hash = await sha256.ComputeHashAsync(stream).ConfigureAwait(false);
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
                try
                {
                    var backupDir = $"{pluginDir}_backup_{DateTime.Now:yyyyMMddHHmmss}";
                    Directory.Move(pluginDir, backupDir);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Renamed existing plugin directory {pluginDir} to {backupDir} to resolve conflict.");
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to rename plugin directory {pluginDir}, falling back to deletion: {ex.Message}");
                    
                    try
                    {
                        Directory.Delete(pluginDir, true);
                    }
                    catch (Exception deleteEx)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to delete plugin directory {pluginDir}: {deleteEx.Message}");
                        
                        // Try to delete individual files instead
                        try
                        {
                            foreach (var file in Directory.GetFiles(pluginDir, "*.*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    File.Delete(file);
                                }
                                catch
                                {
                                    // Continue with next file
                                }
                            }
                        }
                        catch
                        {
                            // Continue with copy
                        }
                    }
                }
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
    /// Get plugin download URL - supports both local development and GitHub releases
    /// </summary>
    private string GetPluginDownloadUrl(PluginManifest manifest)
    {
        // Check if local plugin zip exists for development
        var localPluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", $"{manifest.Id}.zip");
        if (File.Exists(localPluginPath))
        {
            // For local development, we need to use a file:// URL
            var uri = new Uri(localPluginPath);
            return uri.AbsoluteUri;
        }
        
// Fall back to GitHub URL
        return $"https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases/download/{manifest.Id}-v{manifest.Version}/{manifest.Id}.zip";
    }

    private async Task<string> FetchStoreJsonFromRemoteAsync()
    {
        Exception? lastException = null;

        foreach (var url in PluginStoreUrls)
        {
            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Fetching store.json from GitHub: {url}");

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to fetch store.json from {url}: {ex.Message}");
            }
        }

        throw new HttpRequestException("Failed to fetch plugin store metadata from all known URLs.", lastException);
    }

    private static string? FindPluginMainDll(string extractPath, string pluginId)
    {
        var pluginDlls = Directory.GetFiles(extractPath, "*.dll", SearchOption.AllDirectories)
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase) &&
                       !fileName.Equals("LenovoLegionToolkit.Plugins.SDK.dll", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if (!pluginDlls.Any())
            return null;

        var exactMatch = pluginDlls.FirstOrDefault(path =>
            Path.GetFileNameWithoutExtension(path).Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;

        var normalizedPluginId = NormalizePluginToken(pluginId);
        var normalizedMatches = pluginDlls
            .Where(path => NormalizePluginToken(Path.GetFileNameWithoutExtension(path))
                .Equals(normalizedPluginId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (normalizedMatches.Count == 1)
            return normalizedMatches[0];

        if (normalizedMatches.Count > 1)
        {
            return normalizedMatches.FirstOrDefault(path =>
                Path.GetFileName(path).StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
                ?? normalizedMatches[0];
        }

        var prefixedMatch = pluginDlls.FirstOrDefault(path =>
        {
            var fileName = Path.GetFileName(path);
            if (!fileName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
                return false;

            var normalizedFileName = NormalizePluginToken(Path.GetFileNameWithoutExtension(path));
            return normalizedFileName.Contains(normalizedPluginId, StringComparison.OrdinalIgnoreCase);
        });

        return prefixedMatch ?? pluginDlls[0];
    }

    private static string NormalizePluginToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToLowerInvariant();
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
        var availablePlugins = await FetchAvailablePluginsAsync().ConfigureAwait(false);
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

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try
                {
                    _httpClient?.Dispose();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error during PluginRepositoryService disposal", ex);
                }
            }
            _disposed = true;
        }
    }
}
