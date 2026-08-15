using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
public sealed class PackagingGuardTests
{
    [Fact]
    public void ElectronBuilder_ShouldShipNsisAndNsisWebWithMaximumCompression()
    {
        var config = RepositoryPaths.ReadFile("UniversalDeviceToolkit.Electron", "electron-builder.yml");

        config.Should().Contain("compression: maximum");
        config.Should().Contain("target: nsis");
        config.Should().Contain("target: nsis-web");
        config.Should().Contain("UniversalDeviceToolkitOnlineSetup-${version}.${ext}");
        config.Should().Contain("provider: github");
    }

    [Fact]
    public void ElectronInstallerScript_ShouldAssertOnlineStubSizeAndSplitTargets()
    {
        var script = RepositoryPaths.ReadFile("Scripts", "Build-ElectronInstaller.ps1");

        script.Should().Contain("15MB");
        script.Should().Contain("UniversalDeviceToolkitOnlineSetup.exe");
        script.Should().Contain("'--win', $Target");
        script.Should().Contain("AllowedCultures 'en'");
        script.Should().Contain("*.nsis.7z");
        script.Should().Contain("nsis-web package (*.nsis.7z) not found");
        script.Should().Contain("--prepackaged");
        script.Should().Contain("PackagePreparedPayloads");
        script.Should().Contain("PrepareInstallerShellOnly");
        script.Should().Contain("--prepackaged $installerShellDir");
        script.Should().Contain("ELECTRON_BUILDER_NSIS_DIR");
        script.Should().Contain("stage-nsis-toolset.mjs");
        script.Should().Contain("UTF8Encoding($false)");
        script.Should().NotContain("Set-Content -LiteralPath $packageJsonPath -Encoding utf8");
        script.Should().NotContain("-Target 'nsis' -PrepackagedPath $fullPayloadDir");
    }

    [Fact]
    public void ReleaseWorkflow_ShouldPublishDistinctFullAndOnlineInstallers()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Release.yml");

        workflow.Should().Contain("UniversalDeviceToolkitOnlineSetup.exe");
        workflow.Should().Contain("UniversalDeviceToolkitSetup.exe");
        workflow.Should().Contain("*.nsis.7z");
        workflow.Should().Contain("Prune-ShippingFootprint.ps1");
        workflow.Should().Contain("UniversalDeviceToolkit.Host/publish/win-x64");
        workflow.Should().NotContain("OnlineInstallerPath = \"$env:INSTALLER_OUTPUT\\UniversalDeviceToolkitSetup.exe\"");
    }

    [Fact]
    public void FootprintPackaging_ShouldKeepOnlyRequiredChromiumLocalesAndAuditEveryPackage()
    {
        var builderConfig = RepositoryPaths.ReadFile("UniversalDeviceToolkit.Electron", "electron-builder.yml");
        var installerConfig = RepositoryPaths.ReadFile("UniversalDeviceToolkit.Electron", "custom-installer.yml");
        var auditScript = RepositoryPaths.ReadFile("UniversalDeviceToolkit.Electron", "scripts", "package-footprint.mjs");
        var hostProject = RepositoryPaths.ReadFile("UniversalDeviceToolkit.Host", "UniversalDeviceToolkit.Host.csproj");

        builderConfig.Should().Contain("afterPack: ./scripts/package-footprint.mjs");
        builderConfig.Should().Contain("electronLanguages:");
        builderConfig.Should().Contain("- zh-TW");
        builderConfig.Should().Contain("- pt-PT");
        installerConfig.Should().Contain("electronLanguages:");
        installerConfig.Should().Contain("- en-US");
        auditScript.Should().Contain("app.asar contains node_modules entries");
        auditScript.Should().Contain("Host contains PDB files");
        auditScript.Should().Contain("Chromium locale mismatch");
        hostProject.Should().Contain("<IsUdtShippingApp>true</IsUdtShippingApp>");
    }

    [Fact]
    public void FootprintWorkflow_ShouldBuildNativePackagesOnSupportedRunners()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "package-footprint.yml");
        var crossPlatformCliWorkflow = RepositoryPaths.ReadFile(".github", "workflows", "CrossPlatformCli.yml");

        workflow.Should().Contain("windows-2022");
        workflow.Should().Contain("ubuntu-24.04");
        workflow.Should().Contain("macos-15");
        workflow.Should().Contain("macos-15-intel");
        workflow.Should().Contain("npm ci");
        workflow.Should().Contain("smoke-host.mjs");
        workflow.Should().Contain("package-footprint.mjs");
        crossPlatformCliWorkflow.Should().Contain("os: macos-15");
        crossPlatformCliWorkflow.Should().Contain("os: macos-15-intel");
        crossPlatformCliWorkflow.Should().NotContain("os: macos-13");
    }

    [Fact]
    public void ElectronInstaller_ShouldPersistLanguageAndDeviceSelectionBeforeFirstLaunch()
    {
        var script = RepositoryPaths.ReadFile("UniversalDeviceToolkit.Electron", "buildResources", "installer.nsh");

        script.Should().Contain("customPageAfterChangeDir");
        script.Should().Contain("Page custom UdtLanguagePage UdtLanguageLeave");
        script.Should().Contain("Page custom UdtDevicePage UdtDeviceLeave");
        script.Should().Contain("$INSTDIR\\installer-selection.ini");
        script.Should().Contain("WriteINIStr");
        script.Should().Contain("deviceMode");
    }
}
