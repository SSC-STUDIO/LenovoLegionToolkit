using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using Microsoft.Win32;
using Win32Registry = Microsoft.Win32.Registry;

namespace LenovoLegionToolkit.WPF.Services;

public static class NilesoftShellService
{
    private const string ContextMenuClsid = "{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}";
    private const string AppSignature = "\u0020@nilesoft.shell";
    private const string AppCompanyName = "Nilesoft.Shell";
    private const string AppFullName = "Nilesoft Shell";

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHChangeNotify(int wEventId, IntPtr uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const int SHCNF_FLUSH = 0x1000;
    private const int SHCNF_FLUSHNOWAIT = 0x2000;

    /// <summary>
    /// Check if Nilesoft Shell is installed (using API from Lib project)
    /// </summary>
    public static bool IsInstalled()
    {
        return NilesoftShellHelper.IsInstalled();
    }

    /// <summary>
    /// Check if Nilesoft Shell is installed and registered
    /// </summary>
    public static bool IsRegistered()
    {
        return NilesoftShellHelper.IsRegistered();
    }

    /// <summary>
    /// Set the import path for Nilesoft Shell
    /// </summary>
    public static void SetImportPath(string importPath)
    {
        NilesoftShellHelper.SetImportPath(importPath);
    }

    /// <summary>
    /// Get the import path for Nilesoft Shell
    /// </summary>
    public static string GetImportPath()
    {
        return NilesoftShellHelper.GetImportPath();
    }

    public static void Install()
    {
        // Directly register shell.dll in the registry
        var shellDllPath = NilesoftShellHelper.GetNilesoftShellDllPath();
        if (string.IsNullOrWhiteSpace(shellDllPath) || !File.Exists(shellDllPath))
            throw new FileNotFoundException("shell.dll not found. Cannot install shell. Please rebuild project to restore shell files.");

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Registering shell.dll: {shellDllPath}");

        // Register COM object in registry (similar to RegistryConfig::Register in C++)
        if (!RegisterContextMenuHandler(shellDllPath))
        {
            throw new InvalidOperationException("Failed to register shell.dll in registry");
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Shell.dll registered successfully");

        // Clear cache after successful installation to ensure fresh check next time
        NilesoftShellHelper.ClearInstallationStatusCache();
        
        // Wait for registration to complete
        // Then actively check installation status using registry check
        // Retry up to 5 times with increasing delays to ensure registration is complete
        const int maxRetries = 5;
        const int initialDelayMs = 2000; // Start with 2 seconds
        
        for (int retry = 0; retry < maxRetries; retry++)
        {
            // Wait with increasing delay (2s, 3s, 4s, 5s, 6s)
            int delayMs = initialDelayMs + (retry * 1000);
            Thread.Sleep(delayMs);
            
            // Check installation status using registry check
            // This directly checks if the CLSID keys are registered
            try
            {
                if (NilesoftShellHelper.IsRegistered())
                {
                    // Installation confirmed, break out of retry loop
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Shell installation confirmed after {retry + 1} retry(s)");
                    break;
                }
            }
            catch
            {
                // Ignore errors when checking status - continue retrying
            }
        }
        
        // Clear cache again after checking status to force fresh check
        NilesoftShellHelper.ClearInstallationStatusCache();
        
        // Restart Windows Explorer to apply registry changes
        RestartExplorer();
        
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Shell installation process completed");
    }

    /// <summary>
    /// Restart Windows Explorer to apply registry changes
    /// </summary>
    private static void RestartExplorer()
    {
        try
        {
            // Method 1: Terminate explorer.exe process and restart it
            var processes = Process.GetProcessesByName("explorer");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Terminated explorer.exe process (PID: {process.Id})");
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to terminate explorer.exe process (PID: {process.Id}): {ex.Message}");
                }
            }

            // Wait a moment for the process to fully terminate
            Thread.Sleep(1000);

            // Start explorer.exe again
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true,
                CreateNoWindow = true
            });

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Windows Explorer restarted successfully");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to restart Windows Explorer: {ex.Message}");
        }
    }

    /// <summary>
    /// Notify shell of registry changes
    /// </summary>
    private static void NotifyShellChange()
    {
        try
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Notified shell of registry changes");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to notify shell of changes: {ex.Message}");
        }
    }

    /// <summary>
    /// Register shell.dll as a COM context menu handler
    /// This replicates RegistryConfig::Register functionality from C++
    /// </summary>
    private static bool RegisterContextMenuHandler(string dllPath)
    {
        try
        {
            // Step 1: Register COM object for context menu handler
            // HKCR\CLSID\{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}
            using var clsidKey = Win32Registry.ClassesRoot.CreateSubKey($@"CLSID\{ContextMenuClsid}");
            if (clsidKey == null)
                return false;
            
            clsidKey.SetValue(null, AppFullName);
            
            // HKCR\CLSID\{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}\InprocServer32
            using var inprocKey = clsidKey.CreateSubKey("InprocServer32");
            if (inprocKey == null)
                return false;
            
            inprocKey.SetValue(null, dllPath);
            inprocKey.SetValue("ThreadingModel", "Apartment");

            // Step 2: Add to approved shell extensions
            // HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved
            using var approvedKey = Win32Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved");
            if (approvedKey != null)
            {
                approvedKey.SetValue(ContextMenuClsid, AppSignature);
            }

            // Step 3: Register context menu handler for various file types
            // This replicates RegisterContextMenuHandler(true) from C++
            string[] fileTypes = { "*", "Directory", "Drive", "Folder", "Directory\\Background", "DesktopBackground", "LibraryFolder", "LibraryFolder\\Background" };
            
            foreach (var fileType in fileTypes)
            {
                try
                {
                    // HKCR\<FileType>\shellex\ContextMenuHandlers\  @nilesoft.shell
                    using var contextMenuKey = Win32Registry.ClassesRoot.CreateSubKey($@"{fileType}\shellex\ContextMenuHandlers\{AppSignature}");
                    if (contextMenuKey != null)
                    {
                        contextMenuKey.SetValue(null, ContextMenuClsid);
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to register context menu handler for {fileType}: {ex.Message}");
                }
            }

            // Step 4: Register .nss file association
            using var nssKey = Win32Registry.ClassesRoot.CreateSubKey(".nss");
            if (nssKey != null)
            {
                nssKey.SetValue("Content Type", "text/plain");
                using var cmdKey = nssKey.CreateSubKey(@"shell\open\command");
                if (cmdKey != null)
                {
                    cmdKey.SetValue(null, "notepad \"%1\"");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to register context menu handler: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Unregister shell.dll from the registry
    /// This replicates the RegistryConfig::Unregister functionality from C++
    /// </summary>
    private static bool UnregisterContextMenuHandler()
    {
        try
        {
            int deletedCount = 0;

            // Step 1: Delete CLSID registration
            // HKCR\CLSID\{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}
            try
            {
                Win32Registry.ClassesRoot.DeleteSubKeyTree($@"CLSID\{ContextMenuClsid}");
                deletedCount++;
            }
            catch
            {
                // Key might not exist
            }

            // Step 2: Remove from approved shell extensions
            try
            {
                using var approvedKey = Win32Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", true);
                if (approvedKey != null)
                {
                    approvedKey.DeleteValue(ContextMenuClsid);
                    deletedCount++;
                }
            }
            catch
            {
                // Key might not exist
            }

            // Step 3: Unregister context menu handler for various file types
            string[] fileTypes = { "*", "Directory", "Drive", "Folder", "Directory\\Background", "DesktopBackground", "LibraryFolder", "LibraryFolder\\Background" };
            
            foreach (var fileType in fileTypes)
            {
                try
                {
                    // HKCR\<FileType>\shellex\ContextMenuHandlers\  @nilesoft.shell
                    Win32Registry.ClassesRoot.DeleteSubKey($@"{fileType}\shellex\ContextMenuHandlers\{AppSignature}");
                    deletedCount++;
                }
                catch
                {
                    // Key might not exist
                }
            }

            // Step 4: Delete .nss file association
            try
            {
                Win32Registry.ClassesRoot.DeleteSubKeyTree(".nss");
                deletedCount++;
            }
            catch
            {
                // Key might not exist
            }

            return deletedCount > 0;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to unregister context menu handler: {ex.Message}", ex);
            return false;
        }
    }

    public static void Uninstall()
    {
        var shellDllPath = NilesoftShellHelper.GetNilesoftShellDllPath();
        if (string.IsNullOrWhiteSpace(shellDllPath) || !File.Exists(shellDllPath))
        {
            // If shell.dll is not found, just clear the cache
            // This handles the case where files were already deleted
            NilesoftShellHelper.ClearInstallationStatusCache();
            return;
        }

        // Only unregister if currently registered
        // Do NOT delete files - they should remain for potential reinstallation
        if (NilesoftShellHelper.IsRegistered())
        {
            // Unregister from registry
            if (!UnregisterContextMenuHandler())
            {
                throw new InvalidOperationException("Failed to unregister shell.dll from registry");
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shell.dll unregistered successfully");
        }

        // Clear cache to ensure fresh check next time
        NilesoftShellHelper.ClearInstallationStatusCache();
        
        // Restart Windows Explorer to apply registry changes
        RestartExplorer();
        
        // Note: We intentionally do NOT delete shell.exe, shell.dll, or shell.nss files
        // The files should remain in the application directory so they can be used for reinstallation
        // If files need to be removed, it should be done explicitly by the user or during application uninstallation
    }

}

