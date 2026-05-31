using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

var command = args.FirstOrDefault() ?? "status";

return command.ToLowerInvariant() switch
{
    "status" => PrintStatus(),
    "json" => PrintJson(),
    "hardware" => PrintHardware(),
    "telemetry" => PrintTelemetry(),
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

    if (telemetry.Temperatures.Length > 0)
    {
        Console.WriteLine("Temperatures:");
        foreach (var reading in telemetry.Temperatures)
            Console.WriteLine($"  {reading.Name}: {reading.Celsius:0.0} C ({reading.Source})");
    }

    foreach (var note in telemetry.Notes)
        Console.WriteLine($"Note: {note}");

    return 0;
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
    if (telemetry.Temperatures.Length > 0)
        parts.Add($"{telemetry.Temperatures.Length} temperature readings");

    return parts.Count == 0 ? $"unknown ({telemetry.Source})" : $"{string.Join(", ", parts)} ({telemetry.Source})";
}

static string ValueOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;

static string FormatGibibytes(double? value) => value is null ? "unknown" : $"{value:0.##} GiB";

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

        return new CrossPlatformStatus(
            "Universal Device Toolkit",
            GetVersion(),
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.MachineName,
            RuntimeInformation.FrameworkDescription,
            hardware,
            telemetry,
            supportLevel,
            BuildCapabilities(isWindows, isMacOS, isLinux));
    }

    private static CapabilityStatus[] BuildCapabilities(bool isWindows, bool isMacOS, bool isLinux) =>
    [
        new("Cross-platform CLI", true, "This net10.0 entry point runs without WindowsDesktop, WPF, WMI, registry, or Win32 APIs."),
        new("Machine diagnostics", true, "Reports OS, architecture, machine name, and .NET runtime."),
        new("Hardware identity", true, "Reads Linux DMI or macOS system profiler identity when available; avoids privileged hardware writes."),
        new("Read-only telemetry", true, "Reads Linux procfs/sysfs or macOS sysctl CPU, memory, and safe temperature telemetry where available."),
        new("Basic-mode compatibility", true, "Non-Windows systems are treated as safe basic mode until platform-specific packs are implemented."),
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
