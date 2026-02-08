using System.Collections.Generic;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Services;

/// <summary>
/// Interface for managing shell extensions
/// </summary>
public interface IShellExtensionManager
{
    /// <summary>
    /// Get all registered shell extensions
    /// </summary>
    IEnumerable<ShellExtension> GetExtensions();

    /// <summary>
    /// Get enabled shell extensions
    /// </summary>
    IEnumerable<ShellExtension> GetEnabledExtensions();

    /// <summary>
    /// Enable or disable a shell extension
    /// </summary>
    bool ToggleExtension(string extensionId, bool enabled);

    /// <summary>
    /// Configure shell extension settings
    /// </summary>
    bool ConfigureExtension(string extensionId, Dictionary<string, object> settings);

    /// <summary>
    /// Get shell extension by ID
    /// </summary>
    ShellExtension? GetExtension(string extensionId);

    /// <summary>
    /// Refresh shell extensions list
    /// </summary>
    void RefreshExtensions();

    /// <summary>
    /// Check if shell extensions are properly loaded
    /// </summary>
    bool IsSystemWorking();
}

/// <summary>
/// Shell extension model
/// </summary>
public class ShellExtension
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "";
    public string DllPath { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public bool IsSystem { get; set; } = false;
    public ShellExtensionType Type { get; set; } = ShellExtensionType.ContextMenu;
    public Dictionary<string, object> Settings { get; set; } = new();
    public string[] SupportedFileTypes { get; set; } = [];
}

/// <summary>
/// Shell extension types
/// </summary>
public enum ShellExtensionType
{
    ContextMenu,
    PropertySheet,
    IconHandler,
    DropHandler,
    DataHandler,
    ColumnProvider
}