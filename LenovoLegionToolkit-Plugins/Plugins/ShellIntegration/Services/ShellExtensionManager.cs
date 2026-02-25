using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Services;

/// <summary>
/// Shell extension manager implementation
/// </summary>
public class ShellExtensionManager : IShellExtensionManager
{
    private readonly ILogger<ShellExtensionManager> _logger;

    private List<ShellExtension> _extensions = new();

    public ShellExtensionManager(ILogger<ShellExtensionManager> logger)
    {
        _logger = logger;
        LoadExtensions();
    }

    public IEnumerable<ShellExtension> GetExtensions()
    {
        return _extensions.OrderBy(e => e.IsSystem ? 0 : 1).ThenBy(e => e.Name);
    }

    public IEnumerable<ShellExtension> GetEnabledExtensions()
    {
        return GetExtensions().Where(e => e.IsEnabled);
    }

    public bool ToggleExtension(string extensionId, bool enabled)
    {
        try
        {
            var extension = GetExtension(extensionId);
            if (extension == null)
            {
                _logger.LogWarning("Shell extension with ID {ExtensionId} not found", extensionId);
                return false;
            }

            if (extension.IsSystem && !enabled)
            {
                _logger.LogWarning("Cannot disable system extension: {ExtensionName}", extension.Name);
                return false;
            }

            var registryKey = extension.Type switch
            {
                ShellExtensionType.ContextMenu => @"*\shellex\ContextMenuHandlers",
                ShellExtensionType.PropertySheet => @"*\shellex\PropertySheetHandlers",
                _ => @"*\shellex"
            };

            var handlerPath = $@"{registryPath}\{extension.Name}";
            
            using var key = Registry.ClassesRoot.OpenSubKey(handlerPath, true);
            if (key != null)
            {
                if (enabled)
                {
                    key.SetValue("", extension.Id);
                }
                else
                {
                    key.DeleteValue("", false);
                }
                key.Close();
            }

            extension.IsEnabled = enabled;
            _logger.LogInformation("Toggled shell extension {ExtensionName}: {Enabled}", extension.Name, enabled ? "Enabled" : "Disabled");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle shell extension");
            return false;
        }
    }

    public bool ConfigureExtension(string extensionId, Dictionary<string, object> settings)
    {
        try
        {
            var extension = GetExtension(extensionId);
            if (extension == null)
            {
                _logger.LogWarning("Shell extension with ID {ExtensionId} not found", extensionId);
                return false;
            }

            // Merge settings
            foreach (var kvp in settings)
            {
                extension.Settings[kvp.Key] = kvp.Value;
            }

            // Save settings to registry
            var settingsPath = $@"Software\Nilesoft\Shell\Extensions\{extensionId}";
            using var key = Registry.CurrentUser.CreateSubKey(settingsPath);
            if (key != null)
            {
                foreach (var kvp in settings)
                {
                    key.SetValue(kvp.Key, kvp.Value?.ToString() ?? "");
                }
                key.Close();
            }

            _logger.LogInformation("Configured shell extension {ExtensionName}", extension.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure shell extension");
            return false;
        }
    }

    public ShellExtension? GetExtension(string extensionId)
    {
        return _extensions.FirstOrDefault(e => e.Id.Equals(extensionId, StringComparison.OrdinalIgnoreCase));
    }

    public void RefreshExtensions()
    {
        _logger.LogInformation("Refreshing shell extensions list...");
        LoadExtensions();
        _logger.LogInformation("Loaded {Count} shell extensions", _extensions.Count);
    }

    public bool IsSystemWorking()
    {
        try
        {
            // Check if Windows Shell is functioning properly
            using var key = Registry.ClassesRoot.OpenSubKey(@"*\shellex\ContextMenuHandlers");
            return key != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows Shell system check failed");
            return false;
        }
    }

    private void LoadExtensions()
    {
        try
        {
            _extensions.Clear();

            // Load Nilesoft Shell extensions
            LoadNilesoftExtensions();

            // Load system shell extensions
            LoadSystemExtensions();

            // Load third-party extensions
            LoadThirdPartyExtensions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load shell extensions");
        }
    }

    private void LoadNilesoftExtensions()
    {
        try
        {
            var nilesoftExtensions = new[]
            {
                new ShellExtension
                {
                    Id = "{NilesoftShellContextMenuHandler}",
                    Name = "Nilesoft Shell",
                    Description = "Enhanced context menu with custom commands",
                    Version = "2.0.0",
                    IsEnabled = true,
                    IsSystem = false,
                    Type = ShellExtensionType.ContextMenu,
                    SupportedFileTypes = ["*"]
                },
                new ShellExtension
                {
                    Id = "{NilesoftShellPropertyHandler}",
                    Name = "Nilesoft Properties",
                    Description = "Enhanced file properties",
                    Version = "2.0.0",
                    IsEnabled = true,
                    IsSystem = false,
                    Type = ShellExtensionType.PropertySheet,
                    SupportedFileTypes = ["*"]
                }
            };

            _extensions.AddRange(nilesoftExtensions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Nilesoft extensions");
        }
    }

    private void LoadSystemExtensions()
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(@"*\shellex\ContextMenuHandlers");
            if (key == null)
                return;

            var systemExtensions = new[]
            {
                new ShellExtension
                {
                    Id = "{09799AFB-CE67-4C61-ADE0-5A180AC1C2FC}",
                    Name = "Windows Defender",
                    Description = "Windows Defender context menu",
                    IsEnabled = true,
                    IsSystem = true,
                    Type = ShellExtensionType.ContextMenu,
                    SupportedFileTypes = ["*"]
                },
                new ShellExtension
                {
                    Id = "{B96E84E5-9E2D-4D75-BB61-33C991405846}",
                    Name = "Windows Security",
                    Description = "Windows Security context menu",
                    IsEnabled = true,
                    IsSystem = true,
                    Type = ShellExtensionType.ContextMenu,
                    SupportedFileTypes = ["*"]
                }
            };

            _extensions.AddRange(systemExtensions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load system extensions");
        }
    }

    private void LoadThirdPartyExtensions()
    {
        try
        {
            // This would scan for third-party shell extensions
            // For now, we'll add some common examples
            var thirdPartyExtensions = new[]
            {
                new ShellExtension
                {
                    Id = "{7CC3D766-93EA-4F8A-9D6C-53A2ED30A48F}",
                    Name = "7-Zip",
                    Description = "7-Zip compression utility",
                    Version = "23.01",
                    IsEnabled = false, // Example: disabled by default
                    IsSystem = false,
                    Type = ShellExtensionType.ContextMenu,
                    SupportedFileTypes = [".zip", ".rar", ".7z", ".tar", ".gz"]
                },
                new ShellExtension
                {
                    Id = "{B3A1A7B1-5B9A-4F2C-8D6A-8A7B8C9D0E1F}",
                    Name = "Notepad++",
                    Description = "Notepad++ text editor",
                    Version = "8.5.3",
                    IsEnabled = true,
                    IsSystem = false,
                    Type = ShellExtensionType.ContextMenu,
                    SupportedFileTypes = [".txt", ".log", ".ini", ".xml", ".json", ".cs", ".cpp", ".h"]
                }
            };

            _extensions.AddRange(thirdPartyExtensions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load third-party extensions");
        }
    }

    private string registryPath => extensionType switch
    {
        ShellExtensionType.ContextMenu => @"*\shellex\ContextMenuHandlers",
        ShellExtensionType.PropertySheet => @"*\shellex\PropertySheetHandlers",
        ShellExtensionType.IconHandler => @"*\shellex\IconHandler",
        ShellExtensionType.DropHandler => @"*\shellex\DropHandler",
        ShellExtensionType.DataHandler => @"*\shellex\DataHandler",
        ShellExtensionType.ColumnProvider => @"*\shellex\ColumnProvider",
        _ => @"*\shellex"
    };

    private ShellExtensionType extensionType { get; set; } = ShellExtensionType.ContextMenu;
}