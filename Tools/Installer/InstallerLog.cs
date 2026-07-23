using System;
using System.IO;

namespace UniversalDeviceToolkit.Installer;

/// <summary>
/// Minimal file logger so silent runs stay diagnosable. Writes to
/// %TEMP%\udt-installer.log; failures to log never break the install.
/// </summary>
internal static class InstallerLog
{
    private static readonly object Gate = new();
    private static string? _path;

    public static void Enable()
    {
        _path ??= Path.Combine(Path.GetTempPath(), "udt-installer.log");
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message}: {ex}");

    private static void Write(string level, string message)
    {
        if (_path is null)
            return;

        try
        {
            lock (Gate)
            {
                File.AppendAllText(_path,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never fail the install.
        }
    }
}
