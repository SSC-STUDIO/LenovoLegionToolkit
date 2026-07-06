using LenovoLegionToolkit.Plugins.BatteryHealth;
using LenovoLegionToolkit.Plugins.TestCommon;
using Xunit;

namespace LenovoLegionToolkit.Plugins.BatteryHealth.Tests;

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

        PluginPageAssertions.AssertPluginPage(plugin.GetFeatureExtension(), BatteryHealthText.FeaturePageTitle, "PuzzlePiece24");
        PluginPageAssertions.AssertPluginPage(plugin.GetSettingsPage(), BatteryHealthText.SettingsPageTitle, "Settings24");
    }
}