using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class InstallerRuntimeIntegrityGuardTests
{
    [Fact]
    public void DesktopRuntimeInstaller_ShouldUsePublishedHashBeforeExecution()
    {
        var root = RepositoryPaths.FindRoot();
        var engine = File.ReadAllText(Path.Combine(root, "Tools", "Installer", "InstallerEngine.cs"));
        var constants = File.ReadAllText(Path.Combine(root, "Tools", "Installer", "InstallerConstants.cs"));
        var downloader = File.ReadAllText(Path.Combine(root, "Tools", "Installer", "Downloader.cs"));

        constants.Should().Contain("DotNetRuntimeInstallerSha512");
        engine.Should().Contain("InstallerConstants.DotNetRuntimeInstallerSha512");
        engine.Should().Contain("Downloader.DownloadFileAsync");
        downloader.Should().Contain("ComputeSha512Async");
        engine.IndexOf("Downloader.DownloadFileAsync", StringComparison.Ordinal)
            .Should().BeLessThan(engine.IndexOf("RunProcessAsync(installerPath", StringComparison.Ordinal));
    }

    [Fact]
    public void DesktopRuntimeInstaller_ShouldUseUniqueTemporaryPathAndCleanup()
    {
        var root = RepositoryPaths.FindRoot();
        var engine = File.ReadAllText(Path.Combine(root, "Tools", "Installer", "InstallerEngine.cs"));

        engine.Should().Contain("udt-windowsdesktop-runtime-{Guid.NewGuid():N}.exe");
        engine.Should().Contain("File.Delete(installerPath)");
    }
}
