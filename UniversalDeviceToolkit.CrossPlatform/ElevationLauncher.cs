using System.Diagnostics;
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
        try
        {
            Process.Start(new ProcessStartInfo(fileName)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory
            }.WithArguments(launchArguments));

            return new ElevationLaunchResult(true, "Started an elevated UAC prompt for the requested command.");
        }
        catch (Exception ex)
        {
            return new ElevationLaunchResult(false, $"Could not start the elevated command: {ex.Message}");
        }
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
