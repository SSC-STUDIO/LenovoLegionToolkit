using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Tiny disk-cached image loader for game covers (Steam CDN header art).
/// Never throws: failures return null so callers can fall back to icon tiles.
/// </summary>
public static class NetworkImageCache
{
    private static readonly HttpClient SharedClient = CreateClient();

    private static string CacheRoot => Path.Combine(Folders.AppData, "image-cache");

    public static string SteamHeaderUrl(int appId) =>
        $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg";

    public static async Task<BitmapImage?> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            var cachePath = Path.Combine(CacheRoot, Sha1(url) + ".img");

            byte[] bytes;
            if (File.Exists(cachePath))
            {
                bytes = await File.ReadAllBytesAsync(cachePath, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                bytes = await SharedClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
                if (bytes.Length < 256)
                    return null;

                try
                {
                    Directory.CreateDirectory(CacheRoot);
                    await File.WriteAllBytesAsync(cachePath, bytes, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Cache write is best-effort; the decoded image still works.
                }
            }

            using var stream = new MemoryStream(bytes, writable: false);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("UniversalDeviceToolkit/5.0");
        return client;
    }

    private static string Sha1(string value)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
