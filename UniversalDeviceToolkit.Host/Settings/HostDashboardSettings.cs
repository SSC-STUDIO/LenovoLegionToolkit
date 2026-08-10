using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Host.Settings;

public enum DashboardGroupType
{
    Power,
    Graphics,
    Display,
    Other,
    Custom
}

public enum DashboardItem
{
    PowerMode,
    BatteryMode,
    BatteryNightChargeMode,
    AlwaysOnUsb,
    InstantBoot,
    HybridMode,
    DiscreteGpu,
    OverclockDiscreteGpu,
    PanelLogoBacklight,
    PortsBacklight,
    Resolution,
    RefreshRate,
    DpiScale,
    Hdr,
    OverDrive,
    TurnOffMonitors,
    Microphone,
    FlipToStart,
    TouchpadLock,
    FnLock,
    WinKeyLock,
    WhiteKeyboardBacklight,
    ItsMode
}

public sealed class DashboardGroup
{
    public DashboardGroupType Type { get; set; }

    public string? CustomName { get; set; }

    public List<DashboardItem> Items { get; set; } = [];

    /// <summary>Mirrors the WPF DashboardGroup.DefaultGroups layout.</summary>
    public static List<DashboardGroup> DefaultGroups => new()
    {
        new()
        {
            Type = DashboardGroupType.Power,
            Items =
            [
                DashboardItem.PowerMode,
                DashboardItem.ItsMode,
                DashboardItem.BatteryMode,
                DashboardItem.BatteryNightChargeMode,
                DashboardItem.AlwaysOnUsb,
                DashboardItem.InstantBoot,
                DashboardItem.FlipToStart,
            ],
        },
        new()
        {
            Type = DashboardGroupType.Graphics,
            Items =
            [
                DashboardItem.HybridMode,
                DashboardItem.DiscreteGpu,
                DashboardItem.OverclockDiscreteGpu,
            ],
        },
        new()
        {
            Type = DashboardGroupType.Display,
            Items =
            [
                DashboardItem.Resolution,
                DashboardItem.RefreshRate,
                DashboardItem.DpiScale,
                DashboardItem.Hdr,
                DashboardItem.OverDrive,
                DashboardItem.TurnOffMonitors,
            ],
        },
        new()
        {
            Type = DashboardGroupType.Other,
            Items =
            [
                DashboardItem.Microphone,
                DashboardItem.WhiteKeyboardBacklight,
                DashboardItem.PanelLogoBacklight,
                DashboardItem.PortsBacklight,
                DashboardItem.TouchpadLock,
                DashboardItem.FnLock,
                DashboardItem.WinKeyLock,
            ],
        },
    };
}

/// <summary>
/// Host-side copy of the WPF dashboard.json settings (same schema) so the
/// Electron frontend can read/customize the dashboard without the WPF app.
/// </summary>
public sealed class HostDashboardSettings() : AbstractSettings<HostDashboardSettings.DashboardSettingsStore>("dashboard.json")
{
    public sealed class DashboardSettingsStore
    {
        public bool ShowSensors { get; set; } = true;
        public double SensorsRefreshIntervalSeconds { get; set; } = 1;
        public List<DashboardGroup>? Groups { get; set; }
    }

    protected override DashboardSettingsStore Default => new()
    {
        Groups = DashboardGroup.DefaultGroups,
    };

    public override DashboardSettingsStore? LoadStore()
    {
        var store = base.LoadStore();
        if (store is not null && store.Groups is not { Count: > 0 })
            store.Groups = DashboardGroup.DefaultGroups;
        return store;
    }

    public override async Task<DashboardSettingsStore?> LoadStoreAsync()
    {
        var store = await base.LoadStoreAsync().ConfigureAwait(false);
        if (store is not null && store.Groups is not { Count: > 0 })
            store.Groups = DashboardGroup.DefaultGroups;
        return store;
    }
}
