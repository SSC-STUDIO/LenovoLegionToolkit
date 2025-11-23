using System;
using System.Diagnostics;
using System.IO;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

using ToolkitRegistry = LenovoLegionToolkit.Lib.System.Registry;

namespace LenovoLegionToolkit.Lib.System;

public static class NilesoftShellHelper
{
    private const string NilesoftShellContextMenuClsid = "{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}";

    /// <summary>
    /// 检查 Nilesoft Shell 是否已安装（shell.exe 文件是否存在）
    /// </summary>
    public static bool IsInstalled()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking if Nilesoft Shell is installed...");

        var shellExePath = GetNilesoftShellExePath();
        var isInstalled = !string.IsNullOrWhiteSpace(shellExePath) && File.Exists(shellExePath);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Nilesoft Shell installation check result: {isInstalled}, path: {shellExePath ?? "null"}");

        return isInstalled;
    }

    /// <summary>
    /// 检查 Nilesoft Shell 是否已注册到系统（右键菜单是否已启用）
    /// </summary>
    public static bool IsRegistered()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking if Nilesoft Shell is registered...");

        var isRegistered = IsNilesoftShellContextMenuRegistered();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Nilesoft Shell registration check result: {isRegistered}");

        return isRegistered;
    }

    /// <summary>
    /// 检查 Nilesoft Shell 是否已安装（使用 shell.exe 的 API，更准确）
    /// 返回 true 表示文件存在且已注册
    /// </summary>
    public static bool IsInstalledUsingShellExe()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking if Nilesoft Shell is installed using shell.exe API...");

        try
        {
            var shellExePath = GetNilesoftShellExePath();
            if (string.IsNullOrWhiteSpace(shellExePath) || !File.Exists(shellExePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Nilesoft Shell executable not found at path: {shellExePath ?? "null"}");
                return false;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Querying shell.exe installation status via: {shellExePath} -isinstalled");

            // First, try to read from registry (shell.exe writes status there to avoid console output issues)
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\LenovoLegionToolkit", false);
                if (key != null)
                {
                    var value = key.GetValue("ShellInstalled");
                    if (value != null && value is int intValue)
                    {
                        var isInstalledFromRegistry = intValue != 0;
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Nilesoft Shell installation status (from registry): {isInstalledFromRegistry}");
                        return isInstalledFromRegistry;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to read from registry, falling back to stdout", ex);
            }

            // Fallback: Use shell.exe's stdout output (for backward compatibility)
            // Use shell.exe's built-in API to check installation status
            // This is more accurate as shell.exe can check its own installation state
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shellExePath,
                    Arguments = "-isinstalled",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            // Read streams asynchronously to prevent deadlock
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            // Wait for output to be read completely
            var output = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult();

            // Parse output: shell.exe outputs "true" or "false"
            if (string.IsNullOrWhiteSpace(output))
            {
                // If no output, check error output as fallback
                if (!string.IsNullOrWhiteSpace(error))
                {
                    var errorResult = error.Trim().ToLowerInvariant();
                    if (errorResult == "true" || errorResult == "false")
                    {
                        var isInstalledFromError = errorResult == "true";
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Nilesoft Shell installation status (from stderr): {isInstalledFromError}");
                        return isInstalledFromError;
                    }
                }
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Nilesoft Shell installation status query returned empty output");
                return false;
            }

            var outputResult = output.Trim().ToLowerInvariant();
            var isInstalled = outputResult == "true";
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Nilesoft Shell installation status (from shell.exe stdout): {isInstalled}, output: {output.Trim()}");

            return isInstalled;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to query shell.exe installation status, falling back to file/registry check", ex);
            
            // Fallback to checking file existence and registration status
            return IsInstalled() && IsRegistered();
        }
    }

    /// <summary>
    /// 获取 shell.exe 的路径
    /// </summary>
    public static string? GetNilesoftShellExePath()
    {
        try
        {
            // 1) Prefer current process directory (where our app runs)
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                // direct file in base dir
                var directCandidate = Path.Combine(baseDir, "shell.exe");
                if (File.Exists(directCandidate) && File.Exists(Path.Combine(baseDir, "shell.dll")))
                    return directCandidate;

                // search recursively under base dir (covers ThirdParty/Shell/src/bin/**)
                try
                {
                    var files = Directory.GetFiles(baseDir, "shell.exe", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var dir = Path.GetDirectoryName(file);
                        if (!string.IsNullOrWhiteSpace(dir))
                        {
                            var dllPath = Path.Combine(dir, "shell.dll");
                            if (File.Exists(dllPath))
                                return file;
                        }
                    }
                }
                catch { /* ignore */ }
            }

            // 2) Fallback to default installation path
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                var candidate = Path.Combine(programFiles, "Nilesoft Shell", "shell.exe");
                if (File.Exists(candidate) && File.Exists(Path.Combine(programFiles, "Nilesoft Shell", "shell.dll")))
                    return candidate;
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private static bool IsNilesoftShellContextMenuRegistered()
    {
        try
        {
            // Primary check: Check if CLSID key exists
            // This is the most reliable indicator that the context menu handler is registered
            var clsidKey = $@"CLSID\{NilesoftShellContextMenuClsid}";
            if (ToolkitRegistry.KeyExists("HKEY_CLASSES_ROOT", clsidKey))
            {
                // Also verify that InprocServer32 subkey exists (confirms it's properly registered)
                var inprocServerKey = $@"{clsidKey}\InprocServer32";
                if (ToolkitRegistry.KeyExists("HKEY_CLASSES_ROOT", inprocServerKey))
                    return true;
            }

            // Fallback check: Check if registered in ContextMenuHandlers for Directory
            // The handler name is " @nilesoft.shell" (note the leading space)
            var directoryHandlerPath = @"Directory\shellex\ContextMenuHandlers\ @nilesoft.shell";
            if (ToolkitRegistry.KeyExists("HKEY_CLASSES_ROOT", directoryHandlerPath))
            {
                var clsidValue = ToolkitRegistry.GetValue<string>(
                    "HKEY_CLASSES_ROOT",
                    directoryHandlerPath,
                    string.Empty,
                    string.Empty);
                if (!string.IsNullOrWhiteSpace(clsidValue) && 
                    clsidValue.Contains(NilesoftShellContextMenuClsid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

