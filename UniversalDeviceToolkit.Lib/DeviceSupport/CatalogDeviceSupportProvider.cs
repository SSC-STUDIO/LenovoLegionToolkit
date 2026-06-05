using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;

namespace LenovoLegionToolkit.Lib.DeviceSupport;

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

    public string Id { get; } = id;

    public Task<DeviceSupportCatalog> GetCatalogAsync(CancellationToken token = default) =>
        Task.FromResult(builtInCatalog);

    public void SetInstalledCatalog(DeviceSupportCatalog? catalog)
    {
        _installedCatalog = catalog;
    }

    public DeviceFeatureAvailability Evaluate(MachineInformation machineInformation, DeviceSupportCatalog? catalog = null)
    {
        catalog ??= MergeCatalogs(_installedCatalog, builtInCatalog);

        var devicePacks = catalog.DevicePacks ?? [];
        var pack = devicePacks.FirstOrDefault(devicePack => MatchesMachineType(devicePack, machineInformation))
                   ?? devicePacks.FirstOrDefault(devicePack => MatchesModel(devicePack, machineInformation));
        if (pack is null)
            return BasicMode();

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

    private static bool MatchesMachineType(DevicePack pack, MachineInformation machineInformation) =>
        VendorMatches(pack, machineInformation) &&
        !string.IsNullOrWhiteSpace(machineInformation.MachineType) &&
        GetCollectionOrEmpty(pack.MachineTypes).Any(machineType => machineType.Equals(machineInformation.MachineType, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesModel(DevicePack pack, MachineInformation machineInformation)
    {
        if (!VendorMatches(pack, machineInformation))
            return false;

        var modelSignals = GetModelSignals(machineInformation).ToArray();
        var modelPrefixes = GetCollectionOrEmpty(pack.ModelPrefixes);
        var machineTypes = GetCollectionOrEmpty(pack.MachineTypes);
        var modelKeywords = GetCollectionOrEmpty(pack.ModelKeywords);
        var families = GetCollectionOrEmpty(pack.Families);

        if (modelPrefixes.Any(prefix => modelSignals.Any(signal => signal.Contains(prefix, StringComparison.OrdinalIgnoreCase))))
            return true;

        if (modelKeywords.Any(keyword => modelSignals.Any(signal => signal.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
            return true;

        if (modelPrefixes.Count == 0 &&
            machineTypes.Count == 0 &&
            modelKeywords.Count == 0 &&
            families.Any(family => modelSignals.Any(signal => signal.Contains(family, StringComparison.OrdinalIgnoreCase))))
            return true;

        return modelPrefixes.Count == 0 &&
               machineTypes.Count == 0 &&
               modelKeywords.Count == 0 &&
               families.Count == 0;
    }

    private static bool VendorMatches(DevicePack pack, MachineInformation machineInformation)
    {
        var vendorSignals = GetVendorSignals(machineInformation).ToArray();
        if (vendorSignals.Length == 0)
            return false;

        if (string.IsNullOrWhiteSpace(pack.Vendor))
            return false;

        if (pack.Vendor.Equals("*", StringComparison.OrdinalIgnoreCase))
            return true;

        return vendorSignals.Any(vendor =>
        {
            var normalizedVendor = NormalizeVendorName(vendor);
            return VendorNameMatches(pack.Vendor, vendor, normalizedVendor) ||
                   GetCollectionOrEmpty(pack.VendorAliases).Any(alias => VendorNameMatches(alias, vendor, normalizedVendor));
        });
    }

    private static IEnumerable<string> GetVendorSignals(MachineInformation machineInformation)
    {
        if (!string.IsNullOrWhiteSpace(machineInformation.Vendor))
            yield return machineInformation.Vendor;

        var hardware = machineInformation.Hardware ?? HardwareInventory.Empty;
        if (!string.IsNullOrWhiteSpace(hardware.ComputerSystem.Manufacturer))
            yield return hardware.ComputerSystem.Manufacturer;
        if (!string.IsNullOrWhiteSpace(hardware.BaseBoard.Manufacturer))
            yield return hardware.BaseBoard.Manufacturer;
        if (!string.IsNullOrWhiteSpace(hardware.Chassis.Manufacturer))
            yield return hardware.Chassis.Manufacturer;
    }

    private static IEnumerable<string> GetModelSignals(MachineInformation machineInformation)
    {
        if (!string.IsNullOrWhiteSpace(machineInformation.Model))
            yield return machineInformation.Model;

        var hardware = machineInformation.Hardware ?? HardwareInventory.Empty;
        foreach (var signal in hardware.MatchSignals)
        {
            if (!string.IsNullOrWhiteSpace(signal))
                yield return signal;
        }
    }

    private static bool VendorNameMatches(string packVendor, string machineVendor, string normalizedMachineVendor)
    {
        if (packVendor.Equals(machineVendor, StringComparison.OrdinalIgnoreCase))
            return true;

        var normalizedPackVendor = NormalizeVendorName(packVendor);
        return !string.IsNullOrWhiteSpace(normalizedPackVendor) &&
               !string.IsNullOrWhiteSpace(normalizedMachineVendor) &&
               (normalizedPackVendor.Equals(normalizedMachineVendor, StringComparison.OrdinalIgnoreCase) ||
                normalizedMachineVendor.StartsWith(normalizedPackVendor, StringComparison.OrdinalIgnoreCase) ||
                normalizedPackVendor.StartsWith(normalizedMachineVendor, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeVendorName(string vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor))
            return string.Empty;

        var builder = new StringBuilder(vendor.Length);
        foreach (var character in vendor.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString()
            .Replace("INCORPORATED", "INC", StringComparison.Ordinal)
            .Replace("CORPORATION", "CORP", StringComparison.Ordinal)
            .Replace("COMPANY", "CO", StringComparison.Ordinal)
            .Replace("LIMITED", "LTD", StringComparison.Ordinal);
    }

    private static IReadOnlyCollection<string> GetCollectionOrEmpty(IReadOnlyCollection<string>? values) =>
        values ?? [];
}
