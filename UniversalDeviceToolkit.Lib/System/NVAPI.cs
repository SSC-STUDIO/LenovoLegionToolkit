using System.Collections.Generic;
using System.Linq;
using UniversalDeviceToolkit.Lib.Utils;
using NvAPIWrapper;
using NvAPIWrapper.Display;
using NvAPIWrapper.GPU;
using NvAPIWrapper.Native.Exceptions;
using NvAPIWrapper.Native.GPU;

namespace UniversalDeviceToolkit.Lib.System;

// TODO(#142): NvAPIWrapper.Net 0.8.1.101 is no longer actively maintained and
// surfaces as a Dependabot/Renovate warning. Plan a replacement with P/Invoke
// generated through Microsoft.Windows.CsWin32 (already a direct dependency)
// or hand-written wrappers over nvapi.h. The surface area used here is small
// (Initialize, Unload, PhysicalGPU enumeration, Display enumeration, GPU bus
// info), so a manual migration is feasible but out of scope for the current
// pass. Tracked in issue #142.
internal static class NVAPI
{
    public static void Initialize() => NVIDIA.Initialize();

    public static void Unload() => NVIDIA.Unload();

    public static PhysicalGPU? GetGPU()
    {
        try
        {
            return PhysicalGPU.GetPhysicalGPUs().FirstOrDefault(gpu => gpu.SystemType == SystemType.Laptop);
        }
        catch (NVIDIAApiException ex)
        {
            Log.Instance.TraceOnce("nvapi-get-gpu", "NVAPI GetPhysicalGPUs failed (driver unloaded or unsupported).", ex);
            return null;
        }
    }

    public static bool IsDisplayConnected(PhysicalGPU gpu)
    {
        try
        {
            return Display.GetDisplays().Any(d => d.PhysicalGPUs.Contains(gpu, PhysicalGPUEqualityComparer.Instance));
        }
        catch (NVIDIAApiException ex)
        {
            Log.Instance.TraceOnce("nvapi-display-connected", "NVAPI display connection probe failed.", ex);
            return false;
        }
    }

    public static string? GetGPUId(PhysicalGPU gpu)
    {
        try
        {
            return gpu.BusInformation.PCIIdentifiers.ToString();
        }
        catch (NVIDIAApiException ex)
        {
            Log.Instance.TraceOnce("nvapi-gpu-id", "NVAPI PCIIdentifiers read failed.", ex);
            return null;
        }
    }

    private class PhysicalGPUEqualityComparer : IEqualityComparer<PhysicalGPU>
    {
        public static readonly PhysicalGPUEqualityComparer Instance = new();

        private PhysicalGPUEqualityComparer() { }

        public bool Equals(PhysicalGPU? x, PhysicalGPU? y) => x?.GPUId == y?.GPUId;

        public int GetHashCode(PhysicalGPU obj) => obj.GPUId.GetHashCode();
    }
}
