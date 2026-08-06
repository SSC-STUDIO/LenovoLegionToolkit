using System;

namespace UniversalDeviceToolkit.Plugins.Core;

/// <summary>
/// Plugin-side logging fa莽ade. Plugins call <see cref="Trace"/> and <see cref="Error"/>
/// without depending on any concrete host logger. The hosting plugin must call
/// <see cref="Configure"/> once at startup to bridge to the real logger
/// (e.g. UniversalDeviceToolkit.Lib.Utils.Log.Instance).
/// </summary>
public static class PluginLog
{
    private static Func<bool> _isTraceEnabled = static () => false;
    private static Action<string, Exception?> _trace = static (_, _) => { };
    private static Action<string, Exception?> _error = static (_, _) => { };

    /// <summary>
    /// Gets whether trace-level logging is currently enabled.
    /// Returns false before <see cref="Configure"/> is called.
    /// </summary>
    public static bool IsTraceEnabled => _isTraceEnabled();

    /// <summary>
    /// Configures the logging fa莽ade by providing a trace-enable predicate, a trace-level
    /// log-writing action, and an error-level log-writing action.
    /// Must be called once at startup before any log messages are produced.
    /// </summary>
    /// <param name="isTraceEnabled">A predicate that returns true when trace-level logging should be emitted.</param>
    /// <param name="trace">The action that writes a trace-level log message with an optional exception.</param>
    /// <param name="error">The action that writes an error-level log message. Error messages must always be
    /// logged regardless of trace level, because errors represent genuine failures.
    /// When null, <see cref="Error"/> falls back to the <paramref name="trace"/> action.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="isTraceEnabled"/> or <paramref name="trace"/> is null.</exception>
    public static void Configure(Func<bool> isTraceEnabled, Action<string, Exception?> trace, Action<string, Exception?>? error = null)
    {
        _isTraceEnabled = isTraceEnabled ?? throw new ArgumentNullException(nameof(isTraceEnabled));
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _error = error ?? trace;
    }

    /// <summary>
    /// Resets the logging fa莽ade to its unconfigured state.
    /// After calling this method, <see cref="IsTraceEnabled"/> returns false
    /// and log calls become no-ops until <see cref="Configure"/> is called again.
    /// </summary>
    public static void Reset()
    {
        _isTraceEnabled = static () => false;
        _trace = static (_, _) => { };
        _error = static (_, _) => { };
    }

    /// <summary>
    /// Logs a trace message only when <see cref="IsTraceEnabled"/> is true.
    /// Safe to call before <see cref="Configure"/> 鈥?becomes a no-op.
    /// </summary>
    public static void Trace(string message, Exception? exception = null)
    {
        if (!_isTraceEnabled())
        {
            return;
        }
        _trace(message, exception);
    }

    /// <summary>
    /// Logs an error-level message. Error messages are always logged regardless of trace level,
    /// because errors represent genuine failures that must be visible in production.
    /// Uses the error-level sink configured via <see cref="Configure(Func{bool}, Action{string, Exception?}, Action{string, Exception?}?)"/>.
    /// When no dedicated error sink is configured, falls back to the trace sink.
    /// </summary>
    public static void Error(string message, Exception? exception = null)
    {
        _error(message, exception);
    }
}
