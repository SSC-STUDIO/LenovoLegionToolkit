using System.Collections.Generic;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Services;

/// <summary>
/// Interface for managing context menu items
/// </summary>
public interface IContextMenuItemManager
{
    /// <summary>
    /// Get all available context menu items
    /// </summary>
    IEnumerable<ContextMenuItem> GetMenuItems();

    /// <summary>
    /// Add a new context menu item
    /// </summary>
    bool AddMenuItem(ContextMenuItem item);

    /// <summary>
    /// Remove a context menu item
    /// </summary>
    bool RemoveMenuItem(string itemId);

    /// <summary>
    /// Update an existing context menu item
    /// </summary>
    bool UpdateMenuItem(ContextMenuItem item);

    /// <summary>
    /// Enable or disable a context menu item
    /// </summary>
    bool ToggleMenuItem(string itemId, bool enabled);

    /// <summary>
    /// Get context menu item by ID
    /// </summary>
    ContextMenuItem? GetMenuItem(string itemId);

    /// <summary>
    /// Reset context menu items to default
    /// </summary>
    void ResetToDefaults();
}

/// <summary>
/// Context menu item model
/// </summary>
public class ContextMenuItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Command { get; set; } = "";
    public string Arguments { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public string Category { get; set; } = "";
    public string[] FileTypes { get; set; } = [];
    public bool RequiresAdmin { get; set; } = false;
    public int Priority { get; set; } = 100;
}