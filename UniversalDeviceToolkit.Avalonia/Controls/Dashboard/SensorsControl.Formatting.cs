using System;
using System.Collections.Generic;
using Avalonia;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public partial class SensorsControl
{
    internal static string GetGpuMemoryUsageTitle(bool isIntegratedGpu) =>
        isIntegratedGpu
            ? T("SensorsControl_SharedMemoryUsage_Title", "Shared Memory Usage")
            : T("SensorsControl_VramUsage_Title", "VRAM Usage");

    internal static string FormatUsageInGigabytes(float usedGb, float totalGb, float percentage = -1f)
    {
        if (usedGb < 0)
        {
            return percentage >= 0
                ? $"{percentage:0}%"
                : "-";
        }

        if (totalGb <= 0)
        {
            return percentage >= 0
                ? $"{usedGb:0.0} GB ({percentage:0}%)"
                : $"{usedGb:0.0} GB";
        }

        if (percentage < 0)
            percentage = (usedGb / totalGb) * 100f;

        return percentage >= 0
            ? $"{usedGb:0.0} / {totalGb:0.0} GB ({percentage:0}%)"
            : $"{usedGb:0.0} / {totalGb:0.0} GB";
    }

    internal static string FormatTemperaturePair((float first, float second) temperatures, TemperatureUnit temperatureUnit)
    {
        var first = temperatures.first >= 0 ? FormatTemperature(temperatures.first, temperatureUnit) : null;
        var second = temperatures.second >= 0 ? FormatTemperature(temperatures.second, temperatureUnit) : null;

        return (first, second) switch
        {
            ({ } a, { } b) => $"{a} / {b}",
            ({ } a, null) => a,
            (null, { } b) => b,
            _ => "-"
        };
    }

    internal static string FormatThroughputPair(float rxBytesPerSecond, float txBytesPerSecond)
    {
        var rx = FormatThroughput(rxBytesPerSecond);
        var tx = FormatThroughput(txBytesPerSecond);

        return (rx, tx) switch
        {
            ({ } a, { } b) => $"Rx {a}\nTx {b}",
            ({ } a, null) => $"Rx {a}",
            (null, { } b) => $"Tx {b}",
            _ => "-"
        };
    }

    internal static string FormatCpuPowerBreakdown(float totalWatts, (float cores, float memory, float platform) components)
    {
        var parts = new List<string>(4);
        var coresLabel = T("SensorsControl_CpuCoresPower_Label", "Cores");
        var memoryLabel = T("SensorsControl_CpuMemoryPower_Label", "Memory");
        var platformLabel = T("SensorsControl_CpuPlatformPower_Label", "Platform");

        if (totalWatts >= 0)
            parts.Add($"{totalWatts} W");

        if (components.cores > 0)
            parts.Add($"{coresLabel} {components.cores:0.#} W");

        if (components.memory > 0)
            parts.Add($"{memoryLabel} {components.memory:0.#} W");

        if (components.platform > 0)
            parts.Add($"{platformLabel} {components.platform:0.#} W");

        return parts.Count > 0 ? string.Join(" | ", parts) : NotAvailableText();
    }

    internal static string FormatVoltage(float voltage) =>
        voltage > 0 ? $"{voltage:0.000} V" : NotAvailableText();

    internal static string FormatPower(float wattage) =>
        wattage >= 0 ? $"{wattage:0.#} W" : NotAvailableText();

    internal static string FormatPowerKeepingPrevious(float wattage, string? previousText) =>
        wattage >= 0
            ? FormatPower(wattage)
            : !string.IsNullOrWhiteSpace(previousText) && previousText != NotAvailableText()
                ? previousText
                : NotAvailableText();

    internal static string FormatNullableTemperature(double? temperature, TemperatureUnit temperatureUnit) =>
        temperature is { } value ? FormatTemperature(value, temperatureUnit) : NotAvailableText();

    internal static string FormatFrequency(float frequencyMHz) =>
        frequencyMHz > 0 ? $"{frequencyMHz / 1000.0:0.0} {GigahertzUnit}" : NotAvailableText();

    internal static string FormatFallbackRangeText(string? primaryValue, string? existingRangeText)
    {
        if (!string.IsNullOrWhiteSpace(existingRangeText) && existingRangeText != NotAvailableText())
            return existingRangeText;

        return !string.IsNullOrWhiteSpace(primaryValue) && primaryValue != NotAvailableText()
            ? primaryValue
            : NotAvailableText();
    }

    internal static string FormatTemperatureRangeText(string? primaryTemperatureText, string? existingRangeText) =>
        FormatFallbackRangeText(primaryTemperatureText, existingRangeText);

    internal static bool IsUsefulDetailValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        return !string.Equals(trimmed, "-", StringComparison.Ordinal)
            && !string.Equals(trimmed, NotAvailableText(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? FormatThroughput(float bytesPerSecond)
    {
        if (bytesPerSecond < 0)
            return null;

        const float kb = 1024f;
        const float mb = kb * 1024f;
        const float gb = mb * 1024f;

        return bytesPerSecond switch
        {
            >= gb => $"{bytesPerSecond / gb:0.00} GB/s",
            >= mb => $"{bytesPerSecond / mb:0.00} MB/s",
            >= kb => $"{bytesPerSecond / kb:0.00} KB/s",
            _ => $"{bytesPerSecond:0} B/s"
        };
    }

    internal static string FormatTemperature(double temperature, TemperatureUnit temperatureUnit)
    {
        if (temperatureUnit == TemperatureUnit.F)
        {
            temperature *= 9.0 / 5.0;
            temperature += 32;
            return $"{temperature:0} {FahrenheitUnit}";
        }

        return $"{temperature:0} {CelsiusUnit}";
    }

    internal static string? NormalizeModelName(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return null;

        return modelName.Trim();
    }

    internal static string NormalizeHardwareNameOrFallback(string? hardwareName, string fallback)
    {
        var normalized = NormalizeModelName(hardwareName);
        return normalized is null || string.Equals(normalized, "UNKNOWN", StringComparison.OrdinalIgnoreCase)
            ? fallback
            : normalized;
    }

    private void UpdateModelNameText(string elementName, string? modelName)
    {
        if (FindNameCached(elementName) is not TextBlock textBlock)
            return;

        var normalizedModelName = NormalizeModelName(modelName);
        textBlock.Text = normalizedModelName ?? string.Empty;
        textBlock.IsVisible = normalizedModelName is null || _sensorSummaryLayoutMode == SensorSummaryLayoutMode.Compact
            ? false
            : true;
    }
}
