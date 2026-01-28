using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.ShellIntegration.Services;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Lib.System;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

/// <summary>
/// Shell Integration Plugin - Windows Shell Extension Enhancement
/// </summary>
[Plugin("shell-integration", "Shell Integration", "1.0.0", "Enhanced Windows Shell integration with context menu extensions", "Lenovo Legion Toolkit")]
public class ShellIntegrationPlugin : LenovoLegionToolkit.Lib.Plugins.PluginBase, IShellIntegrationHelper
{
    public override string Id => "shell-integration";
    
    public override string Name => "Shell Integration";
    
    public override string Description => "Enhanced Windows Shell integration with context menu extensions and file management tools";
    
    public override string Icon => "ContextMenu24";
    
    public override bool IsSystemPlugin => true;

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
            var isInstalledUsingShellExe = NilesoftShellHelper.IsInstalledUsingShellExe();

            // Action 1: Enable/Disable Modern Context Menu (Nilesoft Shell)
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.contextMenu",
                 isInstalledUsingShellExe ? "ShellIntegration_Action_NilesoftShell_Uninstall_Title" : "ShellIntegration_Action_NilesoftShell_Enable_Title",
                 isInstalledUsingShellExe ? "ShellIntegration_Action_NilesoftShell_Uninstall_Description" : "ShellIntegration_Action_NilesoftShell_Enable_Description",
                async ct =>
                {
                    var shellDll = NilesoftShellHelper.GetNilesoftShellDllPath();
                    if (string.IsNullOrWhiteSpace(shellDll))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Nilesoft Shell not found. Command skipped.");
                        return;
                    }

                    if (isInstalledUsingShellExe)
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
                IsAppliedAsync: async ct => await Task.FromResult(isInstalledUsingShellExe)
            ));

            // Action 2: Enable/Disable Context Menu Animations
            actions.Add(new WindowsOptimizationActionDefinition(
                "shell-integration.animations.contextMenu",
                "ShellIntegration_Action_ContextMenuAnimations_Title",
                "ShellIntegration_Action_ContextMenuAnimations_Description",
                async ct =>
                {
                    // Toggle context menu animations in registry
                    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
                    if (key != null)
                    {
                        var currentValue = key.GetValue("MenuShowDelay")?.ToString();
                        key.SetValue("MenuShowDelay", currentValue == "0" ? "400" : "0", Microsoft.Win32.RegistryValueKind.String);
                    }
                },
                Recommended: false,
                IsAppliedAsync: async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                    var value = key?.GetValue("MenuShowDelay")?.ToString();
                    return await Task.FromResult(value == "0");
                }
            ));

            // Action 3: Enable/Disable Show File Extensions
            actions.Add(new WindowsOptimizationActionDefinition(
                "shell-integration.visibility.showFileExt",
                "ShellIntegration_Action_ShowFileExtensions_Title",
                "ShellIntegration_Action_ShowFileExtensions_Description",
                async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    if (key != null)
                    {
                        var currentValue = key.GetValue("HideFileExt") as int?;
                        key.SetValue("HideFileExt", currentValue == 0 ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                },
                Recommended: false,
                IsAppliedAsync: async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    var value = key?.GetValue("HideFileExt") as int?;
                    return await Task.FromResult(value == 0);
                }
            ));

            // Action 4: Enable/Disable Show Hidden Files
            actions.Add(new WindowsOptimizationActionDefinition(
                "shell-integration.visibility.showHiddenFiles",
                "ShellIntegration_Action_ShowHiddenFiles_Title",
                "ShellIntegration_Action_ShowHiddenFiles_Description",
                async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    if (key != null)
                    {
                        var currentValue = key.GetValue("Hidden") as int?;
                        key.SetValue("Hidden", currentValue == 1 ? 0 : 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                },
                Recommended: false,
                IsAppliedAsync: async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    var value = key?.GetValue("Hidden") as int?;
                    return await Task.FromResult(value == 1);
                }
            ));

            // Action 5: Enable/Disable Quick Access in File Explorer
            actions.Add(new WindowsOptimizationActionDefinition(
                "shell-integration.navigation.quickAccess",
                "ShellIntegration_Action_QuickAccess_Title",
                "ShellIntegration_Action_QuickAccess_Description",
                async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    if (key != null)
                    {
                        var currentValue = key.GetValue("LaunchTo") as int?;
                        key.SetValue("LaunchTo", currentValue == 2 ? 1 : 2, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                },
                Recommended: false,
                IsAppliedAsync: async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    var value = key?.GetValue("LaunchTo") as int?;
                    return await Task.FromResult(value == 2);
                }
            ));

            // Action 6: Enable/Disable File Explorer Preview Pane
            actions.Add(new WindowsOptimizationActionDefinition(
                "shell-integration.view.previewPane",
                "ShellIntegration_Action_PreviewPane_Title",
                "ShellIntegration_Action_PreviewPane_Description",
                async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    if (key != null)
                    {
                        var currentValue = key.GetValue("PreviewPane") as int?;
                        key.SetValue("PreviewPane", currentValue == 1 ? 0 : 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                },
                Recommended: false,
                IsAppliedAsync: async ct =>
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    var value = key?.GetValue("PreviewPane") as int?;
                    return await Task.FromResult(value == 1);
                }
            ));

             // Beautification Actions
             // Theme: Auto
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.theme.auto",
                 "ShellIntegration_Action_ThemeAuto_Title",
                 "ShellIntegration_Action_ThemeAuto_Description",
                 async ct =>
                 {
                     await ApplyThemeAsync("auto");
                 },
                 Recommended: false,
                 IsAppliedAsync: async ct => await Task.FromResult(GetCurrentTheme() == "auto")
             ));
             
             // Theme: Light
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.theme.light",
                 "ShellIntegration_Action_ThemeLight_Title",
                 "ShellIntegration_Action_ThemeLight_Description",
                 async ct =>
                 {
                     await ApplyThemeAsync("light");
                 },
                 Recommended: false,
                 IsAppliedAsync: async ct => await Task.FromResult(GetCurrentTheme() == "light")
             ));
             
             // Theme: Dark
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.theme.dark",
                 "ShellIntegration_Action_ThemeDark_Title",
                 "ShellIntegration_Action_ThemeDark_Description",
                 async ct =>
                 {
                     await ApplyThemeAsync("dark");
                 },
                 Recommended: false,
                 IsAppliedAsync: async ct => await Task.FromResult(GetCurrentTheme() == "dark")
             ));
             
             // Theme: Classic
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.theme.classic",
                 "ShellIntegration_Action_ThemeClassic_Title",
                 "ShellIntegration_Action_ThemeClassic_Description",
                 async ct =>
                 {
                     await ApplyThemeAsync("classic");
                 },
                 Recommended: false,
                 IsAppliedAsync: async ct => await Task.FromResult(GetCurrentTheme() == "classic")
             ));
             
             // Theme: Modern
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.theme.modern",
                 "ShellIntegration_Action_ThemeModern_Title",
                 "ShellIntegration_Action_ThemeModern_Description",
                 async ct =>
                 {
                     await ApplyThemeAsync("modern");
                 },
                 Recommended: false,
                 IsAppliedAsync: async ct => await Task.FromResult(GetCurrentTheme() == "modern")
             ));
             
             // Transparency
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.transparency",
                 "ShellIntegration_Action_Transparency_Title",
                 "ShellIntegration_Action_Transparency_Description",
                 async ct =>
                 {
                     await ToggleTransparencyAsync();
                 },
                 Recommended: false,
                 IsAppliedAsync: async ct => await Task.FromResult(GetTransparencyEnabled())
             ));
             
             // Rounded Corners
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.roundedCorners",
                 "ShellIntegration_Action_RoundedCorners_Title",
                 "ShellIntegration_Action_RoundedCorners_Description",
                 async ct =>
                 {
                     await ToggleRoundedCornersAsync();
                 },
                 Recommended: false,
                 IsAppliedAsync: async ct => await Task.FromResult(GetRoundedCornersEnabled())
             ));
             
             // Shadows
             actions.Add(new WindowsOptimizationActionDefinition(
                 "beautify.shadows",
                 "ShellIntegration_Action_Shadows_Title",
                 "ShellIntegration_Action_Shadows_Description",
                 async ct =>
                 {
                     await ToggleShadowsAsync();
                 },
                 Recommended: false,
                 IsAppliedAsync: async ct => await Task.FromResult(GetShadowsEnabled())
             ));
             
             // Return category if we have actions
            if (actions.Count > 0)
            {
                 return new WindowsOptimizationCategoryDefinition(
                     "beautify.shell-integration",
                     "ShellIntegration_Category_Title",
                     "ShellIntegration_Category_Description",
                     actions
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

    private static string GetShellConfigPath()
    {
        try
        {
            var shellExePath = NilesoftShellHelper.GetNilesoftShellExePath();
            if (string.IsNullOrWhiteSpace(shellExePath))
                return null;
                
            var shellDir = Path.GetDirectoryName(shellExePath);
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
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to execute command: {command}. Error: {ex.Message}", ex);
            }
        }
    }

    #region IShellIntegrationHelper Implementation
    
    public bool IsInstalled() => NilesoftShellHelper.IsInstalled();
    
    public bool IsInstalledUsingShellExe() => NilesoftShellHelper.IsInstalledUsingShellExe();
    
    public Task<bool> IsInstalledUsingShellExeAsync() => NilesoftShellHelper.IsInstalledUsingShellExeAsync();
    
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
            if (NilesoftShellHelper.IsInstalledUsingShellExe())
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
            if (Log.Instance.IsTraceEnabled)
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
            if (NilesoftShellHelper.IsInstalledUsingShellExe())
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