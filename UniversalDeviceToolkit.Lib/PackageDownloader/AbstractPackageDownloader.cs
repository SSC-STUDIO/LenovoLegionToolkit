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

        await using (var fileStream = File.OpenWrite(tempPath))
            await httpClient.DownloadAsync(package.FileLocation, fileStream, progress, token).ConfigureAwait(false);

        await TryValidateChecksum(package, tempPath, httpClient, token).ConfigureAwait(false);

        var filename = SanitizeFileName(package.Title) + " - " + SanitizeFileName(Path.GetFileName(package.FileName));
        var finalPath = Path.Combine(location, filename);

        File.Move(tempPath, finalPath, true);

        return finalPath;
    }

    private static async Task TryValidateChecksum(Package package, string tempPath, HttpClient httpClient, CancellationToken token)
    {
        await using var fileStream = File.OpenRead(tempPath);
        using var managedSha256 = SHA256.Create();

        var fileSha256Bytes = await managedSha256.ComputeHashAsync(fileStream, token).ConfigureAwait(false);
        var fileSha256 = fileSha256Bytes.Aggregate(string.Empty, (current, b) => current + b.ToString("X2"));

        if (!string.IsNullOrEmpty(package.FileCrc) && fileSha256.Equals(package.FileCrc, StringComparison.InvariantCultureIgnoreCase))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Package file checksum match. [fileName={package.FileName}, fileLocation={package.FileLocation}, fileCrc={package.FileCrc}]");
            return;
        }

        try
        {
            var externalSha256Content = await httpClient.GetStringAsync($"{package.FileLocation}.sha256", token).ConfigureAwait(false);
            var externalSha256 = TryExtractExpectedSha256(externalSha256Content, GetPackageFileNameCandidates(package));
            if (fileSha256.Equals(externalSha256, StringComparison.InvariantCultureIgnoreCase))
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

        throw new InvalidDataException("File checksum mismatch");
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
        return Regex.Replace(name, invalidRegStr, "_");
    }

    internal static string? TryExtractExpectedSha256(string? hashContent, IReadOnlyCollection<string> packageFileNames)
    {
        if (string.IsNullOrWhiteSpace(hashContent))
            return null;

        var lines = hashContent
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (packageFileNames.Count != 0)
        {
            foreach (var line in lines)
            {
                if (!packageFileNames.Any(fileName => LineReferencesFileName(line, fileName)))
                    continue;

                var lineHash = TryExtractFirstSha256Hash(line);
                if (lineHash is not null)
                    return lineHash;
            }
        }

        foreach (var line in lines)
        {
            var lineHash = TryExtractFirstSha256Hash(line);
            if (lineHash is not null &&
                (line.Contains("sha256", StringComparison.OrdinalIgnoreCase) || lines.Length == 1))
                return lineHash;
        }

        var allHashes = ExtractAllSha256Hashes(hashContent);
        return allHashes.Count == 1 ? allHashes[0] : null;
    }

    private static string? TryExtractFirstSha256Hash(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = Regex.Match(text, @"(?<![a-fA-F0-9])([a-fA-F0-9]{64})(?![a-fA-F0-9])", RegexOptions.IgnoreCase);
        return match.Success
            ? match.Groups[1].Value.ToLowerInvariant()
            : null;
    }

    private static List<string> ExtractAllSha256Hashes(string text)
    {
        var hashes = new List<string>();

        foreach (Match match in Regex.Matches(text, @"(?<![a-fA-F0-9])([a-fA-F0-9]{64})(?![a-fA-F0-9])", RegexOptions.IgnoreCase))
        {
            var hash = match.Groups[1].Value.ToLowerInvariant();
            if (!hashes.Contains(hash, StringComparer.OrdinalIgnoreCase))
                hashes.Add(hash);
        }

        return hashes;
    }

    private static string[] GetPackageFileNameCandidates(Package package)
    {
        var candidates = new List<string>();

        AddFileNameCandidate(candidates, package.FileName);

        if (Uri.TryCreate(package.FileLocation, UriKind.Absolute, out var uri))
            AddFileNameCandidate(candidates, Uri.UnescapeDataString(uri.AbsolutePath));
        else
            AddFileNameCandidate(candidates, package.FileLocation);

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddFileNameCandidate(List<string> candidates, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var fileName = Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(fileName))
            candidates.Add(fileName);
    }

    private static bool LineReferencesFileName(string line, string fileName)
    {
        var index = 0;
        while ((index = line.IndexOf(fileName, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var beforeOk = index == 0 || !IsFileNamePartChar(line[index - 1]);
            var afterIndex = index + fileName.Length;
            var afterOk = afterIndex >= line.Length || !IsFileNamePartChar(line[afterIndex]);
            if (beforeOk && afterOk)
                return true;

            index += fileName.Length;
        }

        return false;
    }

    private static bool IsFileNamePartChar(char value) =>
        char.IsLetterOrDigit(value) || value is '.' or '-' or '_';
}
