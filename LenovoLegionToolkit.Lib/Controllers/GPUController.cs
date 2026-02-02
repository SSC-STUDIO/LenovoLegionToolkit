using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Resources;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;
using NeoSmart.AsyncLock;

namespace LenovoLegionToolkit.Lib.Controllers;

public class GPUController : IDisposable
{
    private readonly AsyncLock _lock = new();
    private readonly IGPUProcessManager _processManager;
    private readonly IGPUHardwareManager _hardwareManager;
    private volatile bool _disposed = false;

    private Task? _refreshTask;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    private GPUState _state = GPUState.Unknown;
    private List<Process> _processes = [];
    private string? _gpuInstanceId;
    private string? _performanceState;
    private int _currentInterval;
    private DateTime _lastStateChangeTime = DateTime.MinValue;
    private const int ActiveInterval = 2000;
    private const int InactiveInterval = 10000;
    private const int StabilizationDelay = 5000;

    public event EventHandler<GPUStatus>? Refreshed;
    public bool IsStarted { get => _refreshTask != null; }

    public GPUController(IGPUProcessManager processManager, IGPUHardwareManager hardwareManager)
    {
        _processManager = processManager;
        _hardwareManager = hardwareManager;
    }

    public bool IsSupported()
    {
        try
        {
            var mi = Compatibility.GetMachineInformationAsync().GetAwaiter().GetResult();
            
            // Strictly disable specialized machine features on incompatible machines
            if (!Compatibility.IsSupportedLegionMachine(mi))
                return false;

            NVAPI.Initialize();
            return NVAPI.GetGPU() is not null;
        }
        catch
        {
            return false;
        }
        finally
        {
            try
            {
                NVAPI.Unload();
            }
            catch (Exception ex) 
            { 
                // Log NVAPI unload failures but don't throw
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"NVAPI unload failed: {ex.Message}", ex);
            }
        }
    }

    public async Task<GPUState> GetLastKnownStateAsync()
    {
        using (await _lock.LockAsync().ConfigureAwait(false))
            return _state;
    }

    public async Task<GPUStatus> RefreshNowAsync()
    {
        using (await _lock.LockAsync().ConfigureAwait(false))
        {
            await RefreshLoopAsync(0, 0, CancellationToken.None).ConfigureAwait(false);
            return new GPUStatus(_state, _performanceState, _processes);
        }
    }

    public Task StartAsync(int delay = 1_000, int interval = 5_000)
    {
        if (IsStarted)
            return Task.CompletedTask;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Starting... [delay={delay}, interval={interval}]");

        _currentInterval = interval;
        _refreshCancellationTokenSource = new CancellationTokenSource();
        var token = _refreshCancellationTokenSource.Token;
        _refreshTask = Task.Run(() => RefreshLoopAsync(delay, interval, token), token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(bool waitForFinish = false)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Stopping... [refreshTask.isNull={_refreshTask is null}, _refreshCancellationTokenSource.IsCancellationRequested={_refreshCancellationTokenSource?.IsCancellationRequested}]");

        if (_refreshCancellationTokenSource is not null)
            await _refreshCancellationTokenSource.CancelAsync().ConfigureAwait(false);

        if (waitForFinish)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Waiting to finish...");

            if (_refreshTask is not null)
            {
                try
                {
                    await _refreshTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Finished");
        }

        _refreshCancellationTokenSource = null;
        _refreshTask = null;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Stopped");
    }

    public async Task RestartGPUAsync()
    {
        using (await _lock.LockAsync().ConfigureAwait(false))
        {
            if (_state is not GPUState.Active and not GPUState.Inactive)
                return;

            if (string.IsNullOrEmpty(_gpuInstanceId))
                return;

            await _hardwareManager.RestartGPUAsync(_gpuInstanceId).ConfigureAwait(false);
        }
    }

    public async Task KillGPUProcessesAsync()
    {
        using (await _lock.LockAsync().ConfigureAwait(false))
        {
            if (_state is not GPUState.Active)
                return;

            if (_processes.Count == 0)
                return;

            await _processManager.KillGPUProcessesAsync(_processes).ConfigureAwait(false);
        }
    }

    private async Task RefreshLoopAsync(int delay, int interval, CancellationToken token)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Initializing NVAPI...");

            NVAPI.Initialize();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Initialized NVAPI");

            await Task.Delay(delay, token).ConfigureAwait(false);

            while (true)
            {
                token.ThrowIfCancellationRequested();

                using (await _lock.LockAsync(token).ConfigureAwait(false))
                {

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Will refresh...");

                    await RefreshStateAsync().ConfigureAwait(false);

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Refreshed");

                    Refreshed?.Invoke(this, new GPUStatus(_state, _performanceState, _processes));
                }

                var adjustedInterval = AdjustRefreshInterval();
                if (adjustedInterval > 0)
                    await Task.Delay(adjustedInterval, token).ConfigureAwait(false);
                else
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception occurred", ex);

            throw;
        }
        finally
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unloading NVAPI...");

            NVAPI.Unload();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unloaded NVAPI");
        }
    }

    private int AdjustRefreshInterval()
    {
        var now = DateTime.UtcNow;
        var timeSinceStateChange = (now - _lastStateChangeTime).TotalMilliseconds;

        if (_state == GPUState.Active)
        {
            if (timeSinceStateChange > StabilizationDelay)
            {
                if (_currentInterval != ActiveInterval)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Adjusting interval to active mode: {ActiveInterval}ms");
                    _currentInterval = ActiveInterval;
                }
                return ActiveInterval;
            }
        }
        else if (_state == GPUState.Inactive || _state == GPUState.MonitorConnected)
        {
            if (timeSinceStateChange > StabilizationDelay)
            {
                if (_currentInterval != InactiveInterval)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Adjusting interval to inactive mode: {InactiveInterval}ms");
                    _currentInterval = InactiveInterval;
                }
                return InactiveInterval;
            }
        }

        return _currentInterval;
    }

    private async Task RefreshStateAsync()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Refresh in progress...");

        var previousState = _state;
        ResetState();

        var gpu = NVAPI.GetGPU();
        if (gpu is null)
        {
            HandleGpuNotFound(previousState);
            return;
        }

        TryGetPerformanceState(gpu);

        var pnpDeviceIdPart = NVAPI.GetGPUId(gpu);
        if (string.IsNullOrEmpty(pnpDeviceIdPart))
            throw new InvalidOperationException("pnpDeviceIdPart is null or empty");

        var gpuInstanceId = await WMI.Win32.PnpEntity.GetDeviceIDAsync(pnpDeviceIdPart).ConfigureAwait(false);
        var processNames = NVAPIExtensions.GetActiveProcesses(gpu);

        DetermineGpuState(gpu, gpuInstanceId, processNames, pnpDeviceIdPart, previousState);
    }

    private void ResetState()
    {
        _state = GPUState.Unknown;
        _processes = [];
        _gpuInstanceId = null;
        _performanceState = null;
    }

    private void HandleGpuNotFound(GPUState previousState)
    {
        _state = GPUState.NvidiaGpuNotFound;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"GPU present [state={_state}, processes.Count={_processes.Count}, gpuInstanceId={_gpuInstanceId}]");

        CheckStateChange(previousState);
    }

    private void TryGetPerformanceState(NvAPIWrapper.GPU.PhysicalGPU gpu)
    {
        try
        {
            var stateId = gpu.PerformanceStatesInfo.CurrentPerformanceState.StateId.ToString().GetUntilOrEmpty("_");
            _performanceState = Resource.GPUController_PoweredOn;
            if (!string.IsNullOrWhiteSpace(stateId))
                _performanceState += $", {stateId}";
        }
        catch (Exception ex) when (ex.Message == "NVAPI_GPU_NOT_POWERED")
        {
            _state = GPUState.PoweredOff;
            _performanceState = Resource.GPUController_PoweredOff;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Powered off [state={_state}, processes.Count={_processes.Count}, gpuInstanceId={_gpuInstanceId}]");

            CheckStateChange(_state);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GPU status exception.", ex);

            _performanceState = "Unknown";
        }
    }

    private void DetermineGpuState(NvAPIWrapper.GPU.PhysicalGPU gpu, string? gpuInstanceId, List<Process> processNames, string pnpDeviceIdPart, GPUState previousState)
    {
        if (NVAPI.IsDisplayConnected(gpu))
        {
            HandleMonitorConnected(processNames, previousState);
        }
        else if (processNames.Count != 0)
        {
            HandleActive(gpuInstanceId, processNames, pnpDeviceIdPart, previousState);
        }
        else
        {
            HandleInactive(gpuInstanceId, previousState);
        }
    }

    private void HandleMonitorConnected(List<Process> processNames, GPUState previousState)
    {
        _processes = processNames;
        _state = GPUState.MonitorConnected;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Monitor connected [state={_state}, processes.Count={_processes.Count}, gpuInstanceId={_gpuInstanceId}]");

        CheckStateChange(previousState);
    }

    private void HandleActive(string? gpuInstanceId, List<Process> processNames, string pnpDeviceIdPart, GPUState previousState)
    {
        _processes = processNames;
        _state = GPUState.Active;
        _gpuInstanceId = gpuInstanceId;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Active [state={_state}, processes.Count={_processes.Count}, gpuInstanceId={_gpuInstanceId}, pnpDeviceIdPart={pnpDeviceIdPart}]");

        CheckStateChange(previousState);
    }

    private void HandleInactive(string? gpuInstanceId, GPUState previousState)
    {
        _state = GPUState.Inactive;
        _gpuInstanceId = gpuInstanceId;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Inactive [state={_state}, processes.Count={_processes.Count}, gpuInstanceId={_gpuInstanceId}]");

        CheckStateChange(previousState);
    }

    private void CheckStateChange(GPUState previousState)
    {
        if (_state != previousState)
        {
            _lastStateChangeTime = DateTime.UtcNow;
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GPU state changed from {previousState} to {_state}");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                try
                {
                    // Cancel refresh task
                    if (_refreshCancellationTokenSource != null)
                    {
                        _refreshCancellationTokenSource.Cancel();
                        _refreshCancellationTokenSource.Dispose();
                        _refreshCancellationTokenSource = null;
                    }

                    // Dispose all processes in the list
                    if (_processes != null)
                    {
                        foreach (var process in _processes)
                        {
                            try { process.Dispose(); } catch { /* Ignore */ }
                        }
                        _processes.Clear();
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error during GPUController disposal", ex);
                }
            }

            _disposed = true;
        }
    }
}
