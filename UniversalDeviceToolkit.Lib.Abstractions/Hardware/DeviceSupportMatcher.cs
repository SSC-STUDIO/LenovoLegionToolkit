using System.Globalization;
using System.Text;

namespace UniversalDeviceToolkit.Abstractions.Hardware;

/// <summary>
/// Matches a portable device identity against the shared device-pack catalog.
/// </summary>
public static class DeviceSupportMatcher
{
    public const string GenericBasicPackId = "generic-pc-basic";

    private const string HardwareControlsFeatureId = "lenovo-hardware-controls";

    private static readonly string[] BasicEnabledFeatures =
    [
        "diagnostics",
        "hardware-identity",
        "read-only-telemetry",
        "safe-basic-mode"
    ];

    private static readonly string[] BasicHiddenFeatures =
    [
        HardwareControlsFeatureId,
        "power-modes",
        "battery-conservation",
        "keyboard-backlight",
        "fan-curve",
        "gpu-overclock",
        "plugin-runtime"
    ];

    public static DeviceSupportInfo Evaluate(
        DeviceIdentity identity,
        IReadOnlyCollection<DevicePackDefinition> packs,
        bool allowVendorHardwareControl = false)
    {
        var matchedPack = FindBestMatch(packs, identity);
        if (matchedPack is null)
            return GenericBasic(identity, "No shared device pack matched the hardware identity.");

        var enabled = BasicEnabledFeatures
            .Concat(matchedPack.EnabledFeatures)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hidden = BasicHiddenFeatures
            .Concat(matchedPack.HiddenFeatures)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!allowVendorHardwareControl && !hidden.Contains(HardwareControlsFeatureId, StringComparer.OrdinalIgnoreCase))
            hidden.Add(HardwareControlsFeatureId);

        var reason = allowVendorHardwareControl && identity.Platform.Equals("windows", StringComparison.OrdinalIgnoreCase)
            ? "Matched shared device pack; vendor hardware control requires a verified Windows backend."
            : identity.Platform.Equals("windows", StringComparison.OrdinalIgnoreCase)
                ? "Matched shared device pack. Use the Windows desktop app for verified hardware-control features; hardware writes remain disabled in this basic surface."
                : "Matched shared device pack; hardware writes remain disabled on this platform.";

        return new DeviceSupportInfo(
            "Safe basic mode",
            matchedPack.Id,
            matchedPack.Id.Equals(GenericBasicPackId, StringComparison.OrdinalIgnoreCase)
                ? "Generic PC Basic"
                : matchedPack.DisplayName,
            enabled,
            hidden,
            reason);
    }

    private static DevicePackDefinition? FindBestMatch(
        IReadOnlyCollection<DevicePackDefinition> packs,
        DeviceIdentity identity)
    {
        DevicePackDefinition? bestPack = null;
        var bestScore = -1;

        foreach (var pack in packs)
        {
            var score = GetMatchScore(pack, identity);
            if (score <= bestScore)
                continue;

            bestPack = pack;
            bestScore = score;
        }

        return bestPack;
    }

    private static int GetMatchScore(DevicePackDefinition pack, DeviceIdentity identity)
    {
        if (!VendorMatches(pack, identity))
            return -1;

        var modelSignals = ModelSignals(identity).ToArray();
        var machineTypes = pack.MachineTypes;
        if (machineTypes.Count > 0 && !string.IsNullOrWhiteSpace(identity.MachineType))
        {
            if (machineTypes.Any(machineType => machineType.Equals(identity.MachineType, StringComparison.OrdinalIgnoreCase)))
                return VendorScore(pack) + 5000 + identity.MachineType.Length;
        }

        var constrained = pack.ModelPrefixes.Count > 0 ||
                          pack.ModelKeywords.Count > 0 ||
                          pack.Families.Count > 0 ||
                          machineTypes.Count > 0;
        if (!constrained)
            return VendorScore(pack);

        if (modelSignals.Length == 0)
            return -1;

        var keywordScore = pack.ModelKeywords
            .Where(keyword => ContainsSignal(modelSignals, keyword))
            .Select(keyword => 3000 + keyword.Length)
            .DefaultIfEmpty(-1)
            .Max();
        var prefixScore = pack.ModelPrefixes
            .Where(prefix => ContainsSignal(modelSignals, prefix))
            .Select(prefix => 2000 + prefix.Length)
            .DefaultIfEmpty(-1)
            .Max();
        var familyScore = pack.Families
            .Where(family => ContainsSignal(modelSignals, family))
            .Select(family => 1000 + family.Length)
            .DefaultIfEmpty(-1)
            .Max();

        return VendorScore(pack) + Math.Max(keywordScore, Math.Max(prefixScore, familyScore));
    }

    private static int VendorScore(DevicePackDefinition pack) =>
        pack.Vendor.Equals("*", StringComparison.OrdinalIgnoreCase) ? 0 : 10000;

    private static bool VendorMatches(DevicePackDefinition pack, DeviceIdentity identity)
    {
        if (pack.Vendor.Equals("*", StringComparison.OrdinalIgnoreCase))
            return true;

        var vendorSignals = new[] { identity.Vendor }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        if (vendorSignals.Length == 0)
            return false;

        return vendorSignals.Any(vendor =>
            VendorNameMatches(pack.Vendor, vendor) ||
            pack.VendorAliases.Any(alias => VendorNameMatches(alias, vendor)));
    }

    private static IEnumerable<string> ModelSignals(DeviceIdentity identity)
    {
        if (!string.IsNullOrWhiteSpace(identity.Model))
            yield return identity.Model;
        if (!string.IsNullOrWhiteSpace(identity.ProductName))
            yield return identity.ProductName;
    }

    private static bool ContainsSignal(IEnumerable<string> signals, string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        signals.Any(signal => signal.Contains(value, StringComparison.OrdinalIgnoreCase));

    private static bool VendorNameMatches(string expected, string actual)
    {
        if (expected.Equals(actual, StringComparison.OrdinalIgnoreCase))
            return true;

        var normalizedExpected = NormalizeVendorName(expected);
        var normalizedActual = NormalizeVendorName(actual);
        return !string.IsNullOrWhiteSpace(normalizedExpected) &&
               !string.IsNullOrWhiteSpace(normalizedActual) &&
               (normalizedExpected.Equals(normalizedActual, StringComparison.OrdinalIgnoreCase) ||
                normalizedActual.StartsWith(normalizedExpected, StringComparison.OrdinalIgnoreCase) ||
                normalizedExpected.StartsWith(normalizedActual, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeVendorName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
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

    private static DeviceSupportInfo GenericBasic(DeviceIdentity identity, string reason) =>
        new(
            "Safe basic mode",
            GenericBasicPackId,
            "Generic PC Basic",
            BasicEnabledFeatures,
            BasicHiddenFeatures,
            reason);
}
