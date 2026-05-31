using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class HardwareControlSurfaceTests
{
    [Fact]
    public void Read_ShouldExposeWritablePowerProfileAndHiddenVendorControls()
    {
        var powerProfile = new PowerProfileStatus(
            "linux-powerprofilesctl",
            "balanced",
            [
                new PowerProfileOption("power-saver", "Power saver", false),
                new PowerProfileOption("balanced", "Balanced", true),
                new PowerProfileOption("performance", "Performance", false)
            ],
            true,
            []);
        var plugins = new PluginDiscoveryReport(
            "test",
            [],
            [new PluginDescriptor("cross", "Cross", "1.0.0", "/plugins/cross/plugin.json", true, true, 1, ["linux"], "test")],
            []);
        var cpuGovernor = new CpuGovernorStatus(
            "linux-cpufreq",
            "schedutil",
            [new CpuGovernorPolicy("policy0", "schedutil", ["performance", "powersave", "schedutil"], "/sys/devices/system/cpu/cpufreq/policy0/scaling_governor", "linux-cpufreq")],
            [
                new CpuGovernorOption("performance", "Performance", false),
                new CpuGovernorOption("powersave", "Power save", false),
                new CpuGovernorOption("schedutil", "Schedutil", true)
            ],
            true,
            []);
        var brightness = new DisplayBrightnessStatus(
            "linux-backlight",
            [new DisplayBrightnessDevice("intel_backlight", "intel_backlight", 480, 960, 50, "/sys/class/backlight/intel_backlight/brightness", "linux-backlight")],
            []);
        var deviceSupport = new DeviceSupportStatus(
            "Basic",
            "framework-basic",
            "Framework Basic",
            ["plugins"],
            ["vendor-hardware-controls"],
            "test");

        var surface = new HardwareControlSurfaceReader(powerProfile, cpuGovernor, brightness, plugins, deviceSupport).Read();

        surface.Controls.Should().ContainEquivalentOf(new HardwareControlDescriptor(
            "power-profile",
            "Platform power profile",
            "standard-os",
            true,
            true,
            "balanced",
            [
                new HardwareControlOption("power-saver", "Power saver", false),
                new HardwareControlOption("balanced", "Balanced", true),
                new HardwareControlOption("performance", "Performance", false)
            ],
            "Set through Linux powerprofilesctl or macOS pmset where available."));
        surface.Controls.Should().Contain(control =>
            control.Id == "cpu-governor" &&
            control.IsAvailable &&
            control.IsWritable &&
            control.CurrentValue == "schedutil");
        surface.Controls.Should().Contain(control =>
            control.Id == "display-brightness" &&
            control.IsAvailable &&
            control.IsWritable &&
            control.CurrentValue == "50%");
        surface.Controls.Should().Contain(control =>
            control.Id == "plugin-manifests" &&
            control.IsAvailable &&
            !control.IsWritable &&
            control.Detail.Contains("1 cross-platform", StringComparison.OrdinalIgnoreCase));
        surface.Controls.Should().Contain(control =>
            control.Id == "vendor-hardware-controls" &&
            !control.IsAvailable &&
            !control.IsWritable &&
            control.CurrentValue == "hidden");
    }

    [Fact]
    public void Writer_WhenControlIsCpuGovernor_ShouldDelegateToCpuGovernorWriter()
    {
        var fileSystem = new FakeFileSystem(new Dictionary<string, string>
        {
            ["/sys/devices/system/cpu/cpufreq/policy0/scaling_governor"] = "powersave\n",
            ["/sys/devices/system/cpu/cpufreq/policy0/scaling_available_governors"] = "performance powersave\n"
        });
        var runner = new FakeCommandRunner(new Dictionary<string, CommandResult>
        {
            ["sh -c printf %s 'performance' > '/sys/devices/system/cpu/cpufreq/policy0/scaling_governor'"] = new(0, "", "")
        });

        var result = new HardwareControlSurfaceWriter(fileSystem, runner, CrossPlatformControlPlatform.Linux).Set("cpu_governor", "performance");

        result.Succeeded.Should().BeTrue();
        result.ControlId.Should().Be("cpu-governor");
        result.Value.Should().Be("performance");
        runner.Commands.Should().ContainSingle().Which.Should().Be("sh -c printf %s 'performance' > '/sys/devices/system/cpu/cpufreq/policy0/scaling_governor'");
    }

    [Fact]
    public void Writer_WhenControlIsPowerProfile_ShouldDelegateToPowerProfileWriter()
    {
        var fileSystem = new FakeFileSystem(new Dictionary<string, string>());
        var runner = new FakeCommandRunner(new Dictionary<string, CommandResult>
        {
            ["powerprofilesctl set performance"] = new(0, "", "")
        });

        var result = new HardwareControlSurfaceWriter(fileSystem, runner, CrossPlatformControlPlatform.Linux).Set("power_profile", "performance");

        result.Succeeded.Should().BeTrue();
        result.ControlId.Should().Be("power-profile");
        result.Value.Should().Be("performance");
        runner.Commands.Should().ContainSingle().Which.Should().Be("powerprofilesctl set performance");
    }

    [Fact]
    public void Writer_WhenControlIsNotWritable_ShouldFailBeforeRunningCommand()
    {
        var runner = new FakeCommandRunner(new Dictionary<string, CommandResult>());

        var result = new HardwareControlSurfaceWriter(new FakeFileSystem(new Dictionary<string, string>()), runner).Set("vendor-hardware-controls", "performance");

        result.Succeeded.Should().BeFalse();
        result.Detail.Should().Contain("Only standard power-profile, cpu-governor, and display-brightness controls");
        runner.Commands.Should().BeEmpty();
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

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => [];

        public bool DirectoryExists(string path) =>
            files.Keys.Any(file => file.StartsWith(path.TrimEnd('/') + "/", StringComparison.Ordinal));

        public string GetFileName(string path) => path.TrimEnd('/').Split('/').Last();
    }

    private sealed class FakeCommandRunner(IReadOnlyDictionary<string, CommandResult> results) : ICommandResultRunner
    {
        public List<string> Commands { get; } = [];

        public string Run(string fileName, params string[] arguments)
        {
            var result = RunResult(fileName, arguments);
            return result.Succeeded ? result.StandardOutput : string.Empty;
        }

        public CommandResult RunResult(string fileName, params string[] arguments)
        {
            var key = string.Join(' ', new[] { fileName }.Concat(arguments));
            Commands.Add(key);
            return results.TryGetValue(key, out var result) ? result : new CommandResult(1, "", "not found");
        }
    }
}
