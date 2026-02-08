using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.System;

[SupportedOSPlatform("windows")]
public static class NilesoftShellHelper
{
    private const string NilesoftShellContextMenuClsid = "{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}";
    
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

    public static string? GetNilesoftShellDllPath()
    {
        try
        {
            // Check registry for CLSID registration (most reliable method)
            // Check HKEY_CLASSES_ROOT\CLSID\{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}\InprocServer32
            using var clsidKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"CLSID\{NilesoftShellContextMenuClsid}\InprocServer32", false);
            if (clsidKey != null)
            {
                var dllPath = clsidKey.GetValue("") as string;
                if (!string.IsNullOrWhiteSpace(dllPath))
                {
                    dllPath = dllPath.Trim('"');
                    return Environment.ExpandEnvironmentVariables(dllPath);
                }
            }
            
            // If not found in HKCR, check HKEY_CURRENT_USER\Software\Classes\CLSID (for per-user registration)
            using var hkcuClsidKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($@"Software\Classes\CLSID\{NilesoftShellContextMenuClsid}\InprocServer32", false);
            if (hkcuClsidKey != null)
            {
                var dllPath = hkcuClsidKey.GetValue("") as string;
                if (!string.IsNullOrWhiteSpace(dllPath))
                {
                    dllPath = dllPath.Trim('"');
                    return Environment.ExpandEnvironmentVariables(dllPath);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get Nilesoft Shell DLL path: {ex.Message}", ex);
        }
        
        return null;
    }

    public static string? GetNilesoftShellExePath()
    {
        var dllPath = GetNilesoftShellDllPath();
        if (string.IsNullOrEmpty(dllPath))
            return null;

        var directory = Path.GetDirectoryName(dllPath);
        if (string.IsNullOrEmpty(directory))
            return null;

        var exePath = Path.Combine(directory, "shell.exe");
        return File.Exists(exePath) ? exePath : null;
    }

    public static bool IsInstalledUsingShellExe()
    {
        var exePath = GetNilesoftShellExePath();
        return !string.IsNullOrEmpty(exePath);
    }

    public static Task<bool> IsInstalledUsingShellExeAsync()
    {
        var exePath = GetNilesoftShellExePath();
        return Task.FromResult(!string.IsNullOrEmpty(exePath));
    }

    public static bool IsInstalledUsingShellDll()
    {
        // For backward compatibility, provide synchronous wrapper that calls async version
        // Since IsInstalledUsingShellDllAsync() now returns Task.FromResult (synchronous operation),
        // we can directly await it without Task.Run
        return IsInstalledUsingShellDllAsync().GetAwaiter().GetResult();
    }

    public static Task<bool> IsInstalledUsingShellDllAsync()
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

        var dllPath = GetNilesoftShellDllPath();
        var result = !string.IsNullOrWhiteSpace(dllPath) && File.Exists(dllPath);
        
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

    public static bool IsRegistered()
    {
        var ShellDllPath = GetNilesoftShellDllPath();
        return !string.IsNullOrEmpty(ShellDllPath) && File.Exists(ShellDllPath);
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



}

