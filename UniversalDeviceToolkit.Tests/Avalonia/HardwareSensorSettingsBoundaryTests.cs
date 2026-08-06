using FluentAssertions;
using UniversalDeviceToolkit.Lib.Settings;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class HardwareSensorSettingsBoundaryTests
{
    [Fact]
    public void SharedStore_PreservesTheWpfFileContractAndDefaults()
    {
        var settings = new HardwareSensorSettings();

        settings.Store.VisibleSections.Should().Equal("CPU", "Battery", "GPU");
        settings.Store.SectionOrder.Should().Equal("CPU", "Battery", "GPU");
        settings.Store.SelectedGpuIsIgpu.Should().BeFalse();
        settings.Store.ShowCpuAverageFrequency.Should().BeFalse();
        settings.Store.DisplayMemoryInGigabytes.Should().BeFalse();
    }

    [Fact]
    public void WpfCompatibilityType_UsesTheSharedSettingsContract()
    {
        typeof(UniversalDeviceToolkit.WPF.Settings.HardwareSensorSettings)
            .Should().BeDerivedFrom<HardwareSensorSettings>();
    }

    [Fact]
    public void AvaloniaWindowsServices_DoNotReferenceWpfSensorSettings()
    {
        var root = RepositoryPaths.FindRoot();
        var files = new[]
        {
            Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "DashboardPageViewModel.cs"),
            Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Services", "WindowsAvaloniaSettingsService.cs"),
            Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Services", "WindowsFeatureHostServices.cs"),
        };

        foreach (var file in files)
            File.ReadAllText(file).Should().NotContain("UniversalDeviceToolkit.WPF.Settings.HardwareSensorSettings");
    }
}
