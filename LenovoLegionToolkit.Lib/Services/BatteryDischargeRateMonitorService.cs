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

    public async Task StartStopIfNeededAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (_refreshTask != null)
            return;

        if (!Battery.TestBatterySupport())
            return;

        if (_cts is not null)
            await _cts.CancelAsync().ConfigureAwait(false);

        _cts = new CancellationTokenSource();

        var token = _cts.Token;

        _refreshTask = Task.Run(async () =>
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
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
            await _cts.CancelAsync().ConfigureAwait(false);

        _cts = null;

        if (_refreshTask is not null)
            await _refreshTask.ConfigureAwait(false);

        _refreshTask = null;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Battery monitoring service stopped");
    }

    private bool _disposed = false;

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
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _cts = null;
                    _refreshTask?.Dispose();
                    _refreshTask = null;
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error during BatteryDischargeRateMonitorService disposal", ex);
                }
            }
            _disposed = true;
        }
    }
}
