using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Services;

/// <summary>
/// Shell Integration service implementation
/// </summary>
[SupportedOSPlatform("windows")]
public class ShellIntegrationService : IShellIntegrationService
{
    private readonly ILogger<ShellIntegrationService> _logger;
    private readonly string _shellIntegrationPath;
    private readonly string _shellDllPath;
    private readonly string _ShellDllPath;
    private bool _isRunning = false;
    private ShellIntegrationStatus _status = ShellIntegrationStatus.NotInstalled;

    // Registry paths for shell extension registration
    private const string ShellExtensionRegistryPath = @"Software\Nilesoft\Shell";
    private const string ContextMenuHandlersPath = @"*\shellex\ContextMenuHandlers\NilesoftShell";

    public bool IsRunning => _isRunning;

    public bool IsInstalled => CheckInstallationStatus();

    public ShellIntegrationStatus Status => _status;

    public event EventHandler<ShellIntegrationStatusEventArgs>? StatusChanged;

    public ShellIntegrationService(ILogger<ShellIntegrationService> logger)
    {
        _logger = logger;
        
        // Set paths relative to the plugin directory
        var pluginDir = Path.GetDirectoryName(typeof(ShellIntegrationService).Assembly.Location) ?? "";
        var shellIntegrationDir = Path.Combine(pluginDir, "ShellIntegration");
        _shellIntegrationPath = shellIntegrationDir;
        _shellDllPath = Path.Combine(shellIntegrationDir, "shell.dll");
        _ShellDllPath = Path.Combine(shellIntegrationDir, "shell.exe");

        UpdateStatus(ShellIntegrationStatus.NotInstalled, "Service initialized");
    }

    public async Task<bool> InstallAsync()
    {
        try
        {
            _logger.LogInformation("Installing Shell Integration...");

            UpdateStatus(ShellIntegrationStatus.Updating, "Installing shell extension...");

            // Check if ShellIntegration files exist
            if (!Directory.Exists(_shellIntegrationPath) || !File.Exists(_shellDllPath) || !File.Exists(_ShellDllPath))
            {
                // Try to extract from resources or download
                await EnsureShellIntegrationFilesAsync();
            }

            // Register shell extension
            var registered = await RegisterShellExtensionAsync();
            if (!registered)
            {
                UpdateStatus(ShellIntegrationStatus.Error, "Failed to register shell extension");
                return false;
            }

            UpdateStatus(ShellIntegrationStatus.Stopped, "Shell extension installed successfully");
            _logger.LogInformation("Shell Integration installed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install Shell Integration");
            UpdateStatus(ShellIntegrationStatus.Error, $"Installation failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UninstallAsync()
    {
        try
        {
            _logger.LogInformation("Uninstalling Shell Integration...");

            // Stop the service first
            await StopAsync();

            UpdateStatus(ShellIntegrationStatus.Updating, "Uninstalling shell extension...");

            // Unregister shell extension
            var unregistered = await UnregisterShellExtensionAsync();
            if (!unregistered)
            {
                _logger.LogWarning("Failed to unregister shell extension, continuing...");
            }

            UpdateStatus(ShellIntegrationStatus.NotInstalled, "Shell extension uninstalled successfully");
            _logger.LogInformation("Shell Integration uninstalled successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall Shell Integration");
            UpdateStatus(ShellIntegrationStatus.Error, $"Uninstallation failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StartAsync()
    {
        try
        {
            if (_isRunning)
            {
                _logger.LogDebug("Shell Integration is already running");
                return true;
            }

            if (!IsInstalled)
            {
                _logger.LogWarning("Shell Integration is not installed");
                return false;
            }

            UpdateStatus(ShellIntegrationStatus.Starting, "Starting shell integration service...");

            // Start shell integration if necessary
            await Task.Run(() =>
            {
                // Here we could start a background service or initialize components
                // For now, we'll just mark it as running
            });

            _isRunning = true;
            UpdateStatus(ShellIntegrationStatus.Running, "Shell integration service started successfully");
            _logger.LogInformation("Shell Integration started successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Shell Integration");
            UpdateStatus(ShellIntegrationStatus.Error, $"Failed to start: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopAsync()
    {
        try
        {
            if (!_isRunning)
            {
                _logger.LogDebug("Shell Integration is not running");
                return true;
            }

            UpdateStatus(ShellIntegrationStatus.Stopping, "Stopping shell integration service...");

            await Task.Run(() =>
            {
                // Stop any running processes or services
                // For now, we'll just mark it as stopped
            });

            _isRunning = false;
            UpdateStatus(ShellIntegrationStatus.Stopped, "Shell integration service stopped");
            _logger.LogInformation("Shell Integration stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop Shell Integration");
            UpdateStatus(ShellIntegrationStatus.Error, $"Failed to stop: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestartAsync()
    {
        _logger.LogInformation("Restarting Shell Integration...");
        
        var stopped = await StopAsync();
        if (!stopped)
        {
            return false;
        }

        await Task.Delay(1000); // Brief pause
        return await StartAsync();
    }

    public string GetStatusDescription()
    {
        return Status switch
        {
            ShellIntegrationStatus.NotInstalled => "Not Installed",
            ShellIntegrationStatus.Stopped => "Stopped",
            ShellIntegrationStatus.Starting => "Starting...",
            ShellIntegrationStatus.Running => "Running",
            ShellIntegrationStatus.Stopping => "Stopping...",
            ShellIntegrationStatus.Error => "Error",
            ShellIntegrationStatus.Updating => "Updating...",
            _ => "Unknown"
        };
    }

    public async Task<bool> CheckRegistrationAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                using var key = Registry.ClassesRoot.OpenSubKey(ContextMenuHandlersPath);
                return key != null;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check shell extension registration");
            return false;
        }
    }

    public async Task<string?> GetVersionAsync()
    {
        try
        {
            if (!File.Exists(_ShellDllPath))
            {
                return null;
            }

            return await Task.Run(() =>
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(_ShellDllPath);
                return versionInfo.FileVersion;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shell extension version");
            return null;
        }
    }

    private bool CheckInstallationStatus()
    {
        try
        {
            // Check if files exist and extension is registered
            var filesExist = Directory.Exists(_shellIntegrationPath) && 
                           File.Exists(_shellDllPath) && 
                           File.Exists(_ShellDllPath);
            
            var isRegistered = CheckRegistrationAsync().GetAwaiter().GetResult();
            
            return filesExist && isRegistered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check installation status");
            return false;
        }
    }

    private async Task EnsureShellIntegrationFilesAsync()
    {
        // In a real implementation, this would extract the ShellIntegration files
        // from embedded resources or download them
        _logger.LogWarning("Shell Integration files not found. This would be extracted from resources or downloaded.");
        
        // For now, create a placeholder directory structure
        Directory.CreateDirectory(_shellIntegrationPath);
        
        await Task.CompletedTask;
    }

    private async Task<bool> RegisterShellExtensionAsync()
    {
        try
        {
            return await Task.Run(async () =>
            {
                // Create registry entries for shell extension
                using var key = Registry.ClassesRoot.CreateSubKey(ContextMenuHandlersPath);
                if (key != null)
                {
                    key.SetValue("", "{NilesoftShellContextMenuHandler}");
                    key.Close();
                }

                // Create application registry entries
                using var appKey = Registry.LocalMachine.CreateSubKey(ShellExtensionRegistryPath);
                if (appKey != null)
                {
                    appKey.SetValue("InstallPath", _shellIntegrationPath);
                    appKey.SetValue("Version", await GetVersionAsync() ?? "1.0.0");
                    appKey.Close();
                }

                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register shell extension");
            return false;
        }
    }

    private async Task<bool> UnregisterShellExtensionAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                // Remove registry entries
                Registry.ClassesRoot.DeleteSubKeyTree(ContextMenuHandlersPath, false);
                Registry.LocalMachine.DeleteSubKeyTree(ShellExtensionRegistryPath, false);
                
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister shell extension");
            return false;
        }
    }

    private void UpdateStatus(ShellIntegrationStatus status, string message = "")
    {
        if (_status != status)
        {
            _status = status;
            StatusChanged?.Invoke(this, new ShellIntegrationStatusEventArgs(status, message));
            _logger.LogDebug("Shell Integration status updated to {Status}: {Message}", status, message);
        }
    }

    public void Dispose()
    {
        if (_isRunning)
        {
            // Use Task.Run to avoid deadlocks on the UI thread during shutdown
            var stopTask = Task.Run(async () => await StopAsync());
            stopTask.Wait(TimeSpan.FromSeconds(3));
        }
    }
}