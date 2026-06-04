using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

public sealed class ShippingPayloadGuardTests
{
    [Fact]
    public void ShippingPayloadGuard_ShouldRejectTestAndValidationArtifacts()
    {
        var script = ReadRepositoryFile("Scripts", "Assert-ShippingPayload.ps1");

        script.Should().Contain("'UniversalDeviceToolkit.Tests'");
        script.Should().Contain("'UniversalDeviceToolkit.CrossPlatform.Tests'");
        script.Should().Contain("'MainAppPluginUi.Smoke'");
        script.Should().Contain("'HardwareValidation'");
        script.Should().Contain("'PresetUiValidation'");
        script.Should().Contain("'SensorInventoryDump'");
        script.Should().Contain("'testhost'");
        script.Should().Contain("'*.Tests.*'");
        script.Should().Contain("'*.Smoke.*'");
        script.Should().Contain("'*Validation*'");
    }

    [Fact]
    public void ReleaseWorkflow_ShouldValidateAllShippingPayloads()
    {
        var workflow = ReadRepositoryFile(".github", "workflows", "Release.yml");
        var releaseNotesScript = ReadRepositoryFile("Scripts", "New-ReleaseNotes.ps1");
        var languageAssetsScript = ReadRepositoryFile("Scripts", "Build-LanguageAssets.ps1");
        var crossPlatformScript = ReadRepositoryFile("Scripts", "Build-CrossPlatformCliAsset.ps1");
        var makeScript = ReadRepositoryFile("Make.bat");

        workflow.Should().Contain("SETUP_FULL_ASSET=UniversalDeviceToolkit_v$versionLabel");
        workflow.Should().Contain("$includeCrossPlatformCli = $majorVersionNumber -ge 5");
        workflow.Should().Contain("ENABLE_CROSS_PLATFORM_CLI=$($includeCrossPlatformCli.ToString().ToLowerInvariant())");
        workflow.Should().Contain("./Scripts/Assert-ShippingPayload.ps1 -PayloadPath $env:BUILD_OUTPUT");
        workflow.Should().Contain("./Scripts/Assert-ShippingPayload.ps1 -PayloadPath $env:ONLINE_BUILD_OUTPUT");
        workflow.Should().Contain("./Scripts/Build-CrossPlatformCliAsset.ps1");
        workflow.Should().Contain("if: ${{ env.ENABLE_CROSS_PLATFORM_CLI == 'true' }}");
        workflow.Should().Contain("-AssetVersion $env:VERSION");
        workflow.Should().Contain("$env:CLI_CROSS_PLATFORM_ASSET");
        workflow.Should().Contain("./Packaging/Prepare-PackageManifests.ps1");
        workflow.Should().Contain("-HashManifestPath \"$env:RELEASE_OUTPUT\\$env:HASH_ASSET\"");

        releaseNotesScript.Should().Contain("Get-CompatibilityLines");
        releaseNotesScript.Should().Contain("if ($hasCrossPlatformCli)");

        crossPlatformScript.Should().Contain("[string]$AssetVersion");
        crossPlatformScript.Should().Contain("$resolvedAssetVersion = if ([string]::IsNullOrWhiteSpace($AssetVersion)) { $Version } else { $AssetVersion }");
        crossPlatformScript.Should().Contain("${AssetPrefix}_v${resolvedAssetVersion}_CLI_cross-platform.zip");
        crossPlatformScript.Should().Contain("$shippingPayloadGuard = Resolve-RepoPath 'Scripts\\Assert-ShippingPayload.ps1'");
        crossPlatformScript.Should().Contain("& $shippingPayloadGuard -PayloadPath $publishOutputPath");

        languageAssetsScript.Should().Contain("Get-CrossPlatformCliAssetName");
        languageAssetsScript.Should().Contain("[switch]$IncludeCrossPlatformCli");
        languageAssetsScript.Should().Contain("if ($IncludeCrossPlatformCli)");
        languageAssetsScript.Should().Contain("if ($IncludeCrossPlatformCli -and (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $crossPlatformCliName)))");
        languageAssetsScript.Should().Contain("$hashAssetNames = @($fullSetupName, $onlineSetupName, $fullZipName, $onlineZipName, $legacySetupName)");
        languageAssetsScript.Should().Contain("$downloads['cli']");

        makeScript.Should().Contain("Scripts\\Build-CrossPlatformCliAsset.ps1");
        makeScript.Should().Contain("ENABLE_CROSS_PLATFORM_CLI");
        makeScript.Should().Contain("IF !VERSION_MAJOR! GEQ 5 SET ENABLE_CROSS_PLATFORM_CLI=1");
        makeScript.Should().Contain("%CROSS_PLATFORM_CLI_FINALIZE_ARG%");
        makeScript.IndexOf("Scripts\\Build-CrossPlatformCliAsset.ps1", StringComparison.Ordinal)
            .Should()
            .BeLessThan(makeScript.IndexOf("Scripts\\Build-LanguageAssets.ps1\" -FinalizeOnly", StringComparison.Ordinal));
    }

    [Fact]
    public void ReleaseNotes_ShouldOnlyAdvertiseCrossPlatformCliWhenCliAssetIsPresent()
    {
        var notesWithoutCli = RunReleaseNotesScript(
            "4.2.0",
            [
                "UniversalDeviceToolkit_v4.2.0_Full_Setup.exe",
                "UniversalDeviceToolkit_v4.2.0_Online_Setup.exe",
                "UniversalDeviceToolkit_v4.2.0_Full_win-x64.zip",
                "UniversalDeviceToolkit_v4.2.0_Online_win-x64.zip",
                "LenovoLegionToolkit_v4.2.0_Setup.exe",
                "UniversalDeviceToolkit_v4.2.0_SHA256.txt",
            ]);

        notesWithoutCli.Should().Contain("Desktop app and hardware controls: Windows 10/11 x64");
        notesWithoutCli.Should().NotContain("Cross-platform diagnostics CLI");
        notesWithoutCli.Should().NotContain("macOS, and Linux");
        notesWithoutCli.Should().NotContain("_CLI_cross-platform.zip");

        var notesWithCli = RunReleaseNotesScript(
            "5.0.0",
            [
                "UniversalDeviceToolkit_v5.0.0_Full_Setup.exe",
                "UniversalDeviceToolkit_v5.0.0_Online_Setup.exe",
                "UniversalDeviceToolkit_v5.0.0_Full_win-x64.zip",
                "UniversalDeviceToolkit_v5.0.0_Online_win-x64.zip",
                "UniversalDeviceToolkit_v5.0.0_CLI_cross-platform.zip",
                "LenovoLegionToolkit_v5.0.0_Setup.exe",
                "UniversalDeviceToolkit_v5.0.0_SHA256.txt",
            ]);

        notesWithCli.Should().Contain("UniversalDeviceToolkit_v5.0.0_CLI_cross-platform.zip");
        notesWithCli.Should().Contain("Cross-platform diagnostics CLI: Windows, macOS, and Linux with .NET 10 runtime");
    }

    private static string RunReleaseNotesScript(string version, string[] assetNames)
    {
        var repositoryRoot = FindRepositoryRoot();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"UDT-release-notes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var changelogPath = Path.Combine(tempDirectory, "CHANGELOG.md");
            File.WriteAllText(
                changelogPath,
                $"""
                # Changelog

                ## [{version}] - 2026-06-04

                ### Highlights

                - Synthetic release note guard.
                """);

            var wrapperPath = Path.Combine(tempDirectory, "run-release-notes.ps1");
            var assetArray = string.Join(
                Environment.NewLine,
                assetNames.Select(assetName => $"    '{EscapePowerShellSingleQuotedString(assetName)}'"));
            File.WriteAllText(
                wrapperPath,
                $"""
                $assetNames = @(
                {assetArray}
                )

                & '{EscapePowerShellSingleQuotedString(Path.Combine(repositoryRoot, "Scripts", "New-ReleaseNotes.ps1"))}' `
                    -Version '{EscapePowerShellSingleQuotedString(version)}' `
                    -ChangelogPath '{EscapePowerShellSingleQuotedString(changelogPath)}' `
                    -AssetNames $assetNames `
                    -ProductName 'Universal Device Toolkit'
                """);

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(wrapperPath);

            using var process = System.Diagnostics.Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PowerShell.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit(30_000).Should().BeTrue("release notes generation should finish quickly");
            process.ExitCode.Should().Be(0, error);

            return output;
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string EscapePowerShellSingleQuotedString(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
    }

    private static string FindRepositoryRoot()
    {
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
