using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Lib.Plugins;

public sealed record PluginOperationOutcome(
    bool Success,
    bool Degraded = false,
    bool UnloadPending = false,
    string? RecoveryId = null,
    string? RecoveryPath = null,
    string? Error = null);

public sealed record PluginScanOutcome(
    bool Success,
    bool Degraded,
    bool UnloadPending,
    int RegisteredCount,
    IReadOnlyList<PluginOperationOutcome> Failures);

public sealed class PluginOperationRecoveryException(
    string message,
    string? recoveryId,
    string? recoveryPath,
    bool unloadPending,
    Exception? innerException = null) : Exception(message, innerException)
{
    public string? RecoveryId { get; } = recoveryId;
    public string? RecoveryPath { get; } = recoveryPath;
    public bool UnloadPending { get; } = unloadPending;
}

/// <summary>
/// Immutable per-plugin installation marker token used for transaction rollback.
/// </summary>
public sealed class PluginInstallationStateSnapshot
{
    /// <summary>
    /// Creates a per-plugin installation marker snapshot.
    /// </summary>
    public PluginInstallationStateSnapshot(
        string pluginId,
        bool wasInstalled,
        bool wasPendingDeletion)
        : this(
            pluginId,
            wasInstalled ? pluginId : null,
            installedIndex: -1,
            wasPendingDeletion ? pluginId : null,
            pendingDeletionIndex: -1)
    {
    }

    internal PluginInstallationStateSnapshot(
        string pluginId,
        string? installedMarker,
        int installedIndex,
        string? pendingDeletionMarker,
        int pendingDeletionIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        PluginId = pluginId;
        InstalledMarker = installedMarker;
        InstalledIndex = installedIndex;
        PendingDeletionMarker = pendingDeletionMarker;
        PendingDeletionIndex = pendingDeletionIndex;
    }

    /// <summary>
    /// Target plugin ID.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Whether the target plugin was marked installed before commit.
    /// </summary>
    public bool WasInstalled => InstalledMarker is not null;

    /// <summary>
    /// Whether the target plugin was pending deletion before commit.
    /// </summary>
    public bool WasPendingDeletion => PendingDeletionMarker is not null;

    internal string? InstalledMarker { get; }

    internal int InstalledIndex { get; }

    internal string? PendingDeletionMarker { get; }

    internal int PendingDeletionIndex { get; }
}

/// <summary>
/// Opaque snapshot of registered plugin runtimes used for narrow rollback.
/// </summary>
public sealed class PluginRuntimeSnapshot
{
    internal PluginRuntimeSnapshot(
        IReadOnlyDictionary<string, PluginRuntimeIdentity> identities)
    {
        Identities = identities;
    }

    internal IReadOnlyDictionary<string, PluginRuntimeIdentity> Identities { get; }
}

/// <summary>
/// Opaque set of runtime IDs affected by one replacement transaction.
/// </summary>
public sealed class PluginRuntimeReconciliation
{
    internal PluginRuntimeReconciliation(IEnumerable<string> affectedPluginIds)
    {
        AffectedPluginIds = new HashSet<string>(
            affectedPluginIds,
            StringComparer.OrdinalIgnoreCase);
    }

    internal IReadOnlySet<string> AffectedPluginIds { get; }
}

internal sealed class PluginRuntimeIdentity
{
    private readonly WeakReference<IPlugin> _pluginReference;

    internal PluginRuntimeIdentity(
        IPlugin plugin,
        string? filePath,
        bool wasStarted,
        Guid runtimeGeneration,
        string? assemblySha256)
    {
        _pluginReference = new WeakReference<IPlugin>(plugin);
        FilePath = filePath;
        WasStarted = wasStarted;
        RuntimeGeneration = runtimeGeneration;
        AssemblySha256 = assemblySha256;
    }

    internal PluginRuntimeIdentity(
        IPlugin plugin,
        string? filePath,
        bool wasStarted)
        : this(plugin, filePath, wasStarted, Guid.NewGuid(), null)
    {
    }

    internal string? FilePath { get; }

    internal bool WasStarted { get; }

    internal Guid RuntimeGeneration { get; }

    internal string? AssemblySha256 { get; }

    internal bool IsSameInstance(IPlugin? plugin) =>
        plugin is not null &&
        _pluginReference.TryGetTarget(out var captured) &&
        ReferenceEquals(captured, plugin);
}

/// <summary>
/// Plugin manager interface
/// </summary>
public interface IPluginManager : IDisposable
{
    /// <summary>
    /// Plugin state changed event
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginStateChanged;

    /// <summary>
    /// Get all registered plugins
    /// </summary>
    IEnumerable<IPlugin> GetRegisteredPlugins();

    /// <summary>
    /// Get plugin metadata
    /// </summary>
    PluginMetadata? GetPluginMetadata(string pluginId);

    /// <summary>
    /// Check if plugin is installed
    /// </summary>
    bool IsInstalled(string pluginId);

    /// <summary>
    /// Install plugin
    /// </summary>
    void InstallPlugin(string pluginId);

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    bool UninstallPlugin(string pluginId);

    /// <summary>
    /// Atomically captures and commits one plugin's installation markers.
    /// </summary>
    PluginInstallationStateSnapshot CommitPluginInstallationState(
        string pluginId,
        IDisposable? mutationLease = null);

    /// <summary>
    /// Commits installation markers and lifecycle callbacks as one visible operation.
    /// </summary>
    PluginInstallationStateSnapshot CommitPluginInstallation(
        string pluginId,
        IDisposable? mutationLease = null,
        Action? coordinatedStateCommit = null);

    /// <summary>
    /// Restores only the token's plugin markers without uninstall side effects.
    /// </summary>
    void RestorePluginInstallationState(
        PluginInstallationStateSnapshot snapshot,
        IDisposable? mutationLease = null);

    /// <summary>
    /// Uninstall retired plugins that were removed from the catalog (e.g. migrated to built-in features).
    /// </summary>
    void PruneRetiredPlugins();

    /// <summary>
    /// Get all installed plugin IDs
    /// </summary>
    IEnumerable<string> GetInstalledPluginIds();

    /// <summary>
    /// Scan and load plugins from the plugins directory
    /// </summary>
    /// <returns>A task that completes when the scan is finished</returns>
    Task ScanAndLoadPluginsAsync(bool forceRefresh = false);

    Task<PluginScanOutcome> ScanAndLoadPluginsWithOutcomeAsync(bool forceRefresh = false);

    int RecoverDiscardedPluginCandidates();

    /// <summary>
    /// Captures registered runtime identities before a replacement scan.
    /// </summary>
    PluginRuntimeSnapshot CapturePluginRuntimeSnapshot();

    /// <summary>
    /// Strictly scans, verifies, and starts one exact plugin runtime.
    /// </summary>
    Task ActivatePluginRuntimeStrictAsync(
        string pluginId,
        string expectedMainDllPath,
        IDisposable? mutationLease = null,
        PluginPackageAuthorization? packageAuthorization = null);

    Task LoadPluginRuntimeStrictAsync(
        string pluginId,
        string expectedMainDllPath,
        IDisposable? mutationLease = null,
        PluginPackageAuthorization? packageAuthorization = null);

    void PreparePluginInstallation(string pluginId, IDisposable? mutationLease = null);

    void RollbackPreparedPluginInstallation(string pluginId, IDisposable? mutationLease = null);

    /// <summary>
    /// Removes every new or changed runtime sourced from a replacement directory.
    /// </summary>
    PluginRuntimeReconciliation ReconcilePluginRuntimes(
        PluginRuntimeSnapshot baseline,
        string replacementDirectory,
        IDisposable? mutationLease = null,
        string? expectedPluginId = null);

    void RestorePluginRuntimeSnapshot(
        PluginRuntimeSnapshot baseline,
        IDisposable? mutationLease = null,
        PluginRuntimeReconciliation? reconciliation = null);

    /// <summary>
    /// Permanently delete plugin files from disk asynchronously
    /// </summary>
    Task<bool> PermanentlyDeletePluginAsync(string pluginId);

    /// <summary>
    /// Unload all plugins and release references (useful before plugin updates)
    /// </summary>
    void UnloadAllPlugins();

    /// <summary>
    /// Stop a specific plugin (call its Stop method) before update or uninstallation
    /// </summary>
    bool StopPlugin(string pluginId);

    /// <summary>
    /// Stop all plugins (call Stop method for each plugin)
    /// </summary>
    void StopAllPlugins();

    /// <summary>
    /// Try to get a plugin by ID
    /// </summary>
    /// <param name="pluginId">The plugin ID</param>
    /// <param name="plugin">The plugin instance if found</param>
    /// <returns>True if the plugin was found</returns>
    bool TryGetPlugin(string pluginId, out IPlugin? plugin);

    /// <summary>
    /// Forgets a loaded plugin runtime without changing markers or firing callbacks.
    /// </summary>
    bool ForgetPluginRuntime(string pluginId, IDisposable? mutationLease = null);

    /// <summary>
    /// Reports whether a collectible runtime is active, pending unload, or no longer tracked.
    /// </summary>
    PluginRuntimeUnloadState GetPluginRuntimeUnloadState(string pluginId);

    /// <summary>
    /// Acquires the shared per-plugin mutation lease.
    /// </summary>
    IDisposable AcquirePluginMutation(string pluginId);
    
    /// <summary>
    /// Perform pending plugin deletions asynchronously
    /// </summary>
    Task PerformPendingDeletionsAsync();
    
    /// <summary>
    /// Check if all plugin dependencies are satisfied
    /// </summary>
    bool CheckDependencies(string pluginId, out List<string> missingDependencies);

    /// <summary>
    /// Check for plugin updates (returns a dictionary of pluginId -> availableVersion)
    /// </summary>
    Task<Dictionary<string, string>> CheckForUpdatesAsync();
}

/// <summary>
/// Plugin event arguments
/// </summary>
public class PluginEventArgs : EventArgs
{
    public string PluginId { get; }
    public bool IsInstalled { get; }

    public PluginEventArgs(string pluginId, bool isInstalled)
    {
        PluginId = pluginId;
        IsInstalled = isInstalled;
    }
}
