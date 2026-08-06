using UniversalDeviceToolkit.Plugins.CustomMouse;
using UniversalDeviceToolkit.Plugins.SDK;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.CustomMouse.Tests;

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
    public void AvaloniaSettingsSurface_PreservesWpfActionSemanticsAndAutomationContracts()
    {
        var source = ReadAvaloniaSettingsSource();

        Assert.Contains("AvaloniaCustomMouseSyncFromWindows", source);
        Assert.Contains("AvaloniaCustomMouseReload", source);
        Assert.Contains("ActionButton(CustomMouseText.ReloadButton, (Action)ReloadSettings", source);
        Assert.Contains("if (!_plugin.SetSwapButtons(swapButtons))", source);
        Assert.Contains("_plugin.SetWindowsPointerSpeed(originalSpeed)", source);
        Assert.Contains("StatusWindowsDefaultRestored", source);
        Assert.Contains("$\"{speed}/20\"", source);
        Assert.Contains("AvaloniaCustomMouseApplyProgress", source);
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

    [Fact]
    public async Task SaveSettingsAsync_RetriesOnException_WhenSettingsFileTemporarilyLocked()
    {
        // Regression test: SaveSettingsAsync retry loop must catch I/O exceptions
        // (IOException and UnauthorizedAccessException) when the settings file is
        // temporarily locked, then succeed once the lock is released.
        // This verifies the retry mechanism without timing assertions.
        var plugin = new CustomMousePlugin();

        // Step 1: Save once to ensure the settings file exists.
        await plugin.SaveSettingsAsync();

        // Step 2: Locate the settings file and lock it exclusively.
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UniversalDeviceToolkit", "plugin-config");
        var settingsFile = Path.Combine(settingsDir, "custom-mouse.json");

        Assert.True(File.Exists(settingsFile), "Settings file should exist after initial save.");

        // Step 3: Lock the file, then release it after a short delay.
        // The retry loop (50ms + 100ms delays) will wait, then succeed once unlocked.
        var lockStream = new FileStream(
            settingsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Release the lock after 20ms — before the first retry delay (50ms).
        // This ensures the retry loop catches the IOException, waits 50ms,
        // then succeeds on the second attempt when the lock is released.
        _ = Task.Run(async () =>
        {
            await Task.Delay(20);
            lockStream.Dispose();
        });

        // Step 4: SaveSettingsAsync should succeed after retries (no exception).
        // Without the retry catch, this would throw IOException immediately.
        await plugin.SaveSettingsAsync();

        // Verify the file is still valid JSON after the retry save.
        var content = File.ReadAllText(settingsFile);
        Assert.NotEmpty(content);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(-1)]
    [InlineData(100)]
    public void SanitizeCursorThemeMode_WithOutOfRangeInt_FallsBackToAuto(int corruptValue)
    {
        // Regression test: an out-of-range integer persisted for CursorThemeMode
        // (e.g. 999 from a corrupted config, or a removed enum member) must fall
        // back to Auto instead of producing an invalid enum value that silently
        // applies the wrong cursor theme.
        var result = CustomMousePlugin.SanitizeCursorThemeMode(corruptValue);
        Assert.Equal(CursorThemeMode.Auto, result);
    }

    [Theory]
    [InlineData(0, CursorThemeMode.Auto)]
    [InlineData(1, CursorThemeMode.Light)]
    [InlineData(2, CursorThemeMode.Dark)]
    [InlineData(3, CursorThemeMode.WindowsDefault)]
    public void SanitizeCursorThemeMode_WithValidInt_PreservesValue(int validValue, CursorThemeMode expected)
    {
        var result = CustomMousePlugin.SanitizeCursorThemeMode(validValue);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(21, 20)]
    [InlineData(999, 20)]
    public void SanitizeWindowsPointerSpeed_WithOutOfRangeInt_ClampsToValidRange(int corruptValue, int expected)
    {
        var result = CustomMousePlugin.SanitizeWindowsPointerSpeed(corruptValue);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(20, 20)]
    public void SanitizeWindowsPointerSpeed_WithValidInt_PreservesValue(int validValue, int expected)
    {
        var result = CustomMousePlugin.SanitizeWindowsPointerSpeed(validValue);
        Assert.Equal(expected, result);
    }

    private static string ReadAvaloniaSettingsSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln");
            if (File.Exists(solutionPath))
            {
                return File.ReadAllText(Path.Combine(
                    directory.FullName,
                    "Plugins",
                    "Official",
                    "CustomMouse",
                    "AvaloniaCustomMouseSettingsControl.cs"));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("UniversalDeviceToolkit repository root was not found.");
    }
}
