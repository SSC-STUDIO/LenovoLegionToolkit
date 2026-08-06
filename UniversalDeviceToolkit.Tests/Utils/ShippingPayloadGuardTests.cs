using FluentAssertions;
using System.Xml.Linq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class ShippingPayloadGuardTests
{
    [Fact]
    public void ShippingPayloadGuard_ShouldRejectTestAndValidationArtifacts()
    {
        var script = ReadRepositoryFile("Scripts", "Assert-ShippingPayload.ps1");

        script.Should().Contain("'UniversalDeviceToolkit.Plugins.SDK.dll'");
        script.Should().Contain("'UniversalDeviceToolkit.Plugins.Shared.Core.dll'");
        script.Should().Contain("'UniversalDeviceToolkit.Plugins.Shared.dll'");
        script.Should().Contain("Shipping payload is missing required plugin runtime files");
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
        script.Should().Contain("'UDT_APPDATA_OVERRIDE'");
        script.Should().Contain("'x86'");
        script.Should().Contain("'arm64'");
        script.Should().Contain("'*.pdb'");
        script.Should().Contain("Test-ContainsBinaryMarker");
        script.Should().Contain("[System.Text.Encoding]::UTF8.GetBytes($Marker)");
        script.Should().Contain("[System.Text.Encoding]::Unicode.GetBytes($Marker)");
        script.Should().Contain("[System.Text.Encoding]::BigEndianUnicode.GetBytes($Marker)");
        script.Should().Contain("[System.IO.File]::ReadAllBytes($Path)");
        script.Should().NotContain("Select-String -LiteralPath $file.FullName -SimpleMatch $marker -Quiet");
    }

    [Fact]
    public void ShippingPayloadGuard_ShouldRejectPayloadMissingPluginRuntime()
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        var payloadRoot = NewTempDirectory("UDT-shipping-missing-runtime");
        File.WriteAllBytes(Path.Combine(payloadRoot, "Universal Device Toolkit.dll"), [0x01]);

        try
        {
            var output = RunPowerShellScript(
                Path.Combine(repositoryRoot, "Scripts", "Assert-ShippingPayload.ps1"),
                ["-PayloadPath", payloadRoot],
                repositoryRoot,
                expectSuccess: false);

            output.Should().Contain("Shipping payload is missing required plugin runtime files:");
            output.Should().Contain("UniversalDeviceToolkit.Plugins.SDK.dll");
            output.Should().Contain("UniversalDeviceToolkit.Plugins.Shared.Core.dll");
            output.Should().Contain("UniversalDeviceToolkit.Plugins.Shared.dll");
        }
        finally
        {
            Directory.Delete(payloadRoot, recursive: true);
        }
    }

    [Fact]
    public void ShippingPayloadGuard_ShouldRejectUtf16BinaryTestHookMarkers()
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        var payloadRoot = NewTempDirectory("UDT-shipping-marker-payload");

        try
        {
            SeedRequiredPluginRuntimeFiles(payloadRoot);
            var markerPath = Path.Combine(payloadRoot, "Universal Device Toolkit.dll");
            File.WriteAllBytes(markerPath, System.Text.Encoding.Unicode.GetBytes("prefix UDT_APPDATA_OVERRIDE suffix"));

            var output = RunPowerShellScript(
                Path.Combine(repositoryRoot, "Scripts", "Assert-ShippingPayload.ps1"),
                ["-PayloadPath", payloadRoot],
                repositoryRoot,
                expectSuccess: false);

            output.Should().Contain("Shipping payload contains test or validation tool artifacts:");
            output.Should().Contain(markerPath);
        }
        finally
        {
            Directory.Delete(payloadRoot, recursive: true);
        }
    }

    [Fact]
    public void ReleaseWorkflow_ShouldValidateAllShippingPayloads()
    {
        var workflow = GitHubWorkflowContract.Parse(
            ReadRepositoryFile(".github", "workflows", "Release.yml"));
        var releaseNotesScript = ReadRepositoryFile("Scripts", "New-ReleaseNotes.ps1");
        var languageAssetsScript = ReadRepositoryFile("Scripts", "Build-LanguageAssets.ps1");
        var crossPlatformScript = ReadRepositoryFile("Scripts", "Build-CrossPlatformCliAsset.ps1");
        var makeScript = ReadRepositoryFile("Make.bat");
        var installerAssetsScript = ReadRepositoryFile("Scripts", "Build-InstallerAssets.ps1");
        var mainAppSmokeWorkflow = ReadRepositoryFile(".github", "workflows", "MainAppPluginUi.Smoke.yml");

        var releaseJob = workflow.Job("build");
        var versionStep = releaseJob.Step("Resolve release version");
        var publishStep = releaseJob.Step("Publish release payload");
        var prepareStep = releaseJob.Step("Prepare release and Pages resources");
        var crossPlatformStep = releaseJob.Step("Publish cross-platform CLI asset");
        var installerStep = releaseJob.Step("Build installers");
        var finalizeStep = releaseJob.Step("Finalize release assets");
        var packageManifestStep = releaseJob.Step("Validate package manifests");
        var releaseNotesStep = releaseJob.Step("Generate release notes");

        versionStep.Run.Should().Contain("SETUP_FULL_ASSET=UniversalDeviceToolkit_v$versionLabel");
        versionStep.Run.Should().Contain("$includeCrossPlatformCli = $majorVersionNumber -ge 5");
        versionStep.Run.Should().Contain("ENABLE_CROSS_PLATFORM_CLI=$($includeCrossPlatformCli.ToString().ToLowerInvariant())");
        publishStep.Run.Should().Contain("./Scripts/Build-PluginRuntimeAssets.ps1");
        publishStep.Run.Should().Contain("./Scripts/Assert-ShippingPayload.ps1 -PayloadPath $env:BUILD_OUTPUT");
        publishStep.Run.Should().Contain("./Scripts/Prune-ShippingFootprint.ps1 -PayloadPath $env:BUILD_OUTPUT");
        publishStep.Run.Should().NotContain("-AllowedCultures 'en;zh-Hans;zh-Hant'");
        prepareStep.Run.Should().Contain("./Scripts/Assert-ShippingPayload.ps1 -PayloadPath $env:ONLINE_BUILD_OUTPUT");
        crossPlatformStep.Run.Should().Contain("./Scripts/Build-CrossPlatformCliAsset.ps1");
        crossPlatformStep.Condition.Should().Be("${{ env.ENABLE_CROSS_PLATFORM_CLI == 'true' }}");
        crossPlatformStep.Run.Should().Contain("-AssetVersion $env:VERSION");
        packageManifestStep.Run.Should().Contain("./Packaging/Prepare-PackageManifests.ps1");
        packageManifestStep.Run.Should().Contain("-HashManifestPath \"$env:RELEASE_OUTPUT\\$env:HASH_ASSET\"");
        installerStep.Run.Should().Contain("./Scripts/Build-InstallerAssets.ps1");
        installerStep.Run.Should().NotContain("iscc");
        installerStep.Run.Should().NotContain("MakeInstaller.iss");
        finalizeStep.Run.Should().Contain("$finalizeArgs = @{");
        releaseNotesStep.Run.Should().Contain("$env:CLI_CROSS_PLATFORM_ASSET");

        installerAssetsScript.Should().Contain("Tools\\Installer\\UniversalDeviceToolkit.Installer.csproj");
        installerAssetsScript.Should().Contain("-p:PayloadZipPath=$fullZipPath");
        installerAssetsScript.Should().Contain("UniversalDeviceToolkitSetup-Full.exe");
        installerAssetsScript.Should().Contain("UniversalDeviceToolkitSetup-Online.exe");
        installerAssetsScript.Should().Contain("Expected installer output was not created");
        finalizeStep.Run.Should().Contain("FinalizeOnly = $true");
        finalizeStep.Run.Should().Contain("Repository = '${{ github.repository }}'");
        finalizeStep.Run.Should().Contain("$finalizeArgs['IncludeCrossPlatformCli'] = $true");
        releaseJob.Steps.Select(step => step.Run ?? string.Empty)
            .Should().NotContain(run => run.Contains("/p:EnableUdtTestHooks=true", StringComparison.Ordinal));

        releaseNotesScript.Should().Contain("Get-CompatibilityLines");
        releaseNotesScript.Should().Contain("if ($hasCrossPlatformCli)");
        releaseNotesScript.Should().Contain("Assert-CrossPlatformCliReleaseAllowed -ReleaseVersion $Version -Names $AssetNames");
        releaseNotesScript.Should().Contain("Cross-platform CLI assets are not published before 5.x.x.");

        crossPlatformScript.Should().Contain("[string]$AssetVersion");
        crossPlatformScript.Should().Contain("$resolvedAssetVersion = if ([string]::IsNullOrWhiteSpace($AssetVersion)) { $Version } else { $AssetVersion }");
        crossPlatformScript.Should().Contain("${AssetPrefix}_v${resolvedAssetVersion}_CLI_cross-platform.zip");
        crossPlatformScript.Should().Contain("Assert-CrossPlatformCliReleaseAllowed -BuildVersion $Version -PublishedVersion $resolvedAssetVersion");
        crossPlatformScript.Should().Contain("Cross-platform CLI assets are not published before 5.x.x.");
        crossPlatformScript.Should().Contain("$shippingPayloadGuard = Resolve-RepoPath 'Scripts\\Assert-ShippingPayload.ps1'");
        crossPlatformScript.Should().Contain("& $shippingPayloadGuard -PayloadPath $publishOutputPath");

        languageAssetsScript.Should().Contain("Get-CrossPlatformCliAssetName");
        languageAssetsScript.Should().Contain("[switch]$IncludeCrossPlatformCli");
        languageAssetsScript.Should().Contain("Assert-CrossPlatformCliReleaseAllowed -ReleaseVersion $Version");
        languageAssetsScript.Should().Contain("Cross-platform CLI assets are not published before 5.x.x.");
        languageAssetsScript.Should().Contain("if ($IncludeCrossPlatformCli)");
        languageAssetsScript.Should().Contain("Universal Device Toolkit.resources.dll");
        languageAssetsScript.Should().Contain("Build all supported satellite cultures before packaging.");
        languageAssetsScript.Should().Contain("if ($IncludeCrossPlatformCli -and (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $crossPlatformCliName)))");
        languageAssetsScript.Should().Contain("$hashAssetNames = @($fullSetupName, $onlineSetupName, $fullZipName, $onlineZipName, $legacySetupName)");
        languageAssetsScript.Should().Contain("$downloads['cli']");

        makeScript.Should().Contain("Scripts\\Build-CrossPlatformCliAsset.ps1");
        makeScript.Should().Contain("ENABLE_CROSS_PLATFORM_CLI");
        makeScript.Should().Contain("IF !VERSION_MAJOR! GEQ 5 SET ENABLE_CROSS_PLATFORM_CLI=1");
        makeScript.Should().Contain("%CROSS_PLATFORM_CLI_FINALIZE_ARG%");
        makeScript.Should().Contain("Scripts\\Build-InstallerAssets.ps1");
        makeScript.Should().NotContain("iscc");
        makeScript.Should().Contain("Expected online installer was not created.");
        makeScript.IndexOf("Scripts\\Build-CrossPlatformCliAsset.ps1", StringComparison.Ordinal)
            .Should()
            .BeLessThan(makeScript.IndexOf("Scripts\\Build-LanguageAssets.ps1\" -FinalizeOnly", StringComparison.Ordinal));

        mainAppSmokeWorkflow.Should().Contain("/p:EnableUdtTestHooks=true");
        mainAppSmokeWorkflow.Should().Contain("dotnet build UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj");
        mainAppSmokeWorkflow.Should().NotContain("dotnet publish UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj");
    }

    [Fact]
    public void LegacyLanguagePublisher_ShouldUseSharedCanonicalCultureNames()
    {
        var script = ReadRepositoryFile("Tools", "Publish-LanguageResources.ps1");

        script.Should().Contain("UniversalDeviceToolkit.Lib.Abstractions\\Localization\\LocalizationCatalog.cs");
        script.Should().Contain("Resolve-CanonicalCulture");
        script.Should().NotContain("$dir.Name.ToLowerInvariant()");
    }

    [Fact]
    public void ShippingAppProjects_ShouldRejectPublishWithTestHooks()
    {
        var directoryTargets = ReadRepositoryFile("Directory.Build.targets");
        directoryTargets.Should().Contain("RejectShippingAppPublishWithTestHooks");
        directoryTargets.Should().Contain("BeforeTargets=\"BeforeBuild;SetGenerateManifests;PrepareForPublish\"");
        directoryTargets.Should().Contain("'$(_IsPublishing)' == 'true'");
        directoryTargets.Should().Contain("'$(IsUdtShippingApp)' == 'true'");
        directoryTargets.Should().Contain("'$(EnableUdtTestHooks)' == 'true'");
        directoryTargets.Should().Contain("'$(AllowUdtTestHookPublish)' != 'true'");

        var wpfProject = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "UniversalDeviceToolkit.WPF.csproj");
        var cliProject = ReadRepositoryFile("UniversalDeviceToolkit.CLI", "UniversalDeviceToolkit.CLI.csproj");

        wpfProject.Should().Contain("<IsUdtShippingApp>true</IsUdtShippingApp>");
        cliProject.Should().Contain("<IsUdtShippingApp>true</IsUdtShippingApp>");
    }

    [Fact]
    public void MainAppProject_ShouldNotReferenceTestOrValidationTools()
    {
        var projectPath = Path.Combine(RepositoryPaths.FindRoot(), "UniversalDeviceToolkit.WPF", "UniversalDeviceToolkit.WPF.csproj");
        var project = XDocument.Load(projectPath);
        var projectReferences = project
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Replace('/', '\\'))
            .ToArray();

        projectReferences.Should().NotContain(reference =>
            reference.Contains("\\Tools\\", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains("\\Tests\\", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains(".Smoke", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains("Validation", StringComparison.OrdinalIgnoreCase),
            "shipping app project references must stay limited to production libraries");
    }

    [Fact]
    public void Repository_ShouldNotContainMainAppDebugPatchScripts()
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        var files = Directory
            .EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredRepositoryPath(repositoryRoot, path))
            .ToArray();

        files.Should().NotContain(
            path => Path.GetFileName(path).Contains("_patch_app_debug", StringComparison.OrdinalIgnoreCase),
            "temporary debug patch scripts must not be checked in");

        files.Should().NotContain(
            path => Path.GetFileName(path).EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) &&
                    File.ReadAllText(path).Contains("AgentDebugLog.Write", StringComparison.Ordinal),
            "scripts must not inject debug logging into the shipping WPF app");
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
                "UniversalDeviceToolkit_v4.2.0_Setup.exe",
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
                "UniversalDeviceToolkit_v5.0.0_Setup.exe",
                "UniversalDeviceToolkit_v5.0.0_SHA256.txt",
            ]);

        notesWithCli.Should().Contain("UniversalDeviceToolkit_v5.0.0_CLI_cross-platform.zip");
        notesWithCli.Should().Contain("Cross-platform diagnostics CLI: Windows, macOS, and Linux with .NET 10 runtime");
    }

    [Fact]
    public void ReleaseScripts_ShouldRejectCrossPlatformCliAssetsBeforeFive()
    {
        var releaseNotesFailure = RunReleaseNotesScript(
            "4.2.0",
            [
                "UniversalDeviceToolkit_v4.2.0_CLI_cross-platform.zip",
            ],
            expectSuccess: false);

        releaseNotesFailure.Should().Contain("Cross-platform CLI assets are not published before 5.x.x.");

        var repositoryRoot = RepositoryPaths.FindRoot();
        RunPowerShellScript(
                Path.Combine(repositoryRoot, "Scripts", "Build-LanguageAssets.ps1"),
                [
                    "-FinalizeOnly",
                    "-ReleaseOutput", NewTempDirectory("UDT-release-output"),
                    "-PagesOutput", NewTempDirectory("UDT-pages-output"),
                    "-Version", "4.2.0",
                    "-FullInstallerPath", NewTempFile("UDT-full", ".exe"),
                    "-OnlineInstallerPath", NewTempFile("UDT-online", ".exe"),
                    "-IncludeCrossPlatformCli",
                ],
                repositoryRoot,
                expectSuccess: false)
            .Should()
            .Contain("Cross-platform CLI assets are not published before 5.x.x.");

        RunPowerShellScript(
                Path.Combine(repositoryRoot, "Scripts", "Build-CrossPlatformCliAsset.ps1"),
                [
                    "-Version", "4.2.0",
                    "-ReleaseOutput", NewTempDirectory("UDT-cli-output"),
                    "-SkipHashUpdate",
                ],
                repositoryRoot,
                expectSuccess: false)
            .Should()
            .Contain("Cross-platform CLI assets are not published before 5.x.x.");
    }

    private static string RunReleaseNotesScript(string version, string[] assetNames, bool expectSuccess = true)
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
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
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
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

    private static void SeedRequiredPluginRuntimeFiles(string payloadRoot)
    {
        File.WriteAllBytes(Path.Combine(payloadRoot, "UniversalDeviceToolkit.Plugins.Shared.Core.dll"), [0x00]);
        File.WriteAllBytes(Path.Combine(payloadRoot, "UniversalDeviceToolkit.Plugins.SDK.dll"), [0x01]);
        File.WriteAllBytes(Path.Combine(payloadRoot, "UniversalDeviceToolkit.Plugins.Shared.dll"), [0x02]);
    }

    private static string NewTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string NewTempFile(string prefix, string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, []);
        return path;
    }

    private static string EscapePowerShellSingleQuotedString(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static bool IsIgnoredRepositoryPath(string repositoryRoot, string path)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, path);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Any(static segment =>
            segment.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
    }

}
