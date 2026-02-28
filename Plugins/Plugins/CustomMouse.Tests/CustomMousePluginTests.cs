using LenovoLegionToolkit.Plugins.CustomMouse;
using LenovoLegionToolkit.Plugins.SDK;
using Xunit;

namespace LenovoLegionToolkit.Plugins.CustomMouse.Tests;

public class CustomMousePluginTests
{
    [Fact]
    public void Plugin_HasExpectedMetadata()
    {
        var plugin = new CustomMousePlugin();

        Assert.Equal("custom-mouse", plugin.Id);
        Assert.Equal(CustomMouseText.PluginName, plugin.Name);
        Assert.False(plugin.IsSystemPlugin);
        Assert.Equal(CustomMouseText.PluginDescription, plugin.Description);
        Assert.False(string.IsNullOrWhiteSpace(plugin.Icon));
    }

    [Fact]
    public void OnInstalled_ResetsToDefaultSettings()
    {
        var plugin = new CustomMousePlugin();
        plugin.SetDpi(3200);
        plugin.SetPollingRate(500);

        plugin.OnInstalled();

        Assert.Equal(1600, plugin.Settings.Dpi);
        Assert.Equal(1000, plugin.Settings.PollingRate);
        Assert.Empty(plugin.Settings.ButtonMappings);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(800)]
    [InlineData(1600)]
    [InlineData(3200)]
    [InlineData(16000)]
    public void SetDpi_WithValidValue_UpdatesSettings(int dpi)
    {
        var plugin = new CustomMousePlugin();

        var changed = plugin.SetDpi(dpi);

        Assert.True(changed);
        Assert.Equal(dpi, plugin.Settings.Dpi);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(16001)]
    [InlineData(-1)]
    public void SetDpi_WithInvalidValue_DoesNotUpdate(int dpi)
    {
        var plugin = new CustomMousePlugin();
        var originalDpi = plugin.Settings.Dpi;

        var changed = plugin.SetDpi(dpi);

        Assert.False(changed);
        Assert.Equal(originalDpi, plugin.Settings.Dpi);
    }

    [Theory]
    [InlineData(125)]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(1000)]
    public void SetPollingRate_WithValidValue_UpdatesSettings(int rate)
    {
        var plugin = new CustomMousePlugin();

        var changed = plugin.SetPollingRate(rate);

        Assert.True(changed);
        Assert.Equal(rate, plugin.Settings.PollingRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(126)]
    [InlineData(1001)]
    [InlineData(-1)]
    public void SetPollingRate_WithInvalidValue_DoesNotUpdate(int rate)
    {
        var plugin = new CustomMousePlugin();
        var originalRate = plugin.Settings.PollingRate;

        var changed = plugin.SetPollingRate(rate);

        Assert.False(changed);
        Assert.Equal(originalRate, plugin.Settings.PollingRate);
    }

    [Fact]
    public void Plugin_LifecycleMethods_DoNotThrow()
    {
        var plugin = new CustomMousePlugin();

        plugin.OnInstalled();
        var featurePage = plugin.GetFeatureExtension();
        var settingsPage = Assert.IsAssignableFrom<IPluginPage>(plugin.GetSettingsPage());
        var category = plugin.GetOptimizationCategory();

        Assert.Null(featurePage);
        Assert.NotNull(settingsPage);
        Assert.Equal(CustomMouseText.SettingsPageTitle, settingsPage.PageTitle);
        Assert.NotNull(category);
        Assert.Equal("custom.mouse", category!.Key);
        Assert.Equal(plugin.Id, category.PluginId);
        Assert.Equal(2, category.Actions.Count);
        Assert.Equal("custom.mouse.cursor.auto-theme.enable", category.Actions[0].Key);
        Assert.Equal("custom.mouse.cursor.auto-theme.disable", category.Actions[1].Key);
    }

    [Fact]
    public void SetAutoThemeCursorStyle_UpdatesSetting()
    {
        var plugin = new CustomMousePlugin();

        var changedToDisabled = plugin.SetAutoThemeCursorStyle(false);
        var changedToEnabled = plugin.SetAutoThemeCursorStyle(true);

        Assert.True(changedToDisabled);
        Assert.True(changedToEnabled);
        Assert.True(plugin.Settings.AutoThemeCursorStyle);
    }
}
