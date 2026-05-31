using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

var command = args.FirstOrDefault() ?? "status";
var commandArguments = args.Skip(1).ToArray();

return command.ToLowerInvariant() switch
{
    "status" => PrintStatus(),
    "json" => PrintJson(),
    "hardware" => PrintHardware(),
    "telemetry" => PrintTelemetry(),
    "power" => PrintPower(),
    "profile" or "power-profile" => PrintOrSetPowerProfile(commandArguments),
    "plugins" => PrintPlugins(commandArguments),
    "controls" => PrintControls(),
    "set" => SetControl(commandArguments),
    "verify" => VerifyControl(commandArguments),
    "support" => PrintSupport(),
    "doctor" => PrintDoctor(),
    "help" or "--help" or "-h" => PrintHelp(),
    _ => PrintUnknownCommand(command)
};

static int PrintStatus()
{
    var status = CrossPlatformStatus.Create();

    Console.WriteLine($"{status.ProductName} cross-platform diagnostics");
    Console.WriteLine($"Version: {status.Version}");
    Console.WriteLine($"OS: {status.OsDescription}");
    Console.WriteLine($"Architecture: {status.Architecture}");
    Console.WriteLine($"Machine: {status.MachineName}");
    Console.WriteLine($"Runtime: {status.DotNetRuntime}");
    Console.WriteLine($"Hardware: {FormatHardwareSummary(status.Hardware)}");
    Console.WriteLine($"Telemetry: {FormatTelemetrySummary(status.Telemetry)}");
    Console.WriteLine($"Power: {FormatPowerSummary(status.Power)}");
    Console.WriteLine($"Power profile: {FormatPowerProfileSummary(status.PowerProfile)}");
    Console.WriteLine($"CPU governor: {FormatCpuGovernorSummary(status.CpuGovernor)}");
    Console.WriteLine($"Battery charge limit: {FormatBatteryChargeLimitSummary(status.BatteryChargeLimit)}");
    Console.WriteLine($"Display brightness: {FormatDisplayBrightnessSummary(status.DisplayBrightness)}");
    Console.WriteLine($"Plugins: {FormatPluginSummary(status.Plugins)}");
    Console.WriteLine($"Controls: {FormatControlSummary(status.Controls)}");
    Console.WriteLine($"Device pack: {status.DeviceSupport.DisplayName} ({status.DeviceSupport.DevicePackId})");
    Console.WriteLine($"Support level: {status.SupportLevel}");
    Console.WriteLine();

    foreach (var capability in status.Capabilities)
        Console.WriteLine($"[{(capability.Available ? "yes" : "no ")}] {capability.Name} - {capability.Detail}");

    return 0;
}

static int PrintJson()
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    Console.WriteLine(JsonSerializer.Serialize(CrossPlatformStatus.Create(), options));
    return 0;
}

static int PrintHardware()
{
    var hardware = CrossPlatformStatus.Create().Hardware;

    Console.WriteLine("Hardware identity");
    Console.WriteLine($"Vendor: {ValueOrUnknown(hardware.Vendor)}");
    Console.WriteLine($"Model: {ValueOrUnknown(hardware.Model)}");
    Console.WriteLine($"Product: {ValueOrUnknown(hardware.ProductName)}");
    Console.WriteLine($"Serial: {ValueOrUnknown(hardware.SerialNumber)}");
    Console.WriteLine($"Source: {hardware.Source}");
    return 0;
}

static int PrintTelemetry()
{
    var telemetry = CrossPlatformStatus.Create().Telemetry;

    Console.WriteLine("System telemetry");
    Console.WriteLine($"CPU: {ValueOrUnknown(telemetry.CpuModel)}");
    Console.WriteLine($"Logical processors: {telemetry.LogicalProcessorCount?.ToString() ?? "unknown"}");
    Console.WriteLine($"Memory total: {FormatGibibytes(telemetry.MemoryTotalGiB)}");
    Console.WriteLine($"Memory available: {FormatGibibytes(telemetry.MemoryAvailableGiB)}");
    Console.WriteLine($"Source: {telemetry.Source}");

    if (telemetry.CpuFrequencies.Length > 0)
    {
        Console.WriteLine("CPU frequencies:");
        foreach (var reading in telemetry.CpuFrequencies)
            Console.WriteLine($"  {reading.Name}: {reading.MHz:0.#} MHz ({reading.Source})");
    }

    if (telemetry.Temperatures.Length > 0)
    {
        Console.WriteLine("Temperatures:");
        foreach (var reading in telemetry.Temperatures)
            Console.WriteLine($"  {reading.Name}: {reading.Celsius:0.0} C ({reading.Source})");
    }

    if (telemetry.FanSpeeds.Length > 0)
    {
        Console.WriteLine("Fans:");
        foreach (var reading in telemetry.FanSpeeds)
            Console.WriteLine($"  {reading.Name}: {reading.Rpm} RPM ({reading.Source})");
    }

    foreach (var note in telemetry.Notes)
        Console.WriteLine($"Note: {note}");

    return 0;
}

static int PrintPower()
{
    var power = CrossPlatformStatus.Create().Power;

    Console.WriteLine("Power status");
    Console.WriteLine($"Source: {power.Source}");
    Console.WriteLine($"External power: {FormatBoolean(power.IsExternalPowerConnected)}");
    Console.WriteLine($"Battery present: {(power.HasBattery ? "yes" : "no")}");

    if (power.Supplies.Length > 0)
    {
        Console.WriteLine("Supplies:");
        foreach (var supply in power.Supplies)
        {
            Console.WriteLine($"  {supply.Name} ({supply.Type})");
            Console.WriteLine($"    Status: {ValueOrUnknown(supply.Status)}");
            Console.WriteLine($"    Charge: {FormatPercent(supply.ChargePercent)}");
            Console.WriteLine($"    Energy: {FormatEnergy(supply.EnergyNowWh, supply.EnergyFullWh, supply.EnergyFullDesignWh)}");
            Console.WriteLine($"    Power draw: {FormatWatts(supply.PowerDrawW)}");
            Console.WriteLine($"    Voltage: {FormatVolts(supply.VoltageV)}");
            Console.WriteLine($"    Cycle count: {supply.CycleCount?.ToString() ?? "unknown"}");
            Console.WriteLine($"    Online: {FormatBoolean(supply.IsOnline)}");
            Console.WriteLine($"    Present: {FormatBoolean(supply.IsPresent)}");
            Console.WriteLine($"    Health: {ValueOrUnknown(supply.Health)}");
        }
    }

    foreach (var note in power.Notes)
        Console.WriteLine($"Note: {note}");

    return 0;
}

static int PrintOrSetPowerProfile(IReadOnlyList<string> arguments)
{
    if (arguments.Count == 0)
        return PrintPowerProfile();

    var result = new PowerProfileWriter(new ProcessCommandRunner()).SetProfile(arguments[0]);
    Console.WriteLine(result.Succeeded ? "Power profile changed" : "Power profile change failed");
    Console.WriteLine($"Profile: {result.ProfileId}");
    Console.WriteLine($"Detail: {result.Detail}");
    return result.Succeeded ? 0 : 1;
}

static int PrintPowerProfile()
{
    var profile = CrossPlatformStatus.Create().PowerProfile;

    Console.WriteLine("Power profile");
    Console.WriteLine($"Source: {profile.Source}");
    Console.WriteLine($"Active: {ValueOrUnknown(profile.ActiveProfile)}");
    Console.WriteLine($"Can set profile: {(profile.CanSetProfile ? "yes" : "no")}");

    if (profile.AvailableProfiles.Length > 0)
    {
        Console.WriteLine("Available profiles:");
        foreach (var option in profile.AvailableProfiles)
            Console.WriteLine($"  [{(option.IsActive ? "yes" : "no ")}] {option.Id} - {option.DisplayName}");
    }

    foreach (var note in profile.Notes)
        Console.WriteLine($"Note: {note}");

    return 0;
}

static int PrintPlugins(IReadOnlyList<string> arguments)
{
    var explicitRoot = arguments.Count > 0 ? arguments[0] : null;
    var plugins = explicitRoot is null
        ? CrossPlatformStatus.Create().Plugins
        : new PluginDiscoveryReader(new PhysicalFileSystem(), explicitRoot).Read();

    Console.WriteLine("Plugin discovery");
    Console.WriteLine($"Source: {plugins.Source}");
    Console.WriteLine($"Search roots: {plugins.SearchRoots.Length}");
    foreach (var root in plugins.SearchRoots)
        Console.WriteLine($"  {root}");

    Console.WriteLine($"Plugins: {plugins.Plugins.Length}");
    foreach (var plugin in plugins.Plugins)
    {
        Console.WriteLine($"  {plugin.Id} ({plugin.Version})");
        Console.WriteLine($"    Name: {ValueOrUnknown(plugin.Name)}");
        Console.WriteLine($"    Cross-platform candidate: {(plugin.IsCrossPlatformCandidate ? "yes" : "no")}");
        Console.WriteLine($"    Runtime contribution: {(plugin.HasRuntimeContribution ? "yes" : "no")}");
        Console.WriteLine($"    Optimization actions: {plugin.OptimizationActionCount}");
        Console.WriteLine($"    Target platforms: {(plugin.TargetPlatforms.Length == 0 ? "unspecified" : string.Join(", ", plugin.TargetPlatforms))}");
        Console.WriteLine($"    Reason: {plugin.Reason}");
        Console.WriteLine($"    Manifest: {plugin.ManifestPath}");
    }

    foreach (var note in plugins.Notes)
        Console.WriteLine($"Note: {note}");

    return 0;
}

static int PrintControls()
{
    var controls = CrossPlatformStatus.Create().Controls;

    Console.WriteLine("Hardware controls");
    Console.WriteLine($"Source: {controls.Source}");
    Console.WriteLine($"Controls: {controls.Controls.Length}");

    foreach (var control in controls.Controls)
    {
        Console.WriteLine($"  {control.Id} - {control.DisplayName}");
        Console.WriteLine($"    Kind: {control.Kind}");
        Console.WriteLine($"    Available: {(control.IsAvailable ? "yes" : "no")}");
        Console.WriteLine($"    Writable: {(control.IsWritable ? "yes" : "no")}");
        Console.WriteLine($"    Current: {ValueOrUnknown(control.CurrentValue)}");
        Console.WriteLine($"    Detail: {control.Detail}");
        if (control.Options.Length > 0)
        {
            Console.WriteLine("    Options:");
            foreach (var option in control.Options)
                Console.WriteLine($"      [{(option.IsActive ? "yes" : "no ")}] {option.Value} - {option.DisplayName}");
        }
    }

    foreach (var note in controls.Notes)
        Console.WriteLine($"Note: {note}");

    return 0;
}

static int SetControl(IReadOnlyList<string> arguments)
{
    if (arguments.Count < 2)
    {
        Console.Error.WriteLine("Usage: udt set <control-id> <value>");
        return 2;
    }

    var result = new HardwareControlSurfaceWriter(new PhysicalFileSystem(), new ProcessCommandRunner()).Set(arguments[0], arguments[1]);
    Console.WriteLine(result.Succeeded ? "Control changed" : "Control change failed");
    Console.WriteLine($"Control: {result.ControlId}");
    Console.WriteLine($"Value: {result.Value}");
    Console.WriteLine($"Detail: {result.Detail}");
    return result.Succeeded ? 0 : 1;
}

static int VerifyControl(IReadOnlyList<string> arguments)
{
    if (arguments.Count < 2)
    {
        Console.Error.WriteLine("Usage: udt verify <control-id> <value>");
        return 2;
    }

    var fileSystem = new PhysicalFileSystem();
    var commandRunner = new ProcessCommandRunner();
    var verifier = new PerformanceEffectVerifier(
        () => new SystemTelemetryReader(fileSystem, commandRunner).Read(),
        (controlId, value) => new HardwareControlSurfaceWriter(fileSystem, commandRunner).Set(controlId, value),
        () => Thread.Sleep(TimeSpan.FromSeconds(2)));
    var report = verifier.Verify(arguments[0], arguments[1]);

    Console.WriteLine("Performance verification");
    Console.WriteLine($"Control: {report.ControlId}");
    Console.WriteLine($"Requested value: {report.RequestedValue}");
    Console.WriteLine(report.ControlResult.Succeeded ? "Control write: succeeded" : "Control write: failed");
    Console.WriteLine($"Detail: {report.ControlResult.Detail}");
    PrintCpuFrequencySummary("Before CPU frequency", report.BeforeCpuFrequency);
    PrintCpuFrequencySummary("After CPU frequency", report.AfterCpuFrequency);
    Console.WriteLine($"Average CPU frequency delta: {FormatMegahertz(report.AverageFrequencyDeltaMHz)}");

    foreach (var note in report.Notes)
        Console.WriteLine($"Note: {note}");

    return report.Succeeded ? 0 : 1;
}

static int PrintSupport()
{
    var support = CrossPlatformStatus.Create().DeviceSupport;

    Console.WriteLine("Device support");
    Console.WriteLine($"Level: {support.SupportLevel}");
    Console.WriteLine($"Pack: {support.DisplayName} ({support.DevicePackId})");
    Console.WriteLine($"Reason: {support.Reason}");
    Console.WriteLine($"Hardware controls: {(support.IsHardwareControlAvailable ? "available" : "hidden")}");
    Console.WriteLine($"Enabled features: {string.Join(", ", support.EnabledFeatures)}");
    Console.WriteLine($"Hidden features: {string.Join(", ", support.HiddenFeatures)}");
    return 0;
}

static int PrintDoctor()
{
    var report = CrossPlatformStatus.Create().Doctor;

    Console.WriteLine("Doctor report");
    Console.WriteLine($"Overall: {report.OverallStatus}");
    foreach (var check in report.Checks)
        Console.WriteLine($"[{check.Status.ToString().ToLowerInvariant()}] {check.Name} - {check.Detail}");

    return report.OverallStatus == "fail" ? 1 : 0;
}

static int PrintHelp()
{
    Console.WriteLine("Universal Device Toolkit cross-platform diagnostics");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  udt status    Print human-readable platform support status.");
    Console.WriteLine("  udt json      Print platform support status as JSON.");
    Console.WriteLine("  udt hardware  Print basic hardware identity for device-pack matching.");
    Console.WriteLine("  udt telemetry Print safe read-only CPU, memory, and temperature telemetry.");
    Console.WriteLine("  udt power     Print safe read-only battery and external power status.");
    Console.WriteLine("  udt profile   Print platform power profile, or set it with a profile argument.");
    Console.WriteLine("  udt plugins   Inspect plugin manifests without loading Windows/WPF assemblies.");
    Console.WriteLine("  udt controls  Print writable and hidden cross-platform hardware controls.");
    Console.WriteLine("  udt set <id> <value>  Set a writable cross-platform control.");
    Console.WriteLine("  udt verify <id> <value>  Set a control and sample CPU frequency before/after.");
    Console.WriteLine("  udt support   Print safe basic-mode device support matching.");
    Console.WriteLine("  udt doctor    Print aggregated cross-platform readiness checks.");
    Console.WriteLine("  udt help      Show this help.");
    Console.WriteLine();
    Console.WriteLine("Windows hardware controls remain in the Windows desktop app. macOS and Linux support starts with diagnostics, safe basic-mode discovery, and future plugin/runtime expansion.");
    return 0;
}

static string FormatHardwareSummary(HardwareIdentity hardware)
{
    var values = new[] { hardware.Vendor, hardware.Model }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToArray();

    return values.Length == 0 ? $"unknown ({hardware.Source})" : $"{string.Join(' ', values)} ({hardware.Source})";
}

static string FormatTelemetrySummary(SystemTelemetry telemetry)
{
    var parts = new List<string>();
    if (!string.IsNullOrWhiteSpace(telemetry.CpuModel))
        parts.Add(telemetry.CpuModel);
    if (telemetry.MemoryTotalGiB is not null)
        parts.Add($"{telemetry.MemoryTotalGiB:0.##} GiB RAM");
    if (telemetry.CpuFrequencies.Length > 0)
        parts.Add($"{telemetry.CpuFrequencies.Length} CPU frequency readings");
    if (telemetry.Temperatures.Length > 0)
        parts.Add($"{telemetry.Temperatures.Length} temperature readings");
    if (telemetry.FanSpeeds.Length > 0)
        parts.Add($"{telemetry.FanSpeeds.Length} fan readings");

    return parts.Count == 0 ? $"unknown ({telemetry.Source})" : $"{string.Join(", ", parts)} ({telemetry.Source})";
}

static string FormatPowerSummary(PowerStatus power)
{
    var parts = new List<string>();
    if (power.HasBattery)
    {
        var battery = power.Supplies.First(supply => supply.Type.Equals("Battery", StringComparison.OrdinalIgnoreCase));
        var batteryParts = new List<string> { ValueOrUnknown(battery.Status) };
        if (battery.ChargePercent is not null)
            batteryParts.Add($"{battery.ChargePercent:0.#}%");
        parts.Add($"battery {string.Join(' ', batteryParts.Where(part => !string.IsNullOrWhiteSpace(part)))}");
    }

    if (power.IsExternalPowerConnected is not null)
        parts.Add(power.IsExternalPowerConnected.Value ? "external power connected" : "external power offline");

    return parts.Count == 0 ? $"unknown ({power.Source})" : $"{string.Join(", ", parts)} ({power.Source})";
}

static string FormatPowerProfileSummary(PowerProfileStatus profile)
{
    if (!string.IsNullOrWhiteSpace(profile.ActiveProfile))
        return $"{profile.ActiveProfile} ({profile.Source})";

    return $"unknown ({profile.Source})";
}

static string FormatCpuGovernorSummary(CpuGovernorStatus governor)
{
    if (!string.IsNullOrWhiteSpace(governor.ActiveGovernor))
        return $"{governor.ActiveGovernor} across {governor.Policies.Length} policies ({governor.Source})";

    return $"unknown ({governor.Source})";
}

static string FormatBatteryChargeLimitSummary(BatteryChargeLimitStatus chargeLimit)
{
    var device = chargeLimit.Devices.FirstOrDefault();
    return device?.EndThreshold is null
        ? $"unknown ({chargeLimit.Source})"
        : $"{device.EndThreshold}% on {device.Id} ({chargeLimit.Source})";
}

static string FormatDisplayBrightnessSummary(DisplayBrightnessStatus brightness)
{
    var device = brightness.Devices.FirstOrDefault();
    return device is null
        ? $"unknown ({brightness.Source})"
        : $"{device.Percent}% on {device.Id} ({brightness.Source})";
}

static string FormatPluginSummary(PluginDiscoveryReport plugins)
{
    var candidates = plugins.Plugins.Count(plugin => plugin.IsCrossPlatformCandidate);
    return plugins.Plugins.Length == 0
        ? $"none found ({plugins.Source})"
        : $"{plugins.Plugins.Length} manifests, {candidates} cross-platform candidates ({plugins.Source})";
}

static string FormatControlSummary(HardwareControlSurface controls)
{
    var writable = controls.Controls.Count(control => control.IsWritable);
    var available = controls.Controls.Count(control => control.IsAvailable);
    return $"{available} available, {writable} writable ({controls.Source})";
}

static string ValueOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;

static string FormatGibibytes(double? value) => value is null ? "unknown" : $"{value:0.##} GiB";

static string FormatPercent(double? value) => value is null ? "unknown" : $"{value:0.#}%";

static string FormatWatts(double? value) => value is null ? "unknown" : $"{value:0.##} W";

static string FormatVolts(double? value) => value is null ? "unknown" : $"{value:0.##} V";

static string FormatBoolean(bool? value) => value is null ? "unknown" : value.Value ? "yes" : "no";

static string FormatMegahertz(double? value) => value is null ? "unknown" : $"{value:0.#} MHz";

static void PrintCpuFrequencySummary(string label, CpuFrequencySummary summary)
{
    if (summary.Count == 0)
    {
        Console.WriteLine($"{label}: unavailable");
        return;
    }

    Console.WriteLine(
        $"{label}: avg {FormatMegahertz(summary.AverageMHz)}, min {FormatMegahertz(summary.MinMHz)}, max {FormatMegahertz(summary.MaxMHz)}, {summary.Count} readings ({ValueOrUnknown(summary.Source)})");
}

static string FormatEnergy(double? nowWh, double? fullWh, double? designWh)
{
    var parts = new List<string>();
    if (nowWh is not null)
        parts.Add($"{nowWh:0.##} Wh now");
    if (fullWh is not null)
        parts.Add($"{fullWh:0.##} Wh full");
    if (designWh is not null)
        parts.Add($"{designWh:0.##} Wh design");

    return parts.Count == 0 ? "unknown" : string.Join(", ", parts);
}

static int PrintUnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Run 'udt help'.");
    return 2;
}

internal sealed record CrossPlatformStatus(
    string ProductName,
    string Version,
    string OsDescription,
    string Architecture,
    string MachineName,
    string DotNetRuntime,
    HardwareIdentity Hardware,
    SystemTelemetry Telemetry,
    PowerStatus Power,
    PowerProfileStatus PowerProfile,
    CpuGovernorStatus CpuGovernor,
    BatteryChargeLimitStatus BatteryChargeLimit,
    DisplayBrightnessStatus DisplayBrightness,
    PluginDiscoveryReport Plugins,
    HardwareControlSurface Controls,
    DeviceSupportStatus DeviceSupport,
    DoctorReport Doctor,
    string SupportLevel,
    CapabilityStatus[] Capabilities)
{
    public static CrossPlatformStatus Create()
    {
        var isWindows = System.OperatingSystem.IsWindows();
        var isMacOS = System.OperatingSystem.IsMacOS();
        var isLinux = System.OperatingSystem.IsLinux();
        var supportLevel = isWindows
            ? "Windows desktop app and full hardware-control stack are available."
            : isMacOS || isLinux
                ? "Basic cross-platform diagnostics are available; vendor-specific hardware control is not enabled on this platform."
                : "Unsupported OS; diagnostics may be incomplete.";

        var hardware = new HardwareIdentityReader(
            new PhysicalFileSystem(),
            new ProcessCommandRunner()).Read();
        var telemetry = new SystemTelemetryReader(
            new PhysicalFileSystem(),
            new ProcessCommandRunner()).Read();
        var power = new PowerStatusReader(
            new PhysicalFileSystem(),
            new ProcessCommandRunner()).Read();
        var powerProfile = new PowerProfileReader(
            new ProcessCommandRunner()).Read();
        var cpuGovernor = new CpuGovernorReader(
            new PhysicalFileSystem()).Read();
        var batteryChargeLimit = new BatteryChargeLimitReader(
            new PhysicalFileSystem()).Read();
        var displayBrightness = new DisplayBrightnessReader(
            new PhysicalFileSystem()).Read();
        var plugins = new PluginDiscoveryReader(
            new PhysicalFileSystem()).Read();
        var deviceSupport = new CrossPlatformDeviceSupportEvaluator().Evaluate(hardware, isWindows);
        var controls = new HardwareControlSurfaceReader(powerProfile, cpuGovernor, batteryChargeLimit, displayBrightness, plugins, deviceSupport).Read();
        var status = new CrossPlatformStatus(
            "Universal Device Toolkit",
            GetVersion(),
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.MachineName,
            RuntimeInformation.FrameworkDescription,
            hardware,
            telemetry,
            power,
            powerProfile,
            cpuGovernor,
            batteryChargeLimit,
            displayBrightness,
            plugins,
            controls,
            deviceSupport,
            DoctorReport.CreatePlaceholder(),
            supportLevel,
            BuildCapabilities(isWindows, isMacOS, isLinux));

        return status with { Doctor = DoctorReport.Create(status) };
    }

    private static CapabilityStatus[] BuildCapabilities(bool isWindows, bool isMacOS, bool isLinux) =>
    [
        new("Cross-platform CLI", true, "This net10.0 entry point runs without WindowsDesktop, WPF, WMI, registry, or Win32 APIs."),
        new("Machine diagnostics", true, "Reports OS, architecture, machine name, and .NET runtime."),
        new("Hardware identity", true, "Reads Linux DMI or macOS system profiler identity when available; avoids privileged hardware writes."),
        new("Read-only telemetry", true, "Reads Linux procfs/sysfs or macOS sysctl CPU, memory, frequency, and safe temperature/fan telemetry where available."),
        new("Power diagnostics", true, "Reads Linux power_supply or macOS pmset battery and external power status without changing hardware state."),
        new("Platform power profiles", isMacOS || isLinux, isLinux
            ? "Can inspect and set Linux power-profiles-daemon profiles through powerprofilesctl."
            : isMacOS
                ? "Can inspect and set macOS low power mode through pmset."
                : "Use the Windows desktop app for Windows power mode and Lenovo thermal mode integration."),
        new("CPU governor", isLinux, isLinux
            ? "Can inspect and set Linux CPU frequency governors through /sys/devices/system/cpu."
            : "Linux CPU governor control is not available on this platform."),
        new("Battery charge limit", isLinux, isLinux
            ? "Can inspect and set Linux battery charge thresholds through /sys/class/power_supply."
            : "Linux battery charge threshold control is not available on this platform."),
        new("Display brightness", isLinux, isLinux
            ? "Can inspect and set Linux backlight brightness through /sys/class/backlight."
            : "Linux backlight control is not available on this platform."),
        new("Plugin manifest discovery", true, "Inspects plugin manifests on every platform without loading WPF or Windows-only plugin assemblies."),
        new("Cross-platform control surface", true, "Lists writable standard OS controls and hidden vendor-specific controls through one metadata surface."),
        new("Basic-mode compatibility", true, "Matches common vendors to safe basic device packs and hides hardware-write features on non-Windows platforms."),
        new("Windows hardware controls", isWindows, isWindows
            ? "Use the Windows desktop app or existing llt.exe CLI for Lenovo hardware controls."
            : "Windows-only controls are intentionally hidden on macOS/Linux."),
        new("Plugin runtime", isWindows, isWindows
            ? "Windows plugin workflows remain available in the desktop app."
            : "Cross-platform plugin loading is a future expansion point and is not enabled yet."),
        new("Linux diagnostics", isLinux, isLinux
            ? "Running on Linux; safe diagnostics are enabled."
            : "Not running on Linux."),
        new("macOS diagnostics", isMacOS, isMacOS
            ? "Running on macOS; safe diagnostics are enabled."
            : "Not running on macOS.")
    ];

    private static string GetVersion() =>
        typeof(CrossPlatformStatus).Assembly.GetName().Version?.ToString() ?? "unknown";
}

internal sealed record CapabilityStatus(string Name, bool Available, string Detail);
