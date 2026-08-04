using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DashboardPageViewModel : ObservableObject
{
    public ObservableCollection<FeatureGroupItem> FeatureGroups { get; } = new();
    public ObservableCollection<DashboardSensorViewModel> SensorReadings { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _deviceName = "Unknown device";

    [ObservableProperty]
    private string _deviceSupport = "Not supported";

    [ObservableProperty]
    private string _powerStatus = "Unknown";

    [ObservableProperty]
    private string _lastUpdatedText = string.Empty;

    private readonly IPlatformServices _platformServices;
    private CancellationTokenSource? _pollingCancellation;
    private int _refreshVersion;

    public DashboardPageViewModel(IPlatformServices platformServices) => _platformServices = platformServices;

    public void StartPolling()
    {
        if (_pollingCancellation is not null)
            return;

        _pollingCancellation = new CancellationTokenSource();
        _ = PollAsync(_pollingCancellation.Token);
    }

    public void StopPolling()
    {
        _pollingCancellation?.Cancel();
        _pollingCancellation?.Dispose();
        _pollingCancellation = null;
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        var version = Interlocked.Increment(ref _refreshVersion);
        IsLoading = true;
        try
        {
            var snapshot = await _platformServices.GetDashboardSnapshotAsync();
            if (version != Volatile.Read(ref _refreshVersion))
                return;

            DeviceName = snapshot.DeviceName;
            DeviceSupport = snapshot.DeviceSupport;
            PowerStatus = snapshot.PowerStatus;
            LastUpdatedText = snapshot.CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss");

            FeatureGroups.Clear();
            foreach (var group in snapshot.FeatureGroups)
                FeatureGroups.Add(group);

            MergeSensors(snapshot.SensorReadings);
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

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadAsync();
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await LoadAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void MergeSensors(IReadOnlyList<SensorReadingItem> readings)
    {
        var byName = SensorReadings.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var reading in readings)
        {
            seen.Add(reading.Name);
            if (byName.TryGetValue(reading.Name, out var existing))
            {
                existing.Update(reading);
                continue;
            }

            SensorReadings.Add(new DashboardSensorViewModel(reading));
        }

        for (var index = SensorReadings.Count - 1; index >= 0; index--)
        {
            if (!seen.Contains(SensorReadings[index].Name))
                SensorReadings.RemoveAt(index);
        }
    }
}

public sealed partial class DashboardSensorViewModel : ObservableObject
{
    public string Name { get; private set; }
    public string CategoryLabel { get; private set; }
    public string Unit { get; private set; }
    public ObservableCollection<double> History { get; } = new();

    [ObservableProperty]
    private string _displayValue;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private bool _hasProgress;

    public DashboardSensorViewModel(SensorReadingItem reading)
    {
        Name = reading.Name;
        CategoryLabel = reading.CategoryLabel;
        Unit = reading.Unit;
        _displayValue = reading.DisplayValue;
        Update(reading);
    }

    public void Update(SensorReadingItem reading)
    {
        Name = reading.Name;
        CategoryLabel = reading.CategoryLabel;
        Unit = reading.Unit;
        DisplayValue = reading.DisplayValue;
        HasProgress = reading.HasProgress;
        ProgressPercent = reading.ProgressPercent;
        if (reading.Value is double numeric && double.IsFinite(numeric))
        {
            History.Add(numeric);
            while (History.Count > 30)
                History.RemoveAt(0);
        }
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(CategoryLabel));
        OnPropertyChanged(nameof(Unit));
    }
}
