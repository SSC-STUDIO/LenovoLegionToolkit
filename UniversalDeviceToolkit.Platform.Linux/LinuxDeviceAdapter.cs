using System.Runtime.InteropServices;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Platform;

namespace UniversalDeviceToolkit.Platform.Linux;

/// <summary>
/// Projects the existing Linux capability probe into the shared snapshot contract.
/// The generic Avalonia surface does not claim write support without a verified
/// backend, even when a platform path exists.
/// </summary>
public sealed class LinuxDeviceAdapter : IDeviceAdapter
{
    private readonly IPlatformServices _services;
    private readonly IReadOnlyCollection<DevicePackDefinition> _packs;

    public LinuxDeviceAdapter(
        IPlatformServices? services = null,
        IReadOnlyCollection<DevicePackDefinition>? packs = null)
    {
        _services = services ?? new LinuxPlatformServices();
        _packs = packs ?? DevicePackCatalogLoader.Load();
    }

    public string PlatformId => "linux";

    public Task<DeviceSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var identity = DeviceIdentity.Unknown("linux", "linux-platform-probe") with
        {
            Architecture = RuntimeInformation.OSArchitecture.ToString()
        };
        var support = DeviceSupportMatcher.Evaluate(identity, _packs);
        IReadOnlyList<DeviceCapability> capabilities =
        [
            Capability("gpu-management", _services.SupportsGpuManagement, "Linux GPU backend was detected."),
            Capability("fan-control", _services.SupportsFanControl, "Linux fan backend was detected."),
            Capability("keyboard-backlight", _services.SupportsKeyboardBacklight, "Linux keyboard backlight backend was detected."),
            Capability("battery-management", _services.SupportsBatteryManagement, "Linux battery backend was detected."),
            Capability("display-control", _services.SupportsDisplayControl, "Linux display backend was detected."),
            Capability("power-profile", _services.SupportsPowerProfile, "Linux power profile backend was detected."),
            Capability("read-only-telemetry", _services.SupportsSystemTelemetry, "Linux telemetry probe is available."),
        ];

        return Task.FromResult(new DeviceSnapshot(
            identity,
            support,
            capabilities,
            [],
            null,
            "linux-platform-probe"));
    }

    private static DeviceCapability Capability(string id, bool available, string availableReason) =>
        available
            ? new DeviceCapability(id, true, false, "linux-platform-probe", availableReason)
            : DeviceCapability.Unavailable(id, "No verified generic Linux backend was detected.", "linux-platform-probe");
}
