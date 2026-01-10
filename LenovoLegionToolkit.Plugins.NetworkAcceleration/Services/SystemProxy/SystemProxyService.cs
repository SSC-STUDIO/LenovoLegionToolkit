using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.SystemProxy;

/// <summary>
/// System proxy configuration service for Windows
/// </summary>
public class SystemProxyService
{
    private const string ProxyRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private string? _originalProxyServer;
    private bool _originalProxyEnabled;
    private string? _originalProxyOverride;

    /// <summary>
    /// Configure system proxy to use the local proxy server
    /// </summary>
    public async Task<bool> SetSystemProxyAsync(string proxyAddress, ushort proxyPort)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Setting system proxy to {proxyAddress}:{proxyPort}...");

            // Save current proxy settings
            await SaveCurrentProxySettingsAsync();

            // Set new proxy settings
            var proxyServer = $"{proxyAddress}:{proxyPort}";
            using var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryKey, true);
            if (key == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to open registry key for proxy settings.");
                return false;
            }

            key.SetValue("ProxyServer", proxyServer, RegistryValueKind.String);
            key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
            key.SetValue("ProxyOverride", "<local>", RegistryValueKind.String);

            // Notify system of changes
            NotifyProxyChange();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"System proxy configured successfully.");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error setting system proxy: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Restore original system proxy settings
    /// </summary>
    public async Task<bool> RestoreSystemProxyAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Restoring system proxy settings...");

            using var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryKey, true);
            if (key == null)
            {
                return await Task.FromResult(false);
            }

            if (_originalProxyServer != null)
            {
                key.SetValue("ProxyServer", _originalProxyServer, RegistryValueKind.String);
            }
            else
            {
                try
                {
                    key.DeleteValue("ProxyServer", false);
                }
                catch
                {
                    // Ignore if value doesn't exist
                }
            }

            key.SetValue("ProxyEnable", _originalProxyEnabled ? 1 : 0, RegistryValueKind.DWord);

            if (_originalProxyOverride != null)
            {
                key.SetValue("ProxyOverride", _originalProxyOverride, RegistryValueKind.String);
            }
            else
            {
                try
                {
                    key.DeleteValue("ProxyOverride", false);
                }
                catch
                {
                    // Ignore if value doesn't exist
                }
            }

            // Notify system of changes
            NotifyProxyChange();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"System proxy settings restored.");

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error restoring system proxy: {ex.Message}", ex);
            return await Task.FromResult(false);
        }
    }
    
    /// <summary>
    /// Force disable system proxy (used when service fails or crashes)
    /// </summary>
    public async Task<bool> ForceDisableProxyAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Force disabling system proxy...");

            using var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryKey, true);
            if (key == null)
            {
                return await Task.FromResult(false);
            }

            // Disable proxy
            key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);

            // Notify system of changes
            NotifyProxyChange();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"System proxy force disabled.");

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error force disabling system proxy: {ex.Message}", ex);
            return await Task.FromResult(false);
        }
    }

    private async Task SaveCurrentProxySettingsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryKey, false);
                if (key == null)
                {
                    return;
                }

                _originalProxyServer = key.GetValue("ProxyServer") as string;
                _originalProxyEnabled = ((int?)key.GetValue("ProxyEnable") ?? 0) != 0;
                _originalProxyOverride = key.GetValue("ProxyOverride") as string;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error saving current proxy settings: {ex.Message}", ex);
            }
        });
    }

    private static unsafe void NotifyProxyChange()
    {
        const string INTERNET_SETTINGS = "Internet Settings";
        fixed (void* ptr = INTERNET_SETTINGS)
        {
            PInvoke.SendNotifyMessage(HWND.HWND_BROADCAST, PInvoke.WM_SETTINGCHANGE, 0, new IntPtr(ptr));
        }
    }
}

