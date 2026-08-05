using System.Text.Json;
using System.Linq;
using UniversalDeviceToolkit.Shared.Utils;

namespace UniversalDeviceToolkit.Shared.Settings;

/// <summary>
/// Cross-platform dashboard presentation preferences used by the Avalonia host.
/// The dedicated file keeps dashboard UI state isolated from the host-owned settings
/// document while allowing the portable and Windows Avalonia targets to share it.
/// </summary>
public sealed class AvaloniaDashboardPreferenceStore
{
    /// <summary>Whether the sensor telemetry section is shown on the dashboard.</summary>
    public bool ShowSensors { get; set; } = true;

    /// <summary>
    /// Polling interval in seconds. Values outside the supported range are normalized
    /// by the view model before creating a timer.
    /// </summary>
    public int SensorsRefreshIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Dashboard groups use the same string enum names as the WPF dashboard.json
    /// document, allowing either host to preserve a user's layout.
    /// </summary>
    public List<AvaloniaDashboardGroupPreference> Groups { get; set; } =
        AvaloniaDashboardPreferences.CreateDefaultGroups();
}

public sealed class AvaloniaDashboardGroupPreference
{
    public string Type { get; set; } = "Power";
    public string? CustomName { get; set; }
    public List<string> Items { get; set; } = [];
}

/// <summary>
/// Persists Avalonia dashboard preferences without clobbering WPF-owned settings.
/// </summary>
public sealed class AvaloniaDashboardPreferences : AbstractSettings<AvaloniaDashboardPreferenceStore>
{
    private readonly string? _dataRoot;

    public AvaloniaDashboardPreferences(string? dataRoot = null) : base("avalonia-dashboard.json")
    {
        _dataRoot = string.IsNullOrWhiteSpace(dataRoot) ? null : Path.GetFullPath(dataRoot);
    }

    protected override string SettingsFilePath => _dataRoot is null
        ? base.SettingsFilePath
        : Path.Combine(_dataRoot, "avalonia-dashboard.json");

    public override AvaloniaDashboardPreferenceStore? LoadStore()
    {
        var store = base.LoadStore();
        return Normalize(store ?? ImportWpfStore());
    }

    public override async Task<AvaloniaDashboardPreferenceStore?> LoadStoreAsync()
    {
        var store = await base.LoadStoreAsync().ConfigureAwait(false);
        return Normalize(store ?? ImportWpfStore());
    }

    public static List<AvaloniaDashboardGroupPreference> CreateDefaultGroups() =>
    [
        new()
        {
            Type = "Power",
            Items = [
                "PowerMode",
                "ItsMode",
                "BatteryMode",
                "BatteryNightChargeMode",
                "AlwaysOnUsb",
                "InstantBoot",
                "FlipToStart",
            ],
        },
        new()
        {
            Type = "Graphics",
            Items = ["HybridMode", "DiscreteGpu", "OverclockDiscreteGpu"],
        },
        new()
        {
            Type = "Display",
            Items = ["Resolution", "RefreshRate", "DpiScale", "Hdr", "OverDrive", "TurnOffMonitors"],
        },
        new()
        {
            Type = "Other",
            Items = [
                "Microphone",
                "WhiteKeyboardBacklight",
                "PanelLogoBacklight",
                "PortsBacklight",
                "TouchpadLock",
                "FnLock",
                "WinKeyLock",
            ],
        },
    ];

    private AvaloniaDashboardPreferenceStore? ImportWpfStore()
    {
        var path = Path.Combine(_dataRoot ?? Folders.AppData, "dashboard.json");
        if (!File.Exists(path))
            return null;

        try
        {
            var options = UniversalDeviceToolkit.Shared.Serialization.LltJson.CreateSettingsOptions();
            return JsonSerializer.Deserialize<AvaloniaDashboardPreferenceStore>(
                File.ReadAllText(path),
                options);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static AvaloniaDashboardPreferenceStore Normalize(AvaloniaDashboardPreferenceStore? store)
    {
        var normalized = store ?? new AvaloniaDashboardPreferenceStore();
        normalized.SensorsRefreshIntervalSeconds = Math.Clamp(
            normalized.SensorsRefreshIntervalSeconds,
            1,
            60);

        var groups = normalized.Groups ?? [];
        normalized.Groups = groups
            .Where(group => group is not null && !string.IsNullOrWhiteSpace(group.Type))
            .Select(group => new AvaloniaDashboardGroupPreference
            {
                Type = group.Type,
                CustomName = group.Type.Equals("Custom", StringComparison.OrdinalIgnoreCase)
                    ? group.CustomName
                    : null,
                Items = (group.Items ?? [])
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            })
            .ToList();

        if (normalized.Groups.Count == 0)
            normalized.Groups = CreateDefaultGroups();

        return normalized;
    }
}
