using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;

namespace LenovoLegionToolkit.WPF.Services;

public static class NilesoftShellService
{
    /// <summary>
    /// 检查 Nilesoft Shell 是否已安装（使用 Lib 项目中的 API）
    /// </summary>
    public static bool IsInstalled()
    {
        return NilesoftShellHelper.IsInstalled();
    }

    /// <summary>
    /// 检查 Nilesoft Shell 是否已安装并注册（使用 shell.exe 的 API）
    /// </summary>
    public static bool IsRegistered()
    {
        return NilesoftShellHelper.IsInstalledUsingShellExe();
    }

    public static void Install()
    {
        // Directly use shell files from shellIntegration directory without copying
        // GetNilesoftShellExePath() will find files from build\shellIntegration first
        var shellExePath = NilesoftShellHelper.GetNilesoftShellExePath();
        if (string.IsNullOrWhiteSpace(shellExePath) || !File.Exists(shellExePath))
            throw new FileNotFoundException("shell.exe not found. Cannot install shell. Please rebuild the project to restore shell files.");

        // Get the directory where shell.exe is located (should be build\shellIntegration)
        // This is important so shell.exe can find imports folder and shell.nss in the same directory
        var shellExeDir = Path.GetDirectoryName(shellExePath);
        if (string.IsNullOrWhiteSpace(shellExeDir))
            throw new InvalidOperationException("Cannot determine shell.exe directory.");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shellExePath,
                Arguments = "-register -treat -restart",
                WorkingDirectory = shellExeDir, // Set working directory to shell.exe location so it can find imports and shell.nss
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Shell installation failed with exit code {process.ExitCode}: {error}");
        }

        // Clear cache after successful installation to ensure fresh check next time
        NilesoftShellHelper.ClearInstallationStatusCache();
        
        // Wait for registration to complete and Explorer to restart
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
                if (NilesoftShellHelper.IsInstalledUsingShellExe())
                {
                    // Installation confirmed, break out of retry loop
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
    }

    public static void Uninstall()
    {
        var shellExePath = NilesoftShellHelper.GetNilesoftShellExePath();
        if (string.IsNullOrWhiteSpace(shellExePath) || !File.Exists(shellExePath))
        {
            // If shell.exe is not found, just clear the registry and cache
            // This handles the case where files were already deleted
            NilesoftShellHelper.ClearRegistryInstallationStatus();
            NilesoftShellHelper.ClearInstallationStatusCache();
            return;
        }

        // Only unregister if currently registered
        // Do NOT delete files - they should remain for potential reinstallation
        if (NilesoftShellHelper.IsInstalledUsingShellExe())
        {
            // Get the directory where shell.exe is located (should be build\shellIntegration)
            var shellExeDir = Path.GetDirectoryName(shellExePath);
            if (string.IsNullOrWhiteSpace(shellExeDir))
                throw new InvalidOperationException("Cannot determine shell.exe directory.");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shellExePath,
                    Arguments = "-unregister -treat -restart",
                    WorkingDirectory = shellExeDir, // Set working directory to shell.exe location so it can find imports and shell.nss
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"Shell unregistration failed with exit code {process.ExitCode}: {error}");
            }
        }

        // Clear registry installation status
        NilesoftShellHelper.ClearRegistryInstallationStatus();
        // Clear cache to ensure fresh check next time
        NilesoftShellHelper.ClearInstallationStatusCache();
        
        // Note: We intentionally do NOT delete shell.exe, shell.dll, or shell.nss files
        // The files should remain in the application directory so they can be used for reinstallation
        // If files need to be removed, it should be done explicitly by the user or during application uninstallation
    }

}

