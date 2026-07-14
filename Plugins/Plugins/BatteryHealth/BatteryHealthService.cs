using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.Shared;
#nullable enable

namespace UniversalDeviceToolkit.Plugins.BatteryHealth;

/// <summary>
/// Battery health classification driven by user-configured thresholds.
/// </summary>
public enum BatteryHealthStatus
{
    Healthy,
    Warning,
    Critical,
    NoBattery,
    Unknown
}

/// <summary>
/// Snapshot of battery health metrics gathered from WMI.
/// </summary>
public sealed class BatteryHealthReport
{
    public long DesignCapacity { get; set; }
    public long FullChargeCapacity { get; set; }
    public int CycleCount { get; set; }
    public int EstimatedChargeRemaining { get; set; }
    public int HealthPercentage { get; set; }
    public int WearPercentage { get; set; }
    public BatteryHealthStatus Status { get; set; } = BatteryHealthStatus.Unknown;
}

/// <summary>
/// Reads battery health information from WMI with strict async timeouts and
/// cancellation support, satisfying the WMI deadlock-protection engineering pillar.
/// DesignedCapacity / FullChargeCapacity / CycleCount come from the ACPI
/// BatteryStaticData class (root\wmi); EstimatedChargeRemaining comes from
/// Win32_Battery (root\cimv2), which is reliably populated across devices.
/// </summary>
public sealed class BatteryHealthService
{
    private const int WmiTimeoutMs = 3000;
    private const int WmiQueryTimeoutMs = 2500;
    private const string AcpiNamespace = @"root\wmi";
    private const string Cimv2Namespace = @"root\cimv2";

    private readonly SettingsManager<BatteryHealthSettings> _settingsManager;

    public BatteryHealthService(SettingsManager<BatteryHealthSettings> settingsManager)
    {
        _settingsManager = settingsManager;
    }

    /// <summary>
    /// Asynchronously gathers the current battery health report.
    /// </summary>
    public Task<BatteryHealthReport> GetBatteryHealthReportAsync(CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(WmiTimeoutMs);

        var token = cts.Token;
        return Task.Run(() =>
        {
            using (cts)
            {
                return QueryBatteryHealth(token);
            }
        }, token);
    }

    private BatteryHealthReport QueryBatteryHealth(CancellationToken cancellationToken)
    {
        var report = new BatteryHealthReport();

        try
        {
            var staticData = QueryBatteryStaticData(cancellationToken);
            var estimatedCharge = QueryEstimatedChargeRemaining(cancellationToken);

            // Nothing reported from either WMI source: no battery present.
            if (staticData is null && estimatedCharge is null)
            {
                report.Status = BatteryHealthStatus.NoBattery;
                return report;
            }

            report.DesignCapacity = staticData?.DesignCapacity ?? 0;
            report.FullChargeCapacity = staticData?.FullChargeCapacity ?? 0;
            report.CycleCount = staticData?.CycleCount ?? 0;
            report.EstimatedChargeRemaining = estimatedCharge ?? 0;

            if (report.DesignCapacity > 0 && report.FullChargeCapacity > 0)
            {
                report.HealthPercentage = (int)Math.Round((double)report.FullChargeCapacity / report.DesignCapacity * 100);
                report.HealthPercentage = Math.Clamp(report.HealthPercentage, 0, 100);
                report.WearPercentage = Math.Clamp(100 - report.HealthPercentage, 0, 100);
            }

            report.Status = ClassifyStatus(report);
            return report;
        }
        catch (OperationCanceledException)
        {
            report.Status = BatteryHealthStatus.Unknown;
            return report;
        }
        catch (ManagementException)
        {
            report.Status = BatteryHealthStatus.NoBattery;
            return report;
        }
        catch (COMException)
        {
            report.Status = BatteryHealthStatus.NoBattery;
            return report;
        }
    }

    private BatteryHealthStatus ClassifyStatus(BatteryHealthReport report)
    {
        var settings = _settingsManager.Load();

        // Guard against misconfigured thresholds. If Critical >= Low or
        // either is out of [0,100], the Healthy/Warning/Critical cascade
        // collapses and silently misclassifies all batteries as Healthy.
        if (settings.EnsureValidThresholds())
        {
            PluginLog.Trace($"BatteryHealth: Thresholds were invalid (Low={settings.LowHealthThreshold}, Critical={settings.CriticalHealthThreshold}). Auto-corrected to safe values.");
            _settingsManager.Save(settings);
        }

        if (report.DesignCapacity <= 0 || report.FullChargeCapacity <= 0)
        {
            return BatteryHealthStatus.Unknown;
        }

        if (report.HealthPercentage >= settings.LowHealthThreshold)
        {
            return BatteryHealthStatus.Healthy;
        }

        if (report.HealthPercentage >= settings.CriticalHealthThreshold)
        {
            return BatteryHealthStatus.Warning;
        }

        return BatteryHealthStatus.Critical;
    }

    private static BatteryStaticDataSnapshot? QueryBatteryStaticData(CancellationToken cancellationToken)
    {
        long design = 0;
        long full = 0;
        int cycle = 0;
        var found = false;

        QueryFirst(AcpiNamespace, "SELECT * FROM BatteryStaticData", cancellationToken, battery =>
        {
            found = true;
            design = ToInt64(battery["DesignedCapacity"]);
            full = ToInt64(battery["FullChargedCapacity"]);
            cycle = ToInt32(battery["CycleCount"]);
        });

        // Fall back to Win32_Battery capacities when the ACPI provider is unavailable,
        // so batteries that only expose cimv2 still surface a health ratio.
        if (design <= 0 || full <= 0)
        {
            QueryFirst(Cimv2Namespace, "SELECT * FROM Win32_Battery", cancellationToken, battery =>
            {
                found = true;
                if (design <= 0)
                {
                    design = ToInt64(battery["DesignCapacity"]);
                }

                if (full <= 0)
                {
                    full = ToInt64(battery["FullChargeCapacity"]);
                }

                if (cycle <= 0)
                {
                    cycle = ToInt32(battery["CycleCount"]);
                }
            });
        }

        return found
            ? new BatteryStaticDataSnapshot { DesignCapacity = design, FullChargeCapacity = full, CycleCount = cycle }
            : null;
    }

    private static int? QueryEstimatedChargeRemaining(CancellationToken cancellationToken)
    {
        int? value = null;

        QueryFirst(Cimv2Namespace, "SELECT * FROM Win32_Battery", cancellationToken, battery =>
        {
            value = ToInt32(battery["EstimatedChargeRemaining"]);
        });

        return value;
    }

    private static void QueryFirst(string scopePath, string query, CancellationToken cancellationToken, Action<ManagementObject> read)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // ManagementObjectSearcher.Get() performs a blocking native COM enumeration that
        // does NOT observe CancellationToken, so CancelAfter cannot interrupt a hung ACPI/WMI
        // provider once the delegate is running. Race the enumeration off-thread against a
        // hard deadline (the abandon pattern used by the host's async WMI helpers) so a stuck
        // provider cannot pin a thread-pool task (and the caller's await) past the timeout
        // contract.
        var firstRowTask = Task.Run<ManagementObject?>(() =>
        {
            var scope = new ManagementScope(scopePath);
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            foreach (var raw in searcher.Get())
            {
                return (ManagementObject)raw; // Only the first battery instance is relevant.
            }
            return null;
        }, cts.Token);

        bool completed;
        try
        {
            completed = firstRowTask.Wait(TimeSpan.FromMilliseconds(WmiQueryTimeoutMs), cts.Token);
        }
        catch (AggregateException ae)
        {
            // The enumeration faulted (ManagementException / COMException / etc). Unwrap so
            // the caller's typed catch blocks in QueryBatteryHealth still match instead of
            // masking them behind AggregateException.
            throw ae.InnerExceptions.Count == 1 ? ae.InnerExceptions[0] : ae;
        }

        if (!completed)
        {
            cts.Cancel(); // Best effort: signals the abandoned enumeration task.
            throw new TimeoutException($"WMI query timed out after {WmiQueryTimeoutMs}ms: {query}");
        }

        var firstRow = firstRowTask.Result;
        if (firstRow is null)
        {
            return; // No matching WMI instances.
        }

        using (firstRow)
        {
            cancellationToken.ThrowIfCancellationRequested();
            read(firstRow);
        }
    }

    private static long ToInt64(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        try
        {
            return Convert.ToInt64(value);
        }
        catch
        {
            return 0;
        }
    }

    private static int ToInt32(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return 0;
        }
    }

    private sealed class BatteryStaticDataSnapshot
    {
        public long DesignCapacity { get; set; }
        public long FullChargeCapacity { get; set; }
        public int CycleCount { get; set; }
    }
}
