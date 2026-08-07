using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class DriverDownloadMigrationContractTests
{
    [Fact]
    public void DriverDownloadPage_PreservesWpfQueueAndRecoverySurface()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DriverDownloadPage.axaml"));
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DriverDownloadPage.axaml.cs"));

        markup.Should().Contain("AvaloniaDriverDownloadPath");
        markup.Should().Contain("AvaloniaDriverSelectRecommended");
        markup.Should().Contain("AvaloniaDriverStartPause");
        markup.Should().Contain("AvaloniaDriverRestoreHidden");
        source.Should().Contain("SetDriverDownloadPathAsync");
        source.Should().Contain("SetSelectedDriverPackagesAsync");
        source.Should().Contain("SelectRecommendedDriverPackagesAsync");
        source.Should().Contain("StartSelectedDriverPackagesAsync");
        source.Should().Contain("PauseDriverDownloadsAsync");
        source.Should().Contain("HideDriverPackagesAsync");
        source.Should().Contain("RestoreHiddenDriverPackagesAsync");
        source.Should().Contain("DriverPackageStatus.Downloading");
        source.Should().Contain("OpenDownloadPathButton_Click");
    }

    [Fact]
    public void DriverDownloadHost_PreservesWpfStatefulQueueAndPersistenceContracts()
    {
        var root = RepositoryPaths.FindRoot();
        var contract = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "IPlatformServices.cs"));
        var host = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));

        contract.Should().Contain("SetDriverDownloadPathAsync");
        contract.Should().Contain("SetSelectedDriverPackagesAsync");
        contract.Should().Contain("SelectRecommendedDriverPackagesAsync");
        contract.Should().Contain("StartSelectedDriverPackagesAsync");
        contract.Should().Contain("PauseDriverDownloadsAsync");
        contract.Should().Contain("HideDriverPackagesAsync");
        contract.Should().Contain("RestoreHiddenDriverPackagesAsync");
        contract.Should().Contain("DriverPackageStatus");
        contract.Should().Contain("HiddenPackageCount");

        host.Should().Contain("_driverPackageStates");
        host.Should().Contain("ProcessDriverDownloadQueueAsync");
        host.Should().Contain("DownloadPackageFileAsync");
        host.Should().Contain("_packageDownloaderSettings.Store.DownloadPath");
        host.Should().Contain("_packageDownloaderSettings.Store.HiddenPackages");
        host.Should().Contain("package.IsUpdate,");
    }

    [Fact]
    public void DriverDownloadContracts_DefaultToSafeQueueState()
    {
        var package = new DriverPackageItem(
            "driver-id",
            "Driver",
            "Description",
            "1.0",
            "Category",
            "10 MB",
            true,
            true);
        var state = new DriverDownloadState(
            true,
            false,
            "82XX",
            "Windows11",
            "Vantage",
            [package]);

        package.IsSelected.Should().BeFalse();
        package.Status.Should().Be(DriverPackageStatus.NotStarted);
        package.Progress.Should().Be(0);
        state.DownloadPath.Should().BeEmpty();
        state.HiddenPackageCount.Should().Be(0);
        state.IsQueueRunning.Should().BeFalse();
    }

    [Fact]
    public void DriverDownloadPage_AutoDefaultsOperatingSystemToCurrentOs()
    {
        var operatingSystems = new[] { "Windows11", "Windows10", "Windows8", "Windows7" };

        DriverDownloadPage.ResolveDefaultOperatingSystem("Windows10", operatingSystems)
            .Should().Be("Windows10");
        DriverDownloadPage.ResolveDefaultOperatingSystem("windows11", operatingSystems)
            .Should().Be("Windows11");
        DriverDownloadPage.ResolveDefaultOperatingSystem(null, operatingSystems)
            .Should().Be("Windows11");
        DriverDownloadPage.ResolveDefaultOperatingSystem("WindowsXP", operatingSystems)
            .Should().Be("Windows11");
        DriverDownloadPage.ResolveDefaultOperatingSystem("Windows10", [])
            .Should().BeEmpty();
    }

    [Fact]
    public void DriverDownloadPage_RequiresRescanConfirmationOnlyWhileDownloadsRun()
    {
        var idle = new DriverPackageItem(
            "driver-id",
            "Driver",
            "Description",
            "1.0",
            "Category",
            "10 MB",
            true,
            true);
        var downloading = idle with { Status = DriverPackageStatus.Downloading };
        var queued = idle with { Status = DriverPackageStatus.Queued };
        var paused = idle with { Status = DriverPackageStatus.Paused };

        DriverDownloadPage.IsDriverDownloadRunning([idle, paused]).Should().BeFalse();
        DriverDownloadPage.IsDriverDownloadRunning([queued]).Should().BeTrue();
        DriverDownloadPage.IsDriverDownloadRunning([downloading]).Should().BeTrue();
        DriverDownloadPage.IsDriverDownloadRunning([]).Should().BeFalse();
    }

    [Fact]
    public void DriverDownloadPage_DecidesPerPackagePauseResumeFromStatus()
    {
        DriverDownloadPage.GetPackagePauseResumeAction(DriverPackageStatus.Downloading)
            .Should().Be(DriverPackageAction.Pause);
        DriverDownloadPage.GetPackagePauseResumeAction(DriverPackageStatus.Queued)
            .Should().Be(DriverPackageAction.Pause);
        DriverDownloadPage.GetPackagePauseResumeAction(DriverPackageStatus.Paused)
            .Should().Be(DriverPackageAction.Resume);
        DriverDownloadPage.GetPackagePauseResumeAction(DriverPackageStatus.NotStarted)
            .Should().Be(DriverPackageAction.None);
        DriverDownloadPage.GetPackagePauseResumeAction(DriverPackageStatus.Completed)
            .Should().Be(DriverPackageAction.None);
        DriverDownloadPage.GetPackagePauseResumeAction(DriverPackageStatus.Failed)
            .Should().Be(DriverPackageAction.None);
    }

    [Fact]
    public void DriverDownloadPage_PreservesWpfPauseResumeAndScanInterruptionSurface()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DriverDownloadPage.axaml.cs"));

        source.Should().Contain("GetPackagePauseResumeAction");
        source.Should().Contain("AvaloniaDriverPause_");
        source.Should().Contain("PauseDriverDownloadsAsync");
        source.Should().Contain("StartSelectedDriverPackagesAsync");
        source.Should().Contain("ConfirmScanInterruptionAsync");
        source.Should().Contain("DriverScanInterruptWindow");
        source.Should().Contain("GetMachineInformationAsync");
        source.Should().Contain("OSExtensions.GetCurrent()");
        source.Should().Contain("ResolveDefaultOperatingSystem");
        source.Should().Contain("IsDriverDownloadRunning");
    }
}
