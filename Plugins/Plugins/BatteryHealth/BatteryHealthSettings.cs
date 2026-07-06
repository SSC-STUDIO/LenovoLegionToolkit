using System;
using System.Collections.ObjectModel;
using System.Management;
using LenovoLegionToolkit.Plugins.Shared;
using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.BatteryHealth;

/// <summary>
/// Settings for Battery Health plugin.
/// </summary>
public class BatteryHealthSettings
{
    public bool EnableRealTimeMonitoring { get; set; } = true;
    public int LowHealthThreshold { get; set; } = 80; // < 80% = warning
    public int CriticalHealthThreshold { get; set; } = 60; // < 60% = critical
    public bool EnableNotification { get; set; } = true;
}
