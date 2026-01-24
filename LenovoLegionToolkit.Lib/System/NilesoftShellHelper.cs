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

        var shellDllPath = GetNilesoftShellDllPath();
        var isInstalled = !string.IsNullOrWhiteSpace(shellDllPath) && File.Exists(shellDllPath);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Nilesoft Shell installation check result: {isInstalled}, dll path: {shellDllPath ?? "null"}");

        return isInstalled;
    }

    public static bool IsInstalledUsingShellExe()
    {
        // For backward compatibility, provide synchronous wrapper that calls async version
        // Since IsInstalledUsingShellExeAsync() now returns Task.FromResult (synchronous operation),
        // we can directly await it without Task.Run
        return IsInstalledUsingShellExeAsync().GetAwaiter().GetResult();
    }

    public static Task<bool> IsInstalledUsingShellExeAsync()
    {
        // 检查缓存
        lock (_cacheLock)
        {
            if (_cachedInstallationStatus.HasValue && 
                DateTime.UtcNow - _cacheTimestamp < CacheExpiration)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Nilesoft Shell installation status (from cache): {_cachedInstallationStatus.Value}");
                return Task.FromResult(_cachedInstallationStatus.Value);
            }
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking if Nilesoft Shell is installed by checking CLSID registry...");

        bool result = false;
        try
        {
            // Check registry for CLSID registration (most reliable method)
            // Check HKEY_CLASSES_ROOT\CLSID\{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}\InprocServer32
            using var clsidKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"CLSID\{NilesoftShellContextMenuClsid}", false);
            if (clsidKey != null)
            {
                using var inprocKey = clsidKey.OpenSubKey("InprocServer32", false);
                if (inprocKey != null)
                {
                    var dllPath = inprocKey.GetValue("") as string;
                    if (!string.IsNullOrWhiteSpace(dllPath))
                    {
                        dllPath = dllPath.Trim('"');
                        dllPath = Environment.ExpandEnvironmentVariables(dllPath);
                        
                        if (File.Exists(dllPath))
                        {
                            result = true;
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Nilesoft Shell installation status (from CLSID registry): {result}, DLL path: {dllPath}");
                            
                            // 缓存结果
                            lock (_cacheLock)
                            {
                                _cachedInstallationStatus = result;
                                _cacheTimestamp = DateTime.UtcNow;
                            }
                            return Task.FromResult(result);
                        }
                        else if (Log.Instance.IsTraceEnabled)
                        {
                            Log.Instance.Trace($"CLSID registered but DLL file not found at path: {dllPath}");
                        }
                    }
                }
            }
            
            // If not found in HKCR, check HKEY_CURRENT_USER\Software\Classes\CLSID (for per-user registration)
            if (!result)
            {
                using var hkcuClsidKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($@"Software\Classes\CLSID\{NilesoftShellContextMenuClsid}", false);
                if (hkcuClsidKey != null)
                {
                    using var hkcuInprocKey = hkcuClsidKey.OpenSubKey("InprocServer32", false);
                    if (hkcuInprocKey != null)
                    {
                        var dllPath = hkcuInprocKey.GetValue("") as string;
                        if (!string.IsNullOrWhiteSpace(dllPath))
                        {
                            dllPath = dllPath.Trim('"');
                            dllPath = Environment.ExpandEnvironmentVariables(dllPath);
                            if (File.Exists(dllPath))
                            {
                                result = true;
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Nilesoft Shell installation status (from HKCU CLSID registry): {result}, DLL path: {dllPath}");
                                
                                // 缓存结果
                                lock (_cacheLock)
                                {
                                    _cachedInstallationStatus = result;
                                    _cacheTimestamp = DateTime.UtcNow;
                                }
                                return Task.FromResult(result);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to check CLSID registry: {ex.Message}", ex);
        }
        
        // 缓存结果
        lock (_cacheLock)
        {
            _cachedInstallationStatus = result;
            _cacheTimestamp = DateTime.UtcNow;
        }
        
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Nilesoft Shell installation status (final): {result}");
        
        return Task.FromResult(result);
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
            // 1) Check build/shellIntegration directory first (primary location for all shell files)
            // This is the central location where all shell files should be stored
            try
            {
                // Try current directory first
                var currentDir = Directory.GetCurrentDirectory();
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
                    }
                    
                // Also check AppContext.BaseDirectory relative to build/shellIntegration
                var appBaseDir = AppContext.BaseDirectory;
                if (!string.IsNullOrWhiteSpace(appBaseDir))
                {
                    // Check if appBaseDir is in a build subdirectory, then look for sibling build/shellIntegration
                    var appBaseParent = Directory.GetParent(appBaseDir)?.FullName;
                    if (!string.IsNullOrWhiteSpace(appBaseParent))
                    {
                        var appBaseShellIntegrationDir = Path.Combine(appBaseParent, "build", "shellIntegration");
                        if (Directory.Exists(appBaseShellIntegrationDir))
                        {
                            var appBaseShellIntegrationCandidate = Path.Combine(appBaseShellIntegrationDir, "shell.exe");
                            var appBaseShellIntegrationDll = Path.Combine(appBaseShellIntegrationDir, "shell.dll");
                            if (File.Exists(appBaseShellIntegrationCandidate) && File.Exists(appBaseShellIntegrationDll))
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Found shell.exe in AppContext relative build/shellIntegration directory: {appBaseShellIntegrationCandidate}");
                                return appBaseShellIntegrationCandidate;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Build/shellIntegration directory search failed: {ex.Message}");
            }

            // 2) Check current process directory (where our app runs) - fallback only
            var appDir = AppContext.BaseDirectory;
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Searching for shell.exe, AppContext.BaseDirectory: {appDir ?? "null"}");
            
            if (!string.IsNullOrWhiteSpace(appDir))
            {
                // direct file in app dir
                var directCandidate = Path.Combine(appDir, "shell.exe");
                if (File.Exists(directCandidate))
                {
                    var dllPath = Path.Combine(appDir, "shell.dll");
                    if (File.Exists(dllPath))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe at app directory path: {directCandidate}");
                        return directCandidate;
                    }
                }
            }

            // 3) Check ShellIntegration source build directories (development fallback)
            try
            {
                // Try to find the repository root by looking for ShellIntegration directory
                var currentDir = Directory.GetCurrentDirectory();
                var repoRoot = currentDir;
                
                // Find ShellIntegration directory
                var shellIntegrationPath = Path.Combine(currentDir, "ShellIntegration");
                if (!Directory.Exists(shellIntegrationPath))
                {
                    // Try parent directory
                    var parentDir = Directory.GetParent(currentDir)?.FullName;
                    if (!string.IsNullOrWhiteSpace(parentDir))
                    {
                        shellIntegrationPath = Path.Combine(parentDir, "ShellIntegration");
                        repoRoot = parentDir;
                    }
                }
                
                if (Directory.Exists(shellIntegrationPath))
                {
                    // Check src/bin (expected build output location)
                    var srcBinDir = Path.Combine(shellIntegrationPath, "src", "bin");
                    if (Directory.Exists(srcBinDir))
                    {
                        var srcBinExe = Path.Combine(srcBinDir, "shell.exe");
                        var srcBinDll = Path.Combine(srcBinDir, "shell.dll");
                        if (File.Exists(srcBinExe) && File.Exists(srcBinDll))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Found shell.exe in ShellIntegration/src/bin: {srcBinExe}");
                            return srcBinExe;
                        }
                    }
                    
                    // Check src/exe/bin (alternative build output location)
                    var exeBinDir = Path.Combine(shellIntegrationPath, "src", "exe", "bin");
                    var srcBinDllCheck = Path.Combine(shellIntegrationPath, "src", "bin");
                    if (Directory.Exists(exeBinDir))
                    {
                        var exeBinExe = Path.Combine(exeBinDir, "shell.exe");
                        var dllPath = Path.Combine(srcBinDllCheck, "shell.dll");
                        if (File.Exists(exeBinExe) && File.Exists(dllPath))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Found shell.exe in ShellIntegration/src/exe/bin: {exeBinExe}");
                            return exeBinExe;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ShellIntegration source directory search failed: {ex.Message}");
            }

            // 4) Check build directory (legacy fallback)
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                var buildDir = Path.Combine(currentDir, "build");
                if (Directory.Exists(buildDir))
                {
                    var buildCandidate = Path.Combine(buildDir, "shell.exe");
                    var buildDll = Path.Combine(buildDir, "shell.dll");
                    if (File.Exists(buildCandidate) && File.Exists(buildDll))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe in build directory (legacy): {buildCandidate}");
                        return buildCandidate;
                    }
                }
                
                var parentDir = Directory.GetParent(currentDir)?.FullName;
                if (!string.IsNullOrWhiteSpace(parentDir))
                {
                    var parentBuildDir = Path.Combine(parentDir, "build");
                    if (Directory.Exists(parentBuildDir))
                    {
                        var parentBuildCandidate = Path.Combine(parentBuildDir, "shell.exe");
                        var parentBuildDll = Path.Combine(parentBuildDir, "shell.dll");
                        if (File.Exists(parentBuildCandidate) && File.Exists(parentBuildDll))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Found shell.exe in parent build directory (legacy): {parentBuildCandidate}");
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

            // 5) Fallback to default installation path
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

    public static bool IsRegistered()
    {
        var shellExePath = GetNilesoftShellExePath();
        return !string.IsNullOrEmpty(shellExePath) && File.Exists(shellExePath);
    }

    public static void SetImportPath(string path)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\LenovoLegionToolkit", true);
            if (key != null)
            {
                key.SetValue("ShellImportPath", path, Microsoft.Win32.RegistryValueKind.String);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Set Nilesoft Shell import path: {path}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to set Nilesoft Shell import path: {ex.Message}", ex);
        }
    }

    public static string? GetImportPath()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\LenovoLegionToolkit", false);
            if (key != null)
            {
                var path = key.GetValue("ShellImportPath") as string;
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Got Nilesoft Shell import path: {path ?? "null"}");
                return path;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get Nilesoft Shell import path: {ex.Message}", ex);
        }
        return null;
    }

    public static string? GetNilesoftShellDllPath()
    {
        var shellExePath = GetNilesoftShellExePath();
        if (string.IsNullOrEmpty(shellExePath))
            return null;

        var shellDir = Path.GetDirectoryName(shellExePath);
        if (string.IsNullOrEmpty(shellDir))
            return null;

        return Path.Combine(shellDir, "shell.dll");
    }

}

