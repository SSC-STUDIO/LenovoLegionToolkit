using System.Text.Json.Serialization;

internal sealed record DoctorReport(
    string OverallStatus,
    DoctorCheck[] Checks)
{
    public static DoctorReport CreatePlaceholder() => new("pending", []);

    public static DoctorReport Create(CrossPlatformStatus status)
    {
        var checks = new List<DoctorCheck>
        {
            CheckRuntime(status),
            CheckHardwareIdentity(status.Hardware),
            CheckTelemetry(status.Telemetry),
            CheckPower(status.Power),
            CheckPowerProfile(status.PowerProfile),
            CheckPlugins(status.Plugins),
            CheckDeviceSupport(status.DeviceSupport),
            CheckHardwareControls(status)
        };

        var overallStatus = checks.Any(check => check.Status == DoctorCheckStatus.Fail)
            ? "fail"
            : checks.Any(check => check.Status == DoctorCheckStatus.Warn)
                ? "warn"
                : "pass";

        return new DoctorReport(overallStatus, checks.ToArray());
    }

    private static DoctorCheck CheckRuntime(CrossPlatformStatus status) =>
        new(
            "Runtime",
            DoctorCheckStatus.Pass,
            $"{status.OsDescription}; {status.Architecture}; {status.DotNetRuntime}");

    private static DoctorCheck CheckHardwareIdentity(HardwareIdentity hardware)
    {
        var hasIdentity = !string.IsNullOrWhiteSpace(hardware.Vendor) ||
                          !string.IsNullOrWhiteSpace(hardware.Model) ||
                          !string.IsNullOrWhiteSpace(hardware.ProductName);

        return hasIdentity
            ? new DoctorCheck("Hardware identity", DoctorCheckStatus.Pass, $"{FirstPresent(hardware.Vendor, "unknown vendor")} {FirstPresent(hardware.Model, hardware.ProductName, "unknown model")} from {hardware.Source}.")
            : new DoctorCheck("Hardware identity", DoctorCheckStatus.Warn, $"No hardware identity was readable from {hardware.Source}.");
    }

    private static DoctorCheck CheckTelemetry(SystemTelemetry telemetry)
    {
        var hasTelemetry = !string.IsNullOrWhiteSpace(telemetry.CpuModel) ||
                           telemetry.MemoryTotalGiB is not null ||
                           telemetry.Temperatures.Length > 0;

        if (hasTelemetry)
        {
            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(telemetry.CpuModel))
                details.Add(telemetry.CpuModel);
            if (telemetry.MemoryTotalGiB is not null)
                details.Add($"{telemetry.MemoryTotalGiB:0.##} GiB RAM");
            if (telemetry.Temperatures.Length > 0)
                details.Add($"{telemetry.Temperatures.Length} temperature readings");

            return new DoctorCheck("Read-only telemetry", DoctorCheckStatus.Pass, $"{string.Join(", ", details)} from {telemetry.Source}.");
        }

        var note = telemetry.Notes.FirstOrDefault(note => !string.IsNullOrWhiteSpace(note));
        return new DoctorCheck(
            "Read-only telemetry",
            DoctorCheckStatus.Warn,
            string.IsNullOrWhiteSpace(note) ? $"No read-only telemetry was available from {telemetry.Source}." : note);
    }

    private static DoctorCheck CheckPower(PowerStatus power)
    {
        if (power.Supplies.Length == 0)
        {
            var note = power.Notes.FirstOrDefault(note => !string.IsNullOrWhiteSpace(note));
            return new DoctorCheck(
                "Power diagnostics",
                DoctorCheckStatus.Warn,
                string.IsNullOrWhiteSpace(note) ? $"No power status was available from {power.Source}." : note);
        }

        var details = new List<string>();
        if (power.HasBattery)
        {
            var battery = power.Supplies.First(supply => supply.Type.Equals("Battery", StringComparison.OrdinalIgnoreCase));
            var batteryState = FirstPresent(battery.Status, "battery");
            details.Add(battery.ChargePercent is null ? batteryState : $"{batteryState} {battery.ChargePercent:0.#}%");
        }

        if (power.IsExternalPowerConnected is not null)
            details.Add(power.IsExternalPowerConnected.Value ? "external power connected" : "external power offline");

        if (details.Count == 0)
            details.Add($"{power.Supplies.Length} power supplies");

        return new DoctorCheck("Power diagnostics", DoctorCheckStatus.Pass, $"{string.Join(", ", details)} from {power.Source}.");
    }

    private static DoctorCheck CheckPowerProfile(PowerProfileStatus profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.ActiveProfile))
        {
            var writable = profile.CanSetProfile ? "settable" : "read-only";
            return new DoctorCheck("Power profile", DoctorCheckStatus.Pass, $"{profile.ActiveProfile} from {profile.Source}; {writable}.");
        }

        var note = profile.Notes.FirstOrDefault(note => !string.IsNullOrWhiteSpace(note));
        return new DoctorCheck(
            "Power profile",
            DoctorCheckStatus.Warn,
            string.IsNullOrWhiteSpace(note) ? $"No platform power profile was available from {profile.Source}." : note);
    }

    private static DoctorCheck CheckPlugins(PluginDiscoveryReport plugins)
    {
        if (plugins.Plugins.Length == 0)
        {
            var note = plugins.Notes.FirstOrDefault(note => !string.IsNullOrWhiteSpace(note));
            return new DoctorCheck(
                "Plugin manifests",
                DoctorCheckStatus.Warn,
                string.IsNullOrWhiteSpace(note) ? "No plugin manifests were found." : note);
        }

        var candidates = plugins.Plugins.Count(plugin => plugin.IsCrossPlatformCandidate);
        return candidates > 0
            ? new DoctorCheck("Plugin manifests", DoctorCheckStatus.Pass, $"{plugins.Plugins.Length} manifests found; {candidates} cross-platform candidates.")
            : new DoctorCheck("Plugin manifests", DoctorCheckStatus.Warn, $"{plugins.Plugins.Length} manifests found, but none declare cross-platform candidates.");
    }

    private static DoctorCheck CheckDeviceSupport(DeviceSupportStatus support) =>
        support.DevicePackId.Equals("generic-pc-basic", StringComparison.OrdinalIgnoreCase)
            ? new DoctorCheck("Device support", DoctorCheckStatus.Warn, $"{support.DisplayName}: {support.Reason}")
            : new DoctorCheck("Device support", DoctorCheckStatus.Pass, $"{support.DisplayName}: {support.Reason}");

    private static DoctorCheck CheckHardwareControls(CrossPlatformStatus status)
    {
        if (status.DeviceSupport.IsHardwareControlAvailable)
            return new DoctorCheck("Hardware controls", DoctorCheckStatus.Pass, "Hardware controls are available.");

        var detail = status.SupportLevel.StartsWith("Windows desktop", StringComparison.OrdinalIgnoreCase)
            ? "Use the Windows desktop app or llt.exe for supported hardware controls."
            : "Hardware-write controls are intentionally hidden on macOS/Linux.";

        return new DoctorCheck("Hardware controls", DoctorCheckStatus.Warn, detail);
    }

    private static string FirstPresent(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}

internal sealed record DoctorCheck(
    string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    DoctorCheckStatus Status,
    string Detail);

internal enum DoctorCheckStatus
{
    Pass,
    Warn,
    Fail
}
