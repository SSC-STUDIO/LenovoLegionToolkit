using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Application service interface for managing applications
/// </summary>
public interface IApplicationService
{
    /// <summary>
    /// Scan for available applications from Start Menu and Desktop
    /// </summary>
    Task<List<ApplicationInfo>> ScanApplicationsAsync();

    /// <summary>
    /// Get application icon
    /// </summary>
    ImageSource? GetApplicationIcon(string executablePath);

    /// <summary>
    /// Launch an application
    /// </summary>
    Task<bool> LaunchApplicationAsync(string executablePath);

    /// <summary>
    /// Check if an application is running
    /// </summary>
    bool IsApplicationRunning(string executablePath);

    /// <summary>
    /// Get running process IDs for an application
    /// </summary>
    List<int> GetRunningProcessIds(string executablePath);
}

/// <summary>
/// Application information
/// </summary>
public class ApplicationInfo
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public ImageSource? Icon { get; set; }
}


