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
        script.Should().Contain("--win $Target");
        script.Should().Contain("AllowedCultures 'en'");
        script.Should().Contain("*.nsis.7z");
        script.Should().Contain("nsis-web package (*.nsis.7z) not found");
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
