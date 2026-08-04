using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public partial class SensorsControl
{
    private async void SensorsControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        try
        {
            if (IsVisible)
            {
                var width = ActualWidth > 1 ? ActualWidth : 1200;
                ApplySensorSummaryLayout(width, force: true);
                Refresh();
                RefreshBattery();
                return;
            }

            // Always operate on locals after clearing fields — never touch _cts/_batteryCts after await.
            await StopSensorRefreshAsync();
            await StopBatteryRefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(SensorsControl_IsVisibleChanged)}.", ex);
        }
    }

    private static void SafeCancelAndDispose(CancellationTokenSource? cts)
    {
        if (cts is null)
            return;

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            cts.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async Task SafeCancelAndDisposeAsync(CancellationTokenSource? cts)
    {
        if (cts is null)
            return;

        try
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            cts.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async Task SafeAwaitRefreshTaskAsync(Task? task)
    {
        if (task is null)
            return;

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            // Visibility/dispose teardown must not surface refresh loop failures.
            Log.Instance.TraceOnce(
                "sensors-safe-await-refresh",
                "Sensor refresh task faulted during safe await (teardown-safe).",
                ex);
        }
    }

    private void StopSensorRefresh()
    {
        CancellationTokenSource? cts;
        lock (_sensorLifecycleLock)
        {
            cts = _cts;
            _cts = null;
            _refreshTask = null;
        }

        SafeCancelAndDispose(cts);
    }

    private async Task StopSensorRefreshAsync()
    {
        CancellationTokenSource? cts;
        Task? task;
        lock (_sensorLifecycleLock)
        {
            cts = _cts;
            _cts = null;
            task = _refreshTask;
            _refreshTask = null;
        }

        await SafeCancelAndDisposeAsync(cts);
        await SafeAwaitRefreshTaskAsync(task);
    }

    private void StopBatteryRefresh()
    {
        CancellationTokenSource? cts;
        lock (_batteryLifecycleLock)
        {
            cts = _batteryCts;
            _batteryCts = null;
            _batteryRefreshTask = null;
        }

        SafeCancelAndDispose(cts);
    }

    private async Task StopBatteryRefreshAsync()
    {
        CancellationTokenSource? cts;
        Task? task;
        lock (_batteryLifecycleLock)
        {
            cts = _batteryCts;
            _batteryCts = null;
            task = _batteryRefreshTask;
            _batteryRefreshTask = null;
        }

        await SafeCancelAndDisposeAsync(cts);
        await SafeAwaitRefreshTaskAsync(task);
    }

    private void Refresh()
    {
        lock (_sensorLifecycleLock)
        {
            var previous = _cts;
            _cts = null;
            SafeCancelAndDispose(previous);

            var cts = new CancellationTokenSource();
            _cts = cts;
            var token = cts.Token;

            _refreshTask = Task.Run(async () =>
            {
                if (!await _controller.IsSupportedAsync().ConfigureAwait(false))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _sensorRuntimeAvailable = false;
                        SetSensorSectionsVisible(true);
                        ResetSensorValues();
                        CompleteInitialSensorDataLoad();
                    });
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    _sensorRuntimeAvailable = true;
                    SetSensorSectionsVisible(true);
                });

                await _controller.PrepareAsync().ConfigureAwait(false);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // Always request the detailed snapshot while detail panels are open so
                        // wattage/voltage/memory-clock stay on one source (NvAPI/WMI path) and
                        // do not alternate with the LibreHardwareMonitor extended overlay.
                        var detailed = await Dispatcher.InvokeAsync(() =>
                            CanShowSensorDetails
                            && (_detailsExpanded
                                || _forceDetailedRefresh
                                || IsElementVisible("_cpuDetailsPanel")
                                || IsElementVisible("_gpuDetailsPanel"))).Task.ConfigureAwait(false);

                        var data = await _controller.GetDataAsync(detailed).ConfigureAwait(false);
                        if (detailed)
                            _forceDetailedRefresh = false;
                        await Dispatcher.InvokeAsync(() => UpdateValues(data, completesInitialLoad: true, recordTrendHistory: true));
                        _sensorsRefreshFailureLogged = false;
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // Refresh stopped (navigate away / dispose).
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        // Transient cancel from a nested CTS — keep polling.
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled && !_sensorsRefreshFailureLogged)
                        {
                            Log.Instance.Trace($"Sensors refresh failed.", ex);
                            _sensorsRefreshFailureLogged = true;
                        }

                        var cached = TryGetSessionSensorDataForDisplay();
                        if (cached.HasValue)
                            await Dispatcher.InvokeAsync(() => UpdateValues(cached.Value, recordTrendHistory: false));
                    }

                    // Always pace the loop (including after errors) so we never hot-spin
                    // and so one timed-out snapshot cannot stall chart updates forever.
                    try
                    {
                        var intervalSeconds = Math.Max(1, _dashboardSettings.Store.SensorsRefreshIntervalSeconds);
                        await _delayProvider.Delay(TimeSpan.FromSeconds(intervalSeconds), token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }, token);
        }
    }
}
