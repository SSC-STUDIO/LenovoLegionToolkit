using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Serialization;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.ResourcesCatalog;

public sealed class OnlineResourceCatalogClient(HttpClientFactory httpClientFactory)
{
    public const string CatalogUrlEnvironmentVariable = "UDT_RESOURCE_CATALOG_URL";
    private static readonly JsonSerializerOptions JsonOptions = LltJson.CreateCompactOptions();

    public async Task<OnlineResourceCatalog> GetCatalogAsync(CancellationToken token = default)
    {
        var catalogUrl = Environment.GetEnvironmentVariable(CatalogUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(catalogUrl))
            catalogUrl = AppIdentity.StableResourceCatalogUrl;

        using var httpClient = httpClientFactory.Create();
        var json = await httpClient.GetStringAsync(catalogUrl, token).ConfigureAwait(false);
        return JsonSerializer.Deserialize<OnlineResourceCatalog>(json, JsonOptions)
               ?? throw new InvalidDataException("Resource catalog is empty.");
    }

    public async Task DownloadAndVerifyAsync(string url, string expectedSha256, string destinationPath, IProgress<float>? progress = null, CancellationToken token = default)
    {
        await DownloadAsync(url, destinationPath, progress, token).ConfigureAwait(false);
        await VerifySha256Async(destinationPath, expectedSha256, token).ConfigureAwait(false);
    }

    public async Task DownloadAsync(string url, string destinationPath, IProgress<float>? progress = null, CancellationToken token = default)
    {
        try
        {
            await using (var stream = File.Create(destinationPath))
            {
                using var httpClient = httpClientFactory.Create();
                await httpClient.DownloadAsync(url, stream, progress, token).ConfigureAwait(false);
            }
        }
        catch
        {
            try { File.Delete(destinationPath); }
            catch { /* ignore cleanup failure */ }

            throw;
        }
    }

    public async Task VerifySha256Async(string path, string expectedSha256, CancellationToken token = default)
    {
        var actualSha256 = await ComputeSha256Async(path, token).ConfigureAwait(false);
        if (!actualSha256.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(path); }
            catch { /* ignore cleanup failure */ }

            throw new InvalidDataException($"Downloaded resource failed SHA256 validation. Expected {expectedSha256}, got {actualSha256}.");
        }
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, token).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
