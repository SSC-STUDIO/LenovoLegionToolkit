using System.Diagnostics;

namespace PluginTooling.Core;

public sealed class ProcessRunner
{
    public async Task<int> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string workingDirectory,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = PumpAsync(process.StandardOutput, log, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, log, cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask);

        return process.ExitCode;
    }

    public Task<int> RunDotnetAsync(
        IEnumerable<string> arguments,
        string workingDirectory,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        var dotnetHost = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (string.IsNullOrWhiteSpace(dotnetHost))
        {
            dotnetHost = "dotnet";
        }

        return RunAsync(dotnetHost, arguments, workingDirectory, log, cancellationToken);
    }

    private static async Task PumpAsync(StreamReader reader, Action<string>? log, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            log?.Invoke(line);
        }
    }
}
