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
    private int _disposed;

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
                catch (ObjectDisposedException ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("CTS already disposed during cancellation", ex);
                }
                _cts.Dispose();
            }

            newCts = new CancellationTokenSource();
            _cts = newCts;

            var token = newCts.Token;

            newTask = Task.Run(async () =>
            {
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
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Battery monitoring service failed at iteration {iterationCount}", ex);

                        break;
                    }
                }
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
                catch (ObjectDisposedException ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("CTS already disposed during cancellation", ex);
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
            catch (ObjectDisposedException ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("CTS already disposed during stop cancellation", ex);
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
            catch (OperationCanceledException ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Battery monitoring service wait cancelled", ex);
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
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
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
                catch (ObjectDisposedException ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("CTS already disposed during dispose", ex);
                }
            }

            // Non-blocking dispose wait (Pillar A, BUG-2026-07-09-008): the refresh task runs
            // ConfigureAwait(false) on the threadpool, so a bounded Wait cannot deadlock the
            // Dispatcher, but we still avoid needless blocking in the common completed-task case
            // via an IsCompletedSuccessfully fast path (matching the AIController precedent).
            if (taskToWait is not null)
            {
                try
                {
                    if (!taskToWait.IsCompletedSuccessfully && !taskToWait.Wait(TimeSpan.FromSeconds(5)))
                    {
                        // Timed out: leave the task running and observe any later fault so it is
                        // never raised as an unobserved task exception.
                        taskToWait.ContinueWith(t =>
                        {
                            if (t.IsFaulted && Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace("Battery monitoring service dispose wait timed out and faulted later.", t.Exception);
                        }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                    }
                }
                catch (AggregateException ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("Battery monitoring service dispose wait faulted.", ex);
                }
            }

            ctsToDispose?.Dispose();
        }
    }
}
