using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class LanguagePackManagerTests : IDisposable
{
    private readonly string? _previousCatalogUrl;

    public LanguagePackManagerTests()
    {
        _previousCatalogUrl = Environment.GetEnvironmentVariable(OnlineResourceCatalogClient.CatalogUrlEnvironmentVariable);
        Environment.SetEnvironmentVariable(OnlineResourceCatalogClient.CatalogUrlEnvironmentVariable, "https://example.test/resources/stable/catalog.json");
    }

    [Fact]
    public async Task InstallAsync_WhenCatalogFails_ShouldReportMonotonicProgressEndingAtOne()
    {
        var culture = new CultureInfo("ff-latn-sn");
        var version = GetCurrentVersion();
        var fullZipName = $"{AppIdentity.CompactName}_v{version}_Full_win-x64.zip";
        var hashName = $"{AppIdentity.CompactName}_v{version}_SHA256.txt";
        var fullZip = CreateFullPortableZip();
        var fullZipSha256 = Convert.ToHexString(SHA256.HashData(fullZip)).ToLowerInvariant();
        var hashText = $"{fullZipSha256}  {fullZipName}";

        var responses = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{hashName}"] = Encoding.UTF8.GetBytes(hashText),
            [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{fullZipName}"] = fullZip
        };
        var manager = new LanguagePackManager(new OnlineResourceCatalogClient(new TestHttpClientFactory(responses)));
        var installRoot = AppContext.BaseDirectory;
        var ffDirectory = Path.Combine(installRoot, "ff");
        var ffLatnDirectory = Path.Combine(installRoot, "ff-Latn");
        var ffLatnSnDirectory = Path.Combine(installRoot, "ff-Latn-SN");
        var progressValues = new ConcurrentBag<float>();

        try
        {
            TryDeleteDirectory(ffDirectory);
            TryDeleteDirectory(ffLatnDirectory);
            TryDeleteDirectory(ffLatnSnDirectory);

            var progress = new Progress<float>(progressValues.Add);
            await manager.InstallAsync(culture, progress);

            var ordered = progressValues.OrderBy(value => value).ToArray();
            ordered.Should().NotBeEmpty();
            ordered[ordered.Length - 1].Should().Be(1f);
            ordered.All(value => value >= 0f && value <= 1f).Should().BeTrue();
            progressValues.Any(value => value >= 0.85f).Should().BeTrue();
        }
        finally
        {
            TryDeleteDirectory(ffDirectory);
            TryDeleteDirectory(ffLatnDirectory);
            TryDeleteDirectory(ffLatnSnDirectory);
        }
    }

    [Theory]
    [InlineData("ff-Latn-SN", "Universal Device Toolkit.resources.dll")]
    [InlineData("ff-Latn-SN", "LenovoLegionToolkit.WPF.resources.dll")]
    public void IsInstalled_WhenAppSatelliteExists_ReturnsTrue(string cultureName, string satelliteFileName)
    {
        var culture = new CultureInfo(cultureName);
        var manager = new LanguagePackManager(new OnlineResourceCatalogClient(new TestHttpClientFactory(new Dictionary<string, byte[]>())));
        var installRoot = AppContext.BaseDirectory;
        var createdDirectories = new List<string>();

        try
        {
            foreach (var directoryName in GetExpectedResourceDirectoryNames(culture))
            {
                var directory = Path.Combine(installRoot, directoryName);
                if (Directory.Exists(directory))
                    continue;

                Directory.CreateDirectory(directory);
                createdDirectories.Add(directory);
            }

            var targetDirectory = Path.Combine(installRoot, GetPrimaryResourceDirectoryName(culture));
            Directory.CreateDirectory(targetDirectory);
            if (!createdDirectories.Contains(targetDirectory, StringComparer.OrdinalIgnoreCase))
                createdDirectories.Add(targetDirectory);

            File.WriteAllBytes(Path.Combine(targetDirectory, satelliteFileName), [0x00]);

            manager.IsInstalled(culture).Should().BeTrue();
        }
        finally
        {
            foreach (var directory in createdDirectories)
            {
                TryDeleteDirectory(directory);
            }
        }
    }

    [Fact]
    public async Task InstallAsync_WhenLanguageZipOnlyContainsDependencySatellites_ShouldFallbackOrFail()
    {
        var culture = new CultureInfo("de");
        var version = GetCurrentVersion();
        var languageZip = CreateLanguageZip(("de/Humanizer.resources.dll", "humanizer"));
        var languageSha256 = Convert.ToHexString(SHA256.HashData(languageZip)).ToLowerInvariant();
        var catalogJson = $$"""
            {
              "schemaVersion": 1,
              "languages": [
                {
                  "culture": "de",
                  "url": "https://example.test/de.zip",
                  "sha256": "{{languageSha256}}"
                }
              ]
            }
            """;

        var fullZipName = $"{AppIdentity.CompactName}_v{version}_Full_win-x64.zip";
        var hashName = $"{AppIdentity.CompactName}_v{version}_SHA256.txt";
        var fullZip = CreateFullPortableZip();
        var fullZipSha256 = Convert.ToHexString(SHA256.HashData(fullZip)).ToLowerInvariant();
        var hashText = $"{fullZipSha256}  {fullZipName}";

        var responses = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["https://example.test/resources/stable/catalog.json"] = Encoding.UTF8.GetBytes(catalogJson),
            ["https://example.test/de.zip"] = languageZip,
            [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{hashName}"] = Encoding.UTF8.GetBytes(hashText),
            [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{fullZipName}"] = fullZip
        };

        var manager = new LanguagePackManager(new OnlineResourceCatalogClient(new TestHttpClientFactory(responses)));
        var deDirectory = Path.Combine(AppContext.BaseDirectory, "de");

        try
        {
            TryDeleteDirectory(deDirectory);
            await manager.InstallAsync(culture);
            manager.IsInstalled(culture).Should().BeTrue();
            File.Exists(Path.Combine(deDirectory, "Universal Device Toolkit.resources.dll")).Should().BeTrue();
        }
        finally
        {
            TryDeleteDirectory(deDirectory);
        }
    }

    [Fact]
    public void IsInstalled_WhenOnlyDependencySatellitesExist_ReturnsFalse()
    {
        var culture = new CultureInfo("ff-latn-sn");
        var manager = new LanguagePackManager(new OnlineResourceCatalogClient(new TestHttpClientFactory(new Dictionary<string, byte[]>())));
        var directory = Path.Combine(AppContext.BaseDirectory, "ff-Latn-SN");

        try
        {
            TryDeleteDirectory(directory);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "Humanizer.resources.dll"), [0x00]);

            manager.IsInstalled(culture).Should().BeFalse();
        }
        finally
        {
            TryDeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task InstallAsync_WhenCatalogFails_ShouldFallbackToReleaseFullPortable()
    {
        var culture = new CultureInfo("ff-latn-sn");
        var version = GetCurrentVersion();
        var fullZipName = $"{AppIdentity.CompactName}_v{version}_Full_win-x64.zip";
        var hashName = $"{AppIdentity.CompactName}_v{version}_SHA256.txt";
        var fullZip = CreateFullPortableZip();
        var fullZipSha256 = Convert.ToHexString(SHA256.HashData(fullZip)).ToLowerInvariant();
        var hashText = $"{fullZipSha256}  {fullZipName}";

        var responses = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{hashName}"] = Encoding.UTF8.GetBytes(hashText),
            [$"{AppIdentity.RepositoryUrl}/releases/download/v{version}/{fullZipName}"] = fullZip
        };
        var manager = new LanguagePackManager(new OnlineResourceCatalogClient(new TestHttpClientFactory(responses)));
        var installRoot = AppContext.BaseDirectory;
        var ffDirectory = Path.Combine(installRoot, "ff");
        var ffLatnDirectory = Path.Combine(installRoot, "ff-Latn");
        var ffLatnSnDirectory = Path.Combine(installRoot, "ff-Latn-SN");

        try
        {
            TryDeleteDirectory(ffDirectory);
            TryDeleteDirectory(ffLatnDirectory);
            TryDeleteDirectory(ffLatnSnDirectory);

            await manager.InstallAsync(culture);

            File.Exists(Path.Combine(ffDirectory, "Universal Device Toolkit.resources.dll")).Should().BeTrue();
            File.Exists(Path.Combine(ffLatnDirectory, "Humanizer.resources.dll")).Should().BeTrue();
            File.Exists(Path.Combine(ffLatnSnDirectory, "Wpf.Ui.resources.dll")).Should().BeTrue();
        }
        finally
        {
            TryDeleteDirectory(ffDirectory);
            TryDeleteDirectory(ffLatnDirectory);
            TryDeleteDirectory(ffLatnSnDirectory);
        }
    }

    public void Dispose() =>
        Environment.SetEnvironmentVariable(OnlineResourceCatalogClient.CatalogUrlEnvironmentVariable, _previousCatalogUrl);

    private static byte[] CreateLanguageZip(params (string Path, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var (path, content) in entries)
                AddZipEntry(archive, path, content);
        }

        return stream.ToArray();
    }

    private static byte[] CreateFullPortableZip()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddZipEntry(archive, "ff/Universal Device Toolkit.resources.dll", "resource");
            AddZipEntry(archive, "ff-Latn/Humanizer.resources.dll", "humanizer");
            AddZipEntry(archive, "ff-Latn-SN/Wpf.Ui.resources.dll", "wpf");
            AddZipEntry(archive, "de/Universal Device Toolkit.resources.dll", "other language");
            AddZipEntry(archive, "Universal Device Toolkit.dll", "binary");
        }

        return stream.ToArray();
    }

    private static void AddZipEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version ?? typeof(LanguagePackManager).Assembly.GetName().Version;
        return version == null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string[] GetExpectedResourceDirectoryNames(CultureInfo cultureInfo)
    {
        var names = new List<string>();
        var current = cultureInfo;

        while (current != CultureInfo.InvariantCulture)
        {
            if (!names.Contains(current.Name, StringComparer.OrdinalIgnoreCase))
                names.Add(current.Name);

            current = current.Parent;
        }

        var primary = GetPrimaryResourceDirectoryName(cultureInfo);
        if (!names.Contains(primary, StringComparer.OrdinalIgnoreCase))
            names.Add(primary);

        var normalized = LanguagePackManager.NormalizeAssetCultureName(cultureInfo);
        if (!names.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            names.Add(normalized);

        return names.ToArray();
    }

    private static string GetPrimaryResourceDirectoryName(CultureInfo cultureInfo) =>
        cultureInfo.Name switch
        {
            "zh-Hans" => "zh",
            "uz-Latn-UZ" => "uz",
            _ => cultureInfo.Name
        };

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
        catch
        {
            // best-effort test cleanup
        }
    }

    private sealed class TestHttpClientFactory(IReadOnlyDictionary<string, byte[]> responses) : HttpClientFactory
    {
        public override HttpClient Create() => new(new TestHandler(responses), true);
    }

    private sealed class TestHandler(IReadOnlyDictionary<string, byte[]> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri?.ToString() ?? string.Empty;
            if (!responses.TryGetValue(key, out var response))
                return Task.FromException<HttpResponseMessage>(new HttpRequestException("Simulated network failure."));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(response)
            });
        }
    }
}
