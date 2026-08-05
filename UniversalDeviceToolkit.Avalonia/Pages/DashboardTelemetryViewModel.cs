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
    public ObservableCollection<DashboardSensorDetailViewModel> Details { get; } = new();
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

    [ObservableProperty]
    private bool _isDetailsExpanded;

    [ObservableProperty]
    private string _detailsStatusText = string.Empty;

    public bool IsUnavailable => !IsAvailable;
    public bool HasDetails => Details.Count > 0;
    public bool CanShowDetails => IsAvailable;
    public bool HasDetailsStatus => !string.IsNullOrWhiteSpace(DetailsStatusText);

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

    partial void OnIsAvailableChanged(bool value) =>
        OnPropertyChanged(nameof(CanShowDetails));

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

    public void UpdateDetails(SensorDetailsSnapshot snapshot)
    {
        Details.Clear();

        if (!snapshot.IsAvailable)
        {
            DetailsStatusText = AvaloniaLocalization.GetString(
                "Dashboard_Status_NoTelemetry",
                "No detailed telemetry available");
            IsDetailsExpanded = false;
            OnPropertyChanged(nameof(HasDetails));
            OnPropertyChanged(nameof(HasDetailsStatus));
            return;
        }

        DetailsStatusText = string.Empty;

        switch (Key)
        {
            case "cpu":
                AddDetail("SensorsControl_CpuPower_Label", "CPU Power", FormatPower(snapshot.CpuPowerWatts));
                AddDetail("SensorsControl_CpuCoresPower_Label", "Cores", FormatPower(snapshot.CpuCoresPowerWatts));
                AddDetail("SensorsControl_CpuMemoryPower_Label", "Memory", FormatPower(snapshot.CpuMemoryPowerWatts));
                AddDetail("SensorsControl_CpuPlatformPower_Label", "Platform", FormatPower(snapshot.CpuPlatformPowerWatts));
                AddDetail("SensorsControl_Voltage", "Voltage", FormatVoltage(snapshot.CpuVoltageVolts));
                AddDetail("SensorsControl_PCoreClock_Title", "P Core Clock", FormatFrequency(snapshot.CpuPCoreClockMHz));
                AddDetail("SensorsControl_ECoreClock_Title", "E Core Clock", FormatFrequency(snapshot.CpuECoreClockMHz));
                AddDetail("SensorsControl_MemoryUsage_Title", "Memory Usage", FormatMemory(snapshot.CpuMemoryUsedGb, snapshot.CpuMemoryTotalGb, snapshot.CpuMemoryUsagePercent));
                AddDetail("SensorsControl_MemoryTemperature_Title", "Memory Temperature", FormatTemperature(snapshot.CpuMemoryTemperatureCelsius));
                AddDetail("SensorsControl_SsdTemperature_Title", "SSD Temperature", FormatTemperaturePair(snapshot.CpuSsdTemperature1Celsius, snapshot.CpuSsdTemperature2Celsius));
                AddDetail("SensorsControl_Temperature_Title", "Temperature range", FormatRange(snapshot.CpuTemperatureMinimumCelsius, snapshot.CpuTemperatureMaximumCelsius, "°C"));
                AddDetail("SensorsControl_VoltageRange", "Voltage range", FormatRange(snapshot.CpuVoltageMinimumVolts, snapshot.CpuVoltageMaximumVolts, "V"));
                break;
            case "gpu":
                AddDetail("SensorsControl_GpuMemoryClock_Title", "Memory Clock", FormatFrequency(snapshot.GpuMemoryClockMHz));
                AddDetail("SensorsControl_Power", "Power", FormatPower(snapshot.GpuPowerWatts));
                AddDetail("SensorsControl_Voltage", "Voltage", FormatVoltage(snapshot.GpuVoltageVolts));
                AddDetail(
                    "SensorsControl_VramUsage_Title",
                    snapshot.IsIntegratedGpu ? "Shared Memory Usage" : "VRAM Usage",
                    FormatMemory(snapshot.GpuVramUsedGb, snapshot.GpuVramTotalGb, snapshot.GpuVramUsagePercent));
                AddDetail("SensorsControl_VramTemperature_Title", "VRAM Temperature", FormatTemperature(snapshot.GpuVramTemperatureCelsius));
                AddDetail("SensorsControl_GpuHotSpotTemperature_Title", "GPU Hot Spot", FormatTemperature(snapshot.GpuHotSpotTemperatureCelsius));
                AddDetail("SensorsControl_GpuPcieThroughput_Title", "PCIe Throughput", FormatThroughput(snapshot.GpuPcieRxBytesPerSecond, snapshot.GpuPcieTxBytesPerSecond));
                AddDetail("SensorsControl_Temperature_Title", "Temperature range", FormatRange(snapshot.GpuTemperatureMinimumCelsius, snapshot.GpuTemperatureMaximumCelsius, "°C"));
                AddDetail("SensorsControl_VoltageRange", "Voltage range", FormatRange(snapshot.GpuVoltageMinimumVolts, snapshot.GpuVoltageMaximumVolts, "V"));
                break;
            case "battery":
                AddDetail("Dashboard_Sensor_BatteryHealth", "Battery health", FormatPercent(snapshot.BatteryHealthPercent));
                AddDetail("Dashboard_Sensor_DischargeRate", "Discharge rate", FormatPower(snapshot.BatteryRateWatts));
                AddDetail("Dashboard_Sensor_ChargeCapacity", "Charge capacity", FormatUnit(snapshot.BatteryChargeCapacityWh, "Wh"));
                AddDetail("Dashboard_Sensor_FullCapacity", "Full capacity", FormatUnit(snapshot.BatteryFullCapacityWh, "Wh"));
                AddDetail("Dashboard_Sensor_CycleCount", "Cycle count", FormatUnit(snapshot.BatteryCycleCount, string.Empty));
                AddDetail("SensorsControl_MemoryTemperature_Title", "Battery temperature", FormatTemperature(snapshot.BatteryTemperatureCelsius));
                break;
        }

        OnPropertyChanged(nameof(HasDetails));
        OnPropertyChanged(nameof(CanShowDetails));
        if (!HasDetails)
            DetailsStatusText = AvaloniaLocalization.GetString(
                "Dashboard_Status_NoTelemetry",
                "No detailed telemetry available");
        OnPropertyChanged(nameof(HasDetailsStatus));
    }

    private void AddDetail(string key, string fallback, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        Details.Add(new DashboardSensorDetailViewModel(
            AvaloniaLocalization.GetString(key, fallback),
            value));
    }

    private static string? FormatPower(double? value) => FormatUnit(value, "W", "0.#");
    private static string? FormatVoltage(double? value) => FormatUnit(value, "V", "0.000");
    private static string? FormatFrequency(double? value) => value is > 0
        ? $"{value.Value / 1000d:0.0} GHz"
        : null;
    private static string? FormatPercent(double? value) => value is >= 0
        ? $"{value.Value:0.#}%"
        : null;
    private static string? FormatTemperature(double? value) => FormatUnit(value, "°C", "0");
    private static string? FormatUnit(double? value, string unit, string format = "0.#") => value is { } number && double.IsFinite(number)
        ? $"{number.ToString(format, System.Globalization.CultureInfo.CurrentCulture)}{(string.IsNullOrEmpty(unit) ? string.Empty : $" {unit}")}"
        : null;
    private static string? FormatMemory(double? used, double? total, double? percent)
    {
        if (used is not { } usedValue || total is not { } totalValue)
            return FormatPercent(percent);

        var suffix = percent is { } percentValue ? $" ({percentValue:0.#}%)" : string.Empty;
        return $"{usedValue:0.0} / {totalValue:0.0} GB{suffix}";
    }
    private static string? FormatTemperaturePair(double? first, double? second) => (FormatTemperature(first), FormatTemperature(second)) switch
    {
        ({ } left, { } right) => $"{left} / {right}",
        ({ } left, null) => left,
        (null, { } right) => right,
        _ => null,
    };
    private static string? FormatRange(double? minimum, double? maximum, string unit) => (minimum, maximum) switch
    {
        ({ } min, { } max) => $"{min:0.###} - {max:0.###} {unit}",
        _ => null,
    };
    private static string? FormatThroughput(double? rx, double? tx)
    {
        var left = FormatRate(rx);
        var right = FormatRate(tx);
        return (left, right) switch
        {
            ({ } a, { } b) => $"Rx {a}\nTx {b}",
            ({ } a, null) => $"Rx {a}",
            (null, { } b) => $"Tx {b}",
            _ => null,
        };
    }
    private static string? FormatRate(double? bytesPerSecond)
    {
        if (bytesPerSecond is not { } value || value < 0 || !double.IsFinite(value))
            return null;
        const double kb = 1024d;
        const double mb = kb * 1024d;
        const double gb = mb * 1024d;
        return value switch
        {
            >= gb => $"{value / gb:0.00} GB/s",
            >= mb => $"{value / mb:0.00} MB/s",
            >= kb => $"{value / kb:0.00} KB/s",
            _ => $"{value:0} B/s",
        };
    }

    private static SensorReadingItem? SelectPrimary(IReadOnlyList<SensorReadingItem> readings) =>
        readings.FirstOrDefault(reading => reading.HasProgress)
        ?? readings.FirstOrDefault();
}

public sealed record DashboardSensorDetailViewModel(string Name, string Value);

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
