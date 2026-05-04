using System;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using Xunit;

namespace LenovoLegionToolkit.Tests.Controllers;

[Trait("Category", TestCategories.Controller)]
public class RGBKeyboardBacklightControllerTests : UnitTestBase
{
    private RGBKeyboardSettings _settings = null!;
    private VantageDisabler _vantageDisabler = null!;
    private RGBKeyboardBacklightController _controller = null!;

    protected override void Setup()
    {
        _settings = new RGBKeyboardSettings();
        _vantageDisabler = new VantageDisabler();
        _controller = new RGBKeyboardBacklightController(_settings, _vantageDisabler);
    }

    protected override void Cleanup()
    {
        _controller = null!;
        _settings = null!;
        _vantageDisabler = null!;
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        _controller.Should().NotBeNull();
    }

    [Fact]
    public void ForceDisable_DefaultValue_ShouldBeFalse()
    {
        _controller.ForceDisable.Should().BeFalse();
    }

    [Fact]
    public async Task IsSupportedAsync_WhenForceDisable_ShouldReturnFalse()
    {
        _controller.ForceDisable = true;

        var result = await _controller.IsSupportedAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetStateAsync_WhenForceDisable_ShouldThrowInvalidOperationException()
    {
        _controller.ForceDisable = true;

        var act = async () => await _controller.GetStateAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RGB Keyboard unsupported*");
    }

    [Fact]
    public async Task SetStateAsync_WhenForceDisable_ShouldThrowInvalidOperationException()
    {
        _controller.ForceDisable = true;

        var act = async () => await _controller.SetStateAsync(default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RGB Keyboard unsupported*");
    }

    [Fact]
    public async Task SetPresetAsync_WhenForceDisable_ShouldThrowInvalidOperationException()
    {
        _controller.ForceDisable = true;

        var act = async () => await _controller.SetPresetAsync(default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RGB Keyboard unsupported*");
    }

    [Fact]
    public async Task SetNextPresetAsync_WhenForceDisable_ShouldThrowInvalidOperationException()
    {
        _controller.ForceDisable = true;

        var act = async () => await _controller.SetNextPresetAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RGB Keyboard unsupported*");
    }

    [Fact]
    public async Task SetLightControlOwnerAsync_WhenForceDisable_ShouldNotThrow()
    {
        _controller.ForceDisable = true;

        var act = async () => await _controller.SetLightControlOwnerAsync(true);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void ForceDisable_SetAndGet_ShouldReturnCorrectValue()
    {
        _controller.ForceDisable = true;
        _controller.ForceDisable.Should().BeTrue();

        _controller.ForceDisable = false;
        _controller.ForceDisable.Should().BeFalse();
    }
}
