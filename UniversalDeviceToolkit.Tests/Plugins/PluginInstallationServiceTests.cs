using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginInstallationServiceTests : TemporaryFileTestBase
{
    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldMarkImportedPluginAsInstalled()
    {
        // Arrange
        const string pluginId = "test-local-plugin";
        var pluginManager = CreatePluginManagerMock();
        var service = new PluginInstallationService(pluginManager.Object);
        var pluginsRoot = CreateTempDirectory();
        var zipPath = CreatePluginZipPackage(pluginId);

        // Act
        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        // Assert
        result.Should().BeTrue();
        pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldForceRefreshRuntimeAroundMarkingImportedPluginInstalled()
    {
        const string pluginId = "test-local-plugin";
        var sequence = new MockSequence();
        var pluginManager = new Mock<IPluginManager>();
        pluginManager.InSequence(sequence)
            .Setup(manager => manager.ScanAndLoadPluginsAsync(true))
            .Returns(Task.CompletedTask);
        pluginManager.InSequence(sequence)
            .Setup(manager => manager.InstallPlugin(pluginId));
        pluginManager.InSequence(sequence)
            .Setup(manager => manager.ScanAndLoadPluginsAsync(true))
            .Returns(Task.CompletedTask);
        var service = new PluginInstallationService(pluginManager.Object);
        var pluginsRoot = CreateTempDirectory();
        var zipPath = CreatePluginZipPackage(pluginId);

        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        result.Should().BeTrue();
        pluginManager.Verify(manager => manager.ScanAndLoadPluginsAsync(true), Times.Exactly(2));
        pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldPlaceImportedFilesUnderLocalPluginDirectory()
    {
        // Arrange
        const string pluginId = "test-local-plugin";
        var pluginManager = CreatePluginManagerMock();
        var service = new PluginInstallationService(pluginManager.Object);
        var pluginsRoot = CreateTempDirectory();
        var zipPath = CreatePluginZipPackage(pluginId);

        // Act
        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        // Assert
        result.Should().BeTrue();

        var installedPluginDirectory = Path.Combine(pluginsRoot, "local", pluginId);
        Directory.Exists(installedPluginDirectory).Should().BeTrue();
        File.Exists(Path.Combine(installedPluginDirectory, "plugin.json")).Should().BeTrue();
        Directory.GetFiles(installedPluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldNotTrustImportedPayloadForProductionSignaturePolicy()
    {
        const string pluginId = "test-local-plugin";
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());

        try
        {
            var pluginManager = CreatePluginManagerMock();
            var service = new PluginInstallationService(pluginManager.Object);
            var pluginsRoot = CreateTempDirectory();
            var zipPath = CreatePluginZipPackage(pluginId);

            var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

            result.Should().BeTrue();

            var installedPluginDirectory = Path.Combine(pluginsRoot, "local", pluginId);
            var installedDll = Directory.GetFiles(installedPluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(path => !Path.GetFileName(path).Contains(".Shared", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(path).Contains(".SDK", StringComparison.OrdinalIgnoreCase))
                .Should()
                .ContainSingle()
                .Subject;
            var signatureResult = await new PluginSignatureValidator(PluginSignatureSettings.Production)
                .ValidateAsync(installedDll);

            // Local ZIP import must not bypass RequireSignature via the trust store.
            signatureResult.Status.Should().Be(PluginSignatureStatus.NotSigned);
            signatureResult.IsAllowedByPolicy.Should().BeFalse();
            signatureResult.IsValid.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldFilterSharedAndSdkAssembliesFromImportedPluginDirectory()
    {
        const string pluginId = "test-local-plugin";
        var pluginManager = CreatePluginManagerMock();
        var service = new PluginInstallationService(pluginManager.Object);
        var pluginsRoot = CreateTempDirectory();
        var zipPath = CreatePluginZipPackage(pluginId, includeSharedRuntimeFiles: true);
        var preferredShared = "UniversalDeviceToolkit.Plugins.Shared.dll";
        var preferredSdk = "UniversalDeviceToolkit.Plugins.SDK.dll";
        var legacyShared = "LenovoLegionToolkit.Plugins.Shared.dll";
        var legacySdk = "LenovoLegionToolkit.Plugins.SDK.dll";
        var canonicalSharedAssemblyPath = Path.Combine(AppContext.BaseDirectory, preferredShared);
        var assemblySourcePath = Assembly.GetExecutingAssembly().Location;
        var createdCanonicalSharedAssembly = false;

        if (!File.Exists(canonicalSharedAssemblyPath))
        {
            File.Copy(assemblySourcePath, canonicalSharedAssemblyPath, overwrite: true);
            createdCanonicalSharedAssembly = true;
        }

        try
        {
            var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

            result.Should().BeTrue();

            var installedPluginDirectory = Path.Combine(pluginsRoot, "local", pluginId);
            // Host stages Shared under preferred UDT name (legacy package may still contain LLT-named files).
            (File.Exists(Path.Combine(installedPluginDirectory, preferredShared)) ||
             File.Exists(Path.Combine(installedPluginDirectory, legacyShared))).Should().BeTrue();
            File.Exists(Path.Combine(installedPluginDirectory, preferredSdk)).Should().BeFalse();
            File.Exists(Path.Combine(installedPluginDirectory, legacySdk)).Should().BeFalse();
        }
        finally
        {
            if (createdCanonicalSharedAssembly && File.Exists(canonicalSharedAssemblyPath))
                File.Delete(canonicalSharedAssemblyPath);
        }
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldHonorManifestIdEvenWhenJsonUsesPascalCase()
    {
        const string manifestPluginId = "shell-integration";
        var pluginManager = CreatePluginManagerMock();
        var service = new PluginInstallationService(pluginManager.Object);
        var pluginsRoot = CreateTempDirectory();
        var zipPath = CreatePluginZipPackage(
            manifestPluginId,
            manifestContent: """
                             {
                               "Id": "shell-integration",
                               "Name": "Shell Integration",
                               "Version": "1.0.11"
                             }
                             """);

        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        result.Should().BeTrue();
        Directory.Exists(Path.Combine(pluginsRoot, "local", manifestPluginId)).Should().BeTrue();
        pluginManager.Verify(manager => manager.InstallPlugin(manifestPluginId), Times.Once);
    }

    private static Mock<IPluginManager> CreatePluginManagerMock()
    {
        var pluginManager = new Mock<IPluginManager>();
        pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        return pluginManager;
    }

    private string CreatePluginZipPackage(string pluginId, bool includeSharedRuntimeFiles = false, string? manifestContent = null)
    {
        var packageDirectory = CreateTempDirectory();
        var packageRoot = Path.Combine(packageDirectory, "package");
        Directory.CreateDirectory(packageRoot);

        var assemblySourcePath = Assembly.GetExecutingAssembly().Location;
        var assemblyFileName = Path.GetFileName(assemblySourcePath);
        File.Copy(assemblySourcePath, Path.Combine(packageRoot, assemblyFileName), overwrite: true);
        File.WriteAllText(
            Path.Combine(packageRoot, "plugin.json"),
            manifestContent
            ?? $$"""
                 {
                   "id": "{{pluginId}}",
                   "name": "Test Local Plugin",
                   "version": "1.0.0"
                 }
                 """);

        if (includeSharedRuntimeFiles)
        {
            File.Copy(assemblySourcePath, Path.Combine(packageRoot, "UniversalDeviceToolkit.Plugins.Shared.dll"), overwrite: true);
            File.Copy(assemblySourcePath, Path.Combine(packageRoot, "UniversalDeviceToolkit.Plugins.SDK.dll"), overwrite: true);
            // Also include legacy names so dual-load packages still exercise filtering.
            File.Copy(assemblySourcePath, Path.Combine(packageRoot, "LenovoLegionToolkit.Plugins.Shared.dll"), overwrite: true);
            File.Copy(assemblySourcePath, Path.Combine(packageRoot, "LenovoLegionToolkit.Plugins.SDK.dll"), overwrite: true);
        }

        var zipPath = Path.Combine(packageDirectory, $"{pluginId}.zip");
        ZipFile.CreateFromDirectory(packageRoot, zipPath);
        return zipPath;
    }
}
