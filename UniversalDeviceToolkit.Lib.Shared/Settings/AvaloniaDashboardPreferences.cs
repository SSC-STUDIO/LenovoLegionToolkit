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
    public int SchemaVersion { get; set; } = AvaloniaDashboardPreferences.CurrentSchemaVersion;

    /// <summary>Whether the sensor telemetry section is shown on the dashboard.</summary>
    public bool ShowSensors { get; set; } = true;

    /// <summary>
    /// Polling interval in seconds. Values outside the supported range are normalized
    /// by the view model before creating a timer.
    /// </summary>
    public int SensorsRefreshIntervalSeconds { get; set; } = 1;

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
    internal const int CurrentSchemaVersion = 4;
    private const string CanonicalSettingsFileName = "dashboard.json";
    private const string LegacySettingsFileName = "avalonia-dashboard.json";
    private readonly string? _dataRoot;

    public AvaloniaDashboardPreferences(string? dataRoot = null) : base(CanonicalSettingsFileName)
    {
        _dataRoot = string.IsNullOrWhiteSpace(dataRoot) ? null : Path.GetFullPath(dataRoot);
    }

    protected override string SettingsFilePath => _dataRoot is null
        ? base.SettingsFilePath
        : Path.Combine(_dataRoot, CanonicalSettingsFileName);

    public override AvaloniaDashboardPreferenceStore? LoadStore()
    {
        var store = base.LoadStore();
        return Normalize(store ?? ImportLegacyStore());
    }

    public override async Task<AvaloniaDashboardPreferenceStore?> LoadStoreAsync()
    {
        var store = await base.LoadStoreAsync().ConfigureAwait(false);
        return Normalize(store ?? ImportLegacyStore());
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

    private AvaloniaDashboardPreferenceStore? ImportLegacyStore()
    {
        var path = Path.Combine(_dataRoot ?? Folders.AppData, LegacySettingsFileName);
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

    public static AvaloniaDashboardPreferenceStore Normalize(AvaloniaDashboardPreferenceStore? store)
    {
        var normalized = store ?? new AvaloniaDashboardPreferenceStore();
        var isLegacySchema = normalized.SchemaVersion < 3;
        if (isLegacySchema)
            normalized.ShowSensors = true;

        normalized.SensorsRefreshIntervalSeconds = Math.Clamp(
            normalized.SensorsRefreshIntervalSeconds,
            1,
            60);

        var validGroupTypes = CreateDefaultGroups()
            .Select(group => group.Type)
            .Append("Custom")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var validItems = CreateDefaultGroups()
            .SelectMany(group => group.Items)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groups = normalized.Groups ?? [];
        normalized.Groups = groups
            .Where(group => group is not null
                && !string.IsNullOrWhiteSpace(group.Type)
                && validGroupTypes.Contains(group.Type))
            .Select(group => new AvaloniaDashboardGroupPreference
            {
                Type = group.Type,
                CustomName = group.Type.Equals("Custom", StringComparison.OrdinalIgnoreCase)
                    ? group.CustomName
                    : null,
                Items = (group.Items ?? [])
                    .Where(item => !string.IsNullOrWhiteSpace(item) && validItems.Contains(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            })
            .ToList();

        if (normalized.Groups.Count == 0)
            normalized.Groups = CreateDefaultGroups();

        EnsureBuiltInGroup(normalized, "Graphics");
        EnsureBuiltInItem(normalized, "Power", "PowerMode");
        EnsureBuiltInItem(normalized, "Power", "ItsMode", insertAfter: "PowerMode");
        normalized.SchemaVersion = CurrentSchemaVersion;

        return normalized;
    }

    private static void EnsureBuiltInGroup(
        AvaloniaDashboardPreferenceStore store,
        string groupType)
    {
        if (store.Groups.Any(group => group.Type.Equals(groupType, StringComparison.OrdinalIgnoreCase)))
            return;

        var defaultGroup = CreateDefaultGroups()
            .First(group => group.Type.Equals(groupType, StringComparison.OrdinalIgnoreCase));
        store.Groups.Add(defaultGroup);
    }

    private static void EnsureBuiltInItem(
        AvaloniaDashboardPreferenceStore store,
        string groupType,
        string item,
        string? insertAfter = null)
    {
        if (store.Groups.Any(group => group.Items.Any(existing =>
                existing.Equals(item, StringComparison.OrdinalIgnoreCase))))
        {
            return;
        }

        var group = store.Groups.FirstOrDefault(candidate =>
            candidate.Type.Equals(groupType, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            group = new AvaloniaDashboardGroupPreference { Type = groupType };
            store.Groups.Insert(0, group);
        }

        var index = insertAfter is null
            ? -1
            : group.Items.FindIndex(existing =>
                existing.Equals(insertAfter, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            group.Items.Insert(0, item);
        else
            group.Items.Insert(index + 1, item);
    }
}
