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
    public void Evaluate_ShouldMatchAlienwareVendorAlias()
    {
        var support = _evaluator.Evaluate(
            new HardwareIdentity("Alienware", "Alienware m18 R2", "m18 R2", "SERIAL", "test"),
            isWindows: false);

        support.DevicePackId.Should().Be("dell-basic");
        support.DisplayName.Should().Be("Dell Basic");
        support.SupportLevel.Should().Be("Safe basic mode");
        support.IsHardwareControlAvailable.Should().BeFalse();
    }

    [Theory]
    [InlineData("TIMI", "Redmi G Pro 2024", "xiaomi-basic")]
    [InlineData("realme", "realme Book Prime", "realme-basic")]
    [InlineData("Infinix Mobility Limited", "INBook X2", "infinix-basic")]
    [InlineData("Gateway", "Gateway 14.1 Ultra Slim", "gateway-basic")]
    [InlineData("CHUWI", "CoreBook X", "chuwi-basic")]
    [InlineData("TECLAST", "F15 Plus", "teclast-basic")]
    [InlineData("Jumper", "EZbook X3", "jumper-basic")]
    [InlineData("Mechanical Revolution", "Kuangshi 16 Super", "mechrevo-basic")]
    [InlineData("Hasee", "ZhanShen Z8", "hasee-basic")]
    [InlineData("THUNDEROBOT", "911 Zero", "thunderobot-basic")]
    [InlineData("MACHENIKE", "F117", "machenike-basic")]
    [InlineData("COLORFUL", "Evol X15", "colorful-basic")]
    [InlineData("MAIBENBEN", "MaiBook X", "maibenben-basic")]
    [InlineData("Valve Corporation", "Steam Deck", "valve-handheld-basic")]
    [InlineData("GPD", "GPD WIN 4", "gpd-handheld-basic")]
    [InlineData("AYANEO", "AYANEO NEXT", "ayaneo-handheld-basic")]
    [InlineData("ONEXPLAYER", "OneXPlayer 2 Pro", "one-netbook-handheld-basic")]
    [InlineData("AZW", "Beelink SER8", "beelink-basic")]
    public void Evaluate_ShouldMatchExpandedBrandBasicPacks(string vendor, string model, string expectedPackId)
    {
        var support = _evaluator.Evaluate(
            new HardwareIdentity(vendor, model, model, "SERIAL", "test"),
            isWindows: false);

        support.DevicePackId.Should().Be(expectedPackId);
        support.SupportLevel.Should().Be("Safe basic mode");
        support.EnabledFeatures.Should().Contain(["diagnostics", "hardware-identity", "read-only-telemetry"]);
        support.HiddenFeatures.Should().Contain(["lenovo-hardware-controls", "power-modes"]);
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
