using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin manager implementation - coordinates plugin lifecycle operations
/// Delegates loading, registration, and file operations to specialized components
/// </summary>
public class PluginManager : IPluginManager
{
    private static readonly string[] RetiredPluginIds = ["network-acceleration"];

#if WINDOWS
    private readonly ApplicationSettings _applicationSettings;
#else
    private readonly PluginStateStore _pluginState;
#endif
    private readonly IPluginSignatureValidator _signatureValidator;
    private readonly IPluginLoader _loader;
    private readonly IPluginRegistry _registry;
    private readonly IPluginFileSystemManager _fileSystemManager;
    private readonly PluginLifecycleStateMachine _stateMachine = new();
    private readonly Dictionary<string, PluginState> _pluginStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _stateLock = new();
    private readonly object _installationMarkerLock = new();
    private readonly HashSet<string> _rejectedAssemblyPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _rejectedAssemblyLock = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _pluginMutationGates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly AsyncLocal<HashSet<string>?> _heldPluginMutations = new();
    private readonly ConcurrentDictionary<string, PluginFileIdentity> _loadedPluginFileIdentities =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Guid> _runtimeGenerations =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PreparedPluginInstallation> _preparedInstallations =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PendingUninstallTransaction> _pendingUninstallTransactions =
        new(StringComparer.OrdinalIgnoreCase);
    private ResolveEventHandler? _assemblyResolveHandler;
    private bool _disposed;
    internal Action? SynchronizeStateStoreOverride { get; set; }

    private sealed record PreparedPluginInstallation(
        IReadOnlyList<string> CallbackPluginIds,
        IReadOnlyList<IDisposable> DependencyMutationLeases);

    private sealed class PendingUninstallTransaction(
        string pluginId,
        PluginRuntimeSnapshot runtimeBaseline,
        PluginInstallationStateSnapshot markerSnapshot,
        string? trustSnapshot,
        PluginState lifecycleState,
        bool wasStarted)
    {
        public string PluginId { get; } = pluginId;
        public PluginRuntimeSnapshot RuntimeBaseline { get; } = runtimeBaseline;
        public PluginInstallationStateSnapshot MarkerSnapshot { get; } = markerSnapshot;
        public string? TrustSnapshot { get; } = trustSnapshot;
        public PluginState LifecycleState { get; } = lifecycleState;
        public bool WasStarted { get; } = wasStarted;
        public bool UninstallCallbackRan { get; set; }
    }

    public event EventHandler<PluginEventArgs>? PluginStateChanged;

    /// <summary>
    /// Raised when a plugin's <see cref="PluginState"/> transitions through the
    /// lifecycle state machine. This is the rich counterpart of
    /// <see cref="PluginStateChanged"/> and is intended for hosts/UI that want
    /// full before/after state information; existing consumers can keep
    /// subscribing to <see cref="PluginStateChanged"/>.
    /// </summary>
    public event EventHandler<PluginStateChangedEventArgs>? LifecycleStateChanged;

    public PluginManager(
#if WINDOWS
        ApplicationSettings applicationSettings,
#else
        PluginStateStore pluginState,
#endif
        IPluginSignatureValidator signatureValidator,
        IPluginLoader loader,
        IPluginRegistry registry,
        IPluginFileSystemManager fileSystemManager)
    {
#if WINDOWS
        _applicationSettings = applicationSettings;
#else
        _pluginState = pluginState;
#endif
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _fileSystemManager = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
    }

#if WINDOWS
    private ApplicationSettings.ApplicationSettingsStore StateStore => _applicationSettings.Store;
#else
    private PluginStateStore.PluginStateStoreData StateStore => _pluginState.Store;
#endif

    private void SynchronizeStateStore()
    {
        if (SynchronizeStateStoreOverride is not null)
        {
            SynchronizeStateStoreOverride();
            return;
        }
#if WINDOWS
        _applicationSettings.SynchronizeStore();
#else
        _pluginState.SynchronizeStore();
#endif
    }

    /// <summary>
    /// Read the cached lifecycle state for a plugin. Returns
    /// <see cref="PluginState.NotInstalled"/> when no state has been recorded
    /// yet (the natural default for a never-installed plugin).
    /// </summary>
    private PluginState GetPluginState(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return PluginState.NotInstalled;

        lock (_stateLock)
        {
            return _pluginStates.TryGetValue(pluginId, out var state) ? state : PluginState.NotInstalled;
        }
    }

    /// <summary>
    /// Persist the cached lifecycle state for a plugin. Intended to be called
    /// from <see cref="TransitionLifecycleState"/> after the state machine has
    /// approved a transition.
    /// </summary>
    private void SetPluginState(string pluginId, PluginState state)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        lock (_stateLock)
        {
            _pluginStates[pluginId] = state;
        }
    }

    /// <summary>
    /// Drive a plugin through the <see cref="PluginLifecycleStateMachine"/>.
    /// Validates the transition, logs rejected attempts, and on success
    /// updates the cached state and raises the lifecycle events. The
    /// <paramref name="legacyIsInstalled"/> parameter is forwarded to the
    /// existing <see cref="PluginStateChanged"/> event for backward
    /// compatibility with subscribers that only care about the install flag.
    /// </summary>
    /// <returns><c>true</c> when the transition was applied.</returns>
    private bool TransitionLifecycleState(string pluginId, PluginState targetState, bool legacyIsInstalled)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        var oldState = GetPluginState(pluginId);
        var result = _stateMachine.ValidateAndLog(pluginId, oldState, targetState);
        if (!result.IsAllowed)
            return false;

        SetPluginState(pluginId, targetState);

        LifecycleStateChanged?.Invoke(this, new PluginStateChangedEventArgs(pluginId, oldState, targetState));
        OnPluginStateChanged(pluginId, legacyIsInstalled);
        return true;
    }

    private bool TransitionLifecycleStatePostCommit(
        string pluginId,
        PluginState targetState,
        bool legacyIsInstalled)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        var oldState = GetPluginState(pluginId);
        var result = _stateMachine.ValidateAndLog(pluginId, oldState, targetState);
        if (!result.IsAllowed)
        {
            Log.Instance.Warning(
                $"Committed plugin state {pluginId} could not transition from {oldState} to {targetState}.");
            return false;
        }

        SetPluginState(pluginId, targetState);
        PublishLifecycleStateChangedPostCommit(
            new PluginStateChangedEventArgs(pluginId, oldState, targetState));
        PublishPluginStateChangedPostCommit(
            new PluginEventArgs(pluginId, legacyIsInstalled));
        return true;
    }

    private void PublishLifecycleStateChangedPostCommit(PluginStateChangedEventArgs args)
    {
        var subscribers = LifecycleStateChanged?.GetInvocationList();
        if (subscribers is null)
            return;
        foreach (var subscriber in subscribers)
        {
            try
            {
                ((EventHandler<PluginStateChangedEventArgs>)subscriber)(this, args);
            }
            catch (Exception ex)
            {
                Log.Instance.Error(
                    $"A post-commit lifecycle subscriber failed for plugin {args.PluginId}.",
                    ex);
            }
        }
    }

    private void PublishPluginStateChangedPostCommit(PluginEventArgs args)
    {
        var subscribers = PluginStateChanged?.GetInvocationList();
        if (subscribers is null)
            return;
        foreach (var subscriber in subscribers)
        {
            try
            {
                ((EventHandler<PluginEventArgs>)subscriber)(this, args);
            }
            catch (Exception ex)
            {
                Log.Instance.Error(
                    $"A post-commit plugin notification subscriber failed for {args.PluginId}.",
                    ex);
            }
        }
    }

    /// <summary>
    /// Scan and load plugins from the plugins directory
    /// </summary>
    public async Task ScanAndLoadPluginsAsync(bool forceRefresh = false) =>
        _ = await ScanAndLoadPluginsWithOutcomeAsync(forceRefresh).ConfigureAwait(false);

    public async Task<PluginScanOutcome> ScanAndLoadPluginsWithOutcomeAsync(
        bool forceRefresh = false)
    {
        var failures = new List<PluginOperationOutcome>();
        try
        {
            SweepDiscardedPluginCandidates();
            var pluginsDirectory = _fileSystemManager.GetPluginsDirectory();
            if (!Directory.Exists(pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugins directory does not exist: {pluginsDirectory}");
                return new PluginScanOutcome(true, false, false, _registry.Count, failures);
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Scanning plugins directory: {pluginsDirectory}");

            RegisterAssemblyResolver(pluginsDirectory);

            if (forceRefresh)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Clearing plugin file cache before forced scan.");

                _fileSystemManager.ClearFileCache();
            }

            var pluginFiles = _fileSystemManager.GetPluginDllFiles();

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    var expectedRuntimeId = GetExpectedRuntimeId(pluginFile);
                    if (string.IsNullOrWhiteSpace(expectedRuntimeId))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Skipping plugin candidate with no safe filename identity: {pluginFile}");
                        failures.Add(new PluginOperationOutcome(
                            false,
                            Error: $"Plugin candidate has no safe filename identity: {pluginFile}"));
                        continue;
                    }

                    using var mutation = AcquirePluginMutation(expectedRuntimeId);
                    if (ShouldReuseRegisteredRuntime(pluginFile, forceRefresh, mutation))
                        continue;
                    await LoadPluginFromFileAsync(pluginFile, mutation).ConfigureAwait(false);
                    if (!_registry.IsRegistered(expectedRuntimeId))
                    {
                        failures.Add(new PluginOperationOutcome(
                            false,
                            RecoveryId: expectedRuntimeId,
                            Error: $"Plugin candidate did not register a runtime: {pluginFile}"));
                    }
                }
                catch (Exception ex)
                {
                    var expectedRuntimeId = GetExpectedRuntimeId(pluginFile);
                    var unloadPending =
                        !string.IsNullOrWhiteSpace(expectedRuntimeId) &&
                        GetPluginRuntimeUnloadState(expectedRuntimeId) ==
                        PluginRuntimeUnloadState.UnloadRequested;
                    failures.Add(new PluginOperationOutcome(
                        false,
                        Degraded: unloadPending ||
                                  ex is PluginLoadContextUnloadPendingException ||
                                  ex.Message.Contains(
                                      "pending collectible runtime unload",
                                      StringComparison.OrdinalIgnoreCase),
                        UnloadPending: unloadPending,
                        RecoveryId: expectedRuntimeId,
                        RecoveryPath: unloadPending ? pluginFile : null,
                        Error: ex.Message));
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to load plugin from {pluginFile}: {ex.Message}", ex);
                }
            }

            var discardedPending = SweepDiscardedPluginCandidates();
            if (discardedPending > 0)
            {
                failures.Add(new PluginOperationOutcome(
                    false,
                    Degraded: true,
                    UnloadPending: true,
                    RecoveryId: $"discarded:{discardedPending}",
                    Error: $"{discardedPending} discarded plugin load contexts remain pending collection."));
            }
            StartInstalledPlugins();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin scan completed. Total registered plugins: {_registry.Count}");
            var degraded = failures.Any(failure => failure.Degraded);
            return new PluginScanOutcome(
                failures.Count == 0,
                degraded,
                failures.Any(failure => failure.UnloadPending),
                _registry.Count,
                failures);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error scanning plugins directory: {ex.Message}", ex);
            failures.Add(new PluginOperationOutcome(
                false,
                Degraded: true,
                Error: ex.Message));
            return new PluginScanOutcome(
                false,
                true,
                failures.Any(failure => failure.UnloadPending),
                _registry.Count,
                failures);
        }
    }

    private int SweepDiscardedPluginCandidates()
    {
        if (_loader is not ITransactionalPluginLoader transactionalLoader)
            return 0;
        return transactionalLoader.SweepDiscardedCandidates().Pending;
    }

    public int RecoverDiscardedPluginCandidates()
    {
        if (_loader is not ITransactionalPluginLoader transactionalLoader)
            return 0;
        return transactionalLoader.RecoverDiscardedCandidates().Pending;
    }

    private bool ShouldReuseRegisteredRuntime(
        string pluginFilePath,
        bool forceRefresh,
        IDisposable mutationLease)
    {
        var canonicalPath = Path.GetFullPath(pluginFilePath);
        var expectedRuntimeId = GetExpectedRuntimeId(canonicalPath);
        if (!string.IsNullOrWhiteSpace(expectedRuntimeId) &&
            _pendingUninstallTransactions.ContainsKey(expectedRuntimeId))
        {
            throw new InvalidOperationException(
                $"Plugin {expectedRuntimeId} has an unfinished uninstall transaction.");
        }

        if (!string.IsNullOrWhiteSpace(expectedRuntimeId) &&
            GetPluginRuntimeUnloadState(expectedRuntimeId) ==
            PluginRuntimeUnloadState.UnloadRequested)
        {
            if (!ForgetPluginRuntime(expectedRuntimeId, mutationLease))
            {
                throw new InvalidOperationException(
                    $"Plugin {expectedRuntimeId} is still pending collectible runtime unload.");
            }
        }

        var metadata = _registry.GetAllMetadata().ToArray();
        var samePath = metadata.FirstOrDefault(candidate =>
            !string.IsNullOrWhiteSpace(candidate.FilePath) &&
            Path.GetFullPath(candidate.FilePath).Equals(
                canonicalPath,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal));
        if (samePath is not null && _registry.IsRegistered(samePath.Id))
        {
            var currentIdentity = CapturePluginFileIdentity(canonicalPath);
            if (!forceRefresh ||
                _loadedPluginFileIdentities.TryGetValue(samePath.Id, out var loadedIdentity) &&
                currentIdentity == loadedIdentity)
            {
                return true;
            }

            if (!ForgetPluginRuntime(samePath.Id, mutationLease))
                throw new InvalidOperationException(
                    $"Could not unload plugin {samePath.Id} before force refresh.");
            return false;
        }

        var assemblyToken = PluginAssemblyNaming.ExtractPluginIdFromAssemblyFileName(
            Path.GetFileNameWithoutExtension(canonicalPath));
        if (string.IsNullOrWhiteSpace(assemblyToken))
            return false;

        var canonicalToken = NormalizePluginIdentityToken(assemblyToken);
        return metadata.Any(candidate =>
            _registry.IsRegistered(candidate.Id) &&
            NormalizePluginIdentityToken(candidate.Id).Equals(
                canonicalToken,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePluginIdentityToken(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string? GetExpectedRuntimeId(string pluginFilePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(pluginFilePath);
        var id = PluginAssemblyNaming.ExtractPluginIdFromAssemblyFileName(fileName) ?? fileName;
        return PathSecurity.IsValidPluginId(id) ? id : null;
    }

    private void RegisterAssemblyResolver(string pluginsDirectory)
    {
        if (_assemblyResolveHandler != null)
            return;

        _assemblyResolveHandler = (sender, args) =>
        {
            try
            {
                var requestedAssemblyName = new AssemblyName(args.Name);
                var assemblyName = requestedAssemblyName.Name;
                if (string.IsNullOrWhiteSpace(assemblyName))
                    return null;

                if (!IsSafeAssemblyName(assemblyName))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Rejected assembly resolve request with unsafe name: {assemblyName}");
                    return null;
                }
                
                var isSatelliteAssembly = requestedAssemblyName.CultureInfo != null && 
                                          !string.IsNullOrEmpty(requestedAssemblyName.CultureInfo.Name) &&
                                          assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase);
                
                if (isSatelliteAssembly)
                {
                    return ResolveSatelliteAssembly(pluginsDirectory, requestedAssemblyName, assemblyName);
                }
                
                return ResolveDependencyAssembly(pluginsDirectory, assemblyName);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to resolve assembly: {args.Name}, {ex.Message}", ex);
            }
            return null;
        };

        AppDomain.CurrentDomain.AssemblyResolve += _assemblyResolveHandler;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_assemblyResolveHandler != null)
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= _assemblyResolveHandler;
                    _assemblyResolveHandler = null;
                }
            }
            _disposed = true;
        }
    }

    private Assembly? ResolveSatelliteAssembly(string pluginsDirectory, AssemblyName requestedAssemblyName, string assemblyName)
    {
        var cultureName = requestedAssemblyName.CultureInfo!.Name;
        var baseName = assemblyName.Substring(0, assemblyName.Length - ".resources".Length);

        var subdirectories = Directory.GetDirectories(pluginsDirectory);
        var cultureFolders = _fileSystemManager.GetCultureFolders();

        foreach (var subdir in subdirectories)
        {
            var dirName = Path.GetFileName(subdir);
            if (cultureFolders.Contains(dirName))
                continue;

            var satellitePath = Path.Combine(subdir, cultureName, $"{baseName}.resources.dll");
            if (File.Exists(satellitePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Resolving satellite assembly: {requestedAssemblyName.Name} from {satellitePath}");
                return TryLoadTrustedPluginAssembly(satellitePath, pluginsDirectory, $"satellite assembly {requestedAssemblyName.Name}");
            }
        }
        return null;
    }

    private Assembly? ResolveDependencyAssembly(string pluginsDirectory, string assemblyName)
    {
        var dllPath = Path.Combine(pluginsDirectory, $"{assemblyName}.dll");
        if (File.Exists(dllPath))
            return TryLoadTrustedPluginAssembly(dllPath, pluginsDirectory, $"dependency assembly {assemblyName}");

        var subdirectories = Directory.GetDirectories(pluginsDirectory);
        foreach (var subdir in subdirectories)
        {
            var subDirDllPath = Path.Combine(subdir, $"{assemblyName}.dll");
            if (File.Exists(subDirDllPath))
                return TryLoadTrustedPluginAssembly(subDirDllPath, pluginsDirectory, $"dependency assembly {assemblyName}");
        }

        var appBaseDirectory = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
        var appBaseDllPath = Path.Combine(appBaseDirectory, $"{assemblyName}.dll");
        if (File.Exists(appBaseDllPath))
            return TryLoadTrustedPluginAssembly(appBaseDllPath, appBaseDirectory, $"app-base dependency {assemblyName}");

        // Check SDK directory relative to plugins directory
        var sdkDirectory = Path.GetFullPath(Path.Combine(pluginsDirectory, "..", "SDK"));
        var sdkPath = Path.GetFullPath(Path.Combine(sdkDirectory, $"{assemblyName}.dll"));
        if (File.Exists(sdkPath))
            return TryLoadTrustedPluginAssembly(sdkPath, sdkDirectory, $"SDK assembly {assemblyName}");

        // For SDK assembly, try to resolve from already loaded assemblies
        // The SDK types forward to UniversalDeviceToolkit.Lib which is already loaded
        var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);
        if (loadedAssembly != null)
            return loadedAssembly;

        return null;
    }

    private Assembly? TryLoadTrustedPluginAssembly(string candidatePath, string pluginsDirectory, string context)
    {
        try
        {
            var normalizedCandidatePath = Path.GetFullPath(candidatePath);

            lock (_rejectedAssemblyLock)
            {
                if (_rejectedAssemblyPaths.Contains(normalizedCandidatePath))
                    return null;
            }

            var normalizedPluginsDirectory = Path.GetFullPath(pluginsDirectory);

            if (!IsPathWithinDirectory(normalizedCandidatePath, normalizedPluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rejected {context} outside plugins directory: {normalizedCandidatePath}");
                return null;
            }

            // The CLR AppDomain.AssemblyResolve event requires a synchronous
            // return: the runtime needs the resolved Assembly instance on the
            // calling stack to continue loading the dependent assembly. We
            // therefore MUST block here, which means we accept the standard
            // sync-over-async risk on this specific code path. The risk is
            // bounded because:
            //   1. AssemblyResolve is invoked from the JIT thread doing
            //      LoadFrom() on a plugin we already vetted as in-plugins-dir;
            //      there is no SynchronizationContext or Dispatcher captured
            //      on that thread, so we cannot deadlock against a UI pump.
            //   2. The signature validator is pure-async I/O and only awaits
            //      internal tasks; it does not post continuations to a
            //      captured context.
            //   3. The outer call site is LoadPluginFromFileAsync, which runs
            //      on a worker task (.ConfigureAwait(false) upstream), so
            //      there is no UI dispatcher to deadlock against either.
            // If any of those assumptions change, this call must be replaced
            // with a pre-load/cache strategy rather than a blocking wait.
            var signatureResult = _signatureValidator.ValidateAsync(normalizedCandidatePath).GetAwaiter().GetResult();
            if (!signatureResult.IsValid)
            {
                lock (_rejectedAssemblyLock)
                    _rejectedAssemblyPaths.Add(normalizedCandidatePath);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rejected {context} due to invalid signature. [path={normalizedCandidatePath}, status={signatureResult.Status}, error={signatureResult.ErrorMessage}]");
                return null;
            }

            return Assembly.LoadFrom(normalizedCandidatePath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to resolve trusted {context} from {candidatePath}: {ex.Message}", ex);
            return null;
        }
    }

    private static bool IsPathWithinDirectory(string path, string directoryPath)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = EnsureTrailingSeparator(Path.GetFullPath(directoryPath));
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            return path;

        return path + Path.DirectorySeparatorChar;
    }

    private static bool IsSafeAssemblyName(string assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            return false;

        foreach (var c in assemblyName)
        {
            if (!(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Load a plugin from a DLL file
    /// </summary>
    private async Task LoadPluginFromFileAsync(
        string pluginFilePath,
        IDisposable? mutationLease = null,
        PluginPackageAuthorization? packageAuthorization = null)
    {
        PluginCandidateUnloadToken discardedToken = default;
        try
        {
            await LoadPluginFromFileCoreAsync(
                    pluginFilePath,
                    mutationLease,
                    packageAuthorization)
                .ConfigureAwait(false);
            return;
        }
        catch (PluginCandidateDiscardedException discarded)
        {
            discardedToken = discarded.Token;
        }

        // Leave the faulted core state-machine/catch frame before forcing collection;
        // either can otherwise keep the candidate instance and its ALC alive.
        await Task.Yield();
        if (_loader is ITransactionalPluginLoader transactionalLoader &&
            transactionalLoader.ConfirmDiscardedCandidate(discardedToken))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Discarded plugin candidate from {pluginFilePath} is still pending collectible runtime unload.");
    }

    private async Task LoadPluginFromFileCoreAsync(
        string pluginFilePath,
        IDisposable? mutationLease,
        PluginPackageAuthorization? packageAuthorization)
    {
        if (string.IsNullOrWhiteSpace(pluginFilePath))
        {
            Log.Instance.Warning("LoadPluginFromFileAsync: Plugin file path is empty");
            return;
        }

        IPlugin? loadedCandidate = null;
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Attempting to load plugin from: {pluginFilePath}");

            var pluginsDirectory = _fileSystemManager.GetPluginsDirectory();
            if (!PathSecurity.IsPathWithinAllowedDirectory(pluginFilePath, pluginsDirectory))
            {
                Log.Instance.Warning($"SECURITY: Plugin file path is outside allowed directory: {pluginFilePath}");
                return;
            }

            // SECURITY: Validate the file name itself
            var fileName = Path.GetFileName(pluginFilePath);
            if (!PathSecurity.IsValidFileName(fileName))
            {
                Log.Instance.Warning($"SECURITY: Invalid plugin file name: {fileName}");
                return;
            }

            // Check if file exists
            if (!File.Exists(pluginFilePath))
            {
                Log.Instance.Warning($"Plugin file not found: {pluginFilePath}");
                return;
            }

            var effectiveSignatureValidator =
                packageAuthorization?.Scope(_signatureValidator) ?? _signatureValidator;

            // Validate plugin signature before loading (security check)
            var signatureResult = await effectiveSignatureValidator
                .ValidateAsync(pluginFilePath)
                .ConfigureAwait(false);
            if (!signatureResult.IsValid)
            {
                Log.Instance.Warning($"Plugin signature validation failed for {pluginFilePath}. Status: {signatureResult.Status}, Error: {signatureResult.ErrorMessage}");
                return;
            }

            // Use the plugin loader to load the plugin
            IPlugin? plugin;
            try
            {
                plugin = await _loader
                    .LoadFromFileAsync(pluginFilePath, effectiveSignatureValidator)
                    .ConfigureAwait(false);
            }
            catch (PluginLoadContextUnloadPendingException pendingUnload)
            {
                throw new PluginCandidateDiscardedException(pendingUnload.Token);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Error loading plugin assembly from {pluginFilePath}", ex);
                return;
            }

            if (plugin == null)
            {
                Log.Instance.Warning($"Plugin loader returned null for {pluginFilePath}");
                return;
            }
            loadedCandidate = plugin;

            // Validate plugin ID
            if (string.IsNullOrWhiteSpace(plugin.Id))
            {
                Log.Instance.Warning($"Plugin from {pluginFilePath} has invalid or empty ID");
                DiscardLoadedCandidate(plugin);
                return;
            }

            // SECURITY: Validate plugin ID format to prevent path traversal
            if (!PathSecurity.IsValidPluginId(plugin.Id))
            {
                Log.Instance.Warning($"SECURITY: Plugin from {pluginFilePath} has invalid or unsafe plugin ID: {plugin.Id}");
                DiscardLoadedCandidate(plugin);
                return;
            }

            var expectedRuntimeId = GetExpectedRuntimeId(pluginFilePath);
            if (string.IsNullOrWhiteSpace(expectedRuntimeId) ||
                !NormalizePluginIdentityToken(plugin.Id).Equals(
                    NormalizePluginIdentityToken(expectedRuntimeId),
                    StringComparison.OrdinalIgnoreCase))
            {
                Log.Instance.Warning(
                    $"Plugin runtime ID {plugin.Id} does not match candidate filename identity {expectedRuntimeId ?? "<invalid>"}.");
                DiscardLoadedCandidate(plugin);
                return;
            }

            // A candidate assembly context is private until this runtime-ID lease is held.
            // Lock order is candidate discovery -> runtime ID lease -> loader commit -> registry.
            using var runtimeMutation = EnterPluginMutation(plugin.Id, mutationLease);

            // Check if this plugin has the GetFeatureExtension method (SDK plugin)
            var pluginType = plugin.GetType();
            var hasGetFeatureExtension = pluginType.GetMethod("GetFeatureExtension",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null) != null;

            // Try to get PluginAttribute for version and compatibility info
            string pluginVersion = "1.0.0";
            string minimumHostVersion = "1.0.0";
            string? author = null;
            var pluginAttribute = pluginType.GetCustomAttributes(true)
                .FirstOrDefault(attr => attr.GetType().Name == "PluginAttribute");

            if (pluginAttribute != null)
            {
                try
                {
                    var versionProp = pluginAttribute.GetType().GetProperty("Version");
                    var minHostVersionProp = pluginAttribute.GetType().GetProperty("MinimumHostVersion");
                    var authorProp = pluginAttribute.GetType().GetProperty("Author");

                    if (versionProp != null)
                        pluginVersion = versionProp.GetValue(pluginAttribute)?.ToString() ?? "1.0.0";

                    if (minHostVersionProp != null)
                        minimumHostVersion = minHostVersionProp.GetValue(pluginAttribute)?.ToString() ?? "1.0.0";

                    if (authorProp != null)
                        author = authorProp.GetValue(pluginAttribute)?.ToString();
                }
                catch (Exception ex)
                {
                    Log.Instance.Warning($"Error reading plugin attributes for {plugin.Id}: {ex.Message}");
                }
            }

            // Check version compatibility
            if (!IsVersionCompatible(minimumHostVersion))
            {
                Log.Instance.Warning($"Plugin {pluginType.Name} requires host version {minimumHostVersion} or higher. Current host version is incompatible. Skipping.");
                DiscardLoadedCandidate(plugin);
                return;
            }

            // Store plugin metadata with version and compatibility info
            var metadata = new PluginMetadata
            {
                Id = plugin.Id,
                Name = plugin.Name,
                Description = plugin.Description,
                Icon = plugin.Icon,
                IsSystemPlugin = plugin.IsSystemPlugin,
                Dependencies = plugin.Dependencies,
                Version = pluginVersion,
                MinimumHostVersion = minimumHostVersion,
                Author = author,
                FilePath = pluginFilePath,
            };

            // Check for existing plugin with same ID
            var existingMetadata = _registry.GetMetadata(plugin.Id);
            if (existingMetadata != null)
            {
                var versionComparison = ComparePluginVersions(pluginVersion, existingMetadata.Version);
                if (versionComparison < 0)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Skipping plugin {plugin.Id} v{pluginVersion} from {pluginFilePath} because newer version {existingMetadata.Version} is already loaded from {existingMetadata.FilePath}.");
                    DiscardLoadedCandidate(plugin);
                    return;
                }

                if (versionComparison == 0 && _registry.IsRegistered(plugin.Id))
                {
                    var currentWriteTime = GetPluginFileWriteTimeUtc(pluginFilePath);
                    var existingWriteTime = GetPluginFileWriteTimeUtc(existingMetadata.FilePath);

                    if (currentWriteTime <= existingWriteTime)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Skipping duplicate plugin {plugin.Id} v{pluginVersion} from {pluginFilePath}; plugin already registered from {existingMetadata.FilePath}.");
                        DiscardLoadedCandidate(plugin);
                        return;
                    }

                    Log.Instance.Info($"Replacing duplicate plugin {plugin.Id} v{pluginVersion} with newer file {pluginFilePath} ({currentWriteTime:O}) over {existingMetadata.FilePath} ({existingWriteTime:O}).");
                }
            }

            // Publish the loader context only after duplicate arbitration. A rejected
            // candidate can therefore never overwrite the tracked context for another DLL.
            if (_loader is ITransactionalPluginLoader transactionalLoader &&
                !transactionalLoader.CommitCandidate(plugin))
            {
                Log.Instance.Warning(
                    $"Plugin runtime {plugin.Id} already has a committed loader context; discarded {pluginFilePath}.");
                DiscardLoadedCandidate(plugin);
                return;
            }
            loadedCandidate = null;

            try
            {
                _registry.Register(plugin, metadata);
                _runtimeGenerations[plugin.Id] = Guid.NewGuid();
            }
            catch
            {
                _registry.Forget(plugin.Id);
                RequestLoaderUnload(plugin.Id);
                throw;
            }
            _loadedPluginFileIdentities[plugin.Id] = CapturePluginFileIdentity(pluginFilePath);
            _fileSystemManager.UpdateFileCache(pluginFilePath);

            var pluginTypeInfo = hasGetFeatureExtension ? "SDK" : "direct";
            Log.Instance.Info($"Successfully loaded {pluginTypeInfo} plugin: {plugin.Id} ({plugin.Name}) v{pluginVersion} (MinHost: {minimumHostVersion}) from {pluginFilePath}");
            plugin = null;
        }
        catch (PluginCandidateDiscardedException)
        {
            loadedCandidate = null;
            throw;
        }
        catch (Exception ex)
        {
            if (loadedCandidate is not null)
                DiscardLoadedCandidate(loadedCandidate);
            Log.Instance.Error($"Failed to load plugin from {pluginFilePath}", ex);
        }
    }

    private void DiscardLoadedCandidate(IPlugin plugin)
    {
        if (_loader is ITransactionalPluginLoader transactionalLoader)
        {
            var token = transactionalLoader.DiscardCandidate(plugin);
            throw new PluginCandidateDiscardedException(token);
        }
        else
            RequestLoaderUnload(plugin.Id);
    }

    private sealed class PluginCandidateDiscardedException(
        PluginCandidateUnloadToken token) : Exception
    {
        public PluginCandidateUnloadToken Token { get; } = token;
    }

    private static DateTime GetPluginFileWriteTimeUtc(string? filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return DateTime.MinValue;
            return File.GetLastWriteTimeUtc(filePath);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-manager-write-time",
                $"Failed to read plugin file write time: {filePath}",
                ex);
            return DateTime.MinValue;
        }
    }

    private static PluginFileIdentity CapturePluginFileIdentity(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        return new PluginFileIdentity(
            fileInfo.Length,
            fileInfo.LastWriteTimeUtc,
            Convert.ToHexString(SHA256.HashData(stream)));
    }

    private readonly record struct PluginFileIdentity(
        long Length,
        DateTime LastWriteTimeUtc,
        string Sha256);

    public void StartInstalledPlugins()
    {
        try
        {
            foreach (var plugin in _registry.GetAll())
            {
                using var mutation = AcquirePluginMutation(plugin.Id);
                if (!IsInstalled(plugin.Id))
                    continue;

                if (!_registry.MarkStarted(plugin.Id))
                    continue;

                try
                {
                    if (plugin is IAppStartupPlugin startupPlugin)
                    {
                        // Ensure state machine sees the plugin as Installed
                        // before driving it to Enabled. We do not raise
                        // PluginStateChanged for the synthesized Installed
                        // transition because plugins discovered at startup
                        // were already installed by the previous session;
                        // raising the event here would be a duplicate of
                        // what the user already saw.
                        var current = GetPluginState(plugin.Id);
                        if (current == PluginState.NotInstalled)
                            _stateMachine.ValidateAndLog(plugin.Id, current, PluginState.Installed);
                        SetPluginState(plugin.Id, PluginState.Installed);

                        startupPlugin.OnAppStarted();
                        TransitionLifecycleState(plugin.Id, PluginState.Enabled, legacyIsInstalled: true);
                    }
                    else
                    {
                        _registry.MarkStopped(plugin.Id);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _registry.MarkStopped(plugin.Id);
                    TransitionLifecycleState(plugin.Id, PluginState.Error, legacyIsInstalled: true);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to start plugin {plugin.Id}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to start installed plugins: {ex.Message}", ex);
        }
    }

    public IEnumerable<IPlugin> GetRegisteredPlugins()
    {
        return _registry.GetAll();
    }

    /// <inheritdoc />
    public bool TryGetPlugin(string pluginId, out IPlugin? plugin)
    {
        plugin = _registry.Get(pluginId);
        return plugin != null;
    }

    public bool ForgetPluginRuntime(string pluginId, IDisposable? mutationLease = null)
    {
        if (!PathSecurity.IsValidPluginId(pluginId))
            throw new ArgumentException("Invalid plugin ID.", nameof(pluginId));

        using var mutation = EnterPluginMutation(pluginId, mutationLease);
        var unloadResult = RequestRuntimeUnloadAndReleaseRegistration(pluginId);
        if (unloadResult == RuntimeUnloadRequestResult.RefusedBeforeRequest)
            return false;

        if (unloadResult == RuntimeUnloadRequestResult.Pending &&
            !ConfirmReleasedRuntimeUnload(pluginId))
        {
            return false;
        }

        _loadedPluginFileIdentities.TryRemove(pluginId, out _);
        _runtimeGenerations.TryRemove(pluginId, out _);
        return true;
    }

    public PluginRuntimeUnloadState GetPluginRuntimeUnloadState(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return PluginRuntimeUnloadState.NotTracked;

        return _loader is IPluginRuntimeUnloadStateProvider stateProvider
            ? stateProvider.GetUnloadState(pluginId)
            : PluginRuntimeUnloadState.NotTracked;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RuntimeUnloadRequestResult RequestRuntimeUnloadAndReleaseRegistration(string pluginId)
    {
        var unloadState = GetPluginRuntimeUnloadState(pluginId);
        if (unloadState == PluginRuntimeUnloadState.UnloadRequested)
        {
            _registry.Forget(pluginId);
            return RuntimeUnloadRequestResult.Pending;
        }

        var plugin = _registry.Get(pluginId);
        if (plugin is null)
        {
            if (unloadState != PluginRuntimeUnloadState.Active)
                return RuntimeUnloadRequestResult.ConfirmedOrNotTracked;
            return IsUnloadRequestAccepted(RequestLoaderUnload(pluginId))
                ? RuntimeUnloadRequestResult.Pending
                : RuntimeUnloadRequestResult.RefusedBeforeRequest;
        }

        var wasStarted = _registry.IsStarted(pluginId);
        var metadata = _registry.GetMetadata(pluginId);
        try
        {
            if (wasStarted)
            {
                plugin.Stop();
                _registry.MarkStopped(pluginId);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to stop plugin {pluginId} before runtime replacement.",
                ex);
        }

        _registry.Forget(pluginId);
        PluginUnloadRequestResult requestResult;
        try
        {
            requestResult = RequestLoaderUnload(pluginId);
        }
        catch
        {
            if (metadata is not null)
                _registry.Register(plugin, metadata);
            if (wasStarted)
                RestoreStartedStateAfterUnloadRefusal(pluginId, plugin);
            throw;
        }

        if (IsUnloadRequestAccepted(requestResult))
            return RuntimeUnloadRequestResult.Pending;

        if (metadata is not null)
            _registry.Register(plugin, metadata);
        if (wasStarted)
            RestoreStartedStateAfterUnloadRefusal(pluginId, plugin);

        return RuntimeUnloadRequestResult.RefusedBeforeRequest;
    }

    private static bool IsUnloadRequestAccepted(PluginUnloadRequestResult result) =>
        result is PluginUnloadRequestResult.Requested or
            PluginUnloadRequestResult.AlreadyRequested;

    private PluginUnloadRequestResult RequestLoaderUnload(string pluginId)
    {
        var request = _loader.RequestUnload(pluginId);
        if (request != PluginUnloadRequestResult.NotTracked)
            return request;
        if (_loader.Unload(pluginId))
            return PluginUnloadRequestResult.AlreadyRequested;
        return GetPluginRuntimeUnloadState(pluginId) ==
               PluginRuntimeUnloadState.UnloadRequested
            ? PluginUnloadRequestResult.AlreadyRequested
            : PluginUnloadRequestResult.Failed;
    }

    private void RestoreStartedStateAfterUnloadRefusal(string pluginId, IPlugin plugin)
    {
        if (plugin is not IAppStartupPlugin startupPlugin)
        {
            throw new InvalidOperationException(
                $"Plugin {pluginId} stopped but its loader refused unload and the prior started state cannot be restored.");
        }

        try
        {
            startupPlugin.OnAppStarted();
            if (!_registry.MarkStarted(pluginId))
            {
                throw new InvalidOperationException(
                    $"Plugin {pluginId} restarted after unload refusal but its registry state could not be restored.");
            }
        }
        catch (Exception ex)
        {
            _registry.MarkStopped(pluginId);
            throw new InvalidOperationException(
                $"Plugin {pluginId} could not restore its started state after unload refusal.",
                ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ConfirmReleasedRuntimeUnload(string pluginId) =>
        _loader.ConfirmUnload(pluginId) is
            PluginUnloadConfirmationResult.Confirmed or
            PluginUnloadConfirmationResult.NotTracked;

    private enum RuntimeUnloadRequestResult
    {
        RefusedBeforeRequest,
        Pending,
        ConfirmedOrNotTracked,
    }

    public IDisposable AcquirePluginMutation(string pluginId)
    {
        if (!PathSecurity.IsValidPluginId(pluginId))
            throw new ArgumentException("Invalid plugin ID.", nameof(pluginId));

        var mutationKey = NormalizePluginIdentityToken(pluginId);
        var held = _heldPluginMutations.Value;
        var gate = _pluginMutationGates.GetOrAdd(mutationKey, static _ => new SemaphoreSlim(1, 1));
        var gateAlreadyAcquired = false;
        if (held?.Contains(mutationKey) == true)
        {
            if (!gate.Wait(0))
            {
                throw new InvalidOperationException(
                    $"Reentrant public mutation for plugin {pluginId} is not allowed. Internal work must present its explicit lease.");
            }

            // A child ExecutionContext can outlive the lease from which it inherited
            // AsyncLocal detection state. The gate is authoritative: if it is free,
            // discard that stale detector entry and acquire normally.
            held = new HashSet<string>(held, StringComparer.OrdinalIgnoreCase);
            held.Remove(mutationKey);
            _heldPluginMutations.Value = held.Count == 0 ? null : held;
            gateAlreadyAcquired = true;
        }

        var requestsLowerOrderedPlugin = held?.Any(heldPluginId =>
            StringComparer.OrdinalIgnoreCase.Compare(heldPluginId, mutationKey) > 0) == true;
        if (!gateAlreadyAcquired && requestsLowerOrderedPlugin)
        {
            if (!gate.Wait(0))
            {
                throw new InvalidOperationException(
                    $"Plugin mutation lock ordering would deadlock while acquiring {pluginId}.");
            }
        }
        else if (!gateAlreadyAcquired)
        {
            gate.Wait();
        }
        var prior = held;
        _heldPluginMutations.Value = held is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { mutationKey }
            : new HashSet<string>(held, StringComparer.OrdinalIgnoreCase) { mutationKey };
        return new PluginMutationLease(mutationKey, () =>
        {
            _heldPluginMutations.Value = prior;
            gate.Release();
        });
    }

    private IDisposable EnterPluginMutation(string pluginId, IDisposable? explicitLease)
    {
        if (explicitLease is null)
            return AcquirePluginMutation(pluginId);

        var mutationKey = NormalizePluginIdentityToken(pluginId);
        if (explicitLease is not PluginMutationLease lease ||
            !lease.IsActiveFor(mutationKey) ||
            _heldPluginMutations.Value?.Contains(mutationKey) != true)
        {
            throw new InvalidOperationException(
                $"The explicit mutation lease does not authorize plugin {pluginId}.");
        }

        return PluginMutationLease.Empty;
    }

    private static bool IsExplicitLeaseFor(IDisposable? lease, string pluginId) =>
        lease is PluginMutationLease pluginLease &&
        pluginLease.IsActiveFor(NormalizePluginIdentityToken(pluginId));

    public PluginRuntimeSnapshot CapturePluginRuntimeSnapshot()
    {
        var identities = _registry.GetAll().ToDictionary(
            plugin => plugin.Id,
            plugin => new PluginRuntimeIdentity(
                plugin,
                _registry.GetMetadata(plugin.Id)?.FilePath,
                _registry.IsStarted(plugin.Id),
                _runtimeGenerations.GetOrAdd(plugin.Id, static _ => Guid.NewGuid()),
                TryCaptureAssemblySha256(_registry.GetMetadata(plugin.Id)?.FilePath)),
            StringComparer.OrdinalIgnoreCase);
        return new PluginRuntimeSnapshot(identities);
    }

    private static string? TryCaptureAssemblySha256(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public async Task ActivatePluginRuntimeStrictAsync(
        string pluginId,
        string expectedMainDllPath,
        IDisposable? mutationLease = null,
        PluginPackageAuthorization? packageAuthorization = null)
    {
        using var mutation = EnterPluginMutation(pluginId, mutationLease);
        var authorization = mutationLease ?? mutation;
        await LoadPluginRuntimeStrictAsync(
                pluginId,
                expectedMainDllPath,
                authorization,
                packageAuthorization)
            .ConfigureAwait(false);

        ActivateRegisteredRuntimeStrict(pluginId);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ActivateRegisteredRuntimeStrict(string pluginId)
    {
        var plugin = _registry.Get(pluginId);
        if (plugin is not IAppStartupPlugin startupPlugin || _registry.IsStarted(pluginId))
            return;

        try
        {
            startupPlugin.OnAppStarted();
            if (!_registry.MarkStarted(pluginId))
                throw new InvalidOperationException($"Plugin {pluginId} could not enter its startup state.");
        }
        catch (Exception ex)
        {
            _registry.MarkStopped(pluginId);
            throw new InvalidOperationException($"Plugin {pluginId} failed startup activation.", ex);
        }
    }

    public async Task LoadPluginRuntimeStrictAsync(
        string pluginId,
        string expectedMainDllPath,
        IDisposable? mutationLease = null,
        PluginPackageAuthorization? packageAuthorization = null)
    {
        using var mutation = EnterPluginMutation(pluginId, mutationLease);
        if (packageAuthorization is not null &&
            !packageAuthorization.PluginId.Equals(
                pluginId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Package authorization for {packageAuthorization.PluginId} cannot activate {pluginId}.");
        }

        if (_pendingUninstallTransactions.ContainsKey(pluginId))
            throw new InvalidOperationException($"Plugin {pluginId} has an unfinished uninstall transaction.");

        if (GetPluginRuntimeUnloadState(pluginId) ==
                PluginRuntimeUnloadState.UnloadRequested &&
            !ForgetPluginRuntime(pluginId, mutationLease ?? mutation))
        {
            throw new InvalidOperationException(
                $"Plugin {pluginId} is still pending collectible runtime unload.");
        }

        var expectedPath = Path.GetFullPath(expectedMainDllPath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (PrepareStrictRuntimeLoad(
                pluginId,
                expectedPath,
                comparison,
                mutationLease ?? mutation))
        {
            await LoadPluginFromFileAsync(
                    expectedPath,
                    mutationLease ?? mutation,
                    packageAuthorization)
                .ConfigureAwait(false);
        }

        ValidateStrictLoadedRuntime(pluginId, expectedPath, comparison);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool PrepareStrictRuntimeLoad(
        string pluginId,
        string expectedPath,
        StringComparison comparison,
        IDisposable mutationLease)
    {
        var plugin = _registry.Get(pluginId);
        var metadata = _registry.GetMetadata(pluginId);
        var registeredAtExpectedPath = metadata?.FilePath is { Length: > 0 } filePath &&
                                       Path.GetFullPath(filePath).Equals(expectedPath, comparison);
        if (plugin is null)
            return true;
        if (registeredAtExpectedPath)
            return false;
        if (!ForgetPluginRuntime(pluginId, mutationLease))
            throw new InvalidOperationException($"Plugin {pluginId} could not unload before strict activation.");
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ValidateStrictLoadedRuntime(
        string pluginId,
        string expectedPath,
        StringComparison comparison)
    {
        var plugin = _registry.Get(pluginId);
        var metadata = _registry.GetMetadata(pluginId);
        if (plugin is null or PluginManifestAdapter ||
            !plugin.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase) ||
            metadata is null ||
            !metadata.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(metadata.FilePath) ||
            !Path.GetFullPath(metadata.FilePath).Equals(expectedPath, comparison))
        {
            throw new InvalidOperationException(
                $"Plugin {pluginId} did not activate from the expected assembly.");
        }
    }

    public PluginRuntimeReconciliation ReconcilePluginRuntimes(
        PluginRuntimeSnapshot baseline,
        string replacementDirectory,
        IDisposable? mutationLease = null,
        string? expectedPluginId = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        var canonicalReplacementDirectory = Path.GetFullPath(replacementDirectory);
        var failures = new List<Exception>();
        var affectedPluginIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(expectedPluginId))
            affectedPluginIds.Add(expectedPluginId);

        foreach (var affectedPluginId in CaptureReconciliationCandidates(
                     baseline,
                     canonicalReplacementDirectory))
        {
            affectedPluginIds.Add(affectedPluginId);
            try
            {
                if (!ForgetPluginRuntime(
                        affectedPluginId,
                        IsExplicitLeaseFor(mutationLease, affectedPluginId) ? mutationLease : null) &&
                    !ConfirmReconciledRuntimeAfterRelease(
                        affectedPluginId,
                        IsExplicitLeaseFor(mutationLease, affectedPluginId) ? mutationLease : null))
                {
                    failures.Add(new InvalidOperationException(
                        $"Plugin runtime {affectedPluginId} could not be unloaded."));
                }
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
            throw new AggregateException("Replacement plugin runtimes could not be reconciled.", failures);

        return new PluginRuntimeReconciliation(affectedPluginIds);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ConfirmReconciledRuntimeAfterRelease(
        string pluginId,
        IDisposable? mutationLease) =>
        GetPluginRuntimeUnloadState(pluginId) ==
            PluginRuntimeUnloadState.UnloadRequested &&
        ForgetPluginRuntime(pluginId, mutationLease);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IReadOnlyList<string> CaptureReconciliationCandidates(
        PluginRuntimeSnapshot baseline,
        string canonicalReplacementDirectory)
    {
        var affectedPluginIds = new List<string>();
        foreach (var plugin in _registry.GetAll())
        {
            var metadata = _registry.GetMetadata(plugin.Id);
            var fromReplacement = metadata?.FilePath is { Length: > 0 } filePath &&
                                  IsPathWithinDirectory(filePath, canonicalReplacementDirectory);
            var changed = !baseline.Identities.TryGetValue(plugin.Id, out var oldIdentity) ||
                          !oldIdentity.IsSameInstance(plugin);
            if (fromReplacement && changed)
                affectedPluginIds.Add(plugin.Id);
        }
        return affectedPluginIds;
    }

    public void RestorePluginRuntimeSnapshot(
        PluginRuntimeSnapshot baseline,
        IDisposable? mutationLease = null,
        PluginRuntimeReconciliation? reconciliation = null)
    {
        RestorePluginRuntimeSnapshotCore(
            baseline,
            mutationLease,
            reconciliation,
            restoreStartedState: true);
    }

    private void RestorePluginRuntimeSnapshotCore(
        PluginRuntimeSnapshot baseline,
        IDisposable? mutationLease,
        PluginRuntimeReconciliation? reconciliation,
        bool restoreStartedState)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        var failures = new List<Exception>();
        var affectedPluginIds = reconciliation?.AffectedPluginIds ??
                                baseline.Identities.Keys.ToHashSet(
                                    StringComparer.OrdinalIgnoreCase);
        foreach (var pluginId in affectedPluginIds)
        {
            if (!baseline.Identities.TryGetValue(pluginId, out var identity))
                continue;

            if (string.IsNullOrWhiteSpace(identity.FilePath))
                continue;

            try
            {
                RestoreBaselineRuntime(
                    pluginId,
                    identity,
                    IsExplicitLeaseFor(mutationLease, pluginId) ? mutationLease : null,
                    restoreStartedState);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
            throw new AggregateException("Baseline plugin runtimes could not be restored.", failures);
    }

    private void RestoreBaselineRuntime(
        string pluginId,
        PluginRuntimeIdentity identity,
        IDisposable? mutationLease,
        bool restoreStartedState)
    {
        using var mutation = EnterPluginMutation(pluginId, mutationLease);
        var expectedPath = Path.GetFullPath(identity.FilePath!);
        var current = _registry.Get(pluginId);
        var currentPath = _registry.GetMetadata(pluginId)?.FilePath;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var pathMatches = !string.IsNullOrWhiteSpace(currentPath) &&
                          Path.GetFullPath(currentPath).Equals(expectedPath, comparison);
        var generationMatches =
            _runtimeGenerations.TryGetValue(pluginId, out var currentGeneration) &&
            currentGeneration == identity.RuntimeGeneration;
        var instanceMatches = identity.IsSameInstance(current);
        var fingerprintMatches = identity.AssemblySha256 is null ||
                                 string.Equals(
                                     TryCaptureAssemblySha256(expectedPath),
                                     identity.AssemblySha256,
                                     StringComparison.OrdinalIgnoreCase);

        if (!fingerprintMatches)
        {
            throw new InvalidDataException(
                $"Baseline runtime {pluginId} assembly fingerprint does not match the captured original.");
        }

        if (current is null || !pathMatches || !generationMatches || !instanceMatches)
        {
            if (current is PluginManifestAdapter)
            {
                _registry.Forget(pluginId);
                current = null;
            }
            else if (current is not null &&
                     !ForgetPluginRuntime(pluginId, mutationLease ?? mutation))
            {
                throw new InvalidOperationException($"Displaced runtime {pluginId} could not be unloaded.");
            }

            LoadPluginFromFileAsync(
                    expectedPath,
                    mutationLease ?? mutation)
                .GetAwaiter()
                .GetResult();
            current = _registry.Get(pluginId);
            currentPath = _registry.GetMetadata(pluginId)?.FilePath;
            if (current is null ||
                string.IsNullOrWhiteSpace(currentPath) ||
                !Path.GetFullPath(currentPath).Equals(expectedPath, comparison))
            {
                throw new InvalidOperationException(
                    $"Baseline runtime {pluginId} could not be restored from {expectedPath}.");
            }

            if (!_runtimeGenerations.TryGetValue(pluginId, out var restoredGeneration) ||
                restoredGeneration == identity.RuntimeGeneration)
            {
                throw new InvalidOperationException(
                    $"Baseline runtime {pluginId} did not receive a newly approved loader generation.");
            }
        }

        var shouldBeStarted = restoreStartedState && identity.WasStarted;
        var isStarted = _registry.IsStarted(pluginId);
        if (shouldBeStarted == isStarted)
            return;

        if (!shouldBeStarted)
        {
            current.Stop();
            _registry.MarkStopped(pluginId);
            return;
        }

        if (current is not IAppStartupPlugin startupPlugin)
            throw new InvalidOperationException($"Baseline runtime {pluginId} cannot restore its started state.");

        startupPlugin.OnAppStarted();
        if (!_registry.MarkStarted(pluginId))
            throw new InvalidOperationException($"Baseline runtime {pluginId} could not be marked started.");
    }

    public PluginMetadata? GetPluginMetadata(string pluginId)
    {
        return _registry.GetMetadata(pluginId);
    }

    private sealed class PluginMutationLease : IDisposable
    {
        internal static readonly PluginMutationLease Empty = new(string.Empty, null);
        private readonly string _mutationKey;
        private Action? _release;

        internal PluginMutationLease(string mutationKey, Action? release)
        {
            _mutationKey = mutationKey;
            _release = release;
        }

        internal bool IsActiveFor(string mutationKey) =>
            _release is not null &&
            _mutationKey.Equals(mutationKey, StringComparison.OrdinalIgnoreCase);

        public void Dispose()
        {
            Interlocked.Exchange(ref _release, null)?.Invoke();
        }
    }

    public bool IsInstalled(string pluginId)
    {
        // SECURITY: Validate plugin ID format
        if (!PathSecurity.IsValidPluginId(pluginId))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"IsInstalled({pluginId}): Invalid plugin ID format");
            return false;
        }

        bool isInstalled;
        var traceEnabled = Log.Instance.IsTraceEnabled;
        var installedMarkerSnapshot = Array.Empty<string>();
        using var commitRead = PluginInstallationCommitCoordinator.EnterRead();
        lock (_installationMarkerLock)
        {
            isInstalled = StateStore.InstalledExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase);
            if (traceEnabled)
                installedMarkerSnapshot = StateStore.InstalledExtensions.ToArray();
        }

        if (traceEnabled)
        {
            Log.Instance.Trace($"IsInstalled({pluginId}) = {isInstalled} (from settings)");
            Log.Instance.Trace($"  - Installed extensions count: {installedMarkerSnapshot.Length}");
            Log.Instance.Trace($"  - Installed extensions: [{string.Join(", ", installedMarkerSnapshot)}]");
        }

        // If not in installed list, definitely not installed
        if (!isInstalled)
            return false;

        // Check if plugin files actually exist on disk
        try
        {
            var pluginsDirectory = _fileSystemManager.GetPluginsDirectory();
            var mainDllNameCandidates = _fileSystemManager.GetMainPluginDllNameCandidates(pluginId);
            if (mainDllNameCandidates.Length == 0)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"IsInstalled({pluginId}): No safe DLL name candidates generated");
                return false;
            }

            // Check plugin directory
            var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);
            var localPluginDirectory = Path.Combine(pluginsDirectory, "local", pluginId);

            if (!PathSecurity.IsPathWithinAllowedDirectory(pluginDirectory, pluginsDirectory) ||
                !PathSecurity.IsPathWithinAllowedDirectory(localPluginDirectory, pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"IsInstalled({pluginId}): Path traversal detected");
                return false;
            }

            // Check for alternative directory naming (UDT + legacy LLT Plugins.{Id})
            if (!Directory.Exists(pluginDirectory) && !Directory.Exists(localPluginDirectory))
            {
                var noHyphenId = pluginId.Replace("-", "");
                string? resolvedAltDirectory = null;
                foreach (var altName in PluginAssemblyNaming.EnumeratePrefixedPluginNames(pluginId)
                             .Concat(PluginAssemblyNaming.EnumeratePrefixedPluginNames(noHyphenId)))
                {
                    var altPluginDirectory = Path.Combine(pluginsDirectory, altName);
                    if (!PathSecurity.IsPathWithinAllowedDirectory(altPluginDirectory, pluginsDirectory))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"IsInstalled({pluginId}): Path traversal detected in alternative paths");
                        return false;
                    }

                    if (Directory.Exists(altPluginDirectory))
                    {
                        resolvedAltDirectory = altPluginDirectory;
                        break;
                    }
                }

                if (resolvedAltDirectory is not null)
                {
                    pluginDirectory = resolvedAltDirectory;
                }
                else
                {
                    // Check for root level DLLs (backward compatibility)
                    var rootDllCandidates = mainDllNameCandidates
                        .Where(PathSecurity.IsValidFileName)
                        .Select(fileName => Path.Combine(pluginsDirectory, fileName))
                        .Where(path => PathSecurity.IsPathWithinAllowedDirectory(path, pluginsDirectory));

                    if (rootDllCandidates.Any(File.Exists))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"IsInstalled({pluginId}): Found as root level DLL");
                        return true;
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"IsInstalled({pluginId}): Plugin directory does not exist");
                    return false;
                }
            }
            else if (Directory.Exists(localPluginDirectory))
            {
                pluginDirectory = localPluginDirectory;
            }

            if (!PathSecurity.IsPathWithinAllowedDirectory(pluginDirectory, pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"IsInstalled({pluginId}): Resolved plugin directory is outside plugins root");
                return false;
            }

            // Check for DLL files in the plugin directory
            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(filePath => PathSecurity.IsPathWithinAllowedDirectory(filePath, pluginsDirectory))
                .Where(filePath => PathSecurity.IsValidFileName(Path.GetFileName(filePath)))
                .ToArray();
            if (dllFiles.Length == 0)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"IsInstalled({pluginId}): No DLL files found in plugin directory");
                return false;
            }

            // Main plugin DLL is required — presence of other DLLs alone is not "installed".
            var hasMainDll = dllFiles.Any(f => mainDllNameCandidates.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase));
            if (!hasMainDll)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"IsInstalled({pluginId}): Main plugin DLL not found. Available DLLs: [{string.Join(", ", dllFiles.Select(Path.GetFileName))}]");
                return false;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"IsInstalled({pluginId}): Main plugin DLL present, returning true");

            return true;
        }
        catch (Exception ex)
        {
            // Fail closed: settings can lag behind a broken on-disk layout.
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"IsInstalled({pluginId}): Error checking plugin files: {ex.Message}");
            return false;
        }
    }

    public PluginInstallationStateSnapshot CommitPluginInstallationState(
        string pluginId,
        IDisposable? mutationLease = null)
    {
        if (!PathSecurity.IsValidPluginId(pluginId))
            throw new ArgumentException("Invalid plugin ID.", nameof(pluginId));

        using var mutation = EnterPluginMutation(pluginId, mutationLease);
        lock (_installationMarkerLock)
        {
            var snapshot = CapturePluginInstallationStateUnderLock(pluginId);
            var installedExtensions = StateStore.InstalledExtensions;
            var pendingDeletionExtensions = StateStore.PendingDeletionExtensions;
            try
            {
                var changed = pendingDeletionExtensions.RemoveAll(
                    id => StringComparer.OrdinalIgnoreCase.Equals(id, pluginId)) > 0;

                if (!installedExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
                {
                    installedExtensions.Add(pluginId);
                    changed = true;
                }

                if (changed)
                    SynchronizeStateStore();

                return snapshot;
            }
            catch (Exception commitException)
            {
                RestorePluginInstallationStateUnderLock(snapshot);
                try
                {
                    SynchronizeStateStore();
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        "Plugin installation marker commit and rollback persistence both failed.",
                        commitException,
                        rollbackException);
                }

                throw;
            }
        }
    }

    public void RestorePluginInstallationState(
        PluginInstallationStateSnapshot snapshot,
        IDisposable? mutationLease = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!PathSecurity.IsValidPluginId(snapshot.PluginId))
            throw new ArgumentException("Snapshot contains an invalid plugin ID.", nameof(snapshot));

        using var mutation = EnterPluginMutation(snapshot.PluginId, mutationLease);
        lock (_installationMarkerLock)
        {
            RestorePluginInstallationStateUnderLock(snapshot);
            // Always persist during rollback so a prior failed persistence attempt is retried.
            SynchronizeStateStore();
        }
    }

    public PluginInstallationStateSnapshot CommitPluginInstallation(
        string pluginId,
        IDisposable? mutationLease = null,
        Action? coordinatedStateCommit = null)
    {
        if (!PathSecurity.IsValidPluginId(pluginId))
            throw new ArgumentException("Invalid plugin ID.", nameof(pluginId));

        using var mutation = EnterPluginMutation(pluginId, mutationLease);
        var authorization = mutationLease ?? mutation;
        if (!_preparedInstallations.TryGetValue(pluginId, out var preparation))
        {
            PreparePluginInstallation(pluginId, authorization);
            preparation = _preparedInstallations[pluginId];
        }

        IReadOnlyList<PluginInstallationStateSnapshot> snapshots;
        try
        {
            using (PluginInstallationCommitCoordinator.EnterWrite())
            {
                snapshots = CommitPreparedInstallationMarkers(
                    pluginId,
                    preparation.CallbackPluginIds);
                if (coordinatedStateCommit is not null)
                {
                    try
                    {
                        coordinatedStateCommit();
                    }
                    catch
                    {
                        RestoreCommittedInstallationMarkers(snapshots);
                        throw;
                    }
                }
            }
        }
        catch (Exception markerCommitFailure)
        {
            try
            {
                RollbackPreparedPluginInstallation(pluginId, authorization);
            }
            catch (Exception callbackRollbackFailure)
            {
                throw new AggregateException(
                    $"Plugin {pluginId} marker commit and callback rollback both failed.",
                    markerCommitFailure,
                    callbackRollbackFailure);
            }
            throw;
        }

        _preparedInstallations.TryRemove(
            new KeyValuePair<string, PreparedPluginInstallation>(
                pluginId,
                preparation));
        DisposeDependencyMutationLeases(preparation.DependencyMutationLeases);

        foreach (var callbackPluginId in preparation.CallbackPluginIds)
            PublishCommittedInstallationLifecycle(callbackPluginId);

        var markerSnapshot = snapshots.First(snapshot =>
            snapshot.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        if (markerSnapshot.WasPendingDeletion && Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Removed {pluginId} from pending deletion list during install/reinstall.");
        return markerSnapshot;
    }

    private void RestoreCommittedInstallationMarkers(
        IReadOnlyList<PluginInstallationStateSnapshot> snapshots)
    {
        lock (_installationMarkerLock)
        {
            foreach (var snapshot in snapshots.Reverse())
                RestorePluginInstallationStateUnderLock(snapshot);
            SynchronizeStateStore();
        }
    }

    public void PreparePluginInstallation(
        string pluginId,
        IDisposable? mutationLease = null)
    {
        using var mutation = EnterPluginMutation(pluginId, mutationLease);
        if (_preparedInstallations.ContainsKey(pluginId))
            throw new InvalidOperationException($"Plugin {pluginId} already has a staged installation.");

        var callbackOrder = BuildInstallationCallbackOrder(pluginId);
        var dependencyLeases = new List<IDisposable>();
        var completedCallbacks = new List<string>();
        try
        {
            foreach (var dependencyId in callbackOrder
                         .Where(id => !id.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(NormalizePluginIdentityToken, StringComparer.OrdinalIgnoreCase))
            {
                dependencyLeases.Add(AcquirePluginMutation(dependencyId));
            }

            foreach (var callbackPluginId in callbackOrder)
            {
                var callbackPlugin = _registry.Get(callbackPluginId)
                    ?? throw new InvalidOperationException(
                        $"Plugin {callbackPluginId} is not loaded for installation preparation.");
                callbackPlugin.OnInstalled();
                completedCallbacks.Add(callbackPluginId);
            }

            _preparedInstallations[pluginId] =
                new PreparedPluginInstallation(completedCallbacks.ToArray(), dependencyLeases.ToArray());
        }
        catch (Exception preparationFailure)
        {
            var failures = new List<Exception> { preparationFailure };
            foreach (var callbackPluginId in completedCallbacks.AsEnumerable().Reverse())
            {
                try
                {
                    _registry.Get(callbackPluginId)?.OnUninstalled();
                }
                catch (Exception compensationFailure)
                {
                    failures.Add(compensationFailure);
                }
            }

            DisposeDependencyMutationLeases(dependencyLeases, failures);
            if (failures.Count > 1)
            {
                throw new AggregateException(
                    $"Plugin {pluginId} installation preparation and compensation both failed.",
                    failures);
            }

            throw;
        }
    }

    public void RollbackPreparedPluginInstallation(
        string pluginId,
        IDisposable? mutationLease = null)
    {
        using var mutation = EnterPluginMutation(pluginId, mutationLease);
        if (!_preparedInstallations.TryRemove(pluginId, out var preparation))
            return;

        var failures = new List<Exception>();
        var plugin = _registry.Get(pluginId);
        if (_registry.IsStarted(pluginId) && plugin is not null)
        {
            try
            {
                plugin.Stop();
                _registry.MarkStopped(pluginId);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        foreach (var callbackPluginId in preparation.CallbackPluginIds.Reverse())
        {
            try
            {
                _registry.Get(callbackPluginId)?.OnUninstalled();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        DisposeDependencyMutationLeases(preparation.DependencyMutationLeases, failures);
        if (failures.Count > 0)
            throw new AggregateException($"Plugin {pluginId} preparation rollback failed.", failures);
    }

    private IReadOnlyList<string> BuildInstallationCallbackOrder(string rootPluginId)
    {
        var callbackOrder = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Visit(rootPluginId);
        return callbackOrder;

        void Visit(string pluginId)
        {
            if (!PathSecurity.IsValidPluginId(pluginId))
                throw new InvalidOperationException($"Plugin dependency ID {pluginId} is invalid.");
            if (IsPluginMarkedInstalled(pluginId) || visited.Contains(pluginId))
                return;
            if (!visiting.Add(pluginId))
                throw new InvalidOperationException($"Plugin dependency cycle includes {pluginId}.");

            var plugin = _registry.Get(pluginId);
            if (plugin is null)
            {
                visiting.Remove(pluginId);
                if (pluginId.Equals(rootPluginId, StringComparison.OrdinalIgnoreCase))
                {
                    visited.Add(pluginId);
                    return;
                }

                throw new InvalidOperationException(
                    $"Plugin dependency {pluginId} is not loaded for installation preparation.");
            }
            foreach (var dependencyId in plugin.Dependencies ?? [])
            {
                if (NormalizePluginIdentityToken(dependencyId).Equals(
                        NormalizePluginIdentityToken(pluginId),
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Plugin {pluginId} cannot depend on itself.");
                }
                Visit(dependencyId);
            }

            visiting.Remove(pluginId);
            if (visited.Add(pluginId))
                callbackOrder.Add(pluginId);
        }
    }

    private bool IsPluginMarkedInstalled(string pluginId)
    {
        using var commitRead = PluginInstallationCommitCoordinator.EnterRead();
        lock (_installationMarkerLock)
        {
            return StateStore.InstalledExtensions.Contains(
                pluginId,
                StringComparer.OrdinalIgnoreCase);
        }
    }

    private IReadOnlyList<PluginInstallationStateSnapshot> CommitPreparedInstallationMarkers(
        string rootPluginId,
        IReadOnlyList<string> callbackPluginIds)
    {
        var markerPluginIds = callbackPluginIds
            .Append(rootPluginId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        lock (_installationMarkerLock)
        {
            var snapshots = markerPluginIds
                .Select(CapturePluginInstallationStateUnderLock)
                .ToArray();
            try
            {
                foreach (var markerPluginId in markerPluginIds)
                {
                    StateStore.PendingDeletionExtensions.RemoveAll(
                        id => StringComparer.OrdinalIgnoreCase.Equals(id, markerPluginId));
                    if (!StateStore.InstalledExtensions.Contains(
                            markerPluginId,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        StateStore.InstalledExtensions.Add(markerPluginId);
                    }
                }

                SynchronizeStateStore();
                return snapshots;
            }
            catch (Exception commitFailure)
            {
                foreach (var snapshot in snapshots.Reverse())
                    RestorePluginInstallationStateUnderLock(snapshot);
                try
                {
                    SynchronizeStateStore();
                }
                catch (Exception rollbackFailure)
                {
                    throw new AggregateException(
                        "Prepared plugin marker commit and rollback persistence both failed.",
                        commitFailure,
                        rollbackFailure);
                }
                throw;
            }
        }
    }

    private void PublishCommittedInstallationLifecycle(string pluginId)
    {
        TransitionLifecycleStatePostCommit(
            pluginId,
            PluginState.Installed,
            legacyIsInstalled: true);
        if (_registry.IsStarted(pluginId))
        {
            TransitionLifecycleStatePostCommit(
                pluginId,
                PluginState.Enabled,
                legacyIsInstalled: true);
        }
    }

    private static void DisposeDependencyMutationLeases(
        IEnumerable<IDisposable> leases,
        List<Exception>? failures = null)
    {
        foreach (var lease in leases.Reverse())
        {
            try
            {
                lease.Dispose();
            }
            catch (Exception ex)
            {
                failures?.Add(ex);
            }
        }
    }

    private PluginInstallationStateSnapshot CapturePluginInstallationStateUnderLock(string pluginId)
    {
        var installedExtensions = StateStore.InstalledExtensions;
        var pendingDeletionExtensions = StateStore.PendingDeletionExtensions;
        var installedIndex = installedExtensions.FindIndex(
            id => StringComparer.OrdinalIgnoreCase.Equals(id, pluginId));
        var pendingDeletionIndex = pendingDeletionExtensions.FindIndex(
            id => StringComparer.OrdinalIgnoreCase.Equals(id, pluginId));

        return new PluginInstallationStateSnapshot(
            pluginId,
            installedIndex >= 0 ? installedExtensions[installedIndex] : null,
            installedIndex,
            pendingDeletionIndex >= 0 ? pendingDeletionExtensions[pendingDeletionIndex] : null,
            pendingDeletionIndex);
    }

    private void RestorePluginInstallationStateUnderLock(PluginInstallationStateSnapshot snapshot)
    {
        RestoreMarkerMembership(
            StateStore.InstalledExtensions,
            snapshot.PluginId,
            snapshot.InstalledMarker,
            snapshot.InstalledIndex);
        RestoreMarkerMembership(
            StateStore.PendingDeletionExtensions,
            snapshot.PluginId,
            snapshot.PendingDeletionMarker,
            snapshot.PendingDeletionIndex);
    }

    private static void RestoreMarkerMembership(
        List<string> markers,
        string pluginId,
        string? originalMarker,
        int originalIndex)
    {
        var currentIndex = markers.FindIndex(
            id => StringComparer.OrdinalIgnoreCase.Equals(id, pluginId));
        if (originalMarker is null)
        {
            if (currentIndex >= 0)
                markers.RemoveAll(id => StringComparer.OrdinalIgnoreCase.Equals(id, pluginId));
            return;
        }

        if (currentIndex >= 0)
            return;

        var insertionIndex = originalIndex < 0 ? markers.Count : Math.Min(originalIndex, markers.Count);
        markers.Insert(insertionIndex, originalMarker);
    }

    public void InstallPlugin(string pluginId)
    {
        // SECURITY: Validate plugin ID format
        if (!PathSecurity.IsValidPluginId(pluginId))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"InstallPlugin: Invalid plugin ID format: {pluginId}");
            return;
        }

        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        using var mutation = AcquirePluginMutation(pluginId);
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"InstallPlugin called for {pluginId}");

        var markerSnapshot = CommitPluginInstallation(pluginId, mutation);
        if (markerSnapshot.WasInstalled && Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Plugin {pluginId} is already installed");
    }

    public void PruneRetiredPlugins()
    {
        string[] installedExtensions;
        lock (_installationMarkerLock)
            installedExtensions = StateStore.InstalledExtensions.ToArray();

        foreach (var id in RetiredPluginIds)
        {
            // Retired plugins may be listed in settings after store removal; do not require
            // on-disk DLL folders (IsInstalled also checks the plugins directory).
            if (!installedExtensions.Contains(id, StringComparer.OrdinalIgnoreCase))
                continue;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"PruneRetiredPlugins: uninstalling retired plugin '{id}'.");

            UninstallPlugin(id);
        }
    }

    public bool UninstallPlugin(string pluginId)
    {
        // SECURITY: Validate plugin ID format
        if (!PathSecurity.IsValidPluginId(pluginId))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"UninstallPlugin: Invalid plugin ID format: {pluginId}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        using var mutation = AcquirePluginMutation(pluginId);
        if (_pendingUninstallTransactions.TryGetValue(
                pluginId,
                out var pendingTransaction))
        {
            if (!RetryPendingUninstallRuntime(pendingTransaction))
                return false;

            FinalizeRuntimeUnloadBookkeeping(pluginId);
            return FinalizeUninstallTransaction(pendingTransaction, mutation);
        }

        lock (_installationMarkerLock)
        {
            if (!StateStore.InstalledExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        if (HasInstalledDependentPlugin(pluginId))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Cannot uninstall plugin {pluginId} because it is a dependency of other installed plugins.");
            return false;
        }

        var allRuntimeBaseline = CapturePluginRuntimeSnapshot();
        var runtimeBaseline = new PluginRuntimeSnapshot(
            allRuntimeBaseline.Identities.TryGetValue(pluginId, out var runtimeIdentity)
                ? new Dictionary<string, PluginRuntimeIdentity>(StringComparer.OrdinalIgnoreCase)
                {
                    [pluginId] = runtimeIdentity,
                }
                : new Dictionary<string, PluginRuntimeIdentity>(StringComparer.OrdinalIgnoreCase));
        var trustSnapshot = TrustedPluginPackageStore.CaptureExactTrustRecord(pluginId);
        PluginInstallationStateSnapshot markerSnapshot;
        lock (_installationMarkerLock)
            markerSnapshot = CapturePluginInstallationStateUnderLock(pluginId);
        var uninstallTransaction = new PendingUninstallTransaction(
            pluginId,
            runtimeBaseline,
            markerSnapshot,
            trustSnapshot,
            GetPluginState(pluginId),
            _registry.IsStarted(pluginId));
        var teardownResult = BeginUninstallRuntimeTeardown(uninstallTransaction);
        if (teardownResult == RuntimeUnloadRequestResult.RefusedBeforeRequest)
            return false;

        if (teardownResult == RuntimeUnloadRequestResult.Pending)
        {
            if (!_pendingUninstallTransactions.TryAdd(pluginId, uninstallTransaction))
            {
                throw new InvalidOperationException(
                    $"Plugin {pluginId} already has a pending uninstall transaction.");
            }

            if (!ConfirmPendingUninstallRuntime(pluginId))
                return false;
        }

        _pendingUninstallTransactions.TryAdd(pluginId, uninstallTransaction);
        FinalizeRuntimeUnloadBookkeeping(pluginId);
        return FinalizeUninstallTransaction(uninstallTransaction, mutation);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool HasInstalledDependentPlugin(string pluginId)
    {
        foreach (var candidate in _registry.GetAll())
        {
            if (candidate.Dependencies is not null &&
                candidate.Dependencies.Contains(pluginId, StringComparer.OrdinalIgnoreCase) &&
                IsInstalled(candidate.Id))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RuntimeUnloadRequestResult BeginUninstallRuntimeTeardown(
        PendingUninstallTransaction transaction)
    {
        var unloadState = GetPluginRuntimeUnloadState(transaction.PluginId);
        if (unloadState == PluginRuntimeUnloadState.UnloadRequested)
        {
            ReleaseUninstallRegistryReference(transaction.PluginId);
            return RuntimeUnloadRequestResult.Pending;
        }

        var plugin = _registry.Get(transaction.PluginId);
        if (plugin is null or PluginManifestAdapter)
        {
            if (plugin is PluginManifestAdapter)
                _registry.Forget(transaction.PluginId);
            if (unloadState == PluginRuntimeUnloadState.Active)
            {
                return IsUnloadRequestAccepted(RequestLoaderUnload(transaction.PluginId))
                    ? RuntimeUnloadRequestResult.Pending
                    : RuntimeUnloadRequestResult.RefusedBeforeRequest;
            }

            return RuntimeUnloadRequestResult.ConfirmedOrNotTracked;
        }

        if (transaction.WasStarted)
        {
            try
            {
                plugin.Stop();
                _registry.MarkStopped(transaction.PluginId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to stop plugin {transaction.PluginId} before uninstall.",
                    ex);
            }
        }

        try
        {
            plugin.OnUninstalled();
            transaction.UninstallCallbackRan = true;
        }
        catch (Exception uninstallCallbackFailure)
        {
            var failures = new List<Exception> { uninstallCallbackFailure };
            TryRestoreOriginalStartedState(transaction, plugin, failures);
            if (failures.Count > 1)
            {
                throw new AggregateException(
                    $"Plugin {transaction.PluginId} uninstall callback failed and startup restoration was degraded.",
                    failures);
            }
            throw;
        }

        var metadata = _registry.GetMetadata(transaction.PluginId);
        ReleaseUninstallRegistryReference(transaction.PluginId);
        PluginUnloadRequestResult requestResult;
        try
        {
            requestResult = RequestLoaderUnload(transaction.PluginId);
        }
        catch (Exception unloadFailure)
        {
            if (metadata is not null)
                _registry.Register(plugin, metadata);
            var failures = new List<Exception> { unloadFailure };
            CompensateActiveUninstallCallback(transaction, plugin, failures);
            throw new AggregateException(
                $"Plugin {transaction.PluginId} unload request failed and callback restoration was degraded.",
                failures);
        }

        if (IsUnloadRequestAccepted(requestResult))
            return RuntimeUnloadRequestResult.Pending;

        if (metadata is not null)
            _registry.Register(plugin, metadata);
        var compensationFailures = new List<Exception>();
        CompensateActiveUninstallCallback(transaction, plugin, compensationFailures);
        if (compensationFailures.Count > 0)
        {
            throw new AggregateException(
                $"Plugin {transaction.PluginId} unload was refused and callback restoration was degraded.",
                compensationFailures);
        }

        return RuntimeUnloadRequestResult.RefusedBeforeRequest;
    }

    private void ReleaseUninstallRegistryReference(string pluginId)
    {
        if (!_registry.ReplaceWithMetadataAdapter(pluginId))
            _registry.Forget(pluginId);
    }

    private void CompensateActiveUninstallCallback(
        PendingUninstallTransaction transaction,
        IPlugin plugin,
        List<Exception> failures)
    {
        if (transaction.UninstallCallbackRan)
        {
            try
            {
                plugin.OnInstalled();
                transaction.UninstallCallbackRan = false;
            }
            catch (Exception callbackCompensationFailure)
            {
                failures.Add(callbackCompensationFailure);
            }
        }

        TryRestoreOriginalStartedState(transaction, plugin, failures);
    }

    private void TryRestoreOriginalStartedState(
        PendingUninstallTransaction transaction,
        IPlugin plugin,
        List<Exception> failures)
    {
        if (!transaction.WasStarted)
            return;

        try
        {
            if (plugin is not IAppStartupPlugin startupPlugin)
                throw new InvalidOperationException(
                    $"Plugin {transaction.PluginId} cannot restore its started state.");
            startupPlugin.OnAppStarted();
            if (!_registry.MarkStarted(transaction.PluginId))
                throw new InvalidOperationException(
                    $"Plugin {transaction.PluginId} could not be marked started.");
        }
        catch (Exception startupRestorationFailure)
        {
            _registry.MarkStopped(transaction.PluginId);
            failures.Add(startupRestorationFailure);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ConfirmPendingUninstallRuntime(string pluginId) =>
        ConfirmReleasedRuntimeUnload(pluginId);

    private bool RetryPendingUninstallRuntime(PendingUninstallTransaction transaction)
    {
        var unloadState = GetPluginRuntimeUnloadState(transaction.PluginId);
        var hasActiveRegistration = _registry.Get(transaction.PluginId) is not null and
            not PluginManifestAdapter;
        if (hasActiveRegistration || unloadState == PluginRuntimeUnloadState.Active)
        {
            var teardown = BeginUninstallRuntimeTeardown(transaction);
            if (teardown == RuntimeUnloadRequestResult.RefusedBeforeRequest)
                return false;
            if (teardown == RuntimeUnloadRequestResult.ConfirmedOrNotTracked)
                return true;
        }

        return ConfirmPendingUninstallRuntime(transaction.PluginId);
    }

    private void FinalizeRuntimeUnloadBookkeeping(string pluginId)
    {
        _loadedPluginFileIdentities.TryRemove(pluginId, out _);
        _runtimeGenerations.TryRemove(pluginId, out _);
    }

    private bool FinalizeUninstallTransaction(
        PendingUninstallTransaction transaction,
        IDisposable mutationLease)
    {
        try
        {
            CommitUninstallMarkerAndTrust(transaction);
        }
        catch (Exception uninstallFailure)
        {
            var failures = new List<Exception> { uninstallFailure };
            SetPluginState(transaction.PluginId, transaction.LifecycleState);

            try
            {
                RestoreUninstalledRuntimeAndCallbackState(transaction, mutationLease);
            }
            catch (Exception runtimeRestoreFailure)
            {
                failures.Add(runtimeRestoreFailure);
            }

            if (failures.Count == 1)
            {
                _pendingUninstallTransactions.TryRemove(
                    new KeyValuePair<string, PendingUninstallTransaction>(
                        transaction.PluginId,
                        transaction));
            }

            if (failures.Count > 1)
            {
                throw new AggregateException(
                    $"Plugin {transaction.PluginId} uninstall failed and restoration was degraded.",
                    failures);
            }
            throw;
        }

        _pendingUninstallTransactions.TryRemove(
            new KeyValuePair<string, PendingUninstallTransaction>(
                transaction.PluginId,
                transaction));
        if (!TransitionLifecycleStatePostCommit(
                transaction.PluginId,
                PluginState.NotInstalled,
                legacyIsInstalled: false))
        {
            PublishPluginStateChangedPostCommit(
                new PluginEventArgs(transaction.PluginId, false));
        }
        return true;
    }

    private void CommitUninstallMarkerAndTrust(PendingUninstallTransaction transaction)
    {
        using var commitWrite = PluginInstallationCommitCoordinator.EnterWrite();
        var markerMutationAttempted = false;
        var trustMutationAttempted = false;
        try
        {
            markerMutationAttempted = true;
            lock (_installationMarkerLock)
            {
                if (!StateStore.InstalledExtensions.Contains(
                        transaction.PluginId,
                        StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Plugin {transaction.PluginId} installation marker changed during uninstall.");
                }

                StateStore.InstalledExtensions.RemoveAll(
                    id => id.Equals(transaction.PluginId, StringComparison.OrdinalIgnoreCase));
                if (!StateStore.PendingDeletionExtensions.Contains(
                        transaction.PluginId,
                        StringComparer.OrdinalIgnoreCase))
                {
                    StateStore.PendingDeletionExtensions.Add(transaction.PluginId);
                }
                SynchronizeStateStore();
            }

            trustMutationAttempted = true;
            TrustedPluginPackageStore.RemoveStrictUnderCommitLease(transaction.PluginId);
        }
        catch (Exception commitFailure)
        {
            var failures = new List<Exception> { commitFailure };
            if (trustMutationAttempted)
            {
                try
                {
                    TrustedPluginPackageStore.RestoreExactTrustRecordUnderCommitLease(
                        transaction.PluginId,
                        transaction.TrustSnapshot);
                }
                catch (Exception trustRestoreFailure)
                {
                    failures.Add(trustRestoreFailure);
                }
            }

            if (markerMutationAttempted)
            {
                try
                {
                    lock (_installationMarkerLock)
                    {
                        RestorePluginInstallationStateUnderLock(transaction.MarkerSnapshot);
                        SynchronizeStateStore();
                    }
                }
                catch (Exception markerRestoreFailure)
                {
                    failures.Add(markerRestoreFailure);
                }
            }

            if (failures.Count > 1)
            {
                throw new AggregateException(
                    $"Plugin {transaction.PluginId} uninstall state commit failed and rollback was degraded.",
                    failures);
            }
            throw;
        }
    }

    private void RestoreUninstalledRuntimeAndCallbackState(
        PendingUninstallTransaction transaction,
        IDisposable mutationLease)
    {
        _registry.Forget(transaction.PluginId);
        RestorePluginRuntimeSnapshotCore(
            transaction.RuntimeBaseline,
            mutationLease,
            new PluginRuntimeReconciliation([transaction.PluginId]),
            restoreStartedState: false);

        var restoredPlugin = _registry.Get(transaction.PluginId);
        if (transaction.UninstallCallbackRan)
        {
            if (restoredPlugin is null)
                throw new InvalidOperationException(
                    $"Plugin {transaction.PluginId} runtime was not restored for callback compensation.");
            restoredPlugin.OnInstalled();
            transaction.UninstallCallbackRan = false;
        }

        if (!transaction.WasStarted)
        {
            _registry.MarkStopped(transaction.PluginId);
            return;
        }

        if (restoredPlugin is not IAppStartupPlugin startupPlugin)
            throw new InvalidOperationException(
                $"Plugin {transaction.PluginId} cannot restore its started state.");
        startupPlugin.OnAppStarted();
        if (!_registry.MarkStarted(transaction.PluginId))
            throw new InvalidOperationException(
                $"Plugin {transaction.PluginId} could not be marked started after restoration.");
    }

    public IEnumerable<string> GetInstalledPluginIds()
    {
        using var commitRead = PluginInstallationCommitCoordinator.EnterRead();
        lock (_installationMarkerLock)
            return StateStore.InstalledExtensions.ToArray();
    }

    public async Task<bool> PermanentlyDeletePluginAsync(string pluginId)
    {
        // SECURITY: Validate plugin ID format
        if (!PathSecurity.IsValidPluginId(pluginId))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"PermanentlyDeletePluginAsync: Invalid plugin ID format: {pluginId}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        using var mutation = AcquirePluginMutation(pluginId);
        var trustCleanupAllowed = false;
        try
        {
            var unloadState = GetPluginRuntimeUnloadState(pluginId);
            if ((HasLiveRuntimeRegistration(pluginId) ||
                 unloadState is PluginRuntimeUnloadState.Active or
                     PluginRuntimeUnloadState.UnloadRequested) &&
                !ForgetPluginRuntime(pluginId, mutation))
            {
                return false;
            }
            if (GetPluginRuntimeUnloadState(pluginId) ==
                PluginRuntimeUnloadState.UnloadRequested)
            {
                return false;
            }
            _registry.Forget(pluginId);
            trustCleanupAllowed = true;

            // Get plugins directory
            var pluginsDirectory = _fileSystemManager.GetPluginsDirectory();
            if (!Directory.Exists(pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugins directory does not exist: {pluginsDirectory}");
                return false;
            }

            // Try to find plugin file by scanning all plugin DLLs and matching by ID
            var foundFiles = new List<string>();
            var pluginDirectoryToDelete = new List<string>();

            // Check subdirectories (plugins are often in their own folder)
            var subdirectories = Directory.GetDirectories(pluginsDirectory);
            var cultureFolders = _fileSystemManager.GetCultureFolders();

            foreach (var subdir in subdirectories)
            {
                var dirName = Path.GetFileName(subdir);
                if (cultureFolders.Contains(dirName))
                    continue;

                var directoriesToScan = new List<string> { subdir };

                // If this is the "local" directory, we need to scan its subdirectories
                if (dirName.Equals("local", StringComparison.OrdinalIgnoreCase))
                {
                    directoriesToScan.Clear();
                    directoriesToScan.AddRange(Directory.GetDirectories(subdir));
                }

                foreach (var scanDir in directoriesToScan)
                {
                    // Check all DLL files in this directory
                    var dllFiles = Directory.GetFiles(scanDir, "*.dll", SearchOption.TopDirectoryOnly)
                        .Where(f =>
                        {
                            var fileName = Path.GetFileName(f);
                            return PluginAssemblyNaming.HasPluginAssemblyPrefix(fileName) &&
                                   !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase);
                        });

                    foreach (var dllFile in dllFiles)
                    {
                        // SECURITY: Use filename-only matching to identify plugins during deletion.
                        // Assembly.LoadFrom was removed because it executes static constructors
                        // and loads arbitrary code into the current AppDomain — a security risk
                        // when the goal is only to locate files for deletion.
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);
                        if (fileName.EndsWith($".{pluginId}", StringComparison.OrdinalIgnoreCase) ||
                            PluginAssemblyNaming.EnumeratePrefixedPluginNames(pluginId)
                                .Any(name => fileName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            foundFiles.Add(dllFile);
                            pluginDirectoryToDelete.Add(scanDir);
                        }
                    }
                }
            }

            // Also check root plugins directory
            var rootDllFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(f =>
                {
                    var fileName = Path.GetFileName(f);
                    return PluginAssemblyNaming.HasPluginAssemblyPrefix(fileName) &&
                           !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase);
                });

            foreach (var dllFile in rootDllFiles)
            {
                // SECURITY: Use filename-only matching (see above for rationale).
                var fileName = Path.GetFileNameWithoutExtension(dllFile);
                if (fileName.EndsWith($".{pluginId}", StringComparison.OrdinalIgnoreCase) ||
                    PluginAssemblyNaming.EnumeratePrefixedPluginNames(pluginId)
                        .Any(name => fileName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    foundFiles.Add(dllFile);
                }
            }

            // Delete plugin directories (if entire directory contains only this plugin)
            foreach (var dir in pluginDirectoryToDelete.Distinct())
            {
                try
                {
                    // Check if directory only contains files related to this plugin
                    var allFiles = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                        .Where(f => !f.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var pluginBaseName = Path.GetFileNameWithoutExtension(foundFiles.FirstOrDefault(f => f.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) ?? "");
                    if (!string.IsNullOrEmpty(pluginBaseName) &&
                        allFiles.All(f => Path.GetFileName(f).StartsWith(pluginBaseName, StringComparison.OrdinalIgnoreCase)))
                    {
                        await _fileSystemManager.DeleteDirectoryWithRetryAsync(dir).ConfigureAwait(false);
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Deleted plugin directory: {dir}");
                        // Remove files from foundFiles list since directory is deleted
                        foundFiles.RemoveAll(f => f.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to delete plugin directory {dir}: {ex.Message}", ex);
                }
            }

            // Delete all found plugin files
            var deletedAny = false;
            foreach (var filePath in foundFiles)
            {
                var deleted = await _fileSystemManager.DeleteFileWithRetryAsync(filePath).ConfigureAwait(false);
                if (deleted)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Deleted plugin file: {filePath}");
                    deletedAny = true;

                    // Also delete related files (pdb, deps.json, etc.)
                    var basePath = Path.ChangeExtension(filePath, null);
                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        var relatedExtensions = new[] { ".pdb", ".deps.json", ".config" };
                        foreach (var ext in relatedExtensions)
                        {
                            var relatedFile = basePath + ext;
                            if (File.Exists(relatedFile))
                            {
                                await _fileSystemManager.DeleteFileWithRetryAsync(relatedFile).ConfigureAwait(false);
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Deleted related file: {relatedFile}");
                            }
                        }

                        // Also delete satellite assemblies (resource DLLs)
                        var cultureDirs = Directory.GetDirectories(dir);
                        foreach (var cultureDir in cultureDirs)
                        {
                            var cultureName = Path.GetFileName(cultureDir);
                            if (cultureFolders.Contains(cultureName))
                            {
                                var satelliteFiles = Directory.GetFiles(cultureDir, "*.*.resources.dll");
                                var pluginBaseName = Path.GetFileNameWithoutExtension(filePath);
                                foreach (var satelliteFile in satelliteFiles)
                                {
                                    var satelliteFileName = Path.GetFileNameWithoutExtension(satelliteFile);
                                    if (satelliteFileName.StartsWith(pluginBaseName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await _fileSystemManager.DeleteFileWithRetryAsync(satelliteFile).ConfigureAwait(false);
                                        if (Log.Instance.IsTraceEnabled)
                                            Log.Instance.Trace($"Deleted satellite assembly: {satelliteFile}");
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Failed to delete plugin file: {filePath}");
                }
            }

            return deletedAny;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error permanently deleting plugin {pluginId}: {ex.Message}", ex);
            return false;
        }
        finally
        {
            if (trustCleanupAllowed)
                TrustedPluginPackageStore.RemoveBestEffort(pluginId);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool HasLiveRuntimeRegistration(string pluginId) =>
        _registry.Get(pluginId) is not null and not PluginManifestAdapter;

    public bool CheckDependencies(string pluginId, out List<string> missingDependencies)
    {
        missingDependencies = new List<string>();
        
        var metadata = _registry.GetMetadata(pluginId);
        if (metadata?.Dependencies == null || metadata.Dependencies.Length == 0)
            return true;

        foreach (var depId in metadata.Dependencies)
        {
            if (!_registry.IsRegistered(depId) || !IsInstalled(depId))
            {
                missingDependencies.Add(depId);
            }
        }

        return missingDependencies.Count == 0;
    }

    private PluginRepositoryService? _pluginRepositoryService;

    /// <summary>
    /// Check for plugin updates (returns a dictionary of pluginId -> availableVersion)
    /// </summary>
    public async Task<Dictionary<string, string>> CheckForUpdatesAsync()
    {
        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Checking for plugin updates...");
            
            // Get all registered plugins
            var plugins = _registry.GetAll();
            
            // Build installed plugin manifests for update check
            var installedManifests = new List<PluginManifest>();
            foreach (var plugin in plugins)
            {
                var metadata = _registry.GetMetadata(plugin.Id);
                installedManifests.Add(new PluginManifest
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    Version = PluginVersionParser.ResolveInstalledVersion(plugin.Id, metadata?.Version) ?? "0.0.0",
                    Icon = plugin.Icon,
                    IsSystemPlugin = plugin.IsSystemPlugin
                });
            }
            
            // Delegate to PluginRepositoryService for actual update checking
            _pluginRepositoryService ??= IoCContainer.Resolve<PluginRepositoryService>();
            var availableUpdates = await _pluginRepositoryService
                .CheckForUpdatesAsync(installedManifests)
                .ConfigureAwait(false);
            
            // Map List<PluginManifest> to Dictionary<string, string> (pluginId -> version)
            foreach (var manifest in availableUpdates)
            {
                if (!string.IsNullOrWhiteSpace(manifest.Id) && !string.IsNullOrWhiteSpace(manifest.Version))
                    updates[manifest.Id] = manifest.Version;
            }
            
            Log.Instance.Info($"Update check complete. Found {updates.Count} available updates.");
        }
        catch (Exception ex)
        {
            Log.Instance.Error("Error checking for plugin updates", ex);
        }
        
        return updates;
    }

    /// <summary>
    /// Perform actual deletion of plugins marked for deletion (call on app exit)
    /// </summary>
    public async Task PerformPendingDeletionsAsync()
    {
        List<string> pendingDeletions;
        lock (_installationMarkerLock)
            pendingDeletions = StateStore.PendingDeletionExtensions.ToList();

        if (!pendingDeletions.Any())
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"No pending plugin deletions to process.");
            return;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Processing {pendingDeletions.Count} pending plugin deletion(s).");

        foreach (var pluginId in pendingDeletions)
        {
            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Deleting plugin files for: {pluginId}");

                await PermanentlyDeletePluginAsync(pluginId).ConfigureAwait(false);

                // Remove from pending deletions list
                lock (_installationMarkerLock)
                {
                    StateStore.PendingDeletionExtensions.RemoveAll(
                        id => StringComparer.OrdinalIgnoreCase.Equals(id, pluginId));
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to delete plugin {pluginId}: {ex.Message}", ex);
            }
        }

        // Save settings after processing all deletions
        lock (_installationMarkerLock)
            SynchronizeStateStore();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Pending plugin deletions completed.");
    }

    public void UnloadAllPlugins()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unloading all plugins...");

            // Use registry.Clear() which triggers OnUninstalled for all plugins
            var pluginIds = _registry.GetAll().Select(plugin => plugin.Id).ToList();
            _registry.Clear();
            foreach (var pluginId in pluginIds)
                RequestLoaderUnload(pluginId);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"All plugins unloaded successfully");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error unloading plugins: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public bool StopPlugin(string pluginId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return false;

            using var mutation = AcquirePluginMutation(pluginId);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping plugin: {pluginId}");

            var unloadResult = StopRuntimeAndRequestUnload(pluginId);
            if (unloadResult == RuntimeUnloadRequestResult.RefusedBeforeRequest)
                return false;
            if (unloadResult == RuntimeUnloadRequestResult.Pending &&
                !ConfirmReleasedRuntimeUnload(pluginId))
            {
                TransitionLifecycleState(
                    pluginId,
                    PluginState.Installed,
                    legacyIsInstalled: true);
                return false;
            }
            FinalizeRuntimeUnloadBookkeeping(pluginId);

            // Drive the state machine Enabled -> Installed. We deliberately
            // do not block on the result: a plugin that was never started
            // (state NotInstalled) should not be force-transitioned to
            // Installed just because Stop was called. The state machine logs
            // any illegal transition attempt at trace level.
            TransitionLifecycleState(pluginId, PluginState.Installed, legacyIsInstalled: true);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin {pluginId} stopped successfully");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping plugin {pluginId}: {ex.Message}", ex);
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private RuntimeUnloadRequestResult StopRuntimeAndRequestUnload(string pluginId)
    {
        var plugin = _registry.Get(pluginId);
        if (plugin is null)
        {
            var unloadState = GetPluginRuntimeUnloadState(pluginId);
            if (unloadState == PluginRuntimeUnloadState.UnloadRequested)
                return RuntimeUnloadRequestResult.Pending;
            if (unloadState != PluginRuntimeUnloadState.Active)
                return RuntimeUnloadRequestResult.RefusedBeforeRequest;
            return IsUnloadRequestAccepted(RequestLoaderUnload(pluginId))
                ? RuntimeUnloadRequestResult.Pending
                : RuntimeUnloadRequestResult.RefusedBeforeRequest;
        }

        var wasStarted = _registry.IsStarted(pluginId);
        var metadata = _registry.GetMetadata(pluginId);
        plugin.Stop();
        _registry.MarkStopped(pluginId);
        ReleaseUninstallRegistryReference(pluginId);
        if (IsUnloadRequestAccepted(RequestLoaderUnload(pluginId)))
            return RuntimeUnloadRequestResult.Pending;

        if (metadata is not null)
            _registry.Register(plugin, metadata);
        if (wasStarted)
            RestoreStartedStateAfterUnloadRefusal(pluginId, plugin);
        return RuntimeUnloadRequestResult.RefusedBeforeRequest;
    }

    /// <inheritdoc/>
    public void StopAllPlugins()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping all plugins...");

            foreach (var pluginId in CaptureRegisteredPluginIds())
            {
                StopPlugin(pluginId);
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"All plugins stopped successfully");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping all plugins: {ex.Message}", ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private string[] CaptureRegisteredPluginIds() =>
        _registry.GetAll().Select(plugin => plugin.Id).ToArray();

    protected virtual void OnPluginStateChanged(string pluginId, bool isInstalled)
    {
        PluginStateChanged?.Invoke(this, new PluginEventArgs(pluginId, isInstalled));
    }

    /// <summary>
    /// Check if the current host version meets the plugin's minimum requirements
    /// </summary>
    private static bool IsVersionCompatible(string minimumHostVersion)
    {
        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion == null)
                return false;

            if (!Version.TryParse(minimumHostVersion, out var minVersion))
                return false; // Reject unparseable version strings (secure default)

            return currentVersion >= minVersion;
        }
        catch
        {
            return false; // Reject on any check failure (secure default)
        }
    }

    private static int ComparePluginVersions(string? left, string? right)
    {
        if (PluginVersionParser.TryParse(left, out var leftVersion) &&
            PluginVersionParser.TryParse(right, out var rightVersion))
            return leftVersion.CompareTo(rightVersion);

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
