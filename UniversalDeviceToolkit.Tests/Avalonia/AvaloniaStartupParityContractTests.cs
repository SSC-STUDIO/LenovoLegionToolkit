using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaStartupParityContractTests
{
    [Fact]
    public void AvaloniaStartupFlags_ShouldParseWpfStartupSwitches()
    {
        var flags = AvaloniaStartupFlags.Parse(new[]
        {
            "--minimized",
            "--disable-update-checker",
            "--disable-tray-tooltip",
            "--trace",
            "--allow-all-power-modes-on-battery",
            "--force-disable-rgbkb",
            "--force-disable-spectrumkb",
            "--force-disable-lenovolighting",
            "--experimental-gpu-working-mode",
            "--proxy-url",
            "http://proxy.local:8080",
            "--proxy-username",
            "user",
            "--proxy-password",
            "secret",
        });

        flags.Minimized.Should().BeTrue();
        flags.DisableUpdateChecker.Should().BeTrue();
        flags.DisableTrayTooltip.Should().BeTrue();
        flags.IsTraceEnabled.Should().BeTrue();
        flags.AllowAllPowerModesOnBattery.Should().BeTrue();
        flags.ForceDisableRgbKeyboardSupport.Should().BeTrue();
        flags.ForceDisableSpectrumKeyboardSupport.Should().BeTrue();
        flags.ForceDisableLenovoLighting.Should().BeTrue();
        flags.ExperimentalGPUWorkingMode.Should().BeTrue();
        flags.ProxyUrl.Should().Be(new Uri("http://proxy.local:8080"));
        flags.ProxyUsername.Should().Be("user");
        flags.ProxyPassword.Should().Be("secret");
        flags.SafeStart.Should().BeFalse();
        flags.ResetHardwareState.Should().BeFalse();
        flags.ResetNetworkState.Should().BeFalse();
    }

    [Fact]
    public void AvaloniaStartupFlags_ShouldParseSafeStartAndResetSwitches()
    {
        var flags = AvaloniaStartupFlags.Parse(new[]
        {
            AvaloniaStartupFlags.SafeStartSwitch,
            AvaloniaStartupFlags.ResetHardwareStateSwitch,
            AvaloniaStartupFlags.ResetNetworkStateSwitch,
            AvaloniaStartupFlags.RestoreProcessorMinStateSwitch,
        });

        flags.SafeStart.Should().BeTrue();
        flags.ResetHardwareState.Should().BeTrue();
        flags.ResetNetworkState.Should().BeTrue();
        flags.RestoreProcessorMinState.Should().BeTrue();
        flags.DisableUpdateChecker.Should().BeFalse();
    }

    [Fact]
    public void AvaloniaStartupFlags_ShouldParseKeyEqualsValueForms()
    {
        var flags = AvaloniaStartupFlags.Parse(new[]
        {
            "--proxy-url=http://proxy.local:8080",
            "--proxy-username=user2",
        });

        flags.ProxyUrl.Should().Be(new Uri("http://proxy.local:8080"));
        flags.ProxyUsername.Should().Be("user2");
    }

    [Fact]
    public void AvaloniaStartupFlags_ShouldIgnoreMalformedProxyValues()
    {
        var flags = AvaloniaStartupFlags.Parse(new[] { "--proxy-url", "not-a-url" });

        flags.ProxyUrl.Should().BeNull();
    }

    [Fact]
    public void AvaloniaApp_ShouldApplyStartupFlagsAndMinimizedSwitch()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));

        source.Should().Contain("private void ApplyStartupFlags()");
        source.Should().Contain("StartupFlags.Minimized");
        source.Should().Contain("DisableUpdateChecker");
        source.Should().Contain("IoCContainer.TryResolve<HttpClientFactory>()?");
        source.Should().Contain("RequestAutomaticUpdateCheck()");
        source.Should().Contain("if (StartupFlags.DisableUpdateChecker)");
        source.Should().Contain("StartupFlags.DisableTrayTooltip");
    }

    [Fact]
    public void AvaloniaApp_ShouldGatePluginLoadsOnExtensionsAndInstalledPlugins()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));

        source.Should().Contain("settings is not { ExtensionsEnabled: true }");
        source.Should().Contain("HasInstalledPlugins()");
        source.Should().Contain("ScanAndLoadPluginsAsync()");
        source.Should().Contain("PluginPaths.GetAllPossiblePluginsDirectories()");
        source.Should().Contain("StartupFlags.SafeStart");
    }

    [Fact]
    public void AvaloniaApp_ShouldGateFirstRunLanguageSelectionBeforeMainWindow()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));

        source.Should().Contain("IsFirstRunLanguageSelection()");
        source.Should().Contain("!File.Exists(LocalizationRuntime.LanguageFilePath)");
        source.Should().Contain("RunFirstRunLanguageGateAsync(desktop)");
        source.Should().Contain("new AvaloniaLanguageSelectorWindow(");
        source.Should().Contain("CompleteStartup(desktop)");
    }

    [Fact]
    public void AvaloniaApp_ShouldConfigureSoftwareRenderingBeforeStartup()
    {
        var root = RepositoryPaths.FindRoot();
        var program = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Program.cs"));
        var helper = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "AvaloniaRenderingCompatibilityHelper.cs"));

        program.Should().Contain("AvaloniaStartupFlags.Current = AvaloniaStartupFlags.Parse(args);");
        program.Should().Contain("AvaloniaRenderingCompatibilityHelper.Configure(builder);");
        helper.Should().Contain("Win32RenderingMode.Software");
        helper.Should().Contain("GetSystemMetrics");
        helper.Should().Contain("SM_REMOTESESSION");
        helper.Should().Contain("ShouldForceSoftwareRendering()");
    }

    [Fact]
    public void AvaloniaWindowsHost_ShouldRunTheWpfStartupServicesWithoutWpfUiDependencies()
    {
        var root = RepositoryPaths.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var coordinator = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Startup",
            "AvaloniaWindowsStartupCoordinator.cs"));
        var featureHost = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));

        app.Should().Contain("new AvaloniaWindowsStartupCoordinator().RunAsync()");
        app.Should().Contain("StartAutomationForHostAsync()");
        coordinator.Should().Contain("EnsureCleanSystemStateOnStartupAsync");
        coordinator.Should().Contain("StartupInitializationRunner");
        coordinator.Should().Contain("StartupHealthGuard.MarkHardwareInitInProgress()");
        coordinator.Should().Contain("StartupHealthGuard.ClearHardwareInitInProgress()");
        coordinator.Should().Contain("IsHardwareInitInProgressMarkerPresent()");
        coordinator.Should().Contain("--safe-start");
        coordinator.Should().Contain("--reset-hardware-state");
        coordinator.Should().Contain("--reset-network-state");
        coordinator.Should().Contain("EnsureGodModeStateIsAppliedAsync");
        coordinator.Should().Contain("EnsureCorrectBatteryModeIsSetAsync");
        coordinator.Should().Contain("StartAuroraIfNeededAsync");
        coordinator.Should().Contain("EnsureOverclockIsAppliedAsync");
        coordinator.Should().Contain("EnsureDGPUEjectedIfNeededAsync");
        coordinator.Should().Contain("LoadAndApply(settings.Store.Entries)");
        coordinator.Should().Contain("StartIfNeededAsync");
        coordinator.Should().Contain("StartStopIfNeededAsync");
        coordinator.Should().Contain("IpcServer");
        coordinator.Should().Contain("MacroController");
        featureHost.Should().Contain("_automation.RunOnStartup()");
        featureHost.Should().Contain("_automationStartupInvoked");
        coordinator.Should().NotContain("UniversalDeviceToolkit.WPF");
    }
}
