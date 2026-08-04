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
}
