using System.Globalization;
using System.Text;

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
}

internal sealed record CrossPlatformDevicePack(
    string Id,
    string DisplayName,
    string Vendor,
    string[] VendorAliases,
    string[] ModelKeywords,
    string[] EnabledFeatures,
    string[] HiddenFeatures);

internal sealed class CrossPlatformDeviceSupportEvaluator
{
    private static readonly string[] BasicEnabledFeatures =
    [
        "diagnostics",
        "hardware-identity",
        "read-only-telemetry",
        "safe-basic-mode"
    ];

    private static readonly string[] BasicHiddenFeatures =
    [
        "lenovo-hardware-controls",
        "power-modes",
        "battery-conservation",
        "keyboard-backlight",
        "fan-curve",
        "gpu-overclock",
        "plugin-runtime"
    ];

    private static readonly CrossPlatformDevicePack[] DevicePacks =
    [
        BasicPack("apple-mac-basic", "Apple Mac Basic", "Apple Inc.", ["Apple"], ["MacBook", "MacBookPro", "MacBookAir", "Mac", "iMac", "Macmini", "MacStudio"]),
        BasicPack("lenovo-legion-basic", "Lenovo Legion Basic", "LENOVO", ["Lenovo"], ["Legion", "LOQ", "Y7000", "Y9000", "R7000", "R9000"]),
        BasicPack("lenovo-think-basic", "Lenovo Think Basic", "LENOVO", ["Lenovo"], ["ThinkPad", "ThinkBook", "ThinkCentre", "ThinkStation"]),
        BasicPack("asus-basic", "ASUS Basic", "ASUSTeK COMPUTER INC.", ["ASUS", "ASUSTeK COMPUTER INC", "ASUSTeK COMPUTER INCORPORATED"], ["ROG", "TUF", "Zephyrus", "Strix", "VivoBook", "Vivobook", "Zenbook", "ProArt", "ExpertBook"]),
        BasicPack("dell-basic", "Dell Basic", "Dell Inc.", ["Dell", "Dell Computer Corporation", "Alienware"], ["Alienware", "Area-51m", "XPS", "Inspiron", "Precision", "Latitude", "Dell G", "G15", "G16", "m15", "m16", "m18", "x14", "x15", "x16", "x17", "OptiPlex", "Vostro"]),
        BasicPack("hp-basic", "HP Basic", "HP", ["HP Inc.", "Hewlett-Packard", "Hewlett-Packard Company"], ["OMEN", "Victus", "Pavilion", "Envy", "EliteBook", "ProBook", "ZBook", "Spectre"]),
        BasicPack("acer-basic", "Acer Basic", "Acer", ["Acer Incorporated", "Acer Inc."], ["Predator", "Nitro", "Swift", "Aspire", "TravelMate", "ConceptD", "Extensa", "Spin"]),
        BasicPack("msi-basic", "MSI Basic", "Micro-Star International Co., Ltd.", ["MSI", "Micro-Star International", "MICRO-STAR INTERNATIONAL CO., LTD"], ["Raider", "Stealth", "Vector", "Katana", "Cyborg", "Creator", "Prestige", "Modern", "Summit"]),
        BasicPack("microsoft-surface-basic", "Microsoft Surface Basic", "Microsoft Corporation", ["Microsoft"], ["Surface Laptop", "Surface Pro", "Surface Book", "Surface Studio", "Surface Go"]),
        BasicPack("gigabyte-basic", "GIGABYTE Basic", "GIGABYTE", ["Gigabyte Technology Co., Ltd.", "Gigabyte Technology Co., Ltd"], ["AORUS", "AERO", "GIGABYTE G"]),
        BasicPack("razer-basic", "Razer Basic", "Razer", ["Razer Inc.", "Razer Inc"], ["Blade", "Razer Book"]),
        BasicPack("samsung-basic", "Samsung Basic", "SAMSUNG ELECTRONICS CO., LTD.", ["Samsung", "Samsung Electronics", "SAMSUNG ELECTRONICS CO., LTD"], ["Galaxy Book", "Notebook 9"]),
        BasicPack("motorola-basic", "Motorola Basic", "Motorola", ["Motorola Mobility", "Motorola Mobility LLC", "MOTOROLA"], ["Moto Book", "MotoBook", "Motobook", "14IRH10R"]),
        BasicPack("huawei-basic", "HUAWEI Basic", "HUAWEI", ["Huawei Technologies Co., Ltd.", "Huawei Technologies Co., Ltd"], ["MateBook"]),
        BasicPack("xiaomi-basic", "Xiaomi Basic", "Xiaomi", ["Xiaomi Inc.", "Xiaomi Corporation", "Redmi", "TIMI"], ["Mi Notebook", "RedmiBook", "Redmi G", "Xiaomi Book", "Xiaomi Book Pro", "Book Pro"]),
        BasicPack("realme-basic", "realme Basic", "realme", ["realme Chongqing MobileTelecommunications Corp., Ltd.", "realme"], ["realme Book"]),
        BasicPack("infinix-basic", "Infinix Basic", "INFINIX", ["Infinix Mobility Limited", "Infinix"], ["INBook", "Inbook"]),
        BasicPack("honor-basic", "HONOR Basic", "HONOR", ["Honor Device Co., Ltd.", "Honor Device Co., Ltd"], ["MagicBook"]),
        BasicPack("lg-basic", "LG Basic", "LG Electronics", ["LG Electronics Inc.", "LG Electronics Inc", "LG"], ["gram", "UltraPC"]),
        BasicPack("framework-basic", "Framework Basic", "Framework", ["Framework Computer Inc.", "Framework Computer"], ["Framework Laptop"]),
        BasicPack("panasonic-basic", "Panasonic Basic", "Panasonic", ["Panasonic Corporation"], ["TOUGHBOOK", "Let's note", "Lets note"]),
        BasicPack("dynabook-basic", "Dynabook Basic", "Dynabook Inc.", ["Dynabook", "TOSHIBA", "TOSHIBA CORPORATION"], ["Portege", "Tecra", "Satellite"]),
        BasicPack("fujitsu-basic", "Fujitsu Basic", "FUJITSU", ["FUJITSU CLIENT COMPUTING LIMITED", "Fujitsu Client Computing Limited"], ["LIFEBOOK", "CELSIUS"]),
        BasicPack("vaio-basic", "VAIO Basic", "VAIO Corporation", ["VAIO"], ["VAIO"]),
        BasicPack("gateway-basic", "Gateway Basic", "Gateway", ["Gateway Inc.", "Acer Gateway"], ["Gateway"]),
        BasicPack("chuwi-basic", "CHUWI Basic", "CHUWI", ["Chuwi Innovation And Technology", "CHUWI Innovation Limited"], ["HeroBook", "CoreBook", "MiniBook", "GemiBook", "FreeBook"]),
        BasicPack("teclast-basic", "TECLAST Basic", "TECLAST", ["Teclast", "Guangzhou Shangke Information Technology"], ["F15", "F16", "F7", "X6"]),
        BasicPack("jumper-basic", "Jumper Basic", "Jumper", ["Jumper Computer", "Jumper Tech"], ["EZbook", "EZpad"]),
        BasicPack("medion-basic", "MEDION Basic", "MEDION", ["MEDION AG"], ["ERAZER", "AKOYA"]),
        BasicPack("xmg-schenker-basic", "XMG/SCHENKER Basic", "SCHENKER", ["Schenker Technologies GmbH", "XMG", "TUXEDO"], ["XMG", "SCHENKER", "TUXEDO"]),
        BasicPack("hasee-basic", "Hasee Basic", "HASEE", ["Hasee", "Hasee Computer"], ["Hasee", "ZhanShen", "Zhan Shen"]),
        BasicPack("thunderobot-basic", "THUNDEROBOT Basic", "THUNDEROBOT", ["Thunderobot", "Raytheon"], ["Thunderobot", "911", "Zero", "Black Warrior"]),
        BasicPack("machenike-basic", "MACHENIKE Basic", "MACHENIKE", ["Machenike"], ["MACHENIKE", "Machenike", "T58", "F117", "L16"]),
        BasicPack("colorful-basic", "COLORFUL Basic", "COLORFUL", ["Colorful Technology And Development Co., Ltd.", "Colorful"], ["COLORFUL", "Evol", "X15", "MEOW"]),
        BasicPack("maibenben-basic", "MAIBENBEN Basic", "MAIBENBEN", ["Maibenben", "MaiBenBen"], ["Maibenben", "MaiBook", "Xiaomai"]),
        BasicPack("mechrevo-basic", "MECHREVO Basic", "MECHREVO", ["Mechanical Revolution", "MECHREVO INC.", "Tongfang", "THTF", "Tsinghua Tongfang"], ["MECHREVO", "Mechanical Revolution", "Jiaolong", "Kuangshi", "Code", "Unbounded", "F1"]),
        BasicPack("valve-handheld-basic", "Valve Handheld Basic", "Valve", ["Valve Corporation"], ["Steam Deck"]),
        BasicPack("gpd-handheld-basic", "GPD Handheld Basic", "GPD", ["GamePad Digital", "Shenzhen GPD Technology Co., Ltd."], ["GPD WIN", "GPD Win", "Win Max", "Win Mini", "Pocket", "Duo"]),
        BasicPack("ayaneo-handheld-basic", "AYANEO Handheld Basic", "AYANEO", ["AYANEO", "AOKZOE", "Ayn Technologies", "AYN"], ["AYANEO", "AOKZOE", "Loki", "NEXT", "Air Plus"]),
        BasicPack("one-netbook-handheld-basic", "ONE-NETBOOK Handheld Basic", "ONE-NETBOOK", ["One-Netbook", "ONE-NETBOOK Technology", "ONEXPLAYER", "OneXPlayer"], ["OneXPlayer", "ONEXPLAYER", "One-Netbook", "OneMix", "OneGx"]),
        BasicPack("minisforum-basic", "MINISFORUM Basic", "MINISFORUM", ["Micro Computer (HK) Tech Limited", "Minisforum"], ["MINISFORUM", "UM", "HX", "Venus Series"]),
        BasicPack("beelink-basic", "Beelink Basic", "Beelink", ["AZW", "Shenzhen AZW Technology Co., Ltd.", "Beelink"], ["SER", "GTR", "EQ", "Beelink"]),
        BasicPack("geekom-basic", "GEEKOM Basic", "GEEKOM", ["Geekom", "GEEKOM"], ["Mini IT", "MiniAir", "A7", "GT"]),
        BasicPack("zotac-basic", "ZOTAC Basic", "ZOTAC", ["ZOTAC International"], ["ZBOX", "MAGNUS", "ZOTAC"]),
        BasicPack("clevo-tongfang-basic", "Clevo/Tongfang Basic", "CLEVO", ["Notebook", "Tongfang", "Eluktronics", "MECHREVO", "THUNDEROBOT", "Hasee", "SAGER"], ["MECHREVO", "THUNDEROBOT", "Hasee", "SAGER", "Eluktronics", "Maingear"]),
        BasicPack("handheld-pc-basic", "Handheld PC Basic", "*", [], ["Steam Deck", "GPD", "AYANEO", "AOKZOE", "ONEXPLAYER", "ONE-NETBOOK", "ROG Ally", "Legion Go"]),
        BasicPack("mini-pc-basic", "Mini PC Basic", "*", [], ["MINISFORUM", "Beelink", "GEEKOM", "ZOTAC"])
    ];

    public DeviceSupportStatus Evaluate(HardwareIdentity hardware, bool isWindows)
    {
        var matchedPack = DevicePacks.FirstOrDefault(pack => Matches(pack, hardware));
        if (matchedPack is null)
            return GenericBasic("No cross-platform device pack matched the hardware identity.");

        var reason = isWindows
            ? "Matched cross-platform basic device pack. Use the Windows desktop app for supported hardware-control features."
            : "Matched cross-platform basic device pack. Hardware writes remain disabled on non-Windows platforms.";

        return new DeviceSupportStatus(
            "Safe basic mode",
            matchedPack.Id,
            matchedPack.DisplayName,
            matchedPack.EnabledFeatures,
            matchedPack.HiddenFeatures,
            reason);
    }

    private static bool Matches(CrossPlatformDevicePack pack, HardwareIdentity hardware)
    {
        if (!VendorMatches(pack, hardware))
            return false;

        if (pack.ModelKeywords.Length == 0)
            return true;

        var modelSignals = ModelSignals(hardware).ToArray();
        if (modelSignals.Length == 0)
            return !pack.Vendor.Equals("*", StringComparison.OrdinalIgnoreCase);

        return pack.ModelKeywords.Any(keyword => modelSignals.Any(signal => signal.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool VendorMatches(CrossPlatformDevicePack pack, HardwareIdentity hardware)
    {
        if (pack.Vendor.Equals("*", StringComparison.OrdinalIgnoreCase))
            return true;

        var vendorSignals = VendorSignals(hardware).ToArray();
        if (vendorSignals.Length == 0)
            return false;

        return vendorSignals.Any(vendor =>
        {
            var normalizedVendor = NormalizeVendorName(vendor);
            return VendorNameMatches(pack.Vendor, vendor, normalizedVendor) ||
                   pack.VendorAliases.Any(alias => VendorNameMatches(alias, vendor, normalizedVendor));
        });
    }

    private static IEnumerable<string> VendorSignals(HardwareIdentity hardware)
    {
        if (!string.IsNullOrWhiteSpace(hardware.Vendor))
            yield return hardware.Vendor;
    }

    private static IEnumerable<string> ModelSignals(HardwareIdentity hardware)
    {
        if (!string.IsNullOrWhiteSpace(hardware.Model))
            yield return hardware.Model;
        if (!string.IsNullOrWhiteSpace(hardware.ProductName))
            yield return hardware.ProductName;
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

    private static DeviceSupportStatus GenericBasic(string reason) =>
        new(
            "Safe basic mode",
            "generic-pc-basic",
            "Generic PC Basic",
            BasicEnabledFeatures,
            BasicHiddenFeatures,
            reason);

    private static CrossPlatformDevicePack BasicPack(
        string id,
        string displayName,
        string vendor,
        string[] vendorAliases,
        string[] modelKeywords) =>
        new(
            id,
            displayName,
            vendor,
            vendorAliases,
            modelKeywords,
            BasicEnabledFeatures,
            BasicHiddenFeatures);
}
