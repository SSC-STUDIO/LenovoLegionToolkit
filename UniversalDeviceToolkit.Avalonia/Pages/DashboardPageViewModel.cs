using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DashboardPageViewModel : ObservableObject
{
    public ObservableCollection<FeatureGroupItem> FeatureGroups { get; } = new();
    public ObservableCollection<SensorReadingItem> SensorReadings { get; } = new();

    public DashboardPageViewModel()
    {
        // Sample feature groups (will be populated from platform services)
        FeatureGroups.Add(new("Power Mode", "System power management", "Active"));
        FeatureGroups.Add(new("Fan Control", "Fan speed management", "Ready"));
        FeatureGroups.Add(new("Display", "Refresh rate control", "Available"));
        FeatureGroups.Add(new("GPU", "GPU management", "N/A"));
        FeatureGroups.Add(new("Battery", "Battery management", "Active"));
        FeatureGroups.Add(new("Keyboard", "Backlight control", "Available"));

        // Sample sensor readings
        SensorReadings.Add(new("CPU Temperature", "65°C"));
        SensorReadings.Add(new("GPU Temperature", "58°C"));
        SensorReadings.Add(new("CPU Usage", "23%"));
        SensorReadings.Add(new("Memory Usage", "8.2 GB / 16 GB"));
        SensorReadings.Add(new("Fan Speed", "2400 RPM"));
        SensorReadings.Add(new("Battery", "87%"));
    }
}

public record FeatureGroupItem(string Title, string Description, string Status);
public record SensorReadingItem(string Name, string DisplayValue);
