using System.Runtime.InteropServices;
using System.Text.Json;
using UniversalDeviceToolkit.Abstractions.Hardware;

internal sealed record DeviceSupportStatus(
    string SupportLevel,
    string DevicePackId,
    string DisplayName,
    string[] EnabledFeatures,
    string[] HiddenFeatures,
    string Reason)
{
    public bool IsHardwareControlAvailable =>
        EnabledFeatures.Contains("lenovo-hardware-controls", StringComparer.OrdinalIgnoreCase) &&
        !HiddenFeatures.Contains("lenovo-hardware-controls", StringComparer.OrdinalIgnoreCase);

    public static DeviceSupportStatus From(DeviceSupportInfo support) =>
        new(
            support.SupportLevel,
            support.DevicePackId,
            support.DisplayName,
            support.EnabledFeatures.ToArray(),
            support.HiddenFeatures.ToArray(),
            support.Reason);
}

internal sealed class CrossPlatformDeviceSupportEvaluator
{
    private readonly IReadOnlyCollection<DevicePackDefinition> _packs;

    public CrossPlatformDeviceSupportEvaluator(IReadOnlyCollection<DevicePackDefinition>? packs = null)
    {
        _packs = packs ?? DevicePackCatalogLoader.Load();
    }

    public DeviceSupportStatus Evaluate(HardwareIdentity hardware, bool isWindows)
    {
        var platform = hardware.Source.StartsWith("linux", StringComparison.OrdinalIgnoreCase)
            ? "linux"
            : hardware.Source.StartsWith("macos", StringComparison.OrdinalIgnoreCase)
                ? "macos"
                : isWindows ? "windows" : "unknown";
        var identity = new DeviceIdentity(
            platform,
            RuntimeInformation.OSArchitecture.ToString(),
            hardware.Vendor,
            hardware.Model,
            hardware.ProductName,
            string.Empty,
            hardware.SerialNumber,
            hardware.Source);

        return DeviceSupportStatus.From(
            DeviceSupportMatcher.Evaluate(identity, _packs));
    }
}

internal static class DevicePackCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyCollection<DevicePackDefinition> Load()
    {
        foreach (var path in CandidatePaths())
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                var json = File.ReadAllText(path);
                var packs = JsonSerializer.Deserialize<DevicePackDefinition[]>(json, JsonOptions);
                if (packs is { Length: > 0 })
                    return packs;
            }
            catch
            {
                // A missing or invalid optional catalog degrades to generic basic mode.
            }
        }

        return [];
    }

    private static IEnumerable<string> CandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "resources", "device-packs.json");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "resources", "device-packs.json");

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 8 && directory is not null; depth++, directory = directory.Parent)
            yield return Path.Combine(directory.FullName, "resources", "device-packs.json");
    }
}
