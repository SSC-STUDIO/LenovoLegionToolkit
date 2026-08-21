using UniversalDeviceToolkit.Plugins.CustomMouse;
using UniversalDeviceToolkit.Plugins.SDK;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        try
        {
            plugin.OnInstalled();
            Assert.Empty(plugin.Settings.ButtonMappings);
        }
        finally
        {
            plugin.Stop();
        }
    }

    [Fact]
    public void Plugin_LifecycleMethods_DoNotThrow()
    {
        var plugin = new CustomMousePlugin();

        try
        {
            plugin.OnInstalled();
            Assert.Null(plugin.GetSettingsPage());
            var category = plugin.GetOptimizationCategory();

            Assert.NotNull(category);
            Assert.Equal("custom.mouse", category!.Key);
            Assert.Equal(plugin.Id, category.PluginId);
            Assert.Equal(2, category.Actions.Count);
            Assert.Equal("custom.mouse.cursor.auto-theme.enable", category.Actions[0].Key);
            Assert.Equal("custom.mouse.cursor.auto-theme.disable", category.Actions[1].Key);
        }
        finally
        {
            plugin.Stop();
        }
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
            try
            {
                Assert.Equal(Path.GetFullPath(resourceRoot), Path.GetFullPath(plugin.GetResourceRoot()));
            }
            finally
            {
                plugin.Stop();
            }
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

        try
        {
            var changedToDisabled = plugin.SetAutoThemeCursorStyle(false);
            var changedToEnabled = plugin.SetAutoThemeCursorStyle(true);

            Assert.True(changedToDisabled);
            Assert.True(changedToEnabled);
            Assert.True(plugin.Settings.AutoThemeCursorStyle);
        }
        finally
        {
            plugin.Stop();
        }
    }

    [Fact]
    public void SetAutoThemeCursorStyle_EnablingAutoSetsCursorThemeModeAuto()
    {
        var plugin = new CustomMousePlugin();
        plugin.Settings.LastAppliedTheme = "dark";

        try
        {
            var changed = plugin.SetAutoThemeCursorStyle(true);

            Assert.True(changed);
            Assert.True(plugin.Settings.AutoThemeCursorStyle);
            Assert.Equal(CursorThemeMode.Auto, plugin.Settings.CursorThemeMode);
        }
        finally
        {
            plugin.Stop();
        }
    }

    [Theory]
    [InlineData("light", CursorThemeMode.Light)]
    [InlineData("dark", CursorThemeMode.Dark)]
    public void SetAutoThemeCursorStyle_DisablingAutoPreservesLastAppliedTheme(string lastAppliedTheme, CursorThemeMode expectedMode)
    {
        var plugin = new CustomMousePlugin();
        plugin.Settings.LastAppliedTheme = lastAppliedTheme;

        try
        {
            var changed = plugin.SetAutoThemeCursorStyle(false);

            Assert.True(changed);
            Assert.False(plugin.Settings.AutoThemeCursorStyle);
            Assert.Equal(expectedMode, plugin.Settings.CursorThemeMode);
        }
        finally
        {
            plugin.Stop();
        }
    }

    [Fact]
    public async Task RestoreWindowsDefaultCursorThemeAsync_SetsWindowsDefaultMode()
    {
        var plugin = new CustomMousePlugin();
        using var snapshot = CursorRegistrySnapshot.Capture();
        using var settingsSnapshot = PluginSettingsFileSnapshot.Capture();
        using var schemesKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors\Schemes", true);
        Assert.NotNull(schemesKey);

        var schemeName = CustomMousePlugin.WindowsDefaultCursorSchemeName;
        var originalScheme = schemesKey!.GetValue(schemeName);
        var originalKind = originalScheme is null
            ? RegistryValueKind.ExpandString
            : schemesKey.GetValueKind(schemeName);
        var parts = new string[15];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = $@"C:\Windows\cursors\aero_{i}.cur";
        }

        try
        {
            using (var backupKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors", true))
            {
                Assert.NotNull(backupKey);
                backupKey!.SetValue(string.Empty, "UDT Custom Mouse Dark");
                backupKey.SetValue("Arrow", @"D:\fake\dark\Pointer.cur");
            }

            schemesKey.SetValue(schemeName, string.Join(",", parts), RegistryValueKind.ExpandString);

            var restored = await plugin.RestoreWindowsDefaultCursorThemeAsync();

            Assert.True(restored);
            Assert.Equal(CursorThemeMode.WindowsDefault, plugin.Settings.CursorThemeMode);
            Assert.False(plugin.Settings.AutoThemeCursorStyle);
            Assert.Equal(string.Empty, plugin.Settings.LastAppliedTheme);
        }
        finally
        {
            if (originalScheme is null)
            {
                schemesKey.DeleteValue(schemeName, throwOnMissingValue: false);
            }
            else
            {
                schemesKey.SetValue(schemeName, originalScheme, originalKind);
            }

            snapshot.Restore();
            plugin.Stop();
        }
    }

    [Fact]
    public async Task SetCursorThemeModeAsync_WindowsDefault_DoesNotApplyDarkWhenRestoreFails()
    {
        var plugin = new CustomMousePlugin();
        plugin.Settings.CursorThemeMode = CursorThemeMode.Light;
        plugin.Settings.AutoThemeCursorStyle = false;
        plugin.Settings.LastAppliedTheme = "light";
        using var snapshot = CursorRegistrySnapshot.Capture();
        using var settingsSnapshot = PluginSettingsFileSnapshot.Capture();
        using var schemesKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors\Schemes", true);
        Assert.NotNull(schemesKey);

        var schemeName = CustomMousePlugin.WindowsDefaultCursorSchemeName;
        var originalScheme = schemesKey!.GetValue(schemeName);
        var originalKind = originalScheme is null
            ? RegistryValueKind.ExpandString
            : schemesKey.GetValueKind(schemeName);

        try
        {
            // HKCU is consulted first and shadows HKLM, so a too-short scheme forces restore failure.
            schemesKey.SetValue(schemeName, "invalid", RegistryValueKind.ExpandString);

            var applied = await plugin.SetCursorThemeModeAsync(CursorThemeMode.WindowsDefault);

            Assert.False(applied);
            Assert.Equal(CursorThemeMode.Light, plugin.Settings.CursorThemeMode);
            Assert.Equal("light", plugin.Settings.LastAppliedTheme);
            Assert.False(plugin.Settings.AutoThemeCursorStyle);
        }
        finally
        {
            if (originalScheme is null)
            {
                schemesKey.DeleteValue(schemeName, throwOnMissingValue: false);
            }
            else
            {
                schemesKey.SetValue(schemeName, originalScheme, originalKind);
            }

            snapshot.Restore();
            plugin.Stop();
        }
    }

    [Fact]
    public async Task SetCursorThemeModeAsync_InvalidMode_ReturnsFalseWithoutMutating()
    {
        var plugin = new CustomMousePlugin();
        plugin.Settings.CursorThemeMode = CursorThemeMode.Light;
        plugin.Settings.AutoThemeCursorStyle = false;
        plugin.Settings.LastAppliedTheme = "light";

        var applied = await plugin.SetCursorThemeModeAsync((CursorThemeMode)999);

        Assert.False(applied);
        Assert.Equal(CursorThemeMode.Light, plugin.Settings.CursorThemeMode);
        Assert.Equal("light", plugin.Settings.LastAppliedTheme);
        Assert.False(plugin.Settings.AutoThemeCursorStyle);
    }

    [Fact]
    public void RestoreCursorScheme_WithFifteenPartUserScheme_Succeeds()
    {
        var plugin = new CustomMousePlugin();
        var schemeName = $"UDT-Test-Aero-{Guid.NewGuid():N}";
        var parts = new string[15];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = $@"C:\Windows\cursors\test_{i}.cur";
        }

        using var snapshot = CursorRegistrySnapshot.Capture();
        using var schemesKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors\Schemes", true);
        Assert.NotNull(schemesKey);

        try
        {
            schemesKey!.SetValue(schemeName, string.Join(",", parts), RegistryValueKind.ExpandString);

            var restored = plugin.RestoreCursorScheme(schemeName);

            Assert.True(restored);
            using var cursorKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", false);
            Assert.NotNull(cursorKey);
            Assert.Equal(parts[0], Convert.ToString(cursorKey!.GetValue("Arrow")));
            Assert.Equal(parts[0], Convert.ToString(cursorKey.GetValue("Person")));
            Assert.Equal(parts[0], Convert.ToString(cursorKey.GetValue("Pin")));
        }
        finally
        {
            schemesKey.DeleteValue(schemeName, throwOnMissingValue: false);
            snapshot.Restore();
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
        var settingsFile = PluginSettingsFileSnapshot.SettingsFilePath;
        using var settingsSnapshot = PluginSettingsFileSnapshot.Capture();
        var plugin = new CustomMousePlugin();

        try
        {
            await plugin.SaveSettingsAsync();
            Assert.True(File.Exists(settingsFile), "Settings file should exist after initial save.");

            var uniqueMarker = $"retry-{Guid.NewGuid():N}";
            plugin.Settings.LastAppliedTheme = uniqueMarker;

            // Hold temporary lock for 60ms so initial save attempt encounters IOException and retries
            var lockAcquired = new ManualResetEventSlim(false);
            var lockTask = Task.Run(() =>
            {
                using var stream = File.Open(settingsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                lockAcquired.Set();
                Thread.Sleep(60);
            });
            lockAcquired.Wait(TimeSpan.FromSeconds(5));

            await plugin.SaveSettingsAsync();
            await lockTask;

            Assert.Contains(uniqueMarker, File.ReadAllText(settingsFile), StringComparison.Ordinal);
        }
        finally
        {
            plugin.Stop();
        }
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

    [Fact]
    public void ReloadSettingsFromSystem_WhenAutoAndCustomLightCursors_KeepsAuto()
    {
        var plugin = new CustomMousePlugin();
        plugin.Settings.CursorThemeMode = CursorThemeMode.Auto;
        plugin.Settings.AutoThemeCursorStyle = true;
        plugin.Settings.LastAppliedTheme = "dark";

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
            mouseKey.SetValue("MouseSensitivity", "11");
            mouseKey.SetValue("SwapMouseButtons", "0");
            cursorKey.SetValue(string.Empty, "UDT Custom Mouse Light");
            cursorKey.SetValue("Arrow", @"C:\plugins\W11-CC-V2.2-HDPI\Light\Regular\Base\Pointer.cur");

            plugin.ReloadSettingsFromSystem();

            Assert.Equal(11, plugin.Settings.WindowsPointerSpeed);
            Assert.False(plugin.Settings.SwapButtons);
            Assert.Equal(CursorThemeMode.Auto, plugin.Settings.CursorThemeMode);
            Assert.True(plugin.Settings.AutoThemeCursorStyle);
            Assert.Equal("light", plugin.Settings.LastAppliedTheme);
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

internal sealed class CursorRegistrySnapshot : IDisposable
{
    private const string CursorPath = @"Control Panel\Cursors";
    private readonly Dictionary<string, (object? Value, RegistryValueKind Kind)> _values = new(StringComparer.OrdinalIgnoreCase);

    private CursorRegistrySnapshot()
    {
    }

    public static CursorRegistrySnapshot Capture()
    {
        var snapshot = new CursorRegistrySnapshot();
        using var key = Registry.CurrentUser.OpenSubKey(CursorPath, false);
        if (key is null)
        {
            return snapshot;
        }

        snapshot._values[string.Empty] = (key.GetValue(string.Empty), RegistryValueKind.String);
        foreach (var name in key.GetValueNames())
        {
            var value = key.GetValue(name);
            snapshot._values[name] = (value, value is null ? RegistryValueKind.String : key.GetValueKind(name));
        }

        return snapshot;
    }

    public void Restore()
    {
        using var key = Registry.CurrentUser.CreateSubKey(CursorPath, true);
        if (key is null)
        {
            return;
        }

        foreach (var name in key.GetValueNames())
        {
            if (!_values.ContainsKey(name))
            {
                key.DeleteValue(name, throwOnMissingValue: false);
            }
        }

        foreach (var (name, (value, kind)) in _values)
        {
            if (value is null)
            {
                if (string.IsNullOrEmpty(name))
                {
                    key.SetValue(string.Empty, string.Empty, RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(name, throwOnMissingValue: false);
                }

                continue;
            }

            key.SetValue(string.IsNullOrEmpty(name) ? string.Empty : name, value, kind);
        }
    }

    public void Dispose()
    {
        Restore();
    }
}

internal sealed class PluginSettingsFileSnapshot : IDisposable
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UniversalDeviceToolkit",
        "plugin-config");

    public static string SettingsFilePath => Path.Combine(SettingsDir, "custom-mouse.json");

    private readonly string? _originalContent;
    private readonly bool _originallyExisted;

    private PluginSettingsFileSnapshot()
    {
        _originallyExisted = File.Exists(SettingsFilePath);
        if (_originallyExisted)
        {
            _originalContent = File.ReadAllText(SettingsFilePath);
        }
    }

    public static PluginSettingsFileSnapshot Capture()
    {
        return new PluginSettingsFileSnapshot();
    }

    public void Restore()
    {
        if (_originallyExisted && _originalContent is not null)
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(SettingsFilePath, _originalContent);
        }
        else if (File.Exists(SettingsFilePath))
        {
            try
            {
                File.Delete(SettingsFilePath);
            }
            catch
            {
                // ignored
            }
        }
    }

    public void Dispose()
    {
        Restore();
    }
}
