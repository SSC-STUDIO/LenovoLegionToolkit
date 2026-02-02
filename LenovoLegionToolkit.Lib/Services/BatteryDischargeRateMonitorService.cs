using System;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Services;

public class BatteryDischargeRateMonitorService
{
    private CancellationTokenSource? _cts;
    private Task? _refreshTask;

    public async Task StartStopIfNeededAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (_refreshTask != null)
            return;

        // Check if battery operations are supported to avoid infinite loops
        if (!Battery.TestBatterySupport())
        {
            // Don't start the monitoring service if battery operations fail
            return;
        }

        if (_cts is not null)
            await _cts.CancelAsync().ConfigureAwait(false);

        _cts = new CancellationTokenSource();

        var token = _cts.Token;

        _refreshTask = Task.Run(async () =>
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Battery monitoring service started...");

            var iterationCount = 0;
            
            while (!token.IsCancellationRequested)
            {
                try
                {
                    iterationCount++;
                    
                    // Add safety check to prevent infinite loops
                    if (iterationCount > 1000)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Battery monitoring service exceeded safe iteration limit ({iterationCount}), stopping to prevent infinite loop");
                        break;
                    }

                    if (Log.Instance.IsTraceEnabled && iterationCount % 10 == 0)
                        Log.Instance.Trace($"Battery monitoring iteration: {iterationCount}");

                    Battery.SetMinMaxDischargeRate();

                    await Task.Delay(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) 
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Battery monitoring service cancelled.");
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Battery monitoring service failed at iteration {iterationCount}.", ex);
                    
                    // Break on exception to prevent error loops
                    break;
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Battery monitoring service stopped after {iterationCount} iterations.");
        }, token);
    }

    public async Task StopAsync()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Stopping...");

        if (_cts is not null)
            await _cts.CancelAsync().ConfigureAwait(false);

        _cts = null;

        if (_refreshTask is not null)
            await _refreshTask.ConfigureAwait(false);

        _refreshTask = null;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Stopped.");
    }
}
