using System.Text.Json;
using FluentAssertions;
using UniversalDeviceToolkit.Shared.Settings;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class DashboardPreferencesTests
{
    [Fact]
    public void DashboardPreferenceStore_DefaultsToVisibleSensorsAndTwoSecondPolling()
    {
        var store = new AvaloniaDashboardPreferenceStore();

        store.ShowSensors.Should().BeTrue();
        store.SensorsRefreshIntervalSeconds.Should().Be(2);
    }

    [Fact]
    public void DashboardPreferenceStore_PreservesUserSelections()
    {
        var store = new AvaloniaDashboardPreferenceStore
        {
            ShowSensors = false,
            SensorsRefreshIntervalSeconds = 15,
        };

        store.ShowSensors.Should().BeFalse();
        store.SensorsRefreshIntervalSeconds.Should().Be(15);
    }

    [Fact]
    public void DashboardPreferenceStore_DefaultsToTheWpfDashboardGroups()
    {
        var store = new AvaloniaDashboardPreferenceStore();

        store.Groups.Select(group => group.Type).Should().Equal("Power", "Graphics", "Display", "Other");
        store.Groups[0].Items.Should().ContainInOrder("PowerMode", "ItsMode", "BatteryMode");
        store.Groups[3].Items.Should().Contain("WinKeyLock");
    }

    [Fact]
    public void DashboardPreferences_ImportsSensorVisibilityFromWpfStore()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "udt-dashboard-preferences-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            File.WriteAllText(
                Path.Combine(tempRoot, "dashboard.json"),
                JsonSerializer.Serialize(new { ShowSensors = false, SensorsRefreshIntervalSeconds = 1 }));

            var preferences = new AvaloniaDashboardPreferences(tempRoot);

            preferences.Store.ShowSensors.Should().BeFalse();
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    [Fact]
    public void DashboardPreferences_ImportsAndNormalizesWpfGroups()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "udt-dashboard-groups-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            File.WriteAllText(
                Path.Combine(tempRoot, "dashboard.json"),
                """
                {
                  "ShowSensors": true,
                  "SensorsRefreshIntervalSeconds": 999,
                  "Groups": [
                    { "Type": "Custom", "CustomName": "Work", "Items": ["PowerMode", "PowerMode", "  "] },
                    { "Type": "", "Items": ["Invalid"] }
                  ]
                }
                """);

            var preferences = new AvaloniaDashboardPreferences(tempRoot);

            preferences.Store.SensorsRefreshIntervalSeconds.Should().Be(60);
            preferences.Store.Groups.Should().ContainSingle();
            preferences.Store.Groups[0].CustomName.Should().Be("Work");
            preferences.Store.Groups[0].Items.Should().Equal("PowerMode");
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }
}
