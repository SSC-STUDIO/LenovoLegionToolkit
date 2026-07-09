using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LenovoLegionToolkit.Plugins.Shared;

/// <summary>
/// Safe process execution wrapper with input validation and security checks.
/// Prevents command injection and provides standardized process execution.
/// </summary>
public class ProcessRunner
{
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the ProcessRunner class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic messages</param>
    public ProcessRunner(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs a process safely with input validation and captures output.
    /// </summary>
    /// <param name="filePath">Path to the executable</param>
    /// <param name="arguments">Command line arguments</param>
    /// <param name="result">Standard output from the process</param>
    /// <param name="timeoutSeconds">Timeout in seconds (default: 30)</param>
    /// <returns>True if process exited successfully, false otherwise</returns>
    [Obsolete("Use RunProcessAsync instead — this synchronous overload lacks CancellationToken support and blocks the calling thread. " +
              "Callers that depend on TryRunProcess should migrate to RunProcessAsync(filePath, arguments, cancellationToken, timeoutSeconds) " +
              "for proper async cancellation and timeout handling.")]
    public bool TryRunProcess(string filePath, string arguments, out string result, int timeoutSeconds = Constants.DefaultTimeoutSeconds)
    {
        result = string.Empty;
        var effectiveTimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : Constants.DefaultTimeoutSeconds;

        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger?.LogError("ProcessRunner: File path is null or empty");
                return false;
            }

            // Path security checks
            if (IsDangerousPath(filePath))
            {
                _logger?.LogError("ProcessRunner: Potentially dangerous path detected: {FilePath}", filePath);
                return false;
            }

            // Argument security checks
            if (ContainsDangerousCharacters(arguments))
            {
                _logger?.LogError("ProcessRunner: Potentially dangerous arguments detected");
                return false;
            }

            // Ensure the file exists
            if (!File.Exists(filePath))
            {
                _logger?.LogError("ProcessRunner: File not found: {FilePath}", filePath);
                return false;
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            process.OutputDataReceived += (sender, e) => { if (e.Data != null) { outputBuilder.AppendLine(e.Data); } };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) { errorBuilder.AppendLine(e.Data); } };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (process.WaitForExit(effectiveTimeoutSeconds * 1000))
            {
                // Drain async readers completely before reading StringBuilder output
                process.WaitForExit();
                result = outputBuilder.ToString();
                var error = errorBuilder.ToString();

                if (process.ExitCode != 0)
                {
                    _logger?.LogWarning("Process exited with code {ExitCode}. Error: {Error}",
                        process.ExitCode, error);
                    return false;
                }

                return true;
            }
            else
            {
                _logger?.LogError("Process timed out after {Timeout} seconds", effectiveTimeoutSeconds);
                TryTerminateProcess(process);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ProcessRunner failed to execute process: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Runs a process asynchronously with cancellation support.
    /// </summary>
    public async Task<ProcessResult> RunProcessAsync(
        string filePath,
        string arguments,
        CancellationToken cancellationToken = default,
        int timeoutSeconds = Constants.DefaultTimeoutSeconds)
    {
        var effectiveTimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : Constants.DefaultTimeoutSeconds;

        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return ProcessResult.Failure("File path is null or empty");
            }

            if (IsDangerousPath(filePath))
            {
                return ProcessResult.Failure("Potentially dangerous path detected");
            }

            if (ContainsDangerousCharacters(arguments))
            {
                return ProcessResult.Failure("Potentially dangerous arguments detected");
            }

            if (!File.Exists(filePath))
            {
                return ProcessResult.Failure($"File not found: {filePath}");
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            process.OutputDataReceived += (sender, e) => { if (e.Data != null) { outputBuilder.AppendLine(e.Data); } };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) { errorBuilder.AppendLine(e.Data); } };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(effectiveTimeoutSeconds));

            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                var wasCancelled = cancellationToken.IsCancellationRequested;
                TryTerminateProcess(process);

                // Preserve partial output captured before the timeout/cancellation.
                // A process that hangs often emits diagnostic messages (error text,
                // progress info) before becoming unresponsive — discarding that data
                // makes the failure impossible to diagnose from logs alone.
                var partialOutput = outputBuilder.ToString();
                var partialError = errorBuilder.ToString();

                var reason = wasCancelled ? "Process cancelled" : "Process timed out";
                _logger?.LogError("{Reason}. Partial output: {Output}. Partial error: {Error}",
                    reason,
                    string.IsNullOrWhiteSpace(partialOutput) ? "(none)" : partialOutput,
                    string.IsNullOrWhiteSpace(partialError) ? "(none)" : partialError);

                return ProcessResult.Failure(
                    error: wasCancelled
                        ? $"Process cancelled. Partial stderr: {partialError}"
                        : $"Process timed out after {effectiveTimeoutSeconds}s. Partial stderr: {partialError}",
                    exitCode: -1,
                    output: partialOutput);
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (process.ExitCode != 0)
            {
                _logger?.LogWarning("Process exited with code {ExitCode}. Error: {Error}",
                    process.ExitCode, error);
                return ProcessResult.Failure(error, process.ExitCode, output);
            }

            return ProcessResult.Ok(output, process.ExitCode);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ProcessRunner failed to execute process: {FilePath}", filePath);
            return ProcessResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Checks if the path contains dangerous patterns (command injection prevention).
    /// </summary>
    private static bool IsDangerousPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        // M-005 fix: Only check for path traversal and null bytes.
        // Shell metacharacters are harmless in file paths when
        // UseShellExecute = false.
        if (path.Contains(".."))
        {
            return true;
        }

        if (path.Contains('\0'))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if arguments contain dangerous characters that could be used for injection.
    /// </summary>
    private static bool ContainsDangerousCharacters(string? arguments)
    {
        if (string.IsNullOrEmpty(arguments))
        {
            return false;
        }

        // Allow common argument characters but block shell metacharacters
        var dangerousPatterns = new[] { "&", "|", ";", "`", "$(", "${", "<", ">", "\n", "\r" };
        foreach (var pattern in dangerousPatterns)
        {
            if (arguments.Contains(pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        catch
        {
            // Best effort cleanup only.
        }
    }
}

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public class ProcessResult
{
    /// <summary>
    /// Indicates whether the process completed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// The standard output captured from the process.
    /// </summary>
    public string Output { get; }

    /// <summary>
    /// The standard error captured from the process, if any.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// The exit code returned by the process.
    /// </summary>
    public int ExitCode { get; }

    private ProcessResult(bool success, string output, string error, int exitCode)
    {
        Success = success;
        Output = output;
        Error = error;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Creates a successful process result.
    /// </summary>
    /// <param name="output">The captured output</param>
    /// <param name="exitCode">The exit code (defaults to 0)</param>
    /// <returns>A successful ProcessResult instance</returns>
    public static ProcessResult Ok(string output, int exitCode = 0)
        => new ProcessResult(true, output, string.Empty, exitCode);

    /// <summary>
    /// Creates a failed process result.
    /// </summary>
    /// <param name="error">The error message</param>
    /// <param name="exitCode">The exit code (defaults to -1)</param>
    /// <param name="output">Any partial output captured before failure</param>
    /// <returns>A failed ProcessResult instance</returns>
    public static ProcessResult Failure(string error, int exitCode = -1, string output = "")
        => new ProcessResult(false, output, error, exitCode);
}
