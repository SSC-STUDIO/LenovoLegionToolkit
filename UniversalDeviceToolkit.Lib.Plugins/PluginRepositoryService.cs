using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Plugins.Resources;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Service for managing online plugin repository
/// </summary>
public partial class PluginRepositoryService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IPluginManager _pluginManager;
    private readonly string _pluginsDirectory;
    private readonly string _tempDownloadDirectory;
    private readonly string _storeCachePath;
    private readonly Action<string, string> _moveDirectory;
    private readonly Action<string> _deleteDirectory;
    private readonly Func<string, string, bool> _atomicMoveSupported;
    private readonly Action<string> _repositoryMutationBoundary;
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private const string StoreCacheAppSeed = "UDT_PluginStoreCache_v2";
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

    // The catalog and plugin packages are published together in one rolling release
    // so the main repository's Releases page stays readable. Preview hosts read
    // plugin-catalog-preview; stable hosts keep plugin-catalog.
    private readonly string _catalogTag;
    private readonly bool _allowCatalogPrerelease;
    private readonly string[] _pluginStoreUrls;
    private readonly string[] _pluginReleasesApiUrls;
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
        : this(pluginManager, httpClientFactory, forceAllowFileUrls, informationalVersion: null)
    {
    }

    internal PluginRepositoryService(
        IPluginManager pluginManager,
        HttpClientFactory httpClientFactory,
        bool forceAllowFileUrls,
        string? informationalVersion,
        Action<string, string>? moveDirectory = null,
        Action<string>? deleteDirectory = null,
        Func<string, string, bool>? atomicMoveSupported = null,
        Action<string>? mutationBoundary = null)
    {
        _pluginManager = pluginManager;
        _httpClient = httpClientFactory.Create();
        _forceAllowFileUrls = forceAllowFileUrls;
        _moveDirectory = moveDirectory ?? Directory.Move;
        _deleteDirectory = deleteDirectory ?? (path => Directory.Delete(path, recursive: true));
        _atomicMoveSupported = atomicMoveSupported ?? PluginInstallationService.ProbeAtomicDirectoryMove;
        _repositoryMutationBoundary = mutationBoundary ?? (static _ => { });
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "UniversalDeviceToolkit-PluginManager");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        _catalogTag = PluginCatalogTags.ResolveTag(informationalVersion ?? ReadInformationalVersion());
        _allowCatalogPrerelease = string.Equals(_catalogTag, PluginCatalogTags.Preview, StringComparison.OrdinalIgnoreCase);
        _pluginStoreUrls = [PluginCatalogTags.StoreDownloadUrl(_catalogTag)];
        _pluginReleasesApiUrls = [PluginCatalogTags.ReleasesApiUrl(_catalogTag)];

        _pluginsDirectory = GetPluginsDirectory();
        _tempDownloadDirectory = Path.Combine(Path.GetTempPath(), "UDTPluginDownloads");
        _storeCachePath = Path.Combine(Folders.AppData, "plugin-store-cache.json");

        if (!Directory.Exists(_tempDownloadDirectory))
        {
            Directory.CreateDirectory(_tempDownloadDirectory);
        }
    }

    private static string? ReadInformationalVersion() =>
        typeof(PluginRepositoryService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

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

    private static readonly HashSet<string> AllowedDownloadHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "github.com",
        "api.github.com",
        "raw.githubusercontent.com",
        "jsdelivr.net",
        "cdn.jsdelivr.net",
        "gh-proxy.com",
        "ghfast.top",
    };

    private List<string> GetDownloadUrlCandidates(PluginManifest manifest)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(manifest.DownloadUrl) &&
            (manifest.DownloadUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
             ShouldTrustDownloadedPluginPackage(manifest.DownloadUrl, manifest.Id, manifest.Version)))
        {
            candidates.Add(manifest.DownloadUrl);
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
            KillProcessTree(process);
            DeletePartialDownload(destinationPath);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Native curl fallback timed out for plugin {manifest.Id} from {candidateUrl}");

            return false;
        }
        catch (Exception ex)
        {
            KillProcessTree(process);
            DeletePartialDownload(destinationPath);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Native curl fallback failed for plugin {manifest.Id} from {candidateUrl}: {ex.Message}", ex);

            return false;
        }
        finally
        {
            KillProcessTree(process);
            process?.Dispose();
        }
    }

    private static void KillProcessTree(Process? process)
    {
        if (process is not { HasExited: false })
            return;

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
        merged.ZipHash = FirstNonEmpty(merged.ZipHash, storeManifest.ZipHash);
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
        
        // Official plugin packages are assets of the main repository's rolling catalog release.
        return PluginCatalogTags.PackageDownloadUrl(_catalogTag, manifest.Id, manifest.Version);
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
        // A just-finished (or just-killed) downloader may still hold the file handle
        // for a brief moment; retry a few times before giving up.
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
                return;
            }
            catch (Exception ex) when (attempt < 3)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Retrying partial plugin download cleanup (attempt {attempt}/3): {destinationPath} ({ex.Message})");
                Thread.Sleep(250);
            }
            catch (Exception ex)
            {
                Log.Instance.TraceOnce(
                    "plugin-repo-partial-cleanup",
                    $"Failed to delete partial plugin download: {destinationPath}",
                    ex);
            }
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
