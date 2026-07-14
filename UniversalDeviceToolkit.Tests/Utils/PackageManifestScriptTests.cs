using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public sealed class PackageManifestScriptTests
{
    [Fact]
    public void PrepareAndTestPackageManifests_ShouldRoundTripReleaseMetadata()
    {
        var repositoryRoot = FindRepositoryRoot();
        var tempRoot = NewTempDirectory("UDT-package-manifests");
        const string version = "9.8.7";
        const string releaseDate = "2026-06-06";
        var installerHash = new string('a', 64);
        var hashManifestPath = Path.Combine(tempRoot, "UniversalDeviceToolkit_v9.8.7_SHA256.txt");
        var installerScriptPath = Path.Combine(tempRoot, "MakeInstaller.iss");

        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, "Packaging"));
            File.WriteAllText(
                hashManifestPath,
                $"{installerHash.ToUpperInvariant()}  LenovoLegionToolkit_v{version}_Setup.exe{Environment.NewLine}");
            File.WriteAllText(
                installerScriptPath,
                """
                #define MyAppPublisher "SSC-STUDIO"
                """);

            RunPowerShellScript(
                Path.Combine(repositoryRoot, "Packaging", "Prepare-PackageManifests.ps1"),
                [
                    "-RootPath", tempRoot,
                    "-Version", version,
                    "-ReleaseDate", releaseDate,
                    "-HashManifestPath", hashManifestPath,
                    "-UpdatePublishedScoopManifest",
                ],
                repositoryRoot);

            var wingetDirectory = Path.Combine(tempRoot, "Packaging", "winget", "manifests", "s", "SSC-STUDIO", "LenovoLegionToolkit", version);
            var scoopDraftPath = Path.Combine(tempRoot, "Packaging", "scoop", $"lenovolegiontoolkit.{version}.draft.json");
            var scoopPublishedPath = Path.Combine(tempRoot, "Packaging", "scoop", "lenovolegiontoolkit.json");

            File.Exists(Path.Combine(wingetDirectory, "SSC-STUDIO.LenovoLegionToolkit.installer.yaml")).Should().BeTrue();
            File.Exists(scoopDraftPath).Should().BeTrue();
            File.Exists(scoopPublishedPath).Should().BeTrue();

            var draftValidationOutput = RunPowerShellScript(
                Path.Combine(repositoryRoot, "Packaging", "Test-PackageManifests.ps1"),
                [
                    "-Version", version,
                    "-HashManifestPath", hashManifestPath,
                    "-InstallerScriptPath", installerScriptPath,
                    "-WingetManifestDirectory", wingetDirectory,
                    "-ScoopManifestPaths", scoopDraftPath,
                ],
                repositoryRoot);
            var publishedValidationOutput = RunPowerShellScript(
                Path.Combine(repositoryRoot, "Packaging", "Test-PackageManifests.ps1"),
                [
                    "-Version", version,
                    "-HashManifestPath", hashManifestPath,
                    "-InstallerScriptPath", installerScriptPath,
                    "-WingetManifestDirectory", wingetDirectory,
                    "-ScoopManifestPaths", scoopPublishedPath,
                ],
                repositoryRoot);

            draftValidationOutput.Should().Contain($"Package manifests match LenovoLegionToolkit_v{version}_Setup.exe");
            publishedValidationOutput.Should().Contain($"Package manifests match LenovoLegionToolkit_v{version}_Setup.exe");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void TestPackageManifests_ShouldRejectHashThatDiffersFromReleaseManifest()
    {
        var repositoryRoot = FindRepositoryRoot();
        var tempRoot = NewTempDirectory("UDT-package-manifest-hash-mismatch");
        const string version = "9.8.6";
        var releaseHash = new string('b', 64);
        var wrongHash = new string('c', 64);
        var hashManifestPath = Path.Combine(tempRoot, "UniversalDeviceToolkit_v9.8.6_SHA256.txt");

        try
        {
            File.WriteAllText(
                hashManifestPath,
                $"{releaseHash.ToUpperInvariant()}  LenovoLegionToolkit_v{version}_Setup.exe{Environment.NewLine}");

            RunPowerShellScript(
                    Path.Combine(repositoryRoot, "Packaging", "Test-PackageManifests.ps1"),
                    [
                        "-Version", version,
                        "-HashManifestPath", hashManifestPath,
                        "-ExpectedInstallerSha256", wrongHash,
                    ],
                    repositoryRoot,
                    expectSuccess: false)
                .Should()
                .Contain("Expected installer SHA256 and release SHA256 manifest mismatch");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string RunPowerShellScript(string scriptPath, string[] arguments, string workingDirectory, bool expectSuccess = true)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PowerShell.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit(30_000).Should().BeTrue($"{Path.GetFileName(scriptPath)} should finish quickly");
        if (expectSuccess)
        {
            process.ExitCode.Should().Be(0, error);
        }
        else
        {
            process.ExitCode.Should().NotBe(0, output);
        }

        return output + error;
    }

    private static string NewTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindRepositoryRoot()
    {
        var overrideRoot = Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(overrideRoot) &&
            File.Exists(Path.Combine(overrideRoot, "UniversalDeviceToolkit.sln")))
        {
            return Path.GetFullPath(overrideRoot);
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
