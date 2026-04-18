using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using Octokit;
using Xunit;

namespace LenovoLegionToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public class UpdateCheckerTests : TemporaryFileTestBase
{
    private static readonly MethodInfo ValidateUpdatePackageAsyncMethod = typeof(UpdateChecker)
        .GetMethod("ValidateUpdatePackageAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public void Update_ShouldRecognizeCurrentSha256TextAsset()
    {
        // Arrange
        const string packageUrl = "https://example.com/LenovoLegionToolkit_v2.14.0_Setup.exe";
        const string sha256Url = "https://example.com/LenovoLegionToolkit_v2.14.0_SHA256.txt";
        var release = CreateRelease(
            body: string.Empty,
            ("LenovoLegionToolkit_v2.14.0_Setup.exe", packageUrl),
            ("LenovoLegionToolkit_v2.14.0_SHA256.txt", sha256Url));

        // Act
        var update = new Update(release);

        // Assert
        update.Url.Should().Be(packageUrl);
        update.Sha256Url.Should().Be(sha256Url);
        update.Sha256Hash.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldParsePackageSpecificHashFromReleaseBody()
    {
        // Arrange
        const string packageUrl = "https://example.com/LenovoLegionToolkit_v2.14.0_Setup.exe";
        var zipHash = new string('1', 64);
        var setupHash = new string('a', 64);
        var release = CreateRelease(
            body: $"""
                   ## Verification
                   LenovoLegionToolkit_v2.14.0_win-x64.zip: {zipHash}
                   LenovoLegionToolkit_v2.14.0_Setup.exe: {setupHash}
                   """,
            ("LenovoLegionToolkit_v2.14.0_Setup.exe", packageUrl));

        // Act
        var update = new Update(release);

        // Assert
        update.Sha256Hash.Should().Be(setupHash);
    }

    [Fact]
    public async Task ValidateUpdatePackageAsync_WhenSha256TxtContainsMultipleEntries_ShouldUsePackageSpecificHash()
    {
        // Arrange
        const string packageUrl = "https://example.com/LenovoLegionToolkit_v2.14.0_Setup.exe";
        const string sha256Url = "https://example.com/LenovoLegionToolkit_v2.14.0_SHA256.txt";
        var packageBytes = "signed update payload"u8.ToArray();
        var expectedHash = ComputeSha256(packageBytes);
        var tempFile = CreateTempFile();
        await File.WriteAllBytesAsync(tempFile, packageBytes);

        var update = new Update(CreateRelease(
            body: string.Empty,
            ("LenovoLegionToolkit_v2.14.0_Setup.exe", packageUrl),
            ("LenovoLegionToolkit_v2.14.0_SHA256.txt", sha256Url)));

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            request.RequestUri.Should().NotBeNull();
            request.RequestUri!.ToString().Should().Be(sha256Url);

            var sha256List = $"""
                              {new string('b', 64)} LenovoLegionToolkit_v2.14.0_win-x64.zip
                              SHA256 (LenovoLegionToolkit_v2.14.0_Setup.exe) = {expectedHash}
                              """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sha256List)
            };
        }));

        // Act
        await InvokeValidateUpdatePackageAsync(tempFile, update, httpClient);

        // Assert
        File.Exists(tempFile).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateUpdatePackageAsync_WhenLegacySha256AssetContainsRawHash_ShouldAcceptPackage()
    {
        // Arrange
        const string packageUrl = "https://example.com/LenovoLegionToolkitSetup.exe";
        const string sha256Url = "https://example.com/LenovoLegionToolkitSetup.exe.sha256";
        var packageBytes = "legacy update payload"u8.ToArray();
        var expectedHash = ComputeSha256(packageBytes);
        var tempFile = CreateTempFile();
        await File.WriteAllBytesAsync(tempFile, packageBytes);

        var update = new Update(CreateRelease(
            body: string.Empty,
            ("LenovoLegionToolkitSetup.exe", packageUrl),
            ("LenovoLegionToolkitSetup.exe.sha256", sha256Url)));

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedHash)
        }));

        // Act
        await InvokeValidateUpdatePackageAsync(tempFile, update, httpClient);

        // Assert
        File.Exists(tempFile).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateUpdatePackageAsync_WhenBodyHashDoesNotMatch_ShouldDeleteFileAndThrow()
    {
        // Arrange
        const string packageUrl = "https://example.com/LenovoLegionToolkitSetup.exe";
        var tempFile = CreateTempFile();
        await File.WriteAllBytesAsync(tempFile, "tampered payload"u8.ToArray());

        var update = new Update(CreateRelease(
            body: $"SHA256: {new string('c', 64)}",
            ("LenovoLegionToolkitSetup.exe", packageUrl)));

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("unexpected request")));

        // Act
        var action = () => InvokeValidateUpdatePackageAsync(tempFile, update, httpClient);

        // Assert
        await action.Should().ThrowAsync<InvalidDataException>();
        File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public async Task ValidateUpdatePackageAsync_WhenNoHashIsAvailable_ShouldSkipValidation()
    {
        // Arrange
        const string packageUrl = "https://example.com/LenovoLegionToolkitSetup.exe";
        var tempFile = CreateTempFile();
        await File.WriteAllBytesAsync(tempFile, "unsigned payload"u8.ToArray());

        var update = new Update(CreateRelease(
            body: string.Empty,
            ("LenovoLegionToolkitSetup.exe", packageUrl)));

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("unexpected request")));

        // Act
        await InvokeValidateUpdatePackageAsync(tempFile, update, httpClient);

        // Assert
        File.Exists(tempFile).Should().BeTrue();
    }

    private static async Task InvokeValidateUpdatePackageAsync(string filePath, Update update, HttpClient httpClient)
    {
        var task = (Task)ValidateUpdatePackageAsyncMethod.Invoke(null, [filePath, update, httpClient, CancellationToken.None])!;
        await task;
    }

    private static string ComputeSha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static Release CreateRelease(string body, params (string Name, string Url)[] assets)
    {
        var release = CreateUninitialized<Release>();
        SetAutoProperty(release, nameof(Release.TagName), "v2.14.0");
        SetAutoProperty(release, nameof(Release.Name), "Lenovo Legion Toolkit v2.14.0");
        SetAutoProperty(release, nameof(Release.Body), body);
        SetAutoProperty(release, nameof(Release.Prerelease), false);
        SetAutoProperty(release, nameof(Release.Draft), false);
        SetAutoProperty(release, nameof(Release.CreatedAt), DateTimeOffset.UtcNow);
        SetAutoProperty(release, nameof(Release.PublishedAt), DateTimeOffset.UtcNow);
        SetAutoProperty(release, nameof(Release.Assets), Array.ConvertAll(assets, asset => CreateReleaseAsset(asset.Name, asset.Url)));
        return release;
    }

    private static ReleaseAsset CreateReleaseAsset(string name, string url)
    {
        var asset = CreateUninitialized<ReleaseAsset>();
        SetAutoProperty(asset, nameof(ReleaseAsset.Name), name);
        SetAutoProperty(asset, nameof(ReleaseAsset.BrowserDownloadUrl), url);
        return asset;
    }

    private static T CreateUninitialized<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static void SetAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull($"{target.GetType().Name}.{propertyName} should exist");
        field!.SetValue(target, value);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
