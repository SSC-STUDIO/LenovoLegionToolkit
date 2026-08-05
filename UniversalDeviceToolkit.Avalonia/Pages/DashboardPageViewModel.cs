using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Shared.Settings;
#if WINDOWS
using UniversalDeviceToolkit.Lib;
using WpfHardwareSensorSettings = UniversalDeviceToolkit.WPF.Settings.HardwareSensorSettings;
#endif

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DashboardPageViewModel : ObservableObject
{
    public ObservableCollection<FeatureGroupItem> FeatureGroups { get; } = new();
    public ObservableCollection<DashboardGroupViewModel> DashboardGroups { get; } = new();
    public ObservableCollection<DashboardSensorViewModel> SensorReadings { get; } = new();
    public ObservableCollection<DashboardTelemetryCardViewModel> TelemetryCards { get; } =
        new(DashboardTelemetryGroups.CreateDefaults());

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

    [ObservableProperty]
    private bool _isLayoutEditorOpen;

    [ObservableProperty]
    private string _layoutStatusText = string.Empty;

    private readonly AvaloniaDashboardPreferences _dashboardPreferences;
    private bool _showSensors;
    private int _sensorsRefreshIntervalSeconds;
#if WINDOWS
    private readonly WpfHardwareSensorSettings? _hardwareSensorSettings;
#endif

    /// <summary>
    /// Mirrors the WPF dashboard's sensor visibility setting while keeping the
    /// portable Avalonia host independent from WPF-only settings types.
    /// </summary>
    public bool ShowSensors
    {
        get => _showSensors;
        set
        {
            if (!SetProperty(ref _showSensors, value))
                return;

            _dashboardPreferences.Store.ShowSensors = value;
            _dashboardPreferences.SynchronizeStore();
        }
    }

    private readonly IPlatformServices _platformServices;
    private readonly Action<string>? _navigate;
    private CancellationTokenSource? _pollingCancellation;
    private int _refreshVersion;

    public DashboardPageViewModel(
        IPlatformServices platformServices,
        AvaloniaDashboardPreferences? dashboardPreferences = null,
        Action<string>? navigate = null)
    {
        _platformServices = platformServices;
        _navigate = navigate;
        _dashboardPreferences = dashboardPreferences ?? new AvaloniaDashboardPreferences();
#if WINDOWS
        _hardwareSensorSettings = IoCContainer.TryResolve<WpfHardwareSensorSettings>();
#endif
        _showSensors = _dashboardPreferences.Store.ShowSensors;
        _sensorsRefreshIntervalSeconds = NormalizeRefreshInterval(
            _dashboardPreferences.Store.SensorsRefreshIntervalSeconds);
    }

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
            var snapshotTask = _platformServices.GetDashboardSnapshotAsync();
            var layoutTask = _platformServices.GetDashboardLayoutAsync();
            await Task.WhenAll(snapshotTask, layoutTask).ConfigureAwait(false);
            var snapshot = await snapshotTask.ConfigureAwait(false);
            var layout = await layoutTask.ConfigureAwait(false);
            var itemStateTask = _platformServices.GetDashboardItemStatesAsync(
                layout.Groups.SelectMany(group => group.Items).ToArray());
            var itemStates = await itemStateTask.ConfigureAwait(false);
            if (version != Volatile.Read(ref _refreshVersion))
                return;

            DeviceName = snapshot.DeviceName;
            DeviceSupport = snapshot.DeviceSupport;
            PowerStatus = snapshot.PowerStatus;
            LastUpdatedText = snapshot.CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss");

            FeatureGroups.Clear();
            foreach (var group in snapshot.FeatureGroups)
                FeatureGroups.Add(group);

            ApplyDashboardLayout(layout);
            ApplyDashboardItemStates(itemStates);
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

    [RelayCommand]
    private void OpenFeature(FeatureGroupItem? item)
    {
        if (item?.IsNavigable == true && item.RouteKey is not null)
            _navigate?.Invoke(item.RouteKey);
    }

    [RelayCommand]
    private void ToggleLayoutEditor() => IsLayoutEditorOpen = !IsLayoutEditorOpen;

    [RelayCommand]
    private async Task SaveDashboardLayoutAsync()
    {
        var layout = new DashboardLayoutState(
            ShowSensors,
            _sensorsRefreshIntervalSeconds,
            DashboardGroups.Select(group => group.ToState()).ToArray());

        if (await _platformServices.SaveDashboardLayoutAsync(layout).ConfigureAwait(false))
        {
            _dashboardPreferences.Store.ShowSensors = layout.ShowSensors;
            _dashboardPreferences.Store.SensorsRefreshIntervalSeconds = layout.SensorsRefreshIntervalSeconds;
            _dashboardPreferences.Store.Groups = layout.Groups
                .Select(group => new AvaloniaDashboardGroupPreference
                {
                    Type = group.Type,
                    CustomName = group.CustomName,
                    Items = group.Items.ToList(),
                })
                .ToList();
            _dashboardPreferences.SynchronizeStore();
            LayoutStatusText = AvaloniaLocalization.GetString(
                "Dashboard_LayoutSaved",
                "Dashboard layout saved");
        }
        else
        {
            LayoutStatusText = AvaloniaLocalization.GetString(
                "Dashboard_LayoutSaveFailed",
                "Dashboard layout could not be saved");
        }
    }

    [RelayCommand]
    private void RestoreDefaultDashboardLayout()
    {
        ApplyDashboardLayout(new DashboardLayoutState(
            true,
            1,
            AvaloniaDashboardPreferences.CreateDefaultGroups()
                .Select(group => new DashboardGroupState(
                    group.Type,
                    group.CustomName,
                    group.Items.ToArray()))
                .ToArray()));
        LayoutStatusText = AvaloniaLocalization.GetString(
            "Dashboard_LayoutRestored",
            "Default dashboard layout restored. Save to keep it.");
    }

    [RelayCommand]
    private void AddCustomDashboardGroup()
    {
        var index = DashboardGroups.Count(group => group.Type.Equals("Custom", StringComparison.OrdinalIgnoreCase)) + 1;
        DashboardGroups.Add(new DashboardGroupViewModel(
            new DashboardGroupState("Custom", $"Custom {index}", Array.Empty<string>())));
    }

    [RelayCommand]
    private void RemoveDashboardGroup(DashboardGroupViewModel? group)
    {
        if (group is null || !group.Type.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            return;

        DashboardGroups.Remove(group);
    }

    [RelayCommand]
    private void MoveDashboardGroupUp(DashboardGroupViewModel? group)
    {
        if (group is null)
            return;

        var index = DashboardGroups.IndexOf(group);
        if (index > 0)
            DashboardGroups.Move(index, index - 1);
    }

    [RelayCommand]
    private void MoveDashboardGroupDown(DashboardGroupViewModel? group)
    {
        if (group is null)
            return;

        var index = DashboardGroups.IndexOf(group);
        if (index >= 0 && index < DashboardGroups.Count - 1)
            DashboardGroups.Move(index, index + 1);
    }

    [RelayCommand]
    private void MoveDashboardItemUp(DashboardLayoutItemViewModel? item)
    {
        if (item?.Group is null)
            return;

        var index = item.Group.Items.IndexOf(item);
        if (index > 0)
            item.Group.Items.Move(index, index - 1);
    }

    [RelayCommand]
    private void MoveDashboardItemDown(DashboardLayoutItemViewModel? item)
    {
        if (item?.Group is null)
            return;

        var index = item.Group.Items.IndexOf(item);
        if (index >= 0 && index < item.Group.Items.Count - 1)
            item.Group.Items.Move(index, index + 1);
    }

    [RelayCommand]
    private void RemoveDashboardItem(DashboardLayoutItemViewModel? item)
    {
        if (item?.Group is not null)
            item.Group.Items.Remove(item);
    }

    private void ApplyDashboardLayout(DashboardLayoutState layout)
    {
        _showSensors = layout.ShowSensors;
        OnPropertyChanged(nameof(ShowSensors));
        _sensorsRefreshIntervalSeconds = NormalizeRefreshInterval(layout.SensorsRefreshIntervalSeconds);

        DashboardGroups.Clear();
        foreach (var group in layout.Groups)
            DashboardGroups.Add(new DashboardGroupViewModel(group));
    }

    private void ApplyDashboardItemStates(IReadOnlyList<DashboardItemState> states)
    {
        var byIdentifier = states.ToDictionary(
            state => state.Identifier,
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in DashboardGroups.SelectMany(group => group.Items))
        {
            if (byIdentifier.TryGetValue(item.Identifier, out var state))
                item.ApplyState(state);
        }
    }

    [RelayCommand]
    private async Task SetDashboardItemStateAsync(DashboardLayoutItemViewModel? item)
    {
        if (item?.SelectedOption is null || !item.IsAvailable)
            return;

        var state = item.SelectedOption.Value;
        var succeeded = await _platformServices.SetDashboardItemStateAsync(
            item.Identifier,
            state).ConfigureAwait(false);
        item.StateStatusText = succeeded
            ? AvaloniaLocalization.GetString("Dashboard_ItemStateSaved", "Setting applied")
            : AvaloniaLocalization.GetString("Dashboard_ItemStateSaveFailed", "Setting could not be applied");
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadAsync();
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_sensorsRefreshIntervalSeconds));
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await LoadAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void MergeSensors(IReadOnlyList<SensorReadingItem> readings)
    {
        var effectiveReadings = ApplyHardwareSensorPreferences(readings);
        var byName = SensorReadings.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var reading in effectiveReadings)
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

        var grouped = effectiveReadings
            .GroupBy(DashboardTelemetryGroups.Classify, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<SensorReadingItem>)group.ToArray(), StringComparer.OrdinalIgnoreCase);

        foreach (var card in TelemetryCards)
        {
            card.Update(grouped.TryGetValue(card.Key, out var values)
                ? values
                : Array.Empty<SensorReadingItem>());
        }
    }

    private IReadOnlyList<SensorReadingItem> ApplyHardwareSensorPreferences(
        IReadOnlyList<SensorReadingItem> readings)
    {
#if WINDOWS
        var store = _hardwareSensorSettings?.Store;
        var visibleSections = store?.VisibleSections;
        var sectionOrder = store?.SectionOrder;
#else
        IReadOnlyList<string>? visibleSections = null;
        IReadOnlyList<string>? sectionOrder = null;
#endif

        var effectiveReadings = DashboardSensorLayout.FilterAndOrder(
            readings,
            visibleSections,
            sectionOrder);

        var visible = DashboardSensorLayout.NormalizeVisibleSections(visibleSections);
        var orderedCards = DashboardSensorLayout.GetCardOrder(sectionOrder);
        foreach (var card in TelemetryCards)
            card.IsVisible = DashboardSensorLayout.IsCardVisible(card.Key, visible);

        for (var targetIndex = 0; targetIndex < orderedCards.Count; targetIndex++)
        {
            var currentIndex = -1;
            for (var index = 0; index < TelemetryCards.Count; index++)
            {
                if (string.Equals(TelemetryCards[index].Key, orderedCards[targetIndex], StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = index;
                    break;
                }
            }

            if (currentIndex >= 0 && currentIndex != targetIndex)
                TelemetryCards.Move(currentIndex, targetIndex);
        }

        return effectiveReadings;
    }

    private static int NormalizeRefreshInterval(int seconds) => Math.Clamp(seconds, 1, 60);
}

public sealed class DashboardGroupViewModel : ObservableObject
{
    private string? _customName;

    public DashboardGroupViewModel(DashboardGroupState state)
    {
        Type = state.Type;
        _customName = state.CustomName;
        Items = new ObservableCollection<DashboardLayoutItemViewModel>(
            state.Items.Select(item => new DashboardLayoutItemViewModel(this, item)));
    }

    public string Type { get; }
    public bool IsCustom => Type.Equals("Custom", StringComparison.OrdinalIgnoreCase);
    public string DisplayName => IsCustom && !string.IsNullOrWhiteSpace(CustomName)
        ? CustomName!
        : Type switch
        {
            "Power" => AvaloniaLocalization.GetString("DashboardPage_Power_Title", "Power"),
            "Graphics" => AvaloniaLocalization.GetString("DashboardPage_Graphics_Title", "Graphics"),
            "Display" => AvaloniaLocalization.GetString("DashboardPage_Display_Title", "Display"),
            "Other" => AvaloniaLocalization.GetString("DashboardPage_Other_Title", "Other"),
            _ => Type,
        };

    public ObservableCollection<DashboardLayoutItemViewModel> Items { get; }

    public string? CustomName
    {
        get => _customName;
        set
        {
            if (SetProperty(ref _customName, value))
                OnPropertyChanged(nameof(DisplayName));
        }
    }

    public DashboardGroupState ToState() => new(
        Type,
        IsCustom ? CustomName : null,
        Items.Select(item => item.Identifier).ToArray());
}

public sealed record DashboardItemDescriptor(
    string TitleKey,
    string FallbackTitle,
    string IconIdentifier);

public static class DashboardItemDescriptors
{
    private static readonly IReadOnlyDictionary<string, DashboardItemDescriptor> Items =
        new Dictionary<string, DashboardItemDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["PowerMode"] = new("PowerModeControl_Title", "Power mode", "Gauge24"),
            ["ItsMode"] = new("DashboardITSModeControl_Title", "Intelligent thermal system", "Gauge24"),
            ["BatteryMode"] = new("BatteryModeControl_Title", "Battery mode", "BatteryCharge24"),
            ["BatteryNightChargeMode"] = new("BatteryNightChargeModeControl_Title", "Battery night charge", "WeatherMoon24"),
            ["AlwaysOnUsb"] = new("AlwaysOnUSBControl_Title", "Always-on USB", "UsbStick24"),
            ["InstantBoot"] = new("InstantBootControl_Title", "Instant boot", "PlugDisconnected24"),
            ["FlipToStart"] = new("FlipToStartControl_Title", "Flip to start", "Power24"),
            ["HybridMode"] = new("ComboBoxHybridModeControl_Title", "Hybrid graphics", "LeafOne24"),
            ["DiscreteGpu"] = new("DiscreteGPUControl_Title", "Discrete GPU", "DeveloperBoard24"),
            ["OverclockDiscreteGpu"] = new("OverclockDiscreteGPUControl_Title", "Overclock discrete GPU", "DeveloperBoardLightning20"),
            ["Resolution"] = new("ResolutionControl_Title", "Resolution", "ScaleFill24"),
            ["RefreshRate"] = new("RefreshRateControl_Title", "Refresh rate", "DesktopPulse24"),
            ["DpiScale"] = new("DpiScaleControl_Title", "Display scale", "TextFontSize24"),
            ["Hdr"] = new("HDRControl_Title", "HDR", "Hdr24"),
            ["OverDrive"] = new("OverDriveControl_Title", "OverDrive", "TopSpeed24"),
            ["TurnOffMonitors"] = new("TurnOffMonitorsControl_Title", "Turn off monitors", "Desktop24"),
            ["Microphone"] = new("MicrophoneControl_Title", "Microphone", "Mic24"),
            ["WhiteKeyboardBacklight"] = new("WhiteKeyboardBacklightControl_Title", "White keyboard backlight", "Keyboard24"),
            ["PanelLogoBacklight"] = new("PanelLogoBacklightControl_Title", "Panel logo backlight", "LightbulbCircle24"),
            ["PortsBacklight"] = new("PortsBacklightControl_Title", "Ports backlight", "UsbPlug24"),
            ["TouchpadLock"] = new("TouchpadLockControl_Title", "Touchpad lock", "Tablet24"),
            ["FnLock"] = new("FnLockControl_Title", "Fn lock", "Keyboard24"),
            ["WinKeyLock"] = new("WinKeyControl_Title", "Windows key lock", "Keyboard24"),
        };

    public static DashboardItemDescriptor Get(string identifier) =>
        Items.TryGetValue(identifier, out var descriptor)
            ? descriptor
            : new DashboardItemDescriptor(identifier, identifier, "Info24");
}

public sealed record DashboardStateOption(string Value, string DisplayName);

public sealed partial class DashboardLayoutItemViewModel : ObservableObject
{
    public DashboardLayoutItemViewModel(DashboardGroupViewModel group, string identifier)
    {
        Group = group;
        Identifier = identifier;
    }

    public DashboardGroupViewModel Group { get; }
    public string Identifier { get; }
    public DashboardItemDescriptor Descriptor => DashboardItemDescriptors.Get(Identifier);
    public string IconIdentifier => Descriptor.IconIdentifier;
    public string DisplayName => AvaloniaLocalization.GetString(
        Descriptor.TitleKey,
        Descriptor.FallbackTitle);
    public ObservableCollection<DashboardStateOption> Options { get; } = new();

    [ObservableProperty]
    private DashboardStateOption? _selectedOption;

    [ObservableProperty]
    private bool _isAvailable;

    [ObservableProperty]
    private string? _stateError;

    [ObservableProperty]
    private string _stateStatusText = string.Empty;

    public bool HasOptions => Options.Count > 0;

    public void ApplyState(DashboardItemState state)
    {
        IsAvailable = state.IsAvailable;
        StateError = state.ErrorMessage;
        Options.Clear();
        foreach (var value in state.Options.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Options.Add(new DashboardStateOption(
                value,
                GetStateDisplayName(Identifier, value)));
        }

        if (state.CurrentValue is not null
            && Options.All(option => !option.Value.Equals(state.CurrentValue, StringComparison.OrdinalIgnoreCase)))
        {
            Options.Add(new DashboardStateOption(
                state.CurrentValue,
                GetStateDisplayName(Identifier, state.CurrentValue)));
        }

        SelectedOption = Options.FirstOrDefault(option =>
            option.Value.Equals(state.CurrentValue, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(HasOptions));
    }

    private static string GetStateDisplayName(string identifier, string value)
    {
        if (identifier.Equals("PowerMode", StringComparison.OrdinalIgnoreCase))
        {
            return AvaloniaLocalization.GetString(
                $"PowerModeState_{value}",
                Humanize(value));
        }

        return Humanize(value);
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]))
                builder.Append(' ');
            builder.Append(index == 0 ? char.ToUpperInvariant(character) : character);
        }

        return builder.ToString();
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
