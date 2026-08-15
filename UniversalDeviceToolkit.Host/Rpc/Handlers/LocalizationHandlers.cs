using System.Text.Json;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Process-wide culture bridge shared by the Electron renderer and the Host.
/// </summary>
public static class LocalizationHandlers
{
    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("localization.getCulture", async (_, _) =>
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return BridgeResult.Ok(new { culture = LocalizationRuntime.CurrentCulture.Name });
        });

        rpc.RegisterHandler("localization.setCulture", async request =>
        {
            if (!TryGetCultureName(request.Parameters, out var cultureName))
                return BridgeResult.Error(-32602, "Expected parameter: culture (string).");

            var culture = await LocalizationRuntime.SetCultureAsync(cultureName, persist: true)
                .ConfigureAwait(false);
            return BridgeResult.Ok(new { culture = culture.Name });
        });
    }

    private static bool TryGetCultureName(JsonElement parameters, out string cultureName)
    {
        cultureName = string.Empty;
        if (parameters.ValueKind != JsonValueKind.Object)
            return false;

        if (!parameters.TryGetProperty("culture", out var culture) ||
            culture.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var value = culture.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return false;

        cultureName = value.Trim();
        return true;
    }
}
