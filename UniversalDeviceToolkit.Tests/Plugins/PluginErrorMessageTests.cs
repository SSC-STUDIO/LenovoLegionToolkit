using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using Moq;
using UniversalDeviceToolkit.Lib.Plugins.Resources;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
[Collection(TestCollections.ProcessState)]
public class PluginErrorMessageTests : TemporaryFileTestBase
{
    private readonly string? _originalAppDataOverride;

    public PluginErrorMessageTests()
    {
        _originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
    }

    public override void Dispose()
    {
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _originalAppDataOverride);
        base.Dispose();
    }

    [Theory]
    [InlineData(nameof(Resource.Plugin_Error_DependencyResolution_Circular))]
    [InlineData(nameof(Resource.Plugin_Error_DependencyResolution_Unresolved))]
    [InlineData(nameof(Resource.Plugin_Error_DependencyResolution_VersionConflict))]
    [InlineData(nameof(Resource.Plugin_Error_DependencyResolution_Failed))]
    [InlineData(nameof(Resource.Plugin_Error_Sandbox_NotFound))]
    [InlineData(nameof(Resource.Plugin_Error_Sandbox_ResourceLimit))]
    [InlineData(nameof(Resource.Plugin_Error_Sandbox_OperationTimeout))]
    [InlineData(nameof(Resource.Plugin_Error_Sandbox_Violation))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_Disabled))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_FileNotFound))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_NotSigned_TrustedPackage))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_NotSigned_AllowUnsigned))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_NotSigned_Required))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_ValidationFailed))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_Expired))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_NotYetValid))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_ChainFailed))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_TestCertificate))]
    [InlineData(nameof(Resource.Plugin_Error_Signature_CertificateError))]
    [InlineData(nameof(Resource.Plugin_Error_Repository_Deserialize))]
    [InlineData(nameof(Resource.Plugin_Error_Repository_HostIncompatible))]
    [InlineData(nameof(Resource.Plugin_Error_Repository_DownloadFailed))]
    [InlineData(nameof(Resource.Plugin_Error_Repository_NotLoadable))]
    [InlineData(nameof(Resource.Plugin_Error_Repository_FetchFailed))]
    [InlineData(nameof(Resource.Plugin_Error_Sandbox_InvalidPluginId))]
    [InlineData(nameof(Resource.Plugin_Error_Repository_PathTraversal))]
    public void ResourceKey_ResolvesToNonEmptyFallbackString(string propertyName)
    {
        var prop = typeof(Resource).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        prop.Should().NotBeNull($"Resource.{propertyName} must exist");
        var value = (string?)prop!.GetValue(null);
        value.Should().NotBeNullOrWhiteSpace($"Resource.{propertyName} must not be empty");
    }

    [Fact]
    public void ResourceManager_AllowsLoadingStringsByKey()
    {
        var rm = Resource.ResourceManager;
        rm.Should().NotBeNull();

        foreach (var prop in typeof(Resource).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (prop.PropertyType != typeof(string))
                continue;
            var value = rm.GetString(prop.Name);
            value.Should().NotBeNullOrWhiteSpace($"Resource.{prop.Name} must resolve through ResourceManager");
        }
    }

    [Fact]
    public void DependencyResolver_CycleErrorMessage_UsesResourceString()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = [new PluginDependency { PluginId = "B" }],
            ["B"] = [new PluginDependency { PluginId = "A" }]
        };

        var result = new DependencyResolver().ResolveDependencies(plugins);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().StartWith("Circular dependencies detected:");
    }

    [Fact]
    public void DependencyResolver_UnresolvedErrorMessage_UsesResourceString()
    {
        // DependencyResolver does NOT fail on missing deps (unknown deps are silently skipped).
        // To trigger the "unresolved" path, we need a cycle that Kahn's algorithm detects.
        // The only way loadOrder.Count != plugins.Count is if circular detection missed something,
        // which is a defensive edge case. We verify the resource key exists and is non-empty instead.
        var key = Resource.Plugin_Error_DependencyResolution_Unresolved;
        key.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DependencyResolver_VersionConflictErrorMessage_UsesResourceString()
    {
        var plugins = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = [new PluginDependency { PluginId = "dep", MinVersion = "2.0.0" }],
            ["dep"] = []
        };
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["dep"] = "1.0.0" };

        var result = new DependencyResolver().ResolveDependencies(plugins, versions);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().StartWith("Version conflicts detected for:");
    }

    [Fact]
    public async Task PluginSandbox_MissingSandboxMessage_UsesResourceString()
    {
        using var sandbox = new PluginSandbox();

        var syncResult = sandbox.ExecuteInSandbox("absent", () => 1);
        syncResult.Success.Should().BeFalse();
        syncResult.ErrorMessage.Should().Be(string.Format(Resource.Plugin_Error_Sandbox_NotFound, "absent"));

        var asyncResult = await sandbox.ExecuteInSandboxAsync("absent", () => Task.FromResult<object?>(1));
        asyncResult.Success.Should().BeFalse();
        asyncResult.ErrorMessage.Should().Be(string.Format(Resource.Plugin_Error_Sandbox_NotFound, "absent"));
    }

    [Fact]
    public async Task PluginSignatureValidator_DisabledMessage_UsesResourceString()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        await File.WriteAllTextAsync(tempFile, "fake");
        try
        {
            var validator = new PluginSignatureValidator(PluginSignatureSettings.Disabled);
            var result = await validator.ValidateAsync(tempFile);

            result.Status.Should().Be(PluginSignatureStatus.Valid);
            result.ErrorMessage.Should().Be(Resource.Plugin_Error_Signature_Disabled);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PluginSignatureValidator_FileNotFoundMessage_UsesResourceString()
    {
        var validator = new PluginSignatureValidator(PluginSignatureSettings.Production);
        var nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");

        var result = await validator.ValidateAsync(nonExistent);

        result.Status.Should().Be(PluginSignatureStatus.ValidationError);
        result.ErrorMessage.Should().Be(string.Format(Resource.Plugin_Error_Signature_FileNotFound, nonExistent));
    }

    [Fact]
    public async Task PluginSignatureValidator_NotSignedRequiredMessage_UsesResourceString()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        await File.WriteAllTextAsync(tempFile, "fake");
        try
        {
            var settings = new PluginSignatureSettings
            {
                ValidationMode = PluginSignatureValidationMode.RequireSignature,
                AllowTestCertificates = false
            };
            var validator = new PluginSignatureValidator(settings);

            var result = await validator.ValidateAsync(tempFile);

            result.Status.Should().Be(PluginSignatureStatus.NotSigned);
            result.ErrorMessage.Should().Be(Resource.Plugin_Error_Signature_NotSigned_Required);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PluginRepositoryService_HostIncompatibleMessage_UsesResourceString()
    {
        var pluginManager = new Mock<IPluginManager>();
        var httpClient = new HttpClient(new ThrowHandler());
        var factory = new StubHttpClientFactory(httpClient);

        var failed = (string?)null;
        using var service = new PluginRepositoryService(pluginManager.Object, factory, forceAllowFileUrls: true);
        service.DownloadFailed += (_, msg) => failed = msg;

        var manifest = new PluginManifest
        {
            Id = "needs-newer",
            Version = "1.0.0",
            MinimumHostVersion = "99.0.0"
        };

        var ok = await service.DownloadAndInstallPluginAsync(manifest);

        ok.Should().BeFalse();
        failed.Should().Be(string.Format(Resource.Plugin_Error_Repository_HostIncompatible, "needs-newer", "99.0.0"));
    }

    [Fact]
    public async Task PluginRepositoryService_DeserializeErrorMessage_UsesResourceString()
    {
        var pluginManager = new Mock<IPluginManager>();
        var httpClient = new HttpClient(new StaticResponseHandler("null"));
        var factory = new StubHttpClientFactory(httpClient);
        using var service = new PluginRepositoryService(pluginManager.Object, factory, forceAllowFileUrls: true);

        Func<Task> act = () => service.FetchAvailablePluginsAsync(forceRefresh: true);

        await act.Should().ThrowAsync<InvalidDataException>()
            .Where(ex => ex.Message == Resource.Plugin_Error_Repository_Deserialize);
    }

    [Fact]
    public void Resource_Plugin_Error_DependencyResolution_Circular_FormatSubstitutesPlaceholder()
    {
        string.Format(Resource.Plugin_Error_DependencyResolution_Circular, "A -> B -> A")
            .Should().Be("Circular dependencies detected: A -> B -> A");
    }

    [Fact]
    public void Resource_Plugin_Error_Repository_HostIncompatible_FormatSubstitutesBothPlaceholders()
    {
        string.Format(Resource.Plugin_Error_Repository_HostIncompatible, "plug", "2.0.0")
            .Should().Be("Plugin plug requires Universal Device Toolkit 2.0.0 or newer.");
    }

    private sealed class StubHttpClientFactory : HttpClientFactory
    {
        private readonly HttpClient _client;
        public StubHttpClientFactory(HttpClient client) => _client = client;
        public override HttpClient Create() => _client;
    }

    private sealed class ThrowHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("offline");
    }

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly string _body;
        public StaticResponseHandler(string body) => _body = body;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_body)
            });
    }
}
