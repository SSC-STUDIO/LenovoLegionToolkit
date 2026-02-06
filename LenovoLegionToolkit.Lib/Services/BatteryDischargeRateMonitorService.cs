using System;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Services;

public class BatteryDischargeRateMonitorService : IDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _refreshTask;
    private readonly object _lock = new();
    private bool _disposed;

    public async Task StartStopIfNeededAsync()
    {
        if (!Battery.TestBatterySupport())
            return;

        CancellationTokenSource? newCts = null;
        Task? newTask = null;

        lock (_lock)
        {
            if (_refreshTask != null)
                return;

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

                        await Task.Delay(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
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

        if (newTask is not null)
            await newTask.ConfigureAwait(false);
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
                await ctsToDispose.CancelAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
            }
            ctsToDispose.Dispose();
        }

        if (taskToWait is not null)
            await taskToWait.ConfigureAwait(false);

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
