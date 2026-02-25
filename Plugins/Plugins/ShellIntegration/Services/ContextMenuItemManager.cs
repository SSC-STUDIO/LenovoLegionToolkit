using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Services;

/// <summary>
/// Context menu item manager implementation
/// </summary>
public class ContextMenuItemManager : IContextMenuItemManager
{
    private readonly ILogger<ContextMenuItemManager> _logger;
    private readonly string _menuItemsRegistryPath = @"Software\Nilesoft\Shell\MenuItems";
    private List<ContextMenuItem> _menuItems = new();

    public ContextMenuItemManager(ILogger<ContextMenuItemManager> logger)
    {
        _logger = logger;
        LoadMenuItems();
    }

    public IEnumerable<ContextMenuItem> GetMenuItems()
    {
        return _menuItems.OrderBy(item => item.Category).ThenBy(item => item.Priority);
    }

    public bool AddMenuItem(ContextMenuItem item)
    {
        try
        {
            if (string.IsNullOrEmpty(item.Id) || string.IsNullOrEmpty(item.Name))
            {
                _logger.LogWarning("Invalid menu item: missing ID or name");
                return false;
            }

            if (_menuItems.Any(m => m.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Menu item with ID {ItemId} already exists", item.Id);
                return false;
            }

            _menuItems.Add(item);
            SaveMenuItems();
            _logger.LogInformation("Added context menu item: {ItemName}", item.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add context menu item");
            return false;
        }
    }

    public bool RemoveMenuItem(string itemId)
    {
        try
        {
            var item = _menuItems.FirstOrDefault(m => m.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                _logger.LogWarning("Menu item with ID {ItemId} not found", itemId);
                return false;
            }

            _menuItems.Remove(item);
            SaveMenuItems();
            _logger.LogInformation("Removed context menu item: {ItemName}", item.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove context menu item");
            return false;
        }
    }

    public bool UpdateMenuItem(ContextMenuItem item)
    {
        try
        {
            var existingIndex = _menuItems.FindIndex(m => m.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            if (existingIndex == -1)
            {
                _logger.LogWarning("Menu item with ID {ItemId} not found for update", item.Id);
                return false;
            }

            _menuItems[existingIndex] = item;
            SaveMenuItems();
            _logger.LogInformation("Updated context menu item: {ItemName}", item.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update context menu item");
            return false;
        }
    }

    public bool ToggleMenuItem(string itemId, bool enabled)
    {
        try
        {
            var item = GetMenuItem(itemId);
            if (item == null)
            {
                _logger.LogWarning("Menu item with ID {ItemId} not found", itemId);
                return false;
            }

            item.IsEnabled = enabled;
            SaveMenuItems();
            _logger.LogInformation("Toggled context menu item {ItemName}: {Enabled}", item.Name, enabled ? "Enabled" : "Disabled");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle context menu item");
            return false;
        }
    }

    public ContextMenuItem? GetMenuItem(string itemId)
    {
        return _menuItems.FirstOrDefault(m => m.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
    }

    public void ResetToDefaults()
    {
        try
        {
            _menuItems.Clear();
            LoadDefaultMenuItems();
            SaveMenuItems();
            _logger.LogInformation("Reset context menu items to defaults");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset context menu items");
        }
    }

    private void LoadMenuItems()
    {
        try
        {
            _menuItems.Clear();

            using var key = Registry.CurrentUser.OpenSubKey(_menuItemsRegistryPath);
            if (key == null)
            {
                LoadDefaultMenuItems();
                return;
            }

            var itemNames = key.GetValueNames();
            foreach (var itemName in itemNames)
            {
                try
                {
                    var itemJson = key.GetValue(itemName)?.ToString();
                    if (string.IsNullOrEmpty(itemJson))
                        continue;

                    var item = JsonSerializer.Deserialize<ContextMenuItem>(itemJson);
                    if (item != null)
                    {
                        _menuItems.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load menu item: {ItemName}", itemName);
                }
            }

            if (_menuItems.Count == 0)
            {
                LoadDefaultMenuItems();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load menu items");
            LoadDefaultMenuItems();
        }
    }

    private void LoadDefaultMenuItems()
    {
        _menuItems.AddRange(new[]
        {
            new ContextMenuItem
            {
                Id = "open_terminal_here",
                Name = "Open Terminal Here",
                Description = "Open Windows Terminal in current directory",
                Icon = "Terminal24",
                Command = "wt.exe",
                Arguments = "-d .",
                Category = "Navigation",
                FileTypes = ["*"],
                Priority = 10
            },
            new ContextMenuItem
            {
                Id = "open_powershell_here",
                Name = "Open PowerShell Here",
                Description = "Open PowerShell in current directory",
                Icon = "Console24",
                Command = "powershell.exe",
                Arguments = "-NoExit -Command \"Set-Location -LiteralPath '%V'\"",
                Category = "Navigation",
                FileTypes = ["Directory", "Directory\\Background"],
                Priority = 20
            },
            new ContextMenuItem
            {
                Id = "copy_file_path",
                Name = "Copy File Path",
                Description = "Copy full file path to clipboard",
                Icon = "Copy24",
                Command = "cmd.exe",
                Arguments = "/c echo \"%1\" | clip",
                Category = "Utilities",
                FileTypes = ["*"],
                Priority = 30
            },
            new ContextMenuItem
            {
                Id = "run_as_admin",
                Name = "Run as Administrator",
                Description = "Run selected program as administrator",
                Icon = "Shield24",
                Command = "powershell.exe",
                Arguments = "-Command \"Start-Process '%1' -Verb RunAs\"",
                Category = "System",
                FileTypes = [".exe", ".msi", ".bat", ".cmd"],
                RequiresAdmin = true,
                Priority = 40
            },
            new ContextMenuItem
            {
                Id = "open_file_location",
                Name = "Open File Location",
                Description = "Open folder containing the file",
                Icon = "Folder24",
                Command = "explorer.exe",
                Arguments = "/select,\"%1\"",
                Category = "Navigation",
                FileTypes = ["*"],
                Priority = 50
            }
        });
    }

    private void SaveMenuItems()
    {
        try
        {
            // Delete existing registry entries
            Registry.CurrentUser.DeleteSubKeyTree(_menuItemsRegistryPath, false);

            // Create new registry entries
            using var key = Registry.CurrentUser.CreateSubKey(_menuItemsRegistryPath);
            if (key != null)
            {
                foreach (var item in _menuItems)
                {
                    try
                    {
                        var itemJson = JsonSerializer.Serialize(item);
                        key.SetValue(item.Id, itemJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save menu item: {ItemId}", item.Id);
                    }
                }
                key.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save menu items");
        }
    }
}