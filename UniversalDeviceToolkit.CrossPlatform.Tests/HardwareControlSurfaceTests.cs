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

        var surface = new HardwareControlSurfaceReader(powerProfile, brightness, plugins, deviceSupport).Read();

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
        result.Detail.Should().Contain("Only standard power-profile and display-brightness controls");
        runner.Commands.Should().BeEmpty();
    }

    private sealed class FakeFileSystem(IReadOnlyDictionary<string, string> files) : IFileSystem
    {
        public string ReadAllText(string path) => files.TryGetValue(path, out var value) ? value : string.Empty;

        public IEnumerable<string> EnumerateDirectories(string path) => [];

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => [];

        public bool DirectoryExists(string path) => false;

        public string GetFileName(string path) => Path.GetFileName(path);
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
