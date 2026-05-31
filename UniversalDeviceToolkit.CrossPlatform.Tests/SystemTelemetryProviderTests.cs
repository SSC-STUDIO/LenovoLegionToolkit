using FluentAssertions;
using System.Text.RegularExpressions;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class SystemTelemetryProviderTests
{
    [Fact]
    public void LinuxProvider_ShouldReadCpuMemoryAndHwmonTemperatures()
    {
        var fileSystem = new FakeFileSystem(new Dictionary<string, string>
        {
            ["/proc/cpuinfo"] = """
                processor   : 0
                model name  : AMD Ryzen 7 7840U w/ Radeon 780M Graphics
                """,
            ["/proc/meminfo"] = """
                MemTotal:       32768000 kB
                MemAvailable:   12345678 kB
                """,
            ["/sys/class/hwmon/hwmon0/name"] = "k10temp\n",
            ["/sys/class/hwmon/hwmon0/temp1_input"] = "54125\n",
            ["/sys/class/hwmon/hwmon0/temp1_label"] = "Tctl\n",
            ["/sys/class/hwmon/hwmon1/name"] = "nvme\n",
            ["/sys/class/hwmon/hwmon1/temp1_input"] = "42100\n",
        });

        var telemetry = new LinuxSystemTelemetryProvider(fileSystem).Read();

        telemetry.Source.Should().Be("linux-procfs-sysfs");
        telemetry.CpuModel.Should().Be("AMD Ryzen 7 7840U w/ Radeon 780M Graphics");
        telemetry.LogicalProcessorCount.Should().Be(Environment.ProcessorCount);
        telemetry.MemoryTotalGiB.Should().Be(31.25);
        telemetry.MemoryAvailableGiB.Should().Be(11.77);
        telemetry.Temperatures.Should().BeEquivalentTo(
        [
            new TemperatureReading("Tctl", 54.1, "linux-hwmon"),
            new TemperatureReading("nvme", 42.1, "linux-hwmon")
        ]);
        telemetry.Notes.Should().BeEmpty();
    }

    [Fact]
    public void LinuxProvider_WhenNoTemperatures_ShouldReturnNote()
    {
        var fileSystem = new FakeFileSystem(new Dictionary<string, string>
        {
            ["/proc/meminfo"] = "MemTotal: 1048576 kB\n"
        });

        var telemetry = new LinuxSystemTelemetryProvider(fileSystem).Read();

        telemetry.Temperatures.Should().BeEmpty();
        telemetry.Notes.Should().ContainSingle(note => note.Contains("No readable hwmon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MacProvider_ShouldReadSysctlCpuAndMemory()
    {
        var runner = new FakeCommandRunner(new Dictionary<string, string>
        {
            ["sysctl -n machdep.cpu.brand_string"] = "Apple M3 Pro\n",
            ["sysctl -n hw.logicalcpu"] = "12\n",
            ["sysctl -n hw.memsize"] = "19327352832\n"
        });

        var telemetry = new MacSystemTelemetryProvider(runner).Read();

        telemetry.Source.Should().Be("macos-sysctl");
        telemetry.CpuModel.Should().Be("Apple M3 Pro");
        telemetry.LogicalProcessorCount.Should().Be(12);
        telemetry.MemoryTotalGiB.Should().Be(18);
        telemetry.MemoryAvailableGiB.Should().BeNull();
        telemetry.Temperatures.Should().BeEmpty();
        telemetry.Notes.Should().ContainSingle(note => note.Contains("SMC", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeFileSystem(IReadOnlyDictionary<string, string> files) : IFileSystem
    {
        public string ReadAllText(string path) => files.TryGetValue(path, out var value) ? value : string.Empty;

        public IEnumerable<string> EnumerateDirectories(string path) =>
            files.Keys
                .Where(file => file.StartsWith(path.TrimEnd('/') + "/", StringComparison.Ordinal))
                .Select(file =>
                {
                    var relativePath = file[(path.TrimEnd('/').Length + 1)..];
                    var separator = relativePath.IndexOf('/');
                    return separator < 0 ? string.Empty : $"{path.TrimEnd('/')}/{relativePath[..separator]}";
                })
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .Distinct(StringComparer.Ordinal);

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            var regex = new Regex(
                "^" + Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
                RegexOptions.IgnoreCase);

            return files.Keys
                .Where(file => file.StartsWith(path.TrimEnd('/') + "/", StringComparison.Ordinal))
                .Where(file => !file[(path.TrimEnd('/').Length + 1)..].Contains('/'))
                .Where(file => regex.IsMatch(Path.GetFileName(file)));
        }
    }

    private sealed class FakeCommandRunner(IReadOnlyDictionary<string, string> outputs) : ICommandRunner
    {
        public string Run(string fileName, params string[] arguments)
        {
            var key = string.Join(' ', new[] { fileName }.Concat(arguments));
            return outputs.TryGetValue(key, out var value) ? value : string.Empty;
        }
    }
}
