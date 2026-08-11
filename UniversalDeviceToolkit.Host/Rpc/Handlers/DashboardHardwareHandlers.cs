using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Bridges dashboard controls that are not represented by IFeature&lt;T&gt;.
/// </summary>
public static class DashboardHardwareHandlers
{
    private static readonly object GpuStatusLock = new();
    private static GPUController? _subscribedGpuController;
    private static GPUStatus? _lastGpuStatus;

    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("dashboardHardware.getState", (request, _) => HandleGetStateAsync());
        rpc.RegisterHandler("dashboardHardware.killGpuProcesses", (request, _) => HandleKillGpuProcessesAsync());
        rpc.RegisterHandler("dashboardHardware.restartGpu", (request, _) => HandleRestartGpuAsync());
        rpc.RegisterHandler("dashboardHardware.setOverclockEnabled", (request, _) => HandleSetOverclockEnabledAsync(request));
        rpc.RegisterHandler("dashboardHardware.setOverclock", (request, _) => HandleSetOverclockAsync(request));
        rpc.RegisterHandler("dashboardHardware.turnOffMonitors", (request, _) => HandleTurnOffMonitorsAsync());
    }

    private static async Task<BridgeResult> HandleGetStateAsync()
    {
        try
        {
            var gpuController = IoCContainer.Resolve<GPUController>();
            var discreteGpuSupported = await gpuController.IsSupportedAsync().ConfigureAwait(false);
            GPUStatus? gpuStatus = null;

            if (discreteGpuSupported)
            {
                EnsureGpuStatusSubscription(gpuController);
                await gpuController.StartAsync(delay: 0, interval: 5_000).ConfigureAwait(false);
                gpuStatus = await WaitForInitialGpuStatusAsync().ConfigureAwait(false);
            }

            var overclockController = IoCContainer.Resolve<GPUOverclockController>();
            var overclockSupported = await overclockController.IsSupportedAsync().ConfigureAwait(false);
            var (overclockEnabled, overclockInfo) = overclockSupported
                ? overclockController.GetState()
                : (false, GPUOverclockInfo.Zero);

            return BridgeResult.Ok(new
            {
                discreteGpu = new
                {
                    supported = discreteGpuSupported,
                    state = gpuStatus?.State.ToString() ?? GPUState.Unknown.ToString(),
                    performanceState = gpuStatus?.PerformanceState,
                    processes = gpuStatus is { } status
                        ? GetProcessNames(status.Processes)
                        : Array.Empty<string>(),
                },
                overclockDiscreteGpu = new
                {
                    supported = overclockSupported,
                    enabled = overclockEnabled,
                    coreDeltaMhz = overclockInfo.CoreDeltaMhz,
                    memoryDeltaMhz = overclockInfo.MemoryDeltaMhz,
                    maxCoreDeltaMhz = GPUOverclockController.GetMaxCoreDeltaMhz(),
                    maxMemoryDeltaMhz = overclockSupported
                        ? GPUOverclockController.GetMaxMemoryDeltaMhz()
                        : 0,
                },
                turnOffMonitors = new { supported = OperatingSystem.IsWindows() },
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleKillGpuProcessesAsync()
    {
        try
        {
            await IoCContainer.Resolve<GPUController>().KillGPUProcessesAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleRestartGpuAsync()
    {
        try
        {
            await IoCContainer.Resolve<GPUController>().RestartGPUAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSetOverclockEnabledAsync(BridgeRequest request)
    {
        try
        {
            var enabled = GetRequiredBoolean(request, "enabled");
            var controller = IoCContainer.Resolve<GPUOverclockController>();
            var (_, info) = controller.GetState();
            controller.SaveState(enabled, info);
            await controller.ApplyStateAsync(force: true).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true, enabled });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSetOverclockAsync(BridgeRequest request)
    {
        try
        {
            var coreDeltaMhz = GetRequiredInt32(request, "coreDeltaMhz");
            var memoryDeltaMhz = GetRequiredInt32(request, "memoryDeltaMhz");
            var maxCoreDeltaMhz = GPUOverclockController.GetMaxCoreDeltaMhz();
            var maxMemoryDeltaMhz = GPUOverclockController.GetMaxMemoryDeltaMhz();

            if (coreDeltaMhz < 0 || coreDeltaMhz > maxCoreDeltaMhz ||
                memoryDeltaMhz < 0 || memoryDeltaMhz > maxMemoryDeltaMhz)
            {
                throw new BridgeErrorException(-32602, "GPU overclock offsets are outside the supported range.");
            }

            var controller = IoCContainer.Resolve<GPUOverclockController>();
            var (enabled, _) = controller.GetState();
            controller.SaveState(enabled, new GPUOverclockInfo(coreDeltaMhz, memoryDeltaMhz));
            if (enabled)
                await controller.ApplyStateAsync().ConfigureAwait(false);

            return BridgeResult.Ok(new { ok = true });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleTurnOffMonitorsAsync()
    {
        try
        {
            await IoCContainer.Resolve<NativeWindowsMessageListener>().TurnOffMonitorAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void EnsureGpuStatusSubscription(GPUController controller)
    {
        lock (GpuStatusLock)
        {
            if (ReferenceEquals(_subscribedGpuController, controller))
                return;

            if (_subscribedGpuController is not null)
                _subscribedGpuController.Refreshed -= GpuControllerOnRefreshed;

            _subscribedGpuController = controller;
            _subscribedGpuController.Refreshed += GpuControllerOnRefreshed;
        }
    }

    private static void GpuControllerOnRefreshed(object? sender, GPUStatus status)
    {
        lock (GpuStatusLock)
            _lastGpuStatus = status;
    }

    private static async Task<GPUStatus?> WaitForInitialGpuStatusAsync()
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            lock (GpuStatusLock)
            {
                if (_lastGpuStatus is { } status)
                    return status;
            }

            await Task.Delay(50).ConfigureAwait(false);
        }

        return null;
    }

    private static string[] GetProcessNames(IReadOnlyList<Process> processes) => processes
        .Select(TryGetProcessName)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Select(name => name!)
        .GroupBy(name => name, StringComparer.CurrentCultureIgnoreCase)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
        .Select(group => group.Count() > 1 ? $"{group.Key} x {group.Count()}" : group.Key)
        .ToArray();

    private static string? TryGetProcessName(Process process)
    {
        try
        {
            return process.HasExited ? null : process.ProcessName;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static bool GetRequiredBoolean(BridgeRequest request, string name)
    {
        if (!request.Parameters.TryGetProperty(name, out var property) ||
            property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new BridgeErrorException(-32602, $"Missing boolean '{name}' parameter.");
        }

        return property.GetBoolean();
    }

    private static int GetRequiredInt32(BridgeRequest request, string name)
    {
        if (!request.Parameters.TryGetProperty(name, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt32(out var value))
        {
            throw new BridgeErrorException(-32602, $"Missing integer '{name}' parameter.");
        }

        return value;
    }
}
