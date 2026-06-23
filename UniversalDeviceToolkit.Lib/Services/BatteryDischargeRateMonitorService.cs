using System;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Services;

public class BatteryDischargeRateMonitorService(IDelayProvider? delayProvider = null) : IDisposable
{
    private readonly IDelayProvider _delayProvider = delayProvider ?? new DefaultDelayProvider();
    private CancellationTokenSource? _cts;
    private Task? _refreshTask;
    private readonly object _lock = new();
    private bool _disposed;

    public Task StartStopIfNeededAsync()
    {
        if (!Battery.IsBatteryMonitoringSupported())
            return Task.CompletedTask;

        CancellationTokenSource? newCts = null;
        Task? newTask = null;

        lock (_lock)
        {
            if (_refreshTask != null)
                return Task.CompletedTask;

            if (_cts is not null)
            {
                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                _cts.Dispose();
            }

            newCts = new CancellationTokenSource();
            _cts = newCts;

            var token = newCts.Token;

            newTask = Task.Run(async () =>
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Battery monitoring service started");

                var iterationCount = 0;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        iterationCount++;

                        if (iterationCount > 1000)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Battery monitoring service exceeded safe iteration limit ({iterationCount})");
                            break;
                        }

                        Battery.SetMinMaxDischargeRate();

                        await _delayProvider.Delay(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Battery monitoring service cancelled");
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Battery monitoring service failed at iteration {iterationCount}", ex);

                        break;
                    }
                }

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Battery monitoring service stopped");
            }, token);

            _refreshTask = newTask;
        }

        _ = newTask.ContinueWith(t =>
        {
            lock (_lock)
            {
                if (ReferenceEquals(_refreshTask, t))
                    _refreshTask = null;
            }

            if (t.IsFaulted && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Battery monitoring service task faulted.", t.Exception);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? ctsToDispose = null;
        Task? taskToWait = null;

        lock (_lock)
        {
            if (_cts is not null)
            {
                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                ctsToDispose = _cts;
                _cts = null;
            }

            taskToWait = _refreshTask;
            _refreshTask = null;
        }

        if (ctsToDispose is not null)
        {
            try
            {
                ctsToDispose.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        if (taskToWait is not null)
        {
            try
            {
                await taskToWait.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Battery monitoring service did not stop in time.");
            }
            catch (OperationCanceledException)
            {
            }
        }

        ctsToDispose?.Dispose();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Battery monitoring service stopped");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            CancellationTokenSource? ctsToDispose = null;
            Task? taskToWait = null;

            lock (_lock)
            {
                if (_cts is not null)
                {
                    ctsToDispose = _cts;
                    _cts = null;
                }
                taskToWait = _refreshTask;
                _refreshTask = null;
            }

            if (ctsToDispose is not null)
            {
                try
                {
                    ctsToDispose.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                ctsToDispose.Dispose();
            }

            taskToWait?.Wait(TimeSpan.FromSeconds(5));
        }

        _disposed = true;
    }
}
