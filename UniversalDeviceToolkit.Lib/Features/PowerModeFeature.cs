using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Features;

public class PowerModeUnavailableWithoutACException(PowerModeState powerMode)
    : Exception($"Power mode '{powerMode}' is unavailable without AC adapter.")
{
    public PowerModeState PowerMode { get; } = powerMode;
}

public class PowerModeFeature(
    GodModeController godModeController,
    WindowsPowerModeController windowsPowerModeController,
    WindowsPowerPlanController windowsPowerPlanController,
    ThermalModeListener thermalModeListener,
    PowerModeListener powerModeListener)
    : AbstractWmiFeature<PowerModeState>(WMI.LenovoGameZoneData.GetSmartFanModeAsync, WMI.LenovoGameZoneData.SetSmartFanModeAsync, WMI.LenovoGameZoneData.IsSupportSmartFanAsync, 1), IFeature<PowerModeState>
{
    private PowerModeState? _lastKnownState;

    public bool AllowAllPowerModesOnBattery { get; set; }

    async Task<bool> IFeature<PowerModeState>.IsSupportedAsync() => await IsSupportedAsync().ConfigureAwait(false);
    async Task<PowerModeState> IFeature<PowerModeState>.GetStateAsync() => await GetStateAsync().ConfigureAwait(false);

    public new async Task<bool> IsSupportedAsync()
    {
        if (await base.IsSupportedAsync().ConfigureAwait(false))
            return true;

        return (await GetAllStatesAsync().ConfigureAwait(false)).Length > 0;
    }

    public new async Task<PowerModeState> GetStateAsync()
    {
        try
        {
            var state = await ReadStateCoreAsync().ConfigureAwait(false);
            state = await NormalizeExtremeStateAsync(state).ConfigureAwait(false);
            _lastKnownState = state;
            return state;
        }
        catch (Exception ex)
        {
            var fallbackState = await GetFallbackStateAsync().ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Falling back to {fallbackState} after power mode state read failure [feature={nameof(PowerModeFeature)}]", ex);

            return fallbackState;
        }
    }

    public override async Task<PowerModeState[]> GetAllStatesAsync()
    {
        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        var supportedPowerModes = mi.SupportedPowerModes ?? [];
        var states = new List<PowerModeState>
        {
            PowerModeState.Quiet,
            PowerModeState.Balance,
            PowerModeState.Performance
        };

        foreach (var supportedPowerMode in supportedPowerModes)
        {
            if (supportedPowerMode == PowerModeState.Extreme)
                continue;

            if (!states.Contains(supportedPowerMode))
                states.Add(supportedPowerMode);
        }

        if (mi.Properties.SupportsGodMode || supportedPowerModes.Contains(PowerModeState.GodMode))
            states.Add(PowerModeState.GodMode);

        return [.. states.Distinct()];
    }

    public override async Task SetStateAsync(PowerModeState state)
    {
        if (state == PowerModeState.Extreme)
            throw new InvalidOperationException($"Unsupported power mode {state}");

        var allStates = await GetAllStatesAsync().ConfigureAwait(false);
        if (!allStates.Contains(state))
            throw new InvalidOperationException($"Unsupported power mode {state}");

        if (state is PowerModeState.Performance or PowerModeState.GodMode
            && !AllowAllPowerModesOnBattery
            && await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false) is PowerAdapterStatus.Disconnected)
            throw new PowerModeUnavailableWithoutACException(state);

        var currentState = await GetStateAsync().ConfigureAwait(false);

        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

        if (mi.Properties.HasQuietToPerformanceModeSwitchingBug && currentState == PowerModeState.Quiet && state == PowerModeState.Performance)
        {
            thermalModeListener.SuppressNext();
            await base.SetStateAsync(PowerModeState.Balance).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
        }

        if (mi.Properties.HasGodModeToOtherModeSwitchingBug && currentState == PowerModeState.GodMode && state != PowerModeState.GodMode)
        {
            thermalModeListener.SuppressNext();

            switch (state)
            {
                case PowerModeState.Quiet:
                    await base.SetStateAsync(PowerModeState.Performance).ConfigureAwait(false);
                    break;
                case PowerModeState.Balance:
                    await base.SetStateAsync(PowerModeState.Quiet).ConfigureAwait(false);
                    break;
                case PowerModeState.Performance:
                    await base.SetStateAsync(PowerModeState.Balance).ConfigureAwait(false);
                    break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

        }

        thermalModeListener.SuppressNext();
        await base.SetStateAsync(state).ConfigureAwait(false);
        _lastKnownState = state;

        await powerModeListener.NotifyAsync(state).ConfigureAwait(false);
    }

    public async Task EnsureCorrectWindowsPowerSettingsAreSetAsync()
    {
        var state = await GetStateAsync().ConfigureAwait(false);
        await windowsPowerModeController.SetPowerModeAsync(state).ConfigureAwait(false);
        await windowsPowerPlanController.SetPowerPlanAsync(state, true).ConfigureAwait(false);
    }

    public async Task EnsureGodModeStateIsAppliedAsync()
    {
        var state = await GetStateAsync().ConfigureAwait(false);
        if (state != PowerModeState.GodMode)
            return;

        await godModeController.ApplyStateAsync().ConfigureAwait(false);
    }

    internal virtual Task<PowerModeState> ReadStateCoreAsync() => base.GetStateAsync();

    private async Task<PowerModeState> NormalizeExtremeStateAsync(PowerModeState state)
    {
        if (state != PowerModeState.Extreme)
            return state;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Migrating {PowerModeState.Extreme} to {PowerModeState.Performance} [feature={nameof(PowerModeFeature)}]");

        thermalModeListener.SuppressNext();
        await base.SetStateAsync(PowerModeState.Performance).ConfigureAwait(false);
        await powerModeListener.NotifyAsync(PowerModeState.Performance).ConfigureAwait(false);
        return PowerModeState.Performance;
    }

    private async Task<PowerModeState> GetFallbackStateAsync()
    {
        if (_lastKnownState.HasValue)
            return _lastKnownState.Value;

        try
        {
            var allStates = await GetAllStatesAsync().ConfigureAwait(false);

            if (allStates.Contains(PowerModeState.Balance))
                return PowerModeState.Balance;

            if (allStates.Length > 0)
                return allStates[0];
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to compute fallback power mode state [feature={nameof(PowerModeFeature)}]", ex);
        }

        return PowerModeState.Balance;
    }
}
