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
/// GPU鎺у埗鍣紝鐢ㄤ簬鐩戞帶鍜岀鐞哊VIDIA鐙珛GPU鐘舵€併€?/// </summary>
/// <remarks>
/// <para>
/// 姝ゆ帶鍒跺櫒鎻愪緵浠ヤ笅鍔熻兘锛?/// </para>
/// <list type="bullet">
///   <item><description>GPU鐘舵€佺洃鎺э紙婵€娲汇€侀潪婵€娲汇€佸凡鍏虫満绛夛級</description></item>
///   <item><description>GPU杩涚▼绠＄悊</description></item>
///   <item><description>GPU閲嶅惎鍜岃繘绋嬬粓姝?/description></item>
///   <item><description>鑷€傚簲鍒锋柊闂撮殧锛堟椿璺冩椂2绉掞紝闈炴椿璺冩椂10绉掞級</description></item>
/// </list>
/// <para>
/// 浣跨敤NVAPI涓嶯VIDIA椹卞姩閫氫俊锛岄渶瑕丯VIDIA GPU鏀寔銆?/// </para>
/// </remarks>
public class GPUController : IDisposable
{
    private readonly AsyncLock _lock = new();
    private readonly IGPUProcessManager _processManager;
    private readonly IGPUHardwareManager _hardwareManager;
    private readonly IDelayProvider _delayProvider;
    private volatile bool _disposed = false;

    private Task? _refreshTask;
    private CancellationTokenSource? _refreshCancellationTokenSource;
    private readonly object _startStopLock = new();

    private GPUState _state = GPUState.Unknown;
    private IReadOnlyList<Process> _processes = Array.Empty<Process>();
    private string? _gpuInstanceId;
    private string? _performanceState;
    private int _currentInterval;
    private DateTime _lastStateChangeTime = DateTime.MinValue;
    private const int ActiveInterval = 2000;
    private const int InactiveInterval = 10000;
    private const int StabilizationDelay = 5000;

    /// <summary>
    /// 褰揋PU鐘舵€佸埛鏂版椂瑙﹀彂鐨勪簨浠躲€?    /// </summary>
    public event EventHandler<GPUStatus>? Refreshed;

    /// <summary>
    /// 鑾峰彇GPU鐩戞帶鏈嶅姟鏄惁宸插惎鍔ㄣ€?    /// </summary>
    public bool IsStarted
    {
        get
        {
            lock (_startStopLock)
                return _refreshTask is { IsCompleted: false };
        }
    }

    /// <summary>
    /// 鍒濆鍖朑PUController鐨勬柊瀹炰緥銆?    /// </summary>
    /// <param name="processManager">GPU杩涚▼绠＄悊鍣ㄣ€?/param>
    /// <param name="hardwareManager">GPU纭欢绠＄悊鍣ㄣ€?/param>
    public GPUController(IGPUProcessManager processManager, IGPUHardwareManager hardwareManager, IDelayProvider delayProvider)
    {
        _processManager = processManager;
        _hardwareManager = hardwareManager;
        _delayProvider = delayProvider;
    }

    public async Task<bool> IsSupportedAsync()
    {
        try
        {
            var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

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
        lock (_startStopLock)
        {
            if (_refreshTask is { IsCompleted: false })
                return Task.CompletedTask;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting GPU service [interval={interval}ms]");

            Log.Instance.Info($"GPU monitoring started [controller={nameof(GPUController)}]");

            _currentInterval = interval;
            _refreshCancellationTokenSource?.Dispose();
            _refreshCancellationTokenSource = new CancellationTokenSource();
            var token = _refreshCancellationTokenSource.Token;
            _refreshTask = Task.Run(() => RefreshLoopAsync(delay, interval, token), token);
            return Task.CompletedTask;
        }
    }

    public async Task StopAsync(bool waitForFinish = false)
    {
        Task? taskToWait = null;

        lock (_startStopLock)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping GPU service");

            Log.Instance.Info($"GPU monitoring stopped [controller={nameof(GPUController)}]");

            if (_refreshCancellationTokenSource is not null)
            {
                _refreshCancellationTokenSource.Cancel();
                taskToWait = _refreshTask;
            }

            _refreshCancellationTokenSource = null;
            _refreshTask = null;
        }

        if (waitForFinish && taskToWait is not null)
        {
            try
            {
                await taskToWait.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when GPU service is stopped, no action needed
            }
        }

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

            await _delayProvider.Delay(TimeSpan.FromMilliseconds(delay), token).ConfigureAwait(false);

            while (true)
            {
                token.ThrowIfCancellationRequested();

                using (await _lock.LockAsync(token).ConfigureAwait(false))
                {
                    await RefreshStateAsync().ConfigureAwait(false);
                    Refreshed?.Invoke(this, new GPUStatus(_state, _performanceState, _processes));
                }

                var adjustedInterval = AdjustRefreshInterval();
                if (adjustedInterval > 0)
                    await _delayProvider.Delay(TimeSpan.FromMilliseconds(adjustedInterval), token).ConfigureAwait(false);
                else
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Warning($"GPU controller exception", ex);
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
        var gpuInstanceId = await TryGetGpuInstanceIdAsync(pnpDeviceIdPart).ConfigureAwait(false);
        var processNames = NVAPIExtensions.GetActiveProcesses(gpu);

        DetermineGpuState(gpu, gpuInstanceId, processNames, previousState);
    }

    private static async Task<string?> TryGetGpuInstanceIdAsync(string? pnpDeviceIdPart)
    {
        if (string.IsNullOrWhiteSpace(pnpDeviceIdPart))
            return null;

        try
        {
            var gpuInstanceId = await WMI.Win32.PnpEntity.GetDeviceIDAsync(pnpDeviceIdPart).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(gpuInstanceId) ? null : gpuInstanceId;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void ResetState()
    {
        _state = GPUState.Unknown;
        _processes = Array.Empty<Process>();
        _gpuInstanceId = null;
        _performanceState = null;
    }

    private void HandleGpuNotFound(GPUState previousState)
    {
        _state = GPUState.NvidiaGpuNotFound;
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
            CheckStateChange(_state);
        }
        catch (Exception)
        {
            _performanceState = "Unknown";
        }
    }

    private void DetermineGpuState(NvAPIWrapper.GPU.PhysicalGPU gpu, string? gpuInstanceId, List<Process> processNames, GPUState previousState)
    {
        if (NVAPI.IsDisplayConnected(gpu))
        {
            HandleMonitorConnected(processNames, previousState);
        }
        else if (processNames.Count != 0)
        {
            HandleActive(gpuInstanceId, processNames, previousState);
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
        CheckStateChange(previousState);
    }

    private void HandleActive(string? gpuInstanceId, List<Process> processNames, GPUState previousState)
    {
        _processes = processNames;
        _state = GPUState.Active;
        _gpuInstanceId = gpuInstanceId;
        CheckStateChange(previousState);
    }

    private void HandleInactive(string? gpuInstanceId, GPUState previousState)
    {
        _state = GPUState.Inactive;
        _gpuInstanceId = gpuInstanceId;
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
                            try { process.Dispose(); } catch
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace("Failed to dispose process");
                            }
                        }
                        _processes = Array.Empty<Process>();
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
