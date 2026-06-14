using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.PackageDownloader;
using Xunit;

namespace UniversalDeviceToolkit.Tests.PackageDownloader;

[Trait("Category", TestCategories.Unit)]
public sealed class PackageChecksumValidationTests
{
    [Fact]
    public async Task TryValidateChecksum_WithGnuStyleSidecar_ShouldAcceptValidDownload()
    {
        var packageBytes = "package-payload"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(packageBytes));
        var sidecar = $"{hash}  package.exe";

        var package = new Package
        {
            Title = "Test",
            FileName = "package.exe",
            FileLocation = "https://example.test/package.exe",
            FileCrc = string.Empty
        };

        var handler = new StaticResponseHandler(new Dictionary<string, HttpResponseMessage>(StringComparer.OrdinalIgnoreCase)
        {
            ["https://example.test/package.exe.sha256"] = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sidecar)
            }
        });

        var tempPath = Path.Combine(Path.GetTempPath(), $"udt-checksum-{Guid.NewGuid():N}.bin");
        await File.WriteAllBytesAsync(tempPath, packageBytes);

        try
        {
            var method = typeof(AbstractPackageDownloader).GetMethod(
                "TryValidateChecksum",
                BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            using var httpClient = new HttpClient(handler, disposeHandler: true);
            var action = async () => await (Task)method!.Invoke(null, [package, tempPath, httpClient, CancellationToken.None])!;

            await action.Should().NotThrowAsync();
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private sealed class StaticResponseHandler(IReadOnlyDictionary<string, HttpResponseMessage> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri?.ToString() ?? string.Empty;
            if (responses.TryGetValue(key, out var response))
                return Task.FromResult(response);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
