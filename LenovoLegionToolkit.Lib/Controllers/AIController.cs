using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.AutoListeners;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;
using NeoSmart.AsyncLock;

namespace LenovoLegionToolkit.Lib.Controllers;

public class AIController(
    PowerModeListener powerModeListener,
    PowerStateListener powerStateListener,
    GameAutoListener gameAutoListener,
    PowerModeFeature powerModeFeature,
    BalanceModeSettings settings) : IDisposable
{
    private readonly ThrottleLastDispatcher _dispatcher = new(TimeSpan.FromSeconds(1), nameof(AIController));

    private readonly AsyncLock _startStopLock = new();

    public bool IsAIModeEnabled
    {
        get => settings.Store.AIModeEnabled;
        set
        {
            settings.Store.AIModeEnabled = value;
            settings.SynchronizeStore();
        }
    }

    public async Task StartIfNeededAsync()
    {
        if (!await IsSupportedAndLogAsync().ConfigureAwait(false))
            return;

        await StopAsync().ConfigureAwait(false);

        if (!IsAIModeEnabled)
            return;

        using (await _startStopLock.LockAsync().ConfigureAwait(false))
        {
            powerModeListener.Changed += PowerModeListener_Changed;
            powerStateListener.Changed += PowerStateListener_Changed;

            await gameAutoListener.SubscribeChangedAsync(GameAutoListener_Changed).ConfigureAwait(false);

            await RefreshAsync().ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"AI controller started");
        }
    }

    public async Task StopAsync()
    {
        if (!await IsSupportedAndLogAsync().ConfigureAwait(false))
            return;

        using (await _startStopLock.LockAsync().ConfigureAwait(false))
        {
            powerModeListener.Changed -= PowerModeListener_Changed;
            powerStateListener.Changed -= PowerStateListener_Changed;

            await gameAutoListener.UnsubscribeChangedAsync(GameAutoListener_Changed).ConfigureAwait(false);

            if (await ShouldDisableAsync().ConfigureAwait(false))
                await DisableAsync().ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"AI controller stopped");
        }
    }

    private async Task PowerModeListener_ChangedAsync(object? sender, PowerModeListener.ChangedEventArgs e)
    {
        try
        {
            await _dispatcher.DispatchAsync(RefreshAsync).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error in PowerModeListener_Changed: {ex.Message}", ex);
        }
    }

    private void PowerModeListener_Changed(object? sender, PowerModeListener.ChangedEventArgs e)
    {
        _ = PowerModeListener_ChangedAsync(sender, e);
    }

    private async Task PowerStateListener_ChangedAsync(object? sender, PowerStateListener.ChangedEventArgs e)
    {
        try
        {
            await _dispatcher.DispatchAsync(RefreshAsync).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error in PowerStateListener_Changed: {ex.Message}", ex);
        }
    }

    private void PowerStateListener_Changed(object? sender, PowerStateListener.ChangedEventArgs e)
    {
        _ = PowerStateListener_ChangedAsync(sender, e);
    }

    private async Task GameAutoListener_ChangedAsync(object? sender, GameAutoListener.ChangedEventArgs e)
    {
        try
        {
            await _dispatcher.DispatchAsync(RefreshAsync).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error in GameAutoListener_Changed: {ex.Message}", ex);
        }
    }

    private void GameAutoListener_Changed(object? sender, GameAutoListener.ChangedEventArgs e)
    {
        _ = GameAutoListener_ChangedAsync(sender, e);
    }

    private async Task RefreshAsync()
    {
        if (!await IsSupportedAndLogAsync().ConfigureAwait(false))
            return;

        using (await _startStopLock.LockAsync().ConfigureAwait(false))
        {
            if (await ShouldDisableAsync().ConfigureAwait(false))
                await DisableAsync().ConfigureAwait(false);

            if (await ShouldEnableAsync().ConfigureAwait(false))
                await EnableAsync().ConfigureAwait(false);
        }
    }

    private static async Task<bool> IsSupportedAndLogAsync()
    {
        if (!await IsSupportedAsync().ConfigureAwait(false))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Not supported.");
            return false;
        }
        return true;
    }

    private static async Task<bool> IsSupportedAsync()
    {
        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

        if (!Compatibility.IsSupportedLegionMachine(mi))
            return false;

        return mi.Properties.SupportsAIMode;
    }

    private async Task<bool> ShouldEnableAsync()
    {
        if (await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false) != PowerAdapterStatus.Connected)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Power adapter not connected.");

            return false;
        }

        if (await powerModeFeature.GetStateAsync().ConfigureAwait(false) != PowerModeState.Balance)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Not in balanced mode.");

            return false;
        }

        if (!gameAutoListener.AreGamesRunning())
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Games aren't running.");

            return false;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"All conditions met.");

        return true;
    }

    private async Task<bool> ShouldDisableAsync()
    {
        if (await powerModeFeature.GetStateAsync().ConfigureAwait(false) != PowerModeState.Balance)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Not in balanced mode.");

            return false;
        }

        if (await WMI.LenovoGameZoneData.GetIntelligentSubModeAsync().ConfigureAwait(false) == 0)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Not needed.");

            return false;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"All conditions met.");

        return true;
    }

    private static async Task EnableAsync()
    {
        try
        {
            var targetSubMode = 1;

            var intelligentOpList = await WMI.LenovoIntelligentOPList.ReadAsync().ConfigureAwait(false);
            foreach (var (processName, subMode) in intelligentOpList)
            {
                var process = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName)).FirstOrDefault();
                if (process is null)
                    continue;

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Found running process {processName} (subMode={subMode})");

                targetSubMode = subMode;
                break;
            }

            await WMI.LenovoGameZoneData.SetIntelligentSubModeAsync(targetSubMode).ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"AI mode enabled (subMode={targetSubMode})");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to start.", ex);
        }
    }

    private static async Task DisableAsync()
    {
        try
        {
            await WMI.LenovoGameZoneData.SetIntelligentSubModeAsync(0).ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"AI mode disabled");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to disable AI mode", ex);
        }
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
                    _dispatcher?.Dispose();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"AIController disposal error", ex);
                }
            }
            _disposed = true;
        }
    }
}
