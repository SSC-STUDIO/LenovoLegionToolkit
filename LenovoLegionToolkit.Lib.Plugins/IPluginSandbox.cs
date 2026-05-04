using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Defines the level of access a plugin has to system resources
/// </summary>
public enum SandboxPermission
{
    /// <summary>
    /// No special permissions
    /// </summary>
    None = 0,

    /// <summary>
    /// Access to file system (read-only)
    /// </summary>
    FileSystemRead = 1,

    /// <summary>
    /// Access to file system (read-write)
    /// </summary>
    FileSystemWrite = 2,

    /// <summary>
    /// Access to network
    /// </summary>
    NetworkAccess = 4,

    /// <summary>
    /// Access to registry (read-only)
    /// </summary>
    RegistryRead = 8,

    /// <summary>
    /// Access to registry (read-write)
    /// </summary>
    RegistryWrite = 16,

    /// <summary>
    /// Access to system information
    /// </summary>
    SystemInformation = 32,

    /// <summary>
    /// Access to hardware control APIs
    /// </summary>
    HardwareAccess = 64,

    /// <summary>
    /// Access to UI customization
    /// </summary>
    UICustomization = 128,

    /// <summary>
    /// Access to inter-plugin communication
    /// </summary>
    InterPluginCommunication = 256,

    /// <summary>
    /// All permissions (use with caution)
    /// </summary>
    All = ~0
}

/// <summary>
/// Configuration for a plugin sandbox
/// </summary>
public class SandboxConfiguration
{
    /// <summary>
    /// The permissions granted to the plugin
    /// </summary>
    public SandboxPermission Permissions { get; set; } = SandboxPermission.None;

    /// <summary>
    /// Maximum memory allowed for the plugin (in MB)
    /// </summary>
    public int MaxMemoryMB { get; set; } = 100;

    /// <summary>
    /// Maximum CPU usage allowed (percentage)
    /// </summary>
    public int MaxCpuPercentage { get; set; } = 10;

    /// <summary>
    /// Allowed file system paths (if FileSystem permission granted)
    /// </summary>
    public List<string> AllowedPaths { get; set; } = new();

    /// <summary>
    /// Blocked file system paths
    /// </summary>
    public List<string> BlockedPaths { get; set; } = new();

    /// <summary>
    /// Allowed network hosts (if NetworkAccess permission granted)
    /// </summary>
    public List<string> AllowedHosts { get; set; } = new();

    /// <summary>
    /// Whether the plugin can load additional assemblies
    /// </summary>
    public bool AllowDynamicAssemblyLoading { get; set; } = false;

    /// <summary>
    /// Whether the plugin can use reflection
    /// </summary>
    public bool AllowReflection { get; set; } = false;

    /// <summary>
    /// Timeout for plugin operations (in seconds)
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Information about a sandboxed plugin
/// </summary>
public class SandboxedPluginInfo
{
    /// <summary>
    /// Plugin ID
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Plugin name
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// Plugin version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Current sandbox configuration
    /// </summary>
    public SandboxConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Current memory usage (in bytes)
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Whether the plugin is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the plugin was loaded
    /// </summary>
    public DateTime LoadedAt { get; set; }
}

/// <summary>
/// Result of a sandboxed operation
/// </summary>
public class SandboxOperationResult
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result data (if any)
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the operation was blocked by sandbox restrictions
    /// </summary>
    public bool WasBlocked { get; set; }
}

/// <summary>
/// Interface for plugin sandboxing - provides security isolation for plugins
/// </summary>
public interface IPluginSandbox
{
    /// <summary>
    /// Creates a sandbox for a plugin with the specified configuration
    /// </summary>
    /// <param name="pluginId">Unique plugin identifier</param>
    /// <param name="assemblyPath">Path to the plugin assembly</param>
    /// <param name="configuration">Sandbox configuration</param>
    /// <returns>True if sandbox was created successfully</returns>
    bool CreateSandbox(string pluginId, string assemblyPath, SandboxConfiguration configuration);

    /// <summary>
    /// Loads a plugin into its sandbox
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>The loaded plugin instance</returns>
    IPlugin? LoadPlugin(string pluginId);

    /// <summary>
    /// Unloads a plugin from its sandbox
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if unloaded successfully</returns>
    bool UnloadPlugin(string pluginId);

    /// <summary>
    /// Executes an operation in the sandbox with security restrictions
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="operation">The operation to execute</param>
    /// <returns>Result of the operation</returns>
    SandboxOperationResult ExecuteInSandbox(string pluginId, Func<object?> operation);

    /// <summary>
    /// Executes an async operation in the sandbox
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="operation">The async operation to execute</param>
    /// <returns>Result of the operation</returns>
    Task<SandboxOperationResult> ExecuteInSandboxAsync(string pluginId, Func<Task<object?>> operation);

    /// <summary>
    /// Checks if a plugin has a specific permission
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="permission">Permission to check</param>
    /// <returns>True if the plugin has the permission</returns>
    bool HasPermission(string pluginId, SandboxPermission permission);

    /// <summary>
    /// Gets information about a sandboxed plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>Plugin information or null if not found</returns>
    SandboxedPluginInfo? GetPluginInfo(string pluginId);

    /// <summary>
    /// Gets all sandboxed plugins
    /// </summary>
    /// <returns>List of all sandboxed plugins</returns>
    IEnumerable<SandboxedPluginInfo> GetAllSandboxedPlugins();

    /// <summary>
    /// Updates the sandbox configuration for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="configuration">New configuration</param>
    /// <returns>True if updated successfully</returns>
    bool UpdateConfiguration(string pluginId, SandboxConfiguration configuration);

    /// <summary>
    /// Gets the current resource usage for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>Resource usage statistics</returns>
    SandboxResourceUsage GetResourceUsage(string pluginId);

    /// <summary>
    /// Destroys a sandbox and releases all resources
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if destroyed successfully</returns>
    bool DestroySandbox(string pluginId);

    /// <summary>
    /// Event raised when a plugin violates sandbox restrictions
    /// </summary>
    event EventHandler<SandboxViolationEventArgs>? SandboxViolation;

    /// <summary>
    /// Event raised when a plugin exceeds resource limits
    /// </summary>
    event EventHandler<ResourceLimitExceededEventArgs>? ResourceLimitExceeded;
}

/// <summary>
/// Event args for sandbox violations
/// </summary>
public class SandboxViolationEventArgs : EventArgs
{
    /// <summary>
    /// Plugin ID that violated the sandbox
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// The permission that was violated
    /// </summary>
    public SandboxPermission ViolatedPermission { get; set; }

    /// <summary>
    /// Description of the violation
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the violation occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Stack trace at the time of violation (if available)
    /// </summary>
    public string? StackTrace { get; set; }
}

/// <summary>
/// Event args for resource limit exceeded
/// </summary>
public class ResourceLimitExceededEventArgs : EventArgs
{
    /// <summary>
    /// Plugin ID that exceeded limits
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Type of resource that exceeded limit
    /// </summary>
    public ResourceType ResourceType { get; set; }

    /// <summary>
    /// Current usage
    /// </summary>
    public double CurrentUsage { get; set; }

    /// <summary>
    /// Maximum allowed
    /// </summary>
    public double MaximumAllowed { get; set; }

    /// <summary>
    /// When the limit was exceeded
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Types of resources that can be monitored
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// Memory usage
    /// </summary>
    Memory,

    /// <summary>
    /// CPU usage
    /// </summary>
    Cpu,

    /// <summary>
    /// File system operations
    /// </summary>
    FileSystem,

    /// <summary>
    /// Network usage
    /// </summary>
    Network,

    /// <summary>
    /// Operation execution time
    /// </summary>
    ExecutionTime
}

/// <summary>
/// Resource usage statistics for a sandboxed plugin
/// </summary>
public class SandboxResourceUsage
{
    /// <summary>
    /// Current memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Peak memory usage in bytes
    /// </summary>
    public long PeakMemoryUsageBytes { get; set; }

    /// <summary>
    /// Current CPU usage percentage
    /// </summary>
    public double CpuUsagePercentage { get; set; }

    /// <summary>
    /// Number of file system operations
    /// </summary>
    public int FileSystemOperationCount { get; set; }

    /// <summary>
    /// Number of network operations
    /// </summary>
    public int NetworkOperationCount { get; set; }

    /// <summary>
    /// Average operation execution time in milliseconds
    /// </summary>
    public double AverageOperationTimeMs { get; set; }

    /// <summary>
    /// Number of sandbox violations
    /// </summary>
    public int ViolationCount { get; set; }

    /// <summary>
    /// When the sandbox was created
    /// </summary>
    public DateTime SandboxCreatedAt { get; set; }

    /// <summary>
    /// Total time the plugin has been running
    /// </summary>
    public TimeSpan TotalRunningTime { get; set; }
}
