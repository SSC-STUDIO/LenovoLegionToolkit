using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using LenovoLegionToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Helper class for saving and managing crash reports.
/// Crash reports are saved locally and can be reviewed by the user on next startup.
/// </summary>
public static class CrashReportHelper
{
    private static readonly string CrashReportFolder = Path.Combine(Folders.AppData, "crash_reports");

    /// <summary>
    /// Directory where crash reports are stored.
    /// </summary>
    public static string CrashReportDirectory => CrashReportFolder;

    /// <summary>
    /// Saves a crash report to disk with system and exception information.
    /// </summary>
    /// <param name="exception">The exception that caused the crash.</param>
    /// <param name="source">The source of the exception (e.g., "AppDomain", "Dispatcher", "Task").</param>
    /// <returns>The path to the saved crash report file.</returns>
    public static string SaveCrashReport(Exception? exception, string source = "Unknown")
    {
        try
        {
            Directory.CreateDirectory(CrashReportFolder);

            var report = new CrashReport
            {
                Timestamp = DateTime.UtcNow,
                AppVersion = GetAppVersion(),
                OsVersion = Environment.OSVersion.VersionString,
                RuntimeVersion = $".NET {Environment.Version}",
                ExceptionType = exception?.GetType().FullName ?? "Unknown",
                ExceptionMessage = exception?.Message ?? "No exception message",
                StackTrace = exception?.StackTrace ?? "No stack trace available",
                Source = source,
                Uptime = AppUptimeTracker.GetUptime()
            };

            // Include inner exception if present
            if (exception?.InnerException != null)
            {
                report.InnerExceptionType = exception.InnerException.GetType().FullName;
                report.InnerExceptionMessage = exception.InnerException.Message;
                report.InnerExceptionStackTrace = exception.InnerException.StackTrace;
            }

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var fileName = $"crash_{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}.json";
            var filePath = Path.Combine(CrashReportFolder, fileName);

            File.WriteAllText(filePath, json, new UTF8Encoding(false));

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Crash report saved to: {filePath}");

            return filePath;
        }
        catch (Exception ex)
        {
            // If we can't save the crash report, at least try to log the error
            try
            {
                Log.Instance.Error($"Failed to save crash report: {ex.Message}", ex);
            }
            catch
            {
                // Ignore any logging failures
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Gets all unsent crash reports from the crash report directory.
    /// </summary>
    /// <returns>An enumerable of file paths to crash report files.</returns>
    public static IEnumerable<string> GetUnsentCrashReports()
    {
        try
        {
            if (!Directory.Exists(CrashReportFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(CrashReportFolder, "crash_*.json");
        }
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "crash-report-list",
                "Failed to enumerate unsent crash reports.",
                ex);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Deletes a crash report file.
    /// </summary>
    /// <param name="path">The path to the crash report file to delete.</param>
    public static void DeleteCrashReport(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to delete crash report: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a crash report from a file.
    /// </summary>
    /// <param name="path">The path to the crash report file.</param>
    /// <returns>The deserialized crash report, or null if loading fails.</returns>
    public static CrashReport? LoadCrashReport(string path)
    {
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<CrashReport>(json);
        }
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "crash-report-load",
                $"Failed to load crash report: {path}",
                ex);
            return null;
        }
    }

    /// <summary>
    /// Deletes all crash reports older than the specified number of days.
    /// </summary>
    /// <param name="daysToKeep">Number of days to keep crash reports. Default is 30.</param>
    public static void CleanupOldCrashReports(int daysToKeep = 30)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);

            foreach (var file in GetUnsentCrashReports())
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoff)
                {
                    DeleteCrashReport(file);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "crash-report-cleanup",
                "Failed during old crash report cleanup.",
                ex);
        }
    }

    private static string GetAppVersion()
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly();
            var version = assembly?.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "crash-report-app-version",
                "Failed to read app version for crash report metadata.",
                ex);
            return "Unknown";
        }
    }
}

/// <summary>
/// Represents a crash report with all relevant system and exception information.
/// </summary>
public class CrashReport
{
    /// <summary>
    /// The UTC timestamp when the crash occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The application version when the crash occurred.
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// The operating system version.
    /// </summary>
    public string OsVersion { get; set; } = string.Empty;

    /// <summary>
    /// The .NET runtime version.
    /// </summary>
    public string RuntimeVersion { get; set; } = string.Empty;

    /// <summary>
    /// The fully qualified type name of the exception.
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// The exception message.
    /// </summary>
    public string ExceptionMessage { get; set; } = string.Empty;

    /// <summary>
    /// The stack trace of the exception.
    /// </summary>
    public string StackTrace { get; set; } = string.Empty;

    /// <summary>
    /// The source of the exception (e.g., "AppDomain", "Dispatcher", "Task").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The application uptime when the crash occurred.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// The fully qualified type name of the inner exception, if any.
    /// </summary>
    public string? InnerExceptionType { get; set; }

    /// <summary>
    /// The inner exception message, if any.
    /// </summary>
    public string? InnerExceptionMessage { get; set; }

    /// <summary>
    /// The inner exception stack trace, if any.
    /// </summary>
    public string? InnerExceptionStackTrace { get; set; }
}

/// <summary>
/// Tracks application uptime for crash reports.
/// </summary>
internal static class AppUptimeTracker
{
    private static readonly DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Gets the application uptime.
    /// </summary>
    public static TimeSpan GetUptime() => DateTime.UtcNow - _startTime;
}
