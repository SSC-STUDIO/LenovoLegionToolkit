using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Tests.PluginFixture;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
[Collection(TestCollections.ProcessState)]
public class PluginInstallationServiceTests : TemporaryFileTestBase
{
    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldMarkImportedPluginAsInstalled()
    {
        // Arrange
        const string pluginId = "test-local-plugin";
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);
        var zipPath = CreatePluginZipPackage(pluginId);

        // Act
        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        // Assert
        result.Should().BeTrue();
        pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldForceRefreshRuntimeBeforeCommittingInstallationState()
    {
        const string pluginId = "test-local-plugin";
        var sequence = new MockSequence();
        var pluginManager = new Mock<IPluginManager>();
        pluginManager.InSequence(sequence)
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(Task.CompletedTask);
        pluginManager.InSequence(sequence)
            .Setup(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()))
            .Returns(new PluginInstallationStateSnapshot(pluginId, false, false));
        var service = new PluginInstallationService(pluginManager.Object);
        var pluginsRoot = CreateTempDirectory();
        ConfigureActivatedPlugin(pluginManager, pluginsRoot);
        var zipPath = CreatePluginZipPackage(pluginId);

        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        result.Should().BeTrue();
        pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()),
            Times.Once);
        pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldPlaceImportedFilesUnderLocalPluginDirectory()
    {
        // Arrange
        const string pluginId = "test-local-plugin";
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);
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
            var pluginsRoot = CreateTempDirectory();
            var pluginManager = CreatePluginManagerMock(pluginsRoot);
            var service = new PluginInstallationService(pluginManager.Object);
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
    public async Task ExtractAndInstallPluginAsync_ShouldNotTrustRestoredUnsignedPluginAfterFailedReplacement()
    {
        const string pluginId = "test-local-plugin";
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());

        try
        {
            var pluginsRoot = CreateTempDirectory();
            var existingPluginDirectory = Path.Combine(pluginsRoot, "local", pluginId);
            Directory.CreateDirectory(existingPluginDirectory);

            var assemblySourcePath = Assembly.GetExecutingAssembly().Location;
            var existingDllPath = Path.Combine(
                existingPluginDirectory,
                $"UniversalDeviceToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll");
            var sentinelPath = Path.Combine(existingPluginDirectory, "original.txt");
            File.Copy(assemblySourcePath, existingDllPath);
            File.WriteAllText(
                Path.Combine(existingPluginDirectory, "plugin.json"),
                """{"id":"test-local-plugin","name":"Existing Local Plugin","version":"1.0.0"}""");
            File.WriteAllText(sentinelPath, "original payload");

            var pluginManager = new Mock<IPluginManager>();
            ConfigureActivatedPlugin(pluginManager, pluginsRoot);
            pluginManager
                .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                    pluginId,
                    It.IsAny<string>(),
                    It.IsAny<IDisposable?>()))
                .Returns(Task.FromException(new InvalidOperationException("runtime validation failed")));

            var service = new PluginInstallationService(pluginManager.Object);
            var zipPath = CreatePluginZipPackage(pluginId);
            TrustedPluginPackageStore.IsTrustedFile(existingDllPath).Should().BeFalse();

            Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*runtime validation failed*");
            File.ReadAllText(sentinelPath).Should().Be("original payload");
            TrustedPluginPackageStore.IsTrustedFile(existingDllPath).Should().BeFalse();
            pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Never);
            pluginManager.Verify(
                manager => manager.RestorePluginInstallationState(It.IsAny<PluginInstallationStateSnapshot>()),
                Times.Never);
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldLeaveExistingPluginUntouchedWhenBackupMoveFails()
    {
        const string pluginId = "test-local-plugin";
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());

        try
        {
            var pluginsRoot = CreateTempDirectory();
            var existingPluginDirectory = Path.Combine(pluginsRoot, "local", pluginId);
            Directory.CreateDirectory(existingPluginDirectory);
            var sentinelPath = Path.Combine(existingPluginDirectory, "original.txt");
            var existingDllPath = Path.Combine(
                existingPluginDirectory,
                $"UniversalDeviceToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll");
            File.Copy(Assembly.GetExecutingAssembly().Location, existingDllPath);
            File.WriteAllText(sentinelPath, "original payload");

            var settings = new ApplicationSettings();
            settings.Store.InstalledExtensions.Add(pluginId);
            settings.Store.PendingDeletionExtensions.Add(pluginId);
            settings.SynchronizeStore();

            using var pluginManager = new PluginManager(
                settings,
                Mock.Of<IPluginSignatureValidator>(),
                Mock.Of<IPluginLoader>(),
                Mock.Of<IPluginRegistry>(),
                Mock.Of<IPluginFileSystemManager>());
            var service = new PluginInstallationService(
                pluginManager,
                (source, destination) =>
                {
                    if (string.Equals(source, existingPluginDirectory, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("simulated atomic backup failure");

                    Directory.Move(source, destination);
                });
            var zipPath = CreatePluginZipPackage(pluginId);
            TrustedPluginPackageStore.IsTrustedFile(existingDllPath).Should().BeFalse();

            Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

            await action.Should().ThrowAsync<IOException>()
                .WithMessage("*simulated atomic backup failure*");
            File.ReadAllText(sentinelPath).Should().Be("original payload");
            Directory.GetDirectories(Path.Combine(pluginsRoot, "local"), $"{pluginId}_backup_*")
                .Should()
                .BeEmpty();
            settings.Store.InstalledExtensions.Should().Equal(pluginId);
            settings.Store.PendingDeletionExtensions.Should().Equal(pluginId);
            TrustedPluginPackageStore.IsTrustedFile(existingDllPath).Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRelyOnAtomicManagerRollbackWhenCommitFails()
    {
        const string pluginId = "test-local-plugin";
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());

        try
        {
            var pluginManager = new Mock<IPluginManager>();
            pluginManager
                .Setup(manager => manager.ScanAndLoadPluginsAsync(true))
                .Returns(Task.CompletedTask);
            pluginManager
                .Setup(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()))
                .Throws(new InvalidOperationException("state commit failed"));

            var service = new PluginInstallationService(pluginManager.Object);
            var pluginsRoot = CreateTempDirectory();
            ConfigureActivatedPlugin(pluginManager, pluginsRoot);
            var zipPath = CreatePluginZipPackage(pluginId);

            Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*state commit failed*");
            Directory.Exists(Path.Combine(pluginsRoot, "local", pluginId)).Should().BeFalse();
            pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Once);
            pluginManager.Verify(
                manager => manager.RestorePluginInstallationState(It.IsAny<PluginInstallationStateSnapshot>()),
                Times.Never);
            pluginManager.Verify(manager => manager.UninstallPlugin(It.IsAny<string>()), Times.Never);
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
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);
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
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);
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
        pluginManager.Verify(manager => manager.CommitPluginInstallation(manifestPluginId, It.IsAny<IDisposable?>()), Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectRootedManifestIdBeforeTargetCreation()
    {
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var rootedTarget = Path.Combine(sandbox, "outside", "rooted-plugin");
        Directory.CreateDirectory(pluginsRoot);

        var manifest = JsonSerializer.Serialize(new
        {
            id = rootedTarget,
            name = "Invalid Rooted Plugin",
            version = "1.0.0"
        });
        var zipPath = CreatePluginZipPackage("safe-plugin", manifestContent: manifest);
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*invalid plugin ID*");
        Directory.Exists(rootedTarget).Should().BeFalse();
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectTraversalManifestIdWithoutTouchingSibling()
    {
        const string manifestPluginId = "../victim";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var victimDirectory = Path.Combine(pluginsRoot, "victim");
        var sentinelPath = Path.Combine(victimDirectory, "sentinel.txt");
        Directory.CreateDirectory(victimDirectory);
        File.WriteAllText(sentinelPath, "preserve");

        var manifest = JsonSerializer.Serialize(new
        {
            id = manifestPluginId,
            name = "Invalid Traversal Plugin",
            version = "1.0.0"
        });
        var zipPath = CreatePluginZipPackage("safe-plugin", manifestContent: manifest);
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*invalid plugin ID*");
        File.ReadAllText(sentinelPath).Should().Be("preserve");
        Directory.GetDirectories(pluginsRoot, "victim_backup_*").Should().BeEmpty();
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectBackslashTraversalManifestIdWithoutTouchingSibling()
    {
        const string manifestPluginId = @"..\victim";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var victimDirectory = Path.Combine(pluginsRoot, "victim");
        var sentinelPath = Path.Combine(victimDirectory, "sentinel.txt");
        Directory.CreateDirectory(victimDirectory);
        File.WriteAllText(sentinelPath, "preserve");

        var manifest = JsonSerializer.Serialize(new
        {
            id = manifestPluginId,
            name = "Invalid Traversal Plugin",
            version = "1.0.0"
        });
        var zipPath = CreatePluginZipPackage("safe-plugin", manifestContent: manifest);
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*invalid plugin ID*");
        File.ReadAllText(sentinelPath).Should().Be("preserve");
        Directory.Exists(Path.Combine(pluginsRoot, "local")).Should().BeFalse();
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectParentEscapeManifestIdWithoutTouchingOutside()
    {
        const string manifestPluginId = @"..\..\outside";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var outsideDirectory = Path.Combine(sandbox, "outside");
        var sentinelPath = Path.Combine(outsideDirectory, "sentinel.txt");
        Directory.CreateDirectory(pluginsRoot);
        Directory.CreateDirectory(outsideDirectory);
        File.WriteAllText(sentinelPath, "preserve");

        var manifest = JsonSerializer.Serialize(new
        {
            id = manifestPluginId,
            name = "Invalid Parent Escape Plugin",
            version = "1.0.0"
        });
        var zipPath = CreatePluginZipPackage("safe-plugin", manifestContent: manifest);
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*invalid plugin ID*");
        File.ReadAllText(sentinelPath).Should().Be("preserve");
        Directory.GetFileSystemEntries(outsideDirectory).Should().Equal(sentinelPath);
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectTraversalManifestIdWithoutTouchingExistingLocalPlugin()
    {
        const string existingPluginId = "legit-plugin";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var existingDirectory = CreateExistingPlugin(pluginsRoot, existingPluginId, "preserve-local");

        var manifest = JsonSerializer.Serialize(new
        {
            id = "../" + existingPluginId,
            name = "Invalid Traversal Plugin",
            version = "1.0.0"
        });
        var zipPath = CreatePluginZipPackage("safe-plugin", manifestContent: manifest);
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*invalid plugin ID*");
        File.ReadAllText(Path.Combine(existingDirectory, "original.txt")).Should().Be("preserve-local");
        Directory.Exists(existingDirectory).Should().BeTrue();
        Directory.GetDirectories(Path.Combine(pluginsRoot, "local"), $"{existingPluginId}_backup_*")
            .Should().BeEmpty();
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectTraversalManifestIdInWrapperDirectory()
    {
        const string manifestPluginId = "../victim";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var victimDirectory = Path.Combine(pluginsRoot, "victim");
        var sentinelPath = Path.Combine(victimDirectory, "sentinel.txt");
        Directory.CreateDirectory(victimDirectory);
        File.WriteAllText(sentinelPath, "preserve");

        var zipPath = CreateArchive(archive =>
        {
            var manifestEntry = archive.CreateEntry("safe-wrapper/plugin.json");
            using (var writer = new StreamWriter(manifestEntry.Open()))
            {
                writer.Write(JsonSerializer.Serialize(new
                {
                    id = manifestPluginId,
                    name = "Invalid Traversal Plugin",
                    version = "1.0.0"
                }));
            }

            var dllEntry = archive.CreateEntry("safe-wrapper/UniversalDeviceToolkit.Plugins.SafePlugin.dll");
            using var source = File.OpenRead(Assembly.GetExecutingAssembly().Location);
            using var destination = dllEntry.Open();
            source.CopyTo(destination);
        });
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*invalid plugin ID*");
        File.ReadAllText(sentinelPath).Should().Be("preserve");
        Directory.Exists(Path.Combine(pluginsRoot, "local")).Should().BeFalse();
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldFailPackageWhenAnyArchiveEntryIsInvalid()
    {
        const string pluginId = "test-local-plugin";
        var zipPath = CreatePluginZipPackage(pluginId);
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
        {
            var invalidEntry = archive.CreateEntry("../escaped.txt");
            using var writer = new StreamWriter(invalidEntry.Open());
            writer.Write("must not be extracted");
        }

        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*invalid entry path*");
        Directory.Exists(Path.Combine(pluginsRoot, "local", pluginId)).Should().BeFalse();
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectArchiveOverEntryCountQuota()
    {
        var zipPath = CreateArchive(archive =>
        {
            for (var index = 0; index <= PluginInstallationService.MaxArchiveEntryCount; index++)
                archive.CreateEntry($"entries/{index}.txt", CompressionLevel.NoCompression);
        });
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*2048*");
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectExcessiveCompressionRatio()
    {
        var zipPath = CreateArchive(archive =>
        {
            var entry = archive.CreateEntry("payload.bin", CompressionLevel.SmallestSize);
            using var stream = entry.Open();
            var buffer = new byte[8192];
            var remaining = PluginInstallationService.MinimumCompressionRatioCheckBytes + buffer.Length;
            while (remaining > 0)
            {
                var bytesToWrite = (int)Math.Min(buffer.Length, remaining);
                stream.Write(buffer, 0, bytesToWrite);
                remaining -= bytesToWrite;
            }
        });
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*compression ratio limit*");
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectAncestorDirectorySymlink()
    {
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var outsideDirectory = Path.Combine(sandbox, "outside");
        Directory.CreateDirectory(pluginsRoot);
        Directory.CreateDirectory(outsideDirectory);
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(pluginsRoot, "local"), outsideDirectory);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException or PlatformNotSupportedException ||
            ex is IOException && OperatingSystem.IsWindows())
        {
            throw Xunit.Sdk.SkipException.ForSkip($"Directory symlinks are unavailable: {ex.Message}");
        }

        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);
        var zipPath = CreatePluginZipPackage("symlink-probe");

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*symbolic link or reparse point*");
        Directory.GetFileSystemEntries(outsideDirectory).Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectParentChangedAtMutationBoundary()
    {
        const string pluginId = "changed-parent-probe";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var localRoot = Path.Combine(pluginsRoot, "local");
        var outsideDirectory = Path.Combine(sandbox, "outside");
        var targetDirectory = Path.Combine(localRoot, pluginId);
        Directory.CreateDirectory(pluginsRoot);
        Directory.CreateDirectory(outsideDirectory);
        var parentChanged = false;
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(
            pluginManager.Object,
            Directory.Move,
            path =>
            {
                if (parentChanged || !path.Equals(targetDirectory, StringComparison.OrdinalIgnoreCase))
                    return;

                Directory.Delete(localRoot);
                try
                {
                    Directory.CreateSymbolicLink(localRoot, outsideDirectory);
                }
                catch (Exception ex) when (
                    ex is UnauthorizedAccessException or PlatformNotSupportedException ||
                    ex is IOException && OperatingSystem.IsWindows())
                {
                    throw Xunit.Sdk.SkipException.ForSkip($"Directory symlinks are unavailable: {ex.Message}");
                }

                parentChanged = true;
            });
        var zipPath = CreatePluginZipPackage(pluginId);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*symbolic link or reparse point*");
        Directory.GetFileSystemEntries(outsideDirectory).Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectZip64EntryCountBeforeOpeningArchive()
    {
        var zipPath = CreateRawArchive(writer =>
        {
            writer.Write(0x06064b50u);
            writer.Write(44UL);
            writer.Write((ushort)45);
            writer.Write((ushort)45);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(2049UL);
            writer.Write(2049UL);
            writer.Write(0UL);
            writer.Write(0UL);
            writer.Write(0x07064b50u);
            writer.Write(0u);
            writer.Write(0UL);
            writer.Write(1u);
            writer.Write(0x06054b50u);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(ushort.MaxValue);
            writer.Write(ushort.MaxValue);
            writer.Write(uint.MaxValue);
            writer.Write(uint.MaxValue);
            writer.Write((ushort)0);
        });
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*more than 2048 entries*");
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectOversizedCentralDirectoryMetadata()
    {
        var zipPath = CreateRawArchive(writer =>
        {
            writer.Write(0x06054b50u);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((uint)(PluginInstallationService.MaxCentralDirectoryBytes + 1));
            writer.Write(0u);
            writer.Write((ushort)0);
        });
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*central directory exceeds*");
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectOversizedCompressedArchive()
    {
        var archiveDirectory = CreateTempDirectory();
        var zipPath = Path.Combine(archiveDirectory, "oversized.zip");
        using (var stream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            stream.SetLength(PluginInstallationService.MaxArchiveCompressedBytes + 1);
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*must be between*");
        pluginManager.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("CONIN$.txt")]
    [InlineData("CONOUT$.txt")]
    [InlineData("COM\u00B9.txt")]
    [InlineData("LPT\u00B3.txt")]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectWindowsDeviceAliases(string entryName)
    {
        var zipPath = CreateArchive(archive => archive.CreateEntry(entryName));
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*invalid entry path*");
        pluginManager.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRollbackCancellationAfterActivationScan()
    {
        const string pluginId = "cancel-after-scan";
        var pluginsRoot = CreateTempDirectory();
        using var cancellationSource = new CancellationTokenSource();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Callback(cancellationSource.Cancel)
            .Returns(Task.CompletedTask);
        var service = new PluginInstallationService(pluginManager.Object);
        var zipPath = CreatePluginZipPackage(pluginId);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(
            zipPath,
            pluginsRoot,
            cancellationSource.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        Directory.Exists(Path.Combine(pluginsRoot, "local", pluginId)).Should().BeFalse();
        pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Never);
        pluginManager.Verify(
            manager => manager.RollbackPreparedPluginInstallation(
                pluginId,
                It.IsAny<IDisposable?>()),
            Times.Once);
        pluginManager.Verify(manager => manager.ForgetPluginRuntime(pluginId, It.IsAny<IDisposable?>()), Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectActivationWithDifferentPluginId()
    {
        const string pluginId = "expected-plugin";
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .ThrowsAsync(new InvalidOperationException(
                "Plugin expected-plugin did not activate from the expected assembly."));
        var service = new PluginInstallationService(pluginManager.Object);
        var zipPath = CreatePluginZipPackage(pluginId);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*did not activate*");
        Directory.Exists(Path.Combine(pluginsRoot, "local", pluginId)).Should().BeFalse();
        pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Never);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldFailWhenProductionScannerRejectsSignature()
    {
        const string pluginId = "unsigned-production-plugin";
        var pluginsRoot = CreateTempDirectory();
        var expectedDll = Path.Combine(
            pluginsRoot,
            "local",
            pluginId,
            $"UniversalDeviceToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll");
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(manager => manager.GetPluginsDirectory()).Returns(pluginsRoot);
        fileSystem.Setup(manager => manager.GetPluginDllFiles())
            .Returns(() => File.Exists(expectedDll) ? [expectedDll] : []);
        fileSystem.Setup(manager => manager.GetCultureFolders())
            .Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        using var pluginManager = new PluginManager(
            new ApplicationSettings(),
            new PluginSignatureValidator(PluginSignatureSettings.Production),
            Mock.Of<IPluginLoader>(),
            new PluginRegistry(),
            fileSystem.Object);
        var service = new PluginInstallationService(pluginManager);
        var zipPath = CreatePluginZipPackage(pluginId);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*did not activate from the expected assembly*");
        Directory.Exists(Path.Combine(pluginsRoot, "local", pluginId)).Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldKeepBackupOutsideScannerRoot()
    {
        const string pluginId = "backup-isolation-plugin";
        var pluginsRoot = CreateTempDirectory();
        var existingDirectory = Path.Combine(pluginsRoot, "local", pluginId);
        Directory.CreateDirectory(existingDirectory);
        var oldDllName =
            $"LenovoLegionToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll";
        File.Copy(
            Assembly.GetExecutingAssembly().Location,
            Path.Combine(existingDirectory, oldDllName));
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Callback(() => Directory
                .GetFiles(pluginsRoot, oldDllName, SearchOption.AllDirectories)
                .Should()
                .BeEmpty("the displaced backup must be outside every scanner root"))
            .Returns(Task.CompletedTask);
        var service = new PluginInstallationService(pluginManager.Object);
        var zipPath = CreatePluginZipPackage(pluginId);

        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        result.Should().BeTrue();
        pluginManager.Verify(
            manager => manager.ActivatePluginRuntimeStrictAsync(pluginId, It.IsAny<string>(), It.IsAny<IDisposable?>()),
            Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldAcceptValidLegacyPrefixedPackage()
    {
        const string pluginId = "LegacyImport";
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        pluginManager
            .Setup(manager => manager.GetPluginMetadata(pluginId))
            .Returns(new PluginMetadata
            {
                Id = pluginId,
                FilePath = Path.Combine(
                    pluginsRoot,
                    "local",
                    pluginId,
                    $"LenovoLegionToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll"),
            });
        var packageDirectory = CreateTempDirectory();
        var packageRoot = Path.Combine(packageDirectory, "package");
        Directory.CreateDirectory(packageRoot);
        File.Copy(
            Assembly.GetExecutingAssembly().Location,
            Path.Combine(
                packageRoot,
                $"LenovoLegionToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll"));
        var zipPath = Path.Combine(packageDirectory, "legacy.zip");
        ZipFile.CreateFromDirectory(packageRoot, zipPath);
        var service = new PluginInstallationService(pluginManager.Object);

        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        result.Should().BeTrue();
        pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldAcceptCanonicalOfficialAssemblyName()
    {
        const string pluginId = "shell-integration";
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);
        var zipPath = CreatePluginZipPackage(pluginId);

        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        result.Should().BeTrue();
        File.Exists(Path.Combine(
            pluginsRoot,
            "local",
            pluginId,
            "UniversalDeviceToolkit.Plugins.ShellIntegration.dll")).Should().BeTrue();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectAmbiguousCanonicalAssemblies()
    {
        var packageDirectory = CreateTempDirectory();
        var packageRoot = Path.Combine(packageDirectory, "package");
        Directory.CreateDirectory(packageRoot);
        var source = Assembly.GetExecutingAssembly().Location;
        File.Copy(source, Path.Combine(
            packageRoot,
            "UniversalDeviceToolkit.Plugins.ShellIntegration.dll"));
        File.Copy(source, Path.Combine(
            packageRoot,
            "LenovoLegionToolkit.Plugins.ShellIntegration.dll"));
        File.WriteAllText(
            Path.Combine(packageRoot, "plugin.json"),
            """{"id":"shell-integration","name":"Shell Integration","version":"1.0.0"}""");
        var zipPath = Path.Combine(packageDirectory, "shell-integration.zip");
        ZipFile.CreateFromDirectory(packageRoot, zipPath);
        var pluginsRoot = CreateTempDirectory();
        var service = new PluginInstallationService(CreatePluginManagerMock(pluginsRoot).Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No unambiguous plugin DLL*");
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectCanonicalIdMismatch()
    {
        var packageDirectory = CreateTempDirectory();
        var packageRoot = Path.Combine(packageDirectory, "package");
        Directory.CreateDirectory(packageRoot);
        File.Copy(
            Assembly.GetExecutingAssembly().Location,
            Path.Combine(packageRoot, "UniversalDeviceToolkit.Plugins.CustomMouse.dll"));
        File.WriteAllText(
            Path.Combine(packageRoot, "plugin.json"),
            """{"id":"shell-integration","name":"Shell Integration","version":"1.0.0"}""");
        var zipPath = Path.Combine(packageDirectory, "shell-integration.zip");
        ZipFile.CreateFromDirectory(packageRoot, zipPath);
        var pluginsRoot = CreateTempDirectory();
        var service = new PluginInstallationService(CreatePluginManagerMock(pluginsRoot).Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No unambiguous plugin DLL*");
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRetainBackupWhenReplacementDeleteFails()
    {
        const string pluginId = "delete-rollback";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var target = CreateExistingPlugin(pluginsRoot, pluginId, "original-delete-bytes");
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .ThrowsAsync(new InvalidOperationException("replacement activation failed"));
        var targetBoundaryCalls = 0;
        var service = new PluginInstallationService(
            pluginManager.Object,
            Directory.Move,
            path =>
            {
                if (path.Equals(target, StringComparison.OrdinalIgnoreCase) &&
                    ++targetBoundaryCalls == 2)
                {
                    throw new IOException("replacement delete failed");
                }
            });

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(
            CreatePluginZipPackage(pluginId),
            pluginsRoot);

        await action.Should().ThrowAsync<AggregateException>()
            .WithMessage("*rollback is incomplete*Recovery material:*");
        var recoveryFile = FindRecoveryFile(sandbox, "original.txt");
        recoveryFile.Should().NotBeNull();
        File.ReadAllText(recoveryFile!).Should().Be("original-delete-bytes");
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRetainBackupWhenRestoreMoveFails()
    {
        const string pluginId = "restore-rollback";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        CreateExistingPlugin(pluginsRoot, pluginId, "original-restore-bytes");
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .ThrowsAsync(new InvalidOperationException("replacement activation failed"));
        var service = new PluginInstallationService(
            pluginManager.Object,
            (source, destination) =>
            {
                if (Path.GetFileName(source).Equals("backup", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("backup restoration failed");
                Directory.Move(source, destination);
            });

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(
            CreatePluginZipPackage(pluginId),
            pluginsRoot);

        await action.Should().ThrowAsync<AggregateException>()
            .WithMessage("*rollback is incomplete*Recovery material:*");
        var recoveryFile = FindRecoveryFile(sandbox, "original.txt");
        recoveryFile.Should().NotBeNull();
        File.ReadAllText(recoveryFile!).Should().Be("original-restore-bytes");
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldLeaveOriginalFilesWhenRuntimeUnloadFails()
    {
        const string pluginId = "unload-failure";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var target = CreateExistingPlugin(pluginsRoot, pluginId, "original-unload-bytes");
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        pluginManager.Setup(manager => manager.ForgetPluginRuntime(pluginId, It.IsAny<IDisposable?>())).Returns(false);
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(
            CreatePluginZipPackage(pluginId),
            pluginsRoot);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*runtime could not be unloaded*");
        File.ReadAllText(Path.Combine(target, "original.txt"))
            .Should()
            .Be("original-unload-bytes");
        pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Never);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldReportDegradedRecoveryWhenOriginalRestartFails()
    {
        const string pluginId = "degraded-recovery";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var target = CreateExistingPlugin(pluginsRoot, pluginId, "original-recovery-bytes");
        var oldPlugin = new Mock<IPlugin>();
        oldPlugin.SetupGet(plugin => plugin.Id).Returns(pluginId);
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        pluginManager
            .Setup(manager => manager.CapturePluginRuntimeSnapshot())
            .Returns(new PluginRuntimeSnapshot(
                new Dictionary<string, PluginRuntimeIdentity>(StringComparer.OrdinalIgnoreCase)
                {
                    [pluginId] = new(
                        oldPlugin.Object,
                        Path.Combine(
                            target,
                            $"UniversalDeviceToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll"),
                        true),
                }));
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .ThrowsAsync(new InvalidOperationException("replacement failed"));
        pluginManager
            .Setup(manager => manager.RestorePluginRuntimeSnapshot(
                It.IsAny<PluginRuntimeSnapshot>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<PluginRuntimeReconciliation?>()))
            .Throws(new InvalidOperationException("original restart failed"));
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(
            CreatePluginZipPackage(pluginId),
            pluginsRoot);

        await action.Should().ThrowAsync<AggregateException>()
            .WithMessage("*rollback is incomplete*Recovery material:*");
        File.ReadAllText(Path.Combine(target, "original.txt"))
            .Should()
            .Be("original-recovery-bytes");
        pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Never);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_WhenBackupFingerprintChanges_ShouldNotMutateRollbackPayloads()
    {
        const string pluginId = "tampered-local-backup";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var target = CreateExistingPlugin(pluginsRoot, pluginId, "original-before-tamper");
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        string? backupDirectory = null;
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                pluginId,
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Callback(() =>
            {
                var transactionRoot = Path.Combine(sandbox, ".udt-plugin-transactions");
                backupDirectory = Directory.GetDirectories(
                        transactionRoot,
                        "backup",
                        SearchOption.AllDirectories)
                    .Single();
                File.WriteAllText(
                    Path.Combine(backupDirectory, "original.txt"),
                    "tampered-backup-bytes");
            })
            .ThrowsAsync(new InvalidOperationException("activation failed after backup tamper"));
        var service = new PluginInstallationService(pluginManager.Object);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(
            CreatePluginZipPackage(pluginId),
            pluginsRoot);

        await action.Should().ThrowAsync<AggregateException>()
            .WithMessage("*rollback is incomplete*Recovery material:*");
        backupDirectory.Should().NotBeNull();
        Directory.Exists(backupDirectory!).Should().BeTrue();
        File.ReadAllText(Path.Combine(backupDirectory!, "original.txt"))
            .Should().Be("tampered-backup-bytes");
        Directory.Exists(target).Should().BeTrue(
            "the replacement must not be moved or deleted after backup tamper is detected");
        Directory.GetFiles(target, "*.dll").Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_RealLoaderActivationFailure_ShouldRestoreRuntimeAndLeaveUnrelatedPlugin()
    {
        const string pluginId = "loader-fixture";
        const string unrelatedId = "unrelated-runtime";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var target = Path.Combine(pluginsRoot, "local", pluginId);
        Directory.CreateDirectory(target);
        var pluginPath = Path.Combine(
            target,
            Path.GetFileName(typeof(LoaderFixturePlugin).Assembly.Location));
        File.Copy(typeof(LoaderFixturePlugin).Assembly.Location, pluginPath);
        var sentinelPath = Path.Combine(target, "original.txt");
        File.WriteAllText(sentinelPath, "original-real-loader-payload");
        var signatureValidator = new Mock<IPluginSignatureValidator>();
        signatureValidator
            .Setup(candidate => candidate.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid));
        var fileSystem = new Mock<IPluginFileSystemManager>();
        fileSystem.Setup(candidate => candidate.GetPluginsDirectory()).Returns(pluginsRoot);
        var loader = new PluginLoader();
        var registry = new PluginRegistry();
        var unrelatedPlugin = new Mock<IPlugin>();
        unrelatedPlugin.SetupGet(candidate => candidate.Id).Returns(unrelatedId);
        registry.Register(
            unrelatedPlugin.Object,
            new PluginMetadata
            {
                Id = unrelatedId,
                FilePath = Path.Combine(pluginsRoot, "unrelated.dll"),
            });
        var settings = new ApplicationSettings();
        settings.Store.InstalledExtensions.RemoveAll(
            id => id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        settings.Store.PendingDeletionExtensions.RemoveAll(
            id => id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        settings.SynchronizeStore();
        using var manager = new PluginManager(
            settings,
            signatureValidator.Object,
            loader,
            registry,
            fileSystem.Object);
        using (var lease = manager.AcquirePluginMutation(pluginId))
        {
            await manager.LoadPluginRuntimeStrictAsync(pluginId, pluginPath, lease);
            manager.PreparePluginInstallation(pluginId, lease);
            await manager.ActivatePluginRuntimeStrictAsync(pluginId, pluginPath, lease);
            manager.CommitPluginInstallation(pluginId, lease);
        }
        var service = new PluginInstallationService(manager);
        var replacementZip = CreateRealLoaderFixturePackage();
        Environment.SetEnvironmentVariable("UDT_LOADER_FIXTURE_FAIL_START_ONCE", "1");
        try
        {
            Func<Task> action = () => service.ExtractAndInstallPluginAsync(
                replacementZip,
                pluginsRoot);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*one-time fixture startup failure*");

            Directory.Exists(target).Should().BeTrue();
            File.Exists(sentinelPath).Should().BeTrue(
                $"the original sidecar must be restored; files present: {string.Join(", ", Directory.GetFiles(target, "*", SearchOption.AllDirectories))}");
            File.ReadAllText(sentinelPath).Should().Be("original-real-loader-payload");
            registry.Get(pluginId).Should().NotBeNull(
                "the original real runtime must be automatically restored");
            registry.Get(unrelatedId).Should().BeSameAs(unrelatedPlugin.Object);
            registry.GetMetadata(unrelatedId)!.FilePath
                .Should().Be(Path.Combine(pluginsRoot, "unrelated.dll"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("UDT_LOADER_FIXTURE_FAIL_START_ONCE", null);
            manager.ForgetPluginRuntime(pluginId);
            registry.Forget(unrelatedId);
        }
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldRejectAtomicMoveProbeFailure()
    {
        const string pluginId = "cross-volume-probe";
        var sandbox = CreateTempDirectory();
        var pluginsRoot = Path.Combine(sandbox, "plugins");
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(
            pluginManager.Object,
            Directory.Move,
            mutationBoundary: null,
            atomicMoveSupported: static (_, _) => false);

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(
            CreatePluginZipPackage(pluginId),
            pluginsRoot);

        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*cannot use an atomic rename*");
        Directory.Exists(Path.Combine(pluginsRoot, "local", pluginId)).Should().BeFalse();
        pluginManager.Verify(manager => manager.CommitPluginInstallation(pluginId, It.IsAny<IDisposable?>()), Times.Never);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldHonorCancellationBeforeExtraction()
    {
        var zipPath = CreatePluginZipPackage("test-local-plugin");
        var pluginsRoot = CreateTempDirectory();
        var pluginManager = CreatePluginManagerMock(pluginsRoot);
        var service = new PluginInstallationService(pluginManager.Object);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Func<Task> action = () => service.ExtractAndInstallPluginAsync(
            zipPath,
            pluginsRoot,
            cancellationSource.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        pluginManager.VerifyNoOtherCalls();
    }

    private delegate bool TryGetPluginCallback(string pluginId, out IPlugin? plugin);

    private string CreateRealLoaderFixturePackage()
    {
        var packagePath = Path.Combine(
            CreateTempDirectory(),
            "loader-fixture.zip");
        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(
            typeof(LoaderFixturePlugin).Assembly.Location,
            Path.GetFileName(typeof(LoaderFixturePlugin).Assembly.Location));
        var manifestEntry = archive.CreateEntry("plugin.json");
        using (var writer = new StreamWriter(manifestEntry.Open()))
        {
            writer.Write(
                """
                {
                  "id": "loader-fixture",
                  "name": "Loader fixture",
                  "version": "1.0.0",
                  "minimumHostVersion": "1.0.0"
                }
                """);
        }
        return packagePath;
    }

    private static Mock<IPluginManager> CreatePluginManagerMock(string pluginsRoot)
    {
        var pluginManager = new Mock<IPluginManager>();
        pluginManager
            .Setup(manager => manager.ScanAndLoadPluginsAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        pluginManager
            .Setup(manager => manager.AcquirePluginMutation(It.IsAny<string>()))
            .Returns(Mock.Of<IDisposable>());
        pluginManager
            .Setup(manager => manager.CapturePluginRuntimeSnapshot())
            .Returns(new PluginRuntimeSnapshot(
                new Dictionary<string, PluginRuntimeIdentity>(
                    StringComparer.OrdinalIgnoreCase)));
        pluginManager
            .Setup(manager => manager.ForgetPluginRuntime(
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(true);
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(Task.CompletedTask);
        pluginManager
            .Setup(manager => manager.LoadPluginRuntimeStrictAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(Task.CompletedTask);
        pluginManager
            .Setup(manager => manager.CommitPluginInstallation(
                It.IsAny<string>(),
                It.IsAny<IDisposable?>(),
                It.IsAny<Action?>()))
            .Returns((string pluginId, IDisposable? _, Action? coordinatedCommit) =>
            {
                coordinatedCommit?.Invoke();
                return new PluginInstallationStateSnapshot(pluginId, false, false);
            });
        ConfigureActivatedPlugin(pluginManager, pluginsRoot);
        return pluginManager;
    }

    private static void ConfigureActivatedPlugin(
        Mock<IPluginManager> pluginManager,
        string pluginsRoot)
    {
        pluginManager
            .Setup(manager => manager.AcquirePluginMutation(It.IsAny<string>()))
            .Returns(Mock.Of<IDisposable>());
        pluginManager
            .Setup(manager => manager.CapturePluginRuntimeSnapshot())
            .Returns(new PluginRuntimeSnapshot(
                new Dictionary<string, PluginRuntimeIdentity>(
                    StringComparer.OrdinalIgnoreCase)));
        pluginManager
            .Setup(manager => manager.ForgetPluginRuntime(
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(true);
        pluginManager
            .Setup(manager => manager.ActivatePluginRuntimeStrictAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(Task.CompletedTask);
        pluginManager
            .Setup(manager => manager.LoadPluginRuntimeStrictAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDisposable?>()))
            .Returns(Task.CompletedTask);
        pluginManager
            .Setup(manager => manager.TryGetPlugin(
                It.IsAny<string>(),
                out It.Ref<IPlugin?>.IsAny))
            .Returns(new TryGetPluginCallback((string pluginId, out IPlugin? plugin) =>
            {
                var loadedPlugin = new Mock<IPlugin>();
                loadedPlugin.SetupGet(candidate => candidate.Id).Returns(pluginId);
                plugin = loadedPlugin.Object;
                return true;
            }));
        pluginManager
            .Setup(manager => manager.GetPluginMetadata(It.IsAny<string>()))
            .Returns((string pluginId) => new PluginMetadata
            {
                Id = pluginId,
                FilePath = Path.Combine(
                    pluginsRoot,
                    "local",
                    pluginId,
                    $"UniversalDeviceToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll"),
            });
    }

    private string CreatePluginZipPackage(string pluginId, bool includeSharedRuntimeFiles = false, string? manifestContent = null)
    {
        var packageDirectory = CreateTempDirectory();
        var packageRoot = Path.Combine(packageDirectory, "package");
        Directory.CreateDirectory(packageRoot);

        var assemblySourcePath = Assembly.GetExecutingAssembly().Location;
        var assemblyFileName =
            $"UniversalDeviceToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll";
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

    private static string ToOfficialAssemblyToken(string pluginId) =>
        string.Concat(
            pluginId
                .Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));

    private static string CreateExistingPlugin(
        string pluginsRoot,
        string pluginId,
        string originalBytes)
    {
        var target = Path.Combine(pluginsRoot, "local", pluginId);
        Directory.CreateDirectory(target);
        File.Copy(
            Assembly.GetExecutingAssembly().Location,
            Path.Combine(
                target,
                $"UniversalDeviceToolkit.Plugins.{ToOfficialAssemblyToken(pluginId)}.dll"));
        File.WriteAllText(
            Path.Combine(target, "plugin.json"),
            $$"""{"id":"{{pluginId}}","name":"Existing","version":"1.0.0"}""");
        File.WriteAllText(Path.Combine(target, "original.txt"), originalBytes);
        return target;
    }

    private static string? FindRecoveryFile(string sandbox, string fileName) =>
        Directory
            .GetFiles(
                Path.Combine(sandbox, ".udt-plugin-transactions"),
                fileName,
                SearchOption.AllDirectories)
            .SingleOrDefault();

    private string CreateArchive(Action<ZipArchive> configure)
    {
        var archiveDirectory = CreateTempDirectory();
        var zipPath = Path.Combine(archiveDirectory, "archive.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        configure(archive);
        return zipPath;
    }

    private string CreateRawArchive(Action<BinaryWriter> writeArchive)
    {
        var archiveDirectory = CreateTempDirectory();
        var zipPath = Path.Combine(archiveDirectory, "raw-archive.zip");
        using var stream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);
        writeArchive(writer);
        return zipPath;
    }
}
