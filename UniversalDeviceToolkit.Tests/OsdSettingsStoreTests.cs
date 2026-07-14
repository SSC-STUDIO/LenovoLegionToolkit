using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class OsdSettingsStoreTests
{
    [Fact]
    public void Defaults_ShowOsd_ShouldBeFalse()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.ShowOsd.Should().BeFalse();
    }

    [Fact]
    public void Defaults_OsdRefreshInterval_ShouldBeOne()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.OsdRefreshInterval.Should().Be(1);
    }

    [Fact]
    public void Defaults_SelectedStyleIndex_ShouldBeZero()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.SelectedStyleIndex.Should().Be(0);
    }

    [Fact]
    public void Defaults_Items_ShouldContainAllOsdItems()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.Items.Should().NotBeNull();
        store.Items.Should().HaveCount(Enum.GetValues<OsdItem>().Length);
    }

    [Fact]
    public void Defaults_BackgroundOpacity_ShouldBe0Point6()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.BackgroundColor.Should().Be("#1E1E1E");
        store.BackgroundOpacity.Should().Be(0.6);
    }

    [Fact]
    public void Defaults_FontSize_ShouldBe12()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.FontSize.Should().Be(12);
    }

    [Fact]
    public void Defaults_CornerRadius_ShouldBe6()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.CornerRadiusTop.Should().Be(6);
        store.CornerRadiusBottom.Should().Be(6);
    }

    [Fact]
    public void Defaults_IsLocked_ShouldBeFalse()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void Defaults_Positions_ShouldBeNull()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.PanelPositionX.Should().BeNull();
        store.PanelPositionY.Should().BeNull();
        store.BarPositionX.Should().BeNull();
        store.BarPositionY.Should().BeNull();
    }

    [Fact]
    public void Defaults_Thresholds_ShouldHaveReasonableValues()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.TempThresholdWarning.Should().Be(75);
        store.TempThresholdCritical.Should().Be(90);
        store.UsageThresholdWarning.Should().Be(70);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var store = new OsdSettings.OsdSettingsStore();
        store.ShowOsd = true;
        store.ShowOsd.Should().BeTrue();
        store.FontSize = 16;
        store.FontSize.Should().Be(16);
        store.IsLocked = true;
        store.IsLocked.Should().BeTrue();
    }
}