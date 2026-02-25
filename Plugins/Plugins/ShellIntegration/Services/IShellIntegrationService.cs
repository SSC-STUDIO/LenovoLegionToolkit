using System;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Services;

/// <summary>
/// Interface for Shell Integration service
/// </summary>
public interface IShellIntegrationService : IDisposable
{
    /// <summary>
    /// Whether the service is currently running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Whether the shell extension is installed
    /// </summary>
    bool IsInstalled { get; }

    /// <summary>
    /// Current installation status
    /// </summary>
    ShellIntegrationStatus Status { get; }

    /// <summary>
    /// Install the shell extension
    /// </summary>
    Task<bool> InstallAsync();

    /// <summary>
    /// Uninstall the shell extension
    /// </summary>
    Task<bool> UninstallAsync();

    /// <summary>
    /// Start the shell integration service
    /// </summary>
    Task<bool> StartAsync();

    /// <summary>
    /// Stop the shell integration service
    /// </summary>
    Task<bool> StopAsync();

    /// <summary>
    /// Restart the shell integration service
    /// </summary>
    Task<bool> RestartAsync();

    /// <summary>
    /// Get current status description
    /// </summary>
    string GetStatusDescription();

    /// <summary>
    /// Check if shell extension is properly registered
    /// </summary>
    Task<bool> CheckRegistrationAsync();

    /// <summary>
    /// Get shell extension version
    /// </summary>
    Task<string?> GetVersionAsync();

    /// <summary>
    /// Event triggered when service status changes
    /// </summary>
    event EventHandler<ShellIntegrationStatusEventArgs>? StatusChanged;
}

/// <summary>
/// Shell integration status enumeration
/// </summary>
public enum ShellIntegrationStatus
{
    NotInstalled,
    Stopped,
    Starting,
    Running,
    Stopping,
    Error,
    Updating
}

/// <summary>
/// Event arguments for status changes
/// </summary>
public class ShellIntegrationStatusEventArgs : EventArgs
{
    public ShellIntegrationStatus Status { get; }
    public string Message { get; }

    public ShellIntegrationStatusEventArgs(ShellIntegrationStatus status, string message = "")
    {
        Status = status;
        Message = message;
    }
}