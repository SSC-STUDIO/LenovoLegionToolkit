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
    public static async Task<(int, string)> RunAsync(string file, string? arguments, bool createNoWindow = true, bool waitForExit = true, Dictionary<string, string?>? environment = null, CancellationToken token = default)
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

            Task<string>? standardOutputTask = null;
            Task<string>? standardErrorTask = null;
            if (shouldRedirectOutput)
            {
                // Start draining redirected streams before waiting for process exit to avoid deadlocks
                // when the child process writes enough data to fill stdout/stderr buffers.
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

        // Check for dangerous characters in the original input BEFORE normalization
        // This prevents directory traversal attacks
        // Note: & and ; are valid characters in Windows directory names (e.g., "Program Files (x) & Co")
        // They should only be rejected when used as command separators in arguments, not in file paths
        // The ContainsDangerousInput method handles argument validation separately
        if (fileName.Contains("..") || fileName.Contains('|'))
            return false;

        try
        {
            // Check if it's a valid file path
            // Path.GetFullPath normalizes relative paths, which could expand ".." patterns
            // So we validate the original input first, then validate the normalized result
            var path = Path.GetFullPath(fileName);
            
            // After normalization, check again for directory traversal (in case normalization introduced it)
            // Note: | is not valid in Windows paths, but & and ; are valid in directory names
            if (path.Contains("..") || path.Contains('|'))
                return false;
            
            var fileInfo = new FileInfo(path);
            
            // Validate the filename portion using Windows invalid character list
            // This is more permissive than a whitelist and allows legitimate Windows paths
            // like "Program Files (x86)\App\tool.exe" while still blocking dangerous characters
            var fileNameOnly = fileInfo.Name;
            if (string.IsNullOrWhiteSpace(fileNameOnly))
                return false;
            
            // Use Windows' built-in invalid character list to validate the filename
            // This includes wildcards (*, ?) which are not valid in actual executable filenames
            // but are legitimate in file patterns/glob expressions - however, for file execution
            // we need a specific file, not a pattern, so we reject wildcards in the filename
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
        if (string.IsNullOrEmpty(input))
            return false;

        // Check for dangerous characters that could be used for command injection
        // Note: ">" and "<" are allowed for output redirection (e.g., ">nul 2>&1") as they are safe in cmd.exe context
        // Use the same patterns as WindowsOptimizationService for consistency
        string[] dangerousPatterns = { "&&", "||", "|", ";", "`", "$(" };
        foreach (string pattern in dangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.Ordinal))
                return true;
        }

        // Check for single "&" command separator (but allow "2>&1" and "1>&2" for output redirection).
        // We scan all ampersands to block command chaining patterns with or without spaces.
        var index = input.IndexOf('&');
        while (index >= 0)
        {
            // Allow escaped ampersand (e.g. "^&") used to print literal '&' in cmd.exe.
            if (index > 0 && input[index - 1] == '^')
            {
                index = input.IndexOf('&', index + 1);
                continue;
            }

            var isRedirectionPattern = false;
            if (index > 0 && index + 1 < input.Length && input[index - 1] == '>')
            {
                var targetDescriptor = input[index + 1];
                if (targetDescriptor is '1' or '2')
                {
                    // Explicit descriptor redirect: 1>&2 / 2>&1
                    if (index >= 2 && input[index - 2] is '1' or '2')
                    {
                        isRedirectionPattern = true;
                    }
                    // Implicit descriptor redirect commonly used as: >&2
                    else if (index < 2 || char.IsWhiteSpace(input[index - 2]))
                    {
                        isRedirectionPattern = true;
                    }
                }
            }

            if (!isRedirectionPattern)
                return true;

            index = input.IndexOf('&', index + 1);
        }

        return false;
    }
}
