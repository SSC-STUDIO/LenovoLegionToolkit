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
