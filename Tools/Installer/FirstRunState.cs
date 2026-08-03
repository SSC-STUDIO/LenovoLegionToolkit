using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace UniversalDeviceToolkit.Installer;

/// <summary>
/// Writes the same first-run state files the app produces, so a user who
/// answered the language/device questions during setup is not asked again on
/// first launch. Formats mirror the app:
///   %LocalAppData%\UniversalDeviceToolkit\lang          -> culture name, e.g. "zh-Hans"
///   %LocalAppData%\UniversalDeviceToolkit\device-setup  -> key=value lines
/// </summary>
internal static class FirstRunState
{
    public static string LanguagePath => Path.Combine(InstallerConstants.AppDataDir, "lang");
    public static string DeviceSetupPath => Path.Combine(InstallerConstants.AppDataDir, "device-setup");

    public static void SaveLanguage(string cultureName)
    {
        Directory.CreateDirectory(InstallerConstants.AppDataDir);
        // Match the app byte-for-byte: it writes CultureInfo.Name ("zh-Hans", "pt-BR").
        var normalized = new CultureInfo(cultureName).Name;
        File.WriteAllText(LanguagePath, normalized);
        InstallerLog.Info($"Saved first-run language '{normalized}'.");
    }

    public static void SaveDeviceSetup(string? devicePackId, bool isBasicMode)
    {
        Directory.CreateDirectory(InstallerConstants.AppDataDir);
        File.WriteAllLines(DeviceSetupPath,
        [
            $"devicePackId={devicePackId ?? string.Empty}",
            $"basicMode={isBasicMode}",
            $"confirmedAtUtc={DateTimeOffset.UtcNow:O}",
        ]);
        InstallerLog.Info($"Saved device setup '{devicePackId}' (basicMode={isBasicMode}).");
    }
}

/// <summary>Languages the app offers, with native display names (same order as the app).</summary>
internal sealed record AppLanguage(string Culture, string NativeName);

internal static class AppLanguages
{
    public static readonly AppLanguage[] All =
    [
        new("en", "English"),
        new("ar", "العربية"),
        new("bg", "български"),
        new("cs", "čeština"),
        new("de", "Deutsch"),
        new("el", "Ελληνικά"),
        new("es", "español"),
        new("fr", "français"),
        new("hu", "magyar"),
        new("it", "italiano"),
        new("ja", "日本語"),
        new("lv", "latviešu"),
        new("nl-NL", "Nederlands"),
        new("pl", "polski"),
        new("pt", "português"),
        new("pt-BR", "português do Brasil"),
        new("ro", "română"),
        new("ru", "русский"),
        new("sk", "slovenčina"),
        new("tr", "Türkçe"),
        new("uk", "українська"),
        new("vi", "Tiếng Việt"),
        new("zh-Hans", "简体中文"),
        new("zh-Hant", "繁體中文"),
        new("uz-Latn-UZ", "Uzbek (Latin)"),
    ];

    /// <summary>Cultures whose satellite assemblies ship inside the payload zip.</summary>
    public static bool IsBundled(string cultureName) =>
        cultureName.Equals("en", StringComparison.OrdinalIgnoreCase) ||
        cultureName.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase) ||
        cultureName.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase);

    /// <summary>Same preference rules as the app's GetPreferredStartupLanguage.</summary>
    public static AppLanguage GetPreferred()
    {
        var systemCulture = CultureInfo.CurrentUICulture;

        if (systemCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            var traditional = new[] { "TW", "HK", "MO" }.Any(r =>
                systemCulture.Name.Contains(r, StringComparison.OrdinalIgnoreCase));
            return All.First(l => l.Culture == (traditional ? "zh-Hant" : "zh-Hans"));
        }

        return All.FirstOrDefault(l => l.Culture.Equals(systemCulture.Name, StringComparison.OrdinalIgnoreCase))
               ?? All.FirstOrDefault(l => l.Culture.Equals(systemCulture.Parent.Name, StringComparison.OrdinalIgnoreCase))
               ?? All.FirstOrDefault(l => l.Culture.StartsWith(systemCulture.TwoLetterISOLanguageName + "-", StringComparison.OrdinalIgnoreCase)
                                          || l.Culture.Equals(systemCulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
               ?? All[0];
    }
}

internal sealed record DetectedMachine(string Vendor, string ProductName)
{
    public override string ToString() =>
        string.IsNullOrWhiteSpace(ProductName) ? Vendor : $"{Vendor} {ProductName}";
}

/// <summary>Reads machine vendor/product from the BIOS registry hive (no WMI needed).</summary>
internal static class MachineDetector
{
    public static DetectedMachine Detect()
    {
        string vendor = "", product = "";
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            vendor = (key?.GetValue("SystemManufacturer") as string ?? "").Trim();
            product = (key?.GetValue("SystemProductName") as string ?? "").Trim();
        }
        catch
        {
            // Detection is best-effort; the user can still pick manually.
        }

        return new DetectedMachine(vendor, product);
    }
}

internal static class DevicePackMatcher
{
    /// <summary>Recommended pack: exact machine-type match wins, then vendor/model relations.</summary>
    public static DevicePackInfo FindRecommended(DetectedMachine machine)
    {
        var packs = DevicePackSnapshot.Packs;

        if (!string.IsNullOrWhiteSpace(machine.ProductName))
        {
            var byType = packs.FirstOrDefault(p =>
                p.MachineTypes.Any(t => t.Equals(machine.ProductName, StringComparison.OrdinalIgnoreCase)));
            if (byType is not null)
                return byType;
        }

        var related = packs.Where(p => IsVendorRelated(p, machine) || IsModelRelated(p, machine)).ToList();
        var hardware = related.FirstOrDefault(p => p.IsHardware);
        if (hardware is not null)
            return hardware;

        if (related.Count > 0)
            return related[0];

        return packs.First(p => p.Id == DevicePackSnapshot.GenericBasicPackId);
    }

    /// <summary>
    /// Related packs first, then hardware, then a capped list of basics — mirrors the
    /// app's BuildSelectablePacks so the combo stays usable. Generic pack always last.
    /// </summary>
    public static DevicePackInfo[] BuildSelectable(DetectedMachine machine)
    {
        var packs = DevicePackSnapshot.Packs;
        var related = packs.Where(p => IsVendorRelated(p, machine) || IsModelRelated(p, machine)).ToList();
        var rest = packs.Except(related).ToList();

        var hardwareRest = rest.Where(p => p.IsHardware).Take(12);
        var basicRest = rest.Where(p => !p.IsHardware && p.Id != DevicePackSnapshot.GenericBasicPackId)
            .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(24);
        var generic = packs.First(p => p.Id == DevicePackSnapshot.GenericBasicPackId);

        return related
            .Concat(hardwareRest)
            .Concat(basicRest)
            .Concat([generic])
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();
    }

    private static bool IsVendorRelated(DevicePackInfo pack, DetectedMachine machine)
    {
        if (string.IsNullOrWhiteSpace(pack.Vendor) || pack.Vendor == "*" || string.IsNullOrWhiteSpace(machine.Vendor))
            return false;
        if (pack.Vendor.Equals(machine.Vendor, StringComparison.OrdinalIgnoreCase))
            return true;
        if (pack.Vendor.Equals("LENOVO", StringComparison.OrdinalIgnoreCase) &&
            machine.Vendor.Contains("LENOVO", StringComparison.OrdinalIgnoreCase))
            return true;
        return pack.VendorAliases.Any(a =>
            !string.IsNullOrWhiteSpace(a) &&
            (machine.Vendor.Contains(a, StringComparison.OrdinalIgnoreCase) ||
             a.Contains(machine.Vendor, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsModelRelated(DevicePackInfo pack, DetectedMachine machine) =>
        !string.IsNullOrWhiteSpace(machine.ProductName) &&
        pack.ModelKeywords.Any(k =>
            !string.IsNullOrWhiteSpace(k) &&
            machine.ProductName.Contains(k, StringComparison.OrdinalIgnoreCase));
}
