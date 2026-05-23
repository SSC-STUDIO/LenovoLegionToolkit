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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests.WPF;

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
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

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
