using System;
using System.Collections.Generic;
using System.Management;

namespace HardwareValidation;

internal static class NativeFanProbe
{
    public static void Dump()
    {
        DumpClass("LENOVO_FAN_METHOD");
        DumpClass("LENOVO_GAMEZONE_DATA");
        DumpClass("LENOVO_OTHER_METHOD");
        Invoke("LENOVO_FAN_METHOD", "Fan_GetCurrentFanSpeed", new() { ["FanID"] = 0 });
        Invoke("LENOVO_FAN_METHOD", "Fan_GetCurrentFanSpeed", new() { ["FanID"] = 1 });
        Invoke("LENOVO_FAN_METHOD", "Fan_GetCurrentFanSpeed", new() { ["FanID"] = 2 });
        Invoke("LENOVO_GAMEZONE_DATA", "GetFanCount", []);
        Invoke("LENOVO_GAMEZONE_DATA", "GetFan1Speed", []);
        Invoke("LENOVO_GAMEZONE_DATA", "GetFan2Speed", []);
        InvokeNull("LENOVO_GAMEZONE_DATA", "GetFanCount");
        InvokeNull("LENOVO_GAMEZONE_DATA", "GetFan1Speed");
        InvokeNull("LENOVO_GAMEZONE_DATA", "GetFan2Speed");
        InvokeClass("LENOVO_GAMEZONE_DATA", "GetFanCount", []);
        InvokeClass("LENOVO_GAMEZONE_DATA", "GetFan1Speed", []);
        InvokeClass("LENOVO_GAMEZONE_DATA", "GetFan2Speed", []);
        Invoke("LENOVO_OTHER_METHOD", "GetFeatureValue", new() { ["IDs"] = 0x04030001u });
        Invoke("LENOVO_OTHER_METHOD", "GetFeatureValue", new() { ["IDs"] = 0x04030002u });
        Invoke("LENOVO_OTHER_METHOD", "GetFeatureValue", new() { ["IDs"] = 0x04030001 });
        Invoke("LENOVO_OTHER_METHOD", "GetFeatureValue", new() { ["IDs"] = 0x04030002 });
    }

    private static void DumpClass(string className)
    {
        Console.WriteLine($"NativeClass: {className}");
        try
        {
            using var managementClass = new ManagementClass("root\\WMI", className, null);
            foreach (MethodData method in managementClass.Methods)
            {
                if (!method.Name.Contains("Fan", StringComparison.OrdinalIgnoreCase) &&
                    !method.Name.Contains("FeatureValue", StringComparison.OrdinalIgnoreCase))
                    continue;
                object? isStatic = null;
                try { isStatic = method.Qualifiers["Static"]?.Value; } catch (ManagementException) { }
                Console.WriteLine($"  Method: {method.Name} Static={isStatic ?? "<null>"}");
                DumpProperties("    In", method.InParameters?.Properties);
                DumpProperties("    Out", method.OutParameters?.Properties);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ClassError: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
        }
    }



    private static void InvokeNull(string className, string methodName)
    {
        Console.WriteLine($"NativeInvokeNull: {className}.{methodName}");
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\WMI", $"SELECT * FROM {className}");
            using var collection = searcher.Get();
            foreach (ManagementObject instance in collection)
            {
                using (instance)
                using (var output = instance.InvokeMethod(methodName, null, null))
                {
                    DumpProperties("  Result", output?.Properties);
                    return;
                }
            }
            Console.WriteLine("  NoInstance");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  InvokeNullError: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
        }
    }

    private static void InvokeClass(string className, string methodName, Dictionary<string, object> values)
    {
        Console.WriteLine($"NativeClassInvoke: {className}.{methodName}");
        try
        {
            using var managementClass = new ManagementClass("root\\WMI", className, null);
            ManagementBaseObject? input = null;
            if (values.Count > 0)
            {
                input = managementClass.GetMethodParameters(methodName);
                foreach (var pair in values)
                    input[pair.Key] = pair.Value;
            }
            using var output = managementClass.InvokeMethod(methodName, input, null);
            DumpProperties("  Result", output?.Properties);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ClassInvokeError: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
        }
    }

    private static void Invoke(string className, string methodName, Dictionary<string, object> values)
    {
        Console.WriteLine($"NativeInvoke: {className}.{methodName}");
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\WMI", $"SELECT * FROM {className}");
            using var collection = searcher.Get();
            var index = 0;
            foreach (ManagementObject instance in collection)
            {
                using (instance)
                {
                    Console.WriteLine($"  Instance[{index}]: Path={instance.Path?.Path ?? "<null>"} RelPath={instance["__RELPATH"] ?? "<null>"}");
                    try
                    {
                        var input = instance.GetMethodParameters(methodName);
                        foreach (var pair in values)
                            input[pair.Key] = pair.Value;
                        using var output = instance.InvokeMethod(methodName, input, null);
                        DumpProperties($"  Result[{index}]", output?.Properties);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  InvokeError[{index}]: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
                    }
                    index++;
                }
            }
            Console.WriteLine($"  InstanceCount: {index}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  QueryError: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
        }
    }

    private static void DumpProperties(string prefix, PropertyDataCollection? properties)
    {
        if (properties is null)
        {
            Console.WriteLine($"{prefix}: none");
            return;
        }
        foreach (PropertyData property in properties)
            Console.WriteLine($"{prefix}: {property.Name}={property.Value ?? "<null>"} Type={property.Type}");
    }
}
