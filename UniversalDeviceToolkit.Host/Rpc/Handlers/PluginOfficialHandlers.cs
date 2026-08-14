using System;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Official plugin web-page RPC. Host looks up the loaded plugin via
/// <see cref="IPluginManager.TryGetPlugin"/> and invokes public methods by
/// reflection so Host does not take a compile-time reference on plugin projects.
/// Do not use plugins.getConfig/setConfig for these three plugins.
/// </summary>
public static class PluginOfficialHandlers
{
    private const string CustomMouseId = "custom-mouse";
    private const string ShellId = "shell-integration";
    private const string ViveId = "vive-tool";

    private static readonly IPluginManager PluginManager = IoCContainer.Resolve<IPluginManager>();
    private static BridgeRpcServer? _rpc;

    public static void Register(BridgeRpcServer rpc)
    {
        _rpc = rpc;

        rpc.RegisterHandler("plugin.customMouse.getState", (request, _) => HandleCustomMouseGetStateAsync(request));
        rpc.RegisterHandler("plugin.customMouse.applyWindows", (request, _) => HandleCustomMouseApplyWindowsAsync(request));
        rpc.RegisterHandler("plugin.customMouse.setCursorThemeMode", (request, _) => HandleCustomMouseSetCursorThemeModeAsync(request));
        rpc.RegisterHandler("plugin.customMouse.applyCursorThemeNow", (request, _) => HandleCustomMouseApplyCursorThemeNowAsync(request));
        rpc.RegisterHandler("plugin.customMouse.syncFromWindows", (request, _) => HandleCustomMouseSyncFromWindowsAsync(request));
        rpc.RegisterHandler("plugin.customMouse.restoreWindowsDefault", (request, _) => HandleCustomMouseRestoreWindowsDefaultAsync(request));

        rpc.RegisterHandler("plugin.shell.getStatus", (request, _) => HandleShellGetStatusAsync(request));
        rpc.RegisterHandler("plugin.shell.enable", (request, _) => HandleShellEnableAsync(request));
        rpc.RegisterHandler("plugin.shell.disable", (request, _) => HandleShellDisableAsync(request));
        rpc.RegisterHandler("plugin.shell.openFolder", (request, _) => HandleShellOpenFolderAsync(request));
        rpc.RegisterHandler("plugin.shell.openConfig", (request, _) => HandleShellOpenConfigAsync(request));
        rpc.RegisterHandler("plugin.shell.openManagedConfig", (request, _) => HandleShellOpenManagedConfigAsync(request));
        rpc.RegisterHandler("plugin.shell.syncManagedConfig", (request, _) => HandleShellSyncManagedConfigAsync(request));
        rpc.RegisterHandler("plugin.shell.resetManagedConfig", (request, _) => HandleShellResetManagedConfigAsync(request));
        rpc.RegisterHandler("plugin.shell.applyPreset", (request, _) => HandleShellApplyPresetAsync(request));
        rpc.RegisterHandler("plugin.shell.getProfile", (request, _) => HandleShellGetProfileAsync(request));
        rpc.RegisterHandler("plugin.shell.setProfile", (request, _) => HandleShellSetProfileAsync(request));
        rpc.RegisterHandler("plugin.shell.exportProfile", (request, _) => HandleShellExportProfileAsync(request));
        rpc.RegisterHandler("plugin.shell.importProfile", (request, _) => HandleShellImportProfileAsync(request));

        rpc.RegisterHandler("plugin.vive.getStatus", (request, _) => HandleViveGetStatusAsync(request));
        rpc.RegisterHandler("plugin.vive.listFeatures", (request, _) => HandleViveListFeaturesAsync(request));
        rpc.RegisterHandler("plugin.vive.searchFeatures", (request, _) => HandleViveSearchFeaturesAsync(request));
        rpc.RegisterHandler("plugin.vive.enableFeature", (request, _) => HandleViveEnableFeatureAsync(request));
        rpc.RegisterHandler("plugin.vive.disableFeature", (request, _) => HandleViveDisableFeatureAsync(request));
        rpc.RegisterHandler("plugin.vive.refresh", (request, _) => HandleViveRefreshAsync(request));
        rpc.RegisterHandler("plugin.vive.setPath", (request, _) => HandleViveSetPathAsync(request));
        rpc.RegisterHandler("plugin.vive.download", (request, _) => HandleViveDownloadAsync(request));
        rpc.RegisterHandler("plugin.vive.importFile", (request, _) => HandleViveImportFileAsync(request));
        rpc.RegisterHandler("plugin.vive.importUrl", (request, _) => HandleViveImportUrlAsync(request));
        rpc.RegisterHandler("plugin.vive.exportFeatures", (request, _) => HandleViveExportFeaturesAsync(request));
    }

    private static Task<BridgeResult> HandleCustomMouseGetStateAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var plugin = RequirePlugin(CustomMouseId);
            var state = await InvokeMemberAsync(plugin, "GetBridgeState").ConfigureAwait(false);
            return BridgeResult.Ok(state);
        });

    private static Task<BridgeResult> HandleCustomMouseApplyWindowsAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var plugin = RequirePlugin(CustomMouseId);
            var speed = GetRequiredInt32(request, "speed");
            var swapButtons = GetRequiredBoolean(request, "swapButtons");
            var ok = await InvokeMemberAsync(plugin, "ApplyWindowsAsync", speed, swapButtons).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleCustomMouseSetCursorThemeModeAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var plugin = RequirePlugin(CustomMouseId);
            var mode = GetRequiredInt32(request, "mode");
            var ok = await InvokeMemberAsync(plugin, "SetCursorThemeModeAsync", mode).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleCustomMouseApplyCursorThemeNowAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var plugin = RequirePlugin(CustomMouseId);
            var ok = await InvokeMemberAsync(plugin, "ApplyCursorStyleForCurrentThemeAsync").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleCustomMouseSyncFromWindowsAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var plugin = RequirePlugin(CustomMouseId);
            await InvokeMemberAsync(plugin, "ReloadSettingsFromSystem").ConfigureAwait(false);
            await InvokeMemberAsync(plugin, "SaveSettingsAsync").ConfigureAwait(false);
            var state = await InvokeMemberAsync(plugin, "GetBridgeState").ConfigureAwait(false);
            return BridgeResult.Ok(state);
        });

    private static Task<BridgeResult> HandleCustomMouseRestoreWindowsDefaultAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var plugin = RequirePlugin(CustomMouseId);
            var ok = await InvokeMemberAsync(plugin, "RestoreWindowsDefaultCursorThemeAsync").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellGetStatusAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var plugin = RequirePlugin(ShellId);
            var status = await InvokeMemberAsync(plugin, "GetBridgeStatus").ConfigureAwait(false);
            return BridgeResult.Ok(status);
        });

    private static Task<BridgeResult> HandleShellEnableAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "EnableShellAsync").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellDisableAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "DisableShellAsync").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellOpenFolderAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "OpenShellFolder").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellOpenConfigAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "OpenShellConfigFile").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellOpenManagedConfigAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "OpenManagedConfigFolder").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellSyncManagedConfigAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "SyncManagedConfigurationAsync").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellResetManagedConfigAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "ResetManagedConfigurationAsync").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellApplyPresetAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var preset = GetRequiredString(request, "preset");
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "ApplyPresetAsync", preset).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellGetProfileAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var profile = await InvokeMemberAsync(RequirePlugin(ShellId), "GetProfile").ConfigureAwait(false);
            return BridgeResult.Ok(new { profile });
        });

    private static Task<BridgeResult> HandleShellSetProfileAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            if (request.Parameters.ValueKind != JsonValueKind.Object
                || !request.Parameters.TryGetProperty("profile", out var profileElement))
            {
                throw new BridgeErrorException(-32602, "Missing object parameter 'profile'.");
            }

            var json = profileElement.GetRawText();
            var ok = await InvokeMemberAsync(RequirePlugin(ShellId), "SetProfileFromJsonAsync", json).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleShellExportProfileAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var path = GetRequiredString(request, "path");
            var result = await InvokeMemberAsync(RequirePlugin(ShellId), "ExportProfileToFile", path).ConfigureAwait(false);
            return BridgeResult.Ok(result);
        });

    private static Task<BridgeResult> HandleShellImportProfileAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var path = GetRequiredString(request, "path");
            var result = await InvokeMemberAsync(RequirePlugin(ShellId), "ImportProfileFromFileAsync", path).ConfigureAwait(false);
            return BridgeResult.Ok(result);
        });

    private static Task<BridgeResult> HandleViveGetStatusAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var status = await InvokeMemberAsync(RequirePlugin(ViveId), "GetBridgeStatusAsync").ConfigureAwait(false);
            return BridgeResult.Ok(status);
        });

    private static Task<BridgeResult> HandleViveListFeaturesAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var features = await InvokeMemberAsync(RequirePlugin(ViveId), "ListFeaturesForBridgeAsync").ConfigureAwait(false);
            return BridgeResult.Ok(new { features });
        });

    private static Task<BridgeResult> HandleViveSearchFeaturesAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var keyword = GetOptionalString(request, "keyword") ?? string.Empty;
            var features = await InvokeMemberAsync(RequirePlugin(ViveId), "SearchFeaturesForBridgeAsync", keyword).ConfigureAwait(false);
            return BridgeResult.Ok(new { features });
        });

    private static Task<BridgeResult> HandleViveEnableFeatureAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var featureId = GetRequiredInt32(request, "featureId");
            var ok = await InvokeMemberAsync(RequirePlugin(ViveId), "EnableFeatureAsync", featureId).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleViveDisableFeatureAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var featureId = GetRequiredInt32(request, "featureId");
            var ok = await InvokeMemberAsync(RequirePlugin(ViveId), "DisableFeatureAsync", featureId).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleViveRefreshAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            await InvokeMemberAsync(RequirePlugin(ViveId), "RefreshFeatures").ConfigureAwait(false);
            return BridgeResult.Ok(new { ok = true });
        });

    private static Task<BridgeResult> HandleViveSetPathAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var path = GetRequiredString(request, "path");
            var ok = await InvokeMemberAsync(RequirePlugin(ViveId), "SetViveToolPathAsync", path).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleViveDownloadAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            _ = request;
            var plugin = RequirePlugin(ViveId);
            var progress = new Progress<long>(bytes => Publish("plugin.vive.downloadProgress", new { bytes }));
            var ok = await InvokeMemberAsync(plugin, "DownloadViveToolAsync", progress).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static Task<BridgeResult> HandleViveImportFileAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var path = GetRequiredString(request, "path");
            var features = await InvokeMemberAsync(RequirePlugin(ViveId), "ImportFeaturesFromFileAsync", path).ConfigureAwait(false);
            return BridgeResult.Ok(new { features });
        });

    private static Task<BridgeResult> HandleViveImportUrlAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var url = GetRequiredString(request, "url");
            var features = await InvokeMemberAsync(RequirePlugin(ViveId), "ImportFeaturesFromUrlAsync", url).ConfigureAwait(false);
            return BridgeResult.Ok(new { features });
        });

    private static Task<BridgeResult> HandleViveExportFeaturesAsync(BridgeRequest request) =>
        RunAsync(async () =>
        {
            var path = GetRequiredString(request, "path");
            var ok = await InvokeMemberAsync(RequirePlugin(ViveId), "ExportFeaturesToFileAsync", path).ConfigureAwait(false);
            return BridgeResult.Ok(new { ok });
        });

    private static async Task<BridgeResult> RunAsync(Func<Task<BridgeResult>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            return BridgeResult.Error(-32603, $"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static object RequirePlugin(string pluginId)
    {
        if (!PluginManager.TryGetPlugin(pluginId, out var plugin) || plugin is null)
            throw new BridgeErrorException(-32004, $"Plugin '{pluginId}' is not loaded.");
        return plugin;
    }

    private static async Task<object?> InvokeMemberAsync(object instance, string methodName, params object?[] args)
    {
        var type = instance.GetType();
        MethodInfo? method = null;
        foreach (var candidate in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!string.Equals(candidate.Name, methodName, StringComparison.Ordinal))
                continue;

            var parameters = candidate.GetParameters();
            var required = 0;
            foreach (var parameter in parameters)
            {
                if (parameter.IsOptional || parameter.ParameterType == typeof(CancellationToken))
                    continue;
                required++;
            }

            if (required == args.Length || parameters.Length == args.Length)
            {
                method = candidate;
                break;
            }
        }

        if (method is null)
            throw new BridgeErrorException(-32601, $"Plugin method '{methodName}' was not found.");

        var methodParameters = method.GetParameters();
        var callArgs = new object?[methodParameters.Length];
        var argIndex = 0;
        for (var i = 0; i < methodParameters.Length; i++)
        {
            var parameter = methodParameters[i];
            if (argIndex < args.Length)
            {
                callArgs[i] = CoerceArgument(args[argIndex++], parameter.ParameterType);
            }
            else if (parameter.ParameterType == typeof(CancellationToken))
            {
                callArgs[i] = CancellationToken.None;
            }
            else if (parameter.HasDefaultValue)
            {
                callArgs[i] = parameter.DefaultValue;
            }
        }

        var raw = method.Invoke(instance, callArgs);
        if (raw is Task task)
        {
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty is null || task.GetType() == typeof(Task))
                return null;
            return resultProperty.GetValue(task);
        }

        return raw;
    }

    private static object? CoerceArgument(object? value, Type parameterType)
    {
        if (value is null)
            return null;

        var target = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        if (target.IsInstanceOfType(value))
            return value;

        if (target.IsEnum)
        {
            if (value is string name)
                return Enum.Parse(target, name, ignoreCase: true);
            return Enum.ToObject(target, Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        if (target == typeof(IProgress<long>) && value is IProgress<long>)
            return value;

        return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
    }

    private static string GetRequiredString(BridgeRequest request, string name)
    {
        if (request.Parameters.ValueKind != JsonValueKind.Object
            || !request.Parameters.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new BridgeErrorException(-32602, $"Missing or invalid string parameter '{name}'.");
        }

        return property.GetString()!;
    }

    private static string? GetOptionalString(BridgeRequest request, string name)
    {
        if (request.Parameters.ValueKind != JsonValueKind.Object
            || !request.Parameters.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static int GetRequiredInt32(BridgeRequest request, string name)
    {
        if (request.Parameters.ValueKind != JsonValueKind.Object
            || !request.Parameters.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out var value))
        {
            throw new BridgeErrorException(-32602, $"Missing integer '{name}' parameter.");
        }

        return value;
    }

    private static bool GetRequiredBoolean(BridgeRequest request, string name)
    {
        if (request.Parameters.ValueKind != JsonValueKind.Object
            || !request.Parameters.TryGetProperty(name, out var property)
            || property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new BridgeErrorException(-32602, $"Missing boolean '{name}' parameter.");
        }

        return property.GetBoolean();
    }

    private static void Publish(string name, object data)
    {
        try
        {
            _rpc?.Publish(name, data);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to publish bridge event '{name}': {ex.Message}", ex);
        }
    }
}
