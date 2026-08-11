using System;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Startup behavior bridge — mirrors the WPF SettingsApplicationBehaviorControl
/// autorun combo (AutorunState: Enabled / EnabledDelayed / Disabled) backed by
/// the Lib Autorun helper (scheduled task based).
/// </summary>
public static class StartupHandlers
{
    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("app.getAutorun", (_, _) => HandleGetAutorunAsync());
        rpc.RegisterHandler("app.setAutorun", (request, _) => HandleSetAutorunAsync(request));
    }

    private static Task<BridgeResult> HandleGetAutorunAsync()
    {
        try
        {
            return Task.FromResult(BridgeResult.Ok(new { state = Autorun.State.ToString() }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}"));
        }
    }

    private static Task<BridgeResult> HandleSetAutorunAsync(BridgeRequest request)
    {
        try
        {
            if (!request.Parameters.TryGetProperty("state", out var stateProp) ||
                stateProp.ValueKind != JsonValueKind.String)
                return Task.FromResult(BridgeResult.Error(-32602, "Missing string parameter 'state'."));

            if (!Enum.TryParse<AutorunState>(stateProp.GetString()!, ignoreCase: true, out var state))
                return Task.FromResult(BridgeResult.Error(-32602, $"Unknown AutorunState '{stateProp.GetString()}'."));

            Autorun.Set(state);
            return Task.FromResult(BridgeResult.Ok(new { ok = true, state = Autorun.State.ToString() }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}"));
        }
    }
}
