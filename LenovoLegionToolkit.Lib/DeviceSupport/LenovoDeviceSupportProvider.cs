using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.DeviceSupport;

public sealed class LenovoDeviceSupportProvider : IDeviceSupportProvider
{
    public static readonly LenovoDeviceSupportProvider Instance = new();

    private static readonly string[] DefaultEnabledFeatures =
    [
        "lenovo-hardware-controls",
        "sensors",
        "power-modes",
        "battery",
        "plugins",
        "system-optimization"
    ];

    private static readonly string[] BasicModeHiddenFeatures =
    [
        "lenovo-hardware-controls",
        "power-modes",
        "keyboard-backlight",
        "god-mode",
        "gpu-overclock",
        "fan-curve"
    ];

    private static readonly DeviceSupportCatalog BuiltInCatalog = new()
    {
        SchemaVersion = 1,
        AppVersion = "built-in",
        DevicePacks =
        [
            new DevicePack
            {
                Id = "lenovo-legion-5",
                DisplayName = "Lenovo Legion 5",
                Vendor = "LENOVO",
                Families = ["Legion"],
                ModelPrefixes = ["15ACH", "15AKP", "15AHP", "15APH", "15ARH", "15ARP", "15IAH", "15IAX", "15IHU", "15IMH", "15IRH", "15IRX", "15ITH", "16ACH", "16ADR", "16AFR", "16AHP", "16APH", "16ARH", "16ARP", "16ARX", "16IAH", "16IAX", "16IRH", "16IRX", "16ITH", "17ACH", "17ARH", "17IRX", "17ITH", "17IMH"],
                MachineTypes = ["83F0", "83F1", "83M0", "83NX", "83N2", "83LY", "83DG", "83EW", "83EG", "83JJ", "82RC", "82RB", "82TB", "83EF", "82RE", "82RD"],
                ModelKeywords = ["Legion 5", "Y7000", "R7000"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-legion-slim-5",
                DisplayName = "Lenovo Legion Slim 5",
                Vendor = "LENOVO",
                Families = ["Legion", "Lenovo Slim"],
                ModelPrefixes = ["14AHP", "14APH", "14AKP", "14IRP"],
                MachineTypes = ["83DH", "83EX", "82Y5", "82Y9", "82YA", "83D6"],
                ModelKeywords = ["Legion Slim 5", "Lenovo Slim"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-legion-pro-5",
                DisplayName = "Lenovo Legion Pro 5",
                Vendor = "LENOVO",
                Families = ["Legion"],
                ModelPrefixes = ["16IAX", "16IRX", "16ARX"],
                MachineTypes = ["83LT", "83F3", "83DF", "83F2", "83LU", "82WM", "83NN", "82WK", "82JQ"],
                ModelKeywords = ["Legion Pro 5", "Y9000P", "R9000P"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-legion-7",
                DisplayName = "Lenovo Legion 7",
                Vendor = "LENOVO",
                Families = ["Legion"],
                ModelPrefixes = ["16ACH", "16ARH", "16IAH", "16IAX", "16IRH"],
                MachineTypes = ["83KY", "83FD", "82UH", "82TD", "82N6"],
                ModelKeywords = ["Legion 7"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-legion-pro-7",
                DisplayName = "Lenovo Legion Pro 7",
                Vendor = "LENOVO",
                Families = ["Legion"],
                ModelPrefixes = ["16IAX", "16IRX", "16ARX"],
                MachineTypes = ["83RU", "83F5", "83DE", "82WR", "82WQ", "82WS"],
                ModelKeywords = ["Legion Pro 7", "Y9000P", "R9000P"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-legion-9",
                DisplayName = "Lenovo Legion 9",
                Vendor = "LENOVO",
                Families = ["Legion"],
                ModelPrefixes = ["16IRX", "16IAX"],
                MachineTypes = ["83G0", "83EY"],
                ModelKeywords = ["Legion 9"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-legion-go",
                DisplayName = "Lenovo Legion Go",
                Vendor = "LENOVO",
                Families = ["Legion"],
                ModelPrefixes = ["NX"],
                MachineTypes = ["83E1"],
                ModelKeywords = ["Legion Go"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-loq",
                DisplayName = "Lenovo LOQ",
                Vendor = "LENOVO",
                Families = ["LOQ"],
                ModelPrefixes = ["15IAX", "15IRH", "15IRX", "15ARP", "15APH", "16IRH", "16IAX", "16APH"],
                ModelKeywords = ["LOQ"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-ideapad",
                DisplayName = "Lenovo IdeaPad",
                Vendor = "LENOVO",
                Families = ["IdeaPad", "IdeaPad Gaming", "XiaoXin"],
                ModelKeywords = ["IdeaPad Gaming", "IdeaPad", "XiaoXin"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-thinkbook",
                DisplayName = "Lenovo ThinkBook",
                Vendor = "LENOVO",
                Families = ["ThinkBook"],
                ModelPrefixes = ["ThinkBook"],
                ModelKeywords = ["ThinkBook"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-yoga",
                DisplayName = "Lenovo YOGA",
                Vendor = "LENOVO",
                Families = ["YOGA"],
                ModelKeywords = ["YOGA", "Yoga"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "lenovo-legacy-limited",
                DisplayName = "Lenovo Legacy Limited",
                Vendor = "LENOVO",
                Families = ["Legion"],
                ModelPrefixes = ["18IAX", "17IR", "15IR", "15IC", "15IK", "G5000", "R9000", "R7000", "Y9000", "Y7000"],
                ModelKeywords = ["Legion"],
                EnabledFeatures = DefaultEnabledFeatures
            },
            new DevicePack
            {
                Id = "motorola-lenovo-basic",
                DisplayName = "Motorola Lenovo Basic",
                Vendor = "MOTOROLA",
                Families = ["Motorola"],
                ModelKeywords = ["Legion"],
                EnabledFeatures = DefaultEnabledFeatures
            }
        ]
    };

    public string Id => "lenovo";

    public Task<DeviceSupportCatalog> GetCatalogAsync(CancellationToken token = default) =>
        Task.FromResult(BuiltInCatalog);

    public DeviceFeatureAvailability Evaluate(MachineInformation machineInformation, DeviceSupportCatalog? catalog = null)
    {
        catalog ??= BuiltInCatalog;

        if (string.IsNullOrWhiteSpace(machineInformation.Vendor))
            return BasicMode();

        var pack = catalog.DevicePacks.FirstOrDefault(devicePack => MatchesMachineType(devicePack, machineInformation))
                   ?? catalog.DevicePacks.FirstOrDefault(devicePack => MatchesModel(devicePack, machineInformation));
        if (pack is null)
            return BasicMode();

        return new()
        {
            IsSupported = !pack.HiddenFeatures.Contains("lenovo-hardware-controls", StringComparer.OrdinalIgnoreCase),
            DevicePackId = pack.Id,
            EnabledFeatures = pack.EnabledFeatures,
            HiddenFeatures = pack.HiddenFeatures
        };
    }

    private static DeviceFeatureAvailability BasicMode() => new()
    {
        IsSupported = false,
        EnabledFeatures = ["plugins", "system-optimization", "language", "theme", "updates", "logs"],
        HiddenFeatures = BasicModeHiddenFeatures
    };

    private static bool Matches(DevicePack pack, MachineInformation machineInformation)
    {
        if (!pack.Vendor.Equals(machineInformation.Vendor, StringComparison.OrdinalIgnoreCase))
            return false;

        return MatchesMachineType(pack, machineInformation) || MatchesModel(pack, machineInformation);
    }

    private static bool MatchesMachineType(DevicePack pack, MachineInformation machineInformation) =>
        pack.Vendor.Equals(machineInformation.Vendor, StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(machineInformation.MachineType) &&
        pack.MachineTypes.Any(machineType => machineType.Equals(machineInformation.MachineType, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesModel(DevicePack pack, MachineInformation machineInformation)
    {
        if (!pack.Vendor.Equals(machineInformation.Vendor, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(machineInformation.Model) &&
            pack.ModelPrefixes.Any(prefix => machineInformation.Model.Contains(prefix, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (!string.IsNullOrWhiteSpace(machineInformation.Model) &&
            pack.ModelKeywords.Any(keyword => machineInformation.Model.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            return true;

        return pack.ModelPrefixes.Count == 0 &&
               pack.MachineTypes.Count == 0 &&
               pack.ModelKeywords.Count == 0;
    }
}
