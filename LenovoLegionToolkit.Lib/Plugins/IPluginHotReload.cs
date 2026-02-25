using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Represents the state of a plugin that can be persisted across reloads
/// </summary>
public class PluginStateData
{
    /// <summary>
    /// Plugin ID
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Serialized state data
    /// </summary>
    public byte[]? StateBytes { get; set; }

    /// <summary>
    /// State format version
    /// </summary>
    public int StateVersion { get; set; } = 1;

    /// <summary>
    /// When the state was saved
    /// </summary>
    public DateTime SavedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Plugin version when state was saved
    /// </summary>
    public string PluginVersion { get; set; } = string.Empty;

    /// <summary>
    /// Custom metadata for state restoration
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Event args for hot reload events
/// </summary>
public class HotReloadEventArgs : EventArgs
{
    /// <summary>
    /// Plugin ID being reloaded
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Old plugin version
    /// </summary>
    public string OldVersion { get; set; } = string.Empty;

    /// <summary>
    /// New plugin version
    /// </summary>
    public string NewVersion { get; set; } = string.Empty;

    /// <summary>
    /// Whether state was successfully restored
    /// </summary>
    public bool StateRestored { get; set; }

    /// <summary>
    /// Error message if reload failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of the reload operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// When the reload occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Configuration for hot reload behavior
/// </summary>
public class HotReloadConfiguration
{
    /// <summary>
    /// Whether hot reload is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to automatically reload when plugin files change
    /// </summary>
    public bool AutoReloadOnChange { get; set; } = false;

    /// <summary>
    /// Delay before reloading after detecting a change (in milliseconds)
    /// </summary>
    public int ReloadDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to restore state after reload
    /// </summary>
    public bool RestoreState { get; set; } = true;

    /// <summary>
    /// Maximum time to wait for state serialization (in seconds)
    /// </summary>
    public int StateSerializationTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum time to wait for plugin shutdown (in seconds)
    /// </summary>
    public int ShutdownTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to keep old plugin version as backup
    /// </summary>
    public bool KeepBackup { get; set; } = true;

    /// <summary>
    /// Number of backup versions to keep
    /// </summary>
    public int MaxBackupCount { get; set; } = 3;
}

/// <summary>
/// Interface for plugins that support state persistence across reloads
/// </summary>
public interface IStatefulPlugin : IPlugin
{
    /// <summary>
    /// Serializes the plugin's current state
    /// </summary>
    /// <returns>Serialized state data</returns>
    byte[] SerializeState();

    /// <summary>
    /// Deserializes and restores the plugin's state
    /// </summary>
    /// <param name="stateData">Serialized state data</param>
    /// <param name="previousVersion">Version of the plugin that saved the state</param>
    /// <returns>True if state was successfully restored</returns>
    bool DeserializeState(byte[] stateData, string previousVersion);

    /// <summary>
    /// Gets the current state version for compatibility checking
    /// </summary>
    int StateVersion { get; }
}

/// <summary>
/// Interface for plugin hot reload functionality
/// </summary>
public interface IPluginHotReload
{
    /// <summary>
    /// Gets or sets the hot reload configuration
    /// </summary>
    HotReloadConfiguration Configuration { get; set; }

    /// <summary>
    /// Enables hot reload for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if hot reload was enabled</returns>
    bool EnableHotReload(string pluginId);

    /// <summary>
    /// Disables hot reload for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if hot reload was disabled</returns>
    bool DisableHotReload(string pluginId);

    /// <summary>
    /// Checks if hot reload is enabled for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if hot reload is enabled</returns>
    bool IsHotReloadEnabled(string pluginId);

    /// <summary>
    /// Performs a hot reload of a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="newAssemblyPath">Path to the new plugin assembly</param>
    /// <returns>True if reload was successful</returns>
    Task<bool> ReloadPluginAsync(string pluginId, string newAssemblyPath);

    /// <summary>
    /// Saves the current state of a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if state was saved</returns>
    Task<bool> SavePluginStateAsync(string pluginId);

    /// <summary>
    /// Restores the state of a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="plugin">The plugin instance to restore state to</param>
    /// <returns>True if state was restored</returns>
    Task<bool> RestorePluginStateAsync(string pluginId, IPlugin plugin);

    /// <summary>
    /// Gets the saved state for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>Saved state data or null if not found</returns>
    PluginStateData? GetSavedState(string pluginId);

    /// <summary>
    /// Clears the saved state for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if state was cleared</returns>
    bool ClearSavedState(string pluginId);

    /// <summary>
    /// Starts watching a plugin's assembly file for changes
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="assemblyPath">Path to the plugin assembly</param>
    /// <returns>True if watching was started</returns>
    bool StartWatching(string pluginId, string assemblyPath);

    /// <summary>
    /// Stops watching a plugin's assembly file
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if watching was stopped</returns>
    bool StopWatching(string pluginId);

    /// <summary>
    /// Gets all plugins currently being watched
    /// </summary>
    /// <returns>List of watched plugin IDs</returns>
    IEnumerable<string> GetWatchedPlugins();

    /// <summary>
    /// Creates a backup of the current plugin assembly
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="assemblyPath">Path to the plugin assembly</param>
    /// <returns>Path to the backup file or null if failed</returns>
    string? CreateBackup(string pluginId, string assemblyPath);

    /// <summary>
    /// Restores a plugin from backup
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="backupPath">Path to the backup file</param>
    /// <returns>True if restore was successful</returns>
    Task<bool> RestoreFromBackupAsync(string pluginId, string backupPath);

    /// <summary>
    /// Gets a list of available backups for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>List of backup file paths</returns>
    IEnumerable<string> GetBackups(string pluginId);

    /// <summary>
    /// Event raised when a plugin is about to be reloaded
    /// </summary>
    event EventHandler<HotReloadEventArgs>? PluginReloading;

    /// <summary>
    /// Event raised when a plugin has been reloaded
    /// </summary>
    event EventHandler<HotReloadEventArgs>? PluginReloaded;

    /// <summary>
    /// Event raised when a plugin file change is detected
    /// </summary>
    event EventHandler<HotReloadEventArgs>? FileChanged;

    /// <summary>
    /// Event raised when hot reload fails
    /// </summary>
    event EventHandler<HotReloadEventArgs>? ReloadFailed;
}

/// <summary>
/// Result of a hot reload operation
/// </summary>
public class HotReloadResult
{
    /// <summary>
    /// Whether the reload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The old plugin instance (if available)
    /// </summary>
    public IPlugin? OldPlugin { get; set; }

    /// <summary>
    /// The new plugin instance
    /// </summary>
    public IPlugin? NewPlugin { get; set; }

    /// <summary>
    /// Old plugin version
    /// </summary>
    public string OldVersion { get; set; } = string.Empty;

    /// <summary>
    /// New plugin version
    /// </summary>
    public string NewVersion { get; set; } = string.Empty;

    /// <summary>
    /// Whether state was saved
    /// </summary>
    public bool StateSaved { get; set; }

    /// <summary>
    /// Whether state was restored
    /// </summary>
    public bool StateRestored { get; set; }

    /// <summary>
    /// Error message if reload failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of the reload operation
    /// </summary>
    public TimeSpan Duration { get; set; }
}
