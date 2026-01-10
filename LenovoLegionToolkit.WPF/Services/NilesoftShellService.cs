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
        var shellExePath = NilesoftShellHelper.GetNilesoftShellExePath();
        if (string.IsNullOrWhiteSpace(shellExePath) || !File.Exists(shellExePath))
            throw new FileNotFoundException("shell.exe not found. Cannot install shell.");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shellExePath,
                Arguments = "-register -treat -restart",
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
        // Then actively query installation status to update registry cache
        // Retry up to 5 times with increasing delays to ensure registration is complete
        const int maxRetries = 5;
        const int initialDelayMs = 2000; // Start with 2 seconds
        
        for (int retry = 0; retry < maxRetries; retry++)
        {
            // Wait with increasing delay (2s, 3s, 4s, 5s, 6s)
            int delayMs = initialDelayMs + (retry * 1000);
            Thread.Sleep(delayMs);
            
            // Actively query installation status to update the registry cache
            // This ensures that shell.exe writes the correct status to registry
            try
            {
                using var statusProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shellExePath,
                        Arguments = "-isinstalled",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                
                statusProcess.Start();
                statusProcess.WaitForExit();
                
                // Read output to ensure process completes and registry is updated
                statusProcess.StandardOutput.ReadToEnd();
                statusProcess.StandardError.ReadToEnd();
                
                // Exit code 0 means installed, 1 means not installed
                // If status check confirms installation, we're done
                if (statusProcess.ExitCode == 0)
                {
                    // Installation confirmed, break out of retry loop
                    break;
                }
            }
            catch
            {
                // Ignore errors when querying status - continue retrying
            }
        }
        
        // Clear cache again after querying status to force fresh check
        NilesoftShellHelper.ClearInstallationStatusCache();
    }

    public static void Uninstall()
    {
        var shellExePath = NilesoftShellHelper.GetNilesoftShellExePath();
        if (string.IsNullOrWhiteSpace(shellExePath) || !File.Exists(shellExePath))
            throw new FileNotFoundException("shell.exe not found. Cannot uninstall shell.");

        // First, unregister if currently registered
        if (NilesoftShellHelper.IsInstalledUsingShellExe())
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shellExePath,
                    Arguments = "-unregister -treat -restart",
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

        // Then delete the shell.exe and related files
        var shellDir = Path.GetDirectoryName(shellExePath);
        if (!string.IsNullOrWhiteSpace(shellDir))
        {
            try
            {
                var shellDll = Path.Combine(shellDir, "shell.dll");
                var shellNss = Path.Combine(shellDir, "shell.nss");

                if (File.Exists(shellExePath))
                    File.Delete(shellExePath);
                if (File.Exists(shellDll))
                    File.Delete(shellDll);
                if (File.Exists(shellNss))
                    File.Delete(shellNss);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete shell files: {ex.Message}", ex);
            }
        }

        // Clear registry installation status
        NilesoftShellHelper.ClearRegistryInstallationStatus();
        // Clear cache to ensure fresh check next time
        NilesoftShellHelper.ClearInstallationStatusCache();
    }

}

