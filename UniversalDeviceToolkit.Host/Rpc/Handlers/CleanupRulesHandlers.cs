using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Custom cleanup rule bridge: read/write the persisted rule set
/// (ApplicationSettings.CustomCleanupRules → CustomCleanupRule).
/// </summary>
public static class CleanupRulesHandlers
{
    private static ApplicationSettings Settings => IoCContainer.Resolve<ApplicationSettings>();

    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("cleanup.getCustomRules", (_, _) => Task.FromResult(HandleGetCustomRules()));
        rpc.RegisterHandler("cleanup.saveCustomRules", (request, _) => Task.FromResult(HandleSaveCustomRules(request)));
    }

    private static BridgeResult HandleGetCustomRules()
    {
        try
        {
            var rules = Settings.Store.CustomCleanupRules ?? [];

            return BridgeResult.Ok(new
            {
                rules = rules.Select(rule => new
                {
                    directoryPath = rule.DirectoryPath ?? string.Empty,
                    extensions = (rule.Extensions ?? []).ToArray(),
                    recursive = rule.Recursive,
                }).ToArray(),
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static BridgeResult HandleSaveCustomRules(BridgeRequest request)
    {
        try
        {
            if (request.Parameters.ValueKind != JsonValueKind.Object ||
                !request.Parameters.TryGetProperty("rules", out var rulesProp) ||
                rulesProp.ValueKind != JsonValueKind.Array)
            {
                throw new BridgeErrorException(-32602, "Missing or invalid array parameter 'rules'.");
            }

            var incoming = JsonSerializer.Deserialize<List<CustomCleanupRule>>(rulesProp.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? [];

            var normalized = incoming
                .Where(rule => rule is not null)
                .Select(rule => new CustomCleanupRule
                {
                    DirectoryPath = rule.DirectoryPath ?? string.Empty,
                    Extensions = (rule.Extensions ?? [])
                        .Where(extension => !string.IsNullOrWhiteSpace(extension))
                        .ToList(),
                    Recursive = rule.Recursive,
                })
                .ToList();

            Settings.Store.CustomCleanupRules = normalized;
            Settings.SynchronizeStore();

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
}
