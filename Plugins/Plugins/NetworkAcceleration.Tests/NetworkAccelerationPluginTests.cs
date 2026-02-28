using LenovoLegionToolkit.Plugins.NetworkAcceleration;
using LenovoLegionToolkit.Plugins.SDK;
using Xunit;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Tests;

public class NetworkAccelerationPluginTests
{
    [Fact]
    public void Plugin_HasExpectedMetadata()
    {
        var plugin = new NetworkAccelerationPlugin();

        Assert.Equal("network-acceleration", plugin.Id);
        Assert.Equal(NetworkAccelerationText.PluginName, plugin.Name);
        Assert.False(plugin.IsSystemPlugin);
        Assert.Equal("Rocket24", plugin.Icon);
        Assert.Equal(NetworkAccelerationText.PluginDescription, plugin.Description);
    }

    [Fact]
    public void OnInstalled_ResetsToDefaultSettings()
    {
        var plugin = new NetworkAccelerationPlugin();

        plugin.SetPreferredMode(NetworkAccelerationMode.Streaming);
        plugin.SetAutoOptimizeOnStartup(true);
        plugin.SetResetWinsockOnOptimize(false);
        plugin.SetResetTcpIpOnOptimize(true);

        plugin.OnInstalled();

        Assert.Equal(NetworkAccelerationMode.Balanced, plugin.Settings.PreferredMode);
        Assert.False(plugin.Settings.AutoOptimizeOnStartup);
        Assert.True(plugin.Settings.ResetWinsockOnOptimize);
        Assert.False(plugin.Settings.ResetTcpIpOnOptimize);
    }

    [Theory]
    [InlineData(NetworkAccelerationMode.Balanced)]
    [InlineData(NetworkAccelerationMode.Gaming)]
    [InlineData(NetworkAccelerationMode.Streaming)]
    public void SetPreferredMode_UpdatesSettings(NetworkAccelerationMode mode)
    {
        var plugin = new NetworkAccelerationPlugin();

        var changed = plugin.SetPreferredMode(mode);

        Assert.True(changed);
        Assert.Equal(mode, plugin.Settings.PreferredMode);
    }

    [Fact]
    public void BooleanSetters_UpdateSettings()
    {
        var plugin = new NetworkAccelerationPlugin();

        Assert.True(plugin.SetAutoOptimizeOnStartup(true));
        Assert.True(plugin.SetResetWinsockOnOptimize(false));
        Assert.True(plugin.SetResetTcpIpOnOptimize(true));

        Assert.True(plugin.Settings.AutoOptimizeOnStartup);
        Assert.False(plugin.Settings.ResetWinsockOnOptimize);
        Assert.True(plugin.Settings.ResetTcpIpOnOptimize);
    }

    [Fact]
    public void FeatureAndSettingsPages_AreExposedAsPluginPages()
    {
        var plugin = new NetworkAccelerationPlugin();

        var featurePage = Assert.IsAssignableFrom<IPluginPage>(plugin.GetFeatureExtension());
        var settingsPage = Assert.IsAssignableFrom<IPluginPage>(plugin.GetSettingsPage());

        Assert.Equal(NetworkAccelerationText.PageTitle, featurePage.PageTitle);
        Assert.Equal("Rocket24", featurePage.PageIcon);
        Assert.Equal(NetworkAccelerationText.SettingsPageTitle, settingsPage.PageTitle);
        Assert.Equal("Settings24", settingsPage.PageIcon);
    }
}
