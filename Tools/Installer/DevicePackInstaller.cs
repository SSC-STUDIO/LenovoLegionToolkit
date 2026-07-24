using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Installer;

/// <summary>
/// Pre-seeds the selected device pack's metadata into the app's device-packs
/// folder, so even an older app build whose built-in catalog lacks the pack
/// still recognizes it (installed catalog merges over built-in). Same catalog
/// and verification discipline as the language-pack pre-seed; best-effort.
/// </summary>
internal static class DevicePackInstaller
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
        string packId,
        string version,
        CancellationToken ct)
    {
        try
        {
            var resource = await FindPackResourceAsync(packId, ct).ConfigureAwait(false);
            if (resource is null)
            {
                InstallerLog.Info($"Device pack '{packId}' not in the online catalog; relying on the app catalog.");
                return false;
            }

            var raw = $"https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit/master/resources/stable/{version}/devices/{packId}.zip";
            string[] candidates =
            [
                resource.Value.Url,
                raw,
                "https://gh-proxy.com/" + raw,
                "https://ghfast.top/" + raw,
            ];

            var progress = new Progress<DownloadProgress>(_ => { });
            var zipPath = await Downloader.DownloadToTempFileAsync(candidates, resource.Value.Sha256 ?? "", progress, ct)
                .ConfigureAwait(false);

            try
            {
                InstallZip(zipPath, packId);
                InstallerLog.Info($"Device pack '{packId}' installed into the app data folder.");
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
            InstallerLog.Error($"Device pack '{packId}' install failed (the app retries at runtime)", ex);
            return false;
        }
    }

    private static async Task<(string Url, string? Sha256)?> FindPackResourceAsync(string packId, CancellationToken ct)
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
                    if (!document.RootElement.TryGetProperty("devicePacks", out var packs))
                        return null;

                    foreach (var pack in packs.EnumerateArray())
                    {
                        var id = pack.TryGetProperty("id", out var i) ? i.GetString() : null;
                        if (!string.Equals(id, packId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var url = pack.TryGetProperty("url", out var u) ? u.GetString() : null;
                        var sha = pack.TryGetProperty("sha256", out var s) ? s.GetString() : null;
                        return string.IsNullOrWhiteSpace(url) ? null : (url, sha);
                    }

                    return null;
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

    private static void InstallZip(string zipPath, string packId)
    {
        var destinationDir = Path.Combine(InstallerConstants.AppDataDir, "device-packs", packId);

        using var archive = ZipFile.OpenRead(zipPath);
        var manifestEntry = archive.GetEntry("device-pack.json")
            ?? throw new InvalidDataException($"Device pack '{packId}' has no device-pack.json manifest.");

        // Manifest id must match the requested pack (same guard as the app).
        using (var reader = new StreamReader(manifestEntry.Open()))
        {
            using var document = JsonDocument.Parse(reader.ReadToEnd());
            var manifestId = document.RootElement.TryGetProperty("id", out var i) ? i.GetString() : null;
            if (!string.Equals(manifestId, packId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Device pack manifest id '{manifestId}' does not match '{packId}'.");
        }

        var swapDir = destinationDir + ".pending";
        try
        {
            if (Directory.Exists(swapDir))
                Directory.Delete(swapDir, recursive: true);
            Directory.CreateDirectory(swapDir);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) || entry.FullName.Contains('/') || entry.FullName.Contains('\\'))
                    continue; // data-only, flat layout
                entry.ExtractToFile(Path.Combine(swapDir, entry.FullName), overwrite: true);
            }

            if (Directory.Exists(destinationDir))
                Directory.Delete(destinationDir, recursive: true);
            Directory.Move(swapDir, destinationDir);
        }
        finally
        {
            try { if (Directory.Exists(swapDir)) Directory.Delete(swapDir, recursive: true); } catch { /* best effort */ }
        }
    }
}
