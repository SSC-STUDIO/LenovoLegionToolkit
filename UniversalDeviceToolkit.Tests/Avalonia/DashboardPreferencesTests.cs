using System.Text.Json;
using FluentAssertions;
using UniversalDeviceToolkit.Shared.Settings;
using UniversalDeviceToolkit.Shared.Utils;
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
        var previousOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        var tempRoot = Path.Combine(Path.GetTempPath(), "udt-dashboard-preferences-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, tempRoot);
            File.WriteAllText(
                Path.Combine(tempRoot, "dashboard.json"),
                JsonSerializer.Serialize(new { ShowSensors = false, SensorsRefreshIntervalSeconds = 1 }));

            var preferences = new AvaloniaDashboardPreferences();

            preferences.Store.ShowSensors.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, previousOverride);
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }
}
