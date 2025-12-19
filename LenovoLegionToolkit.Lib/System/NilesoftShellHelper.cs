using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

using ToolkitRegistry = LenovoLegionToolkit.Lib.System.Registry;

namespace LenovoLegionToolkit.Lib.System;

public static class NilesoftShellHelper
{
    private const string NilesoftShellContextMenuClsid = "{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}";
    
    // 缓存安装状态检查结果，避免频繁调用 shell.exe
    private static bool? _cachedInstallationStatus;
    private static DateTime _cacheTimestamp = DateTime.MinValue;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(2); // 缓存2秒，减少缓存时间以避免使用过期数据
    private static readonly object _cacheLock = new object();

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

    public static bool IsInstalledUsingShellExe()
    {
        // For backward compatibility, provide synchronous wrapper that calls async version
        // Use Task.Run to avoid deadlocks when called from synchronization contexts
        // This ensures the async method runs on a thread pool thread
        return Task.Run(async () => await IsInstalledUsingShellExeAsync().ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    public static async Task<bool> IsInstalledUsingShellExeAsync()
    {
        // 检查缓存
        lock (_cacheLock)
        {
            if (_cachedInstallationStatus.HasValue && 
                DateTime.UtcNow - _cacheTimestamp < CacheExpiration)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Nilesoft Shell installation status (from cache): {_cachedInstallationStatus.Value}");
                return _cachedInstallationStatus.Value;
            }
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking if Nilesoft Shell is installed using shell.exe API...");

        bool result;
        try
        {
            var shellExePath = GetNilesoftShellExePath();
            if (string.IsNullOrWhiteSpace(shellExePath) || !File.Exists(shellExePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Nilesoft Shell executable not found at path: {shellExePath ?? "null"}");
                result = false;
                
                // 缓存结果
                lock (_cacheLock)
                {
                    _cachedInstallationStatus = result;
                    _cacheTimestamp = DateTime.UtcNow;
                }
                return result;
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
                        
                        result = isInstalledFromRegistry;
                        
                        // 缓存结果
                        lock (_cacheLock)
                        {
                            _cachedInstallationStatus = result;
                            _cacheTimestamp = DateTime.UtcNow;
                        }
                        return result;
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

            // Wait for both read tasks to complete asynchronously before calling WaitForExit
            // This prevents deadlock when process output exceeds buffer capacity
            // The process can write to buffers while we're reading from them
            // Wrap in try-catch to ensure WaitForExitAsync is always called even if tasks throw exceptions
            try
            {
                await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
            }
            catch
            {
                // If either task throws an exception, we still need to wait for the process to exit
                // to prevent leaving the process handle open and unreleased
            }

            // Now safe to wait for process exit since all output has been read (or read failed)
            // This must be called regardless of whether the read tasks succeeded or failed
            await process.WaitForExitAsync().ConfigureAwait(false);

            // Get the results (already completed or may throw exception)
            string output;
            string error;
            try
            {
                output = await outputTask.ConfigureAwait(false);
            }
            catch
            {
                output = string.Empty;
            }
            try
            {
                error = await errorTask.ConfigureAwait(false);
            }
            catch
            {
                error = string.Empty;
            }

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
                        
                        // 缓存结果
                        lock (_cacheLock)
                        {
                            _cachedInstallationStatus = isInstalledFromError;
                            _cacheTimestamp = DateTime.UtcNow;
                        }
                        return isInstalledFromError;
                    }
                }
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Nilesoft Shell installation status query returned empty output");
                
                result = false;
                // 缓存结果
                lock (_cacheLock)
                {
                    _cachedInstallationStatus = result;
                    _cacheTimestamp = DateTime.UtcNow;
                }
                return result;
            }

            var outputResult = output.Trim().ToLowerInvariant();
            result = outputResult == "true";
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Nilesoft Shell installation status (from shell.exe stdout): {result}, output: {output.Trim()}");

            // 缓存结果
            lock (_cacheLock)
            {
                _cachedInstallationStatus = result;
                _cacheTimestamp = DateTime.UtcNow;
            }
            return result;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to query shell.exe installation status, falling back to file/registry check", ex);
            
            // Fallback: If we can't query shell.exe, just check if file exists
            // Note: Without shell.exe API, we can't accurately determine registration status
            result = IsInstalled();
            
            // 缓存结果（即使是fallback结果）
            lock (_cacheLock)
            {
                _cachedInstallationStatus = result;
                _cacheTimestamp = DateTime.UtcNow;
            }
            return result;
        }
    }
    
    /// <summary>
    /// 清除安装状态缓存，强制下次调用时重新检查
    /// </summary>
    public static void ClearInstallationStatusCache()
    {
        lock (_cacheLock)
        {
            _cachedInstallationStatus = null;
            _cacheTimestamp = DateTime.MinValue;
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Nilesoft Shell installation status cache cleared");
        }
    }

    /// <summary>
    /// 清除注册表中的Shell安装状态值
    /// </summary>
    public static void ClearRegistryInstallationStatus()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\LenovoLegionToolkit", true);
            if (key != null)
            {
                key.DeleteValue("ShellInstalled", false);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace((FormattableString)$"Nilesoft Shell registry installation status cleared");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"Failed to clear Nilesoft Shell registry installation status", ex);
        }
    }

    public static string? GetNilesoftShellExePath()
    {
        try
        {
            // 1) Prefer current process directory (where our app runs)
            var baseDir = AppContext.BaseDirectory;
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Searching for shell.exe, AppContext.BaseDirectory: {baseDir ?? "null"}");
            
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                // direct file in base dir
                var directCandidate = Path.Combine(baseDir, "shell.exe");
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Checking direct path: {directCandidate}, exists: {File.Exists(directCandidate)}");
                
                if (File.Exists(directCandidate))
                {
                    var dllPath = Path.Combine(baseDir, "shell.dll");
                    if (File.Exists(dllPath))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe at direct path: {directCandidate}");
                        return directCandidate;
                    }
                    else if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"shell.exe found but shell.dll missing at: {dllPath}");
                }

                // search recursively under base dir (covers ThirdParty/Shell/src/bin/**)
                try
                {
                    var files = Directory.GetFiles(baseDir, "shell.exe", SearchOption.AllDirectories);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Found {files.Length} shell.exe file(s) in recursive search");
                    
                    foreach (var file in files)
                    {
                        var dir = Path.GetDirectoryName(file);
                        if (!string.IsNullOrWhiteSpace(dir))
                        {
                            var dllPath = Path.Combine(dir, "shell.dll");
                            if (File.Exists(dllPath))
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Found shell.exe at recursive path: {file}");
                                return file;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Recursive search failed: {ex.Message}");
                }
            }

            // 2) Check build directory (common location during development)
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                // Check build/shellIntegration directory first
                var shellIntegrationDir = Path.Combine(currentDir, "build", "shellIntegration");
                if (Directory.Exists(shellIntegrationDir))
                {
                    var shellIntegrationCandidate = Path.Combine(shellIntegrationDir, "shell.exe");
                    var shellIntegrationDll = Path.Combine(shellIntegrationDir, "shell.dll");
                    if (File.Exists(shellIntegrationCandidate) && File.Exists(shellIntegrationDll))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe in build/shellIntegration directory: {shellIntegrationCandidate}");
                        return shellIntegrationCandidate;
                    }
                }
                
                // Fallback to build directory (legacy)
                var buildDir = Path.Combine(currentDir, "build");
                if (Directory.Exists(buildDir))
                {
                    var buildCandidate = Path.Combine(buildDir, "shell.exe");
                    var buildDll = Path.Combine(buildDir, "shell.dll");
                    if (File.Exists(buildCandidate) && File.Exists(buildDll))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe in build directory: {buildCandidate}");
                        return buildCandidate;
                    }
                }
                
                // Also check parent directory's build/shellIntegration folder
                var parentDir = Directory.GetParent(currentDir)?.FullName;
                if (!string.IsNullOrWhiteSpace(parentDir))
                {
                    var parentShellIntegrationDir = Path.Combine(parentDir, "build", "shellIntegration");
                    if (Directory.Exists(parentShellIntegrationDir))
                    {
                        var parentShellIntegrationCandidate = Path.Combine(parentShellIntegrationDir, "shell.exe");
                        var parentShellIntegrationDll = Path.Combine(parentShellIntegrationDir, "shell.dll");
                        if (File.Exists(parentShellIntegrationCandidate) && File.Exists(parentShellIntegrationDll))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Found shell.exe in parent build/shellIntegration directory: {parentShellIntegrationCandidate}");
                            return parentShellIntegrationCandidate;
                        }
                    }
                    
                    // Also check parent directory's build folder (legacy)
                    var parentBuildDir = Path.Combine(parentDir, "build");
                    if (Directory.Exists(parentBuildDir))
                    {
                        var parentBuildCandidate = Path.Combine(parentBuildDir, "shell.exe");
                        var parentBuildDll = Path.Combine(parentBuildDir, "shell.dll");
                        if (File.Exists(parentBuildCandidate) && File.Exists(parentBuildDll))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Found shell.exe in parent build directory: {parentBuildCandidate}");
                            return parentBuildCandidate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Build directory search failed: {ex.Message}");
            }

            // 3) Fallback to default installation path
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                var candidate = Path.Combine(programFiles, "Nilesoft Shell", "shell.exe");
                if (File.Exists(candidate))
                {
                    var dllPath = Path.Combine(programFiles, "Nilesoft Shell", "shell.dll");
                    if (File.Exists(dllPath))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe in ProgramFiles: {candidate}");
                        return candidate;
                    }
                }
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"shell.exe not found in any searched location");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GetNilesoftShellExePath failed: {ex.Message}", ex);
        }
        return null;
    }

}

