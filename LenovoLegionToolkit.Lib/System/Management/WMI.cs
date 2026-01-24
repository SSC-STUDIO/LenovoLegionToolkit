using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.System.Management;

public static partial class WMI
{
    internal static async Task<bool> ExistsAsync(string scope, FormattableString query)
    {
        try
        {
            var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
            var mos = new ManagementObjectSearcher(scope, queryFormatted);
            var managementObjects = await mos.GetAsync().ConfigureAwait(false);
            return managementObjects.Any();
        }
        catch
        {
            return false;
        }
    }

    private static LambdaDisposable Listen(string scope, FormattableString query, Action<PropertyDataCollection> handler)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        var watcher = new ManagementEventWatcher(scope, queryFormatted);
        watcher.EventArrived += (_, e) => handler(e.NewEvent.Properties);
        
        try
        {
            watcher.Start();
        }
        catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidClass || ex.ErrorCode == ManagementStatus.InvalidNamespace)
        {
            watcher.Dispose();
            throw new ManagementException($"WMI class or namespace not available [scope={scope}, query={queryFormatted}]", ex);
        }

        return new LambdaDisposable(() =>
        {
            try
            {
                watcher.Stop();
            }
            catch (ManagementException)
            {
                // Ignore exceptions during cleanup
            }
            finally
            {
                watcher.Dispose();
            }
        });
    }

    internal static async Task<IEnumerable<T>> ReadAsync<T>(string scope, FormattableString query, Func<PropertyDataCollection, T> converter)
    {
        try
        {
            var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
            var mos = new ManagementObjectSearcher(scope, queryFormatted);
            var managementObjects = await mos.GetAsync().ConfigureAwait(false);
            var result = managementObjects.Select(mo => mo.Properties).Select(converter);
            return result;
        }
        catch (ManagementException ex)
        {
            throw new ManagementException($"Read failed: {ex.Message} [scope={scope}, query={query}]", ex);
        }
    }

    internal static async Task CallAsync(string scope, FormattableString query, string methodName, Dictionary<string, object> methodParams)
    {
        try
        {
            await CallInternalAsync(scope, query, methodName, methodParams).ConfigureAwait(false);
        }
        catch (ManagementException ex)
        {
            throw new ManagementException($"Call failed: {ex.Message} [scope={scope}, query={query}, methodName={methodName}]", ex);
        }
    }

    internal static async Task<T> CallAsync<T>(string scope, FormattableString query, string methodName, Dictionary<string, object> methodParams, Func<PropertyDataCollection, T> converter)
    {
        try
        {
            var resultProperties = await CallInternalAsync(scope, query, methodName, methodParams).ConfigureAwait(false);
            var result = converter(resultProperties.Properties);
            return result;
        }
        catch (ManagementException ex)
        {
            throw new ManagementException($"Call failed: {ex.Message}. [scope={scope}, query={query}, methodName={methodName}]", ex);
        }
    }

    private static async Task<ManagementBaseObject> CallInternalAsync(string scope, FormattableString query, string methodName, Dictionary<string, object> methodParams)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        var mos = new ManagementObjectSearcher(scope, queryFormatted);
        var managementObjects = await mos.GetAsync().ConfigureAwait(false);
        var managementObject = managementObjects.FirstOrDefault() ?? throw new InvalidOperationException("No results in query");

        var mo = (ManagementObject)managementObject;
        var methodParamsObject = mo.GetMethodParameters(methodName);
        foreach (var pair in methodParams)
            methodParamsObject[pair.Key] = pair.Value;

        return mo.InvokeMethod(methodName, methodParamsObject, new InvokeMethodOptions());
    }

    internal class WMIPropertyValueFormatter : IFormatProvider, ICustomFormatter
    {
        public static readonly WMIPropertyValueFormatter Instance = new();

        private WMIPropertyValueFormatter() { }

        public object GetFormat(Type? formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;

            throw new InvalidOperationException("Invalid type of formatted");
        }

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            var stringArg = arg?.ToString()?.Replace("\\", "\\\\");
            return stringArg ?? string.Empty;
        }
    }
}
