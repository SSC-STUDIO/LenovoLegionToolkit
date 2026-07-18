using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.System.Management;

public static partial class WMI
{
    // WMI method invocations (ManagementObject.InvokeMethod) must honor the 2,500ms ceiling
    // enforced by KNOWNLEDGE_BASE.md "WMI Timeout Protection" rule (#2). Never raise this
    // above 3,000ms or the caller may stall well past the async contract.
    // Keep well under AbstractSensorsController's 2s snapshot budget (CPU+GPU fan in parallel).
    private const int _wmiInvokeTimeoutMs = 800;

    // Soft-failed method signatures for this process — avoids re-invoking known-missing firmware methods
    // (which would otherwise spam first-chance ManagementException during capability probes).
    private static readonly ConcurrentDictionary<string, byte> _softFailedMethodKeys = new(StringComparer.Ordinal);

    private static bool IsAccessDenied(ManagementException ex) =>
        ex.ErrorCode == ManagementStatus.AccessDenied
        || ex.Message.Contains("Access denied", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("拒绝访问", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for WMI failures that capability probing should treat as "feature unavailable"
    /// rather than a hard crash. Access denied is excluded so elevation issues still surface.
    /// </summary>
    private static bool IsSoftWmiFailure(ManagementException ex) =>
        !IsAccessDenied(ex);

    /// <summary>
    /// Only permanent method-missing cases. Do NOT match generic "does not exist" / NotFound —
    /// those fire for bad object state / access and wrongly disabled Fan_GetCurrentFanSpeed
    /// process-wide (sticky 0 RPM with no LHM recovery).
    /// </summary>
    private static bool IsMethodMissing(ManagementException ex) =>
        ex.ErrorCode is ManagementStatus.InvalidMethod
        || ex.Message.Contains("not implemented", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("未实现", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("未在任何类中实现", StringComparison.OrdinalIgnoreCase);

    private static string SoftFailKey(string scope, string queryFormatted, string methodName) =>
        string.Concat(scope, "\u001f", queryFormatted, "\u001f", methodName);

    internal static bool IsWmiMethodSoftFailed(string scope, string queryFormatted, string methodName) =>
        _softFailedMethodKeys.ContainsKey(SoftFailKey(scope, queryFormatted, methodName));

    private static void MarkWmiMethodSoftFailed(string scope, string queryFormatted, string methodName) =>
        _softFailedMethodKeys.TryAdd(SoftFailKey(scope, queryFormatted, methodName), 0);

    /// <summary>
    /// Try GetMethodParameters without rethrowing soft failures (no InvalidOperationException).
    /// Permanent soft-fail cache is only for true method-missing — never for per-ID invoke fails.
    /// </summary>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    private static bool TryGetWmiMethodParameters(
        ManagementObject target,
        string methodName,
        string scope,
        string queryFormatted,
        out ManagementBaseObject? args)
    {
        try
        {
            args = target.GetMethodParameters(methodName);
            return true;
        }
        catch (ManagementException ex) when (IsSoftWmiFailure(ex))
        {
            // Only cache true method-missing; not every soft ManagementException.
            if (IsMethodMissing(ex))
                MarkWmiMethodSoftFailed(scope, queryFormatted, methodName);
            args = null;
            return false;
        }
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    private static bool TryInvokeWmiMethod(
        ManagementObject target,
        string methodName,
        ManagementBaseObject args,
        string scope,
        string queryFormatted,
        out ManagementBaseObject? result)
    {
        try
        {
            result = target.InvokeMethod(methodName, args, new InvokeMethodOptions());
            return result is not null;
        }
        catch (ManagementException ex) when (IsSoftWmiFailure(ex))
        {
            // Soft-fail including Invalid object / invalid object — never rethrow.
            // Lenovo providers often invalidate the MO between Get() and InvokeMethod();
            // TryCallInternalAsync re-queries a fresh instance on the next attempt.
            // Rethrowing here only spammed first-chance exceptions in the debugger while
            // fan/sensor probes ran every second.
            //
            // Do NOT permanently soft-fail on Invoke failures.
            // GetFeatureValue / similar methods accept many IDs: one unsupported fan ID must
            // not poison temperature / other capabilities for the rest of the process.
            // Method-missing is still cached in TryGetWmiMethodParameters.
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace(
                    $"WMI invoke soft-failed (not cached). [method={methodName}, query={queryFormatted}, code={ex.ErrorCode}]",
                    ex);
            result = null;
            return false;
        }
    }

    internal static async Task<bool> ExistsAsync(string scope, FormattableString query)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        try
        {
            using var mos = new ManagementObjectSearcher(scope, queryFormatted);
            var managementObjects = await mos.GetAsyncWithTimeout().ConfigureAwait(false);
            return managementObjects.Any();
        }
        catch (ManagementException ex) when (IsAccessDenied(ex))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI exists probe denied. [scope={scope}, query={queryFormatted}]", ex);

            return false;
        }
        catch (ManagementException ex) when (
            ex.ErrorCode is ManagementStatus.InvalidClass
                or ManagementStatus.InvalidNamespace
                or ManagementStatus.NotSupported
                or ManagementStatus.NotFound
                or ManagementStatus.InvalidQuery
            || ex.Message.Contains("不支持", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    /// Sync WMI event subscription. Blocks the caller while starting the watcher
    /// (see <see cref="ManagementEventWatcherExtensions.StartWithTimeout"/>). Prefer
    /// <see cref="ListenAsync"/> from async / UI code paths.
    /// </summary>
    private static LambdaDisposable Listen(string scope, FormattableString query, Action<PropertyDataCollection> handler)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        var watcher = new ManagementEventWatcher(scope, queryFormatted);
        watcher.EventArrived += (_, e) => handler(e.NewEvent.Properties);

        try
        {
            // Blocks calling thread; Start itself runs on the thread pool. Prefer ListenAsync.
            watcher.StartWithTimeout();
        }
        catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidClass || ex.ErrorCode == ManagementStatus.InvalidNamespace)
        {
            watcher.Dispose();
            throw ExceptionHelper.WmiClassNotAvailable(scope, queryFormatted, ex);
        }

        return CreateWatcherDisposable(watcher);
    }

    /// <summary>
    /// Async WMI event subscription. Does not block the calling thread while starting.
    /// Preferred for <see cref="Listeners.AbstractWMIListener{TEventArgs,TValue,TRawValue}"/> and other async hosts.
    /// </summary>
    private static async Task<IDisposable> ListenAsync(string scope, FormattableString query, Action<PropertyDataCollection> handler)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        var watcher = new ManagementEventWatcher(scope, queryFormatted);
        watcher.EventArrived += (_, e) => handler(e.NewEvent.Properties);

        try
        {
            await watcher.StartAsyncWithTimeout().ConfigureAwait(false);
        }
        catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidClass || ex.ErrorCode == ManagementStatus.InvalidNamespace)
        {
            watcher.Dispose();
            throw ExceptionHelper.WmiClassNotAvailable(scope, queryFormatted, ex);
        }

        return CreateWatcherDisposable(watcher);
    }

    private static LambdaDisposable CreateWatcherDisposable(ManagementEventWatcher watcher) =>
        new(() =>
        {
            try
            {
                watcher.Stop();
            }
            catch (ManagementException ex)
            {
                Log.Instance.TraceOnce("wmi-watcher-stop", "WMI event watcher Stop failed during dispose.", ex);
            }
            finally
            {
                watcher.Dispose();
            }
        });

    internal static async Task<IEnumerable<T>> ReadAsync<T>(string scope, FormattableString query, Func<PropertyDataCollection, T> converter)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        try
        {
            using var mos = new ManagementObjectSearcher(scope, queryFormatted);
            var managementObjects = await mos.GetAsyncWithTimeout().ConfigureAwait(false);
            var result = managementObjects.Select(mo => mo.Properties).Select(converter).ToArray();
            return result;
        }
        catch (ManagementException ex)
        {
            throw ExceptionHelper.WmiReadFailed(ex.Message, scope, query, ex);
        }
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    internal static async Task CallAsync(string scope, FormattableString query, string methodName, Dictionary<string, object> methodParams)
    {
        // Soft-unavailable: do not throw (avoids debugger first-chance spam on probes).
        // Do NOT permanently soft-fail here — only true method-missing is cached inside
        // TryGetWmiMethodParameters. One bad invoke must not disable the method forever.
        _ = await TryCallInternalAsync(scope, query, methodName, methodParams).ConfigureAwait(false);
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    internal static async Task<T> CallAsync<T>(string scope, FormattableString query, string methodName, Dictionary<string, object> methodParams, Func<PropertyDataCollection, T> converter)
    {
        var result = await TryCallInternalAsync(scope, query, methodName, methodParams).ConfigureAwait(false);
        if (result is null)
        {
            // Do not throw: sensor/capability probes hit this path every second on some machines.
            // Return default; callers already treat 0 / false / null as unsupported.
            // Permanent soft-fail is reserved for IsMethodMissing only (see TryGetWmiMethodParameters).
            return default!;
        }

        return converter(result.Properties);
    }

    /// <summary>
    /// Probe-friendly call: returns false instead of throwing when the firmware does not
    /// implement the method (e.g. Fan_GetCurrentFanSpeed on some Legion models).
    /// </summary>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    internal static async Task<(bool Success, T Value)> TryCallAsync<T>(
        string scope,
        FormattableString query,
        string methodName,
        Dictionary<string, object> methodParams,
        Func<PropertyDataCollection, T> converter,
        T fallback = default!)
    {
        var result = await TryCallInternalAsync(scope, query, methodName, methodParams).ConfigureAwait(false);
        if (result is null)
            return (false, fallback);

        try
        {
            return (true, converter(result.Properties));
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                $"wmi-trycall-convert-{methodName}",
                $"WMI TryCallAsync converter failed for {methodName}.",
                ex);
            return (false, fallback);
        }
    }

    /// <summary>
    /// Full WMI method call. Soft failures return false (no InvalidOperationException).
    /// Do not use GetAsyncWithTimeout here — it disposes the collection before InvokeMethod.
    /// </summary>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    private static bool TryExecuteWmiMethodCall(
        string scope,
        string queryFormatted,
        string methodName,
        Dictionary<string, object> methodParams,
        out ManagementBaseObject? result)
    {
        result = null;
        using var searcher = new ManagementObjectSearcher(scope, queryFormatted);
        using var collection = searcher.Get();
        var mo = collection.Cast<ManagementObject>().FirstOrDefault();
        if (mo is null)
            return false;

        if (!TryGetWmiMethodParameters(mo, methodName, scope, queryFormatted, out var args) || args is null)
            return false;

        foreach (var pair in methodParams)
            args[pair.Key] = pair.Value;

        return TryInvokeWmiMethod(mo, methodName, args, scope, queryFormatted, out result);
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    private static async Task<ManagementBaseObject?> TryCallInternalAsync(
        string scope,
        FormattableString query,
        string methodName,
        Dictionary<string, object> methodParams)
    {
        var queryFormatted = query.ToString(WMIPropertyValueFormatter.Instance);
        var softKey = SoftFailKey(scope, queryFormatted, methodName);
        if (_softFailedMethodKeys.ContainsKey(softKey))
            return null;

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var callTask = Task.Run(() =>
                {
                    if (TryExecuteWmiMethodCall(scope, queryFormatted, methodName, methodParams, out var r))
                        return r;
                    return null;
                });

                var completed = await Task.WhenAny(callTask, Task.Delay(_wmiInvokeTimeoutMs)).ConfigureAwait(false);
                if (completed != callTask)
                {
                    _ = callTask.ContinueWith(
                        t =>
                        {
                            try { _ = t.Exception; }
                            catch { /* observe only */ }
                        },
                        TaskScheduler.Default);

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace(
                            $"WMI method {methodName} timed out after {_wmiInvokeTimeoutMs}ms. [query={queryFormatted}]");
                    return null;
                }

                var result = await callTask.ConfigureAwait(false);
                if (result is not null)
                    return result;

                // True method-missing was cached during this attempt — do not retry.
                if (_softFailedMethodKeys.ContainsKey(softKey))
                    return null;

                // Null = transient soft invoke fail (Invalid object / invalid object, bad fan ID…).
                // Re-query once: each TryExecuteWmiMethodCall does a fresh Get(), matching the
                // historical contract when Lenovo invalidates the MO between Get and Invoke.
                // Previously Invalid object was rethrown only to hit catch-retry — that worked
                // but broke into the debugger on every sensor/fan poll.
                if (attempt < 2)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace(
                            $"WMI call soft-failed, retrying with fresh instance. [scope={scope}, query={queryFormatted}, methodName={methodName}, attempt={attempt}]");
                    continue;
                }

                return null;
            }
            catch (Exception ex) when (attempt < 2)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(
                        $"WMI call failed, retrying. [scope={scope}, query={queryFormatted}, methodName={methodName}, attempt={attempt}]",
                        ex);
            }
            catch (ManagementException ex) when (IsMethodMissing(ex))
            {
                MarkWmiMethodSoftFailed(scope, queryFormatted, methodName);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(
                        $"WMI method missing. [scope={scope}, query={queryFormatted}, methodName={methodName}]",
                        ex);
                return null;
            }
            catch (Exception ex)
            {
                // Transient errors: do not permanent-cache; let the next poll retry.
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(
                        $"WMI call failed (not cached). [scope={scope}, query={queryFormatted}, methodName={methodName}]",
                        ex);
                return null;
            }
        }

        return null;
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
