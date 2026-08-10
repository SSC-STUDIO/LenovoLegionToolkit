using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Serialization;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// RGB/Spectrum keyboard backlight bridge: capability detection, RGB preset
/// state and Spectrum profile control. Payloads use LltJson compact options
/// (PascalCase property names, enum values as strings).
/// </summary>
public static class KeyboardBacklightHandlers
{
    private const int NotSupported = -1001;

    private static JsonSerializerOptions? _options;

    private static JsonSerializerOptions Options => _options ??= LltJson.CreateCompactOptions();

    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("keyboard.detect", (request, _) => HandleDetectAsync());

        rpc.RegisterHandler("rgb.isSupported", (request, _) => HandleRgbIsSupportedAsync());
        rpc.RegisterHandler("rgb.getState", (request, _) => HandleRgbGetStateAsync());
        rpc.RegisterHandler("rgb.setState", (request, _) => HandleRgbSetStateAsync(request));
        rpc.RegisterHandler("rgb.setPreset", (request, _) => HandleRgbSetPresetAsync(request));
        rpc.RegisterHandler("rgb.nextPreset", (request, _) => HandleRgbNextPresetAsync());
        rpc.RegisterHandler("rgb.takeOwnership", (request, _) => HandleRgbTakeOwnershipAsync(request));

        rpc.RegisterHandler("spectrum.isSupported", (request, _) => HandleSpectrumIsSupportedAsync());
        rpc.RegisterHandler("spectrum.getLayout", (request, _) => HandleSpectrumGetLayoutAsync());
        rpc.RegisterHandler("spectrum.getBrightness", (request, _) => HandleSpectrumGetBrightnessAsync());
        rpc.RegisterHandler("spectrum.setBrightness", (request, _) => HandleSpectrumSetBrightnessAsync(request));
        rpc.RegisterHandler("spectrum.getLogoStatus", (request, _) => HandleSpectrumGetLogoStatusAsync());
        rpc.RegisterHandler("spectrum.setLogoStatus", (request, _) => HandleSpectrumSetLogoStatusAsync(request));
        rpc.RegisterHandler("spectrum.getProfile", (request, _) => HandleSpectrumGetProfileAsync());
        rpc.RegisterHandler("spectrum.setProfile", (request, _) => HandleSpectrumSetProfileAsync(request));
        rpc.RegisterHandler("spectrum.getProfileDescription", (request, _) => HandleSpectrumGetProfileDescriptionAsync(request));
        rpc.RegisterHandler("spectrum.setProfileDescription", (request, _) => HandleSpectrumSetProfileDescriptionAsync(request));
    }

    private static RGBKeyboardBacklightController GetRgb()
        => IoCContainer.Resolve<RGBKeyboardBacklightController>();

    private static SpectrumKeyboardBacklightController GetSpectrum()
        => IoCContainer.Resolve<SpectrumKeyboardBacklightController>();

    // ── detect ──────────────────────────────────────────────────────────────

    private static async Task<BridgeResult> HandleDetectAsync()
    {
        try
        {
            if (await GetSpectrum().IsSupportedAsync().ConfigureAwait(false))
                return BridgeResult.Ok(new { mode = "spectrum" });

            if (await GetRgb().IsSupportedAsync().ConfigureAwait(false))
                return BridgeResult.Ok(new { mode = "rgb" });

            return BridgeResult.Ok(new { mode = "none" });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    // ── RGB ─────────────────────────────────────────────────────────────────

    private static async Task<BridgeResult> HandleRgbIsSupportedAsync()
    {
        try
        {
            return BridgeResult.Ok(new { supported = await GetRgb().IsSupportedAsync().ConfigureAwait(false) });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleRgbGetStateAsync()
    {
        try
        {
            await EnsureRgbSupportedAsync().ConfigureAwait(false);
            var state = await GetRgb().GetStateAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { state = JsonSerializer.SerializeToElement(state, Options) });
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

    private static async Task<BridgeResult> HandleRgbSetStateAsync(BridgeRequest request)
    {
        try
        {
            await EnsureRgbSupportedAsync().ConfigureAwait(false);

            if (!request.Parameters.TryGetProperty("state", out var stateProp))
                throw new BridgeErrorException(-32602, "Missing 'state' parameter.");

            var state = JsonSerializer.Deserialize<RGBKeyboardBacklightState>(stateProp.GetRawText(), Options);

            await GetRgb().SetStateAsync(state).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
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

    private static async Task<BridgeResult> HandleRgbSetPresetAsync(BridgeRequest request)
    {
        try
        {
            await EnsureRgbSupportedAsync().ConfigureAwait(false);

            if (!request.Parameters.TryGetProperty("preset", out var presetProp) ||
                presetProp.ValueKind != JsonValueKind.String)
                throw new BridgeErrorException(-32602, "Missing string parameter 'preset'.");

            if (!Enum.TryParse<RGBKeyboardBacklightPreset>(presetProp.GetString(), ignoreCase: true, out var preset))
                throw new BridgeErrorException(-32602, $"Invalid preset '{presetProp.GetString()}'.");

            var rgb = GetRgb();
            await rgb.SetPresetAsync(preset).ConfigureAwait(false);
            var state = await rgb.GetStateAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { state = JsonSerializer.SerializeToElement(state, Options) });
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

    private static async Task<BridgeResult> HandleRgbNextPresetAsync()
    {
        try
        {
            await EnsureRgbSupportedAsync().ConfigureAwait(false);

            var rgb = GetRgb();
            await rgb.SetNextPresetAsync().ConfigureAwait(false);
            var state = await rgb.GetStateAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { state = JsonSerializer.SerializeToElement(state, Options) });
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

    private static async Task<BridgeResult> HandleRgbTakeOwnershipAsync(BridgeRequest request)
    {
        try
        {
            await EnsureRgbSupportedAsync().ConfigureAwait(false);

            if (!request.Parameters.TryGetProperty("enable", out var enableProp) ||
                enableProp.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new BridgeErrorException(-32602, "Missing boolean parameter 'enable'.");

            var enable = enableProp.GetBoolean();
            var restorePreset = request.Parameters.TryGetProperty("restorePreset", out var restoreProp) &&
                                restoreProp.ValueKind == JsonValueKind.True;

            await GetRgb().SetLightControlOwnerAsync(enable, restorePreset).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
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

    private static async Task EnsureRgbSupportedAsync()
    {
        if (!await GetRgb().IsSupportedAsync().ConfigureAwait(false))
            throw new BridgeErrorException(NotSupported, "NOT_SUPPORTED");
    }

    // ── Spectrum ────────────────────────────────────────────────────────────

    private static async Task<BridgeResult> HandleSpectrumIsSupportedAsync()
    {
        try
        {
            return BridgeResult.Ok(new { supported = await GetSpectrum().IsSupportedAsync().ConfigureAwait(false) });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSpectrumGetLayoutAsync()
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);

            var (spectrumLayout, keyboardLayout, keys) = await GetSpectrum().GetKeyboardLayoutAsync().ConfigureAwait(false);
            var result = JsonSerializer.SerializeToElement(
                new { spectrumLayout, keyboardLayout, keys = keys.OrderBy(k => k).ToArray() },
                Options);
            return BridgeResult.Ok(result);
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

    private static async Task<BridgeResult> HandleSpectrumGetBrightnessAsync()
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);
            var brightness = await GetSpectrum().GetBrightnessAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { brightness });
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

    private static async Task<BridgeResult> HandleSpectrumSetBrightnessAsync(BridgeRequest request)
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);

            var brightness = GetRequiredInt(request, "brightness");
            if (brightness is < 0 or > 9)
                throw new BridgeErrorException(-32602, $"Invalid brightness '{brightness}' (expected 0..9).");

            await GetSpectrum().SetBrightnessAsync(brightness).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
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

    private static async Task<BridgeResult> HandleSpectrumGetLogoStatusAsync()
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);
            var isOn = await GetSpectrum().GetLogoStatusAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { isOn });
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

    private static async Task<BridgeResult> HandleSpectrumSetLogoStatusAsync(BridgeRequest request)
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);

            if (!request.Parameters.TryGetProperty("isOn", out var isOnProp) ||
                isOnProp.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new BridgeErrorException(-32602, "Missing boolean parameter 'isOn'.");

            await GetSpectrum().SetLogoStatusAsync(isOnProp.GetBoolean()).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
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

    private static async Task<BridgeResult> HandleSpectrumGetProfileAsync()
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);
            var profile = await GetSpectrum().GetProfileAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { profile });
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

    private static async Task<BridgeResult> HandleSpectrumSetProfileAsync(BridgeRequest request)
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);

            var profile = GetRequiredInt(request, "profile");
            if (profile is < 0 or > 6)
                throw new BridgeErrorException(-32602, $"Invalid profile '{profile}' (expected 0..6).");

            await GetSpectrum().SetProfileAsync(profile).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
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

    private static async Task<BridgeResult> HandleSpectrumGetProfileDescriptionAsync(BridgeRequest request)
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);

            var profile = GetRequiredInt(request, "profile");
            var (resolvedProfile, effects) = await GetSpectrum().GetProfileDescriptionAsync(profile).ConfigureAwait(false);
            var result = JsonSerializer.SerializeToElement(new { profile = resolvedProfile, effects }, Options);
            return BridgeResult.Ok(result);
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

    private static async Task<BridgeResult> HandleSpectrumSetProfileDescriptionAsync(BridgeRequest request)
    {
        try
        {
            await EnsureSpectrumSupportedAsync().ConfigureAwait(false);

            var profile = GetRequiredInt(request, "profile");
            if (!request.Parameters.TryGetProperty("effects", out var effectsProp) ||
                effectsProp.ValueKind != JsonValueKind.Array)
                throw new BridgeErrorException(-32602, "Missing array parameter 'effects'.");

            var effects = JsonSerializer.Deserialize<SpectrumKeyboardBacklightEffect[]>(effectsProp.GetRawText(), Options)
                ?? throw new BridgeErrorException(-32602, "Invalid 'effects' parameter.");

            await GetSpectrum().SetProfileDescriptionAsync(profile, effects).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
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

    private static async Task EnsureSpectrumSupportedAsync()
    {
        if (!await GetSpectrum().IsSupportedAsync().ConfigureAwait(false))
            throw new BridgeErrorException(NotSupported, "NOT_SUPPORTED");
    }

    private static int GetRequiredInt(BridgeRequest request, string property)
    {
        if (!request.Parameters.TryGetProperty(property, out var prop) || prop.ValueKind != JsonValueKind.Number)
            throw new BridgeErrorException(-32602, $"Missing number parameter '{property}'.");
        return prop.GetInt32();
    }
}
