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

    public static bool IsTraceEnabled => _isTraceEnabled();

    public static void Configure(Func<bool> isTraceEnabled, Action<string, Exception?> trace)
    {
        _isTraceEnabled = isTraceEnabled ?? throw new ArgumentNullException(nameof(isTraceEnabled));
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
    }

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
            return;
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
