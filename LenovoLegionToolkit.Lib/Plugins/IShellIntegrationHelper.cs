using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Helper interface for Shell Integration plugin functionality
/// </summary>
public interface IShellIntegrationHelper
{
    /// <summary>
    /// Check if Nilesoft Shell is installed by checking file existence
    /// </summary>
    bool IsInstalled();
    
    /// <summary>
    /// Check if Nilesoft Shell is installed using shell.exe API (checks registry registration)
    /// </summary>
    bool IsInstalledUsingShellExe();
    
    /// <summary>
    /// Check if Nilesoft Shell is installed using shell.exe API asynchronously
    /// </summary>
    Task<bool> IsInstalledUsingShellExeAsync();
    
    /// <summary>
    /// Get the path to shell.exe
    /// </summary>
    string? GetNilesoftShellExePath();
    
    /// <summary>
    /// Get the path to shell.dll
    /// </summary>
    string? GetNilesoftShellDllPath();
    
    /// <summary>
    /// Clear the installation status cache
    /// </summary>
    void ClearInstallationStatusCache();
    
    /// <summary>
    /// Clear the registry installation status
    /// </summary>
    void ClearRegistryInstallationStatus();
}