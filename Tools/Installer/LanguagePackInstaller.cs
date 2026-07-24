using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Installer;

/// <summary>
/// Pre-seeds a non-bundled language pack at install time so the app can start
/// directly in the chosen language. Uses the same stable resource catalog the
/// app's LanguagePackManager uses; failures never fail the install (the app
/// re-downloads the pack at runtime).
/// </summary>
internal static class LanguagePackInstaller
{
    private static readonly string[] CatalogCandidates =
    [
        "https://ssc-studio.github.io/UniversalDeviceToolkit/resources/stable/catalog.json",
        "https://cdn.jsdelivr.net/gh/SSC-STUDIO/UniversalDeviceToolkit@master/resources/stable/catalog.json",
        "https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit/master/resources/stable/catalog.json",
        "https://gh-proxy.com/https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit/master/resources/stable/catalog.json",
        "https://ghfast.top/https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit/master/resources/stable/catalog.json",
    ];

    public static async Task<bool> TryInstallAsync(
        string cultureName,
        string installDir,
        string version,
        CancellationToken ct)
    {
        try
        {
            var resource = await FindLanguageResourceAsync(cultureName, version, ct).ConfigureAwait(false);
            if (resource is null)
            {
                InstallerLog.Error($"Language '{cultureName}' not found in the resource catalog.");
                return false;
            }

            var candidates = BuildZipCandidates(resource.Value.Url, cultureName, version);
            var progress = new Progress<DownloadProgress>(_ => { });
            var zipPath = await Downloader.DownloadToTempFileAsync(candidates, resource.Value.Sha256 ?? "", progress, ct)
                .ConfigureAwait(false);

            try
            {
                ExtractLanguageZip(zipPath, installDir);
                InstallerLog.Info($"Language pack '{cultureName}' installed into '{installDir}'.");
                return true;
            }
            finally
            {
                try { File.Delete(zipPath); } catch { /* best effort */ }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            InstallerLog.Error($"Language pack '{cultureName}' install failed (the app retries at runtime)", ex);
            return false;
        }
    }

    private static async Task<(string Url, string? Sha256)?> FindLanguageResourceAsync(
        string cultureName,
        string version,
        CancellationToken ct)
    {
        foreach (var catalogUrl in CatalogCandidates)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"udt-catalog-{Guid.NewGuid():N}.json");
                try
                {
                    await Downloader.DownloadFileAsync(catalogUrl, tempPath, ct).ConfigureAwait(false);
                    var json = await File.ReadAllTextAsync(tempPath, ct).ConfigureAwait(false);

                    using var document = JsonDocument.Parse(json);
                    if (!document.RootElement.TryGetProperty("languages", out var languages))
                        return null;

                    foreach (var language in languages.EnumerateArray())
                    {
                        var culture = language.TryGetProperty("culture", out var c) ? c.GetString() : null;
                        if (!string.Equals(culture, cultureName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var url = language.TryGetProperty("url", out var u) ? u.GetString() : null;
                        var sha = language.TryGetProperty("sha256", out var s) ? s.GetString() : null;
                        return string.IsNullOrWhiteSpace(url) ? null : (url, sha);
                    }

                    return null; // catalog valid but language missing — no point trying other mirrors
                }
                finally
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                InstallerLog.Error($"Catalog candidate failed: {catalogUrl}", ex);
            }
        }

        return null;
    }

    private static string[] BuildZipCandidates(string catalogUrl, string cultureName, string version)
    {
        var raw = $"https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit/master/resources/stable/{version}/languages/{cultureName}.zip";
        return
        [
            catalogUrl,
            raw,
            "https://gh-proxy.com/" + raw,
            "https://ghfast.top/" + raw,
        ];
    }

    private static void ExtractLanguageZip(string zipPath, string installDir)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var destinationPath = Path.GetFullPath(Path.Combine(installDir, entry.FullName));
            if (!destinationPath.StartsWith(installDir, StringComparison.OrdinalIgnoreCase))
                continue; // zip-slip guard

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }
}
