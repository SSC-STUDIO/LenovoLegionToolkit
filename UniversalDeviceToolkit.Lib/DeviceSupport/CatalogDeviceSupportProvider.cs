using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Abstractions.Hardware;

namespace UniversalDeviceToolkit.Lib.DeviceSupport;

public class CatalogDeviceSupportProvider(
    string id,
    DeviceSupportCatalog builtInCatalog) : IDeviceSupportProvider, IInstalledDeviceSupportProvider
{
    public const string GenericBasicPackId = "generic-pc-basic";
    private const string LenovoHardwareControlsFeatureId = "lenovo-hardware-controls";

    private static readonly string[] BasicModeEnabledFeatures =
    [
        "plugins",
        "system-optimization",
        "language",
        "theme",
        "updates",
        "logs"
    ];

    private static readonly string[] BasicModeHiddenFeatures =
    [
        LenovoHardwareControlsFeatureId,
        "power-modes",
        "keyboard-backlight",
        "god-mode",
        "gpu-overclock",
        "fan-curve"
    ];

    private DeviceSupportCatalog? _installedCatalog;
    private string? _preferredDevicePackId;

    public string Id { get; } = id;

    public Task<DeviceSupportCatalog> GetCatalogAsync(CancellationToken token = default) =>
        Task.FromResult(builtInCatalog);

    public void SetInstalledCatalog(DeviceSupportCatalog? catalog)
    {
        _installedCatalog = catalog;
    }

    public void SetPreferredDevicePackId(string? packId)
    {
        _preferredDevicePackId = string.IsNullOrWhiteSpace(packId) ? null : packId.Trim();
    }

    public DeviceFeatureAvailability Evaluate(MachineInformation machineInformation, DeviceSupportCatalog? catalog = null)
    {
        catalog ??= MergeCatalogs(_installedCatalog, builtInCatalog);

        var devicePacks = catalog.DevicePacks ?? [];

        // User-confirmed pack from device setup wins over auto-detect.
        if (!string.IsNullOrWhiteSpace(_preferredDevicePackId))
        {
            if (_preferredDevicePackId.Equals(GenericBasicPackId, StringComparison.OrdinalIgnoreCase))
                return BasicMode();

            var preferred = devicePacks.FirstOrDefault(devicePack =>
                devicePack.Id.Equals(_preferredDevicePackId, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
                return FromPack(preferred);
        }

        var pack = FindSharedMatch(devicePacks, machineInformation);
        if (pack is null)
            return BasicMode();

        return FromPack(pack);
    }

    private static DeviceFeatureAvailability FromPack(DevicePack pack)
    {
        var enabledFeatures = GetCollectionOrEmpty(pack.EnabledFeatures);
        var hiddenFeatures = GetCollectionOrEmpty(pack.HiddenFeatures);

        return new()
        {
            IsSupported = !hiddenFeatures.Contains(LenovoHardwareControlsFeatureId, StringComparer.OrdinalIgnoreCase),
            DevicePackId = pack.Id,
            EnabledFeatures = enabledFeatures.Count == 0 ? BasicModeEnabledFeatures : enabledFeatures,
            HiddenFeatures = hiddenFeatures
        };
    }

    protected DeviceSupportCatalog MergeCatalogs(params DeviceSupportCatalog?[] catalogs)
    {
        var packs = catalogs
            .Where(catalog => catalog is not null)
            .SelectMany(catalog => catalog!.DevicePacks ?? [])
            .GroupBy(pack => pack.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        return packs.Length == 0
            ? builtInCatalog
            : new DeviceSupportCatalog
            {
                SchemaVersion = 1,
                AppVersion = "merged",
                DevicePacks = packs
            };
    }

    private static DeviceFeatureAvailability BasicMode() => new()
    {
        IsSupported = false,
        DevicePackId = GenericBasicPackId,
        EnabledFeatures = BasicModeEnabledFeatures,
        HiddenFeatures = BasicModeHiddenFeatures
    };

    private static DevicePack? FindSharedMatch(
        IReadOnlyCollection<DevicePack> devicePacks,
        MachineInformation machineInformation)
    {
        if (devicePacks.Count == 0)
            return null;

        var definitions = devicePacks.Select(ToDefinition).ToArray();
        var support = DeviceSupportMatcher.Evaluate(ToDeviceIdentity(machineInformation), definitions);
        return devicePacks.FirstOrDefault(pack =>
            pack.Id.Equals(support.DevicePackId, StringComparison.OrdinalIgnoreCase));
    }

    private static DevicePackDefinition ToDefinition(DevicePack pack) =>
        new()
        {
            Id = pack.Id,
            DisplayName = pack.DisplayName,
            Vendor = pack.Vendor,
            VendorAliases = GetCollectionOrEmpty(pack.VendorAliases),
            Families = GetCollectionOrEmpty(pack.Families),
            ModelPrefixes = GetCollectionOrEmpty(pack.ModelPrefixes),
            ModelKeywords = GetCollectionOrEmpty(pack.ModelKeywords),
            MachineTypes = GetCollectionOrEmpty(pack.MachineTypes),
            EnabledFeatures = GetCollectionOrEmpty(pack.EnabledFeatures),
            HiddenFeatures = GetCollectionOrEmpty(pack.HiddenFeatures),
        };

    private static DeviceIdentity ToDeviceIdentity(MachineInformation machineInformation)
    {
        var hardware = machineInformation.Hardware ?? HardwareInventory.Empty;
        var vendor = FirstPresent(
            machineInformation.Vendor,
            hardware.ComputerSystem.Manufacturer,
            hardware.BaseBoard.Manufacturer,
            hardware.Chassis.Manufacturer);
        var modelSignals = new[] { machineInformation.Model }
            .Concat(hardware.MatchSignals)
            .Where(signal => !string.IsNullOrWhiteSpace(signal))
            .Select(signal => signal.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var model = FirstPresent(new[] { machineInformation.Model }.Concat(modelSignals).ToArray());
        var machineType = ExtractMachineTypeToken(machineInformation.MachineType) ??
                          modelSignals.Select(ExtractMachineTypeToken).FirstOrDefault(token => token is not null) ??
                          machineInformation.MachineType?.Trim() ??
                          string.Empty;

        return new DeviceIdentity(
            "windows",
            RuntimeInformation.OSArchitecture.ToString(),
            vendor,
            model,
            string.Join(" ", modelSignals),
            machineInformation.BiosVersionRaw ?? string.Empty,
            machineInformation.SerialNumber,
            "wpf-wmi")
        {
            MachineType = machineType,
        };
    }

    private static string? ExtractMachineTypeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // LENOVO_MT_83DF_BU_...
        const string mtMarker = "_MT_";
        var mtIndex = value.IndexOf(mtMarker, StringComparison.OrdinalIgnoreCase);
        if (mtIndex >= 0 && value.Length >= mtIndex + mtMarker.Length + 4)
        {
            var token = value.Substring(mtIndex + mtMarker.Length, 4);
            if (IsLikelyMachineType(token))
                return token.ToUpperInvariant();
        }

        // Bare 4-char MTM (83DF)
        var trimmed = value.Trim();
        if (trimmed.Length >= 4)
        {
            var head = trimmed[..4];
            if (IsLikelyMachineType(head) && (trimmed.Length == 4 || !char.IsLetterOrDigit(trimmed[4])))
                return head.ToUpperInvariant();
        }

        return null;
    }

    private static bool IsLikelyMachineType(string token) =>
        token.Length == 4 &&
        char.IsDigit(token[0]) &&
        char.IsDigit(token[1]) &&
        token.Skip(2).All(c => char.IsLetterOrDigit(c));

    private static string FirstPresent(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static IReadOnlyCollection<string> GetCollectionOrEmpty(IReadOnlyCollection<string>? values) =>
        values ?? [];
}
