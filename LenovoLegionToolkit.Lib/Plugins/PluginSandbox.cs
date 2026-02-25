using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Implementation of plugin sandbox using AssemblyLoadContext for isolation
/// </summary>
public class PluginSandbox : IPluginSandbox, IDisposable
{
    private readonly Dictionary<string, SandboxContext> _sandboxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<SandboxViolationEventArgs>? SandboxViolation;

    /// <inheritdoc />
    public event EventHandler<ResourceLimitExceededEventArgs>? ResourceLimitExceeded;

    /// <inheritdoc />
    public bool CreateSandbox(string pluginId, string assemblyPath, SandboxConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new ArgumentException("Plugin ID cannot be null or empty", nameof(pluginId));

        if (!File.Exists(assemblyPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Cannot create sandbox: Assembly not found at {assemblyPath}");
            return false;
        }

        lock (_lock)
        {
            if (_sandboxes.ContainsKey(pluginId))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Sandbox already exists for plugin: {pluginId}");
                return false;
            }

            try
            {
                var context = new SandboxContext(pluginId, assemblyPath, configuration);
                _sandboxes[pluginId] = context;

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Created sandbox for plugin: {pluginId} with permissions: {configuration.Permissions}");

                return true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to create sandbox for plugin {pluginId}: {ex.Message}", ex);
                return false;
            }
        }
    }

    /// <inheritdoc />
    public IPlugin? LoadPlugin(string pluginId)
    {
        lock (_lock)
        {
            if (!_sandboxes.TryGetValue(pluginId, out var context))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Sandbox not found for plugin: {pluginId}");
                return null;
            }

            try
            {
                if (context.PluginInstance != null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Plugin {pluginId} is already loaded");
                    return context.PluginInstance;
                }

                // Load the assembly into the sandbox context
                var assemblyBytes = File.ReadAllBytes(context.AssemblyPath);
                var assembly = context.LoadContext.LoadFromStream(new MemoryStream(assemblyBytes));

                // Find the plugin type
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                if (pluginType == null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"No plugin type found in assembly for {pluginId}");
                    return null;
                }

                // Create plugin instance
                var plugin = (IPlugin?)Activator.CreateInstance(pluginType);
                if (plugin == null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to create plugin instance for {pluginId}");
                    return null;
                }

                context.PluginInstance = plugin;
                context.Info.PluginName = plugin.Name;
                context.Info.Version = GetPluginVersion(pluginType);
                context.Info.IsActive = true;
                context.Info.LoadedAt = DateTime.Now;

                // Start resource monitoring
                StartResourceMonitoring(context);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Successfully loaded plugin {pluginId} ({plugin.Name} v{context.Info.Version})");

                return plugin;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to load plugin {pluginId}: {ex.Message}", ex);
                return null;
            }
        }
    }

    /// <inheritdoc />
    public bool UnloadPlugin(string pluginId)
    {
        lock (_lock)
        {
            if (!_sandboxes.TryGetValue(pluginId, out var context))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Sandbox not found for plugin: {pluginId}");
                return false;
            }

            try
            {
                // Stop resource monitoring
                StopResourceMonitoring(context);

                // Call plugin cleanup
                context.PluginInstance?.OnShutdown();
                context.PluginInstance?.Stop();

                context.PluginInstance = null;
                context.Info.IsActive = false;

                // Unload the assembly context
                if (context.LoadContext.IsCollectible)
                {
                    context.LoadContext.Unload();
                }

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Successfully unloaded plugin: {pluginId}");

                return true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error unloading plugin {pluginId}: {ex.Message}", ex);
                return false;
            }
        }
    }

    /// <inheritdoc />
    public SandboxOperationResult ExecuteInSandbox(string pluginId, Func<object?> operation)
    {
        if (!_sandboxes.TryGetValue(pluginId, out var context))
        {
            return new SandboxOperationResult
            {
                Success = false,
                ErrorMessage = $"Sandbox not found for plugin: {pluginId}",
                WasBlocked = false
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check resource limits before execution
            if (IsResourceLimitExceeded(context))
            {
                return new SandboxOperationResult
                {
                    Success = false,
                    ErrorMessage = "Resource limit exceeded",
                    WasBlocked = true
                };
            }

            // Execute the operation
            var result = operation();

            stopwatch.Stop();
            context.RecordOperationTime(stopwatch.ElapsedMilliseconds);

            return new SandboxOperationResult
            {
                Success = true,
                Data = result,
                WasBlocked = false
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Check if this is a sandbox violation
            if (IsSandboxViolation(ex, context.Configuration))
            {
                OnSandboxViolation(pluginId, SandboxPermission.None, ex.Message, ex.StackTrace);

                return new SandboxOperationResult
                {
                    Success = false,
                    ErrorMessage = $"Sandbox violation: {ex.Message}",
                    WasBlocked = true
                };
            }

            return new SandboxOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                WasBlocked = false
            };
        }
    }

    /// <inheritdoc />
    public async Task<SandboxOperationResult> ExecuteInSandboxAsync(string pluginId, Func<Task<object?>> operation)
    {
        if (!_sandboxes.TryGetValue(pluginId, out var context))
        {
            return new SandboxOperationResult
            {
                Success = false,
                ErrorMessage = $"Sandbox not found for plugin: {pluginId}",
                WasBlocked = false
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check resource limits before execution
            if (IsResourceLimitExceeded(context))
            {
                return new SandboxOperationResult
                {
                    Success = false,
                    ErrorMessage = "Resource limit exceeded",
                    WasBlocked = true
                };
            }

            // Execute the operation with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(context.Configuration.OperationTimeoutSeconds));
            var result = await operation().WaitAsync(cts.Token);

            stopwatch.Stop();
            context.RecordOperationTime(stopwatch.ElapsedMilliseconds);

            return new SandboxOperationResult
            {
                Success = true,
                Data = result,
                WasBlocked = false
            };
        }
        catch (OperationCanceledException)
        {
            return new SandboxOperationResult
            {
                Success = false,
                ErrorMessage = "Operation timed out",
                WasBlocked = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            if (IsSandboxViolation(ex, context.Configuration))
            {
                OnSandboxViolation(pluginId, SandboxPermission.None, ex.Message, ex.StackTrace);

                return new SandboxOperationResult
                {
                    Success = false,
                    ErrorMessage = $"Sandbox violation: {ex.Message}",
                    WasBlocked = true
                };
            }

            return new SandboxOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                WasBlocked = false
            };
        }
    }

    /// <inheritdoc />
    public bool HasPermission(string pluginId, SandboxPermission permission)
    {
        if (!_sandboxes.TryGetValue(pluginId, out var context))
            return false;

        return (context.Configuration.Permissions & permission) == permission;
    }

    /// <inheritdoc />
    public SandboxedPluginInfo? GetPluginInfo(string pluginId)
    {
        if (!_sandboxes.TryGetValue(pluginId, out var context))
            return null;

        lock (context)
        {
            context.Info.MemoryUsage = GC.GetTotalMemory(false); // Simplified - should track per-plugin
            return context.Info;
        }
    }

    /// <inheritdoc />
    public IEnumerable<SandboxedPluginInfo> GetAllSandboxedPlugins()
    {
        lock (_lock)
        {
            return _sandboxes.Values.Select(s =>
            {
                lock (s)
                {
                    s.Info.MemoryUsage = GC.GetTotalMemory(false); // Simplified
                    return s.Info;
                }
            }).ToList();
        }
    }

    /// <inheritdoc />
    public bool UpdateConfiguration(string pluginId, SandboxConfiguration configuration)
    {
        lock (_lock)
        {
            if (!_sandboxes.TryGetValue(pluginId, out var context))
                return false;

            context.Configuration = configuration;
            context.Info.Configuration = configuration;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Updated configuration for plugin {pluginId}: {configuration.Permissions}");

            return true;
        }
    }

    /// <inheritdoc />
    public SandboxResourceUsage GetResourceUsage(string pluginId)
    {
        if (!_sandboxes.TryGetValue(pluginId, out var context))
            return new SandboxResourceUsage();

        lock (context)
        {
            return new SandboxResourceUsage
            {
                MemoryUsageBytes = context.ResourceStats.MemoryUsageBytes,
                PeakMemoryUsageBytes = context.ResourceStats.PeakMemoryUsageBytes,
                CpuUsagePercentage = context.ResourceStats.CpuUsagePercentage,
                FileSystemOperationCount = context.ResourceStats.FileSystemOperationCount,
                NetworkOperationCount = context.ResourceStats.NetworkOperationCount,
                AverageOperationTimeMs = context.ResourceStats.AverageOperationTimeMs,
                ViolationCount = context.ResourceStats.ViolationCount,
                SandboxCreatedAt = context.Info.LoadedAt,
                TotalRunningTime = DateTime.Now - context.Info.LoadedAt
            };
        }
    }

    /// <inheritdoc />
    public bool DestroySandbox(string pluginId)
    {
        lock (_lock)
        {
            if (!_sandboxes.TryGetValue(pluginId, out var context))
                return false;

            try
            {
                // Unload plugin first
                UnloadPlugin(pluginId);

                // Remove from dictionary
                _sandboxes.Remove(pluginId);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Destroyed sandbox for plugin: {pluginId}");

                return true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error destroying sandbox for {pluginId}: {ex.Message}", ex);
                return false;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            foreach (var pluginId in _sandboxes.Keys.ToList())
            {
                try
                {
                    DestroySandbox(pluginId);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error disposing sandbox for {pluginId}: {ex.Message}");
                }
            }

            _sandboxes.Clear();
        }

        _disposed = true;
    }

    #region Private Methods

    private void StartResourceMonitoring(SandboxContext context)
    {
        // In a real implementation, this would start a background task
        // to monitor CPU, memory, and other resources
        context.MonitoringCancellation = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!context.MonitoringCancellation.IsCancellationRequested)
            {
                try
                {
                    // Update memory usage
                    var currentMemory = GC.GetTotalMemory(false);
                    context.ResourceStats.MemoryUsageBytes = currentMemory;

                    if (currentMemory > context.ResourceStats.PeakMemoryUsageBytes)
                    {
                        context.ResourceStats.PeakMemoryUsageBytes = currentMemory;
                    }

                    // Check memory limit
                    var maxMemoryBytes = (long)context.Configuration.MaxMemoryMB * 1024 * 1024;
                    if (currentMemory > maxMemoryBytes)
                    {
                        OnResourceLimitExceeded(context.Info.PluginId, ResourceType.Memory, currentMemory, maxMemoryBytes);
                    }

                    await Task.Delay(1000, context.MonitoringCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error in resource monitoring for {context.Info.PluginId}: {ex.Message}");
                }
            }
        }, context.MonitoringCancellation.Token);
    }

    private void StopResourceMonitoring(SandboxContext context)
    {
        context.MonitoringCancellation?.Cancel();
        context.MonitoringCancellation?.Dispose();
        context.MonitoringCancellation = null;
    }

    private bool IsResourceLimitExceeded(SandboxContext context)
    {
        var maxMemoryBytes = (long)context.Configuration.MaxMemoryMB * 1024 * 1024;
        return context.ResourceStats.MemoryUsageBytes > maxMemoryBytes;
    }

    private bool IsSandboxViolation(Exception ex, SandboxConfiguration configuration)
    {
        // Check for common sandbox violation exceptions
        var violationIndicators = new[]
        {
            "SecurityException",
            "UnauthorizedAccessException",
            "FileLoadException",
            "BadImageFormatException"
        };

        return violationIndicators.Any(indicator =>
            ex.GetType().Name.Contains(indicator) ||
            (ex.InnerException != null && ex.InnerException.GetType().Name.Contains(indicator)));
    }

    private void OnSandboxViolation(string pluginId, SandboxPermission permission, string description, string? stackTrace)
    {
        var args = new SandboxViolationEventArgs
        {
            PluginId = pluginId,
            ViolatedPermission = permission,
            Description = description,
            StackTrace = stackTrace,
            Timestamp = DateTime.Now
        };

        SandboxViolation?.Invoke(this, args);

        if (_sandboxes.TryGetValue(pluginId, out var context))
        {
            context.ResourceStats.ViolationCount++;
        }
    }

    private void OnResourceLimitExceeded(string pluginId, ResourceType resourceType, double currentUsage, double maximumAllowed)
    {
        var args = new ResourceLimitExceededEventArgs
        {
            PluginId = pluginId,
            ResourceType = resourceType,
            CurrentUsage = currentUsage,
            MaximumAllowed = maximumAllowed,
            Timestamp = DateTime.Now
        };

        ResourceLimitExceeded?.Invoke(this, args);
    }

    private string GetPluginVersion(Type pluginType)
    {
        var assembly = pluginType.Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    #endregion

    #region Nested Classes

    private class SandboxContext
    {
        public string PluginId { get; }
        public string AssemblyPath { get; }
        public SandboxConfiguration Configuration { get; set; }
        public SandboxedPluginInfo Info { get; }
        public PluginLoadContext LoadContext { get; }
        public IPlugin? PluginInstance { get; set; }
        public ResourceStats ResourceStats { get; }
        public CancellationTokenSource? MonitoringCancellation { get; set; }

        public SandboxContext(string pluginId, string assemblyPath, SandboxConfiguration configuration)
        {
            PluginId = pluginId;
            AssemblyPath = assemblyPath;
            Configuration = configuration;
            Info = new SandboxedPluginInfo
            {
                PluginId = pluginId,
                Configuration = configuration
            };
            LoadContext = new PluginLoadContext(assemblyPath);
            ResourceStats = new ResourceStats();
        }

        public void RecordOperationTime(long milliseconds)
        {
            // Simple moving average
            var count = ResourceStats.OperationCount++;
            ResourceStats.AverageOperationTimeMs =
                (ResourceStats.AverageOperationTimeMs * count + milliseconds) / (count + 1);
        }
    }

    private class ResourceStats
    {
        public long MemoryUsageBytes { get; set; }
        public long PeakMemoryUsageBytes { get; set; }
        public double CpuUsagePercentage { get; set; }
        public int FileSystemOperationCount { get; set; }
        public int NetworkOperationCount { get; set; }
        public double AverageOperationTimeMs { get; set; }
        public int ViolationCount { get; set; }
        public int OperationCount { get; set; }
    }

    private class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Don't load host assemblies into plugin context
            if (assemblyName.Name?.StartsWith("LenovoLegionToolkit") == true &&
                !assemblyName.Name.Contains("Plugins"))
            {
                return null;
            }

            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }

    #endregion
}
