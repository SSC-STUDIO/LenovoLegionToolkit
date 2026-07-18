using UniversalDeviceToolkit.Plugins.SDK;
using UniversalDeviceToolkit.Plugins.Shared;

namespace UniversalDeviceToolkit.Plugins.BatteryHealth;

[Plugin(
    id: "battery-health",
    name: "Battery Health (Legacy)",
    version: "1.0.0",
    description: "Deprecated: health/temperature thresholds now use the main app battery monitor (battery_health_alerts.json). This legacy plugin is retained only for settings migration.",
    author: "SSC-STUDIO",
    MinimumHostVersion = "5.0.0",
    Icon = "BatteryCharge24"
)]
public sealed class BatteryHealthPlugin : PluginBase
{
    private BatteryHealthSettings? _settings;
    private SettingsManager<BatteryHealthSettings>? _settingsManager;

    public override string Id => "battery-health";
    public override string Name => BatteryHealthText.PluginName;
    public override string Description => BatteryHealthText.PluginDescription;
    public override string Icon => "BatteryCharge24";
    public override bool IsSystemPlugin => false;

    public override void OnInstalled()
    {
        _settingsManager = new SettingsManager<BatteryHealthSettings>("battery-health");
        _settings = _settingsManager.Load();
        base.OnInstalled();
    }

    public override object? GetFeatureExtension() => new BatteryHealthFeaturePage();
    public override object? GetSettingsPage() => new BatteryHealthSettingsPage();
}

public sealed class BatteryHealthFeaturePage : IPluginPage
{
    public string PageTitle => BatteryHealthText.FeaturePageTitle;
    public string? PageIcon => "BatteryCharge24";

    public object CreatePage() => new BatteryHealthControl();
}

public sealed class BatteryHealthSettingsPage : IPluginPage
{
    public string PageTitle => BatteryHealthText.SettingsPageTitle;
    public string? PageIcon => "Settings24";

    public object CreatePage() => new BatteryHealthSettingsControl();
}
