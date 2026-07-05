using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Resources;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.PackageDownloader;

public abstract class AbstractPackageDownloader(HttpClientFactory httpClientFactory) : IPackageDownloader
{
    protected HttpClientFactory HttpClientFactory => httpClientFactory;

    public abstract Task<List<Package>> GetPackagesAsync(string machineType, OS os, IProgress<float>? progress = null, CancellationToken token = default);

    public async Task<string> DownloadPackageFileAsync(Package package, string location, IProgress<float>? progress = null, CancellationToken token = default)
    {
        using var httpClient = httpClientFactory.Create();

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        using (var fileStream = File.OpenWrite(tempPath))
            await httpClient.DownloadAsync(package.FileLocation, fileStream, progress, token).ConfigureAwait(false);

        await TryValidateChecksum(package, tempPath, httpClient, token).ConfigureAwait(false);

        var filename = SanitizeFileName(package.Title) + " - " + SanitizeFileName(Path.GetFileName(package.FileName));
        var finalPath = Path.Combine(location, filename);

        File.Move(tempPath, finalPath, true);

        return finalPath;
    }

    private static async Task TryValidateChecksum(Package package, string tempPath, HttpClient httpClient, CancellationToken token)
    {
        using var fileStream = File.OpenRead(tempPath);
        using var managedSha256 = SHA256.Create();

        var fileSha256Bytes = await managedSha256.ComputeHashAsync(fileStream, token).ConfigureAwait(false);
        var fileSha256 = fileSha256Bytes.Aggregate(string.Empty, (current, b) => current + b.ToString("X2"));

        if (!string.IsNullOrEmpty(package.FileCrc) && fileSha256.Equals(package.FileCrc, StringComparison.OrdinalIgnoreCase))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Package file checksum match. [fileName={package.FileName}, fileLocation={package.FileLocation}, fileCrc={package.FileCrc}]");
            return;
        }

        try
        {
            var externalSha256Content = await httpClient.GetStringAsync($"{package.FileLocation}.sha256", token).ConfigureAwait(false);
            var externalSha256 = TryExtractFirstSha256Hash(externalSha256Content);
            if (externalSha256 is not null && fileSha256.Equals(externalSha256, StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"External file checksum match. [fileName={package.FileName}, fileLocation={package.FileLocation}, fileCrc={package.FileCrc}]");
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"External file checksum not found. [statusCode={ex.StatusCode}, fileName={package.FileName}, fileLocation={package.FileLocation}, fileCrc={package.FileCrc}]");
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"File checksum mismatch. [fileName={package.FileName}, fileLocation={package.FileLocation}]");

        throw ExceptionHelper.FileChecksumMismatch();
    }

    private static string? TryExtractFirstSha256Hash(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = Regex.Match(text, @"(?<![a-fA-F0-9])([a-fA-F0-9]{64})(?![a-fA-F0-9])", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
        return Regex.Replace(name, invalidRegStr, "_");
    }
}
