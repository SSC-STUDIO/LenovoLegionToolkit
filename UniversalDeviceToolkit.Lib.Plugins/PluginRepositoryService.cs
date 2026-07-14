using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using UniversalDeviceToolkit.Lib.Plugins.Resources;

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
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private const string StoreCacheAppSeed = "UDT_PluginStoreCache_v1";
    private static readonly byte[] StoreCacheHmacKey;

    static PluginRepositoryService()
    {
        using var derive = new HMACSHA256(Encoding.UTF8.GetBytes(StoreCacheAppSeed));
        StoreCacheHmacKey = derive.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
    }

    private static readonly string[] InstalledManifestFileNames =
    [
        "plugin.manifest.json",
        "plugin.json",
        "Plugin.json"
    ];

    // The plugin store is currently published from master.
    // Keep the source list explicit so the app does not waste time hitting the missing main/store.json endpoint first,
    // and include a CDN mirror because raw.githubusercontent.com can intermittently reset connections on Windows.
    private static readonly string[] PluginStoreUrls =
    {
        "https://cdn.jsdelivr.net/gh/SSC-STUDIO/UniversalDeviceToolkit-Plugins@master/store.json",
        "https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/master/store.json",
        "https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/refs/heads/master/store.json",
        "https://cdn.jsdelivr.net/gh/SSC-STUDIO/LenovoLegionToolkit-Plugins@master/store.json",
        "https://raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/master/store.json",
        "https://raw.githubusercontent.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/refs/heads/master/store.json"
    };
    private static readonly string[] PluginReleasesApiUrls =
    {
        "https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases?per_page=50",
        "https://api.github.com/repos/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases?per_page=50"
    };
    private const int RemoteRequestRetryCount = 3;
    private const int RemoteDownloadRetryCount = 3;
    private static readonly TimeSpan StoreRequestTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ReleaseMetadataRequestTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ApiAssetDownloadRequestTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan BrowserDownloadRequestTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan NativeCurlDownloadTimeoutPadding = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AvailablePluginsMemoryCacheDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StoreDiskCacheDuration = TimeSpan.FromHours(6);

    private static bool IsProductionMode
    {
        get
        {
#if DEBUG
            return false;
#else
            return true;
#endif
        }
    }

    private bool _forceAllowFileUrls;
    private readonly object _availablePluginsCacheLock = new();
    private List<PluginManifest>? _availablePluginsMemoryCache;
    private DateTimeOffset _availablePluginsMemoryCacheUpdatedAt;

    public event EventHandler<PluginDownloadProgress>? DownloadProgressChanged;
    public event EventHandler<string>? DownloadCompleted;
    public event EventHandler<string>? DownloadFailed;

    public PluginRepositoryService(IPluginManager pluginManager, HttpClientFactory httpClientFactory)
        : this(pluginManager, httpClientFactory, false)
    {
    }

    public PluginRepositoryService(IPluginManager pluginManager, HttpClientFactory httpClientFactory, bool forceAllowFileUrls)
    {
        _pluginManager = pluginManager;
        _httpClient = httpClientFactory.Create();
        _forceAllowFileUrls = forceAllowFileUrls;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "UniversalDeviceToolkit-PluginManager");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        _pluginsDirectory = GetPluginsDirectory();
        _tempDownloadDirectory = Path.Combine(Path.GetTempPath(), "UDTPluginDownloads");
        _storeCachePath = Path.Combine(Folders.AppData, "plugin-store-cache.json");

        if (!Directory.Exists(_tempDownloadDirectory))
        {
            Directory.CreateDirectory(_tempDownloadDirectory);
        }
    }

    /// <summary>
    /// Fetch available plugins from the online repository
    /// </summary>
    public async Task<List<PluginManifest>> FetchAvailablePluginsAsync(bool forceRefresh = false)
    {
        try
        {
            if (!forceRefresh && TryGetCachedAvailablePlugins(out var cachedPlugins))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Using in-memory plugin store cache with {cachedPlugins.Count} plugins");

                return cachedPlugins;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Fetching plugins from online repository...");

            // Try local file first for development
            var localStorePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "store.json");
            string storeJson;
            if (File.Exists(localStorePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Using local store.json file at {localStorePath}");
                
                storeJson = await File.ReadAllTextAsync(localStorePath).ConfigureAwait(false);
            }
            else if (!forceRefresh && TryReadFreshStoreCache() is { } cachedStoreJson)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Using fresh plugin store disk cache from {_storeCachePath}");

                storeJson = cachedStoreJson;
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

                throw new InvalidDataException(Resource.Plugin_Error_Repository_Deserialize);
            }

            // Delist Offline / migration-only / Removed packages from the marketplace UI.
            var plugins = storeResponse.Plugins
                .Where(manifest => manifest.IsListedInStore)
                .Select(manifest =>
                {
                    // Use the download URL from store.json if available, otherwise generate one
                    if (string.IsNullOrEmpty(manifest.DownloadUrl))
                    {
                        manifest.DownloadUrl = GetPluginDownloadUrl(manifest);
                    }
                    return manifest;
                }).ToList();

            Log.Instance.Info($"Found {plugins.Count} listed plugins in store");

            CacheAvailablePlugins(plugins);
            return ClonePluginManifestList(plugins);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error fetching plugins from store: {ex.Message}", ex);
            throw;
        }
    }

    public bool TryGetCachedAvailablePlugins(out List<PluginManifest> plugins)
    {
        lock (_availablePluginsCacheLock)
        {
            if (_availablePluginsMemoryCache is null ||
                DateTimeOffset.UtcNow - _availablePluginsMemoryCacheUpdatedAt > AvailablePluginsMemoryCacheDuration)
            {
                plugins = new List<PluginManifest>();
                return false;
            }

            plugins = ClonePluginManifestList(_availablePluginsMemoryCache);
            return true;
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
                var compatibilityMessage = string.Format(
                    Resource.Plugin_Error_Repository_HostIncompatible,
                    manifest.Id,
                    manifest.MinimumHostVersion);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(compatibilityMessage);

                DownloadFailed?.Invoke(this, compatibilityMessage);
                return false;
            }

            // Create temporary download path
            var tempFilePath = Path.Combine(_tempDownloadDirectory, $"{manifest.Id}.zip");

            // Download the plugin
            var downloadResult = await DownloadPluginAsync(manifest, tempFilePath).ConfigureAwait(false);
            if (!downloadResult.Success)
            {
                DownloadFailed?.Invoke(this, string.Format(Resource.Plugin_Error_Repository_DownloadFailed, manifest.Id));
                return false;
            }

            // Extract and install
            var extractPath = Path.Combine(_tempDownloadDirectory, manifest.Id);
            var installed = await ExtractAndInstallPluginAsync(
                tempFilePath,
                extractPath,
                manifest,
                downloadResult.TrustAsOfficialOnlinePackage).ConfigureAwait(false);

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
                await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);

                if (!IsInstalledPluginUsable(manifest))
                {
                    var error = string.Format(Resource.Plugin_Error_Repository_NotLoadable, manifest.Id);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace(error);

                    await RemoveUnusableInstalledPayloadAsync(manifest.Id).ConfigureAwait(false);
                    DownloadFailed?.Invoke(this, error);
                    return false;
                }

                _pluginManager.InstallPlugin(manifest.Id);
                await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);

                DownloadCompleted?.Invoke(this, manifest.Id);
            }

            return installed;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error installing plugin {manifest.Id}: {ex.Message}", ex);
            DownloadFailed?.Invoke(this, ex.Message);
            return false;
        }
    }

    private bool IsInstalledPluginUsable(PluginManifest manifest)
    {
        if (_pluginManager.TryGetPlugin(manifest.Id, out var plugin) && plugin is not null and not PluginManifestAdapter)
            return true;

        return PluginUiCapabilityResolver.ResolveFromManifest(manifest).HasAny ||
               PluginUiCapabilityResolver.ResolveFromInstalledManifest(manifest.Id).HasAny;
    }

    private Task RemoveUnusableInstalledPayloadAsync(string pluginId)
    {
        TrustedPluginPackageStore.Remove(pluginId);
        return RestorePluginDirectoryAsync(Path.Combine(_pluginsDirectory, pluginId), backupDir: null, pluginId);
    }

    /// <summary>
    /// Download plugin package
    /// </summary>
    private async Task<PluginDownloadResult> DownloadPluginAsync(PluginManifest manifest, string destinationPath)
    {
        var candidateUrls = GetDownloadUrlCandidates(manifest);
        var publishedAsset = await TryResolvePublishedAssetAsync(manifest).ConfigureAwait(false);

        if (publishedAsset is not null)
        {
            var preferredCandidateUrls = new List<string>();

            if (!string.IsNullOrWhiteSpace(publishedAsset.DownloadUrl))
                preferredCandidateUrls.Add(publishedAsset.DownloadUrl);

            preferredCandidateUrls.AddRange(candidateUrls);

            if (!string.IsNullOrWhiteSpace(publishedAsset.ApiDownloadUrl))
                preferredCandidateUrls.Add(publishedAsset.ApiDownloadUrl);

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
                return new PluginDownloadResult(
                    Success: true,
                    TrustAsOfficialOnlinePackage: ShouldTrustDownloadedPluginPackage(candidateUrl, manifest.Id));
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
            return new PluginDownloadResult(Success: true, TrustAsOfficialOnlinePackage: false);
        }

        if (Log.Instance.IsTraceEnabled)
        {
            var urlsText = string.Join(", ", candidateUrls);
            Log.Instance.Trace($"Error downloading plugin {manifest.Id}: all candidates failed. Tried URLs: [{urlsText}]");
        }

        return new PluginDownloadResult(Success: false, TrustAsOfficialOnlinePackage: false);
    }

    private async Task<bool> TryDownloadPluginFromUrlAsync(PluginManifest manifest, string candidateUrl, string destinationPath)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Downloading plugin {manifest.Id} from {candidateUrl}");

            if (candidateUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                if (IsProductionMode && !_forceAllowFileUrls)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Blocked file:// URL in production mode for plugin {manifest.Id}: {candidateUrl}");
                    return false;
                }

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

            // GitHub release asset URLs are more reliable through native curl on some Windows
            // machines than through the managed HTTP stack. Prefer that fast path first so the
            // UI smoke flow does not spend multiple long socket timeouts before falling back.
            var preferNativeCurl = ShouldUseNativeCurlDownloadFallback(candidateUrl);
            if (preferNativeCurl)
                return await TryDownloadPluginWithNativeCurlAsync(manifest, candidateUrl, destinationPath).ConfigureAwait(false);

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

    private static readonly HashSet<string> AllowedDownloadHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "github.com",
        "raw.githubusercontent.com",
        "jsdelivr.net",
        "cdn.jsdelivr.net",
    };

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
            .Where(IsUrlAllowed)
            .ToList();
    }

    private static bool IsUrlAllowed(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        return AllowedDownloadHosts.Contains(uri.Host);
    }

    private async Task<bool> TryDownloadPluginWithNativeCurlAsync(PluginManifest manifest, string candidateUrl, string destinationPath)
    {
        if (!ShouldUseNativeCurlDownloadFallback(candidateUrl))
            return false;

        var curlPath = Path.Combine(Environment.SystemDirectory, "curl.exe");
        if (!File.Exists(curlPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Skipping native curl fallback for plugin {manifest.Id}: curl.exe not found at {curlPath}");

            return false;
        }

        Process? process = null; // NOTE: Cross-scope process reference (try + catch) — disposal must be handled at a higher level

        try
        {
            DeletePartialDownload(destinationPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = curlPath,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            AddNativeCurlDownloadArguments(startInfo, destinationPath, candidateUrl);

            process = Process.Start(startInfo);
            if (process is null)
                return false;

            using var cts = new CancellationTokenSource(GetDownloadTimeout(candidateUrl) + NativeCurlDownloadTimeoutPadding);
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

            var standardError = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                DeletePartialDownload(destinationPath);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Native curl fallback failed for plugin {manifest.Id} from {candidateUrl} with exit code {process.ExitCode}: {standardError}");

                return false;
            }

            if (!File.Exists(destinationPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Native curl fallback for plugin {manifest.Id} completed without producing {destinationPath}");

                return false;
            }

            var fileInfo = new FileInfo(destinationPath);
            if (fileInfo.Length <= 0)
            {
                DeletePartialDownload(destinationPath);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Native curl fallback for plugin {manifest.Id} produced an empty file for {candidateUrl}");

                return false;
            }

            DownloadProgressChanged?.Invoke(this, new PluginDownloadProgress
            {
                PluginId = manifest.Id,
                BytesDownloaded = fileInfo.Length,
                TotalBytes = fileInfo.Length,
                ProgressPercentage = 100
            });

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Downloaded plugin {manifest.Id} via native curl fallback to {destinationPath}");

            manifest.DownloadUrl = candidateUrl;
            return true;
        }
        catch (OperationCanceledException)
        {
            DeletePartialDownload(destinationPath);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Native curl fallback timed out for plugin {manifest.Id} from {candidateUrl}");

            return false;
        }
        catch (Exception ex)
        {
            DeletePartialDownload(destinationPath);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Native curl fallback failed for plugin {manifest.Id} from {candidateUrl}: {ex.Message}", ex);

            return false;
        }
        finally
        {
            if (process is { HasExited: false })
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    Log.Instance.TraceOnce(
                        "plugin-repo-curl-kill",
                        "Failed to kill native curl download process during cleanup.",
                        ex);
                }
            }

            process?.Dispose();
        }
    }

    private static void AddNativeCurlDownloadArguments(ProcessStartInfo startInfo, string destinationPath, string candidateUrl)
    {
        startInfo.ArgumentList.Add("--location");
        startInfo.ArgumentList.Add("--fail");
        startInfo.ArgumentList.Add("--silent");
        startInfo.ArgumentList.Add("--show-error");

        if (OperatingSystem.IsWindows())
            startInfo.ArgumentList.Add("--ssl-revoke-best-effort");

        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(destinationPath);
        startInfo.ArgumentList.Add(candidateUrl);
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

            var localVersion = TryReadLocalPluginVersion(localPluginDirectory);
            if (!IsLocalPackageVersionUsableForFallback(manifest.Version, localVersion))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Local package fallback aborted for {manifest.Id}: local version '{localVersion ?? "<unknown>"}' is older than requested version '{manifest.Version}'");

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

    private static bool IsLocalPackageVersionUsableForFallback(string requestedVersion, string? localVersion)
    {
        if (string.IsNullOrWhiteSpace(requestedVersion))
            return true;

        if (string.IsNullOrWhiteSpace(localVersion))
            return false;

        if (PluginVersionParser.TryParse(localVersion, out var parsedLocalVersion) &&
            PluginVersionParser.TryParse(requestedVersion, out var parsedRequestedVersion))
        {
            return parsedLocalVersion >= parsedRequestedVersion;
        }

        return string.Equals(localVersion.Trim(), requestedVersion.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryReadLocalPluginVersion(string pluginDirectory)
    {
        foreach (var manifestPath in EnumerateLocalVersionManifestPaths(pluginDirectory))
        {
            try
            {
                using var stream = File.OpenRead(manifestPath);
                using var document = JsonDocument.Parse(stream);

                var version = TryGetJsonStringProperty(document.RootElement, "version");
                if (!string.IsNullOrWhiteSpace(version))
                    return version;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to read local plugin version from {manifestPath}: {ex.Message}", ex);
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateLocalVersionManifestPaths(string pluginDirectory)
    {
        var candidateNames = new[]
        {
            "plugin.manifest.json",
            "plugin.json",
            "Plugin.json"
        };

        return candidateNames
            .Select(fileName => Path.Combine(pluginDirectory, fileName))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? TryGetJsonStringProperty(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
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
    private async Task<bool> ExtractAndInstallPluginAsync(
        string zipPath,
        string extractPath,
        PluginManifest manifest,
        bool trustAsOfficialOnlinePackage)
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

            // Extract zip with path traversal protection
            var extractRoot = Path.GetFullPath(extractPath);
            if (!extractRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractRoot += Path.DirectorySeparatorChar;

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                var destinationPath = Path.GetFullPath(Path.Combine(extractRoot, entry.FullName));
                if (!destinationPath.StartsWith(extractRoot, StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException(string.Format(Resource.Plugin_Error_Repository_PathTraversal, entry.FullName));

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }

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

            if (string.IsNullOrEmpty(manifest.FileHash))
            {
                if (IsProductionMode)
                    Log.Instance.Warning($"Plugin {manifest.Id} has no fileHash in store manifest; skipping integrity verification.");
            }
            else if (!hashString.Equals(manifest.FileHash, StringComparison.OrdinalIgnoreCase))
            {
                Log.Instance.Warning($"Hash mismatch for {manifest.Id}. Expected: {manifest.FileHash}, Got: {hashString}");
                return false;
            }

            // SECURITY: Validate plugin ID before using in path construction
            if (!PathSecurity.IsValidPluginId(manifest.Id))
            {
                Log.Instance.Warning($"SECURITY: Invalid plugin ID format: {manifest.Id}");
                return false;
            }

            // SECURITY: Verify the constructed path is within allowed directory
            if (!PathSecurity.IsPathWithinAllowedDirectory(pluginDir, _pluginsDirectory))
            {
                Log.Instance.Warning($"SECURITY: Plugin directory path traversal detected: {pluginDir}");
                return false;
            }
            if (Directory.Exists(pluginDir))
            {
                try
                {
                    backupDir = $"{pluginDir}_backup_{DateTime.UtcNow:yyyyMMddHHmmss}";
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
                                catch (Exception fileEx)
                                {
                                    Log.Instance.TraceOnce(
                                        "plugin-repo-delete-file",
                                        $"Could not delete locked plugin file during reinstall: {file}",
                                        fileEx);
                                }
                            }
                        }
                        catch (Exception enumEx)
                        {
                            Log.Instance.TraceOnce(
                                "plugin-repo-delete-enum",
                                $"Could not enumerate plugin files for delete before reinstall: {pluginDir}",
                                enumEx);
                        }
                    }
                }
            }

            Directory.CreateDirectory(pluginDir);

            // Copy all files from extraction
            foreach (var file in Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories))
            {
                if (ShouldSkipPluginPayloadFile(file))
                    continue;

                // SECURITY: skip reparse points (symlinks/junctions) to prevent
                // a malicious archive from writing outside the plugin directory.
                FileInfo fileInfo;
                try
                {
                    fileInfo = new FileInfo(file);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Skipping unreadable payload entry '{file}': {ex.Message}");
                    continue;
                }

                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Skipping reparse point payload entry '{file}'.");
                    continue;
                }

                var relativePath = file.Substring(extractPath.Length).TrimStart('\\', '/');
                var destPath = Path.Combine(pluginDir, relativePath);
                
                var destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(file, destPath, overwrite: true);
            }

            EnsureInstalledManifest(pluginDir, manifest);
            TryStageCanonicalPluginSharedAssembly(pluginDir);
            TryStageCanonicalPluginSdkAssembly(pluginDir);
            if (trustAsOfficialOnlinePackage)
            {
                TrustedPluginPackageStore.TrustPluginDirectory(manifest.Id, pluginDir);
            }
            else
            {
                // A local/dev fallback uses the same marketplace install path, so clear any
                // stale trust record from a previous online install before the plugin is loaded.
                TrustedPluginPackageStore.Remove(manifest.Id);
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

            Log.Instance.Error($"Error extracting plugin {manifest.Id}: {ex.Message}", ex);
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

    private static void EnsureInstalledManifest(string pluginDir, PluginManifest storeManifest)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pluginDir) || string.IsNullOrWhiteSpace(storeManifest.Id))
                return;

            Directory.CreateDirectory(pluginDir);

            var installedManifest = TryReadInstalledManifest(pluginDir, out _);
            var manifestToWrite = MergeInstalledManifest(installedManifest, storeManifest);
            var manifestPath = Path.Combine(pluginDir, "plugin.manifest.json");

            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifestToWrite, ManifestJsonOptions));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to persist plugin manifest metadata for {storeManifest.Id}: {ex.Message}", ex);
        }
    }

    private static PluginManifest? TryReadInstalledManifest(string pluginDir, out string? manifestPath)
    {
        manifestPath = null;

        foreach (var manifestFileName in InstalledManifestFileNames)
        {
            var candidate = Path.Combine(pluginDir, manifestFileName);
            if (!File.Exists(candidate))
                continue;

            try
            {
                manifestPath = candidate;
                return JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(candidate), ManifestJsonOptions);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to read installed plugin manifest {candidate}: {ex.Message}", ex);
            }
        }

        return null;
    }

    private static PluginManifest MergeInstalledManifest(PluginManifest? installedManifest, PluginManifest storeManifest)
    {
        var merged = ClonePluginManifest(installedManifest ?? storeManifest);

        merged.Id = FirstNonEmpty(merged.Id, storeManifest.Id);
        merged.Name = FirstNonEmpty(merged.Name, storeManifest.Name, storeManifest.Id);
        merged.Description = FirstNonEmpty(merged.Description, storeManifest.Description);
        merged.Details = FirstNonEmptyNullable(merged.Details, storeManifest.Details);
        merged.UsageGuide = FirstNonEmptyNullable(merged.UsageGuide, storeManifest.UsageGuide);
        merged.Icon = FirstNonEmpty(merged.Icon, storeManifest.Icon);
        merged.IconBackground = FirstNonEmpty(merged.IconBackground, storeManifest.IconBackground);
        merged.Author = FirstNonEmpty(merged.Author, storeManifest.Author);
        merged.Version = FirstNonEmpty(merged.Version, storeManifest.Version);
        merged.MinimumHostVersion = FirstNonEmpty(merged.MinimumHostVersion, storeManifest.MinimumHostVersion);
        merged.DownloadUrl = FirstNonEmpty(merged.DownloadUrl, storeManifest.DownloadUrl);
        merged.FileHash = FirstNonEmpty(merged.FileHash, storeManifest.FileHash);
        merged.FileSize = merged.FileSize > 0 ? merged.FileSize : storeManifest.FileSize;
        merged.ReleaseDate = FirstNonEmpty(merged.ReleaseDate, storeManifest.ReleaseDate);
        merged.Changelog = FirstNonEmpty(merged.Changelog, storeManifest.Changelog);
        merged.IsSystemPlugin = merged.IsSystemPlugin || storeManifest.IsSystemPlugin;
        merged.Dependencies ??= storeManifest.Dependencies?.ToArray();
        merged.Tags ??= storeManifest.Tags?.ToArray();
        merged.LocalizedNames = MergeLocalizedStrings(merged.LocalizedNames, storeManifest.LocalizedNames);
        merged.LocalizedDescriptions = MergeLocalizedStrings(merged.LocalizedDescriptions, storeManifest.LocalizedDescriptions);
        merged.LocalizedTags = MergeLocalizedTags(merged.LocalizedTags, storeManifest.LocalizedTags);
        merged.Store ??= CloneStore(storeManifest.Store);
        merged.Localizations ??= CloneLocalizations(storeManifest.Localizations);
        merged.Contributes = MergeContributions(merged.Contributes, storeManifest.Contributes);

        return merged;
    }

    private static PluginManifestContributions? MergeContributions(
        PluginManifestContributions? installed,
        PluginManifestContributions? store)
    {
        if (installed is null)
            return CloneContributions(store);

        var merged = CloneContributions(installed)!;
        merged.FeaturePage ??= ClonePageContribution(store?.FeaturePage);
        merged.SettingsPage ??= ClonePageContribution(store?.SettingsPage);
        merged.Runtime ??= store?.Runtime is null
            ? null
            : new PluginManifestRuntimeContribution { Class = store.Runtime.Class };
        merged.OptimizationActions = MergeOptimizationActions(
            installed.OptimizationActions,
            store?.OptimizationActions);

        return merged;
    }

    private static List<PluginManifestOptimizationContribution>? MergeOptimizationActions(
        IEnumerable<PluginManifestOptimizationContribution>? installed,
        IEnumerable<PluginManifestOptimizationContribution>? store)
    {
        var actions = new List<PluginManifestOptimizationContribution>();
        var actionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddOptimizationActions(installed);
        AddOptimizationActions(store);

        return actions.Count == 0 ? null : actions;

        void AddOptimizationActions(IEnumerable<PluginManifestOptimizationContribution>? source)
        {
            if (source is null)
                return;

            foreach (var action in source)
            {
                var actionId = PluginUiCapabilityResolver.GetOptimizationActionId(action);
                if (string.IsNullOrWhiteSpace(actionId) || !actionIds.Add(actionId))
                    continue;

                actions.Add(new PluginManifestOptimizationContribution
                {
                    Id = string.IsNullOrWhiteSpace(action.Id) ? actionId : action.Id,
                    Key = action.Key,
                    Title = action.Title,
                    Description = action.Description,
                    Recommended = action.Recommended
                });
            }
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
        
        // Fall back to the renamed official plugin repository.
        return $"https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/download/{manifest.Id}-v{manifest.Version}/{manifest.Id}-v{manifest.Version}.zip";
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

        throw new HttpRequestException(Resource.Plugin_Error_Repository_FetchFailed, lastException);
    }

    private async Task<PublishedPluginAsset?> TryResolvePublishedAssetAsync(PluginManifest manifest)
    {
        foreach (var releaseApiUrl in PluginReleasesApiUrls)
        {
            for (var attempt = 1; attempt <= RemoteRequestRetryCount; attempt++)
            {
                try
                {
                    using var request = CreateGetRequest(releaseApiUrl);
                    using var cts = new CancellationTokenSource(ReleaseMetadataRequestTimeout);
                    using var response = await _httpClient
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                        .ConfigureAwait(false);

                    if (attempt < RemoteRequestRetryCount && IsRetryableStatusCode(response.StatusCode))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Published asset metadata request for plugin {manifest.Id} from {releaseApiUrl} returned {(int)response.StatusCode} {response.StatusCode}. Retrying...");

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
                        Log.Instance.Trace($"Transient failure resolving published GitHub asset for plugin {manifest.Id} from {releaseApiUrl} on attempt {attempt}/{RemoteRequestRetryCount}: {ex.Message}. Retrying...", ex);

                    await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to resolve published GitHub asset for plugin {manifest.Id} from {releaseApiUrl}: {ex.Message}", ex);

                    break;
                }
            }
        }

        return null;
    }

    private static bool ShouldTrustDownloadedPluginPackage(string candidateUrl, string pluginId)
    {
        if (string.IsNullOrWhiteSpace(candidateUrl) || string.IsNullOrWhiteSpace(pluginId))
            return false;

        if (!Uri.TryCreate(candidateUrl, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            return IsTrustedGitHubBrowserDownloadPath(segments, pluginId);

        if (uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase))
            return IsTrustedGitHubReleaseAssetApiPath(segments);

        return false;
    }

    private static bool IsTrustedGitHubBrowserDownloadPath(IReadOnlyList<string> segments, string pluginId)
    {
        if (segments.Count < 6 || !IsOfficialPluginRepository(segments[0], segments[1]))
            return false;

        if (!segments[2].Equals("releases", StringComparison.OrdinalIgnoreCase))
            return false;

        var assetName = segments[^1];
        if (!IsMatchingPublishedPluginAsset(assetName, pluginId))
            return false;

        if (segments[3].Equals("download", StringComparison.OrdinalIgnoreCase))
            return true;

        return segments.Count >= 6 &&
               segments[3].Equals("latest", StringComparison.OrdinalIgnoreCase) &&
               segments[4].Equals("download", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTrustedGitHubReleaseAssetApiPath(IReadOnlyList<string> segments)
    {
        return segments.Count >= 6 &&
               segments[0].Equals("repos", StringComparison.OrdinalIgnoreCase) &&
               IsOfficialPluginRepository(segments[1], segments[2]) &&
               segments[3].Equals("releases", StringComparison.OrdinalIgnoreCase) &&
               segments[4].Equals("assets", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOfficialPluginRepository(string owner, string repository)
    {
        if (!owner.Equals("SSC-STUDIO", StringComparison.OrdinalIgnoreCase))
            return false;

        return repository.Equals("UniversalDeviceToolkit-Plugins", StringComparison.OrdinalIgnoreCase) ||
               repository.Equals("LenovoLegionToolkit-Plugins", StringComparison.OrdinalIgnoreCase);
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

    private sealed record PluginDownloadResult(bool Success, bool TrustAsOfficialOnlinePackage);

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

    private static bool ShouldUseNativeCurlDownloadFallback(string url)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
               && uri.AbsolutePath.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase);
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

            var dataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(storeJson));
            var hmac = ComputeHmac(dataBase64);

            var envelope = new StoreCacheEnvelope
            {
                Data = dataBase64,
                Hmac = hmac,
            };

            var envelopeJson = JsonSerializer.Serialize(envelope);
            File.WriteAllText(_storeCachePath, envelopeJson, Encoding.UTF8);
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

            var envelopeJson = File.ReadAllText(_storeCachePath, Encoding.UTF8);
            return TryDecodeStoreCacheEnvelope(envelopeJson);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read plugin store cache at {_storeCachePath}: {ex.Message}", ex);
            return null;
        }
    }

    private string? TryReadFreshStoreCache()
    {
        try
        {
            if (!File.Exists(_storeCachePath))
                return null;

            var cacheAge = DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(_storeCachePath);
            if (cacheAge > StoreDiskCacheDuration)
                return null;

            var envelopeJson = File.ReadAllText(_storeCachePath, Encoding.UTF8);
            return TryDecodeStoreCacheEnvelope(envelopeJson);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read fresh plugin store cache at {_storeCachePath}: {ex.Message}", ex);
            return null;
        }
    }

    private static string? TryDecodeStoreCacheEnvelope(string envelopeJson)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<StoreCacheEnvelope>(envelopeJson);
            if (envelope is null || string.IsNullOrWhiteSpace(envelope.Data) || string.IsNullOrWhiteSpace(envelope.Hmac))
                return null;

            var computedHmac = ComputeHmac(envelope.Data);
            if (!string.Equals(envelope.Hmac, computedHmac, StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("HMAC mismatch in plugin store cache; discarding cache.");

                return null;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(envelope.Data));
        }
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "plugin-repo-store-cache-read",
                "Failed to read plugin store cache envelope; discarding cache.",
                ex);
            return null;
        }
    }

    private static string ComputeHmac(string data)
    {
        using var hmac = new HMACSHA256(StoreCacheHmacKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class StoreCacheEnvelope
    {
        public string Data { get; set; } = string.Empty;
        public string Hmac { get; set; } = string.Empty;
    }

    private static void DeletePartialDownload(string destinationPath)
    {
        try
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-repo-partial-cleanup",
                $"Failed to delete partial plugin download: {destinationPath}",
                ex);
        }
    }

    private static string? FindPluginMainDll(string extractPath, string pluginId)
    {
        var pluginDlls = Directory.GetFiles(extractPath, "*.dll", SearchOption.AllDirectories)
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase) &&
                       !fileName.Equals("LenovoLegionToolkit.Plugins.SDK.dll", StringComparison.OrdinalIgnoreCase) &&
                       !fileName.Equals("LenovoLegionToolkit.Plugins.Shared.dll", StringComparison.OrdinalIgnoreCase);
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

    private static bool ShouldSkipPluginPayloadFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.Equals("LenovoLegionToolkit.Plugins.Shared.dll", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryStageCanonicalPluginSharedAssembly(string pluginDirectory)
    {
        var sourceCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "LenovoLegionToolkit.Plugins.Shared.dll"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LenovoLegionToolkit.Plugins.Shared.dll")
        };

        var sourcePath = sourceCandidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        try
        {
            File.Copy(sourcePath, Path.Combine(pluginDirectory, "LenovoLegionToolkit.Plugins.Shared.dll"), overwrite: true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to stage canonical plugin shared runtime into {pluginDirectory}: {ex.Message}", ex);
        }
    }

    private static void TryStageCanonicalPluginSdkAssembly(string pluginDirectory)
    {
        var sourceCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "LenovoLegionToolkit.Plugins.SDK.dll"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LenovoLegionToolkit.Plugins.SDK.dll")
        };

        var sourcePath = sourceCandidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        try
        {
            File.Copy(sourcePath, Path.Combine(pluginDirectory, "LenovoLegionToolkit.Plugins.SDK.dll"), overwrite: true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to stage canonical plugin SDK runtime into {pluginDirectory}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get the plugins directory path
    /// </summary>
    private string GetPluginsDirectory()
    {
        return PluginPaths.GetPluginsDirectory();
    }

    /// <summary>
    /// Check for plugin updates
    /// </summary>
    public async Task<List<PluginManifest>> CheckForUpdatesAsync(List<PluginManifest> installedPlugins, bool forceRefresh = false)
    {
        var availablePlugins = await FetchAvailablePluginsAsync(forceRefresh).ConfigureAwait(false);
        var updates = new List<PluginManifest>();

        foreach (var installed in installedPlugins)
        {
            var available = availablePlugins.FirstOrDefault(p =>
                string.Equals(p.Id, installed.Id, StringComparison.OrdinalIgnoreCase));
            if (available == null)
                continue;

            if (PluginVersionParser.IsNewerThan(available.Version, installed.Version))
                updates.Add(available);
        }

        return updates;
    }

    private void CacheAvailablePlugins(List<PluginManifest> plugins)
    {
        lock (_availablePluginsCacheLock)
        {
            _availablePluginsMemoryCache = ClonePluginManifestList(plugins);
            _availablePluginsMemoryCacheUpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static List<PluginManifest> ClonePluginManifestList(IEnumerable<PluginManifest> plugins) =>
        plugins.Select(ClonePluginManifest).ToList();

    private static PluginManifest ClonePluginManifest(PluginManifest manifest) =>
        new()
        {
            Id = manifest.Id,
            Name = manifest.Name,
            Description = manifest.Description,
            Details = manifest.Details,
            UsageGuide = manifest.UsageGuide,
            Localizations = CloneLocalizations(manifest.Localizations),
            LocalizedNames = CloneLocalizedStrings(manifest.LocalizedNames),
            LocalizedDescriptions = CloneLocalizedStrings(manifest.LocalizedDescriptions),
            LocalizedTags = CloneLocalizedTags(manifest.LocalizedTags),
            Store = CloneStore(manifest.Store),
            Contributes = CloneContributions(manifest.Contributes),
            Icon = manifest.Icon,
            IconBackground = manifest.IconBackground,
            Author = manifest.Author,
            Version = manifest.Version,
            MinimumHostVersion = manifest.MinimumHostVersion,
            Dependencies = manifest.Dependencies?.ToArray(),
            DownloadUrl = manifest.DownloadUrl,
            FileHash = manifest.FileHash,
            FileSize = manifest.FileSize,
            ReleaseDate = manifest.ReleaseDate,
            Changelog = manifest.Changelog,
            Tags = manifest.Tags?.ToArray(),
            IsSystemPlugin = manifest.IsSystemPlugin
        };

    private static PluginManifestStore? CloneStore(PluginManifestStore? store) =>
        store is null
            ? null
            : new PluginManifestStore
            {
                Description = store.Description,
                Details = store.Details,
                UsageGuide = store.UsageGuide,
                Localizations = CloneLocalizations(store.Localizations),
                LocalizedNames = CloneLocalizedStrings(store.LocalizedNames),
                LocalizedDescriptions = CloneLocalizedStrings(store.LocalizedDescriptions),
                LocalizedTags = CloneLocalizedTags(store.LocalizedTags),
                Tags = store.Tags?.ToArray()
            };

    private static Dictionary<string, PluginManifestLocalization>? CloneLocalizations(
        Dictionary<string, PluginManifestLocalization>? localizations) =>
        localizations is null
            ? null
            : localizations.ToDictionary(
                pair => pair.Key,
                pair => new PluginManifestLocalization
                {
                    Name = pair.Value.Name,
                    Description = pair.Value.Description,
                    Details = pair.Value.Details,
                    UsageGuide = pair.Value.UsageGuide
                },
                StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string>? CloneLocalizedStrings(Dictionary<string, string>? localized) =>
        localized is null
            ? null
            : localized.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string[]>? CloneLocalizedTags(Dictionary<string, string[]>? localized) =>
        localized is null
            ? null
            : localized.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string>? MergeLocalizedStrings(
        Dictionary<string, string>? primary,
        Dictionary<string, string>? secondary)
    {
        var merged = CloneLocalizedStrings(primary) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (secondary is not null)
        {
            foreach (var pair in secondary)
                merged.TryAdd(pair.Key, pair.Value);
        }

        return merged.Count == 0 ? null : merged;
    }

    private static Dictionary<string, string[]>? MergeLocalizedTags(
        Dictionary<string, string[]>? primary,
        Dictionary<string, string[]>? secondary)
    {
        var merged = CloneLocalizedTags(primary) ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (secondary is not null)
        {
            foreach (var pair in secondary)
                merged.TryAdd(pair.Key, pair.Value.ToArray());
        }

        return merged.Count == 0 ? null : merged;
    }

    private static PluginManifestContributions? CloneContributions(PluginManifestContributions? contributes) =>
        contributes is null
            ? null
            : new PluginManifestContributions
            {
                FeaturePage = ClonePageContribution(contributes.FeaturePage),
                SettingsPage = ClonePageContribution(contributes.SettingsPage),
                Runtime = contributes.Runtime is null
                    ? null
                    : new PluginManifestRuntimeContribution
                    {
                        Class = contributes.Runtime.Class
                    },
                OptimizationActions = contributes.OptimizationActions?
                    .Select(action => new PluginManifestOptimizationContribution
                    {
                        Id = action.Id,
                        Key = action.Key,
                        Description = action.Description,
                        Recommended = action.Recommended,
                        Title = action.Title
                    })
                    .ToList()
            };

    private static PluginManifestPageContribution? ClonePageContribution(PluginManifestPageContribution? contribution) =>
        contribution is null
            ? null
            : new PluginManifestPageContribution
            {
                Class = contribution.Class,
                Title = contribution.Title
            };

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static string? FirstNonEmptyNullable(params string?[] values)
    {
        var value = FirstNonEmpty(values);
        return string.IsNullOrWhiteSpace(value) ? null : value;
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
