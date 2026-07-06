using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.BatteryHealth;

/// <summary>
/// Provides battery health information via WMI.
/// </summary>
public class BatteryHealthService
{
    private readonly SettingsManager<BatteryHealthSettings> _settingsManager;

    public BatteryHealthService(SettingsManager<BatteryHealthSettings> settingsManager)
    {
        _settingsManager = settingsManager;
    }

    /// <summary>
    /// Gets the battery health report (placeholder — WMI integration pending).
    /// </summary>
    public async Task<BatteryHealthReport> GetBatteryHealthReportAsync()
    {
        return await Task.Run(() =>
        {
            // TODO: Implement actual WMI query
            // For now, return mock data
            return new BatteryHealthReport
            {
                DesignCapacity = 80000,
                FullChargeCapacity = 72000,
                CycleCount = 150,
                EstimatedChargeRemaining = 85,
                Status = "OK",
                HealthPercentage = 90
            };
        });
    }
}

/// <summary>
/// Battery health report data.
/// </summary>
public class BatteryHealthReport
{
    public long DesignCapacity { get; set; }
    public long FullChargeCapacity { get; set; }
    public int CycleCount { get; set; }
    public int EstimatedChargeRemaining { get; set; }
    public string Status { get; set; } = "Unknown";
    public int HealthPercentage { get; set; }
}
