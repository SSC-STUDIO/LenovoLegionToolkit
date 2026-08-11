using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Optimization;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Resources;
using UniversalDeviceToolkit.Lib.Serialization;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Windows optimization bridge (P1): optimization categories/actions, cleanup
/// estimation/execution and network-acceleration status/control.
///
/// Elevation note: the Host process normally runs un-elevated, so apply/revert/
/// cleanup mutations route through WindowsOptimizationElevationClient
/// (UniversalDeviceToolkit.Lib.Automation.Optimization), which starts an elevated
/// worker (UAC prompt) over a private named pipe when needed. The elevated worker
/// only supports built-in actions; plugin-provided actions fall back to the
/// in-process service and require the Host itself to run elevated.
/// </summary>
public static class OptimizationHandlers
{
    /// <summary>Elevation is required to mutate system state from the un-elevated bridge host.</summary>
    private const int ElevationRequiredErrorCode = -1006;

    private static JsonSerializerOptions? _networkJsonOptions;

    /// <summary>LltJson compact options (enums as strings) plus camelCase names for the frontend.</summary>
    private static JsonSerializerOptions NetworkJsonOptions => _networkJsonOptions ??= CreateNetworkJsonOptions();

    private static JsonSerializerOptions CreateNetworkJsonOptions()
    {
        var options = LltJson.CreateCompactOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        return options;
    }

    private static WindowsOptimizationService OptimizationService => IoCContainer.Resolve<WindowsOptimizationService>();

    private static WindowsCleanupService CleanupService => IoCContainer.Resolve<WindowsCleanupService>();

    private static INetworkAccelerationService NetworkService => IoCContainer.Resolve<INetworkAccelerationService>();

    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("optimization.getCategories", (_, ct) => HandleGetCategoriesAsync(ct));
        rpc.RegisterHandler("optimization.apply", (request, ct) => HandleApplyAsync(request, ct));
        rpc.RegisterHandler("optimization.revert", (request, ct) => HandleRevertAsync(request, ct));
        rpc.RegisterHandler("optimization.applyRecommended", (_, ct) => HandleApplyRecommendedAsync(ct));
        rpc.RegisterHandler("optimization.getActionStatus", (request, ct) => HandleGetActionStatusAsync(request, ct));
        rpc.RegisterHandler("cleanup.estimate", (request, ct) => HandleEstimateAsync(request, ct));
        rpc.RegisterHandler("cleanup.run", (request, ct) => HandleRunCleanupAsync(request, ct));
        rpc.RegisterHandler("network.getStatus", async (_, _) => await Task.FromResult(HandleNetworkGetStatusAsync()));
        rpc.RegisterHandler("network.saveConfig", (request, ct) => HandleNetworkSaveConfigAsync(request, ct));
        rpc.RegisterHandler("network.start", (_, ct) => HandleNetworkStartAsync(ct));
        rpc.RegisterHandler("network.stop", (_, ct) => HandleNetworkStopAsync(ct));
    }

    /// <summary>Categories with titles/descriptions resolved for the current culture plus per-action applied state.</summary>
    private static async Task<BridgeResult> HandleGetCategoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var service = OptimizationService;
            var categories = service.GetCategories();

            // Probe the applied state of every action in parallel; a failed probe
            // (or an action without an IsAppliedAsync predicate) surfaces as null → "unknown".
            var actionDefinitions = categories.SelectMany(category => category.Actions).ToList();
            var appliedStates = await Task.WhenAll(
                actionDefinitions.Select(action => service.TryGetActionAppliedAsync(action.Key, cancellationToken)))
                .ConfigureAwait(false);

            var appliedByKey = new Dictionary<string, bool?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < actionDefinitions.Count; i++)
                appliedByKey[actionDefinitions[i].Key] = appliedStates[i];

            return BridgeResult.Ok(new
            {
                categories = categories.Select(category => new
                {
                    key = category.Key,
                    title = Localize(category.TitleResourceKey),
                    description = Localize(category.DescriptionResourceKey),
                    pluginId = category.PluginId,
                    hasSettings = ResolveCategoryHasSettings(category.PluginId),
                    actions = category.Actions.Select(action => new
                    {
                        key = action.Key,
                        title = Localize(action.TitleResourceKey),
                        description = Localize(action.DescriptionResourceKey),
                        recommended = action.Recommended,
                        applied = appliedByKey.TryGetValue(action.Key, out var applied) ? applied : null,
                    }).ToArray(),
                }).ToArray(),
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>Applies the given actions through the elevation channel (or in-process for plugin actions).</summary>
    private static async Task<BridgeResult> HandleApplyAsync(BridgeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetActionKeys(request, out var actionKeys))
                throw new BridgeErrorException(-32602, "Missing or invalid array parameter 'actionKeys'.");

            await ExecuteOptimizationMutationsAsync(actionKeys, apply: true, cancellationToken).ConfigureAwait(false);

            return BridgeResult.Ok(new { applied = true });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return ToElevationError(ex, "optimization.apply");
        }
    }

    /// <summary>Reverts the given actions (rollback); built-in actions run in the elevated worker.</summary>
    private static async Task<BridgeResult> HandleRevertAsync(BridgeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetActionKeys(request, out var actionKeys))
                throw new BridgeErrorException(-32602, "Missing or invalid array parameter 'actionKeys'.");

            await ExecuteOptimizationMutationsAsync(actionKeys, apply: false, cancellationToken).ConfigureAwait(false);

            return BridgeResult.Ok(new { reverted = true });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return ToElevationError(ex, "optimization.revert");
        }
    }

    /// <summary>Applies every recommended non-cleanup action through the elevation channel.</summary>
    private static async Task<BridgeResult> HandleApplyRecommendedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var recommendedKeys = OptimizationService.GetCategories()
                .Where(category => !string.Equals(category.Key, WindowsOptimizationService.CleanupCategoryKey, StringComparison.OrdinalIgnoreCase))
                .SelectMany(category => category.Actions.Where(action => action.Recommended).Select(action => action.Key))
                .ToList();

            await ExecuteOptimizationMutationsAsync(recommendedKeys, apply: true, cancellationToken).ConfigureAwait(false);

            return BridgeResult.Ok(new { applied = true });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return ToElevationError(ex, "optimization.applyRecommended");
        }
    }

    /// <summary>Applied state of a single action: true/false/unknown (null).</summary>
    private static async Task<BridgeResult> HandleGetActionStatusAsync(BridgeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.Parameters.TryGetProperty("actionKey", out var keyProp) ||
                keyProp.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(keyProp.GetString()))
                throw new BridgeErrorException(-32602, "Missing or invalid string parameter 'actionKey'.");

            var applied = await OptimizationService
                .TryGetActionAppliedAsync(keyProp.GetString()!, cancellationToken)
                .ConfigureAwait(false);

            return BridgeResult.Ok(new { applied });
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

    /// <summary>Sums per-action estimates; individual estimate failures are skipped.</summary>
    private static async Task<BridgeResult> HandleEstimateAsync(BridgeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetActionKeys(request, out var actionKeys))
                throw new BridgeErrorException(-32602, "Missing or invalid array parameter 'actionKeys'.");

            var service = CleanupService;
            long totalBytes = 0;
            foreach (var key in actionKeys)
            {
                try
                {
                    totalBytes += await service.EstimateActionSizeAsync(key, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to estimate cleanup size. [action={key}]", ex);
                }
            }

            return BridgeResult.Ok(new { bytes = totalBytes });
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

    /// <summary>
    /// Runs cleanup. The selected cleanup actions (cleanup.temp, cleanup.custom, …)
    /// execute in the elevated worker; custom-cleanup "extra folders" ride along
    /// in-process (permission-tolerant) unless cleanup.custom was already selected.
    /// Failures map to -1006 when the elevation channel is unavailable.
    /// </summary>
    private static async Task<BridgeResult> HandleRunCleanupAsync(BridgeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!TryGetActionKeys(request, out var actionKeys))
                throw new BridgeErrorException(-32602, "Missing or invalid array parameter 'actionKeys'.");

            if (actionKeys.Count > 0)
            {
                if (!WindowsOptimizationElevationBridge.IsAvailable)
                {
                    LogElevationUnavailable("cleanup.run");
                    throw new BridgeErrorException(
                        ElevationRequiredErrorCode,
                        "The optimization elevation executor is not registered; cleanup requires elevation and the bridge host is not elevated.");
                }

                await WindowsOptimizationElevationBridge.ExecuteCleanupAsync(actionKeys, cancellationToken).ConfigureAwait(false);

                // Extra folders from custom cleanup rules (renderer CleanupRulesPanel).
                if (!actionKeys.Contains(WindowsOptimizationService.CustomCleanupActionKey, StringComparer.OrdinalIgnoreCase))
                    await CleanupService.ExecuteCustomCleanupAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await CleanupService.ExecuteCustomCleanupAsync(cancellationToken).ConfigureAwait(false);
            }

            return BridgeResult.Ok(new { done = true });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return ToElevationError(ex, "cleanup.run");
        }
    }

    private static BridgeResult HandleNetworkGetStatusAsync()
    {
        try
        {
            var service = NetworkService;
            var config = JsonSerializer.SerializeToElement(service.Config, NetworkJsonOptions);

            return BridgeResult.Ok(new
            {
                config,
                isBackendReady = service.IsBackendReady,
                isRunning = service.IsRunning,
                statusText = service.StatusText,
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleNetworkSaveConfigAsync(BridgeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.Parameters.TryGetProperty("config", out var configProp))
                throw new BridgeErrorException(-32602, "Missing 'config' parameter.");

            var replacement = JsonSerializer.Deserialize<NetworkAccelerationConfig>(configProp.GetRawText(), NetworkJsonOptions)
                ?? throw new BridgeErrorException(-32603, "Deserialized network config is null.");

            var service = NetworkService;
            CopyProperties(replacement, service.Config);
            await service.SaveConfigAsync(cancellationToken).ConfigureAwait(false);

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

    private static async Task<BridgeResult> HandleNetworkStartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var started = await NetworkService.StartAsync(cancellationToken).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = started });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleNetworkStopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await NetworkService.StopAsync(cancellationToken).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool TryGetActionKeys(BridgeRequest request, out IReadOnlyList<string> actionKeys)
    {
        actionKeys = [];
        if (!request.Parameters.TryGetProperty("actionKeys", out var keysProp) ||
            keysProp.ValueKind != JsonValueKind.Array)
            return false;

        var keys = new List<string>();
        foreach (var item in keysProp.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                return false;
            keys.Add(item.GetString()!);
        }

        actionKeys = keys;
        return true;
    }

    /// <summary>
    /// Resolves a Lib resource key (e.g. "WindowsOptimization_Action_*_Title") to the current
    /// culture's string. Falls back to the raw key so unknown keys stay visible to the frontend.
    /// </summary>
    private static string Localize(string resourceKey)
    {
        try
        {
            return Resource.ResourceManager.GetString(resourceKey) ?? resourceKey;
        }
        catch
        {
            return resourceKey;
        }
    }

    /// <summary>Mirrors WPF OptimizationCategoryViewModel.HasSettings (plugin settings page presence).</summary>
    private static bool ResolveCategoryHasSettings(string? pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        try
        {
            if (IoCContainer.TryResolve<IPluginManager>() is { } pluginManager)
            {
                var plugin = pluginManager.GetRegisteredPlugins()
                    .FirstOrDefault(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
                if (plugin is PluginBase pluginBase && pluginBase.GetSettingsPage() is not null)
                    return true;
            }

            return PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId).SupportsSettingsPage;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Executes apply/revert mutations. Built-in actions run through the elevation
    /// channel (WindowsOptimizationElevationClient starts an elevated worker over a
    /// private named pipe when the bridge host is un-elevated); the worker rejects
    /// plugin-provided actions by design, so those fall back to the in-process
    /// service — which can only succeed when the Host itself runs elevated.
    /// </summary>
    private static async Task ExecuteOptimizationMutationsAsync(
        IReadOnlyList<string> actionKeys,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (actionKeys.Count == 0)
            return;

        var (builtInKeys, pluginKeys) = SplitByPluginOrigin(actionKeys);

        if (builtInKeys.Count > 0)
        {
            if (!WindowsOptimizationElevationBridge.IsAvailable)
            {
                LogElevationUnavailable(apply ? "optimization.apply" : "optimization.revert");
                throw new BridgeErrorException(
                    ElevationRequiredErrorCode,
                    "The optimization elevation executor is not registered; this operation requires elevation and the bridge host is not elevated.");
            }

            if (apply)
            {
                await WindowsOptimizationElevationBridge
                    .ExecuteRecommendedAsync(builtInKeys, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var key in builtInKeys)
                    await WindowsOptimizationElevationBridge
                        .ExecuteActionAsync(key, apply: false, cancellationToken).ConfigureAwait(false);
            }
        }

        if (pluginKeys.Count > 0)
        {
            var service = OptimizationService;
            foreach (var key in pluginKeys)
            {
                try
                {
                    if (apply)
                        await service.ApplyActionAsync(key, cancellationToken).ConfigureAwait(false);
                    else
                        await service.RevertActionAsync(key, cancellationToken).ConfigureAwait(false);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new BridgeErrorException(
                        ElevationRequiredErrorCode,
                        $"Plugin action '{key}' cannot run in the elevated worker; " +
                        $"start the bridge host elevated to mutate plugin state. {ex.Message}");
                }
            }
        }
    }

    /// <summary>Classifies action keys by origin: built-in categories vs. plugin-provided ones.</summary>
    private static (List<string> BuiltIn, List<string> Plugin) SplitByPluginOrigin(IReadOnlyList<string> actionKeys)
    {
        var pluginActionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var category in OptimizationService.GetCategories())
            {
                if (string.IsNullOrWhiteSpace(category.PluginId))
                    continue;
                foreach (var action in category.Actions)
                    pluginActionKeys.Add(action.Key);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to classify optimization actions by plugin origin.", ex);
        }

        var builtIn = new List<string>();
        var plugin = new List<string>();
        foreach (var key in actionKeys)
        {
            if (pluginActionKeys.Contains(key))
                plugin.Add(key);
            else
                builtIn.Add(key);
        }

        return (builtIn, plugin);
    }

    /// <summary>Logs why the elevation channel cannot serve a mutation before returning -1006.</summary>
    private static void LogElevationUnavailable(string method)
    {
        Log.Instance.Warning(
            $"{method} requires elevation but the optimization elevation executor is not registered " +
            "(WindowsOptimizationElevationIoCModule missing from the Host IoC container).");
    }

    private static BridgeResult ToElevationError(Exception ex, string method)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Optimization bridge operation failed. [method={method}]", ex);

        return BridgeResult.Error(
            ElevationRequiredErrorCode,
            $"{method} requires elevation; the bridge host is not elevated. {ex.GetType().Name}: {ex.Message}");
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
