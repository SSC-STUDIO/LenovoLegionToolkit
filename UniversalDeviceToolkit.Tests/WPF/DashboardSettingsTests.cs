using System.Linq;
using System.Reflection;
using FluentAssertions;
using UniversalDeviceToolkit.WPF;
using UniversalDeviceToolkit.WPF.Settings;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

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
    
    [Fact]
    public void Normalize_WhenGroupItemsAreNull_ShouldRepairItemsAndAvoidException()
    {
        // Create a store with groups that have null Items arrays - this reproduces the bug
        var store = new DashboardSettings.DashboardSettingsStore
        {
            SchemaVersion = 4,
            ShowSensors = true,
            SensorsRefreshIntervalSeconds = 1,
            Groups =
            [
                // Use reflection or manual construction to simulate deserialization with null items
                CreateDashboardGroupWithNullItems(DashboardGroupType.Power, null),
                new DashboardGroup(DashboardGroupType.Graphics, null, DashboardItem.HybridMode)
            ]
        };

        // This should not throw!
        var normalized = Normalize(store);

        normalized.SchemaVersion.Should().Be(4);
        normalized.Groups.Should().NotBeNull();
        normalized.Groups!.SelectMany(group => group.Items).Should().Contain(DashboardItem.PowerMode);
        normalized.Groups.SelectMany(group => group.Items).Should().Contain(DashboardItem.ItsMode);
        normalized.Groups.Should().NotContain(group => group.Items == null);
    }

    private static DashboardGroup CreateDashboardGroupWithNullItems(DashboardGroupType type, string? customName)
    {
        // Create a DashboardGroup instance with null Items by using reflection
        var constructor = typeof(DashboardGroup).GetConstructors().First();
        
        // Create a new instance, passing null as items
        return (DashboardGroup)constructor.Invoke([type, customName, null!]);
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
