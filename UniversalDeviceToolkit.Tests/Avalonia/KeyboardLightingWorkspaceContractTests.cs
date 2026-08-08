using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class KeyboardLightingWorkspaceContractTests
{
    [Fact]
    public async Task UnavailableHost_ReportsNoKeyboardAndRejectsEveryMutation()
    {
        var services = new UnavailablePlatformServices();
        var spectrum = new KeyboardLightingUpdate(
            "Spectrum",
            SelectedProfile: 2,
            Brightness: 5,
            LogoEnabled: true,
            SpectrumEffects:
            [
                new KeyboardSpectrumEffectState(
                    "Always",
                    "None",
                    "None",
                    "None",
                    [new KeyboardColorState(1, 2, 3)],
                    [0x01]),
            ]);
        var rgb = new KeyboardLightingUpdate(
            "RGB",
            RgbPreset: "Custom",
            RgbEffect: "Static",
            RgbSpeed: "Slow",
            RgbBrightness: "High",
            RgbZones: [new KeyboardColorState(4, 5, 6)]);

        (await services.GetKeyboardLightingStateAsync()).Should().BeNull();
        (await services.SetKeyboardLightingAsync(spectrum)).Should().BeFalse();
        (await services.SetKeyboardLightingAsync(rgb)).Should().BeFalse();
        (await services.ResetKeyboardSpectrumProfileAsync()).Should().BeFalse();
        (await services.ExportKeyboardSpectrumProfileAsync("profile.json")).Should().BeFalse();
        (await services.ImportKeyboardSpectrumProfileAsync("profile.json")).Should().BeFalse();
    }

    [Fact]
    public void KeyboardLightingState_ExposesExplicitVantageBlockedCapability()
    {
        var state = new KeyboardLightingState(
            "Spectrum",
            4,
            true,
            1,
            [],
            [],
            IsBlockedByVantage: true);

        state.IsBlockedByVantage.Should().BeTrue();
        state.Mode.Should().Be("Spectrum");
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(5.9, 5)]
    [InlineData(9, 9)]
    [InlineData(12, 9)]
    public void SpectrumBrightnessChange_ProducesAnImmediateClampedControllerUpdate(double input, int expected)
    {
        var update = KeyboardBacklightPage.CreateSpectrumBrightnessUpdate(input);

        update.Mode.Should().Be("Spectrum");
        update.Brightness.Should().Be(expected);
    }

    [Fact]
    public void KeyboardPage_UsesTheSharedLightingViewModelForControllerMutations()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml.cs"));

        source.Should().Contain("KeyboardBacklightViewModel");
        source.Should().Contain("LoadWorkspaceAsync");
        source.Should().Contain("_viewModel.ApplyAsync");
        source.Should().Contain("_viewModel.ResetSpectrumProfileAsync");
        source.Should().Contain("_viewModel.ImportSpectrumProfileAsync");
    }
}
