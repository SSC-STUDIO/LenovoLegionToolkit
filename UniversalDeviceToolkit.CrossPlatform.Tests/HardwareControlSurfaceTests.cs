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
        var deviceSupport = new DeviceSupportStatus(
            "Basic",
            "framework-basic",
            "Framework Basic",
            ["plugins"],
            ["vendor-hardware-controls"],
            "test");

        var surface = new HardwareControlSurfaceReader(powerProfile, plugins, deviceSupport).Read();

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
        var runner = new FakeCommandRunner(new Dictionary<string, CommandResult>
        {
            ["powerprofilesctl set performance"] = new(0, "", "")
        });

        var result = new HardwareControlSurfaceWriter(runner, CrossPlatformControlPlatform.Linux).Set("power_profile", "performance");

        result.Succeeded.Should().BeTrue();
        result.ControlId.Should().Be("power-profile");
        result.Value.Should().Be("performance");
        runner.Commands.Should().ContainSingle().Which.Should().Be("powerprofilesctl set performance");
    }

    [Fact]
    public void Writer_WhenControlIsNotWritable_ShouldFailBeforeRunningCommand()
    {
        var runner = new FakeCommandRunner(new Dictionary<string, CommandResult>());

        var result = new HardwareControlSurfaceWriter(runner).Set("vendor-hardware-controls", "performance");

        result.Succeeded.Should().BeFalse();
        result.Detail.Should().Contain("Only the standard power-profile control");
        runner.Commands.Should().BeEmpty();
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
