using System.Text.Json;
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
        return store ?? ImportWpfStore();
    }

    public override async Task<AvaloniaDashboardPreferenceStore?> LoadStoreAsync()
    {
        var store = await base.LoadStoreAsync().ConfigureAwait(false);
        return store ?? ImportWpfStore();
    }

    private AvaloniaDashboardPreferenceStore? ImportWpfStore()
    {
        var path = Path.Combine(_dataRoot ?? Folders.AppData, "dashboard.json");
        if (!File.Exists(path))
            return null;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("ShowSensors", out var showSensors)
                || showSensors.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return null;

            return new AvaloniaDashboardPreferenceStore
            {
                ShowSensors = showSensors.GetBoolean(),
            };
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
}
