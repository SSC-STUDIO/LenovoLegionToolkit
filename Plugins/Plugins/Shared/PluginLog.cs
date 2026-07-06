using System;

namespace LenovoLegionToolkit.Plugins.Shared;

/// <summary>
/// Plugin-side logging façade. Plugins call <see cref="Trace"/> and <see cref="Error"/>
/// without depending on any concrete host logger. The hosting plugin must call
/// <see cref="Configure"/> once at startup to bridge to the real logger
/// (e.g. LenovoLegionToolkit.Lib.Utils.Log.Instance).
/// </summary>
public static class PluginLog
{
    private static Func<bool> _isTraceEnabled = static () => false;
    private static Action<string, Exception?> _trace = static (_, _) => { };

    /// <summary>
    /// Gets whether trace-level logging is currently enabled.
    /// Returns false before <see cref="Configure"/> is called.
    /// </summary>
    public static bool IsTraceEnabled => _isTraceEnabled();

    /// <summary>
    /// Configures the logging façade by providing a trace-enable predicate and a log-writing action.
    /// Must be called once at startup before any log messages are produced.
    /// </summary>
    /// <param name="isTraceEnabled">A predicate that returns true when trace-level logging should be emitted.</param>
    /// <param name="trace">The action that writes a log message with an optional exception.</param>
    /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
    public static void Configure(Func<bool> isTraceEnabled, Action<string, Exception?> trace)
    {
        _isTraceEnabled = isTraceEnabled ?? throw new ArgumentNullException(nameof(isTraceEnabled));
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
    }

    /// <summary>
    /// Resets the logging façade to its unconfigured state.
    /// After calling this method, <see cref="IsTraceEnabled"/> returns false
    /// and log calls become no-ops until <see cref="Configure"/> is called again.
    /// </summary>
    public static void Reset()
    {
        _isTraceEnabled = static () => false;
        _trace = static (_, _) => { };
    }

    /// <summary>
    /// Logs a trace message only when <see cref="IsTraceEnabled"/> is true.
    /// Safe to call before <see cref="Configure"/> — becomes a no-op.
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
    /// </summary>
    public static void Error(string message, Exception? exception = null)
    {
        _trace(message, exception);
    }
}
