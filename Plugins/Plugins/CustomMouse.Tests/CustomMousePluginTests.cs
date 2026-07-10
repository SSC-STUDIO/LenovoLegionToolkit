using LenovoLegionToolkit.Plugins.CustomMouse;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.TestCommon;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace LenovoLegionToolkit.Plugins.CustomMouse.Tests;

[Collection("CustomMouseResourceCulture")]
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
        Assert.Empty(plugin.Settings.ButtonMappings);
    }

    [Fact]
    public void Plugin_LifecycleMethods_DoNotThrow()
    {
        var plugin = new CustomMousePlugin();

        plugin.OnInstalled();
        var settingsPage = PluginPageAssertions.AssertPluginPage(plugin.GetSettingsPage(), CustomMouseText.SettingsPageTitle);
        var category = plugin.GetOptimizationCategory();

        Assert.NotNull(settingsPage);
        Assert.NotNull(category);
        Assert.Equal("custom.mouse", category!.Key);
        Assert.Equal(plugin.Id, category.PluginId);
        Assert.Equal(2, category.Actions.Count);
        Assert.Equal("custom.mouse.cursor.auto-theme.enable", category.Actions[0].Key);
        Assert.Equal("custom.mouse.cursor.auto-theme.disable", category.Actions[1].Key);
    }

    [Fact]
    public void Plugin_DoesNotExposeStandaloneFeaturePage()
    {
        var plugin = new CustomMousePlugin();

        Assert.Null(plugin.GetFeatureExtension());
    }

    [Fact]
    public void GetResourceRoot_WithLocalPluginDirectoryOverride_ReturnsLocalPackageResources()
    {
        const string overrideEnvironmentVariable = "LLT_PLUGIN_DIRECTORY_OVERRIDE";
        var originalOverride = Environment.GetEnvironmentVariable(overrideEnvironmentVariable);
        var pluginsDirectoryPath = Path.Combine(Path.GetTempPath(), $"llt-custom-mouse-plugins-{Guid.NewGuid():N}");
        var resourceRoot = Path.Combine(pluginsDirectoryPath, "local", "custom-mouse", "Resources");

        try
        {
            Directory.CreateDirectory(Path.Combine(resourceRoot, "W11-CC-V2.2-HDPI"));
            Environment.SetEnvironmentVariable(overrideEnvironmentVariable, pluginsDirectoryPath);

            var plugin = new CustomMousePlugin();

            Assert.Equal(Path.GetFullPath(resourceRoot), Path.GetFullPath(plugin.GetResourceRoot()));
        }
        finally
        {
            Environment.SetEnvironmentVariable(overrideEnvironmentVariable, originalOverride);
            if (Directory.Exists(pluginsDirectoryPath))
            {
                Directory.Delete(pluginsDirectoryPath, recursive: true);
            }
        }
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

    [Fact]
    public void SetAutoThemeCursorStyle_EnablingAutoSetsCursorThemeModeAuto()
    {
        var plugin = new CustomMousePlugin();
        plugin.Settings.LastAppliedTheme = "dark";

        var changed = plugin.SetAutoThemeCursorStyle(true);

        Assert.True(changed);
        Assert.True(plugin.Settings.AutoThemeCursorStyle);
        Assert.Equal(CursorThemeMode.Auto, plugin.Settings.CursorThemeMode);
    }

    [Theory]
    [InlineData("light", CursorThemeMode.Light)]
    [InlineData("dark", CursorThemeMode.Dark)]
    public void SetAutoThemeCursorStyle_DisablingAutoPreservesLastAppliedTheme(string lastAppliedTheme, CursorThemeMode expectedMode)
    {
        var plugin = new CustomMousePlugin();
        plugin.Settings.LastAppliedTheme = lastAppliedTheme;

        var changed = plugin.SetAutoThemeCursorStyle(false);

        Assert.True(changed);
        Assert.False(plugin.Settings.AutoThemeCursorStyle);
        Assert.Equal(expectedMode, plugin.Settings.CursorThemeMode);
    }

    [Fact]
    public async Task RestoreWindowsDefaultCursorThemeAsync_SetsWindowsDefaultMode()
    {
        var plugin = new CustomMousePlugin();
        var backupKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors", true);
        Assert.NotNull(backupKey);

        var originalDefault = Convert.ToString(backupKey!.GetValue(string.Empty));
        var originalArrow = Convert.ToString(backupKey.GetValue("Arrow"));

        try
        {
            backupKey.SetValue(string.Empty, "UDT Custom Mouse Dark");
            backupKey.SetValue("Arrow", @"D:\fake\dark\Pointer.cur");

            var restored = await plugin.RestoreWindowsDefaultCursorThemeAsync();

            Assert.True(restored);
            Assert.Equal(CursorThemeMode.WindowsDefault, plugin.Settings.CursorThemeMode);
            Assert.False(plugin.Settings.AutoThemeCursorStyle);
        }
        finally
        {
            backupKey.SetValue(string.Empty, originalDefault ?? string.Empty);
            backupKey.SetValue("Arrow", originalArrow ?? string.Empty);
            backupKey.Dispose();
        }
    }

    [Fact]
    public void ReloadSettingsFromSystem_SyncsPointerAndCursorMode()
    {
        var plugin = new CustomMousePlugin();
        using var mouseKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse", true);
        using var cursorKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors", true);
        Assert.NotNull(mouseKey);
        Assert.NotNull(cursorKey);

        var originalSensitivity = Convert.ToString(mouseKey!.GetValue("MouseSensitivity"));
        var originalSwap = Convert.ToString(mouseKey.GetValue("SwapMouseButtons"));
        var originalDefault = Convert.ToString(cursorKey!.GetValue(string.Empty));
        var originalArrow = Convert.ToString(cursorKey.GetValue("Arrow"));

        try
        {
            mouseKey.SetValue("MouseSensitivity", "13");
            mouseKey.SetValue("SwapMouseButtons", "1");
            cursorKey.SetValue(string.Empty, CustomMousePlugin.WindowsDefaultCursorSchemeName);
            cursorKey.SetValue("Arrow", @"C:\Windows\cursors\aero_arrow.cur");

            plugin.ReloadSettingsFromSystem();

            Assert.Equal(13, plugin.Settings.WindowsPointerSpeed);
            Assert.True(plugin.Settings.SwapButtons);
            Assert.Equal(CursorThemeMode.WindowsDefault, plugin.Settings.CursorThemeMode);
        }
        finally
        {
            mouseKey.SetValue("MouseSensitivity", originalSensitivity ?? "10");
            mouseKey.SetValue("SwapMouseButtons", originalSwap ?? "0");
            cursorKey.SetValue(string.Empty, originalDefault ?? string.Empty);
            cursorKey.SetValue("Arrow", originalArrow ?? string.Empty);
        }
    }
}
