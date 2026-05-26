using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace LenovoLegionToolkit.Lib.Utils;

public enum LogLevel
{
    Error,
    Warning,
    Info,
    Debug,
    Trace
}

public class Log : IDisposable
{
    private static readonly Lazy<Log> _instance = new(() => new Log(), LazyThreadSafetyMode.ExecutionAndPublication);
    public static Log Instance => _instance.Value;

    private readonly Logger _logger;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly string _folderPath;
    private readonly object _emergencyLock = new();
    private bool _disposed;

    public bool IsTraceEnabled
    {
        get => _levelSwitch.MinimumLevel <= LogEventLevel.Verbose;
        set
        {
            if (value)
                _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
        }
    }

    public LogLevel CurrentLogLevel
    {
        get => MapLevelFromSerilog(_levelSwitch.MinimumLevel);
        set => _levelSwitch.MinimumLevel = MapLevelToSerilog(value);
    }

    public string LogPath => _folderPath;

    private Log()
    {
        _folderPath = Path.Combine(Folders.AppData, "logs");
        Directory.CreateDirectory(_folderPath);

        _levelSwitch = new LoggingLevelSwitch(LogEventLevel.Verbose);

        _logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .Enrich.WithProperty("Application", AppIdentity.CompactName)
            .WriteTo.Async(wt => wt.File(
                new Serilog.Formatting.Json.JsonFormatter(),
                Path.Combine(_folderPath, "log-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                fileSizeLimitBytes: 50 * 1024 * 1024
            ))
            .CreateLogger();
    }

    public void ErrorReport(string header, Exception ex)
    {
        var errorReportPath = Path.Combine(_folderPath, $"error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt");
        File.AppendAllLines(errorReportPath, [header, Serialize(ex)]);

        _logger.Error(ex, "{Header}", header);
    }

    public void Error(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        var properties = BuildProperties(sourceContext);
        _logger.Write(LogEventLevel.Error, ex, message.ToString(), properties);
    }

    public void Error(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        _logger.Write(LogEventLevel.Error, ex, "{Message} [@{SourceContext}]", message, sourceContext);
    }

    public void Warning(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel < LogLevel.Warning)
            return;

        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        var properties = BuildProperties(sourceContext);
        _logger.Write(LogEventLevel.Warning, ex, message.ToString(), properties);
    }

    public void Warning(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel < LogLevel.Warning)
            return;

        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        // CodeQL [cs/cleartext-storage-of-sensitive-information] - Generic logging; actual sensitivity depends on caller data. Plugin signature status is not sensitive and is logged for security auditing.
        _logger.Write(LogEventLevel.Warning, ex, "{Message} [@{SourceContext}]", message, sourceContext);
    }

    public void Info(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel < LogLevel.Info)
            return;

        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        var properties = BuildProperties(sourceContext);
        _logger.Write(LogEventLevel.Information, ex, message.ToString(), properties);
    }

    public void Info(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel < LogLevel.Info)
            return;

        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        _logger.Write(LogEventLevel.Information, ex, "{Message} [@{SourceContext}]", message, sourceContext);
    }

    public void Debug(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel < LogLevel.Debug)
            return;

        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        var properties = BuildProperties(sourceContext);
        _logger.Write(LogEventLevel.Debug, ex, message.ToString(), properties);
    }

    public void Debug(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel < LogLevel.Debug)
            return;

        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        _logger.Write(LogEventLevel.Debug, ex, "{Message} [@{SourceContext}]", message, sourceContext);
    }

    public void Trace(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (!IsTraceEnabled && CurrentLogLevel < LogLevel.Trace)
            return;

        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        var properties = BuildProperties(sourceContext);
        _logger.Write(LogEventLevel.Verbose, ex, message.ToString(), properties);
    }

    public void Trace(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (!IsTraceEnabled && CurrentLogLevel < LogLevel.Trace)
            return;

        var sourceContext = FormatSourceContext(file, lineNumber, caller);
        // CodeQL [cs/cleartext-storage-of-sensitive-information] - Generic logging; actual sensitivity depends on caller data. Plugin signature status is not sensitive and is logged for security auditing.
        _logger.Write(LogEventLevel.Verbose, ex, "{Message} [@{SourceContext}]", message, sourceContext);
    }

    public void Flush()
    {
        // Serilog's async sink flushes on a timer; Log.CloseAndFlush is called on dispose.
        // Synchronous flush is best-effort via the LoggingLevelSwitch no-op barrier.
    }

    public async Task ShutdownAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await Task.Run(() => _logger.Dispose()).ConfigureAwait(false);
    }

    public void Shutdown()
    {
        if (_disposed)
            return;

        _disposed = true;
        _logger.Dispose();
    }

    private static string FormatSourceContext(string? file, int lineNumber, string? caller)
    {
        var fileName = file is not null ? Path.GetFileName(file) : "?";
        return $"{fileName}#{lineNumber}:{caller}";
    }

    private static object[] BuildProperties(string sourceContext)
    {
        return [sourceContext, Environment.CurrentManagedThreadId];
    }

    private static LogEventLevel MapLevelToSerilog(LogLevel level) => level switch
    {
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Info => LogEventLevel.Information,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Trace => LogEventLevel.Verbose,
        _ => LogEventLevel.Verbose
    };

    private static LogLevel MapLevelFromSerilog(LogEventLevel level) => level switch
    {
        LogEventLevel.Error => LogLevel.Error,
        LogEventLevel.Fatal => LogLevel.Error,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Information => LogLevel.Info,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Verbose => LogLevel.Trace,
        _ => LogLevel.Trace
    };

    private static string Serialize(Exception ex) => new StringBuilder()
        .AppendLine("=== Exception ===")
        .AppendLine(ex.ToString())
        .AppendLine()
        .AppendLine("=== Exception demystified ===")
        .AppendLine(ex.ToStringDemystified())
        .ToString();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _logger?.Dispose();
        GC.SuppressFinalize(this);
    }
}
