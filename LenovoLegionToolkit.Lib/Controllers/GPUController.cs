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

/// <summary>
/// GPU控制器，用于监控和管理NVIDIA独立GPU状态。
/// </summary>
/// <remarks>
/// <para>
/// 此控制器提供以下功能：
/// </para>
/// <list type="bullet">
///   <item><description>GPU状态监控（激活、非激活、已关机等）</description></item>
///   <item><description>GPU进程管理</description></item>
///   <item><description>GPU重启和进程终止</description></item>
///   <item><description>自适应刷新间隔（活跃时2秒，非活跃时10秒）</description></item>
/// </list>
/// <para>
/// 使用NVAPI与NVIDIA驱动通信，需要NVIDIA GPU支持。
/// </para>
/// </remarks>
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

    /// <summary>
    /// 当GPU状态刷新时触发的事件。
    /// </summary>
    public event EventHandler<GPUStatus>? Refreshed;
    
    /// <summary>
    /// 获取GPU监控服务是否已启动。
    /// </summary>
    public bool IsStarted { get => _refreshTask != null; }

    /// <summary>
    /// 初始化GPUController的新实例。
    /// </summary>
    /// <param name="processManager">GPU进程管理器。</param>
    /// <param name="hardwareManager">GPU硬件管理器。</param>
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
            await RefreshStateAsync().ConfigureAwait(false);
            return new GPUStatus(_state, _performanceState, _processes);
        }
    }

    public Task StartAsync(int delay = 1_000, int interval = 5_000)
    {
        if (IsStarted)
            return Task.CompletedTask;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Starting GPU service [interval={interval}ms]");

        _currentInterval = interval;
        _refreshCancellationTokenSource = new CancellationTokenSource();
        var token = _refreshCancellationTokenSource.Token;
        _refreshTask = Task.Run(() => RefreshLoopAsync(delay, interval, token), token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(bool waitForFinish = false)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Stopping GPU service");

        if (_refreshCancellationTokenSource is not null)
            await _refreshCancellationTokenSource.CancelAsync().ConfigureAwait(false);

        if (waitForFinish && _refreshTask is not null)
        {
            try
            {
                await _refreshTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        _refreshCancellationTokenSource = null;
        _refreshTask = null;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"GPU service stopped");
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
                Log.Instance.Trace($"Initializing NVAPI");

            NVAPI.Initialize();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"NVAPI initialized");

            await Task.Delay(delay, token).ConfigureAwait(false);

            while (true)
            {
                token.ThrowIfCancellationRequested();

                using (await _lock.LockAsync(token).ConfigureAwait(false))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Refreshing GPU state");

                    await RefreshStateAsync().ConfigureAwait(false);
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
                Log.Instance.Trace($"GPU controller exception", ex);

            throw;
        }
        finally
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unloading NVAPI");

            NVAPI.Unload();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"NVAPI unloaded");
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
                        Log.Instance.Trace($"Refresh interval: {ActiveInterval}ms (active)");
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
                        Log.Instance.Trace($"Refresh interval: {InactiveInterval}ms (inactive)");
                    _currentInterval = InactiveInterval;
                }
                return InactiveInterval;
            }
        }

        return _currentInterval;
    }

    private async Task RefreshStateAsync()
    {
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
            Log.Instance.Trace($"Nvidia GPU not found");

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
                Log.Instance.Trace($"GPU powered off");

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
            Log.Instance.Trace($"Monitor connected");

        CheckStateChange(previousState);
    }

    private void HandleActive(string? gpuInstanceId, List<Process> processNames, string pnpDeviceIdPart, GPUState previousState)
    {
        _processes = processNames;
        _state = GPUState.Active;
        _gpuInstanceId = gpuInstanceId;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"GPU active [{_processes.Count} processes]");

        CheckStateChange(previousState);
    }

    private void HandleInactive(string? gpuInstanceId, GPUState previousState)
    {
        _state = GPUState.Inactive;
        _gpuInstanceId = gpuInstanceId;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"GPU inactive");

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
                try
                {
                    if (_refreshCancellationTokenSource != null)
                    {
                        _refreshCancellationTokenSource.Cancel();
                        _refreshCancellationTokenSource.Dispose();
                        _refreshCancellationTokenSource = null;
                    }

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
                        Log.Instance.Trace($"GPUController disposal error", ex);
                }
            }

            _disposed = true;
        }
    }
}
