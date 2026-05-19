using LenovoLegionToolkit.Plugins.NetworkAcceleration;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.TestCommon;
using System.Threading.Tasks;
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
    public void Settings_ReturnsSnapshot()
    {
        var plugin = new NetworkAccelerationPlugin();
        plugin.OnInstalled();

        var snapshot = plugin.Settings;
        snapshot.PreferredMode = NetworkAccelerationMode.Streaming;
        snapshot.AutoOptimizeOnStartup = true;
        snapshot.ResetWinsockOnOptimize = false;
        snapshot.ResetTcpIpOnOptimize = true;

        Assert.Equal(NetworkAccelerationMode.Balanced, plugin.Settings.PreferredMode);
        Assert.False(plugin.Settings.AutoOptimizeOnStartup);
        Assert.True(plugin.Settings.ResetWinsockOnOptimize);
        Assert.False(plugin.Settings.ResetTcpIpOnOptimize);
    }

    [Fact]
    public async Task ApplySettingsAsync_ReplacesCurrentSettings()
    {
        var plugin = new NetworkAccelerationPlugin();
        var updatedSettings = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Streaming,
            AutoOptimizeOnStartup = true,
            ResetWinsockOnOptimize = false,
            ResetTcpIpOnOptimize = true
        };

        await plugin.ApplySettingsAsync(updatedSettings);

        Assert.Equal(NetworkAccelerationMode.Streaming, plugin.Settings.PreferredMode);
        Assert.True(plugin.Settings.AutoOptimizeOnStartup);
        Assert.False(plugin.Settings.ResetWinsockOnOptimize);
        Assert.True(plugin.Settings.ResetTcpIpOnOptimize);
    }

    [Fact]
    public void NetworkAccelerationSettings_With_ReturnsUpdatedClone()
    {
        var original = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Balanced,
            AutoOptimizeOnStartup = false,
            ResetWinsockOnOptimize = true,
            ResetTcpIpOnOptimize = false
        };

        var updated = original.With(
            preferredMode: NetworkAccelerationMode.Streaming,
            autoOptimizeOnStartup: true,
            resetTcpIpOnOptimize: true);

        Assert.NotSame(original, updated);
        Assert.Equal(NetworkAccelerationMode.Balanced, original.PreferredMode);
        Assert.False(original.AutoOptimizeOnStartup);
        Assert.True(original.ResetWinsockOnOptimize);
        Assert.False(original.ResetTcpIpOnOptimize);

        Assert.Equal(NetworkAccelerationMode.Streaming, updated.PreferredMode);
        Assert.True(updated.AutoOptimizeOnStartup);
        Assert.True(updated.ResetWinsockOnOptimize);
        Assert.True(updated.ResetTcpIpOnOptimize);
    }

    [Fact]
    public void FeatureAndSettingsPages_AreExposedAsPluginPages()
    {
        var plugin = new NetworkAccelerationPlugin();

        PluginPageAssertions.AssertPluginPage(plugin.GetFeatureExtension(), NetworkAccelerationText.PageTitle, "Rocket24");
        PluginPageAssertions.AssertPluginPage(plugin.GetSettingsPage(), NetworkAccelerationText.SettingsPageTitle, "Settings24");
    }

    [Fact]
    public void GetOptimizationPlan_BalancedMode_ContainsDnsAndConfiguredWinsock()
    {
        var settings = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Balanced,
            ResetWinsockOnOptimize = true,
            ResetTcpIpOnOptimize = false
        };

        var plan = NetworkAccelerationPlugin.GetOptimizationPlan(settings);

        Assert.Equal(NetworkAccelerationMode.Balanced, plan.Mode);
        Assert.Collection(
            plan.Steps,
            step =>
            {
                Assert.Equal("FlushDns", step.Key);
                Assert.True(step.Required);
            },
            step =>
            {
                Assert.Equal("ResetWinsock", step.Key);
                Assert.True(step.Required);
            });
    }

    [Fact]
    public void GetOptimizationPlan_StreamingMode_AddsTcpResetEvenWhenToggleOff()
    {
        var settings = new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Streaming,
            ResetWinsockOnOptimize = false,
            ResetTcpIpOnOptimize = false
        };

        var plan = NetworkAccelerationPlugin.GetOptimizationPlan(settings);

        Assert.Contains(plan.Steps, step => step.Key == "FlushDns");
        Assert.Contains(plan.Steps, step => step.Key == "ResetTcpIp" && !step.Required);
        Assert.DoesNotContain(plan.Steps, step => step.Key == "ResetWinsock");
    }
}
