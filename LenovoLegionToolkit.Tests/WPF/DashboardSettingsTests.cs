using System.Linq;
using System.Reflection;
using FluentAssertions;
using LenovoLegionToolkit.WPF;
using LenovoLegionToolkit.WPF.Settings;
using Xunit;

namespace LenovoLegionToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class DashboardSettingsTests
{
    [Fact]
    public void Normalize_WhenLegacySchemaHasHiddenSensorsAndMissingPowerMode_ShouldRepairDashboard()
    {
        var store = new DashboardSettings.DashboardSettingsStore
        {
            SchemaVersion = 0,
            ShowSensors = false,
            SensorsRefreshIntervalSeconds = 1,
            Groups =
            [
                new DashboardGroup(DashboardGroupType.Power, null,
                    DashboardItem.BatteryMode,
                    DashboardItem.BatteryNightChargeMode),
                new DashboardGroup(DashboardGroupType.Graphics, null,
                    DashboardItem.HybridMode)
            ]
        };

        var normalized = Normalize(store);

        normalized.SchemaVersion.Should().Be(4);
        normalized.ShowSensors.Should().BeTrue();
        normalized.Groups.Should().NotBeNull();
        normalized.Groups!.SelectMany(group => group.Items).Should().Contain(DashboardItem.PowerMode);
        normalized.Groups.SelectMany(group => group.Items).Should().Contain(DashboardItem.ItsMode);
        normalized.Groups.First(group => group.Type == DashboardGroupType.Power).Items.First().Should().Be(DashboardItem.PowerMode);
    }

    [Fact]
    public void Normalize_WhenPreviousCurrentSchemaOmitsPowerMode_ShouldRepairDashboard()
    {
        var store = new DashboardSettings.DashboardSettingsStore
        {
            SchemaVersion = 3,
            ShowSensors = false,
            SensorsRefreshIntervalSeconds = 1,
            Groups =
            [
                new DashboardGroup(DashboardGroupType.Power, null,
                    DashboardItem.BatteryMode,
                    DashboardItem.BatteryNightChargeMode)
            ]
        };

        var normalized = Normalize(store);

        normalized.SchemaVersion.Should().Be(4);
        normalized.ShowSensors.Should().BeFalse();
        normalized.Groups.Should().NotBeNull();
        normalized.Groups!.SelectMany(group => group.Items).Should().Contain(DashboardItem.PowerMode);
        normalized.Groups.SelectMany(group => group.Items).Should().Contain(DashboardItem.ItsMode);
    }

    [Fact]
    public void Normalize_WhenGroupsAreNull_ShouldFallbackToDefaultGroups()
    {
        var store = new DashboardSettings.DashboardSettingsStore
        {
            SchemaVersion = 3,
            ShowSensors = true,
            SensorsRefreshIntervalSeconds = 1,
            Groups = null
        };

        var normalized = Normalize(store);

        normalized.Groups.Should().NotBeNull();
        normalized.Groups!.SelectMany(group => group.Items).Should().Contain(DashboardItem.PowerMode);
        normalized.Groups.SelectMany(group => group.Items).Should().Contain(DashboardItem.ItsMode);
    }

    [Fact]
    public void Normalize_WhenLegacySchemaIsMissingItsMode_ShouldRestoreItsModeIntoPowerGroup()
    {
        var store = new DashboardSettings.DashboardSettingsStore
        {
            SchemaVersion = 0,
            ShowSensors = false,
            SensorsRefreshIntervalSeconds = 1,
            Groups =
            [
                new DashboardGroup(DashboardGroupType.Power, null,
                    DashboardItem.BatteryMode,
                    DashboardItem.BatteryNightChargeMode),
                new DashboardGroup(DashboardGroupType.Graphics, null,
                    DashboardItem.HybridMode)
            ]
        };

        var normalized = Normalize(store);

        normalized.SchemaVersion.Should().Be(4);
        normalized.ShowSensors.Should().BeTrue();
        normalized.Groups.Should().NotBeNull();
        normalized.Groups!.SelectMany(group => group.Items).Should().Contain(DashboardItem.ItsMode);
        normalized.Groups.First(group => group.Type == DashboardGroupType.Power).Items.Should().Contain(DashboardItem.ItsMode);
    }

    [Fact]
    public void Normalize_WhenCurrentSchemaAlreadyContainsPowerAndItsMode_ShouldPreserveCustomization()
    {
        var store = new DashboardSettings.DashboardSettingsStore
        {
            SchemaVersion = 4,
            ShowSensors = false,
            SensorsRefreshIntervalSeconds = 1,
            Groups =
            [
                new DashboardGroup(DashboardGroupType.Power, null,
                    DashboardItem.PowerMode,
                    DashboardItem.ItsMode,
                    DashboardItem.BatteryMode)
            ]
        };

        var normalized = Normalize(store);

        normalized.SchemaVersion.Should().Be(4);
        normalized.ShowSensors.Should().BeFalse();
        normalized.Groups.Should().BeEquivalentTo(store.Groups);
    }

    private static DashboardSettings.DashboardSettingsStore Normalize(DashboardSettings.DashboardSettingsStore store)
    {
        var method = typeof(DashboardSettings).GetMethod("Normalize", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var normalized = method!.Invoke(null, [store]);

        normalized.Should().BeOfType<DashboardSettings.DashboardSettingsStore>();
        return (DashboardSettings.DashboardSettingsStore)normalized!;
    }
}
