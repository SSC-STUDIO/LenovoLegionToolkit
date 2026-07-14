using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UniversalDeviceToolkit.Lib.Utils;
using NvAPIWrapper.GPU;
using NvAPIWrapper.Native;

namespace UniversalDeviceToolkit.Lib.Extensions;

public static class NVAPIExtensions
{
    private static readonly string[] Exclusions =
    [
        "dwm.exe",
        "explorer.exe",
    ];

    public static List<Process> GetActiveProcesses(PhysicalGPU gpu)
    {
        var processes = new List<Process>();
        var apps = GPUApi.QueryActiveApps(gpu.Handle).Where(app => !Exclusions.Contains(app.ProcessName, StringComparer.OrdinalIgnoreCase));

        foreach (var app in apps)
        {
            try
            {
                var process = Process.GetProcessById(app.ProcessId);
                processes.Add(process);
            }
            catch (ArgumentException ex)
            {
                // Process may have exited, skip this process
                Log.Instance.TraceOnce(
                    "nvapi-active-process-gone",
                    "NVAPI active app process already exited while enumerating.",
                    ex);
            }
        }

        return processes;
    }
}
