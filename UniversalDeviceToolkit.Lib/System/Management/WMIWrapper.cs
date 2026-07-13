using System;
using System.Management;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.System.Management;

/// <summary>
/// Concrete implementation of IWMIWrapper that delegates to the static WMI class.
/// </summary>
public class WMIWrapper : IWMIWrapper
{
    private bool _disposed = false;

    public async Task<T?> QueryAsync<T>(string query) where T : new()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WMIWrapper));

        try
        {
            // Use the existing WMI infrastructure
            // This is a simplified implementation - in practice, you'd parse the query
            // and call the appropriate WMI methods
            var scope = "root\\cimv2";
            using var mos = new ManagementObjectSearcher(scope, query);
            var managementObjects = await mos.GetAsyncWithTimeout().ConfigureAwait(false);

            foreach (var mo in managementObjects)
            {
                // Convert the first result to type T
                // This is a placeholder - real implementation would need proper conversion
                if (mo is ManagementObject managementObject)
                {
                    using (managementObject)
                    {
                    var result = ConvertManagementObject<T>(managementObject);
                    return result;
                    }
                }
            }

            return default;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI query failed: {query}", ex);

            return default;
        }
    }

    public IDisposable Subscribe(string query, Action<object> callback)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WMIWrapper));

        try
        {
            var scope = "root\\cimv2";
            var watcher = new ManagementEventWatcher(scope, query);

            watcher.EventArrived += (_, e) =>
            {
                try
                {
                    callback(e.NewEvent);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"WMI event callback failed", ex);
                }
            };

            watcher.StartWithTimeout();

            return new LambdaDisposable(() =>
            {
                try
                {
                    watcher.Stop();
                }
                catch (ManagementException ex)
                {
                    Log.Instance.TraceOnce("wmi-wrapper-watcher-stop", "WMIWrapper event watcher Stop failed during dispose.", ex);
                }
                finally
                {
                    watcher.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI subscribe failed: {query}", ex);

            throw;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WMIWrapper));

        try
        {
            // Try to create a simple WMI query to test availability
            var scope = "root\\cimv2";
            using var mos = new ManagementObjectSearcher(scope, "SELECT * FROM Win32_OperatingSystem");
            var results = await mos.GetAsyncWithTimeout().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("WMI availability check failed", ex);
            return false;
        }
    }

    private static T ConvertManagementObject<T>(ManagementObject mo) where T : new()
    {
        // This is a simplified conversion - a real implementation would need to
        // map ManagementObject properties to the target type's properties
        var result = new T();
        var type = typeof(T);
        var properties = type.GetProperties();

        foreach (var prop in properties)
        {
            try
            {
                if (mo.Properties[prop.Name] is { } managementProp && managementProp.Value != null)
                {
                    var value = Convert.ChangeType(managementProp.Value, prop.PropertyType);
                    prop.SetValue(result, value);
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to convert WMI property '{prop.Name}' for {typeof(T).Name}", ex);
            }
        }

        return result;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
