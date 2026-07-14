// Plugin SDK surface: see SDK_BOUNDARY.md for the full public contract
// that plugins are allowed to depend on. This file is part of the
// public SDK; transitions between PluginState values are enforced
// by PluginLifecycleStateMachine (host-internal) rather than by
// plugins themselves.

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin state enumeration
/// </summary>
public enum PluginState
{
    /// <summary>
    /// Not installed
    /// </summary>
    NotInstalled,
    
    /// <summary>
    /// Installed but not enabled
    /// </summary>
    Installed,
    
    /// <summary>
    /// Enabled (running)
    /// </summary>
    Enabled,
    
    /// <summary>
    /// Disabled
    /// </summary>
    Disabled,
    
    /// <summary>
    /// Load error
    /// </summary>
    Error
}

/// <summary>
/// Plugin health status enumeration
/// </summary>
public enum PluginHealthStatus
{
    /// <summary>
    /// Plugin is healthy
    /// </summary>
    Healthy,
    
    /// <summary>
    /// Plugin has warnings
    /// </summary>
    Warning,
    
    /// <summary>
    /// Plugin has errors
    /// </summary>
    Error,
    
    /// <summary>
    /// Plugin not found
    /// </summary>
    NotFound,
    
    /// <summary>
    /// Plugin dependency missing
    /// </summary>
    MissingDependencies,
    
    /// <summary>
    /// Plugin version incompatible
    /// </summary>
    VersionIncompatible
}

/// <summary>
/// Plugin state change event arguments
/// </summary>
public class PluginStateChangedEventArgs : global::System.EventArgs
{
    /// <summary>
    /// Plugin ID
    /// </summary>
    public string PluginId { get; }
    
    /// <summary>
    /// Previous state
    /// </summary>
    public PluginState OldState { get; }
    
    /// <summary>
    /// New state
    /// </summary>
    public PluginState NewState { get; }
    
    /// <summary>
    /// Error message (if any)
    /// </summary>
    public string? ErrorMessage { get; }

    public PluginStateChangedEventArgs(string pluginId, PluginState oldState, PluginState newState, string? errorMessage = null)
    {
        PluginId = pluginId;
        OldState = oldState;
        NewState = newState;
        ErrorMessage = errorMessage;
    }
}
