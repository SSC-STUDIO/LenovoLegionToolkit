using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class DeviceSupportStatusTests
{
    private readonly CrossPlatformDeviceSupportEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ShouldMatchAppleMacBasicPack()
    {
        var support = _evaluator.Evaluate(
            new HardwareIdentity("Apple Inc.", "MacBook Pro MacBookPro18,3", "MacBook Pro", "MAC-SERIAL", "test"),
            isWindows: false);

        support.DevicePackId.Should().Be("apple-mac-basic");
        support.DisplayName.Should().Be("Apple Mac Basic");
        support.SupportLevel.Should().Be("Safe basic mode");
        support.IsHardwareControlAvailable.Should().BeFalse();
        support.HiddenFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Fact]
    public void Evaluate_ShouldMatchFrameworkAlias()
    {
        var support = _evaluator.Evaluate(
            new HardwareIdentity("Framework Computer Inc.", "Framework Laptop 16 A8", "Framework Laptop 16", "SERIAL", "test"),
            isWindows: false);

        support.DevicePackId.Should().Be("framework-basic");
        support.EnabledFeatures.Should().Contain("read-only-telemetry");
    }

    [Fact]
    public void Evaluate_ShouldMatchLenovoLegionButStayInBasicMode()
    {
        var support = _evaluator.Evaluate(
            new HardwareIdentity("LENOVO", "Legion Pro 7 16IRX9", "Legion Pro 7", "SERIAL", "test"),
            isWindows: false);

        support.DevicePackId.Should().Be("lenovo-legion-basic");
        support.IsHardwareControlAvailable.Should().BeFalse();
        support.HiddenFeatures.Should().Contain("power-modes");
        support.Reason.Should().MatchRegex("(?i)disabled");
    }

    [Fact]
    public void Evaluate_OnWindowsMatch_ShouldStillUseCrossPlatformBasicPack()
    {
        var support = _evaluator.Evaluate(
            new HardwareIdentity("LENOVO", "Legion Pro 7 16IRX9", "Legion Pro 7", "SERIAL", "test"),
            isWindows: true);

        support.DevicePackId.Should().Be("lenovo-legion-basic");
        support.IsHardwareControlAvailable.Should().BeFalse();
        support.Reason.Should().MatchRegex("(?i)Windows desktop app");
    }

    [Fact]
    public void Evaluate_WhenVendorUnknown_ShouldReturnGenericBasicMode()
    {
        var support = _evaluator.Evaluate(HardwareIdentity.Unknown("test"), isWindows: false);

        support.DevicePackId.Should().Be("generic-pc-basic");
        support.DisplayName.Should().Be("Generic PC Basic");
        support.EnabledFeatures.Should().Contain("safe-basic-mode");
        support.HiddenFeatures.Should().Contain("plugin-runtime");
    }
}
