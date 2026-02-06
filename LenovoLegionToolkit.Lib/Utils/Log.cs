using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
    private static readonly Lazy<Log> _instance = new(() => new Log(), LazyThreadSafetyMode.ExecutionAndPublication);
    public static Log Instance => _instance.Value;

    private readonly object _lock = new();
    private readonly object _fileLock = new();
    private readonly string _folderPath;
    private volatile string _currentLogPath = string.Empty;
    private readonly Queue<string> _logQueue = new();
    private readonly int _maxLogSizeBytes = 50 * 1024 * 1024; // 50MB
    private readonly int _maxQueuedEntries = 1000; // Increased to reduce flush frequency
    private readonly Task _logTask;
    private readonly object _queueLock = new();
    private volatile bool _isRunning = true;

    public bool IsTraceEnabled { get; set; }
    public LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;

    public string LogPath => _currentLogPath ?? string.Empty;

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
                catch (Exception ex) 
                { 
                    // Log cleanup failures but continue processing
                    if (IsTraceEnabled)
                    {
                        var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
                        var threadId = Environment.CurrentManagedThreadId;
                        var logLine = $"[{timestamp}] [{threadId}] [Log.cs#76:CleanupOldLogFiles] [Trace] Failed to delete log file {logFiles[i]}: {ex.Message}";
                        try
                        {
                            File.AppendAllText(Path.Combine(_folderPath, $"cleanup_error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt"), logLine);
                        }
                        catch
                        {
                            // If we can't write the error, continue silently
                        }
                    }
                }
            }
        }
        catch (Exception ex) 
        { 
            // Log cleanup failures but continue processing
            if (IsTraceEnabled)
            {
                var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
                var threadId = Environment.CurrentManagedThreadId;
                var logLine = $"[{timestamp}] [{threadId}] [Log.cs#80:CleanupOldLogFiles] [Trace] Failed during log cleanup: {ex.Message}";
                try
                {
                    File.AppendAllText(Path.Combine(_folderPath, $"cleanup_error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt"), logLine);
                }
                catch
                {
                    // If we can't write the error, continue silently
                }
            }
        }
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

    // Convenience overloads that accept plain strings. These are helpful for callers
    // that pass string literals or non-interpolated strings so they don't need to
    // create FormattableString instances explicitly.
    public void Error(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        LogInternal(LogLevel.Error, global::System.Runtime.CompilerServices.FormattableStringFactory.Create(message, Array.Empty<object>()), ex, file, lineNumber, caller);
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

    public void Warning(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel >= LogLevel.Warning)
            LogInternal(LogLevel.Warning, global::System.Runtime.CompilerServices.FormattableStringFactory.Create(message, Array.Empty<object>()), ex, file, lineNumber, caller);
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

    public void Info(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel >= LogLevel.Info)
            LogInternal(LogLevel.Info, global::System.Runtime.CompilerServices.FormattableStringFactory.Create(message, Array.Empty<object>()), ex, file, lineNumber, caller);
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

    public void Debug(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (CurrentLogLevel >= LogLevel.Debug)
            LogInternal(LogLevel.Debug, global::System.Runtime.CompilerServices.FormattableStringFactory.Create(message, Array.Empty<object>()), ex, file, lineNumber, caller);
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

    public void Trace(string message,
        Exception? ex = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int lineNumber = -1,
        [CallerMemberName] string? caller = null)
    {
        if (IsTraceEnabled || CurrentLogLevel >= LogLevel.Trace)
            LogInternal(LogLevel.Trace, global::System.Runtime.CompilerServices.FormattableStringFactory.Create(message, Array.Empty<object>()), ex, file, lineNumber, caller);
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
                // Drain the queue every 500 ms to reduce I/O operations and improve performance
                await Task.Delay(500).ConfigureAwait(false);
                
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
            catch (Exception ex) 
            { 
                // Log queue processing failures but continue
                if (IsTraceEnabled)
                {
                    var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
                    var threadId = Environment.CurrentManagedThreadId;
                    var logLine = $"[{timestamp}] [{threadId}] [Log.cs#204:ProcessLogQueue] [Trace] Failed during queue processing: {ex.Message}";
                    try
                    {
                        File.AppendAllText(Path.Combine(_folderPath, $"queue_error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt"), logLine);
                    }
                    catch
                    {
                        // If we can't write the error, continue silently
                    }
                }
            }
        }
    }

    private async Task WriteToFileAsync(List<string> lines)
    {
        try
        {
            string logPathToUse;
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
                    catch { _currentLogPath = CreateNewLogFile(); }
                }
                else { _currentLogPath = CreateNewLogFile(); }
                logPathToUse = _currentLogPath;
            }

            // Synchronize actual file I/O to prevent IOExceptions when multiple threads 
            // try to write to the same file (e.g., during Flush and background processing)
            await Task.Run(() =>
            {
                lock (_fileLock)
                {
                    File.AppendAllLines(logPathToUse, lines);
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex) 
        { 
            // Log write failures but continue processing
            if (IsTraceEnabled)
            {
                var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
                var threadId = Environment.CurrentManagedThreadId;
                var logLine = $"[{timestamp}] [{threadId}] [Log.cs#242:WriteToFileAsync] [Trace] Failed to write to log file: {ex.Message}";
                try
                {
                    File.AppendAllText(Path.Combine(_folderPath, $"write_error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt"), logLine);
                }
                catch
                {
                    // If we can't write the error, continue silently
                }
            }
        }
    }
    
    private void ForceWriteToFile(List<string> lines)
    {
        try
        {
            string logPathToUse;
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
                    catch { _currentLogPath = CreateNewLogFile(); }
                }
                else { _currentLogPath = CreateNewLogFile(); }
                logPathToUse = _currentLogPath;
            }

            // Use the same lock as background processing
            lock (_fileLock)
            {
                File.AppendAllLines(logPathToUse, lines);
            }
        }
        catch (Exception ex) 
        { 
            // Log forced write failures but continue processing
            if (IsTraceEnabled)
            {
                var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
                var threadId = Environment.CurrentManagedThreadId;
                var logLine = $"[{timestamp}] [{threadId}] [Log.cs#279:ForceWriteToFile] [Trace] Failed during forced write: {ex.Message}";
                try
                {
                    File.AppendAllText(Path.Combine(_folderPath, $"force_write_error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt"), logLine);
                }
                catch
                {
                    // If we can't write the error, continue silently
                }
            }
        }
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
        // First, wait for ProcessLogQueue to complete its current iteration
        // This ensures any logs enqueued before shutdown are processed
        // ProcessLogQueue checks _isRunning every 500ms, so wait for one iteration
        const int ITERATION_DELAY_MS = 500; // ProcessLogQueue iteration delay
        await Task.Delay(ITERATION_DELAY_MS + 100).ConfigureAwait(false);
        
        // Now set the flag to stop ProcessLogQueue from starting new iterations
        // This prevents new logs from being processed, but any logs already in the queue
        // will be handled by Flush() at the end
        _isRunning = false;

        // Wait for ProcessLogQueue to exit its loop
        // ProcessLogQueue checks _isRunning every 500ms, so we need to wait at least that long
        // plus some buffer for thread scheduling and I/O operations
        const int MAX_WAIT_TIME_MS = 2000; // Total maximum wait time: 2 seconds
        const int BUFFER_TIME_MS = 500; // Buffer for I/O and thread scheduling
        
        var startTime = DateTime.UtcNow;
        
        try
        {
            // First, wait for the task to complete naturally, but limit to MAX_WAIT_TIME_MS
            var firstWaitMs = MAX_WAIT_TIME_MS;
            var completedTask = await Task.WhenAny(_logTask, Task.Delay(firstWaitMs)).ConfigureAwait(false);
            
            // If the task hasn't completed, calculate remaining time and wait for one more iteration
            if (completedTask != _logTask)
            {
                var elapsedMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                var remainingTimeMs = MAX_WAIT_TIME_MS - elapsedMs;
                
                // Only wait additional time if we haven't exceeded MAX_WAIT_TIME_MS
                if (remainingTimeMs > 0)
                {
                    // Wait for at least one more iteration cycle plus buffer, but don't exceed remaining time
                    var additionalWaitMs = Math.Min(ITERATION_DELAY_MS + BUFFER_TIME_MS, remainingTimeMs);
                    if (additionalWaitMs > 0)
                    {
                        var additionalWait = Task.Delay(additionalWaitMs);
                        completedTask = await Task.WhenAny(_logTask, additionalWait).ConfigureAwait(false);
                    }
                }
            }
            
            // Final check: if still not completed, wait synchronously with remaining timeout
            if (!_logTask.IsCompleted)
            {
                try
                {
                    // Calculate remaining timeout based on elapsed time, ensuring we don't exceed MAX_WAIT_TIME_MS
                    var elapsedMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    var remainingTimeout = Math.Max(0, Math.Min(500, MAX_WAIT_TIME_MS - elapsedMs));
                    if (remainingTimeout > 0)
                    {
                        _logTask.Wait(remainingTimeout);
                    }
                }
                catch (Exception) 
                { 
                    // If task faults or times out, log it but continue to flush
                    // Note: We must use ForceWriteToFile directly here because _isRunning is already false,
                    // so ProcessLogQueue has exited and won't process queued messages
                    if (IsTraceEnabled)
                    {
                        var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
                        var threadId = Environment.CurrentManagedThreadId;
                        var logLine = $"[{timestamp}] [{threadId}] [Log.cs#380:ShutdownAsync] [Trace] Log task did not complete within timeout during shutdown. Proceeding with flush.";
                        ForceWriteToFile(new List<string> { logLine });
                    }
                }
            }
        }
        catch (Exception) 
        { 
            // Ignore timeout or cancellation while waiting
            // Continue to flush to ensure any pending logs are written
        }

        // Flush any remaining queued entries
        // This is safe even if ProcessLogQueue is still running because Flush() uses locks
        Flush();
        
        // Final attempt to wait for any ongoing write operations to complete
        // This ensures ProcessLogQueue has finished any File.AppendAllLinesAsync calls
        if (!_logTask.IsCompleted)
        {
            try
            {
                // Give a final short wait for any ongoing I/O operations
                const int FINAL_WAIT_MS = 300; // Final wait for I/O operations to complete
                await Task.WhenAny(_logTask, Task.Delay(FINAL_WAIT_MS)).ConfigureAwait(false);
            }
            catch (Exception ex) 
            { 
                // Log final wait failures
                if (IsTraceEnabled)
                {
                    var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
                    var threadId = Environment.CurrentManagedThreadId;
                    var logLine = $"[{timestamp}] [{threadId}] [Log.cs#392:ShutdownAsync] [Trace] Failed during final wait: {ex.Message}";
                    try
                    {
                        File.AppendAllText(Path.Combine(_folderPath, $"shutdown_wait_error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt"), logLine);
                    }
                    catch
                    {
                        // If we can't write the error, continue silently
                    }
                }
            }
        }
    }

    public void Shutdown()
    {
        try
        {
            // Use ConfigureAwait(false) to avoid potential deadlocks in synchronization contexts
            ShutdownAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Log shutdown errors to help with debugging
            if (IsTraceEnabled)
            {
                var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff");
                var threadId = Environment.CurrentManagedThreadId;
                var logLine = $"[{timestamp}] [{threadId}] [Log.cs#396:Shutdown] [Trace] Error during shutdown: {ex.Message}";
                try
                {
                    File.AppendAllText(Path.Combine(_folderPath, $"shutdown_error_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.txt"), logLine);
                }
                catch
                {
                    // If we can't even write the error, there's nothing more we can do
                }
            }
        }
    }

    private static string Serialize(Exception ex) => new StringBuilder()
        .AppendLine("=== Exception ===")
        .AppendLine(ex.ToString())
        .AppendLine()
        .AppendLine("=== Exception demystified ===")
        .AppendLine(ex.ToStringDemystified())
        .ToString();
}
