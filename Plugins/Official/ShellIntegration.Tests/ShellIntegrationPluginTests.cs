using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using UniversalDeviceToolkit.Plugins.SDK;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Plugins.ShellIntegration;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ShellIntegration.Tests;

[Collection("ShellIntegrationResourceCulture")]
public class ShellIntegrationPluginTests
{
    private static bool? ParseShellRegistrationStatus(string commandOutput)
    {
        var method = typeof(ShellIntegrationPlugin).GetMethod("ParseShellRegistrationStatus", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool?)method!.Invoke(null, [commandOutput]);
    }

    [Fact]
    public void Plugin_HasExpectedMetadata()
    {
        var plugin = new ShellIntegrationPlugin();

        Assert.Equal("shell-integration", plugin.Id);
        Assert.Equal(ShellIntegrationText.PluginName, plugin.Name);
        Assert.True(plugin.IsSystemPlugin);
        Assert.Equal("Folder24", plugin.Icon);
        Assert.Equal(ShellIntegrationText.PluginDescription, plugin.Description);
    }

    [Fact]
    public void GetSettingsPage_ReturnsPluginPage()
    {
        var plugin = new ShellIntegrationPlugin();

        PluginPageAssertions.AssertPluginPage(plugin.GetSettingsPage(), ShellIntegrationText.SettingsPageTitle, "Settings24");
        Assert.Null(plugin.GetFeatureExtension());
    }

    [Fact]
    public void SettingsPage_AvaloniaFactoryProducesAvaloniaControl()
    {
        var plugin = new ShellIntegrationPlugin();

        var avaloniaPage = Assert.IsAssignableFrom<IAvaloniaPluginPage>(plugin.GetSettingsPage());
        var control = avaloniaPage.CreateAvaloniaPage();

        Assert.IsType<AvaloniaShellIntegrationSettingsControl>(control);
    }

    [Fact]
    public void GetOptimizationCategory_ReturnsExpectedActions()
    {
        var plugin = new ShellIntegrationPlugin();

        var category = plugin.GetOptimizationCategory();

        Assert.NotNull(category);
        Assert.Equal("shell.integration", category!.Key);
        Assert.Equal(plugin.Id, category.PluginId);
        Assert.Equal(2, category.Actions.Count);

        var enableAction = category.Actions.Single(a => a.Key == "shell.integration.enable");
        var disableAction = category.Actions.Single(a => a.Key == "shell.integration.disable");

        Assert.True(enableAction.Recommended);
        Assert.False(disableAction.Recommended);
        Assert.NotNull(enableAction.IsAppliedAsync);
        Assert.NotNull(disableAction.IsAppliedAsync);
    }

    [Fact]
    public void AvaloniaSettingsSurface_PreservesWpfCapabilityGatesAndRefreshesActions()
    {
        var source = ReadAvaloniaSettingsSource();

        Assert.Contains("PluginHostContextRuntime.Current.AllowSystemActions", source);
        Assert.Contains("File.Exists(shellConfigPath)", source);
        Assert.Contains("_enableButton.IsVisible = !registered", source);
        Assert.Contains("_disableButton.IsVisible = registered", source);
        Assert.Contains("_openConfigButton.IsEnabled = configExists", source);
        Assert.Contains("await RefreshAsync(", source);
        Assert.Contains("culture.TextInfo.IsRightToLeft", source);
        Assert.Contains("ShowDialog(owner)", source);
        Assert.Contains("AvaloniaShellIntegrationOpenShellConfigFileButton", source);
        Assert.Contains("Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true })", source);
        Assert.Contains("catch (Exception ex)", source);
    }

    [Fact]
    public void ShellDetection_IsConsistentWithResolvedPath()
    {
        var plugin = new ShellIntegrationPlugin();
        var path = plugin.GetShellInstallPath();

        Assert.Equal(path is not null, plugin.IsShellInstalled());
    }

    [Fact]
    public void OpenStyleSettingsWindow_WithoutApplication_DoesNotThrow()
    {
        var plugin = new ShellIntegrationPlugin();
        var hostContext = new RecordingPluginHostContext();

        try
        {
            PluginHostContextRuntime.Current = hostContext;
            RunSta(plugin.OpenStyleSettingsWindow);
        }
        finally
        {
            PluginHostContextRuntime.Reset();
        }
    }

    [Fact]
    public void OpenStyleSettingsWindow_UsesPluginHostContextWhenWindowIsAvailable()
    {
        var plugin = new ShellIntegrationPlugin();
        var hostContext = new RecordingPluginHostContext();

        try
        {
            PluginHostContextRuntime.Current = hostContext;

            RunSta(plugin.OpenStyleSettingsWindow);

            Assert.True(hostContext.ShowDialogCalled);
        }
        finally
        {
            PluginHostContextRuntime.Reset();
        }
    }

    [Fact]
    public void ConfigService_RenderTheme_ContainsManagedAccentAndEffect()
    {
        var profile = ShellIntegrationProfile.CreateDefault();
        profile.AccentColor = "#3366FF";
        profile.BackgroundEffect = ShellVisualEffect.Acrylic;

        var rendered = ShellIntegrationConfigService.RenderTheme(profile);

        Assert.Contains("color = #3366FF", rendered);
        Assert.Contains("effect = [3, #DCE6FF, 92]", rendered);
        Assert.Contains("name = \"modern\"", rendered);
    }

    [Fact]
    public void ConfigService_UpsertManagedImportBlock_IsIdempotent()
    {
        var content = "theme { }";
        var once = ShellIntegrationConfigService.UpsertManagedImportBlock(content);
        var twice = ShellIntegrationConfigService.UpsertManagedImportBlock(once);

        Assert.Equal(once, twice);
        Assert.Contains("imports/lenovo-legion-toolkit/settings.nss", twice);
        Assert.Contains("imports/lenovo-legion-toolkit/theme.nss", twice);
    }

    [Theory]
    [InlineData("Shell integration is not registered.")]
    [InlineData("Registered: false")]
    [InlineData("Enabled: false")]
    [InlineData("State: inactive")]
    public void ParseShellRegistrationStatus_WithNegativeSignals_ReturnsFalse(string output)
    {
        Assert.False(ParseShellRegistrationStatus(output));
    }

    [Theory]
    [InlineData("Shell integration is registered.")]
    [InlineData("Registered: true")]
    [InlineData("Enabled: true")]
    [InlineData("Status: active")]
    public void ParseShellRegistrationStatus_WithPositiveSignals_ReturnsTrue(string output)
    {
        Assert.True(ParseShellRegistrationStatus(output));
    }

    [Fact]
    public void ParseShellRegistrationStatus_PrefersExplicitNegativeSignals()
    {
        var output = """
                     Status: active
                     Registered: false
                     """;

        Assert.False(ParseShellRegistrationStatus(output));
    }

    [Fact]
    public void ParseShellRegistrationStatus_WithUnrelatedOutput_ReturnsNull()
    {
        Assert.Null(ParseShellRegistrationStatus("Shell command completed successfully."));
    }

    private sealed class RecordingPluginHostContext : IPluginHostContext
    {
        public PluginHostMode Mode => PluginHostMode.RealRuntime;
        public bool AllowSystemActions => true;
        public object? OwnerWindow => null;
        public bool ShowDialogCalled { get; private set; }

        public bool OpenPluginSettings(string pluginId) => false;

        public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null)
        {
            ShowDialogCalled = dialogOrContent is not null;
            return ShowDialogCalled;
        }
    }

    private static void RunSta(Action action)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static string ReadAvaloniaSettingsSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
            {
                return File.ReadAllText(Path.Combine(
                    directory.FullName,
                    "Plugins",
                    "Official",
                    "ShellIntegration",
                    "AvaloniaShellIntegrationSettingsControl.cs"));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("UniversalDeviceToolkit repository root was not found.");
    }
}
