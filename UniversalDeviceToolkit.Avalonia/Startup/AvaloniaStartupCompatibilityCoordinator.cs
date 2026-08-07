#if WINDOWS

using Avalonia.Controls;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Startup;

/// <summary>
/// Applies the same hardware compatibility decision as WPF before optional
/// plugins and hardware services are initialized for the Avalonia host.
/// </summary>
internal sealed class AvaloniaStartupCompatibilityCoordinator
{
    public async Task<bool> EnsureCompatibleAsync(Window? owner)
    {
        try
        {
            var (isCompatible, machine) = await Compatibility.IsCompatibleAsync().ConfigureAwait(false);
            if (isCompatible
                || WindowsAvaloniaSettingsService.SharedApplicationSettings
                    .Store.DisableUnsupportedHardwareWarning)
            {
                return true;
            }

            Log.Instance.Trace(
                $"Avalonia incompatible system detected: vendor={machine.Vendor}, " +
                $"model={machine.Model}, type={machine.MachineType}, BIOS={machine.BiosVersion}.");
            return await ShowWarningAsync(owner, machine).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Instance.Error("Avalonia hardware compatibility check failed.", exception);
            return false;
        }
    }

    private static Task<bool> ShowWarningAsync(Window? owner, MachineInformation machine)
    {
        var decision = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                if (owner is null)
                {
                    decision.TrySetResult(false);
                    return;
                }

                var window = new AvaloniaUnsupportedHardwareWindow(machine);
                decision.TrySetResult(await window.ShowDialog<bool>(owner));
            }
            catch (Exception exception)
            {
                Log.Instance.Trace("Avalonia compatibility warning could not be shown.", exception);
                decision.TrySetResult(false);
            }
        });
        return decision.Task;
    }
}

#endif
