using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using Registry = Microsoft.Win32.Registry;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Services;

public interface IShellOptimizationService
{
    bool GetContextMenuAnimations();
    void SetContextMenuAnimations(bool enabled);
    
    bool GetShowFileExtensions();
    void SetShowFileExtensions(bool enabled);
    
    bool GetShowHiddenFiles();
    void SetShowHiddenFiles(bool enabled);
    
    bool GetQuickAccess();
    void SetQuickAccess(bool enabled);
    
    bool GetPreviewPane();
    void SetPreviewPane(bool enabled);

    string GetCurrentTheme();
    Task SetThemeAsync(string theme);

    bool GetTransparency();
    void SetTransparency(bool enabled);

    bool GetRoundedCorners();
    void SetRoundedCorners(bool enabled);

    bool GetShadows();
    void SetShadows(bool enabled);
}

public class ShellOptimizationService : IShellOptimizationService
{
    private readonly ILogger<ShellOptimizationService> _logger;

    public ShellOptimizationService(ILogger<ShellOptimizationService> logger)
    {
        _logger = logger;
    }

    public bool GetContextMenuAnimations()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
        var value = key?.GetValue("MenuShowDelay")?.ToString();
        return value == "0";
    }

    public void SetContextMenuAnimations(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
        key?.SetValue("MenuShowDelay", enabled ? "0" : "400", RegistryValueKind.String);
    }

    public bool GetShowFileExtensions()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        var value = key?.GetValue("HideFileExt") as int?;
        return value == 0;
    }

    public void SetShowFileExtensions(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        key?.SetValue("HideFileExt", enabled ? 0 : 1, RegistryValueKind.DWord);
    }

    public bool GetShowHiddenFiles()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        var value = key?.GetValue("Hidden") as int?;
        return value == 1;
    }

    public void SetShowHiddenFiles(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        key?.SetValue("Hidden", enabled ? 1 : 2, RegistryValueKind.DWord);
    }

    public bool GetQuickAccess()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        var value = key?.GetValue("LaunchTo") as int?;
        return value == 2;
    }

    public void SetQuickAccess(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        key?.SetValue("LaunchTo", enabled ? 2 : 1, RegistryValueKind.DWord);
    }

    public bool GetPreviewPane()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        var value = key?.GetValue("PreviewPane") as int?;
        return value == 1;
    }

    public void SetPreviewPane(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        key?.SetValue("PreviewPane", enabled ? 1 : 0, RegistryValueKind.DWord);
    }

    public string GetCurrentTheme()
    {
        // For now, return auto
        return "auto";
    }

    public async Task SetThemeAsync(string theme)
    {
        // This logic was in ShellIntegrationPlugin.ApplyThemeAsync
        var configPath = GetShellConfigPath();
        if (string.IsNullOrWhiteSpace(configPath))
            return;

        var transparencyEnabled = GetTransparency();
        var roundedCornersEnabled = GetRoundedCorners();
        var shadowsEnabled = GetShadows();
        
        var config = GenerateShellConfig(theme, transparencyEnabled, roundedCornersEnabled, shadowsEnabled);
        await System.IO.File.WriteAllTextAsync(configPath, config);
    }

    public bool GetTransparency()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            var value = key?.GetValue("EnableTransparency");
            return Convert.ToInt32(value ?? 0) != 0;
        }
        catch
        {
            return false;
        }
    }

    public void SetTransparency(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            key?.SetValue("EnableTransparency", enabled ? 1 : 0, RegistryValueKind.DWord);
        }
        catch
        {
            // ignore
        }
    }

    public bool GetRoundedCorners() => true; // Placeholder
    public void SetRoundedCorners(bool enabled) { } // Placeholder

    public bool GetShadows() => true; // Placeholder
    public void SetShadows(bool enabled) { } // Placeholder

    private static string? GetShellConfigPath()
    {
        var ShellDllPath = NilesoftShellHelper.GetNilesoftShellDllPath();
        if (string.IsNullOrWhiteSpace(ShellDllPath))
            return null;
            
        var shellDir = System.IO.Path.GetDirectoryName(ShellDllPath);
        return !string.IsNullOrEmpty(shellDir) ? System.IO.Path.Combine(shellDir, "shell.nss") : null;
    }

    private static string GenerateShellConfig(string theme, bool transparencyEnabled, bool roundedCornersEnabled, bool shadowsEnabled)
    {
        string themeColors = theme switch
        {
            "light" => "background-color: #ffffff;\ntext-color: #000000;",
            "dark" => "background-color: #2d2d2d;\ntext-color: #ffffff;",
            "classic" => "background-color: #f0f0f0;\ntext-color: #000000;",
            "modern" => "background-color: #ffffff;\ntext-color: #000000;",
            _ => "background-color: #ffffff;\ntext-color: #000000;",
        };

        return "# Generated by Lenovo Legion Toolkit\n" +
               $"# Theme: {theme}\n" +
               $"# Transparency: {(transparencyEnabled ? "enabled" : "disabled")}\n" +
               $"# Rounded corners: {(roundedCornersEnabled ? "enabled" : "disabled")}\n" +
               $"# Shadows: {(shadowsEnabled ? "enabled" : "disabled")}\n" +
               $"\n" +
               $"import 'imports/theme.nss'\n" +
               $"import 'imports/images.nss'\n" +
               $"import 'imports/modify.nss'\n" +
               $"\n" +
               $"theme\n" +
               "{\n" +
               $"    corner-radius: {(roundedCornersEnabled ? "5" : "0")}px;\n" +
               $"    shadow: {(shadowsEnabled ? "true" : "false")};\n" +
               $"    transparency: {(transparencyEnabled ? "true" : "false")};\n" +
               $"    {themeColors}\n" +
               "}\n";
    }
}
