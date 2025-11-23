using System;
using System.Diagnostics;
using System.IO;
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
                    Arguments = "-unregister -restart",
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
    }

}

