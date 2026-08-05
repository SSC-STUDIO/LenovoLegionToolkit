using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Plugins.Resources;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
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

                    IEnumerable<JsonElement> releases = document.RootElement.ValueKind == JsonValueKind.Array
                        ? document.RootElement.EnumerateArray()
                        : new[] { document.RootElement };

                    foreach (var release in releases)
                    {
                        if (release.TryGetProperty("draft", out var draftElement) && draftElement.GetBoolean())
                            continue;

                        if (release.TryGetProperty("prerelease", out var prereleaseElement) && prereleaseElement.GetBoolean())
                            continue;

                        var tagName = release.TryGetProperty("tag_name", out var tagNameElement)
                            ? tagNameElement.GetString() ?? string.Empty
                            : string.Empty;

                        if (!tagName.Equals("plugin-catalog", StringComparison.OrdinalIgnoreCase) ||
                            !release.TryGetProperty("assets", out var assetsElement) ||
                            assetsElement.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var asset in assetsElement.EnumerateArray())
                        {
                            var assetName = asset.TryGetProperty("name", out var assetNameElement)
                                ? assetNameElement.GetString() ?? string.Empty
                                : string.Empty;

                            if (!IsMatchingPublishedPluginAsset(assetName, manifest.Id, manifest.Version))
                                continue;

                            var browserDownloadUrl = asset.TryGetProperty("browser_download_url", out var browserDownloadUrlElement)
                                ? browserDownloadUrlElement.GetString()
                                : null;
                            var apiDownloadUrl = asset.TryGetProperty("url", out var apiUrlElement)
                                ? apiUrlElement.GetString()
                                : null;

                            var hasTrustedBrowserDownloadUrl = !string.IsNullOrWhiteSpace(browserDownloadUrl) &&
                                ShouldTrustDownloadedPluginPackage(browserDownloadUrl, manifest.Id, manifest.Version);
                            var hasTrustedApiDownloadUrl = !string.IsNullOrWhiteSpace(apiDownloadUrl) &&
                                IsTrustedGitHubReleaseAssetApiUrl(
                                    apiDownloadUrl,
                                    assetName,
                                    manifest.Id,
                                    manifest.Version);

                            if (!hasTrustedBrowserDownloadUrl && !hasTrustedApiDownloadUrl)
                                continue;

                            return new PublishedPluginAsset(
                                hasTrustedBrowserDownloadUrl ? browserDownloadUrl : null,
                                hasTrustedApiDownloadUrl ? apiDownloadUrl : null,
                                assetName);
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

    private static bool ShouldTrustDownloadedPluginPackage(string candidateUrl, string pluginId, string pluginVersion)
    {
        if (string.IsNullOrWhiteSpace(candidateUrl) || string.IsNullOrWhiteSpace(pluginId))
            return false;

        if (!Uri.TryCreate(candidateUrl, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        if (GitHubDownloadMirrors.IsMirrorHost(uri.Host))
        {
            var innerUrl = uri.AbsolutePath.TrimStart('/');
            return ShouldTrustDownloadedPluginPackage(innerUrl, pluginId, pluginVersion);
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            return IsTrustedGitHubBrowserDownloadPath(segments, pluginId, pluginVersion);

        if (uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase))
        {
            // An API asset URL contains only an opaque numeric ID, not the asset
            // filename. It is trusted only after it is paired with the exact
            // filename returned by the fixed plugin-catalog release metadata.
            return false;
        }

        return false;
    }

    private static bool IsTrustedGitHubBrowserDownloadPath(
        IReadOnlyList<string> segments,
        string pluginId,
        string pluginVersion)
    {
        if (segments.Count != 6 || !IsOfficialPluginRepository(segments[0], segments[1]))
            return false;

        if (!segments[2].Equals("releases", StringComparison.OrdinalIgnoreCase) ||
            !segments[3].Equals("download", StringComparison.OrdinalIgnoreCase) ||
            !segments[4].Equals("plugin-catalog", StringComparison.OrdinalIgnoreCase))
            return false;

        return IsMatchingPublishedPluginAsset(segments[5], pluginId, pluginVersion);
    }

    private static bool IsTrustedGitHubReleaseAssetApiUrl(
        string candidateUrl,
        string assetName,
        string pluginId,
        string pluginVersion)
    {
        if (!Uri.TryCreate(candidateUrl, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase))
            return false;

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        return IsTrustedGitHubReleaseAssetApiPath(segments, assetName, pluginId, pluginVersion);
    }

    private static bool IsTrustedGitHubReleaseAssetApiPath(
        IReadOnlyList<string> segments,
        string assetName,
        string pluginId,
        string pluginVersion)
    {
        return segments.Count == 6 &&
               segments[0].Equals("repos", StringComparison.OrdinalIgnoreCase) &&
               IsOfficialPluginRepository(segments[1], segments[2]) &&
               segments[3].Equals("releases", StringComparison.OrdinalIgnoreCase) &&
               segments[4].Equals("assets", StringComparison.OrdinalIgnoreCase) &&
               long.TryParse(segments[5], out _) &&
               IsMatchingPublishedPluginAsset(assetName, pluginId, pluginVersion);
    }

    private static bool IsOfficialPluginRepository(string owner, string repository)
    {
        return owner.Equals("SSC-STUDIO", StringComparison.OrdinalIgnoreCase) &&
               repository.Equals("UniversalDeviceToolkit", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMatchingPublishedPluginAsset(string assetName, string pluginId, string pluginVersion)
    {
        if (string.IsNullOrWhiteSpace(assetName) ||
            string.IsNullOrWhiteSpace(pluginId) ||
            string.IsNullOrWhiteSpace(pluginVersion))
            return false;

        return assetName.Equals(
            $"{pluginId}-v{pluginVersion}.zip",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublishedAssetCandidate(string candidateUrl, PublishedPluginAsset publishedAsset)
    {
        return string.Equals(candidateUrl, publishedAsset.DownloadUrl, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidateUrl, publishedAsset.ApiDownloadUrl, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record PublishedPluginAsset(string? DownloadUrl, string? ApiDownloadUrl, string AssetName);

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
        return ex is HttpRequestException { StatusCode: null }
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
}
