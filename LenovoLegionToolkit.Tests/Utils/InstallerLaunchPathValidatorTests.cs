using System.IO;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public class InstallerLaunchPathValidatorTests : TemporaryFileTestBase
{
    [Fact]
    public void TryValidateForExecution_WhenInstallerIsInsideDownloadDirectoryAndMatchesName_ShouldSucceed()
    {
        var downloadDirectory = CreateTempDirectory();
        var installerPath = Path.Combine(downloadDirectory, "setup.exe");
        File.WriteAllText(installerPath, "installer");
        TempFiles.Add(installerPath);

        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            installerPath,
            downloadDirectory,
            "setup.exe",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeTrue();
        normalizedPath.Should().Be(Path.GetFullPath(installerPath));
        failureReason.Should().BeEmpty();
    }

    [Fact]
    public void TryValidateForExecution_WhenInstallerEscapesDownloadDirectory_ShouldFail()
    {
        var downloadDirectory = CreateTempDirectory();
        var otherDirectory = CreateTempDirectory();
        var installerPath = Path.Combine(otherDirectory, "setup.exe");
        File.WriteAllText(installerPath, "installer");
        TempFiles.Add(installerPath);

        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            installerPath,
            downloadDirectory,
            "setup.exe",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("outside the configured download directory");
    }

    [Fact]
    public void TryValidateForExecution_WhenInstallerNameDoesNotMatchExpected_ShouldFail()
    {
        var downloadDirectory = CreateTempDirectory();
        var installerPath = Path.Combine(downloadDirectory, "renamed.exe");
        File.WriteAllText(installerPath, "installer");
        TempFiles.Add(installerPath);

        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            installerPath,
            downloadDirectory,
            "setup.exe",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("Unexpected installer file name");
    }
}
