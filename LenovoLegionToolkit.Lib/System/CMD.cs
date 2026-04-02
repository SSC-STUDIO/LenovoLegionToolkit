using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.System;

/// <summary>
/// Provides secure command execution with comprehensive injection prevention.
/// </summary>
public static class CMD
{
    // Dangerous patterns for command injection detection
    private static readonly string[] DangerousPatterns = new[]
    {
        "&&",      // Command chaining
        "||",      // Command chaining
        "|",       // Pipe chaining
        ";",       // Command separator
        "`",       // PowerShell execution
        "$(",      // Command substitution
        "..",      // Directory traversal
        "../",     // Directory traversal
        "..\\",    // Directory traversal
        "%00",     // Null byte injection
        "${",      // Shell variable expansion
        "<(",      // Process substitution
    };

    // PowerShell specific dangerous patterns
    private static readonly Regex[] PowerShellDangerousPatterns = new[]
    {
        new Regex(@"-[eE][nN][cC]?\s+", RegexOptions.Compiled),
        new Regex(@"-[eE][nN][cC]?\s+[a-zA-Z0-9+/]{50,}={0,2}", RegexOptions.Compiled),
        new Regex(@"[iI][eE][xX]\s|[iI][eE][xX]\)|[iI][eE][xX]$", RegexOptions.Compiled),
        new Regex(@"[iI]nvoke-[eE]xpression", RegexOptions.Compiled),
    };

    /// <summary>
    /// Runs a command asynchronously with comprehensive security validation.
    /// </summary>
    public static async Task<(int, string)> RunAsync(string file, string? arguments, bool createNoWindow = true, bool waitForExit = true, Dictionary<string, string?>? environment = null, CancellationToken token = default)
    {
        // Input validation to prevent command injection
        if (!IsValidFileName(file))
            throw new ArgumentException("Invalid file name", nameof(file));
        
        if (arguments != null && ContainsDangerousInput(arguments))
            throw new ArgumentException("Arguments contain dangerous characters", nameof(arguments));

        if (waitForExit && string.IsNullOrWhiteSpace(arguments) && RequiresArgumentsForNonInteractiveShell(file))
            throw new ArgumentException("Interactive shell executables require arguments when waiting for exit", nameof(arguments));

        // Additional PowerShell-specific validation
        if (IsPowerShellExecutable(file) && arguments != null)
        {
            if (ContainsPowerShellDangerousPatterns(arguments))
                throw new ArgumentException("PowerShell arguments contain dangerous patterns", nameof(arguments));
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Running... [file={file}, argument={arguments}, createNoWindow={createNoWindow}, waitForExit={waitForExit}, environment=[{(environment is null ? string.Empty : string.Join(",", environment))}]]");

        Process cmd = new Process();
        try
        {
            var shouldRedirectOutput = createNoWindow && waitForExit;

            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.CreateNoWindow = createNoWindow;
            cmd.StartInfo.RedirectStandardOutput = shouldRedirectOutput;
            cmd.StartInfo.RedirectStandardError = shouldRedirectOutput;
            cmd.StartInfo.WindowStyle = createNoWindow ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal;
            cmd.StartInfo.FileName = file;
            if (!string.IsNullOrWhiteSpace(arguments))
                cmd.StartInfo.Arguments = arguments;

            if (environment is not null)
            {
                foreach (var (key, value) in environment)
                {
                    if (!IsValidEnvironmentVariable(key))
                        throw new ArgumentException($"Invalid environment variable: {key}");

                    if (value == null)
                    {
                        // If value is null, remove the environment variable
                        cmd.StartInfo.Environment.Remove(key);
                    }
                    else if (!ContainsDangerousInput(value))
                    {
                        cmd.StartInfo.Environment[key] = value;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid environment variable value: {key}");
                    }
                }
            }

            cmd.Start();

            if (!waitForExit)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ran [file={file}, argument={arguments}, createNoWindow={createNoWindow}, waitForExit={waitForExit}, environment=[{(environment is null ? string.Empty : string.Join(",", environment))}]]");

                // When waitForExit is false, the process runs asynchronously.
                // We must not dispose the Process object while the process is still running.
                cmd = null!; // Release reference, process continues running
                return (-1, string.Empty);
            }

            Task<string>? standardOutputTask = null;
            Task<string>? standardErrorTask = null;
            if (shouldRedirectOutput)
            {
                standardOutputTask = cmd.StandardOutput.ReadToEndAsync(token);
                standardErrorTask = cmd.StandardError.ReadToEndAsync(token);
            }

            await cmd.WaitForExitAsync(token).ConfigureAwait(false);

            var exitCode = cmd.ExitCode;
            var output = string.Empty;
            if (shouldRedirectOutput)
            {
                await Task.WhenAll(standardOutputTask!, standardErrorTask!).ConfigureAwait(false);

                output = standardOutputTask!.Result;
                var error = standardErrorTask!.Result;
                if (!string.IsNullOrWhiteSpace(error))
                    output = string.IsNullOrWhiteSpace(output) ? error : $"{output}{Environment.NewLine}{error}";
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ran [file={file}, argument={arguments}, createNoWindow={createNoWindow}, waitForExit={waitForExit}, exitCode={exitCode} output={output}]");

            return (exitCode, output);
        }
        finally
        {
            if (waitForExit && cmd is not null)
            {
                cmd.Dispose();
            }
        }
    }

    /// <summary>
    /// Validates a filename for security issues.
    /// </summary>
    private static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Check for dangerous characters in the original input BEFORE normalization
        if (fileName.Contains("..") || fileName.Contains('|'))
            return false;

        try
        {
            // Check if it's a valid file path
            var path = Path.GetFullPath(fileName);
            
            // After normalization, check again for directory traversal
            if (path.Contains("..") || path.Contains('|'))
                return false;
            
            var fileInfo = new FileInfo(path);
            
            // Validate the filename portion using Windows invalid character list
            var fileNameOnly = fileInfo.Name;
            if (string.IsNullOrWhiteSpace(fileNameOnly))
                return false;
            
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileNameOnly.IndexOfAny(invalidChars) >= 0)
                return false;
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates environment variable name format.
    /// </summary>
    private static bool IsValidEnvironmentVariable(string variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            return false;

        // Environment variable names should only contain alphanumeric characters and underscores
        foreach (char c in variableName)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if the executable requires arguments for safe non-interactive execution.
    /// </summary>
    private static bool RequiresArgumentsForNonInteractiveShell(string fileName)
    {
        var executableName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(executableName))
            return false;

        return executableName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase)
            || executableName.Equals("cmd", StringComparison.OrdinalIgnoreCase)
            || executableName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase)
            || executableName.Equals("powershell", StringComparison.OrdinalIgnoreCase)
            || executableName.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase)
            || executableName.Equals("pwsh", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the file is a PowerShell executable.
    /// </summary>
    private static bool IsPowerShellExecutable(string fileName)
    {
        var executableName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(executableName))
            return false;

        return executableName.Equals("powershell", StringComparison.OrdinalIgnoreCase)
            || executableName.Equals("pwsh", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks for PowerShell-specific dangerous patterns.
    /// </summary>
    private static bool ContainsPowerShellDangerousPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        foreach (var regex in PowerShellDangerousPatterns)
        {
            if (regex.IsMatch(input))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if input contains dangerous patterns that could be used for command injection.
    /// </summary>
    public static bool ContainsDangerousInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Check for dangerous patterns
        foreach (string pattern in DangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.Ordinal))
                return true;
        }

        // Check for single ampersand command separator (but allow redirection patterns)
        if (ContainsUnescapedAmpersand(input))
            return true;

        return false;
    }

    /// <summary>
    /// Checks for command separator ampersands vs redirection patterns.
    /// </summary>
    private static bool ContainsUnescapedAmpersand(string input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '&')
            {
                // Skip escaped ampersands (^&)
                if (i > 0 && input[i - 1] == '^')
                    continue;

                // Check if this is part of a redirection pattern
                bool isRedirection = false;

                // Check for >&N pattern
                if (i > 0 && input[i - 1] == '>')
                {
                    if (i + 1 < input.Length && (input[i + 1] == '1' || input[i + 1] == '2'))
                    {
                        if (i >= 2 && input[i - 2] is '1' or '2')
                        {
                            isRedirection = true;
                        }
                        else if (i < 2 || char.IsWhiteSpace(input[i - 2]))
                        {
                            isRedirection = true;
                        }
                    }
                }

                if (!isRedirection)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sanitizes input by removing or escaping dangerous characters.
    /// </summary>
    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sanitized = input;
        
        foreach (var pattern in DangerousPatterns)
        {
            sanitized = sanitized.Replace(pattern, string.Empty, StringComparison.Ordinal);
        }

        return sanitized;
    }

    /// <summary>
    /// Validates that a command is safe to execute.
    /// This is a high-level validation that checks file and arguments together.
    /// </summary>
    public static bool IsSafeCommand(string file, string? arguments)
    {
        if (!IsValidFileName(file))
            return false;

        if (arguments != null && ContainsDangerousInput(arguments))
            return false;

        if (IsPowerShellExecutable(file) && arguments != null)
        {
            if (ContainsPowerShellDangerousPatterns(arguments))
                return false;
        }

        return true;
    }
}
