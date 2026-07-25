using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Features.Asus;
using UniversalDeviceToolkit.Lib.Features.Hp;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Features;

/// <summary>
/// Vendor-agnostic power-mode facade. Keeps the long-standing concrete type so
/// every existing resolve site (dashboard cards, automation, listeners, startup)
/// works unchanged, while the actual backend is resolved once per machine:
/// Lenovo SmartFan first, then the ASUS ATK endpoint. Vendor-specific extras
/// (God Mode, Windows power plan sync, switching-bug workarounds) stay delegated
/// to the Lenovo implementation — they are no-ops on other vendors by design.
/// </summary>
public class PowerModeFeature(
    LenovoPowerModeFeature lenovoFeature,
    AsusPowerModeFeature asusFeature,
    HpPowerModeFeature hpFeature) : IFeature<PowerModeState>
{
    private readonly LenovoPowerModeFeature _lenovoFeature = lenovoFeature;
    private readonly AsusPowerModeFeature _asusFeature = asusFeature;
    private readonly HpPowerModeFeature _hpFeature = hpFeature;
    private IFeature<PowerModeState>? _backend;

    public bool AllowAllPowerModesOnBattery
    {
        get => _lenovoFeature.AllowAllPowerModesOnBattery;
        set => _lenovoFeature.AllowAllPowerModesOnBattery = value;
    }

    public async Task<bool> IsSupportedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ResolveBackendAsync(cancellationToken).ConfigureAwait(false) is not null;
    }

    public async Task<PowerModeState[]> GetAllStatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backend = await RequireBackendAsync(cancellationToken).ConfigureAwait(false);
        return await backend.GetAllStatesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PowerModeState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backend = await RequireBackendAsync(cancellationToken).ConfigureAwait(false);
        return await backend.GetStateAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetStateAsync(PowerModeState state, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backend = await RequireBackendAsync(cancellationToken).ConfigureAwait(false);
        await backend.SetStateAsync(state, cancellationToken).ConfigureAwait(false);
    }

    public void InvalidateResolution()
    {
        _backend = null;
        _lenovoFeature.InvalidateResolution();
        _asusFeature.InvalidateResolution();
        _hpFeature.InvalidateResolution();
    }

    // --- Lenovo-specific flows (delegated; no-ops on machines without Lenovo support) ---

    public Task EnsureCorrectWindowsPowerSettingsAreSetAsync(CancellationToken cancellationToken = default) =>
        _lenovoFeature.EnsureCorrectWindowsPowerSettingsAreSetAsync(cancellationToken);

    public Task EnsureGodModeStateIsAppliedAsync(CancellationToken cancellationToken = default) =>
        _lenovoFeature.EnsureGodModeStateIsAppliedAsync(cancellationToken);

    public Task<PowerModeState> NormalizeExtremeStateIfNeededAsync(CancellationToken cancellationToken = default) =>
        _lenovoFeature.NormalizeExtremeStateIfNeededAsync(cancellationToken);

    private async Task<IFeature<PowerModeState>?> ResolveBackendAsync(CancellationToken cancellationToken)
    {
        if (_backend is not null)
            return _backend;

        if (await _lenovoFeature.IsSupportedAsync(cancellationToken).ConfigureAwait(false))
            return _backend = _lenovoFeature;

        if (await _asusFeature.IsSupportedAsync(cancellationToken).ConfigureAwait(false))
            return _backend = _asusFeature;

        if (await _hpFeature.IsSupportedAsync(cancellationToken).ConfigureAwait(false))
            return _backend = _hpFeature;

        return null;
    }

    private async Task<IFeature<PowerModeState>> RequireBackendAsync(CancellationToken cancellationToken) =>
        await ResolveBackendAsync(cancellationToken).ConfigureAwait(false)
        ?? throw ExceptionHelper.PowerModeNotSupported();
}
