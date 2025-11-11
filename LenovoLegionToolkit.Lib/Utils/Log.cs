using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Utils;

public enum LogLevel
{
    Error,
    Warning,
    Info,
    Debug,
    Trace
}

public class Log
{
    private static Log? _instance;
    public static Log Instance
    {
        get
        {
            _instance ??= new Log();
            return _instance;
        }
    }

    private readonly object _lock = new();
    private readonly string _folderPath;
    private volatile string _currentLogPath;
    private readonly Queue<string> _logQueue = new();
    private readonly int _maxLogSizeBytes = 50 * 1024 * 1024; // 50MB
    private readonly int _maxQueuedEntries = 100;
    private readonly Task _logTask;
    private readonly object _queueLock = new();
    private volatile bool _isRunning = true;

    public bool IsTraceEnabled { get; set; }
    public LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;

    public string LogPath => _currentLogPath;

    private Log()
    {
        _folderPath = Path.Combine(Folders.AppData, "log");
        Directory.CreateDirectory(_folderPath);
        _currentLogPath = CreateNewLogFile();
        // Start background task that writes log entries asynchronously
        _logTask = Task.Run(ProcessLogQueue);
    }

    private string CreateNewLogFile()
    {
        lock (_lock)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss_fff");
            var logPath = Path.Combine(_folderPath, $"log_{timestamp}.txt");
            // Remove older log files, keeping only the ten most recent entries
            CleanupOldLogFiles();
            return logPath;
        }
    }

    private void CleanupOldLogFiles()
    {
        try
        {
            var logFiles = Directory.GetFiles(_folderPath, "log_*.txt")
                .OrderByDescending(File.GetLastWriteTime)
                .ToList();
            
            for (int i = 10; i < logFiles.Count; i++)
            {
                try
                {
                    File.Delete(logFiles[i]);
                }
                catch (Exception) { /* Ignore failures while deleting legacy log files */ }
            }
        }
        catch (Exception) { /* Ignore unexpected failures while cleaning up logs */ }
    }

    public void ErrorReport(string header, Exception ex)
    {
        var errorReportPath = Path.Combine(_folderPath, $"error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt");
        File.AppendAllLines(errorReportPath, [header, Serialize(ex)]);
    }

    public void Error(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        LogInternal(LogLevel.Error, message, ex, file, lineNumber, caller);
    }

    public void Warning(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel >= LogLevel.Warning)
            LogInternal(LogLevel.Warning, message, ex, file, lineNumber, caller);
    }

    public void Info(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel >= LogLevel.Info)
            LogInternal(LogLevel.Info, message, ex, file, lineNumber, caller);
    }

    public void Debug(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel >= LogLevel.Debug)
            LogInternal(LogLevel.Debug, message, ex, file, lineNumber, caller);
    }

    public void Trace(FormattableString message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (IsTraceEnabled || CurrentLogLevel >= LogLevel.Trace)
            LogInternal(LogLevel.Trace, message, ex, file, lineNumber, caller);
    }

    private void LogInternal(LogLevel level,
        FormattableString message,
        Exception? ex,
        string? file,
        int lineNumber,
        string? caller)
    {
        var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
        var threadId = Environment.CurrentManagedThreadId;
        var fileName = Path.GetFileName(file);
        
        var logLine = $"[{timestamp}] [{threadId}] [{fileName}#{lineNumber}:{caller}] [{level}] {message}";
        var logLines = new List<string> { logLine };
        
        if (ex is not null)
            logLines.Add(Serialize(ex));

#if DEBUG
        foreach (var line in logLines)
            global::System.Diagnostics.Debug.WriteLine(line);
#endif
        
        // Enqueue log lines for asynchronous processing
        EnqueueLogLines(logLines);
    }
    
    private void EnqueueLogLines(List<string> logLines)
    {
        lock (_queueLock)
        {
            // Force a flush when the queue reaches the maximum capacity
            if (_logQueue.Count >= _maxQueuedEntries)
            {
                ForceWriteToFile(_logQueue.ToList());
                _logQueue.Clear();
            }
            
            foreach (var line in logLines)
                _logQueue.Enqueue(line);
        }
    }
    
    private async Task ProcessLogQueue()
    {
        while (_isRunning)
        {
            try
            {
                // Drain the queue every 100 ms
                await Task.Delay(100).ConfigureAwait(false);
                
                List<string>? linesToWrite = null;
                lock (_queueLock)
                {
                    if (_logQueue.Count > 0)
                    {
                        linesToWrite = _logQueue.ToList();
                        _logQueue.Clear();
                    }
                }
                
                if (linesToWrite?.Count > 0)
                {
                    await WriteToFileAsync(linesToWrite).ConfigureAwait(false);
                }
            }
            catch (Exception) { /* Ignore transient failures while draining the queue */ }
        }
    }
    
    private string GetLogPathWithRotation()
    {
        lock (_lock)
        {
            if (File.Exists(_currentLogPath))
            {
                try
                {
                    var fileInfo = new FileInfo(_currentLogPath);
                    if (fileInfo.Length > _maxLogSizeBytes)
                        _currentLogPath = CreateNewLogFile();
                }
                catch (Exception)
                {
                    _currentLogPath = CreateNewLogFile();
                }
            }
            else
            {
                _currentLogPath = CreateNewLogFile();
            }

            return _currentLogPath;
        }
    }

    private async Task WriteToFileAsync(List<string> lines)
    {
        try
        {
            var logPathToUse = GetLogPathWithRotation();

            // Append asynchronously to the current log file
            await File.AppendAllLinesAsync(logPathToUse, lines).ConfigureAwait(false);
        }
        catch (Exception) { /* Ignore transient write failures */ }
    }
    
    private void ForceWriteToFile(List<string> lines)
    {
        try
        {
            var logPathToUse = GetLogPathWithRotation();
            File.AppendAllLines(logPathToUse, lines);
        }
        catch (Exception) { /* Ignore failures during the forced write */ }
    }

    public void Flush()
    {
        List<string>? remainingLines = null;
        lock (_queueLock)
        {
            if (_logQueue.Count > 0)
            {
                remainingLines = _logQueue.ToList();
                _logQueue.Clear();
            }
        }
        
        if (remainingLines?.Count > 0)
            ForceWriteToFile(remainingLines);
    }

    public async Task ShutdownAsync()
    {
        _isRunning = false;

        try
        {
            await Task.WhenAny(_logTask, Task.Delay(1000)).ConfigureAwait(false);
        }
        catch (Exception) { /* Ignore timeout or cancellation while waiting */ }

        Flush();
    }

    public void Shutdown() => ShutdownAsync().GetAwaiter().GetResult();

    private static string Serialize(Exception ex) => new StringBuilder()
        .AppendLine("=== Exception ===")
        .AppendLine(ex.ToString())
        .AppendLine()
        .AppendLine("=== Exception demystified ===")
        .AppendLine(ex.ToStringDemystified())
        .ToString();
}
