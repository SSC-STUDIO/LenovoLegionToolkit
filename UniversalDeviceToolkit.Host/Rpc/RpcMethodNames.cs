namespace UniversalDeviceToolkit.Host.Rpc;

/// <summary>
/// Single source of truth for the Windows-only bridge method names.
///
/// Windows builds register a concrete handler for every name listed here
/// (checked at startup by Program.VerifyRpcSurface); portable builds register
/// "-32099 not supported" stubs from the same list. Keeping one list prevents
/// the two surfaces from drifting apart (an unknown method surfaces to the
/// Electron client as -32601 instead of a clean "not supported").
///
/// Not listed here:
/// - Always-on methods registered on every platform (ping, app.getStatus,
///   app.getLogPath, app.quit, localization.*, plugins.*, plugin.*).
/// - macro.*: MacroHandlers compiles on every platform and registers its own
///   non-Windows stubs (see MacroHandlers.cs).
/// - Electron-main methods that never reach the Host (powerPlans.*, power.*,
///   update.getRelease/download/launchInstaller, device.info, dialog:*).
/// </summary>
public static class RpcMethodNames
{
    /// <summary>Windows-only methods; stubbed with -32099 on other platforms.</summary>
    public static readonly string[] WindowsOnly =
    [
        "wmi.getGodModeFnQ",
        "wmi.setGodModeFnQ",
        "godMode.getState",
        "godMode.setState",
        "godMode.apply",
        "software.getStatus",
        "software.setEnabled",
        "app.getAutorun",
        "app.setAutorun",
        "app.setUiActive",
        "app.update.check",
        "app.update.status",
        "system.info",
        "system.powerAdapterStatus",
        "system.accentColor.get",
        "system.accentColor.set",
        "settings.getAll",
        "settings.get",
        "settings.set",
        "settings.save",
        "settings.reload",
        "sensors.getStatus",
        "sensors.getSnapshot",
        "sensors.getDetailed",
        "sensors.subscribe",
        "sensors.unsubscribe",
        "sensors.getSettings",
        "sensors.setSettings",
        "sensors.getFps",
        "sensors.subscribeFps",
        "sensors.unsubscribeFps",
        "bootLogo.getStatus",
        "bootLogo.enable",
        "bootLogo.disable",
        "feature.list",
        "feature.getSupported",
        "feature.getStates",
        "feature.getState",
        "feature.setState",
        "feature.isHdrBlocked",
        "dashboard.getConfig",
        "dashboard.saveConfig",
        "dashboardHardware.getState",
        "dashboardHardware.setMonitoring",
        "dashboardHardware.killGpuProcesses",
        "dashboardHardware.restartGpu",
        "dashboardHardware.setOverclockEnabled",
        "dashboardHardware.setOverclock",
        "dashboardHardware.turnOffMonitors",
        "automation.getState",
        "automation.setEnabled",
        "automation.savePipelines",
        "automation.runNow",
        "automation.getSupportedSteps",
        "keyboard.detect",
        "rgb.isSupported",
        "rgb.getState",
        "rgb.setState",
        "rgb.setPreset",
        "rgb.nextPreset",
        "rgb.takeOwnership",
        "spectrum.isSupported",
        "spectrum.getLayout",
        "spectrum.getState",
        "spectrum.getBrightness",
        "spectrum.setBrightness",
        "spectrum.getLogoStatus",
        "spectrum.setLogoStatus",
        "spectrum.getProfile",
        "spectrum.setProfile",
        "spectrum.getProfileDescription",
        "spectrum.setProfileDescription",
        "optimization.getCategories",
        "optimization.apply",
        "optimization.revert",
        "optimization.applyRecommended",
        "optimization.getActionStatus",
        "cleanup.estimate",
        "cleanup.run",
        "cleanup.getCustomRules",
        "cleanup.saveCustomRules",
        "network.getStatus",
        "network.saveConfig",
        "network.start",
        "network.stop",
        "network.restore",
        "network.detectNat",
        "network.detectDns",
        "network.detectIpv6",
        "ai.getStatus",
        "ai.setEnabled",
        "driver.getSettings",
        "driver.getPackages",
        "driver.getPackageStatuses",
        "driver.start",
        "driver.pause",
        "driver.install",
        "driver.uninstall",
        "driver.setDownloadPath",
        "driver.setOnlyShowUpdates",
        "driver.setHiddenPackageIds",
    ];

    /// <summary>
    /// Telemetry polled unconditionally by the renderer; portable builds answer
    /// with an empty OK payload instead of an error so the UI stays quiet.
    /// </summary>
    public static readonly string[] EmptyOkOnNonWindows =
    [
        "network.getTrafficSnapshot",
        "network.getRuntimeSnapshot",
    ];
}
