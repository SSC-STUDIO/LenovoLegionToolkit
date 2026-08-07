using FluentAssertions;
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
}
