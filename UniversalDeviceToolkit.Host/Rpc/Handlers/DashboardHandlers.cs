using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Serialization;
using UniversalDeviceToolkit.Host.Rpc;
using UniversalDeviceToolkit.Host.Settings;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Dashboard bridge: reads/writes the host dashboard.json (same schema as WPF)
/// using camelCase JSON to match the Electron frontend.
/// </summary>
public static class DashboardHandlers
{
    private static JsonSerializerOptions? _options;

    private static JsonSerializerOptions Options => _options ??= CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = LltJson.CreateCompactOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        return options;
    }

    private static HostDashboardSettings GetSettings() => IoCContainer.Resolve<HostDashboardSettings>();

    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("dashboard.getConfig", (request, _) => HandleGetConfigAsync());
        rpc.RegisterHandler("dashboard.saveConfig", (request, _) => HandleSaveConfigAsync(request));
    }

    private static async Task<BridgeResult> HandleGetConfigAsync()
    {
        try
        {
            var store = GetSettings().Store;
            var node = JsonSerializer.SerializeToNode(store, store.GetType(), Options);

            await Task.CompletedTask;
            return BridgeResult.Ok(node);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSaveConfigAsync(BridgeRequest request)
    {
        try
        {
            if (!request.Parameters.TryGetProperty("config", out var configProp))
                throw new BridgeErrorException(-32602, "Missing 'config' parameter.");

            var replacement = JsonSerializer.Deserialize<HostDashboardSettings.DashboardSettingsStore>(configProp.GetRawText(), Options)
                ?? throw new BridgeErrorException(-32603, "Deserialized dashboard config is null.");
            var settings = GetSettings();
            var store = settings.Store;
            CopyProperties(replacement, store);

            // Keep the persisted layout consistent: an empty group list falls
            // back to the built-in default groups (mirrors WPF NormalizeGroups).
            if (store.Groups is not { Count: > 0 })
                store.Groups = DashboardGroup.DefaultGroups;

            settings.SynchronizeStore();

            await Task.CompletedTask;
            return BridgeResult.Ok(new { saved = true });
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

    private static void CopyProperties(object source, object target)
    {
        var sourceType = source.GetType();
        foreach (var property in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
                continue;
            if (property.SetMethod is not { } setter)
                continue;
            if (setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit)))
                continue;

            var value = property.GetValue(source);
            property.SetValue(target, value);
        }
    }
}
