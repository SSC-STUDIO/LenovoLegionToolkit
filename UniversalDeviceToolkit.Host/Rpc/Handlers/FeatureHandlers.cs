using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Features.FlipToStart;
using UniversalDeviceToolkit.Lib.Features.Hybrid;
using UniversalDeviceToolkit.Lib.Features.InstantBoot;
using UniversalDeviceToolkit.Lib.Features.OverDrive;
using UniversalDeviceToolkit.Lib.Features.PanelLogo;
using UniversalDeviceToolkit.Lib.Features.WhiteKeyboardBacklight;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Generic IFeature&lt;T&gt; bridge: capability probe, state list/current state and
/// state set for the 24 dashboard features. Enum states are encoded as enum
/// names (e.g. "Quiet"); struct states (RefreshRate/Resolution/DpiScale) are
/// encoded as objects with PascalCase property names.
/// </summary>
public static class FeatureHandlers
{
    private const int NotSupported = BridgeErrorCodes.FeatureNotSupported;
    private const int AcRequired = BridgeErrorCodes.AcPowerRequired;
    private const int UndefinedState = BridgeErrorCodes.UndefinedState;

    private static readonly Dictionary<string, IFeatureEntry> Features = new(StringComparer.Ordinal);

    public static void Register(BridgeRpcServer rpc)
    {
        RegisterFeature("alwaysOnUsb", IoCContainer.Resolve<AlwaysOnUSBFeature>());
        RegisterFeature("battery", IoCContainer.Resolve<BatteryFeature>());
        RegisterFeature("batteryNightCharge", IoCContainer.Resolve<BatteryNightChargeFeature>());
        RegisterFeature("flipToStart", IoCContainer.Resolve<FlipToStartFeature>());
        RegisterFeature("fnLock", IoCContainer.Resolve<FnLockFeature>());
        RegisterFeature("gSync", IoCContainer.Resolve<GSyncFeature>());
        RegisterFeature("hdr", IoCContainer.Resolve<HDRFeature>());
        RegisterFeature("hybridMode", IoCContainer.Resolve<HybridModeFeature>());
        RegisterFeature("igpuMode", IoCContainer.Resolve<IGPUModeFeature>());
        RegisterFeature("itsMode", IoCContainer.Resolve<ITSModeFeature>());
        RegisterFeature("instantBoot", IoCContainer.Resolve<InstantBootFeature>());
        RegisterFeature("microphone", IoCContainer.Resolve<MicrophoneFeature>());
        RegisterFeature("overDrive", IoCContainer.Resolve<OverDriveFeature>());
        RegisterFeature("panelLogo", IoCContainer.Resolve<PanelLogoBacklightFeature>());
        RegisterFeature("portsBacklight", IoCContainer.Resolve<PortsBacklightFeature>());
        RegisterFeature("powerMode", IoCContainer.Resolve<PowerModeFeature>());
        RegisterFeature("refreshRate", IoCContainer.Resolve<RefreshRateFeature>());
        RegisterFeature("resolution", IoCContainer.Resolve<ResolutionFeature>());
        RegisterFeature("dpiScale", IoCContainer.Resolve<DpiScaleFeature>());
        RegisterFeature("speaker", IoCContainer.Resolve<SpeakerFeature>());
        RegisterFeature("touchpadLock", IoCContainer.Resolve<TouchpadLockFeature>());
        RegisterFeature("whiteKeyboard", IoCContainer.Resolve<WhiteKeyboardBacklightFeature>());
        RegisterFeature("winKey", IoCContainer.Resolve<WinKeyFeature>());
        RegisterFeature("oneLevelWhiteKeyboard", IoCContainer.Resolve<OneLevelWhiteKeyboardBacklightFeature>());

        rpc.RegisterHandler("feature.list", (request, _) => HandleListAsync());
        rpc.RegisterHandler("feature.getSupported", (request, _) => HandleGetSupportedAsync(request));
        rpc.RegisterHandler("feature.getStates", (request, _) => HandleGetStatesAsync(request));
        rpc.RegisterHandler("feature.getState", (request, _) => HandleGetStateAsync(request));
        rpc.RegisterHandler("feature.setState", (request, _) => HandleSetStateAsync(request));
        rpc.RegisterHandler("feature.isHdrBlocked", (_, _) => HandleIsHdrBlockedAsync());
    }

    /// <summary>
    /// Mirrors HDRControl.OnRefreshAsync: HDR is disabled (with a warning) while
    /// Windows settings block it (e.g. display configuration conflicts).
    /// </summary>
    private static async Task<BridgeResult> HandleIsHdrBlockedAsync()
    {
        try
        {
            var hdr = IoCContainer.Resolve<HDRFeature>();
            var blocked = await hdr.IsHdrBlockedAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { blocked });
        }
        catch (Exception)
        {
            // WPF catches and re-enables the toggle — treat as not blocked.
            return BridgeResult.Ok(new { blocked = false });
        }
    }

    private static void RegisterFeature<T>(string key, IFeature<T> feature) where T : struct
    {
        Features[key] = new FeatureEntry<T>(feature);
    }

    /// <summary>
    /// Non-generic view over a concrete <see cref="IFeature{T}"/> so the registry
    /// can hold all 24 features in one dictionary.
    /// </summary>
    public interface IFeatureEntry
    {
        Type FeatureType { get; }
        object? Instance { get; }
        Task<bool> IsSupportedAsync();
        Task<object> GetStateAsObjectAsync();
        Task<object[]> GetAllStatesAsObjectsAsync();
        Task SetStateFromJsonAsync(JsonElement state);
    }

    private sealed class FeatureEntry<T> : IFeatureEntry where T : struct
    {
        private readonly IFeature<T> _feature;

        public FeatureEntry(IFeature<T> feature) => _feature = feature;

        public Type FeatureType => typeof(T);

        public object? Instance => _feature;

        public async Task<bool> IsSupportedAsync()
        {
            try
            {
                return await _feature.IsSupportedAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<object> GetStateAsObjectAsync()
        {
            var state = await _feature.GetStateAsync().ConfigureAwait(false);
            return ToWireObject(state);
        }

        public async Task<object[]> GetAllStatesAsObjectsAsync()
        {
            var states = await _feature.GetAllStatesAsync().ConfigureAwait(false);
            var result = new object[states.Length];
            for (var i = 0; i < states.Length; i++)
                result[i] = ToWireObject(states[i]);
            return result;
        }

        public async Task SetStateFromJsonAsync(JsonElement state)
        {
            var parsed = FromWireObject(state);
            await _feature.SetStateAsync(parsed).ConfigureAwait(false);
            _feature.InvalidateResolution();
        }

        private static object ToWireObject(T state)
        {
            if (typeof(T).IsEnum)
                return Enum.GetName(typeof(T), state) ?? state.ToString()!;
            return JsonSerializer.SerializeToElement(state);
        }

        private static T FromWireObject(JsonElement state)
        {
            if (typeof(T).IsEnum)
            {
                if (state.ValueKind != JsonValueKind.String)
                    throw new ArgumentException($"State for {typeof(T).Name} must be a string (enum name), got {state.ValueKind}.");
                return (T)Enum.Parse(typeof(T), state.GetString()!, ignoreCase: true);
            }

            if (state.ValueKind != JsonValueKind.Object)
                throw new ArgumentException($"State for {typeof(T).Name} must be an object, got {state.ValueKind}.");
            var deserialized = JsonSerializer.Deserialize<T>(state.GetRawText());
            if (deserialized.Equals(default))
                throw new ArgumentException($"State for {typeof(T).Name} could not be deserialized.");
            return deserialized;
        }
    }

    private static async Task<BridgeResult> HandleListAsync()
    {
        try
        {
            var tasks = Features.Select(async kv => new
            {
                key = kv.Key,
                supported = await kv.Value.IsSupportedAsync().ConfigureAwait(false),
                stateType = kv.Value.FeatureType.Name,
            });
            var features = await Task.WhenAll(tasks).ConfigureAwait(false);

            return BridgeResult.Ok(new { features });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleGetSupportedAsync(BridgeRequest request)
    {
        try
        {
            var entry = GetFeature(request);
            return BridgeResult.Ok(new { supported = await entry.IsSupportedAsync().ConfigureAwait(false) });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleGetStatesAsync(BridgeRequest request)
    {
        try
        {
            var entry = GetFeature(request);
            var states = await entry.GetAllStatesAsObjectsAsync().ConfigureAwait(false);

            return BridgeResult.Ok(new { states });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleGetStateAsync(BridgeRequest request)
    {
        try
        {
            var entry = GetFeature(request);
            var state = await entry.GetStateAsObjectAsync().ConfigureAwait(false);

            return BridgeResult.Ok(new { state });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (NotSupportedException)
        {
            return BridgeResult.Error(NotSupported, "NOT_SUPPORTED");
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSetStateAsync(BridgeRequest request)
    {
        try
        {
            var entry = GetFeature(request);

            if (!request.Parameters.TryGetProperty("state", out var stateProp))
                throw new BridgeErrorException(-32602, "Missing 'state' parameter.");

            await entry.SetStateFromJsonAsync(stateProp).ConfigureAwait(false);

            return BridgeResult.Ok(new { ok = true, partial = (bool?)null });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (PowerModeUnavailableWithoutACException)
        {
            return BridgeResult.Error(AcRequired, "AC_REQUIRED: PowerMode state requires AC power.");
        }
        catch (NotSupportedException)
        {
            return BridgeResult.Error(NotSupported, "NOT_SUPPORTED");
        }
        catch (ArgumentException)
        {
            return BridgeResult.Error(UndefinedState, "UNDEFINED_STATE");
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static IFeatureEntry GetFeature(BridgeRequest request)
    {
        if (!request.Parameters.TryGetProperty("feature", out var featureProp) ||
            featureProp.ValueKind != JsonValueKind.String)
            throw new BridgeErrorException(-32602, "Missing string parameter 'feature'.");

        var key = featureProp.GetString()!;
        if (!Features.TryGetValue(key, out var entry))
            throw new BridgeErrorException(-32602, $"Unknown feature '{key}'.");

        return entry;
    }
}
