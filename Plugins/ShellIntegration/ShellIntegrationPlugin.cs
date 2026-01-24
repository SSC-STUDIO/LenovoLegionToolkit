using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.ShellIntegration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

/// <summary>
/// Shell Integration Plugin - Windows Shell Extension Enhancement
/// </summary>
[Plugin("shell-integration")]
public class ShellIntegrationPlugin : PluginBase
{
    public override string Id => "shell-integration";
    
    public override string Name => "Shell Integration";
    
    public override string Description => "Enhanced Windows Shell integration with context menu extensions and file management tools";
    
    public override string Icon => "ContextMenu24";
    
    public override bool IsSystemPlugin => true;

    private IServiceProvider? _serviceProvider;
    private ILogger<ShellIntegrationPlugin>? _logger;
    private IShellIntegrationService? _shellService;

    public override void OnInstalled()
    {
        _logger?.LogInformation("Shell Integration plugin installed");
    }

    public override void OnUninstalled()
    {
        _shellService?.StopAsync().Wait();
        _logger?.LogInformation("Shell Integration plugin uninstalled");
    }

    public override void Stop()
    {
        _shellService?.StopAsync().Wait();
        _logger?.LogInformation("Shell Integration plugin stopped");
    }

    public override object? GetFeatureExtension()
    {
        return _serviceProvider?.GetService<ShellIntegrationPage>();
    }

    public override object? GetSettingsPage()
    {
        return _serviceProvider?.GetService<ShellIntegrationSettingsPage>();
    }

    /// <summary>
    /// Initialize services for the plugin
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IShellIntegrationService, ShellIntegrationService>();
        services.AddSingleton<IContextMenuItemManager, ContextMenuItemManager>();
        services.AddSingleton<IShellExtensionManager, ShellExtensionManager>();
        
        // Register UI pages
        services.AddSingleton<ShellIntegrationPage>();
        services.AddSingleton<ShellIntegrationSettingsPage>();

        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        
        // Get logger instance
        _logger = _serviceProvider.GetService<ILogger<ShellIntegrationPlugin>>();
        _shellService = _serviceProvider.GetService<IShellIntegrationService>();
        
        _logger?.LogInformation("Shell Integration plugin services configured");
    }
}