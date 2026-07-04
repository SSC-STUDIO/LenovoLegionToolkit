using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Resources;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.System.Management;

public static partial class WMI
{
    private static bool IsAccessDenied(ManagementException ex) =>
        ex.ErrorCode == ManagementStatus.AccessDenied
        || ex.Message.Contains("Access denied", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("拒绝访问", StringComparison.OrdinalIgnoreCase);

    private static bool IsInvalidObject(ManagementException ex) =>
        ex.ErrorCode == ManagementStatus.InvalidObject
        || ex.Message.Contains("Invalid object", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("无效的对象", StringComparison.OrdinalIgnoreCase);

    internal static async Task<bool> ExistsAsync(string scope, FormattableString query)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        try
        {
            using var mos = new ManagementObjectSearcher(scope, queryFormatted);
            var managementObjects = await mos.GetAsync().ConfigureAwait(false);
            return managementObjects.Any();
        }
        catch (ManagementException ex) when (IsAccessDenied(ex))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI exists probe denied. [scope={scope}, query={queryFormatted}]", ex);

            return false;
        }
        catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidClass || ex.ErrorCode == ManagementStatus.InvalidNamespace)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI exists probe unavailable. [scope={scope}, query={queryFormatted}]", ex);

            return false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI exists probe failed or timed out. [scope={scope}, query={queryFormatted}]", ex);

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
            throw ExceptionHelper.WmiClassNotAvailable(scope, queryFormatted, ex);
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
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        try
        {
            using var mos = new ManagementObjectSearcher(scope, queryFormatted);
            var managementObjects = await mos.GetAsync().ConfigureAwait(false);
            var result = managementObjects.Select(mo => mo.Properties).Select(converter).ToArray();
            return result;
        }
        catch (ManagementException ex)
        {
            throw ExceptionHelper.WmiReadFailed(ex.Message, scope, query, ex);
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
            throw ExceptionHelper.WmiCallFailed(ex.Message, scope, query, methodName, ex);
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
            throw ExceptionHelper.WmiCallFailedDot(ex.Message, scope, query, methodName, ex);
        }
    }

    private static async Task<ManagementBaseObject> CallInternalAsync(string scope, FormattableString query, string methodName, Dictionary<string, object> methodParams)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
            using var mos = new ManagementObjectSearcher(scope, queryFormatted);
            var managementObjects = await mos.GetAsync().ConfigureAwait(false);
            var managementObject = managementObjects.Cast<ManagementObject>().FirstOrDefault() ?? throw ExceptionHelper.WmiNoResults();

                var mo = (ManagementObject)managementObject;
                var methodParamsObject = mo.GetMethodParameters(methodName);
                foreach (var pair in methodParams)
                    methodParamsObject[pair.Key] = pair.Value;

                var invokeTask = Task.Run(() => mo.InvokeMethod(methodName, methodParamsObject, new InvokeMethodOptions()));
                using var cts = new CancellationTokenSource();
                var completedInvoke = await Task.WhenAny(invokeTask, Task.Delay(10000, cts.Token)).ConfigureAwait(false);
                if (completedInvoke == invokeTask)
                {
                    cts.Cancel();
                    return await invokeTask.ConfigureAwait(false);
                }

                throw new TimeoutException($"WMI method {methodName} invocation timed out after 3000ms.");
            }
            catch (ManagementException ex) when (attempt < 2 && IsInvalidObject(ex))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"WMI call hit an invalid object, retrying. [scope={scope}, query={queryFormatted}, methodName={methodName}, attempt={attempt}]", ex);
            }
            catch (ManagementException ex) when (IsInvalidObject(ex))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"WMI call unavailable (invalid object). [scope={scope}, query={queryFormatted}, methodName={methodName}]", ex);

                throw ExceptionHelper.WmiCallFailedFormatted(ex.Message, scope, queryFormatted, methodName, ex);
            }
        }
    }

    internal class WMIPropertyValueFormatter : IFormatProvider, ICustomFormatter
    {
        public static readonly WMIPropertyValueFormatter Instance = new();

        private WMIPropertyValueFormatter() { }

        public object GetFormat(Type? formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;

            throw ExceptionHelper.InvalidTypeOfFormatted();
        }

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            var stringArg = arg?.ToString()?.Replace("\\", "\\\\");
            return stringArg ?? string.Empty;
        }
    }
}
