using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.ShellIntegration.Services;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Lib.System;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

/// <summary>
/// Shell Integration Plugin - Windows Shell Extension Enhancement
/// </summary>
[SupportedOSPlatform("windows")]
[Plugin("shell-integration", "Shell Integration", "1.0.0", "Enhanced Windows Shell integration with context menu extensions", "Lenovo Legion Toolkit")]
public class ShellIntegrationPlugin : LenovoLegionToolkit.Lib.Plugins.PluginBase, IShellIntegrationHelper
{
    public override string Id => "shell-integration";
    
    public override string Name => "Shell Integration";
    
    public override string Description => "Enhanced Windows Shell integration with context menu extensions and file management tools";
    
    public override string Icon => "ContextMenu24";
    
    public override bool IsSystemPlugin => true;

    /// <summary>
    /// Get settings page provided by this plugin
    /// </summary>
    public override object? GetSettingsPage()
    {
        try
        {
            // We instantiate the settings page directly because plugin services 
            // are not registered in the main IoC container.
            // This avoids hardcoding plugin-specific services in the main application.
            var loggerFactory = IoCContainer.TryResolve<Microsoft.Extensions.Logging.ILoggerFactory>();
            
            var menuLogger = loggerFactory?.CreateLogger<ContextMenuItemManager>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ContextMenuItemManager>.Instance;
            var extensionLogger = loggerFactory?.CreateLogger<ShellExtensionManager>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ShellExtensionManager>.Instance;
            var optimizationLogger = loggerFactory?.CreateLogger<ShellOptimizationService>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ShellOptimizationService>.Instance;
            var pageLogger = loggerFactory?.CreateLogger<ShellIntegrationSettingsPage>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ShellIntegrationSettingsPage>.Instance;

            var menuManager = new ContextMenuItemManager(menuLogger);
            var extensionManager = new ShellExtensionManager(extensionLogger);
            var optimizationService = new ShellOptimizationService(optimizationLogger);
            
            return new ShellIntegrationSettingsPage(pageLogger, menuManager, extensionManager, optimizationService);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to create ShellIntegrationSettingsPage: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Get optimization category provided by this plugin
    /// </summary>
    public override WindowsOptimizationCategoryDefinition? GetOptimizationCategory()
    {
        try
        {
            var actions = new List<WindowsOptimizationActionDefinition>();
            
            // Check if Nilesoft Shell is installed and registered
            var isInstalled = NilesoftShellHelper.IsInstalled();
            var isInstalledUsingShellDll = NilesoftShellHelper.IsInstalledUsingShellDll();

            // Instantiate ShellOptimizationService
            var loggerFactory = IoCContainer.TryResolve<Microsoft.Extensions.Logging.ILoggerFactory>();
            var optimizationLogger = loggerFactory?.CreateLogger<ShellOptimizationService>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ShellOptimizationService>.Instance;
            var optimizationService = new ShellOptimizationService(optimizationLogger);

            // Action 1: Enable/Disable Modern Context Menu (Nilesoft Shell)
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.contextMenu",
                 isInstalledUsingShellDll ? "ShellIntegration_Action_NilesoftShell_Uninstall_Title" : "ShellIntegration_Action_NilesoftShell_Enable_Title",
                 isInstalledUsingShellDll ? "ShellIntegration_Action_NilesoftShell_Uninstall_Description" : "ShellIntegration_Action_NilesoftShell_Enable_Description",
                async ct =>
                {
                    var shellDll = NilesoftShellHelper.GetNilesoftShellDllPath();
                    if (string.IsNullOrWhiteSpace(shellDll))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Nilesoft Shell not found. Command skipped.");
                        return;
                    }

                    if (NilesoftShellHelper.IsInstalledUsingShellDll())
                    {
                        // Unregister shell DLL
                        await ExecuteCommandsSequentiallyAsync(ct, $"regsvr32.exe /s /u \"{shellDll}\"");
                    }
                    else
                    {
                        // Register shell DLL
                        await ExecuteCommandsSequentiallyAsync(ct, $"regsvr32.exe /s \"{shellDll}\"");
                    }
                },
                Recommended: false,
                IsAppliedAsync: async ct => await Task.FromResult(NilesoftShellHelper.IsInstalledUsingShellDll())
            ));

            // Action 2: Transparency
            actions.Add(new WindowsOptimizationActionDefinition(
                "beautify.transparency",
                "ShellIntegration_Action_Transparency_Title",
                "ShellIntegration_Action_Transparency_Description",
                async ct =>
                {
                    var enabled = optimizationService.GetTransparency();
                    optimizationService.SetTransparency(!enabled);
                    await Task.CompletedTask;
                },
                Recommended: true,
                IsAppliedAsync: async ct => await Task.FromResult(optimizationService.GetTransparency())
            ));

            // Action 3: File Extensions
            actions.Add(new WindowsOptimizationActionDefinition(
                "beautify.fileExtensions",
                "ShellIntegration_Action_FileExtensions_Title",
                "ShellIntegration_Action_FileExtensions_Description",
                async ct =>
                {
                    var enabled = optimizationService.GetShowFileExtensions();
                    optimizationService.SetShowFileExtensions(!enabled);
                    await Task.CompletedTask;
                },
                Recommended: false,
                IsAppliedAsync: async ct => await Task.FromResult(optimizationService.GetShowFileExtensions())
            ));

            // Action 4: Hidden Files
            actions.Add(new WindowsOptimizationActionDefinition(
                "beautify.hiddenFiles",
                "ShellIntegration_Action_HiddenFiles_Title",
                "ShellIntegration_Action_HiddenFiles_Description",
                async ct =>
                {
                    var enabled = optimizationService.GetShowHiddenFiles();
                    optimizationService.SetShowHiddenFiles(!enabled);
                    await Task.CompletedTask;
                },
                Recommended: false,
                IsAppliedAsync: async ct => await Task.FromResult(optimizationService.GetShowHiddenFiles())
            ));

            // Action 5: Quick Access
            actions.Add(new WindowsOptimizationActionDefinition(
                "beautify.quickAccess",
                "ShellIntegration_Action_QuickAccess_Title",
                "ShellIntegration_Action_QuickAccess_Description",
                async ct =>
                {
                    var enabled = optimizationService.GetQuickAccess();
                    optimizationService.SetQuickAccess(!enabled);
                    await Task.CompletedTask;
                },
                Recommended: false,
                IsAppliedAsync: async ct => await Task.FromResult(optimizationService.GetQuickAccess())
            ));

            // Action 6: Preview Pane
            actions.Add(new WindowsOptimizationActionDefinition(
                "beautify.previewPane",
                "ShellIntegration_Action_PreviewPane_Title",
                "ShellIntegration_Action_PreviewPane_Description",
                async ct =>
                {
                    var enabled = optimizationService.GetPreviewPane();
                    optimizationService.SetPreviewPane(!enabled);
                    await Task.CompletedTask;
                },
                Recommended: false,
                IsAppliedAsync: async ct => await Task.FromResult(optimizationService.GetPreviewPane())
            ));

             // Return category if we have actions
            if (actions.Count > 0)
            {
                 return new WindowsOptimizationCategoryDefinition(
                     "beautify.shell-integration",
                     "ShellIntegration_Category_Title",
                     "ShellIntegration_Category_Description",
                     actions,
                     PluginId: Id
                 );
            }

            return null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to create ShellIntegration optimization category: {ex.Message}", ex);
            return null;
        }
    }

    private static bool GetTransparencyEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            var value = key?.GetValue("EnableTransparency");
            return Convert.ToInt32(value ?? 0) != 0;
        }
        catch
        {
            return false;
        }
    }

    private static void SetTransparencyEnabled(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            key?.SetValue("EnableTransparency", enabled ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch
        {
            // ignore
        }
    }

    private static async Task ToggleTransparencyAsync()
    {
        var enabled = GetTransparencyEnabled();
        SetTransparencyEnabled(!enabled);
        await Task.CompletedTask;
    }

    private static string? GetShellConfigPath()
    {
        try
        {
            var ShellDllPath = NilesoftShellHelper.GetNilesoftShellDllPath();
            if (string.IsNullOrWhiteSpace(ShellDllPath))
                return null;
                 
            var shellDir = Path.GetDirectoryName(ShellDllPath);
            if (string.IsNullOrWhiteSpace(shellDir))
                return null;
                 
            return Path.Combine(shellDir, "shell.nss");
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateShellConfig(string theme, bool transparencyEnabled, bool roundedCornersEnabled, bool shadowsEnabled)
    {
        string themeColors;
        switch (theme)
        {
            case "light":
                themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                break;
            case "dark":
                themeColors = "background-color: #2d2d2d;\ntext-color: #ffffff;";
                break;
            case "classic":
                themeColors = "background-color: #f0f0f0;\ntext-color: #000000;";
                break;
            case "modern":
                themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                break;
            default:
                themeColors = "background-color: #ffffff;\ntext-color: #000000;";
                break;
        }

        return "# Generated by Lenovo Legion Toolkit\n" +
               $"# Theme: {theme}\n" +
               $"# Transparency: {(transparencyEnabled ? "enabled" : "disabled")}\n" +
               $"# Rounded corners: {(roundedCornersEnabled ? "enabled" : "disabled")}\n" +
               $"# Shadows: {(shadowsEnabled ? "enabled" : "disabled")}\n" +
               $"\n" +
               $"# Import base theme configuration\n" +
               $"import 'imports/theme.nss'\n" +
               $"import 'imports/images.nss'\n" +
               $"import 'imports/modify.nss'\n" +
               $"\n" +
               $"# Theme settings based on user selection\n" +
                $"theme\n" +
                "{\n" +
                $"    # Appearance settings\n" +
               $"    corner-radius: {(roundedCornersEnabled ? "5" : "0")}px;\n" +
               $"    shadow: {(shadowsEnabled ? "true" : "false")};\n" +
               $"    transparency: {(transparencyEnabled ? "true" : "false")};\n" +
               $"\n" +
               $"    # Color settings based on selected theme\n" +
               $"    {themeColors}\n" +
               $"}}\n" +
               $"\n" +
               $"# Additional configuration for different contexts\n" +
                $".menu\n" +
                "{\n" +
                $"    padding: 4px;\n" +
               $"    border-width: 1px;\n" +
               $"    border-style: solid;\n" +
               $"    {(roundedCornersEnabled ? "border-radius: 5px;" : "")}\n" +
               $"}}\n" +
               $"\n" +
                $".separator\n" +
                "{\n" +
                $"    height: 1px;\n" +
               $"    margin: 4px 20px;\n" +
               $"}}\n";
    }

    private static async Task ApplyThemeAsync(string theme)
    {
        var configPath = GetShellConfigPath();
        if (string.IsNullOrWhiteSpace(configPath))
            return;

        var transparencyEnabled = GetTransparencyEnabled();
        var roundedCornersEnabled = GetRoundedCornersEnabled();
        var shadowsEnabled = GetShadowsEnabled();
        
        var config = GenerateShellConfig(theme, transparencyEnabled, roundedCornersEnabled, shadowsEnabled);
        await File.WriteAllTextAsync(configPath, config);
    }

    private static string GetCurrentTheme()
    {
        // Default to auto
        return "auto";
    }

    private static bool GetRoundedCornersEnabled()
    {
        // Default to true
        return true;
    }

    private static async Task ToggleRoundedCornersAsync()
    {
        await Task.CompletedTask;
        // Placeholder
    }

    private static bool GetShadowsEnabled()
    {
        // Default to true
        return true;
    }

    private static async Task ToggleShadowsAsync()
    {
        await Task.CompletedTask;
        // Placeholder
    }

    private async Task ExecuteCommandsSequentiallyAsync(CancellationToken ct, params string[] commands)
    {
        foreach (var command in commands)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                await Task.Run(() =>
                {
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var process = System.Diagnostics.Process.Start(processStartInfo);
                    if (process != null)
                        process.WaitForExit();
                }, ct);
            }
            catch (Exception ex)
            {
                if (OperatingSystem.IsWindows() && Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to execute command: {command}. Error: {ex.Message}", ex);
            }
        }
    }

    #region IShellIntegrationHelper Implementation
    
    public bool IsInstalled() => NilesoftShellHelper.IsInstalled();
    
    public bool IsInstalledUsingShellExe() => NilesoftShellHelper.IsInstalledUsingShellExe();
    
    public Task<bool> IsInstalledUsingShellExeAsync() => NilesoftShellHelper.IsInstalledUsingShellExeAsync();
    
    public bool IsInstalledUsingShellDll() => NilesoftShellHelper.IsInstalledUsingShellDll();
    
    public Task<bool> IsInstalledUsingShellDllAsync() => NilesoftShellHelper.IsInstalledUsingShellDllAsync();
    
    public string? GetNilesoftShellExePath() => NilesoftShellHelper.GetNilesoftShellExePath();
    
    public string? GetNilesoftShellDllPath() => NilesoftShellHelper.GetNilesoftShellDllPath();
    
    public void ClearInstallationStatusCache() => NilesoftShellHelper.ClearInstallationStatusCache();
    
    public void ClearRegistryInstallationStatus() => NilesoftShellHelper.ClearRegistryInstallationStatus();
    
    #endregion
    
    /// <summary>
    /// Called before plugin update or uninstallation to stop any running processes
    /// </summary>
    public override void Stop()
    {
        try
        {
            // Check if Shell Integration is installed and needs to be unregistered before update
            if (NilesoftShellHelper.IsInstalledUsingShellDll())
            {
                var shellDll = NilesoftShellHelper.GetNilesoftShellDllPath();
                if (!string.IsNullOrWhiteSpace(shellDll))
                {
                    // Unregister shell DLL before update
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "regsvr32.exe",
                        Arguments = $"/s /u \"{shellDll}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var process = System.Diagnostics.Process.Start(processStartInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Shell Integration unregistered before plugin update: {shellDll}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to stop Shell Integration before update: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Called when plugin is installed
    /// </summary>
    public override void OnInstalled()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shell Integration plugin installed");
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsWindows() && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error during Shell Integration plugin installation: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Called when plugin is uninstalled
    /// </summary>
    public override void OnUninstalled()
    {
        try
        {
            // Clean up Shell Integration if installed
            if (NilesoftShellHelper.IsInstalledUsingShellDll())
            {
                var shellDll = NilesoftShellHelper.GetNilesoftShellDllPath();
                if (!string.IsNullOrWhiteSpace(shellDll))
                {
                    // Unregister shell DLL during uninstallation
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "regsvr32.exe",
                        Arguments = $"/s /u \"{shellDll}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var process = System.Diagnostics.Process.Start(processStartInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Shell Integration unregistered during plugin uninstallation: {shellDll}");
                    }
                }
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shell Integration plugin uninstalled");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error during Shell Integration plugin uninstallation: {ex.Message}", ex);
        }
    }
}