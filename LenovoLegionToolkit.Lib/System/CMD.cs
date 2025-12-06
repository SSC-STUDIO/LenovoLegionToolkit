using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.System;

public static class CMD
{
    public static async Task<(int, string)> RunAsync(string file, string arguments, bool createNoWindow = true, bool waitForExit = true, Dictionary<string, string?>? environment = null, CancellationToken token = default)
    {
        // Input validation to prevent command injection
        if (!IsValidFileName(file))
            throw new ArgumentException("Invalid file name", nameof(file));
        if (arguments != null && ContainsDangerousInput(arguments))
            throw new ArgumentException("Arguments contain dangerous characters", nameof(arguments));

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Running... [file={file}, argument={arguments}, createNoWindow={createNoWindow}, waitForExit={waitForExit}, environment=[{(environment is null ? string.Empty : string.Join(",", environment))}]");

        Process cmd = new Process();
        try
        {
        cmd.StartInfo.UseShellExecute = false;
        cmd.StartInfo.CreateNoWindow = createNoWindow;
        cmd.StartInfo.RedirectStandardOutput = createNoWindow;
        cmd.StartInfo.RedirectStandardError = createNoWindow;
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
                    // ProcessStartInfo.Environment does not accept null values
                    cmd.StartInfo.Environment.Remove(key);
                }
                else if (!ContainsDangerousInput(value))
                {
                    // Only set the value if it's not null and doesn't contain dangerous input
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
                Log.Instance.Trace($"Ran [file={file}, argument={arguments}, createNoWindow={createNoWindow}, waitForExit={waitForExit}, environment=[{(environment is null ? string.Empty : string.Join(",", environment))}]");

                // When waitForExit is false, the process runs asynchronously.
                // We must not dispose the Process object while the process is still running,
                // as disposing may close redirected streams and affect the running process.
                // The process will continue running independently, and the Process object
                // will be garbage collected when no longer referenced.
                // Note: This intentionally leaks the Process object to allow the process to complete.
                cmd = null!; // Release reference, process continues running
            return (-1, string.Empty);
        }

        await cmd.WaitForExitAsync(token).ConfigureAwait(false);

        var exitCode = cmd.ExitCode;
        var output = createNoWindow ? await cmd.StandardOutput.ReadToEndAsync(token).ConfigureAwait(false) : string.Empty;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Ran [file={file}, argument={arguments}, createNoWindow={createNoWindow}, waitForExit={waitForExit}, exitCode={exitCode} output={output}]");

        return (exitCode, output);
        }
        finally
        {
            // Only dispose if we're waiting for exit (waitForExit = true)
            // When waitForExit is false, the process runs asynchronously and we don't dispose
            // to avoid closing redirected streams that the process may still be using
            if (waitForExit && cmd is not null)
            {
                cmd.Dispose();
            }
        }
    }

    private static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Check for dangerous characters and paths in the original input BEFORE normalization
        // This prevents directory traversal attacks that could bypass checks after Path.GetFullPath normalization
        if (fileName.Contains("..") || fileName.Contains('|') || fileName.Contains('&') || fileName.Contains(';') || fileName.Contains('*') || fileName.Contains('?'))
            return false;

        try
        {
            // Check if it's a valid file path
            // Path.GetFullPath normalizes relative paths, which could bypass ".." checks
            // So we validate the original input first, then validate the normalized result
            var path = Path.GetFullPath(fileName);
            
            // Validate the full path for dangerous characters after normalization
            // Even though we checked the original input, we also check the normalized path
            // to catch any edge cases where normalization might introduce dangerous patterns
            if (path.Contains("..") || path.Contains('|') || path.Contains('&') || path.Contains(';') || path.Contains('*') || path.Contains('?'))
                return false;
            
            var fileInfo = new FileInfo(path);
            
            // Validate the filename portion using Windows invalid character list
            // This is more permissive than a whitelist and allows legitimate Windows paths
            // like "Program Files (x86)\App\tool.exe" while still blocking dangerous characters
            var fileNameOnly = fileInfo.Name;
            if (string.IsNullOrWhiteSpace(fileNameOnly))
                return false;
            
            // Use Windows' built-in invalid character list to validate the filename
            // This allows all valid Windows filename characters including parentheses, brackets, etc.
            // while still blocking dangerous characters like < > : " / \ | ? *
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileNameOnly.IndexOfAny(invalidChars) >= 0)
                return false;
            
            // Ensure the directory portion exists or is valid (if it's a relative path that was expanded)
            // Path.GetFullPath will throw if the path is invalid, so if we get here, the path structure is valid
            return true;
        }
        catch
        {
            return false;
        }
    }

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

    public static bool ContainsDangerousInput(string input)
    {
        // Check for dangerous characters that could be used for command injection
        // Note: ">" and "<" are allowed for output redirection (e.g., ">nul 2>&1") as they are safe in cmd.exe context
        // Use the same patterns as WindowsOptimizationService for consistency
        string[] dangerousPatterns = { "&&", "||", "|", ";", "`", "$(" };
        foreach (string pattern in dangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.Ordinal))
                return true;
        }

        // Check for single "&" command separator (but allow "2>&1" and "1>&2" for output redirection)
        // Single "&" can be used to chain commands: "command1 & command2"
        // We check for " & " (with spaces on both sides) which is the most common command chaining pattern
        // We also check for "& " (ampersand followed by space) and " &" (space before ampersand)
        // but exclude "2>&1" and "1>&2" patterns where & is part of output redirection
        
        // Check for " & " (space before and after) - this is definitely command chaining
        if (input.Contains(" & ", StringComparison.Ordinal))
            return true;
        
        // Check for "& " (ampersand followed by space) - but exclude "2>&1" and "1>&2"
        var index = input.IndexOf("& ", StringComparison.Ordinal);
        if (index >= 0)
        {
            // If at start or if previous character is not '>' (not part of "2>&1" or "1>&2")
            if (index == 0 || (index > 0 && input[index - 1] != '>'))
                return true;
        }

        // Check for " &" (space before ampersand) - but exclude "2>&1" and "1>&2"
        index = input.IndexOf(" &", StringComparison.Ordinal);
        if (index >= 0)
        {
            // Check if this is part of a valid redirection pattern (2>&1 or 1>&2)
            // index points to the space in " &", so:
            // - input[index-2] and input[index-1] should be "2>" or "1>"
            // - input[index+1] should be '&'
            // - input[index+2] should be '1' or '2'
            bool isRedirectionPattern = false;
            if (index >= 2 && index + 2 < input.Length)
            {
                var charBeforeSpace = input[index - 1]; // Should be '>'
                var charTwoBeforeSpace = input[index - 2]; // Should be '2' or '1'
                var charAfterSpace = input[index + 1]; // Should be '&'
                var charAfterAmpersand = input[index + 2]; // Should be '1' or '2'
                
                // Valid patterns: "2>&1" or "1>&2"
                if (charBeforeSpace == '>' && charAfterSpace == '&' &&
                    ((charTwoBeforeSpace == '2' && charAfterAmpersand == '1') ||
                     (charTwoBeforeSpace == '1' && charAfterAmpersand == '2')))
                {
                    isRedirectionPattern = true;
                }
            }
            
            // If it's not a valid redirection pattern, it's potentially dangerous
            if (!isRedirectionPattern)
                return true;
        }

        return false;
    }
}