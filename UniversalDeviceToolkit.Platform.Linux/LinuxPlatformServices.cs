using UniversalDeviceToolkit.Abstractions.Platform;

namespace UniversalDeviceToolkit.Platform.Linux;

/// <summary>
/// Linux implementation of <see cref="IPlatformServices"/>.
/// Detects feature availability by probing well-known sysfs / procfs paths and CLI tools.
/// </summary>
public sealed class LinuxPlatformServices : IPlatformServices
{
    /// <inheritdoc />
    public string PlatformName => "linux";

    /// <inheritdoc />
    public bool SupportsGpuManagement =>
        File.Exists("/usr/bin/nvidia-smi") || File.Exists("/usr/bin/rocm-smi");

    /// <inheritdoc />
    public bool SupportsFanControl =>
        Directory.Exists("/sys/class/hwmon") &&
        Directory.GetFiles("/sys/class/hwmon", "pwm*", SearchOption.AllDirectories).Length > 0;

    /// <inheritdoc />
    public bool SupportsKeyboardBacklight =>
        Directory.Exists("/sys/class/leds") &&
        Directory.GetDirectories("/sys/class/leds").Any(d =>
            Path.GetFileName(d).Contains("kbd_backlight", StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public bool SupportsBatteryManagement =>
        Directory.Exists("/sys/class/power_supply") &&
        Directory.GetDirectories("/sys/class/power_supply").Any(d =>
            Path.GetFileName(d).StartsWith("BAT", StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public bool SupportsDisplayControl =>
        Directory.Exists("/sys/class/backlight") ||
        File.Exists("/usr/bin/xrandr");

    /// <inheritdoc />
    public bool SupportsPowerProfile =>
        File.Exists("/usr/bin/powerprofilesctl") || File.Exists("/usr/sbin/tuned-adm");

    /// <inheritdoc />
    public bool SupportsSystemTelemetry => true;
}
