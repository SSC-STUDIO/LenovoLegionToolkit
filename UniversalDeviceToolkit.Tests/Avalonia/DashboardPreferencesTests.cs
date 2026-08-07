using System.Text.Json;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
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
        store.SensorsRefreshIntervalSeconds.Should().Be(1);
        store.SchemaVersion.Should().Be(4);
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
    public void DashboardRefreshInterval_UsesWpfChoicesAndPersistsTheSelectedValue()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "udt-dashboard-interval-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var preferences = new AvaloniaDashboardPreferences(tempRoot);
            var viewModel = new DashboardPageViewModel(new UnavailablePlatformServices(), preferences);

            DashboardPageViewModel.SensorRefreshIntervalOptions.Should().Equal(1, 2, 3, 5);
            viewModel.SensorsRefreshIntervalSeconds = 3;

            preferences.Store.SensorsRefreshIntervalSeconds.Should().Be(3);
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(tempRoot, "dashboard.json")));
            document.RootElement.GetProperty("SensorsRefreshIntervalSeconds").GetInt32().Should().Be(3);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
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
            preferences.Store.Groups.Should().Contain(group =>
                group.Type == "Custom" && group.CustomName == "Work");
            preferences.Store.Groups.Should().Contain(group => group.Type == "Graphics");
            var items = preferences.Store.Groups.SelectMany(group => group.Items).ToArray();
            items.Should().Contain("PowerMode");
            items.Should().Contain("ItsMode");
            items.Should().NotContain("Invalid");
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    [Fact]
    public void DashboardPreferences_SaveUsesTheSharedWpfCompatibleFileName()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "udt-dashboard-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var preferences = new AvaloniaDashboardPreferences(tempRoot);
            preferences.Store.ShowSensors = false;
            preferences.SynchronizeStore();

            File.Exists(Path.Combine(tempRoot, "dashboard.json")).Should().BeTrue();
            File.Exists(Path.Combine(tempRoot, "avalonia-dashboard.json")).Should().BeFalse();
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(tempRoot, "dashboard.json")));
            document.RootElement.GetProperty("ShowSensors").GetBoolean().Should().BeFalse();
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    [Fact]
    public void DashboardPreferences_ReadsThePreviousAvaloniaFileWhenCanonicalFileIsMissing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "udt-dashboard-legacy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            File.WriteAllText(
                Path.Combine(tempRoot, "avalonia-dashboard.json"),
                "{\"ShowSensors\":false,\"SensorsRefreshIntervalSeconds\":9}");

            var preferences = new AvaloniaDashboardPreferences(tempRoot);

            preferences.Store.ShowSensors.Should().BeFalse();
            preferences.Store.SensorsRefreshIntervalSeconds.Should().Be(9);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    [Theory]
    [InlineData(600)]
    [InlineData(900)]
    [InlineData(1600)]
    public void DashboardPreferences_DefaultGroupsReflowWithoutDropping(double width)
    {
        var groups = AvaloniaDashboardPreferences.CreateDefaultGroups();

        var columns = DashboardColumnLayout.GetColumnCountForWidth(width);
        var rows = (int)Math.Ceiling(groups.Count / (double)columns);

        columns.Should().BeInRange(1, 3);
        (rows * columns).Should().BeGreaterThanOrEqualTo(groups.Count);
        rows.Should().Be((groups.Count + columns - 1) / columns);
    }
}
