using UniversalDeviceToolkit.Abstractions.Platform;

namespace UniversalDeviceToolkit.Platform.Windows;

public sealed class WindowsPlatformServices : IPlatformServices
{
    public string PlatformName => "windows";
    public bool SupportsGpuManagement => true;
    public bool SupportsFanControl => true;
    public bool SupportsKeyboardBacklight => true;
    public bool SupportsBatteryManagement => true;
    public bool SupportsDisplayControl => true;
    public bool SupportsPowerProfile => true;
    public bool SupportsSystemTelemetry => true;
}
