using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Resources;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Features;

public class PowerModeUnavailableWithoutACException(PowerModeState powerMode)
    : Exception(string.Format(Resource.Exception_PowerModeUnavailableWithoutAC, powerMode))
{
    public PowerModeState PowerMode { get; } = powerMode;
}

public class PowerModeFeature(
    GodModeController godModeController,
    WindowsPowerModeController windowsPowerModeController,
    WindowsPowerPlanController windowsPowerPlanController,
    ThermalModeListener thermalModeListener,
    PowerModeListener powerModeListener,
    IDelayProvider? delayProvider = null)
    : AbstractWmiFeature<PowerModeState>(WMI.LenovoGameZoneData.GetSmartFanModeAsync, WMI.LenovoGameZoneData.SetSmartFanModeAsync, WMI.LenovoGameZoneData.IsSupportSmartFanAsync, 1), IFeature<PowerModeState>
{
    private readonly IDelayProvider _delayProvider = delayProvider ?? new DefaultDelayProvider();
    private PowerModeState? _lastKnownState;

    public bool AllowAllPowerModesOnBattery { get; set; }

    async Task<bool> IFeature<PowerModeState>.IsSupportedAsync(CancellationToken cancellationToken) => await IsSupportedAsync(cancellationToken).ConfigureAwait(false);
    async Task<PowerModeState> IFeature<PowerModeState>.GetStateAsync(CancellationToken cancellationToken) => await GetStateAsync(cancellationToken).ConfigureAwait(false);

    public new async Task<bool> IsSupportedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await IsWmiSupportedAsync(cancellationToken).ConfigureAwait(false))
            return true;

        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        return HasLenovoPowerModeCapability(mi);
    }

    public new async Task<PowerModeState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var state = await ReadStateCoreAsync(cancellationToken).ConfigureAwait(false);
            _lastKnownState = state;
            return state;
        }
        catch (Exception ex)
        {
            var fallbackState = await GetFallbackStateAsync(cancellationToken).ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Falling back to {fallbackState} after power mode state read failure [feature={nameof(PowerModeFeature)}]", ex);

            return fallbackState;
        }
    }

    public override async Task<PowerModeState[]> GetAllStatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

    public override async Task SetStateAsync(PowerModeState state, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!await IsSupportedAsync(cancellationToken).ConfigureAwait(false))
            throw ExceptionHelper.PowerModeNotSupported();

        if (state == PowerModeState.Extreme)
            throw ExceptionHelper.UnsupportedPowerMode(state);

        var allStates = await GetAllStatesAsync(cancellationToken).ConfigureAwait(false);
        if (!allStates.Contains(state))
            throw ExceptionHelper.UnsupportedPowerMode(state);

        if (state is PowerModeState.Performance or PowerModeState.GodMode
            && !AllowAllPowerModesOnBattery
            && await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false) is PowerAdapterStatus.Disconnected)
            throw new PowerModeUnavailableWithoutACException(state);

        var currentState = await NormalizeExtremeStateIfNeededAsync(cancellationToken).ConfigureAwait(false);

        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

        if (mi.Properties.HasQuietToPerformanceModeSwitchingBug && currentState == PowerModeState.Quiet && state == PowerModeState.Performance)
        {
            thermalModeListener.SuppressNext();
            await base.SetStateAsync(PowerModeState.Balance, cancellationToken).ConfigureAwait(false);
            await _delayProvider.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        if (mi.Properties.HasGodModeToOtherModeSwitchingBug && currentState == PowerModeState.GodMode && state != PowerModeState.GodMode)
        {
            thermalModeListener.SuppressNext();

            switch (state)
            {
                case PowerModeState.Quiet:
                    await base.SetStateAsync(PowerModeState.Performance, cancellationToken).ConfigureAwait(false);
                    break;
                case PowerModeState.Balance:
                    await base.SetStateAsync(PowerModeState.Quiet, cancellationToken).ConfigureAwait(false);
                    break;
                case PowerModeState.Performance:
                    await base.SetStateAsync(PowerModeState.Balance, cancellationToken).ConfigureAwait(false);
                    break;
            }

            await _delayProvider.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);

        }

        thermalModeListener.SuppressNext();
        await base.SetStateAsync(state, cancellationToken).ConfigureAwait(false);
        _lastKnownState = state;

        await powerModeListener.NotifyAsync(state).ConfigureAwait(false);
    }

    public async Task EnsureCorrectWindowsPowerSettingsAreSetAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(cancellationToken).ConfigureAwait(false);
        await windowsPowerModeController.SetPowerModeAsync(state).ConfigureAwait(false);
        await windowsPowerPlanController.SetPowerPlanAsync(state, true).ConfigureAwait(false);
    }

    public async Task EnsureGodModeStateIsAppliedAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(cancellationToken).ConfigureAwait(false);
        if (state != PowerModeState.GodMode)
            return;

        await godModeController.ApplyStateAsync().ConfigureAwait(false);
    }

    internal virtual Task<PowerModeState> ReadStateCoreAsync(CancellationToken cancellationToken = default) => base.GetStateAsync(cancellationToken);

    public async Task<PowerModeState> NormalizeExtremeStateIfNeededAsync(CancellationToken cancellationToken = default)
    {
        var state = await ReadStateCoreAsync(cancellationToken).ConfigureAwait(false);
        if (state != PowerModeState.Extreme)
            return state;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Migrating {PowerModeState.Extreme} to {PowerModeState.Performance} [feature={nameof(PowerModeFeature)}]");

        thermalModeListener.SuppressNext();
        await base.SetStateAsync(PowerModeState.Performance, cancellationToken).ConfigureAwait(false);
        await powerModeListener.NotifyAsync(PowerModeState.Performance).ConfigureAwait(false);
        return PowerModeState.Performance;
    }

    private async Task<PowerModeState> GetFallbackStateAsync(CancellationToken cancellationToken)
    {
        if (_lastKnownState.HasValue)
            return _lastKnownState.Value;

        try
        {
            var allStates = await GetAllStatesAsync(cancellationToken).ConfigureAwait(false);

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

    internal virtual Task<bool> IsWmiSupportedAsync(CancellationToken cancellationToken = default) => base.IsSupportedAsync(cancellationToken);

    public override void InvalidateResolution()
    {
    }

    internal static bool HasLenovoPowerModeCapability(MachineInformation machineInformation)
    {
        if (!Compatibility.IsSupportedDevice(machineInformation))
            return false;

        var supportedPowerModes = machineInformation.SupportedPowerModes ?? [];
        return supportedPowerModes.Any(mode => mode is PowerModeState.Quiet or PowerModeState.Balance or PowerModeState.Performance or PowerModeState.GodMode)
               || machineInformation.Properties.SupportsGodMode;
    }
}
