using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Application service implementation
/// </summary>
public class ApplicationService : IApplicationService
{
    public async Task<List<ApplicationInfo>> ScanApplicationsAsync()
    {
        return await Task.Run(() =>
        {
            var applications = new List<ApplicationInfo>();
            var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Scan Start Menu folders
                var startMenuPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs")
                };

                foreach (var startMenuPath in startMenuPaths)
                {
                    if (Directory.Exists(startMenuPath))
                    {
                        ScanDirectory(startMenuPath, applications, processedPaths);
                    }
                }

                // Scan Desktop
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (Directory.Exists(desktopPath))
                {
                    ScanDirectory(desktopPath, applications, processedPaths);
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error scanning applications: {ex.Message}", ex);
            }

            return applications.OrderBy(a => a.Name).ToList();
        });
    }

    private void ScanDirectory(string directory, List<ApplicationInfo> applications, HashSet<string> processedPaths)
    {
        try
        {
            // Scan .lnk files
            var lnkFiles = Directory.GetFiles(directory, "*.lnk", SearchOption.AllDirectories);
            foreach (var lnkFile in lnkFiles)
            {
                try
                {
                    var shellLink = new ShellLink(lnkFile);
                    var targetPath = shellLink.TargetPath;
                    
                    if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                        continue;

                    // Skip if already processed
                    if (processedPaths.Contains(targetPath))
                        continue;

                    // Only include executable files
                    var extension = Path.GetExtension(targetPath).ToLowerInvariant();
                    if (extension != ".exe" && extension != ".bat" && extension != ".cmd")
                        continue;

                    var name = Path.GetFileNameWithoutExtension(lnkFile);
                    if (string.IsNullOrWhiteSpace(name))
                        name = Path.GetFileNameWithoutExtension(targetPath);

                    applications.Add(new ApplicationInfo
                    {
                        Name = name,
                        ExecutablePath = targetPath,
                        IconPath = targetPath
                    });

                    processedPaths.Add(targetPath);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error processing shortcut {lnkFile}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error scanning directory {directory}: {ex.Message}", ex);
        }
    }

    public ImageSource? GetApplicationIcon(string executablePath)
    {
        try
        {
            // Use high-resolution icon service - request larger size for better quality when scaled down
            // Request 512x512 to ensure crisp rendering even when scaled
            return IconService.GetHighResolutionIcon(executablePath, 512);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error getting application icon: {ex.Message}", ex);
            
            // Fallback to original method
            try
            {
                return ImageSourceExtensions.ApplicationIcon(executablePath);
            }
            catch
            {
                return null;
            }
        }
    }

    public async Task<bool> LaunchApplicationAsync(string executablePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(executablePath))
                    return false;

                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error launching application: {ex.Message}", ex);
                return false;
            }
        });
    }

    public bool IsApplicationRunning(string executablePath)
    {
        try
        {
            var processName = Path.GetFileNameWithoutExtension(executablePath);
            var processes = Process.GetProcessesByName(processName);
            
            foreach (var process in processes)
            {
                try
                {
                    if (string.Equals(process.MainModule?.FileName, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore access denied errors
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error checking if application is running: {ex.Message}", ex);
            return false;
        }
    }

    public List<int> GetRunningProcessIds(string executablePath)
    {
        var processIds = new List<int>();
        
        try
        {
            var processName = Path.GetFileNameWithoutExtension(executablePath);
            var processes = Process.GetProcessesByName(processName);
            
            foreach (var process in processes)
            {
                try
                {
                    if (string.Equals(process.MainModule?.FileName, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        processIds.Add(process.Id);
                    }
                }
                catch
                {
                    // Ignore access denied errors
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error getting running process IDs: {ex.Message}", ex);
        }

        return processIds;
    }
}

/// <summary>
/// Simple shell link parser for .lnk files using Windows Script Host
/// </summary>
internal class ShellLink
{
    public string TargetPath { get; }

    public ShellLink(string lnkPath)
    {
        TargetPath = string.Empty;
        
        try
        {
            // Use Windows Script Host COM object to read .lnk files
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                object? shell = Activator.CreateInstance(shellType);
                if (shell != null)
                {
                    object? shortcut = shellType.InvokeMember("CreateShortcut", 
                        System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                    if (shortcut != null)
                    {
                        Type shortcutType = shortcut.GetType();
                        object? targetPath = shortcutType.InvokeMember("TargetPath",
                            System.Reflection.BindingFlags.GetProperty, null, shortcut, null);
                        if (targetPath != null)
                        {
                            TargetPath = targetPath.ToString() ?? string.Empty;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error reading shortcut {lnkPath}: {ex.Message}", ex);
        }
    }
}


            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error reading shortcut {lnkPath}: {ex.Message}", ex);
        }
    }
}


using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Application service implementation
/// </summary>
public class ApplicationService : IApplicationService
{
    public async Task<List<ApplicationInfo>> ScanApplicationsAsync()
    {
        return await Task.Run(() =>
        {
            var applications = new List<ApplicationInfo>();
            var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Scan Start Menu folders
                var startMenuPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs")
                };

                foreach (var startMenuPath in startMenuPaths)
                {
                    if (Directory.Exists(startMenuPath))
                    {
                        ScanDirectory(startMenuPath, applications, processedPaths);
                    }
                }

                // Scan Desktop
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (Directory.Exists(desktopPath))
                {
                    ScanDirectory(desktopPath, applications, processedPaths);
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error scanning applications: {ex.Message}", ex);
            }

            return applications.OrderBy(a => a.Name).ToList();
        });
    }

    private void ScanDirectory(string directory, List<ApplicationInfo> applications, HashSet<string> processedPaths)
    {
        try
        {
            // Scan .lnk files
            var lnkFiles = Directory.GetFiles(directory, "*.lnk", SearchOption.AllDirectories);
            foreach (var lnkFile in lnkFiles)
            {
                try
                {
                    var shellLink = new ShellLink(lnkFile);
                    var targetPath = shellLink.TargetPath;
                    
                    if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                        continue;

                    // Skip if already processed
                    if (processedPaths.Contains(targetPath))
                        continue;

                    // Only include executable files
                    var extension = Path.GetExtension(targetPath).ToLowerInvariant();
                    if (extension != ".exe" && extension != ".bat" && extension != ".cmd")
                        continue;

                    var name = Path.GetFileNameWithoutExtension(lnkFile);
                    if (string.IsNullOrWhiteSpace(name))
                        name = Path.GetFileNameWithoutExtension(targetPath);

                    applications.Add(new ApplicationInfo
                    {
                        Name = name,
                        ExecutablePath = targetPath,
                        IconPath = targetPath
                    });

                    processedPaths.Add(targetPath);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error processing shortcut {lnkFile}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error scanning directory {directory}: {ex.Message}", ex);
        }
    }

    public ImageSource? GetApplicationIcon(string executablePath)
    {
        try
        {
            return ImageSourceExtensions.ApplicationIcon(executablePath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error getting application icon: {ex.Message}", ex);
            return null;
        }
    }

    public async Task<bool> LaunchApplicationAsync(string executablePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(executablePath))
                    return false;

                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error launching application: {ex.Message}", ex);
                return false;
            }
        });
    }

    public bool IsApplicationRunning(string executablePath)
    {
        try
        {
            var processName = Path.GetFileNameWithoutExtension(executablePath);
            var processes = Process.GetProcessesByName(processName);
            
            foreach (var process in processes)
            {
                try
                {
                    if (string.Equals(process.MainModule?.FileName, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore access denied errors
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error checking if application is running: {ex.Message}", ex);
            return false;
        }
    }

    public List<int> GetRunningProcessIds(string executablePath)
    {
        var processIds = new List<int>();
        
        try
        {
            var processName = Path.GetFileNameWithoutExtension(executablePath);
            var processes = Process.GetProcessesByName(processName);
            
            foreach (var process in processes)
            {
                try
                {
                    if (string.Equals(process.MainModule?.FileName, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        processIds.Add(process.Id);
                    }
                }
                catch
                {
                    // Ignore access denied errors
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error getting running process IDs: {ex.Message}", ex);
        }

        return processIds;
    }
}

/// <summary>
/// Simple shell link parser for .lnk files using Windows Script Host
/// </summary>
internal class ShellLink
{
    public string TargetPath { get; }

    public ShellLink(string lnkPath)
    {
        TargetPath = string.Empty;
        
        try
        {
            // Use Windows Script Host COM object to read .lnk files
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                object? shell = Activator.CreateInstance(shellType);
                if (shell != null)
                {
                    object? shortcut = shellType.InvokeMember("CreateShortcut", 
                        System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                    if (shortcut != null)
                    {
                        Type shortcutType = shortcut.GetType();
                        object? targetPath = shortcutType.InvokeMember("TargetPath",
                            System.Reflection.BindingFlags.GetProperty, null, shortcut, null);
                        if (targetPath != null)
                        {
                            TargetPath = targetPath.ToString() ?? string.Empty;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error reading shortcut {lnkPath}: {ex.Message}", ex);
        }
    }
}

