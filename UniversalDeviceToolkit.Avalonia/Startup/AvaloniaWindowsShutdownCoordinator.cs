#if WINDOWS

using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.CLI;
using UniversalDeviceToolkit.Lib.AutoListeners;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Integrations;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.Overclocking.Amd;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Services;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Startup;

/// <summary>
/// Stops Windows-hosted services before Avalonia tears down the desktop lifetime.
/// This mirrors WPF's shutdown ownership and, in particular, releases keyboard
/// hooks and restores network state before the process exits.
/// </summary>
internal sealed class AvaloniaWindowsShutdownCoordinator
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2);

    public async Task StopAsync()
    {
        await StopPluginsAsync().ConfigureAwait(false);
        await StopNetworkAccelerationAsync().ConfigureAwait(false);

        var stopServices = Task.WhenAll(
            StopServiceAsync<AIController>(controller => controller.StopAsync(), "AI controller"),
            StopServiceAsync<RGBKeyboardBacklightController>(controller => controller.SetLightControlOwnerAsync(false), "RGB keyboard controller"),
            StopServiceAsync<SessionLockUnlockListener>(listener => listener.StopAsync(), "session lock/unlock listener"),
            StopServiceAsync<HWiNFOIntegration>(integration => integration.StopAsync(), "HWiNFO integration"),
            StopServiceAsync<IpcServer>(server => server.StopAsync(), "IPC server"),
            StopServiceAsync<BatteryDischargeRateMonitorService>(monitor => monitor.StopAsync(), "battery monitor"),
            StopServiceAsync<LampArrayController>(controller => controller.StopAsync(), "lamp array controller"),
            StopServiceAsync<NativeWindowsMessageListener>(listener => listener.StopAsync(), "native Windows message listener"));

        await DisposeUserInactivityListenerAsync().ConfigureAwait(false);

        if (await Task.WhenAny(stopServices, Task.Delay(ShutdownTimeout)).ConfigureAwait(false) != stopServices)
        {
            Log.Instance.Trace("Avalonia Windows service shutdown timed out after 2 seconds.");
        }
        else
        {
            await stopServices.ConfigureAwait(false);
        }

        StopSmartKeyHandler();
        await FinalizeRuntimeProfilesAsync().ConfigureAwait(false);
        StopMacroController();
    }

    private static async Task StopPluginsAsync()
    {
        var manager = IoCContainer.TryResolve<IPluginManager>();
        if (manager is null)
            return;

        var shutdownTasks = manager.GetRegisteredPlugins()
            .Select(plugin => Task.Run(() =>
            {
                try
                {
                    plugin.OnShutdown();
                }
                catch (Exception exception)
                {
                    Log.Instance.Warning($"Plugin shutdown failed: {plugin.GetType().Name}", exception);
                }
            }));
        await Task.WhenAll(shutdownTasks).ConfigureAwait(false);
    }

    private static async Task StopNetworkAccelerationAsync()
    {
        var network = IoCContainer.TryResolve<INetworkAccelerationService>();
        if (network is null)
            return;

        try
        {
            await network.StopAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia network acceleration shutdown failed.", exception);
        }
    }

    private static async Task StopServiceAsync<TService>(Func<TService, Task> stop, string name)
        where TService : class
    {
        var service = IoCContainer.TryResolve<TService>();
        if (service is null)
            return;

        try
        {
            await stop(service).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Instance.Trace($"Avalonia {name} shutdown failed.", exception);
        }
    }

    private static async Task DisposeUserInactivityListenerAsync()
    {
        var listener = IoCContainer.TryResolve<UserInactivityAutoListener>();
        if (listener is not IDisposable disposable)
            return;

        try
        {
            await Task.Run(disposable.Dispose).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia user inactivity listener shutdown failed.", exception);
        }
    }

    private static void StopSmartKeyHandler()
    {
        try
        {
            AvaloniaSmartKeyHandler.Stop();
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia smart key handler shutdown failed.", exception);
        }
    }

    private static async Task FinalizeRuntimeProfilesAsync()
    {
        try
        {
            if (IoCContainer.TryResolve<AmdOverclockingController>() is { } amdController && amdController.IsActive())
            {
                amdController.SaveShutdownInfo(new ShutdownInfo
                {
                    Status = "Normal",
                    AbnormalCount = 0
                });
            }

            if (IoCContainer.TryResolve<FanCurveManager>() is { } fanManager &&
                await fanManager.IsSupportedAsync().ConfigureAwait(false))
            {
                await fanManager.SetRegisterAsync(false).ConfigureAwait(false);
            }

            if (IoCContainer.TryResolve<LampArrayController>() is { } lampArrayController &&
                IoCContainer.TryResolve<LampArraySettings>() is { } lampArraySettings)
            {
                lampArrayController.SaveSettings(lampArraySettings);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Runtime profile finalization failed: {ex.Message}", ex);
        }
    }

    private static void StopMacroController()
    {
        try
        {
            IoCContainer.TryResolve<MacroController>()?.Stop();
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia macro controller shutdown failed.", exception);
        }
    }
}

#endif
