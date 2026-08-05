using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    /// <summary>
    /// Download plugin package.
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

        }

        foreach (var candidateUrl in candidateUrls
                     .SelectMany(GitHubDownloadMirrors.WithMirrorFallbacks)
                     .Where(IsUrlAllowed))
        {
            var downloaded = await TryDownloadPluginFromUrlAsync(manifest, candidateUrl, destinationPath).ConfigureAwait(false);
            if (!downloaded)
                continue;

            var trustAsOfficial = (publishedAsset is not null &&
                                   IsPublishedAssetCandidate(candidateUrl, publishedAsset)) ||
                                  ShouldTrustDownloadedPluginPackage(candidateUrl, manifest.Id, manifest.Version);
            if (await VerifyDownloadedPackageIntegrityAsync(destinationPath, manifest, trustAsOfficial).ConfigureAwait(false))
                return new PluginDownloadResult(Success: true, TrustAsOfficialOnlinePackage: trustAsOfficial);

            // Corrupted or truncated payload (e.g. a mirror that closed the connection
            // mid-body) — do not stop at this candidate; try the next one.
            Log.Instance.Warning($"Plugin package integrity failed for {manifest.Id} from {candidateUrl}; trying next candidate.");
            DeletePartialDownload(destinationPath);
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
            if (preferNativeCurl &&
                await TryDownloadPluginWithNativeCurlAsync(manifest, candidateUrl, destinationPath).ConfigureAwait(false))
            {
                return true;
            }
            // Fall through to the managed retry loop when curl is unavailable or fails.

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

                    // A proxy or server that closes the connection mid-body ends the read
                    // loop without an exception; treat a short body as a transient failure
                    // so we retry, then fall through to the next mirror candidate.
                    if (totalBytes > 0 && bytesDownloaded < totalBytes)
                    {
                        DeletePartialDownload(destinationPath);

                        if (attempt < RemoteDownloadRetryCount)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Incomplete download for plugin {manifest.Id} from {candidateUrl}: {bytesDownloaded}/{totalBytes} bytes on attempt {attempt}/{RemoteDownloadRetryCount}. Retrying...");

                            await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
                            continue;
                        }

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Download URL failed for plugin {manifest.Id}: {candidateUrl} incomplete body {bytesDownloaded}/{totalBytes} bytes");
                        return false;
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
}
