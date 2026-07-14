using System.IO;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

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

    [Fact]
    public void TryValidateForExecution_WhenInstallerPathIsNull_ShouldFail()
    {
        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            null,
            "/some/dir",
            "setup.exe",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("empty");
    }

    [Fact]
    public void TryValidateForExecution_WhenInstallerPathIsWhitespace_ShouldFail()
    {
        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            "   ",
            "/some/dir",
            "setup.exe",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("empty");
    }

    [Fact]
    public void TryValidateForExecution_WhenDownloadDirectoryIsNull_ShouldFail()
    {
        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            "/some/path/setup.exe",
            null,
            "setup.exe",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("Download directory is empty");
    }

    [Fact]
    public void TryValidateForExecution_WhenDownloadDirectoryIsWhitespace_ShouldFail()
    {
        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            "/some/path/setup.exe",
            "  ",
            "setup.exe",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("Download directory is empty");
    }

    [Fact]
    public void TryValidateForExecution_WhenExpectedFileNameIsNull_ShouldFail()
    {
        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            "/some/path/setup.exe",
            "/some/dir",
            null,
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("Expected installer file name is empty");
    }

    [Fact]
    public void TryValidateForExecution_WhenExpectedFileNameIsWhitespace_ShouldFail()
    {
        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            "/some/path/setup.exe",
            "/some/dir",
            "  ",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("Expected installer file name is empty");
    }

    [Fact]
    public void TryValidateForExecution_WhenInstallerPathDoesNotExist_ShouldFail()
    {
        var downloadDirectory = CreateTempDirectory();
        var nonExistentPath = Path.Combine(downloadDirectory, "missing.exe");

        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            nonExistentPath,
            downloadDirectory,
            "missing.exe",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().Contain("does not exist");
    }

    [Fact]
    public void TryValidateForExecution_WhenInstallerPathIsDirectory_ShouldFail()
    {
        var downloadDirectory = CreateTempDirectory();
        var subDirectory = CreateTempDirectory();

        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            subDirectory,
            downloadDirectory,
            "subdir",
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
    }

    [Fact]
    public void TryValidateForExecution_WhenAllParametersNull_ShouldFail()
    {
        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            null,
            null,
            null,
            out var normalizedPath,
            out var failureReason);

        result.Should().BeFalse();
        normalizedPath.Should().BeEmpty();
        failureReason.Should().NotBeEmpty();
    }

    [Fact]
    public void TryValidateForExecution_WhenMatchingNameIsCaseInsensitive_ShouldSucceed()
    {
        var downloadDirectory = CreateTempDirectory();
        var installerPath = Path.Combine(downloadDirectory, "Setup.EXE");
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
    public void TryValidateForExecution_WhenSubdirectoryTraversal_ShouldFail()
    {
        var downloadDirectory = CreateTempDirectory();
        var subDir = Path.Combine(downloadDirectory, "sub");
        Directory.CreateDirectory(subDir);
        TempDirectories.Add(subDir);

        var installerPath = Path.Combine(subDir, "..", "escape.exe");
        File.WriteAllText(Path.Combine(downloadDirectory, "escape.exe"), "installer");
        TempFiles.Add(Path.Combine(downloadDirectory, "escape.exe"));

        var result = InstallerLaunchPathValidator.TryValidateForExecution(
            installerPath,
            downloadDirectory,
            "escape.exe",
            out var normalizedPath,
            out var failureReason);

        // After normalization, the path resolves to inside the download dir
        result.Should().BeTrue();
        normalizedPath.Should().Contain("escape.exe");
    }
}
