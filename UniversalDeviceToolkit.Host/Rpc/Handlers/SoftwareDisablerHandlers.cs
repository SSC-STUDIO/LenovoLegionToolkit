using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Bridges the WPF SettingsApplicationBehaviorControl software disabler cards
/// (Lenovo Vantage / Legion Zone / Lenovo Hotkeys Fn keys): status probe with
/// the supported-Legion-machine flag and enable/disable toggling.
/// </summary>
public static class SoftwareDisablerHandlers
{
    private static readonly VantageDisabler Vantage = IoCContainer.Resolve<VantageDisabler>();
    private static readonly LegionZoneDisabler LegionZone = IoCContainer.Resolve<LegionZoneDisabler>();
    private static readonly FnKeysDisabler FnKeys = IoCContainer.Resolve<FnKeysDisabler>();

    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("software.getStatus", (request, _) => HandleGetStatusAsync(request));
        rpc.RegisterHandler("software.setEnabled", (request, _) => HandleSetEnabledAsync(request));
    }

    private static AbstractSoftwareDisabler? GetDisabler(string app) => app switch
    {
        "vantage" => Vantage,
        "legionZone" => LegionZone,
        "fnKeys" => FnKeys,
        _ => null,
    };

    private static async Task<BridgeResult> HandleGetStatusAsync(BridgeRequest request)
    {
        try
        {
            var app = ReadApp(request);
            var disabler = GetDisabler(app);
            if (disabler is null)
                return BridgeResult.Error(-32602, $"Unknown software app '{app}'.");

            var status = await disabler.GetStatusAsync().ConfigureAwait(false);
            var machineInformation = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

            return BridgeResult.Ok(new
            {
                status = status.ToString(),
                isLegionMachine = Compatibility.IsSupportedLegionMachine(machineInformation),
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSetEnabledAsync(BridgeRequest request)
    {
        try
        {
            var app = ReadApp(request);
            var disabler = GetDisabler(app);
            if (disabler is null)
                return BridgeResult.Error(-32602, $"Unknown software app '{app}'.");

            if (!request.Parameters.TryGetProperty("enabled", out var enabledProp) ||
                enabledProp.ValueKind != JsonValueKind.True && enabledProp.ValueKind != JsonValueKind.False)
                return BridgeResult.Error(-32602, "Missing boolean parameter 'enabled'.");

            if (enabledProp.GetBoolean())
                await disabler.EnableAsync().ConfigureAwait(false);
            else
                await disabler.DisableAsync().ConfigureAwait(false);

            var status = await disabler.GetStatusAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true, status = status.ToString() });
        }
        catch (SoftwareDisablerException ex)
        {
            return BridgeResult.Error(-32603, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string ReadApp(BridgeRequest request)
    {
        if (!request.Parameters.TryGetProperty("app", out var appProp) ||
            appProp.ValueKind != JsonValueKind.String)
            throw new BridgeErrorException(-32602, "Missing string parameter 'app'.");
        return appProp.GetString()!;
    }
}
