using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Plugin domain bridge: marketplace list (with offline degradation), update check,
/// install/uninstall/import lifecycle and install progress events forwarded to the
/// Electron client.
/// </summary>
public static class PluginHandlers
{
    private static readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private static readonly PluginRepositoryService _repository = IoCContainer.Resolve<PluginRepositoryService>();
    // PluginInstallationService is not registered in Lib.Plugins.IoCModule, so it is
    // constructed directly here (it only depends on IPluginManager).
    private static readonly PluginInstallationService _installationService = new(_pluginManager);

    private static BridgeRpcServer? _rpc;
    private static string? _activeInstallPluginId;

    public static void Register(BridgeRpcServer rpc)
    {
        _rpc = rpc;
        rpc.RegisterHandler("plugins.list", (request, _) => HandleListAsync(request));
        rpc.RegisterHandler("plugins.checkUpdates", (request, _) => HandleCheckUpdatesAsync(request));
        rpc.RegisterHandler("plugins.install", (request, _) => HandleInstallAsync(request));
        rpc.RegisterHandler("plugins.uninstall", (request, _) => HandleUninstallAsync(request));
        rpc.RegisterHandler("plugins.import", (request, _) => HandleImportAsync(request));
        rpc.RegisterHandler("plugins.refresh", (request, _) => HandleRefreshAsync(request));
    }

    // ── handlers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Merges the online catalog with locally installed plugins into one camelCase
    /// view list. When the online store is unreachable the result degrades to
    /// installed-only plugins (online=false) instead of failing.
    /// </summary>
    private static async Task<BridgeResult> HandleListAsync(BridgeRequest request)
    {
        try
        {
            var forceRefresh = ReadForceRefresh(request);

            var onlineTask = FetchOnlinePluginsAsync(forceRefresh);
            var updatesTask = FetchUpdatesAsync();
            await Task.WhenAll(onlineTask, updatesTask).ConfigureAwait(false);

            var registered = _pluginManager.GetRegisteredPlugins().ToList();
            var installedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var plugin in registered)
            {
                if (!string.IsNullOrWhiteSpace(plugin.Id))
                    installedIds.Add(plugin.Id);
            }

            try
            {
                foreach (var id in _pluginManager.GetInstalledPluginIds())
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        installedIds.Add(id);
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"plugins.list: failed to enumerate installed plugin ids: {ex.Message}", ex);
            }

            var installedMetadata = new Dictionary<string, PluginMetadata?>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in installedIds)
            {
                try
                {
                    installedMetadata[id] = _pluginManager.GetPluginMetadata(id);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"plugins.list: failed to read metadata for {id}: {ex.Message}", ex);
                    installedMetadata[id] = null;
                }
            }

            var views = new List<object>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var onlinePlugins = onlineTask.Result;
            if (onlinePlugins is not null)
            {
                foreach (var manifest in onlinePlugins)
                {
                    if (string.IsNullOrWhiteSpace(manifest.Id) || !seen.Add(manifest.Id))
                        continue;
                    views.Add(ProjectOnlineView(manifest, installedMetadata, installedIds, updatesTask.Result));
                }
            }

            // Locally installed plugins that are not in the online catalog
            // (e.g. ZIP imports) still appear in the list.
            foreach (var id in installedIds.OrderBy(static id => id, StringComparer.OrdinalIgnoreCase))
            {
                if (!seen.Add(id))
                    continue;
                installedMetadata.TryGetValue(id, out var metadata);
                views.Add(ProjectInstalledOnlyView(id, metadata, updatesTask.Result));
            }

            return BridgeResult.Ok(new
            {
                plugins = views,
                online = onlinePlugins is not null,
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleCheckUpdatesAsync(BridgeRequest request)
    {
        try
        {
            var updates = await _pluginManager.CheckForUpdatesAsync().ConfigureAwait(false);
            return BridgeResult.Ok(new
            {
                updates = updates.Select(kv => new { id = kv.Key, availableVersion = kv.Value }).ToList(),
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleInstallAsync(BridgeRequest request)
    {
        try
        {
            if (!TryGetStringParameter(request, "pluginId", out var pluginId) || string.IsNullOrWhiteSpace(pluginId))
                throw new BridgeErrorException(-32602, "Missing or invalid string parameter 'pluginId'.");

            var available = await _repository.FetchAvailablePluginsAsync().ConfigureAwait(false);
            var manifest = available.FirstOrDefault(m => string.Equals(m.Id, pluginId, StringComparison.OrdinalIgnoreCase))
                ?? throw new BridgeErrorException(-32603, $"Plugin '{pluginId}' was not found in the online store.");

            _repository.DownloadProgressChanged += OnDownloadProgressChanged;
            _repository.DownloadCompleted += OnDownloadCompleted;
            _repository.DownloadFailed += OnDownloadFailed;
            _activeInstallPluginId = pluginId;
            try
            {
                var installed = await _repository.DownloadAndInstallPluginAsync(manifest).ConfigureAwait(false);
                if (!installed)
                    throw new BridgeErrorException(-32603, $"Failed to download and install plugin '{pluginId}'.");

                await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);
                _pluginManager.InstallPlugin(pluginId);

                PublishEvent("plugins.installed", new { pluginId });
                return BridgeResult.Ok(new { ok = true });
            }
            finally
            {
                _activeInstallPluginId = null;
                _repository.DownloadProgressChanged -= OnDownloadProgressChanged;
                _repository.DownloadCompleted -= OnDownloadCompleted;
                _repository.DownloadFailed -= OnDownloadFailed;
            }
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

    private static async Task<BridgeResult> HandleUninstallAsync(BridgeRequest request)
    {
        try
        {
            if (!TryGetStringParameter(request, "pluginId", out var pluginId) || string.IsNullOrWhiteSpace(pluginId))
                throw new BridgeErrorException(-32602, "Missing or invalid string parameter 'pluginId'.");

            if (!_pluginManager.CheckDependencies(pluginId, out _))
                return BridgeResult.Ok(new { ok = false, dependencyBlocked = true });

            _pluginManager.StopPlugin(pluginId);
            var removed = _pluginManager.UninstallPlugin(pluginId);

            if (removed)
                PublishEvent("plugins.uninstalled", new { pluginId });

            await Task.CompletedTask;
            return BridgeResult.Ok(new { ok = removed });
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

    private static async Task<BridgeResult> HandleImportAsync(BridgeRequest request)
    {
        try
        {
            if (!TryGetStringParameter(request, "filePath", out var filePath) || string.IsNullOrWhiteSpace(filePath))
                throw new BridgeErrorException(-32602, "Missing or invalid string parameter 'filePath'.");

            var installed = await _installationService
                .ExtractAndInstallPluginAsync(filePath, PluginPaths.GetPluginsDirectory())
                .ConfigureAwait(false);
            if (!installed)
                throw new BridgeErrorException(-32603, $"Failed to import plugin from '{filePath}'.");

            await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);

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

    private static async Task<BridgeResult> HandleRefreshAsync(BridgeRequest request)
    {
        try
        {
            await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);
            var registeredCount = _pluginManager.GetRegisteredPlugins().Count();
            return BridgeResult.Ok(new { registeredCount });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    // ── view projection ─────────────────────────────────────────────────────

    private static object ProjectOnlineView(
        PluginManifest manifest,
        Dictionary<string, PluginMetadata?> installedMetadata,
        HashSet<string> installedIds,
        Dictionary<string, string> updates)
    {
        var id = manifest.Id;
        installedMetadata.TryGetValue(id, out var metadata);
        var isInstalled = installedIds.Contains(id);
        updates.TryGetValue(id, out var availableVersion);
        var capabilities = PluginUiCapabilityResolver.ResolveFromManifest(manifest);

        return new
        {
            id,
            name = manifest.Name,
            description = manifest.Description,
            details = manifest.Details,
            usageGuide = manifest.UsageGuide,
            author = manifest.Author,
            version = manifest.Version,
            icon = manifest.Icon,
            iconBackground = manifest.IconBackground,
            tags = manifest.Tags ?? Array.Empty<string>(),
            isSystemPlugin = manifest.IsSystemPlugin,
            dependencies = manifest.Dependencies ?? Array.Empty<string>(),
            changelog = manifest.Changelog,
            releaseDate = manifest.ReleaseDate,
            fileSize = manifest.FileSize,
            installedVersion = metadata?.Version,
            updateAvailable = isInstalled && !string.IsNullOrWhiteSpace(availableVersion),
            availableVersion = string.IsNullOrWhiteSpace(availableVersion) ? null : availableVersion,
            state = isInstalled ? "Installed" : "NotInstalled",
            capabilities = new
            {
                settingsPage = capabilities.SupportsSettingsPage,
                featurePage = capabilities.SupportsFeaturePage,
                optimizationCategory = capabilities.SupportsOptimizationCategory,
                // Lib.Plugins has no executable entry point concept yet; reserved for future use.
                executableEntryPoint = false,
            },
        };
    }

    private static object ProjectInstalledOnlyView(string pluginId, PluginMetadata? metadata, Dictionary<string, string> updates)
    {
        var version = metadata?.Version ?? string.Empty;
        var capabilities = PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId);
        updates.TryGetValue(pluginId, out var availableVersion);

        return new
        {
            id = pluginId,
            name = string.IsNullOrWhiteSpace(metadata?.Name) ? pluginId : metadata!.Name,
            description = metadata?.Description ?? string.Empty,
            details = (string?)null,
            usageGuide = (string?)null,
            author = metadata?.Author ?? string.Empty,
            version,
            icon = metadata?.Icon ?? string.Empty,
            iconBackground = (string?)null,
            tags = metadata?.Tags ?? Array.Empty<string>(),
            isSystemPlugin = metadata?.IsSystemPlugin ?? false,
            dependencies = metadata?.Dependencies ?? Array.Empty<string>(),
            changelog = (string?)null,
            releaseDate = string.Empty,
            fileSize = 0L,
            installedVersion = string.IsNullOrWhiteSpace(version) ? null : version,
            updateAvailable = !string.IsNullOrWhiteSpace(version) && !string.IsNullOrWhiteSpace(availableVersion),
            availableVersion = string.IsNullOrWhiteSpace(availableVersion) ? null : availableVersion,
            state = "Installed",
            capabilities = new
            {
                settingsPage = capabilities.SupportsSettingsPage,
                featurePage = capabilities.SupportsFeaturePage,
                optimizationCategory = capabilities.SupportsOptimizationCategory,
                executableEntryPoint = false,
            },
        };
    }

    // ── download progress event forwarding ─────────────────────────────────

    private static void OnDownloadProgressChanged(object? sender, PluginDownloadProgress progress)
    {
        try
        {
            PublishEvent("plugins.installProgress", new
            {
                pluginId = progress.PluginId,
                progressPercentage = progress.ProgressPercentage,
                statusText = string.Empty,
                phase = "downloading",
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to forward plugin download progress.", ex);
        }
    }

    private static void OnDownloadCompleted(object? sender, string pluginId)
    {
        try
        {
            PublishEvent("plugins.installProgress", new
            {
                pluginId,
                progressPercentage = 100d,
                statusText = string.Empty,
                phase = "completed",
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to forward plugin download completion.", ex);
        }
    }

    private static void OnDownloadFailed(object? sender, string errorMessage)
    {
        try
        {
            PublishEvent("plugins.installProgress", new
            {
                pluginId = _activeInstallPluginId ?? string.Empty,
                progressPercentage = 0d,
                statusText = errorMessage,
                phase = "failed",
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to forward plugin download failure.", ex);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static async Task<List<PluginManifest>?> FetchOnlinePluginsAsync(bool forceRefresh)
    {
        try
        {
            return await _repository.FetchAvailablePluginsAsync(forceRefresh).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"plugins.list: online store unavailable, degrading to installed plugins: {ex.Message}", ex);
            return null;
        }
    }

    private static async Task<Dictionary<string, string>> FetchUpdatesAsync()
    {
        try
        {
            return await _pluginManager.CheckForUpdatesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"plugins.list: update check failed: {ex.Message}", ex);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static bool ReadForceRefresh(BridgeRequest request)
    {
        return request.Parameters.ValueKind == JsonValueKind.Object
            && request.Parameters.TryGetProperty("forceRefresh", out var prop)
            && prop.ValueKind == JsonValueKind.True;
    }

    private static bool TryGetStringParameter(BridgeRequest request, string name, out string? value)
    {
        value = null;
        if (request.Parameters.ValueKind != JsonValueKind.Object)
            return false;
        if (!request.Parameters.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
            return false;
        value = prop.GetString();
        return true;
    }

    private static void PublishEvent(string name, object data)
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
