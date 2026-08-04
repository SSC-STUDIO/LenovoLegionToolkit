using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DashboardPageViewModel : ObservableObject
{
    public ObservableCollection<FeatureGroupItem> FeatureGroups { get; } = new();
    public ObservableCollection<SensorReadingItem> SensorReadings { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    private readonly IPlatformServices _platformServices;

    public DashboardPageViewModel(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        try
        {
            var featureGroups = await _platformServices.GetFeatureGroupsAsync();
            var sensorReadings = await _platformServices.GetSensorReadingsAsync();

            FeatureGroups.Clear();
            foreach (var group in featureGroups)
                FeatureGroups.Add(group);

            SensorReadings.Clear();
            foreach (var reading in sensorReadings)
                SensorReadings.Add(reading);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard load failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();
}
