using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
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
    private readonly string _storeCachePath;
    // The plugin store is currently published from master.
    // Keep the source list explicit so the app does not waste time hitting the missing main/store.json endpoint first,
    // and include a CDN mirror because raw.githubusercontent.com can intermittently reset connections on Windows.
    private static readonly string[] PluginStoreUrls =
    {
        "https://raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/master/store.json",
        "https://raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/refs/heads/master/store.json",
        "https://cdn.jsdelivr.net/gh/SSC-STUDIO/LenovoLegionToolkit-Plugins@master/store.json"
    };
    private const string PluginReleasesApiUrl = "https://api.github.com/repos/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases?per_page=50";
    private const int RemoteRequestRetryCount = 3;
    private const int RemoteDownloadRetryCount = 1;
    private static readonly TimeSpan StoreRequestTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ReleaseMetadataRequestTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ApiAssetDownloadRequestTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan BrowserDownloadRequestTimeout = TimeSpan.FromSeconds(150);

    public event EventHandler<PluginDownloadProgress>? DownloadProgressChanged;
    public event EventHandler<string>? DownloadCompleted;
    public event EventHandler<string>? DownloadFailed;

    public PluginRepositoryService(IPluginManager pluginManager, HttpClientFactory httpClientFactory)
    {
        _pluginManager = pluginManager;
        _httpClient = httpClientFactory.Create();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LenovoLegionToolkit-PluginManager");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        _pluginsDirectory = GetPluginsDirectory();
        _tempDownloadDirectory = Path.Combine(Path.GetTempPath(), "LLTPluginDownloads");
        _storeCachePath = Path.Combine(Folders.AppData, "plugin-store-cache.json");

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

                throw new InvalidDataException("Failed to deserialize plugin store response.");
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
            throw;
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

            var versionChecker = new VersionChecker();
            if (!versionChecker.IsCompatible(manifest.MinimumHostVersion))
            {
                var compatibilityMessage = $"Plugin {manifest.Id} requires Lenovo Legion Toolkit {manifest.MinimumHostVersion} or newer.";
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(compatibilityMessage);

                DownloadFailed?.Invoke(this, compatibilityMessage);
                return false;
            }

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
                // Mark as installed before the scan so startup hooks can run during reload.
                _pluginManager.InstallPlugin(manifest.Id);
                await _pluginManager.ScanAndLoadPluginsAsync().ConfigureAwait(false);
                DownloadCompleted?.Invoke(this, manifest.Id);
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
        var publishedAsset = await TryResolvePublishedAssetAsync(manifest).ConfigureAwait(false);

        if (publishedAsset is not null)
        {
            var preferredCandidateUrls = new List<string>();

            if (!string.IsNullOrWhiteSpace(publishedAsset.ApiDownloadUrl))
                preferredCandidateUrls.Add(publishedAsset.ApiDownloadUrl);

            if (!string.IsNullOrWhiteSpace(publishedAsset.DownloadUrl))
                preferredCandidateUrls.Add(publishedAsset.DownloadUrl);

            candidateUrls = preferredCandidateUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!string.IsNullOrWhiteSpace(publishedAsset.Version))
                manifest.Version = publishedAsset.Version;
        }

        foreach (var candidateUrl in candidateUrls)
        {
            var downloaded = await TryDownloadPluginFromUrlAsync(manifest, candidateUrl, destinationPath).ConfigureAwait(false);
            if (downloaded)
                return true;
        }

        if (publishedAsset is not null)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Published asset candidates for plugin {manifest.Id} were already attempted: {publishedAsset.AssetName}");
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
        }

        return false;
    }

    private async Task<bool> TryDownloadPluginFromUrlAsync(PluginManifest manifest, string candidateUrl, string destinationPath)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Downloading plugin {manifest.Id} from {candidateUrl}");

            if (candidateUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                var filePath = new Uri(candidateUrl).LocalPath;
                if (!File.Exists(filePath))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Local plugin file not found: {filePath}");
                    return false;
                }

                File.Copy(filePath, destinationPath, overwrite: true);

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

            for (var attempt = 1; attempt <= RemoteDownloadRetryCount; attempt++)
            {
                try
                {
                    DeletePartialDownload(destinationPath);

                    using var request = CreateGetRequest(candidateUrl);
                    using var cts = new CancellationTokenSource(GetDownloadTimeout(candidateUrl));
                    using var response = await _httpClient
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                        .ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        if (attempt < RemoteDownloadRetryCount && IsRetryableStatusCode(response.StatusCode))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Download attempt {attempt}/{RemoteDownloadRetryCount} for plugin {manifest.Id} returned {(int)response.StatusCode} {response.StatusCode}. Retrying...");

                            await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
                            continue;
                        }

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Download URL failed for plugin {manifest.Id}: {candidateUrl} returned {(int)response.StatusCode} {response.StatusCode}");
                        return false;
                    }

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var bytesDownloaded = 0L;

                    using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                    using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cts.Token).ConfigureAwait(false);
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
                catch (Exception ex) when (attempt < RemoteDownloadRetryCount && IsTransientRemoteException(ex))
                {
                    DeletePartialDownload(destinationPath);

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Transient error downloading plugin {manifest.Id} from {candidateUrl} on attempt {attempt}/{RemoteDownloadRetryCount}: {ex.Message}. Retrying...", ex);

                    await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            DeletePartialDownload(destinationPath);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error downloading plugin {manifest.Id} from candidate URL: {candidateUrl}, error: {ex.Message}", ex);
            return false;
        }
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
        string? backupDir = null;
        var pluginDir = Path.Combine(_pluginsDirectory, manifest.Id);

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

            var installationService = new PluginInstallationService(_pluginManager);
            var resolvedPluginId = await installationService.AnalyzeAndFixPluginStructureAsync(extractPath).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(resolvedPluginId))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Unable to normalize plugin package structure for {manifest.Id}");
                return false;
            }

            if (!resolvedPluginId.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Normalized plugin package id '{resolvedPluginId}' does not match requested manifest id '{manifest.Id}'. Aborting installation.");

                return false;
            }
            
            // Verify hash
            var dllPath = FindPluginMainDll(extractPath, resolvedPluginId);
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

            // SECURITY: Validate plugin ID before using in path construction
            if (!PathSecurity.IsValidPluginId(manifest.Id))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"SECURITY: Invalid plugin ID format: {manifest.Id}");
                return false;
            }

            // Copy to plugins directory
            var pluginDir = Path.Combine(_pluginsDirectory, manifest.Id);
            
            // SECURITY: Verify the constructed path is within allowed directory
            if (!PathSecurity.IsPathWithinAllowedDirectory(pluginDir, _pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"SECURITY: Plugin directory path traversal detected: {pluginDir}");
                return false;
            }
            if (Directory.Exists(pluginDir))
            {
                try
                {
                    backupDir = $"{pluginDir}_backup_{DateTime.Now:yyyyMMddHHmmss}";
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

            if (!string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir))
            {
                try
                {
                    Directory.Delete(backupDir, true);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to clean up plugin backup directory {backupDir}: {ex.Message}", ex);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            await RestorePluginDirectoryAsync(pluginDir, backupDir, manifest.Id).ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error extracting plugin {manifest.Id}: {ex.Message}", ex);
            return false;
        }
    }

    private static Task RestorePluginDirectoryAsync(string pluginDir, string? backupDir, string pluginId)
    {
        try
        {
            if (Directory.Exists(pluginDir))
                Directory.Delete(pluginDir, true);

            if (!string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir))
            {
                Directory.Move(backupDir, pluginDir);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rolled back plugin directory for {pluginId} from backup {backupDir}.");
            }
        }
        catch (Exception restoreEx)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to roll back plugin directory for {pluginId}: {restoreEx.Message}", restoreEx);
        }

        return Task.CompletedTask;
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
        return $"https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases/download/{manifest.Id}-v{manifest.Version}/{manifest.Id}-v{manifest.Version}.zip";
    }

    private async Task<string> FetchStoreJsonFromRemoteAsync()
    {
        Exception? lastException = null;

        foreach (var url in PluginStoreUrls)
        {
            for (var attempt = 1; attempt <= RemoteRequestRetryCount; attempt++)
            {
                try
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Fetching store.json from GitHub: {url} (attempt {attempt}/{RemoteRequestRetryCount})");

                    using var request = CreateGetRequest(url);
                    using var cts = new CancellationTokenSource(StoreRequestTimeout);
                    using var response = await _httpClient
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                        .ConfigureAwait(false);

                    if (attempt < RemoteRequestRetryCount && IsRetryableStatusCode(response.StatusCode))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Store metadata request to {url} returned {(int)response.StatusCode} {response.StatusCode}. Retrying...");

                        await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var storeJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    TryWriteStoreCache(storeJson);
                    return storeJson;
                }
                catch (Exception ex) when (attempt < RemoteRequestRetryCount && IsTransientRemoteException(ex))
                {
                    lastException = ex;

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Transient failure fetching store.json from {url} on attempt {attempt}/{RemoteRequestRetryCount}: {ex.Message}. Retrying...", ex);

                    await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to fetch store.json from {url}: {ex.Message}", ex);

                    break;
                }
            }
        }

        var cachedStoreJson = TryReadStoreCache();
        if (!string.IsNullOrWhiteSpace(cachedStoreJson))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Using cached plugin store metadata from {_storeCachePath}");

            return cachedStoreJson;
        }

        throw new HttpRequestException("Failed to fetch plugin store metadata from all known URLs.", lastException);
    }

    private async Task<PublishedPluginAsset?> TryResolvePublishedAssetAsync(PluginManifest manifest)
    {
        for (var attempt = 1; attempt <= RemoteRequestRetryCount; attempt++)
        {
            try
            {
                using var request = CreateGetRequest(PluginReleasesApiUrl);
                using var cts = new CancellationTokenSource(ReleaseMetadataRequestTimeout);
                using var response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                    .ConfigureAwait(false);

                if (attempt < RemoteRequestRetryCount && IsRetryableStatusCode(response.StatusCode))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Published asset metadata request for plugin {manifest.Id} returned {(int)response.StatusCode} {response.StatusCode}. Retrying...");

                    await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

                foreach (var release in document.RootElement.EnumerateArray())
                {
                    if (release.TryGetProperty("draft", out var draftElement) && draftElement.GetBoolean())
                        continue;

                    if (release.TryGetProperty("prerelease", out var prereleaseElement) && prereleaseElement.GetBoolean())
                        continue;

                    if (!release.TryGetProperty("assets", out var assetsElement) || assetsElement.ValueKind != JsonValueKind.Array)
                        continue;

                    var tagName = release.TryGetProperty("tag_name", out var tagNameElement)
                        ? tagNameElement.GetString() ?? string.Empty
                        : string.Empty;

                    foreach (var asset in assetsElement.EnumerateArray())
                    {
                        var assetName = asset.TryGetProperty("name", out var assetNameElement)
                            ? assetNameElement.GetString() ?? string.Empty
                            : string.Empty;

                        if (!IsMatchingPublishedPluginAsset(assetName, manifest.Id))
                            continue;

                        var browserDownloadUrl = asset.TryGetProperty("browser_download_url", out var browserDownloadUrlElement)
                            ? browserDownloadUrlElement.GetString()
                            : null;
                        var apiDownloadUrl = asset.TryGetProperty("url", out var apiUrlElement)
                            ? apiUrlElement.GetString()
                            : null;

                        if (string.IsNullOrWhiteSpace(browserDownloadUrl) && string.IsNullOrWhiteSpace(apiDownloadUrl))
                            continue;

                        return new PublishedPluginAsset(
                            browserDownloadUrl,
                            apiDownloadUrl,
                            assetName,
                            ExtractPublishedAssetVersion(assetName, tagName, manifest.Id));
                    }
                }
            }
            catch (Exception ex) when (attempt < RemoteRequestRetryCount && IsTransientRemoteException(ex))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Transient failure resolving published GitHub asset for plugin {manifest.Id} on attempt {attempt}/{RemoteRequestRetryCount}: {ex.Message}. Retrying...", ex);

                await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to resolve published GitHub asset for plugin {manifest.Id}: {ex.Message}", ex);

                return null;
            }
        }

        return null;
    }

    private static bool IsMatchingPublishedPluginAsset(string assetName, string pluginId)
    {
        if (string.IsNullOrWhiteSpace(assetName) || !assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return false;

        return assetName.Equals($"{pluginId}.zip", StringComparison.OrdinalIgnoreCase) ||
               assetName.StartsWith($"{pluginId}-", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractPublishedAssetVersion(string assetName, string tagName, string pluginId)
    {
        if (!string.IsNullOrWhiteSpace(assetName))
        {
            var prefix = $"{pluginId}-v";
            if (assetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return assetName[prefix.Length..^4];
            }
        }

        var tagPrefix = $"{pluginId}-v";
        if (!string.IsNullOrWhiteSpace(tagName) && tagName.StartsWith(tagPrefix, StringComparison.OrdinalIgnoreCase))
            return tagName[tagPrefix.Length..];

        return null;
    }

    private sealed record PublishedPluginAsset(string? DownloadUrl, string? ApiDownloadUrl, string AssetName, string? Version);

    private static HttpRequestMessage CreateGetRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
        };

        if (IsGitHubReleaseAssetApiUrl(url))
        {
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        }

        return request;
    }

    private static bool IsTransientRemoteException(Exception ex)
    {
        return ex is HttpRequestException
            or IOException
            or TaskCanceledException
            or TimeoutException;
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout
               || statusCode == (HttpStatusCode)429
               || statusCode == HttpStatusCode.BadGateway
               || statusCode == HttpStatusCode.ServiceUnavailable
               || statusCode == HttpStatusCode.GatewayTimeout
               || (int)statusCode >= 500;
    }

    private static TimeSpan GetRetryDelay(int attempt)
    {
        var milliseconds = Math.Min(4000, 500 * attempt * attempt);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static bool IsGitHubReleaseAssetApiUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase)
               && uri.AbsolutePath.Contains("/releases/assets/", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan GetDownloadTimeout(string candidateUrl)
    {
        return IsGitHubReleaseAssetApiUrl(candidateUrl)
            ? ApiAssetDownloadRequestTimeout
            : BrowserDownloadRequestTimeout;
    }

    private void TryWriteStoreCache(string storeJson)
    {
        try
        {
            var directory = Path.GetDirectoryName(_storeCachePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_storeCachePath, storeJson, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to update plugin store cache at {_storeCachePath}: {ex.Message}", ex);
        }
    }

    private string? TryReadStoreCache()
    {
        try
        {
            if (!File.Exists(_storeCachePath))
                return null;

            return File.ReadAllText(_storeCachePath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read plugin store cache at {_storeCachePath}: {ex.Message}", ex);
            return null;
        }
    }

    private static void DeletePartialDownload(string destinationPath)
    {
        try
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);
        }
        catch
        {
            // Ignore partial cleanup failures; a later successful download uses overwrite semantics.
        }
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
        var overridePath = PluginPaths.GetPluginsDirectoryOverride();
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        var possiblePaths = new[]
        {
            Path.Combine(appBaseDir, "plugins"),
            Path.Combine(appBaseDir, "Plugins"),
            Path.Combine(appBaseDir, "build", "plugins"),
            Path.Combine(appBaseDir, "..", "..", "..", "build", "plugins"),
            Path.Combine(appBaseDir, "..", "build", "plugins"),
        };

        foreach (var possiblePath in possiblePaths)
        {
            var fullPath = Path.GetFullPath(possiblePath);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        var defaultPath = Path.Combine(appBaseDir, "plugins");
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
