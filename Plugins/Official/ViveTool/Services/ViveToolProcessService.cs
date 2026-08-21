using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.Core;
using UniversalDeviceToolkit.Plugins.ViveTool.Utils;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Services;

/// <summary>
/// Handles process execution for ViVeTool commands.
/// </summary>
public class ViveToolProcessService
{
    /// <summary>
    /// Executes a ViVeTool command and returns the result.
    /// </summary>
    public async Task<(bool Success, string? Output, string? Error)> ExecuteCommandAsync(
        string viveToolPath,
        string arguments)
    {
        try
        {
            if (!ViveToolPathGuard.TryNormalizeExecutablePath(viveToolPath, out var normalizedPath))
            {
                return (false, null, "Potentially dangerous path detected");
            }

            if (ContainsDangerousCharacters(arguments))
            {
                return (false, null, "Potentially dangerous arguments detected");
            }

            if (!File.Exists(normalizedPath))
            {
                return (false, null, $"File not found: {normalizedPath}");
            }

            var workingDirectory = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
            {
                workingDirectory = Environment.CurrentDirectory;
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = normalizedPath,
                    Arguments = arguments ?? string.Empty,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            if (!process.Start())
            {
                return (false, null, "Failed to start process");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Constants.DefaultTimeoutSeconds));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryTerminateProcess(process);
                var partialOutput = await AwaitProcessTextAsync(outputTask).ConfigureAwait(false);
                var partialError = await AwaitProcessTextAsync(errorTask).ConfigureAwait(false);
                return (false, partialOutput, $"Process timed out after {Constants.DefaultTimeoutSeconds}s. Partial stderr: {partialError}");
            }

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return (false, output, error);
            }

            return (true, output, error);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error executing command: {ex.Message}", ex);
            return (false, null, ex.Message);
        }
    }

    private static bool ContainsDangerousCharacters(string? arguments)
    {
        if (string.IsNullOrEmpty(arguments))
        {
            return false;
        }

        ReadOnlySpan<string> dangerousPatterns = ["&", "|", ";", "`", "$(", "${", "<", ">", "\n", "\r"];
        foreach (var pattern in dangerousPatterns)
        {
            if (arguments.Contains(pattern, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<string> AwaitProcessTextAsync(Task<string> textTask)
    {
        try
        {
            var completed = await Task.WhenAny(textTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (completed == textTask)
            {
                return await textTask.ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Failed to drain process output after timeout: {ex.Message}", ex);
        }

        return string.Empty;
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            process.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Failed to terminate process: {ex.Message}", ex);
        }
    }
}
