using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Groups the adapter's raw readings into the three dashboard surfaces used by
/// the WPF SensorsControl. The adapter remains read-only; this type only owns
/// presentation state and short-lived history.
/// </summary>
public sealed partial class DashboardTelemetryCardViewModel : ObservableObject
{
    private const int HistoryCapacity = 30;

    public string Key { get; }
    public string Title { get; }
    public string Description { get; }
    public string IconIdentifier { get; }
    public ObservableCollection<DashboardSensorViewModel> Metrics { get; } = new();
    public ObservableCollection<double> History { get; } = new();

    [ObservableProperty]
    private bool _isAvailable;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private string _primaryValue;

    [ObservableProperty]
    private bool _hasPrimaryProgress;

    [ObservableProperty]
    private double _primaryProgressPercent;

    public bool IsUnavailable => !IsAvailable;

    public DashboardTelemetryCardViewModel(
        string key,
        string title,
        string description,
        string iconIdentifier)
    {
        Key = key;
        Title = title;
        Description = description;
        IconIdentifier = iconIdentifier;
        _statusText = NoTelemetryText;
        _primaryValue = NoTelemetryText;
    }

    private static string NoTelemetryText =>
        AvaloniaLocalization.GetString("Dashboard_Status_NoTelemetry", "No telemetry available");

    public void Update(IReadOnlyList<SensorReadingItem> readings)
    {
        IsAvailable = readings.Count > 0;
        StatusText = IsAvailable
            ? AvaloniaLocalization.GetString("Dashboard_Status_Live", "Live")
            : NoTelemetryText;

        var existing = Metrics.ToDictionary(metric => metric.Name, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reading in readings)
        {
            seen.Add(reading.Name);
            if (existing.TryGetValue(reading.Name, out var metric))
                metric.Update(reading);
            else
                Metrics.Add(new DashboardSensorViewModel(reading));
        }

        for (var index = Metrics.Count - 1; index >= 0; index--)
        {
            if (!seen.Contains(Metrics[index].Name))
                Metrics.RemoveAt(index);
        }

        var primary = SelectPrimary(readings);
        if (primary is null)
        {
            PrimaryValue = NoTelemetryText;
            HasPrimaryProgress = false;
            PrimaryProgressPercent = 0;
            OnPropertyChanged(nameof(IsUnavailable));
            return;
        }

        PrimaryValue = primary.DisplayValue;
        HasPrimaryProgress = primary.HasProgress;
        PrimaryProgressPercent = primary.ProgressPercent;
        if (primary.Value is double numeric && double.IsFinite(numeric))
        {
            History.Add(numeric);
            while (History.Count > HistoryCapacity)
                History.RemoveAt(0);
        }

        OnPropertyChanged(nameof(IsUnavailable));
    }

    private static SensorReadingItem? SelectPrimary(IReadOnlyList<SensorReadingItem> readings) =>
        readings.FirstOrDefault(reading => reading.HasProgress)
        ?? readings.FirstOrDefault();
}

/// <summary>
/// Applies the WPF sensor-section contract to the Avalonia dashboard without
/// coupling the portable host to WPF settings types.
/// </summary>
public static class DashboardSensorLayout
{
    private static readonly string[] DefaultSections = ["CPU", "Battery", "GPU"];

    public static IReadOnlyList<string> NormalizeVisibleSections(IReadOnlyList<string>? values)
    {
        var selected = (values ?? [])
            .Where(value => DefaultSections.Contains(value, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return selected.Length == 0 ? DefaultSections : selected;
    }

    public static IReadOnlyList<string> NormalizeSectionOrder(IReadOnlyList<string>? values)
    {
        var normalized = (values ?? [])
            .Where(value => DefaultSections.Contains(value, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var section in DefaultSections)
        {
            if (!normalized.Contains(section, StringComparer.OrdinalIgnoreCase))
                normalized.Add(section);
        }

        return normalized;
    }

    public static IReadOnlyList<string> GetCardOrder(IReadOnlyList<string>? sectionOrder)
    {
        var order = NormalizeSectionOrder(sectionOrder)
            .Select(section => section.ToLowerInvariant())
            .ToList();
        order.Add("system");
        return order;
    }

    public static bool IsCardVisible(string cardKey, IReadOnlyList<string> visibleSections)
    {
        if (cardKey.Equals("system", StringComparison.OrdinalIgnoreCase))
            return true;

        var section = cardKey switch
        {
            "cpu" => "CPU",
            "battery" => "Battery",
            "gpu" => "GPU",
            _ => null,
        };
        return section is null || visibleSections.Contains(section, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<SensorReadingItem> FilterAndOrder(
        IReadOnlyList<SensorReadingItem> readings,
        IReadOnlyList<string>? visibleSections,
        IReadOnlyList<string>? sectionOrder)
    {
        var visible = NormalizeVisibleSections(visibleSections);
        var order = NormalizeSectionOrder(sectionOrder);
        var orderMap = order
            .Select((section, index) => (section, index))
            .ToDictionary(item => item.section, item => item.index, StringComparer.OrdinalIgnoreCase);

        return readings
            .Select((reading, index) => (reading, index, section: GetSection(reading)))
            .Where(item => item.section is null || visible.Contains(item.section, StringComparer.OrdinalIgnoreCase))
            .OrderBy(item => item.section is not null && orderMap.TryGetValue(item.section, out var position)
                ? position
                : order.Count)
            .ThenBy(item => item.index)
            .Select(item => item.reading)
            .ToArray();
    }

    public static string? GetSection(SensorReadingItem reading)
    {
        var text = $"{reading.Category} {reading.Name}";
        if (text.Contains("battery", StringComparison.OrdinalIgnoreCase)
            || text.Contains("charge", StringComparison.OrdinalIgnoreCase))
            return "Battery";
        if (text.Contains("gpu", StringComparison.OrdinalIgnoreCase)
            || text.Contains("graphics", StringComparison.OrdinalIgnoreCase)
            || text.Contains("video", StringComparison.OrdinalIgnoreCase))
            return "GPU";
        if (text.Contains("cpu", StringComparison.OrdinalIgnoreCase)
            || text.Contains("processor", StringComparison.OrdinalIgnoreCase)
            || text.Contains("core", StringComparison.OrdinalIgnoreCase)
            || text.Contains("package", StringComparison.OrdinalIgnoreCase))
            return "CPU";
        return null;
    }
}

public static class DashboardTelemetryGroups
{
    public static IReadOnlyList<DashboardTelemetryCardViewModel> CreateDefaults()
    {
        return
        [
            new(
                "cpu",
                Get("Dashboard_Telemetry_CPU", "CPU"),
                Get("Dashboard_Telemetry_CPUDescription", "Processor usage and temperature"),
                "Desktop24"),
            new(
                "gpu",
                Get("Dashboard_Telemetry_GPU", "GPU"),
                Get("Dashboard_Telemetry_GPUDescription", "Graphics processor usage and temperature"),
                "Gauge24"),
            new(
                "battery",
                Get("Dashboard_Telemetry_Battery", "Battery"),
                Get("Dashboard_Telemetry_BatteryDescription", "Charge and battery health"),
                "Battery024"),
            new(
                "system",
                Get("Dashboard_Telemetry_System", "System"),
                Get("Dashboard_Telemetry_SystemDescription", "Additional platform telemetry"),
                "Desktop24"),
        ];
    }

    public static string Classify(SensorReadingItem reading)
    {
        var text = $"{reading.Category} {reading.Name}";
        if (text.Contains("battery", StringComparison.OrdinalIgnoreCase)
            || text.Contains("charge", StringComparison.OrdinalIgnoreCase))
            return "battery";

        if (text.Contains("gpu", StringComparison.OrdinalIgnoreCase)
            || text.Contains("graphics", StringComparison.OrdinalIgnoreCase)
            || text.Contains("video", StringComparison.OrdinalIgnoreCase))
            return "gpu";

        if (text.Contains("cpu", StringComparison.OrdinalIgnoreCase)
            || text.Contains("processor", StringComparison.OrdinalIgnoreCase)
            || text.Contains("core", StringComparison.OrdinalIgnoreCase)
            || text.Contains("package", StringComparison.OrdinalIgnoreCase))
            return "cpu";

        return "system";
    }

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);
}
