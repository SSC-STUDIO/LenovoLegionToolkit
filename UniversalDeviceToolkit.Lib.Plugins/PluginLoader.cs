using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin loader interface
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Load a plugin from a DLL file asynchronously
    /// </summary>
    Task<IPlugin?> LoadFromFileAsync(string dllPath, IPluginSignatureValidator signatureValidator);

    /// <summary>
    /// Request unload of a previously loaded plugin assembly context without forcing collection.
    /// </summary>
    PluginUnloadRequestResult RequestUnload(string pluginId);

    /// <summary>
    /// Confirm collection of a previously requested plugin assembly context.
    /// </summary>
    PluginUnloadConfirmationResult ConfirmUnload(string pluginId);

    /// <summary>
    /// Compatibility shim for third-party loaders. New code must use request and confirmation.
    /// </summary>
    bool Unload(string pluginId);

    /// <summary>
    /// Check if a DLL file can be loaded as a plugin
    /// </summary>
    bool CanLoad(string filePath, string? parentDirectoryName = null);
}

/// <summary>
/// Observable state of a collectible plugin runtime.
/// </summary>
public enum PluginRuntimeUnloadState
{
    NotTracked,
    Active,
    UnloadRequested,
    ConfirmedCollected,
}

public enum PluginUnloadRequestResult
{
    NotTracked,
    Requested,
    AlreadyRequested,
    Failed,
}

public enum PluginUnloadConfirmationResult
{
    NotTracked,
    NotRequested,
    Pending,
    Confirmed,
}

/// <summary>
/// Exposes whether a failed unload was refused before the request or is still pending collection.
/// </summary>
public interface IPluginRuntimeUnloadStateProvider
{
    PluginRuntimeUnloadState GetUnloadState(string pluginId);
}

internal interface ITransactionalPluginLoader
{
    bool CommitCandidate(IPlugin plugin);

    PluginCandidateUnloadToken DiscardCandidate(IPlugin plugin);

    bool ConfirmDiscardedCandidate(PluginCandidateUnloadToken token);

    PluginCandidateUnloadSweepResult SweepDiscardedCandidates();

    PluginCandidateUnloadSweepResult RecoverDiscardedCandidates();

    int PendingDiscardedCandidateCount { get; }
}

internal readonly record struct PluginCandidateUnloadToken(Guid Value);
internal readonly record struct PluginCandidateUnloadSweepResult(int Confirmed, int Pending);

internal sealed class PluginLoadContextUnloadPendingException(
    PluginCandidateUnloadToken token) : Exception
{
    public PluginCandidateUnloadToken Token { get; } = token;
}

/// <summary>
/// Plugin loader implementation
/// Handles loading plugin assemblies and creating plugin instances
/// </summary>
public class PluginLoader : IPluginLoader, ITransactionalPluginLoader, IPluginRuntimeUnloadStateProvider
{
    private const int MaxTrackedDiscardedContexts = 128;
    private const int ScheduledDiscardedSweepBatchSize = 8;
    private const int ExplicitDiscardedRecoveryBatchSize = 16;
    private sealed record PendingPluginRuntime(
        PluginAssemblyLoadContext LoadContext,
        RegisteredPluginDependencyResolutionContext? DependencyContext);

    private sealed class TrackedPluginRuntime(
        PluginAssemblyLoadContext loadContext,
        PluginDependencyResolutionContext? dependencyContext)
    {
        public object SyncRoot { get; } = new();
        public PluginAssemblyLoadContext? LoadContext { get; set; } = loadContext;
        public WeakReference? LoadContextWeakReference { get; set; }
        public PluginDependencyResolutionContext? DependencyContext { get; set; } = dependencyContext;
        public PluginRuntimeUnloadState State { get; set; } = PluginRuntimeUnloadState.Active;
    }

    private sealed class DiscardedPluginRuntime(WeakReference loadContextWeakReference)
    {
        public object SyncRoot { get; } = new();
        public WeakReference LoadContextWeakReference { get; } = loadContextWeakReference;
        public PluginRuntimeUnloadState State { get; set; } =
            PluginRuntimeUnloadState.UnloadRequested;
        public long NextScheduledCheckUtcTicks { get; set; }
        public int ScheduledCheckCount { get; set; }
        public int ProcessingState;
    }

    private static readonly ConcurrentDictionary<string, PluginDependencyResolutionContext> DependencyResolutionContexts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, PluginDependencyResolutionContext> PluginDependencyContexts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, TrackedPluginRuntime> PluginLoadContexts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte> RejectedDependencyPaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<IPlugin, PendingPluginRuntime> PendingPluginRuntimes =
        new(ReferenceEqualityComparer.Instance);
    private static readonly ConcurrentDictionary<Guid, DiscardedPluginRuntime> DiscardedPluginLoadContexts = new();
    private static readonly object DiscardedQueueLock = new();
    private static readonly LinkedList<Guid> DiscardedPluginLoadContextQueue = new();
    private static readonly Dictionary<Guid, LinkedListNode<Guid>>
        DiscardedPluginLoadContextMembership = new();
    private static readonly object DependencyResolverRegistrationLock = new();
    private static bool _dependencyResolverRegistered;

    internal static int TrackedContextCount => PluginLoadContexts.Count;

    internal static int PendingContextCount => PendingPluginRuntimes.Count;

    internal static int DiscardedContextCount => DiscardedPluginLoadContexts.Count;
    internal static int DiscardedQueueCount
    {
        get
        {
            lock (DiscardedQueueLock)
                return DiscardedPluginLoadContextMembership.Count;
        }
    }
    internal static int DiscardedForcedCollectionCount =>
        global::System.Threading.Volatile.Read(ref _discardedForcedCollectionCount);
    internal static int DiscardedSweepPassCount =>
        global::System.Threading.Volatile.Read(ref _discardedSweepPassCount);
    internal static int DiscardedScheduledCheckCount =>
        global::System.Threading.Volatile.Read(ref _discardedScheduledCheckCount);
    private static int _discardedForcedCollectionCount;
    private static int _discardedSweepPassCount;
    private static int _discardedScheduledCheckCount;
    private static int _discardedSweepActive;

    public PluginLoader()
    {
    }

    private readonly HashSet<string> _cultureFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu", "it", "ja", "ko",
        "lv", "nl-NL", "pl", "pt", "pt-BR", "ro", "ru", "sk", "tr", "uk", "uz-Latn-UZ",
        "vi", "zh-Hans", "zh-Hant", "tools"
    };

    /// <summary>
    /// Load a plugin from a DLL file asynchronously
    /// </summary>
    public async Task<IPlugin?> LoadFromFileAsync(string dllPath, IPluginSignatureValidator signatureValidator)
    {
        if (DiscardedPluginLoadContexts.Count >= MaxTrackedDiscardedContexts)
        {
            throw new InvalidOperationException(
                $"Plugin candidate loading is blocked by {DiscardedPluginLoadContexts.Count} uncollected discarded contexts.");
        }

        if (string.IsNullOrWhiteSpace(dllPath))
        {
            Log.Instance.Warning("LoadFromFileAsync: DLL path is empty");
            return null;
        }

        if (!File.Exists(dllPath))
        {
            Log.Instance.Warning($"LoadFromFileAsync: DLL file not found: {dllPath}");
            return null;
        }

        RegisteredPluginDependencyResolutionContext? registeredDependencyContext = null;
        var keepDependencyContext = false;

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Attempting to load plugin from: {dllPath}");

            var normalizedDllPath = Path.GetFullPath(dllPath);
            var pluginDirectory = Path.GetDirectoryName(normalizedDllPath);

            // Register AssemblyResolve handler early to handle dependencies that may be loaded
            // during signature validation or assembly loading.
            if (!string.IsNullOrWhiteSpace(pluginDirectory))
            {
                registeredDependencyContext = RegisterPluginDependencyResolutionContext(normalizedDllPath, pluginDirectory, signatureValidator);
            }

            // Validate plugin signature before loading (security check)
            var signatureResult = await signatureValidator.ValidateAsync(dllPath).ConfigureAwait(false);
            if (!signatureResult.IsValid)
            {
                Log.Instance.Warning($"Plugin signature validation failed for {dllPath}. Status: {signatureResult.Status}, Error: {signatureResult.ErrorMessage}");
                return null;
            }

            var assemblyBytes = await File
                .ReadAllBytesAsync(normalizedDllPath)
                .ConfigureAwait(false);
            var loadAttempt = CreatePluginCandidate(
                normalizedDllPath,
                pluginDirectory ?? string.Empty,
                signatureValidator,
                registeredDependencyContext,
                assemblyBytes);
            if (loadAttempt.Plugin is not null)
            {
                keepDependencyContext = true;
                Log.Instance.Info(
                    $"Successfully created plugin instance: {loadAttempt.Plugin.Id} ({loadAttempt.Plugin.Name}) from {dllPath}");
                return loadAttempt.Plugin;
            }

            if (loadAttempt.RejectedLoadContext is not null)
            {
                TrackDiscardedLoadContext(loadAttempt.RejectedLoadContext);
            }
            return null;
        }
        catch (PluginLoadContextUnloadPendingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to load plugin assembly from {dllPath}", ex);
            return null;
        }
        finally
        {
            if (!keepDependencyContext && registeredDependencyContext is { IsNew: true })
                RemovePluginDependencyResolutionContext(registeredDependencyContext.Context);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PluginCandidateLoadAttempt CreatePluginCandidate(
        string normalizedDllPath,
        string pluginDirectory,
        IPluginSignatureValidator signatureValidator,
        RegisteredPluginDependencyResolutionContext? registeredDependencyContext,
        byte[] assemblyBytes)
    {
        PluginAssemblyLoadContext? pluginLoadContext = null;
        try
        {
            pluginLoadContext = new PluginAssemblyLoadContext(
                normalizedDllPath,
                pluginDirectory,
                signatureValidator);
            var assembly = pluginLoadContext.LoadFromStream(new MemoryStream(assemblyBytes));
            registeredDependencyContext?.Context.SetPluginMainAssembly(assembly);

            Type[] pluginTypes;
            try
            {
                pluginTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Instance.Warning($"Failed to get types from assembly {normalizedDllPath}. Loader exceptions:");
                if (ex.LoaderExceptions != null)
                {
                    foreach (var loaderException in ex.LoaderExceptions)
                        Log.Instance.Warning($"  - {loaderException?.Message}", loaderException);
                }
                pluginTypes = ex.Types.Where(type => type != null).OfType<Type>().ToArray();
            }

            var validPluginTypes = pluginTypes
                .Where(type => IsPluginTypeCandidate(type)
                               && !type.IsInterface
                               && !type.IsAbstract
                               && type.GetConstructor(Type.EmptyTypes) != null)
                .ToArray();
            if (validPluginTypes.Length == 0)
            {
                Log.Instance.Warning($"No valid plugin types found in {normalizedDllPath}");
                return RejectPluginCandidate(
                    pluginLoadContext,
                    registeredDependencyContext);
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Found {validPluginTypes.Length} plugin type(s) in {normalizedDllPath}");

            foreach (var pluginType in validPluginTypes)
            {
                try
                {
                    var plugin = CreatePluginInstance(pluginType, normalizedDllPath);
                    if (plugin is null)
                        continue;
                    if (string.IsNullOrWhiteSpace(plugin.Id))
                    {
                        Log.Instance.Warning(
                            $"Plugin from {normalizedDllPath} has empty ID, skipping");
                        continue;
                    }

                    PendingPluginRuntimes[plugin] = new PendingPluginRuntime(
                        pluginLoadContext,
                        registeredDependencyContext);
                    return new PluginCandidateLoadAttempt(plugin, null);
                }
                catch (Exception ex)
                {
                    Log.Instance.Error(
                        $"Failed to create instance of plugin type {pluginType.Name} from {normalizedDllPath}",
                        ex);
                }
            }

            return RejectPluginCandidate(
                pluginLoadContext,
                registeredDependencyContext);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to load assembly from {normalizedDllPath}", ex);
            return pluginLoadContext is null
                ? new PluginCandidateLoadAttempt(null, null)
                : RejectPluginCandidate(pluginLoadContext, registeredDependencyContext);
        }
    }

    private static PluginCandidateLoadAttempt RejectPluginCandidate(
        PluginAssemblyLoadContext loadContext,
        RegisteredPluginDependencyResolutionContext? registeredDependencyContext)
    {
        registeredDependencyContext?.Context.ReleasePluginMainAssembly();
        return new PluginCandidateLoadAttempt(
            null,
            RequestCollectibleUnload(loadContext));
    }

    private static PluginCandidateUnloadToken TrackDiscardedLoadContext(
        WeakReference weakReference)
    {
        var token = new PluginCandidateUnloadToken(Guid.NewGuid());
        if (!DiscardedPluginLoadContexts.TryAdd(
                token.Value,
                new DiscardedPluginRuntime(weakReference)))
            throw new InvalidOperationException("Could not track rejected plugin load context.");
        AddDiscardedQueueMembership(token.Value);
        return token;
    }

    private static bool ConfirmDiscardedLoadContext(
        PluginCandidateUnloadToken token)
    {
        if (!DiscardedPluginLoadContexts.TryGetValue(token.Value, out var discarded))
            return true;
        if (global::System.Threading.Interlocked.CompareExchange(
                ref discarded.ProcessingState,
                1,
                0) != 0)
        {
            return false;
        }
        try
        {
            lock (discarded.SyncRoot)
            {
                if (discarded.State == PluginRuntimeUnloadState.UnloadRequested &&
                    discarded.LoadContextWeakReference.IsAlive)
                {
                    return false;
                }

                discarded.State = PluginRuntimeUnloadState.ConfirmedCollected;
                var removed = DiscardedPluginLoadContexts.TryRemove(
                    new KeyValuePair<Guid, DiscardedPluginRuntime>(
                        token.Value,
                        discarded));
                if (removed)
                    RemoveDiscardedQueueMembership(token.Value);
                return removed;
            }
        }
        finally
        {
            global::System.Threading.Volatile.Write(ref discarded.ProcessingState, 0);
        }
    }

    private sealed record PluginCandidateLoadAttempt(
        IPlugin? Plugin,
        WeakReference? RejectedLoadContext);

    bool ITransactionalPluginLoader.CommitCandidate(IPlugin plugin)
    {
        if (!PendingPluginRuntimes.TryGetValue(plugin, out var pending))
            return false;

        var tracked = new TrackedPluginRuntime(
            pending.LoadContext,
            pending.DependencyContext?.Context);
        if (!PluginLoadContexts.TryAdd(plugin.Id, tracked))
            return false;

        if (pending.DependencyContext is not null &&
            !PluginDependencyContexts.TryAdd(
                plugin.Id,
                pending.DependencyContext.Context))
        {
            PluginLoadContexts.TryRemove(
                new KeyValuePair<string, TrackedPluginRuntime>(plugin.Id, tracked));
            return false;
        }

        PendingPluginRuntimes.TryRemove(
            new KeyValuePair<IPlugin, PendingPluginRuntime>(plugin, pending));
        return true;
    }

    PluginCandidateUnloadToken ITransactionalPluginLoader.DiscardCandidate(IPlugin plugin)
    {
        if (!PendingPluginRuntimes.TryRemove(plugin, out var pending))
            return default;

        return DiscardPendingRuntime(pending);
    }

    bool ITransactionalPluginLoader.ConfirmDiscardedCandidate(
        PluginCandidateUnloadToken token)
    {
        if (token.Value == Guid.Empty)
            return true;
        return ConfirmDiscardedLoadContext(token);
    }

    int ITransactionalPluginLoader.PendingDiscardedCandidateCount =>
        DiscardedPluginLoadContexts.Count;

    PluginCandidateUnloadSweepResult ITransactionalPluginLoader.SweepDiscardedCandidates() =>
        SweepDiscardedLoadContexts(
            ScheduledDiscardedSweepBatchSize,
            requestCollection: false,
            respectBackoff: true);

    PluginCandidateUnloadSweepResult ITransactionalPluginLoader.RecoverDiscardedCandidates() =>
        SweepDiscardedLoadContexts(
            ExplicitDiscardedRecoveryBatchSize,
            requestCollection: true,
            respectBackoff: false);

    private static PluginCandidateUnloadSweepResult SweepDiscardedLoadContexts(
        int batchSize,
        bool requestCollection,
        bool respectBackoff)
    {
        if (global::System.Threading.Interlocked.CompareExchange(
                ref _discardedSweepActive,
                1,
                0) != 0)
        {
            return new PluginCandidateUnloadSweepResult(
                0,
                DiscardedPluginLoadContexts.Count);
        }
        try
        {
        global::System.Threading.Interlocked.Increment(ref _discardedSweepPassCount);
        if (requestCollection && !DiscardedPluginLoadContexts.IsEmpty)
        {
            global::System.Threading.Interlocked.Increment(
                ref _discardedForcedCollectionCount);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
        }
        var confirmed = 0;
        var processed = 0;
        var dequeueBudget = Math.Min(
            GetDiscardedQueueCount(),
            batchSize * 2);
        var nowTicks = DateTime.UtcNow.Ticks;
        while (processed < batchSize &&
               dequeueBudget-- > 0 &&
               TryTakeDiscardedQueueHead(out var token))
        {
            if (!DiscardedPluginLoadContexts.TryGetValue(token, out var discarded))
                continue;

            processed++;
            if (respectBackoff && discarded.NextScheduledCheckUtcTicks > nowTicks)
            {
                AddDiscardedQueueMembership(token);
                continue;
            }
            if (respectBackoff)
            {
                global::System.Threading.Interlocked.Increment(
                    ref _discardedScheduledCheckCount);
            }
            if (ConfirmDiscardedLoadContext(
                    new PluginCandidateUnloadToken(token)))
            {
                confirmed++;
                continue;
            }

            if (respectBackoff)
            {
                lock (discarded.SyncRoot)
                {
                    discarded.ScheduledCheckCount++;
                    var delayMilliseconds = Math.Min(
                        5000,
                        50 * (1 << Math.Min(discarded.ScheduledCheckCount, 6)));
                    discarded.NextScheduledCheckUtcTicks =
                        DateTime.UtcNow.AddMilliseconds(delayMilliseconds).Ticks;
                }
            }
            if (DiscardedPluginLoadContexts.TryGetValue(token, out var retained) &&
                ReferenceEquals(retained, discarded))
            {
                AddDiscardedQueueMembership(token);
            }
        }
        return new PluginCandidateUnloadSweepResult(
            confirmed,
            DiscardedPluginLoadContexts.Count);
        }
        finally
        {
            global::System.Threading.Volatile.Write(ref _discardedSweepActive, 0);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PluginCandidateUnloadToken DiscardPendingRuntime(PendingPluginRuntime pending)
    {
        if (pending.DependencyContext is { IsNew: true })
        {
            pending.DependencyContext.Context.ReleasePluginMainAssembly();
            RemovePluginDependencyResolutionContext(pending.DependencyContext.Context);
        }

        var weakReference = RequestCollectibleUnload(pending.LoadContext);
        var token = new PluginCandidateUnloadToken(Guid.NewGuid());
        if (!DiscardedPluginLoadContexts.TryAdd(
                token.Value,
                new DiscardedPluginRuntime(weakReference)))
            throw new InvalidOperationException("Could not track discarded plugin load context.");
        AddDiscardedQueueMembership(token.Value);
        return token;
    }

    private static int GetDiscardedQueueCount()
    {
        lock (DiscardedQueueLock)
            return DiscardedPluginLoadContextMembership.Count;
    }

    private static void AddDiscardedQueueMembership(Guid token)
    {
        lock (DiscardedQueueLock)
        {
            if (DiscardedPluginLoadContextMembership.ContainsKey(token))
                return;
            var node = DiscardedPluginLoadContextQueue.AddLast(token);
            DiscardedPluginLoadContextMembership.Add(token, node);
        }
    }

    private static void RemoveDiscardedQueueMembership(Guid token)
    {
        lock (DiscardedQueueLock)
        {
            if (!DiscardedPluginLoadContextMembership.Remove(token, out var node))
                return;
            DiscardedPluginLoadContextQueue.Remove(node);
        }
    }

    private static bool TryTakeDiscardedQueueHead(out Guid token)
    {
        lock (DiscardedQueueLock)
        {
            var node = DiscardedPluginLoadContextQueue.First;
            if (node is null)
            {
                token = default;
                return false;
            }
            token = node.Value;
            DiscardedPluginLoadContextQueue.RemoveFirst();
            DiscardedPluginLoadContextMembership.Remove(token);
            return true;
        }
    }

    public PluginUnloadRequestResult RequestUnload(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return PluginUnloadRequestResult.NotTracked;

        if (!PluginLoadContexts.TryGetValue(pluginId, out var tracked))
            return PluginUnloadRequestResult.NotTracked;

        lock (tracked.SyncRoot)
        {
            if (tracked.State == PluginRuntimeUnloadState.UnloadRequested ||
                tracked.State == PluginRuntimeUnloadState.ConfirmedCollected)
                return PluginUnloadRequestResult.AlreadyRequested;

            if (!TryRequestUnload(pluginId, tracked))
                return PluginUnloadRequestResult.Failed;
            TryReleaseDependencyReferences(pluginId, tracked);
            return PluginUnloadRequestResult.Requested;
        }
    }

    public PluginUnloadConfirmationResult ConfirmUnload(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId) ||
            !PluginLoadContexts.TryGetValue(pluginId, out var tracked))
        {
            return PluginUnloadConfirmationResult.NotTracked;
        }

        lock (tracked.SyncRoot)
        {
            if (tracked.State == PluginRuntimeUnloadState.Active)
                return PluginUnloadConfirmationResult.NotRequested;
            if (tracked.State == PluginRuntimeUnloadState.UnloadRequested)
            {
                var weakReference = tracked.LoadContextWeakReference;
                if (weakReference is null || !ConfirmUnloadCollected(weakReference))
                    return PluginUnloadConfirmationResult.Pending;
                tracked.State = PluginRuntimeUnloadState.ConfirmedCollected;
            }

            if (!PluginLoadContexts.TryRemove(
                    new KeyValuePair<string, TrackedPluginRuntime>(pluginId, tracked)))
            {
                return PluginUnloadConfirmationResult.Pending;
            }

            return PluginUnloadConfirmationResult.Confirmed;
        }
    }

    public bool Unload(string pluginId)
    {
        var request = RequestUnload(pluginId);
        if (request is PluginUnloadRequestResult.Failed or
            PluginUnloadRequestResult.NotTracked)
        {
            return false;
        }
        return ConfirmUnload(pluginId) is
            PluginUnloadConfirmationResult.Confirmed or
            PluginUnloadConfirmationResult.NotTracked;
    }

    public PluginRuntimeUnloadState GetUnloadState(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId) ||
            !PluginLoadContexts.TryGetValue(pluginId, out var tracked))
        {
            return PluginRuntimeUnloadState.NotTracked;
        }

        lock (tracked.SyncRoot)
            return tracked.State;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryRequestUnload(string pluginId, TrackedPluginRuntime tracked)
    {
        var loadContext = tracked.LoadContext;
        if (loadContext is null)
            return false;

        try
        {
            var weakReference = RequestCollectibleUnload(loadContext);
            tracked.LoadContextWeakReference = weakReference;
            tracked.LoadContext = null;
            tracked.State = PluginRuntimeUnloadState.UnloadRequested;
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to request plugin load context unload for {pluginId}: {ex.Message}", ex);
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RequestCollectibleUnload(PluginAssemblyLoadContext loadContext)
    {
        var weakReference = new WeakReference(loadContext, trackResurrection: false);
        loadContext.Unload();
        return weakReference;
    }

    private static bool TryReleaseDependencyReferences(
        string pluginId,
        TrackedPluginRuntime tracked)
    {
        var dependencyContext = tracked.DependencyContext;
        if (dependencyContext is null)
            return true;

        try
        {
            dependencyContext.ReleasePluginMainAssembly();
            RemovePluginDependencyResolutionContext(dependencyContext);
            PluginDependencyContexts.TryRemove(
                new KeyValuePair<string, PluginDependencyResolutionContext>(
                    pluginId,
                    dependencyContext));
            tracked.DependencyContext = null;
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to release plugin dependency resolution context for {pluginId}: {ex.Message}", ex);
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ConfirmUnloadCollected(WeakReference loadContextWeakReference)
    {
        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts && loadContextWeakReference.IsAlive; attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            if (loadContextWeakReference.IsAlive)
                Thread.Yield();
        }

        return !loadContextWeakReference.IsAlive;
    }

    private static RegisteredPluginDependencyResolutionContext RegisterPluginDependencyResolutionContext(
        string pluginMainAssemblyPath,
        string pluginDirectory,
        IPluginSignatureValidator signatureValidator)
    {
        var normalizedMainAssemblyPath = Path.GetFullPath(pluginMainAssemblyPath);
        var normalizedPluginDirectory = Path.GetFullPath(pluginDirectory);

        var isNew = DependencyResolutionContexts.TryAdd(normalizedMainAssemblyPath,
            new PluginDependencyResolutionContext(
                normalizedMainAssemblyPath,
                normalizedPluginDirectory,
                signatureValidator));

        var context = DependencyResolutionContexts[normalizedMainAssemblyPath];

        lock (DependencyResolverRegistrationLock)
        {
            if (!_dependencyResolverRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolvePluginDependencyAssembly;
                _dependencyResolverRegistered = true;
            }
        }

        return new RegisteredPluginDependencyResolutionContext(context, isNew);
    }

    private static void RemovePluginDependencyResolutionContext(PluginDependencyResolutionContext context)
    {
        DependencyResolutionContexts.TryRemove(
            new KeyValuePair<string, PluginDependencyResolutionContext>(
                context.PluginMainAssemblyPath,
                context));

        lock (DependencyResolverRegistrationLock)
        {
            if (DependencyResolutionContexts.IsEmpty && _dependencyResolverRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolvePluginDependencyAssembly;
                _dependencyResolverRegistered = false;
            }
        }
    }

    private static Assembly? ResolvePluginDependencyAssembly(object? sender, ResolveEventArgs args)
    {
        var contexts = GetScopedDependencyResolutionContexts(args.RequestingAssembly);

        foreach (var context in contexts)
        {
            var assembly = ResolvePluginDependencyAssembly(
                args.Name,
                context.PluginMainAssemblyPath,
                context.PluginDirectory,
                context.SignatureValidator);
            if (assembly != null)
                return assembly;
        }

        return null;
    }

    private static Assembly? ResolvePluginDependencyAssembly(
        string requestedAssemblyFullName,
        string pluginMainAssemblyPath,
        string pluginDirectory,
        IPluginSignatureValidator signatureValidator)
    {
        try
        {
            var requestedAssemblyName = new AssemblyName(requestedAssemblyFullName);
            var assemblyName = requestedAssemblyName.Name;
            if (string.IsNullOrWhiteSpace(assemblyName) || !IsSafeAssemblyName(assemblyName))
                return null;

            // Try to find a version-compatible loaded assembly
            // Compare name, version, and public key token for proper binding
            var loadedAssembly = FindCompatibleLoadedAssembly(requestedAssemblyName, pluginDirectory);
            if (loadedAssembly != null)
                return loadedAssembly;

            var candidatePath = GetPluginAssemblyCandidatePath(requestedAssemblyName, pluginMainAssemblyPath, pluginDirectory);
            if (string.IsNullOrWhiteSpace(candidatePath) || !File.Exists(candidatePath))
                return null;

            var normalizedCandidatePath = Path.GetFullPath(candidatePath);
            if (RejectedDependencyPaths.ContainsKey(normalizedCandidatePath))
                return null;

            // Note: AssemblyResolve event handlers must return synchronously.
            // We use GetAwaiter().GetResult() here because the event signature requires a synchronous return.
            // This is a known limitation of AppDomain.AssemblyResolve - the alternative would be
            // to skip signature validation for dependencies, which is a security risk.
            // AssemblyResolve requires synchronous return
            var signatureResult = signatureValidator.ValidateAsync(candidatePath).GetAwaiter().GetResult();
            if (!IsValidPluginDependencySignature(signatureResult, requestedAssemblyName, candidatePath))
            {
                RejectedDependencyPaths.TryAdd(normalizedCandidatePath, 0);
                Log.Instance.Warning($"Rejected plugin dependency due to invalid signature. [path={candidatePath}, status={signatureResult.Status}, error={signatureResult.ErrorMessage}]");
                return null;
            }

            return Assembly.LoadFrom(candidatePath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to resolve plugin dependency assembly {requestedAssemblyFullName}.", ex);
            return null;
        }
    }

    private static bool IsValidPluginDependencySignature(
        PluginSignatureResult signatureResult,
        AssemblyName requestedAssemblyName,
        string candidatePath)
    {
        if (signatureResult.IsValid)
            return true;

        if (signatureResult.Status != PluginSignatureStatus.Expired ||
            signatureResult.Certificate == null ||
            !IsMicrosoftSignedStrongNamedDependency(signatureResult.Certificate, requestedAssemblyName, candidatePath))
        {
            return false;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Allowing expired but trusted Microsoft-signed plugin dependency. [path={candidatePath}, expires={signatureResult.ExpirationDate:O}]");

        return true;
    }

    private static bool IsMicrosoftSignedStrongNamedDependency(
        X509Certificate2 certificate,
        AssemblyName requestedAssemblyName,
        string candidatePath)
    {
        var requestedPublicKeyToken = requestedAssemblyName.GetPublicKeyToken();
        if (requestedPublicKeyToken == null || requestedPublicKeyToken.Length == 0)
            return false;

        if (!IsCandidateAssemblyIdentityCompatible(candidatePath, requestedAssemblyName, requestedPublicKeyToken))
            return false;

        if (!CertificateLooksMicrosoftOwned(certificate))
            return false;

        return IsCertificateTrustedIgnoringExpiration(certificate);
    }

    private static bool IsCandidateAssemblyIdentityCompatible(
        string candidatePath,
        AssemblyName requestedAssemblyName,
        byte[] requestedPublicKeyToken)
    {
        try
        {
            var candidateAssemblyName = AssemblyName.GetAssemblyName(candidatePath);
            if (!string.Equals(candidateAssemblyName.Name, requestedAssemblyName.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            if (requestedAssemblyName.Version != null &&
                candidateAssemblyName.Version != null &&
                candidateAssemblyName.Version < requestedAssemblyName.Version)
            {
                return false;
            }

            var candidatePublicKeyToken = candidateAssemblyName.GetPublicKeyToken();
            return candidatePublicKeyToken != null &&
                   candidatePublicKeyToken.SequenceEqual(requestedPublicKeyToken);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-loader-pubkey-token",
                "Failed to compare assembly public key tokens during plugin load.",
                ex);
            return false;
        }
    }

    private static bool CertificateLooksMicrosoftOwned(X509Certificate2 certificate)
    {
        return ContainsMicrosoftCorporation(certificate.Subject) ||
               ContainsMicrosoftCorporation(certificate.Issuer);
    }

    private static bool ContainsMicrosoftCorporation(string value) =>
        value.Contains("Microsoft Corporation", StringComparison.OrdinalIgnoreCase);

    private static bool IsCertificateTrustedIgnoringExpiration(X509Certificate2 certificate)
    {
        try
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;
            chain.ChainPolicy.VerificationTime = DateTime.UtcNow;
            return chain.Build(certificate);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-loader-cert-chain",
                "Failed to build certificate chain while validating plugin signing cert.",
                ex);
            return false;
        }
    }

    /// <summary>
    /// Find a loaded assembly that is compatible with the requested assembly name.
    /// Compares name, version (if specified), and public key token (if specified).
    /// </summary>
    private static Assembly? FindCompatibleLoadedAssembly(AssemblyName requestedName, string? pluginDirectory = null)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var requestedVersion = requestedName.Version;
        var requestedPublicKey = requestedName.GetPublicKeyToken();

        foreach (var assembly in assemblies)
        {
            var assemblyName = assembly.GetName();

            // Name must match (case-insensitive)
            if (!string.Equals(assemblyName.Name, requestedName.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            // If version is specified, check for version compatibility
            // Accept exact match or higher version (binding redirect behavior)
            if (requestedVersion != null && assemblyName.Version != null)
            {
                if (assemblyName.Version < requestedVersion)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Skipping loaded assembly {assemblyName.Name} v{assemblyName.Version} - requested version {requestedVersion} is higher");
                    continue;
                }
            }

            // If public key token is specified, it must match
            if (requestedPublicKey != null && requestedPublicKey.Length > 0)
            {
                var assemblyPublicKey = assemblyName.GetPublicKeyToken();
                if (assemblyPublicKey == null || !requestedPublicKey.SequenceEqual(assemblyPublicKey))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Skipping loaded assembly {assemblyName.Name} - public key token mismatch");
                    continue;
                }
            }

            if (!IsLoadedAssemblyCompatibleWithContext(assembly, pluginDirectory))
                continue;

            return assembly;
        }

        return null;
    }

    private static PluginDependencyResolutionContext[] GetScopedDependencyResolutionContexts(Assembly? requestingAssembly)
    {
        var contexts = DependencyResolutionContexts.Values.ToArray();

        if (contexts.Length == 0)
            return [];

        if (requestingAssembly == null)
            return contexts.Length == 1 ? contexts : [];

        return contexts
            .Where(context => IsRequestingAssemblyInContext(requestingAssembly, context))
            .ToArray();
    }

    private static bool IsRequestingAssemblyInContext(Assembly requestingAssembly, PluginDependencyResolutionContext context)
    {
        if (ReferenceEquals(requestingAssembly, context.PluginMainAssembly))
            return true;

        if (!string.IsNullOrWhiteSpace(context.PluginMainAssemblyFullName) &&
            string.Equals(requestingAssembly.FullName, context.PluginMainAssemblyFullName, StringComparison.Ordinal))
        {
            return true;
        }

        var requestingAssemblyLocation = GetAssemblyLocation(requestingAssembly);
        return !string.IsNullOrWhiteSpace(requestingAssemblyLocation) &&
               IsPathWithinDirectory(requestingAssemblyLocation, context.PluginDirectory);
    }

    private static bool IsLoadedAssemblyCompatibleWithContext(Assembly assembly, string? pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
            return true;

        var location = GetAssemblyLocation(assembly);
        if (string.IsNullOrWhiteSpace(location))
            return IsContextOwnedLoadedAssemblyCompatibleWithContext(assembly, pluginDirectory);

        if (IsPathWithinDirectory(location, pluginDirectory))
            return true;

        var contexts = DependencyResolutionContexts.Values.ToArray();
        return !contexts.Any(context => IsPathWithinDirectory(location, context.PluginDirectory));
    }

    private static bool IsContextOwnedLoadedAssemblyCompatibleWithContext(Assembly assembly, string pluginDirectory)
    {
        var contexts = DependencyResolutionContexts.Values.ToArray();
        var owningContext = contexts.FirstOrDefault(context => ReferenceEquals(assembly, context.PluginMainAssembly));
        return owningContext == null ||
               string.Equals(owningContext.PluginDirectory, Path.GetFullPath(pluginDirectory), StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetAssemblyLocation(Assembly assembly)
    {
        try
        {
            return string.IsNullOrWhiteSpace(assembly.Location)
                ? null
                : Path.GetFullPath(assembly.Location);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-loader-assembly-location",
                "Failed to resolve assembly location during plugin load.",
                ex);
            return null;
        }
    }

    private static string? GetPluginAssemblyCandidatePath(AssemblyName requestedAssemblyName, string pluginMainAssemblyPath, string pluginDirectory)
    {
        var assemblyName = requestedAssemblyName.Name;
        if (string.IsNullOrWhiteSpace(assemblyName) || string.IsNullOrWhiteSpace(pluginDirectory))
            return null;

        var candidatePath = assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) &&
                            requestedAssemblyName.CultureInfo is { Name.Length: > 0 } cultureInfo
            ? Path.Combine(pluginDirectory, cultureInfo.Name, $"{assemblyName[..^".resources".Length]}.resources.dll")
            : Path.Combine(pluginDirectory, $"{assemblyName}.dll");

        var normalizedCandidatePath = Path.GetFullPath(candidatePath);
        if (string.Equals(normalizedCandidatePath, Path.GetFullPath(pluginMainAssemblyPath), StringComparison.OrdinalIgnoreCase))
            return null;

        return IsPathWithinDirectory(normalizedCandidatePath, pluginDirectory)
            ? normalizedCandidatePath
            : null;
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
        foreach (var c in assemblyName)
        {
            if (!(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-'))
                return false;
        }

        return true;
    }

    private sealed class PluginDependencyResolutionContext(
        string pluginMainAssemblyPath,
        string pluginDirectory,
        IPluginSignatureValidator signatureValidator)
    {
        public string PluginMainAssemblyPath { get; } = pluginMainAssemblyPath;
        public string PluginDirectory { get; } = pluginDirectory;
        public IPluginSignatureValidator SignatureValidator { get; } = signatureValidator;
        public Assembly? PluginMainAssembly { get; private set; }
        public string? PluginMainAssemblyFullName { get; private set; }

        public void SetPluginMainAssembly(Assembly assembly)
        {
            PluginMainAssembly = assembly;
            PluginMainAssemblyFullName = assembly.FullName;
        }

        public void ReleasePluginMainAssembly()
        {
            PluginMainAssembly = null;
        }
    }

    private sealed record RegisteredPluginDependencyResolutionContext(
        PluginDependencyResolutionContext Context,
        bool IsNew);

    private sealed class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _pluginMainAssemblyPath;
        private readonly string _pluginDirectory;
        private readonly AssemblyDependencyResolver _resolver;
        private readonly IPluginSignatureValidator _signatureValidator;

        public PluginAssemblyLoadContext(string pluginMainAssemblyPath, string pluginDirectory, IPluginSignatureValidator signatureValidator)
            : base($"Plugin:{Path.GetFileNameWithoutExtension(pluginMainAssemblyPath)}", isCollectible: true)
        {
            _pluginMainAssemblyPath = Path.GetFullPath(pluginMainAssemblyPath);
            _pluginDirectory = string.IsNullOrWhiteSpace(pluginDirectory)
                ? Path.GetDirectoryName(_pluginMainAssemblyPath) ?? string.Empty
                : Path.GetFullPath(pluginDirectory);
            _resolver = new AssemblyDependencyResolver(_pluginMainAssemblyPath);
            _signatureValidator = signatureValidator;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblySimpleName = assemblyName.Name;
            if (string.IsNullOrWhiteSpace(assemblySimpleName))
                return null;

            // Share host contracts and host UI/runtime assemblies with the default context.
            if (ShouldShareDefaultContextAssembly(assemblySimpleName))
            {
                return ResolveSharedHostAssembly(assemblyName);
            }

            var candidatePath = _resolver.ResolveAssemblyToPath(assemblyName)
                               ?? GetPluginAssemblyCandidatePath(assemblyName, _pluginMainAssemblyPath, _pluginDirectory);

            if (string.IsNullOrWhiteSpace(candidatePath) || !File.Exists(candidatePath))
                return null;

            var normalizedCandidatePath = Path.GetFullPath(candidatePath);
            if (!IsPathWithinDirectory(normalizedCandidatePath, _pluginDirectory))
                return null;

            // AssemblyLoadContext.Load requires synchronous return
            PluginSignatureResult? signatureResult;
            try
            {
                signatureResult = _signatureValidator.ValidateAsync(normalizedCandidatePath).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to validate plugin dependency signature. [path={normalizedCandidatePath}]", ex);
                return null;
            }

            if (!IsValidPluginDependencySignature(signatureResult, assemblyName, normalizedCandidatePath))
                return null;

            return LoadFromAssemblyPath(normalizedCandidatePath);
        }

        private static bool ShouldShareDefaultContextAssembly(string assemblySimpleName)
        {
            return PluginAssemblyNaming.ShouldShareHostContractAssembly(assemblySimpleName);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (string.IsNullOrWhiteSpace(libraryPath) || !File.Exists(libraryPath))
                return IntPtr.Zero;

            // Mirror managed load: never load native code outside the plugin tree.
            var fullPath = Path.GetFullPath(libraryPath);
            if (!IsPathWithinDirectory(fullPath, _pluginDirectory))
                return IntPtr.Zero;

            return LoadUnmanagedDllFromPath(fullPath);
        }

        private static Assembly? ResolveSharedHostAssembly(AssemblyName assemblyName)
        {
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly =>
                {
                    var loadedName = assembly.GetName();
                    return string.Equals(loadedName.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase);
                });

            if (loadedAssembly is not null)
                return loadedAssembly;

            var appBaseCandidate = Path.Combine(AppContext.BaseDirectory, $"{assemblyName.Name}.dll");
            if (!File.Exists(appBaseCandidate))
                return null;

            try
            {
                return Assembly.LoadFrom(Path.GetFullPath(appBaseCandidate));
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to load shared host assembly from app base: {appBaseCandidate}", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Create a plugin instance from a type
    /// </summary>
    private static IPlugin? CreatePluginInstance(Type pluginType, string dllPath)
    {
        try
        {
            // Try to get PluginAttribute for version and compatibility info
            string pluginVersion = "1.0.0";
            string minimumHostVersion = "1.0.0";
            var pluginAttribute = pluginType.GetCustomAttributes(true)
                .FirstOrDefault(attr => attr.GetType().Name == "PluginAttribute");

            if (pluginAttribute != null)
            {
                // Use reflection to get version and MinimumHostVersion from attribute
                var versionProp = pluginAttribute.GetType().GetProperty("Version");
                var minHostVersionProp = pluginAttribute.GetType().GetProperty("MinimumHostVersion");

                if (versionProp != null)
                {
                    pluginVersion = versionProp.GetValue(pluginAttribute)?.ToString() ?? "1.0.0";
                }

                if (minHostVersionProp != null)
                {
                    minimumHostVersion = minHostVersionProp.GetValue(pluginAttribute)?.ToString() ?? "1.0.0";
                }
            }

            // Check version compatibility
            if (!IsVersionCompatible(minimumHostVersion))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {pluginType.Name} requires host version {minimumHostVersion} or higher. Current host version is incompatible. Skipping.");
                return null;
            }

            // Create an instance of the plugin
            IPlugin? plugin = null;
            try
            {
                plugin = (IPlugin?)Activator.CreateInstance(pluginType);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to create instance of plugin type {pluginType.Name}: {ex.Message}", ex);
                return null;
            }

            return plugin;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to create plugin instance from type {pluginType.Name}: {ex.Message}", ex);
            return null;
        }
    }

    private static bool IsPluginTypeCandidate(Type type)
    {
        return typeof(IPlugin).IsAssignableFrom(type);
    }

    /// <summary>
    /// Check if the current host version meets the plugin's minimum requirements
    /// </summary>
    private static bool IsVersionCompatible(string? minimumHostVersion)
    {
        // No requirement specified.
        if (string.IsNullOrWhiteSpace(minimumHostVersion))
            return true;

        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion == null)
                return false;

            var normalized = minimumHostVersion.Trim().TrimStart('v', 'V');
            if (!Version.TryParse(normalized, out var minVersion))
                return false; // Fail closed on unparseable requirements

            return currentVersion >= minVersion;
        }
        catch
        {
            return false; // Fail closed if the check itself throws
        }
    }

    /// <summary>
    /// Check if a DLL file can be loaded as a plugin
    /// </summary>
    public bool CanLoad(string filePath, string? parentDirectoryName = null)
    {
        var fileName = Path.GetFileName(filePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

        if (PluginAssemblyNaming.IsSdkOrSharedDllFileName(fileName) ||
            fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (PluginAssemblyNaming.IsPluginPrefixedFileName(fileName))
            return true;

        if (string.IsNullOrWhiteSpace(parentDirectoryName))
            return false;

        var normalizedDllName = NormalizePluginToken(fileNameWithoutExtension);
        var normalizedParentName = NormalizePluginToken(parentDirectoryName);
        var normalizedParentShortName = NormalizePluginToken(
            PluginAssemblyNaming.StripPluginPrefixForNormalization(parentDirectoryName));

        if (string.IsNullOrWhiteSpace(normalizedDllName))
            return false;

        return normalizedDllName.Equals(normalizedParentName, StringComparison.OrdinalIgnoreCase) ||
               normalizedDllName.Equals(normalizedParentShortName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalize a plugin token for comparison
    /// </summary>
    private static string NormalizePluginToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    /// <summary>
    /// Get culture folders that should be skipped during plugin scanning
    /// </summary>
    public HashSet<string> GetCultureFolders() => _cultureFolders;
}
