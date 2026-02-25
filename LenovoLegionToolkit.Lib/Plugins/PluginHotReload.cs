using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Implementation of plugin hot reload functionality
/// </summary>
public class PluginHotReload : IPluginHotReload, IDisposable
{
    private readonly IPluginSandbox _sandbox;
    private readonly string _stateStoragePath;
    private readonly string _backupPath;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginStateData> _savedStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _hotReloadEnabled = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private bool _disposed;

    /// <inheritdoc />
    public HotReloadConfiguration Configuration { get; set; } = new();

    /// <inheritdoc />
    public event EventHandler<HotReloadEventArgs>? PluginReloading;

    /// <inheritdoc />
    public event EventHandler<HotReloadEventArgs>? PluginReloaded;

    /// <inheritdoc />
    public event EventHandler<HotReloadEventArgs>? FileChanged;

    /// <inheritdoc />
    public event EventHandler<HotReloadEventArgs>? ReloadFailed;

    /// <summary>
    /// Creates a new instance of PluginHotReload
    /// </summary>
    /// <param name="sandbox">The plugin sandbox instance</param>
    /// <param name="storagePath">Path for storing plugin states</param>
    public PluginHotReload(IPluginSandbox sandbox, string storagePath)
    {
        _sandbox = sandbox ?? throw new ArgumentNullException(nameof(sandbox));
        _stateStoragePath = Path.Combine(storagePath, "PluginStates");
        _backupPath = Path.Combine(storagePath, "PluginBackups");

        // Ensure directories exist
        Directory.CreateDirectory(_stateStoragePath);
        Directory.CreateDirectory(_backupPath);

        // Load any existing saved states
        LoadSavedStates();
    }

    /// <inheritdoc />
    public bool EnableHotReload(string pluginId)
    {
        lock (_lock)
        {
            if (_hotReloadEnabled.Add(pluginId))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Hot reload enabled for plugin: {pluginId}");
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public bool DisableHotReload(string pluginId)
    {
        lock (_lock)
        {
            if (_hotReloadEnabled.Remove(pluginId))
            {
                // Stop watching if active
                StopWatching(pluginId);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Hot reload disabled for plugin: {pluginId}");
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsHotReloadEnabled(string pluginId)
    {
        lock (_lock)
        {
            return _hotReloadEnabled.Contains(pluginId);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReloadPluginAsync(string pluginId, string newAssemblyPath)
    {
        if (!Configuration.Enabled)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Hot reload is disabled, cannot reload plugin: {pluginId}");
            return false;
        }

        if (!IsHotReloadEnabled(pluginId))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Hot reload not enabled for plugin: {pluginId}");
            return false;
        }

        var stopwatch = Stopwatch.StartNew();
        var oldVersion = string.Empty;
        var newVersion = string.Empty;
        var stateSaved = false;
        var stateRestored = false;

        try
        {
            // Get current plugin info
            var oldInfo = _sandbox.GetPluginInfo(pluginId);
            oldVersion = oldInfo?.Version ?? "unknown";

            // Raise reloading event
            var reloadingArgs = new HotReloadEventArgs
            {
                PluginId = pluginId,
                OldVersion = oldVersion,
                Timestamp = DateTime.Now
            };
            PluginReloading?.Invoke(this, reloadingArgs);

            // Step 1: Save current state if plugin supports it
            if (Configuration.RestoreState)
            {
                stateSaved = await SavePluginStateAsync(pluginId);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"State saved for plugin {pluginId}: {stateSaved}");
            }

            // Step 2: Create backup if configured
            if (Configuration.KeepBackup && oldInfo != null)
            {
                var backupPath = CreateBackup(pluginId, oldInfo.Configuration.AllowedPaths.FirstOrDefault() ?? string.Empty);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Backup created for plugin {pluginId}: {backupPath ?? "failed"}");
            }

            // Step 3: Unload current plugin
            if (!_sandbox.UnloadPlugin(pluginId))
            {
                throw new InvalidOperationException($"Failed to unload plugin: {pluginId}");
            }

            // Step 4: Destroy old sandbox
            if (!_sandbox.DestroySandbox(pluginId))
            {
                throw new InvalidOperationException($"Failed to destroy sandbox for plugin: {pluginId}");
            }

            // Step 5: Create new sandbox with new assembly
            var config = new SandboxConfiguration
            {
                Permissions = oldInfo?.Configuration.Permissions ?? SandboxPermission.None,
                MaxMemoryMB = oldInfo?.Configuration.MaxMemoryMB ?? 100,
                AllowedPaths = new List<string> { Path.GetDirectoryName(newAssemblyPath) ?? string.Empty }
            };

            if (!_sandbox.CreateSandbox(pluginId, newAssemblyPath, config))
            {
                throw new InvalidOperationException($"Failed to create new sandbox for plugin: {pluginId}");
            }

            // Step 6: Load new plugin
            var newPlugin = _sandbox.LoadPlugin(pluginId);
            if (newPlugin == null)
            {
                throw new InvalidOperationException($"Failed to load new plugin: {pluginId}");
            }

            // Get new version
            var newInfo = _sandbox.GetPluginInfo(pluginId);
            newVersion = newInfo?.Version ?? "unknown";

            // Step 7: Restore state if applicable
            if (Configuration.RestoreState && stateSaved)
            {
                stateRestored = await RestorePluginStateAsync(pluginId, newPlugin);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"State restored for plugin {pluginId}: {stateRestored}");
            }

            stopwatch.Stop();

            // Raise reloaded event
            var reloadedArgs = new HotReloadEventArgs
            {
                PluginId = pluginId,
                OldVersion = oldVersion,
                NewVersion = newVersion,
                StateRestored = stateRestored,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.Now
            };
            PluginReloaded?.Invoke(this, reloadedArgs);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Successfully reloaded plugin {pluginId} from {oldVersion} to {newVersion} in {stopwatch.Elapsed.TotalMilliseconds}ms");

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to reload plugin {pluginId}: {ex.Message}", ex);

            // Raise reload failed event
            var failedArgs = new HotReloadEventArgs
            {
                PluginId = pluginId,
                OldVersion = oldVersion,
                NewVersion = newVersion,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTime.Now
            };
            ReloadFailed?.Invoke(this, failedArgs);

            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SavePluginStateAsync(string pluginId)
    {
        try
        {
            var plugin = _sandbox.GetPluginInfo(pluginId);
            if (plugin == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Cannot save state: Plugin not found: {pluginId}");
                return false;
            }

            // Note: In a real implementation, we would need to get the actual plugin instance
            // and check if it implements IStatefulPlugin
            // For now, we'll create a placeholder state

            var stateData = new PluginStateData
            {
                PluginId = pluginId,
                PluginVersion = plugin.Version,
                SavedAt = DateTime.Now,
                Metadata = new Dictionary<string, string>
                {
                    ["IsActive"] = plugin.IsActive.ToString(),
                    ["LoadedAt"] = plugin.LoadedAt.ToString("O")
                }
            };

            // Serialize and save
            var stateJson = JsonSerializer.Serialize(stateData);
            var statePath = GetStateFilePath(pluginId);
            await File.WriteAllTextAsync(statePath, stateJson);

            lock (_lock)
            {
                _savedStates[pluginId] = stateData;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"State saved for plugin {pluginId} to {statePath}");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to save state for plugin {pluginId}: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestorePluginStateAsync(string pluginId, IPlugin plugin)
    {
        try
        {
            var stateData = GetSavedState(pluginId);
            if (stateData == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"No saved state found for plugin: {pluginId}");
                return false;
            }

            // Check if plugin implements IStatefulPlugin
            if (plugin is IStatefulPlugin statefulPlugin)
            {
                if (stateData.StateBytes != null)
                {
                    var result = statefulPlugin.DeserializeState(stateData.StateBytes, stateData.PluginVersion);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"State restored for plugin {pluginId} using IStatefulPlugin: {result}");
                    return result;
                }
            }

            // For non-stateful plugins, we can still restore metadata
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin {pluginId} does not implement IStatefulPlugin, skipping state restoration");

            return false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to restore state for plugin {pluginId}: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc />
    public PluginStateData? GetSavedState(string pluginId)
    {
        lock (_lock)
        {
            if (_savedStates.TryGetValue(pluginId, out var stateData))
            {
                return stateData;
            }
        }

        // Try to load from disk
        var statePath = GetStateFilePath(pluginId);
        if (File.Exists(statePath))
        {
            try
            {
                var stateJson = File.ReadAllText(statePath);
                var stateData = JsonSerializer.Deserialize<PluginStateData>(stateJson);
                if (stateData != null)
                {
                    lock (_lock)
                    {
                        _savedStates[pluginId] = stateData;
                    }
                    return stateData;
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to load state for plugin {pluginId}: {ex.Message}");
            }
        }

        return null;
    }

    /// <inheritdoc />
    public bool ClearSavedState(string pluginId)
    {
        try
        {
            lock (_lock)
            {
                _savedStates.Remove(pluginId);
            }

            var statePath = GetStateFilePath(pluginId);
            if (File.Exists(statePath))
            {
                File.Delete(statePath);
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"State cleared for plugin: {pluginId}");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to clear state for plugin {pluginId}: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public bool StartWatching(string pluginId, string assemblyPath)
    {
        if (!Configuration.AutoReloadOnChange)
            return false;

        lock (_lock)
        {
            if (_watchers.ContainsKey(pluginId))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Already watching plugin: {pluginId}");
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(assemblyPath);
                var fileName = Path.GetFileName(assemblyPath);

                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Invalid assembly path for watching: {assemblyPath}");
                    return false;
                }

                var watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                watcher.Changed += async (sender, e) =>
                {
                    if (e.ChangeType == WatcherChangeTypes.Changed)
                    {
                        await OnPluginFileChanged(pluginId, e.FullPath);
                    }
                };

                _watchers[pluginId] = watcher;

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Started watching plugin {pluginId} at {assemblyPath}");

                return true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to start watching plugin {pluginId}: {ex.Message}");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool StopWatching(string pluginId)
    {
        lock (_lock)
        {
            if (_watchers.TryGetValue(pluginId, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(pluginId);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Stopped watching plugin: {pluginId}");

                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetWatchedPlugins()
    {
        lock (_lock)
        {
            return _watchers.Keys.ToList();
        }
    }

    /// <inheritdoc />
    public string? CreateBackup(string pluginId, string assemblyPath)
    {
        try
        {
            if (!File.Exists(assemblyPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Cannot create backup: Assembly not found: {assemblyPath}");
                return null;
            }

            var backupDir = Path.Combine(_backupPath, pluginId);
            Directory.CreateDirectory(backupDir);

            // Clean up old backups
            CleanupOldBackups(pluginId);

            // Create new backup
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
            var extension = Path.GetExtension(assemblyPath);
            var backupFileName = $"{fileName}_{timestamp}{extension}";
            var backupPath = Path.Combine(backupDir, backupFileName);

            File.Copy(assemblyPath, backupPath, overwrite: true);

            // Also copy related files (.pdb, .deps.json, etc.)
            var sourceDir = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
            foreach (var relatedFile in Directory.GetFiles(sourceDir, $"{fileName}.*"))
            {
                if (relatedFile != assemblyPath)
                {
                    var relatedBackupName = $"{fileName}_{timestamp}{Path.GetExtension(relatedFile)}";
                    var relatedBackupPath = Path.Combine(backupDir, relatedBackupName);
                    File.Copy(relatedFile, relatedBackupPath, overwrite: true);
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Backup created for plugin {pluginId}: {backupPath}");

            return backupPath;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to create backup for plugin {pluginId}: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestoreFromBackupAsync(string pluginId, string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Backup not found: {backupPath}");
                return false;
            }

            // Reload from backup
            var result = await ReloadPluginAsync(pluginId, backupPath);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Restore from backup for plugin {pluginId}: {result}");

            return result;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to restore from backup for plugin {pluginId}: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetBackups(string pluginId)
    {
        var backupDir = Path.Combine(_backupPath, pluginId);
        if (!Directory.Exists(backupDir))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(backupDir, "*.dll")
            .OrderByDescending(f => File.GetCreationTime(f));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            // Stop all watchers
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();

            // Clear saved states
            _savedStates.Clear();
            _hotReloadEnabled.Clear();
        }

        _disposed = true;
    }

    #region Private Methods

    private async Task OnPluginFileChanged(string pluginId, string filePath)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"File changed detected for plugin {pluginId}: {filePath}");

        // Raise file changed event
        var args = new HotReloadEventArgs
        {
            PluginId = pluginId,
            Timestamp = DateTime.Now
        };
        FileChanged?.Invoke(this, args);

        // Wait for the configured delay to avoid reloading while file is still being written
        if (Configuration.ReloadDelayMs > 0)
        {
            await Task.Delay(Configuration.ReloadDelayMs);
        }

        // Perform the reload
        await ReloadPluginAsync(pluginId, filePath);
    }

    private void LoadSavedStates()
    {
        try
        {
            if (!Directory.Exists(_stateStoragePath))
                return;

            foreach (var stateFile in Directory.GetFiles(_stateStoragePath, "*.json"))
            {
                try
                {
                    var stateJson = File.ReadAllText(stateFile);
                    var stateData = JsonSerializer.Deserialize<PluginStateData>(stateJson);
                    if (stateData != null)
                    {
                        _savedStates[stateData.PluginId] = stateData;
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to load state file {stateFile}: {ex.Message}");
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Loaded {_savedStates.Count} saved plugin states");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load saved states: {ex.Message}");
        }
    }

    private string GetStateFilePath(string pluginId)
    {
        return Path.Combine(_stateStoragePath, $"{pluginId}.json");
    }

    private void CleanupOldBackups(string pluginId)
    {
        try
        {
            var backupDir = Path.Combine(_backupPath, pluginId);
            if (!Directory.Exists(backupDir))
                return;

            var backups = Directory.GetFiles(backupDir, "*.dll")
                .Select(f => new { Path = f, Time = File.GetCreationTime(f) })
                .OrderByDescending(f => f.Time)
                .Skip(Configuration.MaxBackupCount)
                .ToList();

            foreach (var backup in backups)
            {
                try
                {
                    File.Delete(backup.Path);

                    // Also delete related files
                    var fileName = Path.GetFileNameWithoutExtension(backup.Path);
                    var dir = Path.GetDirectoryName(backup.Path) ?? string.Empty;
                    foreach (var relatedFile in Directory.GetFiles(dir, $"{fileName}.*"))
                    {
                        File.Delete(relatedFile);
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Cleaned up old backup: {backup.Path}");
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to delete old backup {backup.Path}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to cleanup old backups for {pluginId}: {ex.Message}");
        }
    }

    #endregion
}
