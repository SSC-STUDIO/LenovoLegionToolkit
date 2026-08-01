using System.Diagnostics;
using System.Linq;
using System.Reflection;

internal sealed record ElevationLaunchResult(
    bool Succeeded,
    string Detail);

internal sealed class ElevationLauncher
{
    public ElevationLaunchResult Launch(IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
        {
            return new ElevationLaunchResult(
                false,
                "Usage: udt elevate <command> [arguments]. Example: udt elevate set cpu-governor performance");
        }

        if (!OperatingSystem.IsWindows())
        {
            return new ElevationLaunchResult(
                false,
                "UAC elevation is available on Windows only. On Linux/macOS, rerun the command with sudo or the platform polkit helper when the OS requires elevated hardware-write permissions.");
        }

        var (fileName, launchArguments) = BuildWindowsLaunchCommand(arguments);

        foreach (var arg in launchArguments)
        {
            if (!IsSafeArgument(arg))
            {
                return new ElevationLaunchResult(
                    false,
                    $"Argument contains unsafe characters: '{arg}'. Rejected for security.");
            }
        }

        try
        {
            // UseShellExecute MUST be true for Verb="runas" to trigger the UAC prompt.
            // With UseShellExecute=false the Verb is silently ignored and the process
            // starts without elevation — the exact defect reported at line 41.
            var argumentString = string.Join(" ",
                launchArguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));

            var startInfo = new ProcessStartInfo(fileName)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory,
                Arguments = argumentString
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new ElevationLaunchResult(
                    false,
                    "Process.Start returned null — UAC elevation was denied or failed silently.");
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                return new ElevationLaunchResult(
                    false,
                    $"Elevated process exited with code {process.ExitCode}.");
            }

            return new ElevationLaunchResult(true, "Started the requested command.");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED — user clicked "No" on the UAC prompt.
            return new ElevationLaunchResult(false, "UAC elevation was cancelled by the user.");
        }
        catch (Exception ex)
        {
            return new ElevationLaunchResult(false, $"Could not start the elevated command: {ex.Message}");
        }
    }

    private static bool IsSafeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return false;

        foreach (var c in arg)
        {
            if (char.IsControl(c))
                return false;

            switch (c)
            {
                case '&':
                case '|':
                case ';':
                case '$':
                case '`':
                case '(':
                case ')':
                case '<':
                case '>':
                case '#':
                case '@':
                case '!':
                case '~':
                case '{':
                case '}':
                case '[':
                case ']':
                case '*':
                case '?':
                    return false;
            }
        }

        return true;
    }

    private static (string FileName, string[] Arguments) BuildWindowsLaunchCommand(IReadOnlyList<string> arguments)
    {
        var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(entryAssemblyLocation) &&
            string.Equals(Path.GetExtension(entryAssemblyLocation), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            return ("dotnet", [entryAssemblyLocation, .. arguments]);
        }

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
            return (processPath, arguments.ToArray());

        return ("dotnet", [entryAssemblyLocation ?? "udt.dll", .. arguments]);
    }
}
