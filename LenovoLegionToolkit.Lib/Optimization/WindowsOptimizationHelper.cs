using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using LenovoLegionToolkit.Lib.Utils;
using Microsoft.Win32;
using ToolkitRegistry = LenovoLegionToolkit.Lib.System.Registry;

namespace LenovoLegionToolkit.Lib.Optimization;

internal static class WindowsOptimizationHelper
{
    public static bool AreRegistryTweaksApplied(IEnumerable<RegistryValueDefinition> tweaks)
    {
        foreach (var tweak in tweaks)
        {
            try
            {
                var currentValue = ToolkitRegistry.GetValue<object?>(tweak.Hive, tweak.SubKey, tweak.ValueName, null);
                if (currentValue is null)
                    return false;

                if (!RegistryValueEquals(currentValue, tweak.Value, tweak.Kind))
                    return false;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    public static bool RegistryValueEquals(object currentValue, object expectedValue, RegistryValueKind kind)
    {
        try
        {
            return kind switch
            {
                RegistryValueKind.DWord or RegistryValueKind.QWord => Convert.ToInt64(currentValue) == Convert.ToInt64(expectedValue),
                RegistryValueKind.String or RegistryValueKind.ExpandString => string.Equals(Convert.ToString(currentValue), Convert.ToString(expectedValue), StringComparison.Ordinal),
                _ => Equals(currentValue, expectedValue)
            };
        }
        catch
        {
            return false;
        }
    }

    public static void ApplyRegistryTweak(RegistryValueDefinition tweak)
    {
        try
        {
            ToolkitRegistry.SetValue(tweak.Hive, tweak.SubKey, tweak.ValueName, tweak.Value, true, tweak.Kind);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to apply registry tweak. [hive={tweak.Hive}, key={tweak.SubKey}, value={tweak.ValueName}]", ex);
        }
    }

    public static bool AreServicesDisabled(IEnumerable<string> services)
    {
        foreach (var serviceName in services.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var startValue = ToolkitRegistry.GetValue<int>("HKEY_LOCAL_MACHINE",
                    $@"SYSTEM\CurrentControlSet\Services\{serviceName}", "Start", -1);
                if (startValue != 4)
                    return false;
            }
            catch
            {
                return false;
            }

            try
            {
                using var serviceController = new ServiceController(serviceName);
                if (serviceController.Status is not ServiceControllerStatus.Stopped and not ServiceControllerStatus.StopPending)
                    return false;
            }
            catch (InvalidOperationException)
            {
                // Service not found – treat as already disabled.
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    public static void DisableService(string serviceName)
    {
        try
        {
            ToolkitRegistry.SetValue("HKEY_LOCAL_MACHINE", $@"SYSTEM\CurrentControlSet\Services\{serviceName}", "Start", 4, true, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to set service start type. [service={serviceName}]", ex);
        }

        try
        {
            using var serviceController = new ServiceController(serviceName);

            if (serviceController.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
                return;

            serviceController.Stop();
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
        }
        catch (InvalidOperationException)
        {
            // Service not found, ignore.
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to stop service. [service={serviceName}]", ex);
        }
    }
}
