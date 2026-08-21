using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
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
    private static readonly SemaphoreSlim InstallSessionGate = new(1, 1);

    public static void Register(BridgeRpcServer rpc)
    {
        _rpc = rpc;
        rpc.RegisterHandler("plugins.list", (request, _) => HandleListAsync(request));
        rpc.RegisterHandler("plugins.checkUpdates", (request, _) => HandleCheckUpdatesAsync(request));
        rpc.RegisterHandler("plugins.install", (request, _) => HandleInstallAsync(request));
        rpc.RegisterHandler("plugins.uninstall", (request, _) => HandleUninstallAsync(request));
        rpc.RegisterHandler("plugins.import", (request, _) => HandleImportAsync(request));
        rpc.RegisterHandler("plugins.refresh", (request, _) => HandleRefreshAsync(request));
        rpc.RegisterHandler("plugins.getConfig", (request, _) => HandleGetConfigAsync(request));
        rpc.RegisterHandler("plugins.setConfig", (request, _) => HandleSetConfigAsync(request));
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

            await InstallSessionGate.WaitAsync().ConfigureAwait(false);
            try
            {
                _repository.DownloadProgressChanged += OnDownloadProgressChanged;
                _repository.DownloadCompleted += OnDownloadCompleted;
                _repository.DownloadFailed += OnDownloadFailed;
                _activeInstallPluginId = pluginId;
                try
                {
                    var outcome = await _repository
                        .DownloadAndInstallPluginWithOutcomeAsync(manifest)
                        .ConfigureAwait(false);
                    if (!outcome.Success)
                    {
                        return BridgeResult.Ok(new
                        {
                            ok = false,
                            degraded = outcome.Degraded,
                            unloadPending = outcome.UnloadPending,
                            recoveryId = outcome.RecoveryId,
                            recoveryPath = outcome.RecoveryPath,
                            error = outcome.Error,
                        });
                    }

                    PublishEvent("plugins.installed", new { pluginId });
                    return BridgeResult.Ok(new
                    {
                        ok = true,
                        degraded = false,
                        unloadPending = false,
                    });
                }
                finally
                {
                    _activeInstallPluginId = null;
                    _repository.DownloadProgressChanged -= OnDownloadProgressChanged;
                    _repository.DownloadCompleted -= OnDownloadCompleted;
                    _repository.DownloadFailed -= OnDownloadFailed;
                }
            }
            finally
            {
                InstallSessionGate.Release();
            }
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (PluginOperationRecoveryException ex)
        {
            return BridgeResult.Ok(new
            {
                ok = false,
                degraded = true,
                unloadPending = ex.UnloadPending,
                recoveryId = ex.RecoveryId,
                recoveryPath = ex.RecoveryPath,
                error = ex.Message,
            });
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

            var removed = _pluginManager.UninstallPlugin(pluginId);

            if (removed)
                PublishEvent("plugins.uninstalled", new { pluginId });

            var unloadState = _pluginManager.GetPluginRuntimeUnloadState(pluginId);
            await Task.CompletedTask;
            return BridgeResult.Ok(new
            {
                ok = removed,
                unloadPending = !removed &&
                                unloadState == PluginRuntimeUnloadState.UnloadRequested,
            });
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

            var outcome = await _installationService
                .ExtractAndInstallPluginWithOutcomeAsync(
                    filePath,
                    PluginPaths.GetPluginsDirectory())
                .ConfigureAwait(false);
            return BridgeResult.Ok(new
            {
                ok = outcome.Success,
                degraded = outcome.Degraded,
                unloadPending = outcome.UnloadPending,
                recoveryId = outcome.RecoveryId,
                recoveryPath = outcome.RecoveryPath,
                error = outcome.Error,
            });
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
            var outcome = await _pluginManager
                .ScanAndLoadPluginsWithOutcomeAsync(forceRefresh: true)
                .ConfigureAwait(false);
            return BridgeResult.Ok(new
            {
                ok = outcome.Success,
                registeredCount = outcome.RegisteredCount,
                degraded = outcome.Degraded,
                unloadPending = outcome.UnloadPending,
                failures = outcome.Failures,
            });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    // ── plugin configuration RPC ────────────────────────────────────────────

    // Plugin web pages (contributes.webPage) need a way to persist their own
    // settings. The legacy IPluginConfiguration route only exposes single
    // typed key access and stores into an AppData sidecar file, so config is
    // read/written directly on the plugin's own config.json instead. Isolation
    // is inherent: every plugin only ever touches {pluginDir}/config.json.
    private static readonly object ConfigFileGate = new();
    private static readonly JsonSerializerOptions ConfigJsonOptions = new() { WriteIndented = true };
    private static readonly Encoding ConfigUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static async Task<BridgeResult> HandleGetConfigAsync(BridgeRequest request)
    {
        try
        {
            if (!TryGetStringParameter(request, "pluginId", out var pluginId) || !IsValidPluginId(pluginId))
                throw new BridgeErrorException(-32602, "Missing or invalid string parameter 'pluginId'.");

            var validPluginId = pluginId!;
            var configPath = ResolvePluginConfigPath(validPluginId);

            string? key = null;
            if (request.Parameters.ValueKind == JsonValueKind.Object
                && request.Parameters.TryGetProperty("key", out var keyProperty)
                && keyProperty.ValueKind == JsonValueKind.String)
            {
                key = keyProperty.GetString();
                if (string.IsNullOrWhiteSpace(key))
                    throw new BridgeErrorException(-32602, "Invalid string parameter 'key'.");
            }

            var config = await Task.Run(() =>
            {
                lock (ConfigFileGate)
                {
                    return ReadConfigMap(configPath);
                }
            }).ConfigureAwait(false);

            if (key is not null)
            {
                var single = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                if (config.TryGetValue(key, out var value))
                    single[key] = value;
                return BridgeResult.Ok(new { config = single });
            }

            return BridgeResult.Ok(new { config });
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

    private static async Task<BridgeResult> HandleSetConfigAsync(BridgeRequest request)
    {
        try
        {
            if (!TryGetStringParameter(request, "pluginId", out var pluginId) || !IsValidPluginId(pluginId))
                throw new BridgeErrorException(-32602, "Missing or invalid string parameter 'pluginId'.");

            var validPluginId = pluginId!;

            if (request.Parameters.ValueKind != JsonValueKind.Object
                || !request.Parameters.TryGetProperty("key", out var keyProperty)
                || keyProperty.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(keyProperty.GetString()))
            {
                throw new BridgeErrorException(-32602, "Missing or invalid string parameter 'key'.");
            }

            if (!request.Parameters.TryGetProperty("value", out var valueProperty))
                throw new BridgeErrorException(-32602, "Missing parameter 'value'.");

            var key = keyProperty.GetString()!;
            var value = valueProperty.Clone();
            var configPath = ResolvePluginConfigPath(validPluginId);

            await Task.Run(() =>
            {
                lock (ConfigFileGate)
                {
                    var config = ReadConfigMap(configPath);
                    config[key] = value;
                    var json = JsonSerializer.Serialize(config, ConfigJsonOptions);
                    AtomicWriteAllText(configPath, json);
                }
            }).ConfigureAwait(false);

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

    private static Dictionary<string, JsonElement> ReadConfigMap(string configPath)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (!File.Exists(configPath))
            return map;

        try
        {
            using var stream = File.OpenRead(configPath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return map;

            foreach (var property in document.RootElement.EnumerateObject())
                map[property.Name] = property.Value.Clone();

            return map;
        }
        catch (JsonException ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"plugins config: ignoring unreadable JSON at {configPath}.", ex);
            return map;
        }
    }

    /// <summary>
    /// Persist via temp + replace so a crash mid-write cannot leave a torn
    /// config.json. Disk is the only store: the in-memory map is a local
    /// read-modify copy and is never published as a cache before this I/O
    /// succeeds.
    /// </summary>
    private static void AtomicWriteAllText(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(tempPath, contents, ConfigUtf8);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"plugins config: failed to delete temp file '{tempPath}'.", ex);
                }
            }
        }
    }

    private static string ResolvePluginConfigPath(string pluginId)
    {
        try
        {
            var pluginDirectory = ResolvePluginDirectory(_pluginManager.GetPluginMetadata(pluginId));
            if (!string.IsNullOrWhiteSpace(pluginDirectory))
                return Path.Combine(pluginDirectory, "config.json");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"plugins config: failed to resolve plugin directory for {pluginId}, falling back to default.", ex);
        }

        return PluginPaths.GetPluginConfigFilePath(pluginId);
    }

    /// <summary>
    /// Guards the plugin id before it is used to build a filesystem path:
    /// rejects path separators, traversal segments and characters that are
    /// invalid in Windows file names so a hostile renderer cannot escape the
    /// plugin directory.
    /// </summary>
    private static bool IsValidPluginId(string? pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        if (pluginId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;

        var normalized = pluginId.Replace('\\', '/');
        return !normalized.Contains("..", StringComparison.Ordinal)
            && !normalized.Contains('/', StringComparison.Ordinal)
            && !normalized.Contains(':', StringComparison.Ordinal);
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
        var webPage = manifest.Contributes?.WebPage;

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
            // Local package directory + optional web UI entry. The Electron shell
            // uses these to embed the plugin's web page (contributes.webPage).
            directory = ResolvePluginDirectory(metadata),
            webPage = webPage is { Entry.Length: > 0 } ? webPage.Entry : null,
            capabilities = new
            {
                settingsPage = capabilities.SupportsSettingsPage,
                featurePage = capabilities.SupportsFeaturePage,
                optimizationCategory = capabilities.SupportsOptimizationCategory,
                webPage = capabilities.SupportsWebPage,
                // Lib.Plugins has no executable entry point concept yet; reserved for future use.
                executableEntryPoint = false,
            },
        };
    }

    /// <summary>Plugin package root: the directory that holds plugin.json/manifest.</summary>
    private static string? ResolvePluginDirectory(PluginMetadata? metadata)
    {
        try
        {
            if (metadata?.FilePath is { Length: > 0 } filePath && File.Exists(filePath))
                return Path.GetDirectoryName(filePath);

            if (metadata?.Id is { Length: > 0 } id)
            {
                var candidate = Path.Combine(PluginPaths.GetPluginsDirectory(), id);
                if (Directory.Exists(candidate))
                    return candidate;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("plugins.list: failed to resolve plugin directory.", ex);
        }

        return null;
    }

    private static object ProjectInstalledOnlyView(string pluginId, PluginMetadata? metadata, Dictionary<string, string> updates)
    {
        var version = metadata?.Version ?? string.Empty;
        var capabilities = PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId);
        updates.TryGetValue(pluginId, out var availableVersion);
        var installedManifest = PluginUiCapabilityResolver.ReadInstalledManifest(pluginId);
        var webPage = installedManifest?.Contributes?.WebPage;

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
            directory = ResolvePluginDirectory(metadata),
            webPage = webPage is { Entry.Length: > 0 } ? webPage.Entry : null,
            capabilities = new
            {
                settingsPage = capabilities.SupportsSettingsPage,
                featurePage = capabilities.SupportsFeaturePage,
                optimizationCategory = capabilities.SupportsOptimizationCategory,
                webPage = capabilities.SupportsWebPage,
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
