using UniversalDeviceToolkit.Plugins.BatteryHealth;
using UniversalDeviceToolkit.Plugins.SDK;
using System.Linq;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.BatteryHealth.Tests;

[Collection("BatteryHealthResourceCulture")]
public class BatteryHealthPluginTests
{
    [Fact]
    public void Plugin_HasExpectedMetadata()
    {
        var plugin = new BatteryHealthPlugin();

        Assert.Equal("battery-health", plugin.Id);
        Assert.Equal(BatteryHealthText.PluginName, plugin.Name);
        Assert.Equal("3.6.15", typeof(BatteryHealthPlugin).GetCustomAttributes(typeof(PluginAttribute), false).Cast<PluginAttribute>().Single().MinimumHostVersion);
    }

    [Fact]
    public void Plugin_Pages_AreAvailable()
    {
        var plugin = new BatteryHealthPlugin();

        PluginPageAssertions.AssertPluginPage(plugin.GetFeatureExtension(), BatteryHealthText.FeaturePageTitle, "BatteryCharge24");
        PluginPageAssertions.AssertPluginPage(plugin.GetSettingsPage(), BatteryHealthText.SettingsPageTitle, "Settings24");
    }
}
