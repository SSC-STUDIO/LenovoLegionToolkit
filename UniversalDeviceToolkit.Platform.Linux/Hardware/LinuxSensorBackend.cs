using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using UniversalDeviceToolkit.Abstractions.Hardware;

namespace UniversalDeviceToolkit.Platform.Linux.Hardware;

/// <summary>
/// Linux implementation of <see cref="ISensorBackend"/>.
/// Reads sensor data from sysfs hwmon and /proc/stat.
/// </summary>
public sealed class LinuxSensorBackend : ISensorBackend
{
    private const string HwmonRoot = "/sys/class/hwmon";

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsLinux();

    /// <inheritdoc />
    public IReadOnlyList<SensorReading> GetReadings()
    {
        var readings = new List<SensorReading>();
        ReadHwmonTemperatures(readings);
        ReadHwmonFanSpeeds(readings);
        ReadCpuUsage(readings);
        ReadMemoryUsage(readings);
        return readings;
    }

    private static void ReadHwmonTemperatures(List<SensorReading> readings)
    {
        if (!Directory.Exists(HwmonRoot)) return;

        foreach (var deviceDir in Directory.GetDirectories(HwmonRoot).OrderBy(p => p, StringComparer.Ordinal))
        {
            var chipName = NormalizeName(SafeReadText(Path.Combine(deviceDir, "name")));

            foreach (var inputPath in Directory.GetFiles(deviceDir, "temp*_input").OrderBy(p => p, StringComparer.Ordinal))
            {
                var raw = SafeReadText(inputPath)?.Trim();
                if (raw is null || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var millidegrees))
                    continue;

                var celsius = millidegrees / 1000.0;
                if (celsius < -100 || celsius > 200) continue;

                var labelPath = Regex.Replace(inputPath, "_input$", "_label", RegexOptions.IgnoreCase);
                var label = NormalizeName(SafeReadText(labelPath));
                var name = FirstPresent(label, chipName, Path.GetFileName(deviceDir));

                readings.Add(new SensorReading(name, "Temperature", Math.Round(celsius, 1), "°C"));
            }
        }
    }

    private static void ReadHwmonFanSpeeds(List<SensorReading> readings)
    {
        if (!Directory.Exists(HwmonRoot)) return;

        foreach (var deviceDir in Directory.GetDirectories(HwmonRoot).OrderBy(p => p, StringComparer.Ordinal))
        {
            var chipName = NormalizeName(SafeReadText(Path.Combine(deviceDir, "name")));

            foreach (var inputPath in Directory.GetFiles(deviceDir, "fan*_input").OrderBy(p => p, StringComparer.Ordinal))
            {
                var raw = SafeReadText(inputPath)?.Trim();
                if (raw is null || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rpm))
                    continue;

                if (rpm < 0 || rpm > 100_000) continue;

                var labelPath = Regex.Replace(inputPath, "_input$", "_label", RegexOptions.IgnoreCase);
                var label = NormalizeName(SafeReadText(labelPath));
                var name = FirstPresent(label, chipName, Path.GetFileName(deviceDir));

                readings.Add(new SensorReading(name, "Fan", rpm, "RPM"));
            }
        }
    }

    private static void ReadCpuUsage(List<SensorReading> readings)
    {
        try
        {
            var stat1 = ReadProcStatCpu();
            if (stat1 is null) return;
            Thread.Sleep(100);
            var stat2 = ReadProcStatCpu();
            if (stat2 is null) return;

            var idleDelta = stat2.Value.Idle - stat1.Value.Idle;
            var totalDelta = stat2.Value.Total - stat1.Value.Total;
            if (totalDelta <= 0) return;

            var usage = (1.0 - (double)idleDelta / totalDelta) * 100.0;
            readings.Add(new SensorReading("CPU Total", "Usage", Math.Round(usage, 1), "%"));
        }
        catch
        {
            // Silently ignore failures
        }
    }

    private static void ReadMemoryUsage(List<SensorReading> readings)
    {
        try
        {
            var meminfo = SafeReadText("/proc/meminfo");
            if (meminfo is null) return;

            long? totalKb = null;
            long? availableKb = null;

            foreach (var line in meminfo.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var sep = line.IndexOf(':');
                if (sep < 0) continue;
                var key = line[..sep].Trim();
                var valueText = line[(sep + 1)..].Trim();
                var numericMatch = Regex.Match(valueText, @"(\d+)");
                if (!numericMatch.Success || !long.TryParse(numericMatch.Value, out var kb)) continue;

                if (key.Equals("MemTotal", StringComparison.OrdinalIgnoreCase))
                    totalKb = kb;
                else if (key.Equals("MemAvailable", StringComparison.OrdinalIgnoreCase))
                    availableKb = kb;
            }

            if (totalKb is not null && availableKb is not null && totalKb.Value > 0)
            {
                var usedPct = (1.0 - (double)availableKb.Value / totalKb.Value) * 100.0;
                readings.Add(new SensorReading("Memory", "Usage", Math.Round(usedPct, 1), "%"));
            }
        }
        catch
        {
            // Silently ignore failures
        }
    }

    private static (long Idle, long Total)? ReadProcStatCpu()
    {
        var text = SafeReadText("/proc/stat");
        if (text is null) return null;

        var firstLine = text.Split('\n', 2)[0].Trim();
        if (!firstLine.StartsWith("cpu ", StringComparison.Ordinal)) return null;

        var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return null;

        var values = parts.Skip(1).Select(p => long.TryParse(p, out var v) ? v : 0L).ToArray();
        var total = values.Sum();
        var idle = values.Length > 3 ? values[3] : 0L;
        return (idle, total);
    }

    private static string? SafeReadText(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : null; }
        catch { return null; }
    }

    private static string NormalizeName(string? value) =>
        Regex.Replace((value ?? string.Empty).Trim().Replace('_', ' '), @"\s+", " ").Trim();

    private static string FirstPresent(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}
