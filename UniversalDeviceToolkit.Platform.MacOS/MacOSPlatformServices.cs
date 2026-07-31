using UniversalDeviceToolkit.Abstractions.Platform;

namespace UniversalDeviceToolkit.Platform.MacOS;

/// <summary>
/// macOS implementation of <see cref="IPlatformServices"/>.
/// Detects feature availability using macOS-specific tools and APIs.
/// </summary>
public sealed class MacOSPlatformServices : IPlatformServices
{
    /// <inheritdoc />
    public string PlatformName => "macos";

    /// <inheritdoc />
    public bool SupportsGpuManagement => false; // Limited NVIDIA support on macOS

    /// <inheritdoc />
    public bool SupportsFanControl => false; // Requires root access via SMC

    /// <inheritdoc />
    public bool SupportsKeyboardBacklight => false;

    /// <inheritdoc />
    public bool SupportsBatteryManagement => true; // pmset API available

    /// <inheritdoc />
    public bool SupportsDisplayControl => true; // System Preferences Display API

    /// <inheritdoc />
    public bool SupportsPowerProfile => true; // pmset command available

    /// <inheritdoc />
    public bool SupportsSystemTelemetry => true; // sysctl and IOKit APIs
}
